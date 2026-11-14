"""Prepara uma conferência rastreável, sem gerar greide executivo ou alterar DWG.

Entrada: CSV de projeção produzido por diagnosticar_levantamento.py e LandXML.
Os limiares classificam candidatos nesta amostra; não são normas de engenharia.
"""
import argparse
import csv
import json
import math
from pathlib import Path
from statistics import median
from diagnosticar_levantamento import read_axis

def split_groups(rows, gap):
    groups=[]
    for row in sorted(rows,key=lambda r:r['station']):
        if not groups or row['station']-groups[-1][-1]['station']>gap:
            groups.append([])
        groups[-1].append(row)
    return groups

def original_candidates(group, axis_band, min_step, max_step):
    near=sorted((r for r in group if abs(r['offset'])<=axis_band),key=lambda r:r['z'])
    if len(near)<2: return set(), None
    steps=[(near[i+1]['z']-near[i]['z'], i) for i in range(len(near)-1)]
    candidates=[(step,i) for step,i in steps if min_step<=step<=max_step]
    if len(candidates)!=1: return set(), None
    step,i=candidates[0]
    refs=near[i+1:]
    if refs[-1]['z']-refs[0]['z']>.03: return set(), None
    return {r['number'] for r in refs},step

def main():
    p=argparse.ArgumentParser()
    p.add_argument('projection'); p.add_argument('axis'); p.add_argument('output')
    p.add_argument('--group-gap',type=float,default=2)
    p.add_argument('--axis-band',type=float,default=.5)
    p.add_argument('--step-min',type=float,default=.15)
    p.add_argument('--step-max',type=float,default=.45)
    a=p.parse_args()
    if not (a.group_gap>0 and a.axis_band>0 and 0<a.step_min<a.step_max):
        raise ValueError('Parâmetros de classificação inválidos.')
    axis,segs,_=read_axis(a.axis)
    with open(a.projection,encoding='utf-8-sig',newline='') as stream:
        rows=list(csv.DictReader(stream,delimiter=';'))
    for row in rows:
        for k in ['e','n','z','station','offset','distance','foot_e','foot_n']:
            row[k]=float(row[k])
            if not math.isfinite(row[k]): raise ValueError('Dado não finito.')
        row['segment']=int(row['segment'])
    sections=[]; points=[]
    for index,group in enumerate(split_groups(rows,a.group_gap),1):
        station=median(r['station'] for r in group)
        ref_ids,step=original_candidates(group,a.axis_band,a.step_min,a.step_max)
        refs=[r for r in group if r['number'] in ref_ids]
        # Um candidato ausente não vira automaticamente fundo perto do eixo.
        floor=[r for r in group if r['number'] not in ref_ids and
               (bool(ref_ids) or abs(r['offset'])>a.axis_band)]
        right=[r for r in floor if 0<=r['offset']<=3.5]
        near_floor=min(floor,key=lambda r:abs(r['offset'])) if floor else None
        exact_curve=any(segs[r['segment']]['kind']!='Line' for r in group)
        # Limite inferior somente em tangente e somente nos pontos efetivamente medidos.
        lower=max((r['z']+.23+.02*r['offset'] for r in right),default=None) if not exact_curve else None
        messages=[]
        if not refs: messages.append('SEM REFERÊNCIA ORIGINAL IDENTIFICADA')
        if near_floor is None or abs(near_floor['offset'])>.5: messages.append('SEM FUNDO PRÓXIMO AO EIXO')
        if len(right)<2: messages.append('POUCOS CONTROLES NA FAIXA 0 A 3,50')
        if exact_curve: messages.append('SUPERELEVAÇÃO E TRANSIÇÃO PENDENTES')
        messages.append('ESQUERDA NÃO LEVANTADA')
        sections.append(dict(section=index,station=station,station_min=min(r['station'] for r in group),
            station_max=max(r['station'] for r in group),point_count=len(group),
            original_ids=', '.join(sorted(ref_ids)),original_z=median(r['z'] for r in refs) if refs else None,
            near_floor_id=near_floor['number'] if near_floor else '',
            near_floor_offset=near_floor['offset'] if near_floor else None,
            near_floor_z=near_floor['z'] if near_floor else None,
            measured_step=step,right_floor_count=len(right),
            axis_lower_bound_tangent=lower,geometry='Curva' if exact_curve else 'Reta',
            status='; '.join(messages)))
        for row in group:
            if row['number'] in ref_ids: role='REFERÊNCIA ORIGINAL — CANDIDATO'
            elif not ref_ids and abs(row['offset'])<=a.axis_band: role='PRÓXIMO AO EIXO — REVISAR PAPEL'
            elif row['offset']<0: role='PONTO ISOLADO À ESQUERDA — REVISAR'
            elif row['offset']>3.5: role='FUNDO EXTERNO À FAIXA 3,50 — REVISAR USO'
            else: role='FUNDO DIREITO — CANDIDATO'
            points.append(dict(section=index,number=row['number'],e=row['e'],n=row['n'],z=row['z'],
                               description=row['description'],station=row['station'],offset=row['offset'],
                               role=role,segment=row['segment']))
    out=Path(a.output)
    if out.exists(): raise ValueError('Escolha uma nova pasta; saídas anteriores são preservadas.')
    out.mkdir(parents=True)
    params=dict(group_gap=a.group_gap,axis_band=a.axis_band,min_step=a.step_min,max_step=a.step_max,
                min_thickness=.23,design_speed=60,max_superelevation_percent=6,
                note='Parâmetros de classificação exploratórios; conferir candidatos antes do cálculo executivo.')
    data=dict(axis=axis.get('name'),source_projection=str(Path(a.projection).resolve()),
              source_axis=str(Path(a.axis).resolve()),parameters=params,sections=sections,points=points)
    (out/'conferencia.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    for filename,table in [('secoes.csv',sections),('pontos_classificados.csv',points)]:
        with (out/filename).open('w',encoding='utf-8-sig',newline='') as stream:
            writer=csv.DictWriter(stream,fieldnames=list(table[0]),delimiter=';')
            writer.writeheader();writer.writerows(table)
    summary=dict(points=len(points),sections=len(sections),sections_with_original=sum(bool(s['original_ids']) for s in sections),
                 without_original=[round(s['station'],3) for s in sections if not s['original_ids']],
                 without_near_floor=[round(s['station'],3) for s in sections if s['near_floor_offset'] is None or abs(s['near_floor_offset'])>.5],
                 curve_sections=sum(s['geometry']=='Curva' for s in sections),
                 references=sum('REFERÊNCIA ORIGINAL' in r['role'] for r in points))
    print(json.dumps(summary,ensure_ascii=False,indent=2))

if __name__=='__main__': main()
