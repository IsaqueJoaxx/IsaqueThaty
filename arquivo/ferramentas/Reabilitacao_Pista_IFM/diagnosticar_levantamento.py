"""Diagnóstico somente leitura: pontos Número,E,N,Z,Descrição versus eixo LandXML.

Não classifica automaticamente pavimento, meio-fio ou eixo levantado; não gera
nota de serviço. Progressiva é acumulada pelos comprimentos declarados no XML.
"""
import argparse
import csv
import json
import math
from pathlib import Path
import xml.etree.ElementTree as ET

NS = {'l': 'http://www.landxml.org/schema/LandXML-1.2'}
TAU = 2 * math.pi

def xy(node, tag):
    v = [float(x) for x in node.find('l:' + tag, NS).text.split()]
    return v[1], v[0]  # LandXML: N,E; entrada CSV: E,N

def read_axis(path):
    raw = Path(path).read_bytes()
    if b'<!DOCTYPE' in raw.upper() or b'<!ENTITY' in raw.upper():
        raise ValueError('DTD/entidades não são aceitos.')
    root = ET.fromstring(raw)
    unit = root.find('l:Units/l:Metric', NS)
    if unit is None or unit.get('linearUnit') != 'meter':
        raise ValueError('Eixo precisa estar em metros.')
    axes = root.findall('.//l:Alignment', NS)
    if len(axes) != 1:
        raise ValueError('Este diagnóstico requer exatamente um alinhamento no XML.')
    axis = axes[0]
    station = float(axis.get('staStart'))
    segments = []
    for i, node in enumerate(axis.find('l:CoordGeom', NS)):
        kind = node.tag.split('}')[-1]
        if kind not in ('Line', 'Curve'):
            raise ValueError('Geometria ainda não atendida: ' + kind)
        a, b = xy(node, 'Start'), xy(node, 'End')
        length = float(node.get('length'))
        segment = dict(index=i, kind=kind, start=station, length=length, a=a, b=b)
        if kind == 'Curve':
            center = xy(node, 'Center')
            radius = float(node.get('radius'))
            sign = 1 if node.get('rot') == 'ccw' else -1
            start_angle = math.atan2(a[1]-center[1], a[0]-center[0])
            end_angle = math.atan2(b[1]-center[1], b[0]-center[0])
            sweep = ((end_angle-start_angle)*sign) % TAU
            if radius <= 0 or sweep <= 1e-12 or abs(radius*sweep-length) > max(.02, length*1e-5):
                raise ValueError(f'Curva {i}: arco e comprimento inconsistentes.')
            segment.update(center=center, radius=radius, sign=sign, angle=start_angle, sweep=sweep)
        elif math.dist(a,b) < 1e-9:
            raise ValueError('Reta degenerada.')
        segments.append(segment)
        station += length
    return axis, segments, station

def project(e, n, s):
    a, b = s['a'], s['b']
    if s['kind'] == 'Line':
        dx, dy = b[0]-a[0], b[1]-a[1]
        norm = math.hypot(dx,dy)
        t = max(0, min(1, ((e-a[0])*dx+(n-a[1])*dy)/(norm*norm)))
        foot = a[0]+t*dx, a[1]+t*dy
        tangent = dx/norm, dy/norm
    else:
        c, radius, sign = s['center'], s['radius'], s['sign']
        angle = math.atan2(n-c[1],e-c[0])
        progress = ((angle-s['angle'])*sign) % TAU
        if progress <= s['sweep']:
            t = progress/s['sweep']
        else:
            t = 0 if math.dist((e,n),a) <= math.dist((e,n),b) else 1
        angle = s['angle']+sign*t*s['sweep']
        foot = c[0]+radius*math.cos(angle), c[1]+radius*math.sin(angle)
        tangent = -sign*math.sin(angle), sign*math.cos(angle)
    de, dn = e-foot[0], n-foot[1]
    return dict(station=s['start']+t*s['length'], offset=de*tangent[1]-dn*tangent[0],
                distance=math.hypot(de,dn), foot_e=foot[0], foot_n=foot[1], segment=s['index'])

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('axis'); parser.add_argument('points'); parser.add_argument('output')
    args = parser.parse_args()
    axis, segments, axis_end = read_axis(args.axis)
    rows = []
    seen = set()
    with open(args.points, encoding='utf-8-sig', newline='') as stream:
        for line, row in enumerate(csv.reader(stream),1):
            if not row or all(not c.strip() for c in row): continue
            if len(row) < 5: raise ValueError(f'Linha {line}: esperado número,E,N,Z,descrição.')
            ident = row[0].strip()
            if ident in seen: raise ValueError('Número repetido: ' + ident)
            seen.add(ident)
            e,n,z = map(float,row[1:4])
            if not all(math.isfinite(v) for v in (e,n,z)): raise ValueError('Coordenada não finita.')
            candidates = sorted((project(e,n,s) for s in segments), key=lambda p:p['distance'])
            best = candidates[0]
            rival = next((p for p in candidates[1:] if abs(p['station']-best['station'])>20), None)
            rows.append(dict(number=ident,e=e,n=n,z=z,description=','.join(row[4:]).strip(),**best,
                             competing_station=None if rival is None else rival['station'],
                             competing_distance=None if rival is None else rival['distance']))
    if not rows: raise ValueError('Sem pontos.')
    out = Path(args.output)
    out.mkdir(parents=True,exist_ok=True)
    csv_path = out/'pontos_projetados.csv'
    if csv_path.exists(): raise ValueError('Saída já existe; não sobrescrever diagnóstico anterior.')
    with csv_path.open('w',encoding='utf-8-sig',newline='') as stream:
        writer=csv.DictWriter(stream,fieldnames=list(rows[0]),delimiter=';')
        writer.writeheader(); writer.writerows(rows)
    # Agrupamento exploratório por proximidade longitudinal, sem afirmar estacas de projeto.
    groups=[]
    for row in sorted(rows,key=lambda r:r['station']):
        if not groups or row['station']-groups[-1][-1]['station']>2:
            groups.append([])
        groups[-1].append(row)
    summary=dict(axis=axis.get('name'),axis_start=float(axis.get('staStart')),axis_end=axis_end,
                 point_count=len(rows),descriptions=sorted({r['description'] for r in rows}),
                 point_station_min=min(r['station'] for r in rows),point_station_max=max(r['station'] for r in rows),
                 offset_min=min(r['offset'] for r in rows),offset_max=max(r['offset'] for r in rows),
                 max_distance=max(r['distance'] for r in rows),
                 competing_candidates=sum(r['competing_distance'] is not None and r['competing_distance']-r['distance']<.5 for r in rows),
                 exploratory_groups=[dict(station_min=g[0]['station'],station_max=g[-1]['station'],count=len(g),
                                          offset_min=min(r['offset'] for r in g),offset_max=max(r['offset'] for r in g),
                                          elevation_min=min(r['z'] for r in g),elevation_max=max(r['z'] for r in g)) for g in groups])
    (out/'diagnostico.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in summary.items() if k!='exploratory_groups'},ensure_ascii=False,indent=2))
    print('Grupos exploratórios:',len(groups))
    print(json.dumps(summary['exploratory_groups'][:5],ensure_ascii=False,indent=2))

if __name__=='__main__': main()
