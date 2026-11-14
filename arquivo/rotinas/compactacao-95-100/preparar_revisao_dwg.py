import json,os
from collections import defaultdict
from compactacao import segmentos,separar,cota
ROOT=os.path.dirname(__file__)
data=json.load(open(os.path.join(ROOT,'inspecao','entidades.json'),encoding='utf-8'))
pairs=defaultdict(dict);views=defaultdict(list)
for r in data:
    if r['type'].endswith('.Section'):
        if r['Name'].startswith('GO-220_Prim_Natural Caiapo-'):pairs[r['SampleLineId']]['n']=r
        elif 'GO-220 Executivo R01 - TOP-' in r['Name']:pairs[r['SampleLineId']]['p']=r
    elif r['type'].endswith('.SectionView'):views[r['SampleLineId']].append(r['handle'])
def clip(segs,dz=0):
    out=[]
    for s in segs:
        a,b=max(-6.7,s[0]),min(6.7,s[2])
        if b-a>1e-9:out.append((a,cota(s,a)+dz,b,cota(s,b)+dz))
    if abs(sum(s[2]-s[0] for s in out)-13.4)>1e-5:raise ValueError('Plataforma incompleta')
    return out
records=[]
for sid,pair in pairs.items():
    if len(pair)!=2:raise ValueError('Falta secao')
    station=float(pair['p']['Station'])
    if not (0<=station<=6000 or 7000<=station<=8000):continue
    top=clip(segmentos(pair['p']['points']))
    areas,polys=separar(clip(segmentos(pair['n']['points'])),clip(top,-1))
    for a,za,b,zb in top:
        polys.append({'categoria':4,'pontos':[[a,za-1],[b,zb-1],[b,zb-.8],[a,za-.8]]})
    lines=[]
    for depth in (0,.2,.4,.8,1):
        for a,za,b,zb in top:lines.append([[a,za-depth],[b,zb-depth]])
    records.append({'station':station,'areas':areas+[2.68],'poligonos':polys,'views':views[sid],'linhas':lines})
records.sort(key=lambda r:r['station'])
json.dump({'secoes':records},open(os.path.join(ROOT,'resultado_revisao_dwg.json'),'w',encoding='utf-8'))
print('Secoes da medicao:',len(records))
