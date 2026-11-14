import json,csv,os
from collections import defaultdict
from compactacao import segmentos,separar,volumes,CATEGORIAS
ROOT=os.path.dirname(__file__)
data=json.load(open(os.path.join(ROOT,'inspecao','entidades.json'),encoding='utf-8'))
samples={r['ObjectId']:r for r in data if r['type'].endswith('.SampleLine')}
sections=defaultdict(dict)
views=defaultdict(list)
for r in data:
    if r['type'].endswith('.Section'):
        if r['Name'].startswith('GO-220_Prim_Natural Caiapo-'):
            sections[r['SampleLineId']]['n']=r
        elif 'GO-220 Executivo R01 - TOP-' in r['Name']:
            sections[r['SampleLineId']]['p']=r
    elif r['type'].endswith('.SectionView'):
        views[r['SampleLineId']].append(r['handle'])
groups=defaultdict(list)
errors=[]
for sid,s in sections.items():
    sl=samples[sid]
    try:
        if set(s)!=set(('n','p')):
            raise ValueError('Falta uma das secoes de superficie')
        a,poly=separar(segmentos(s['n']['points']),segmentos(s['p']['points']))
        groups[sl['GroupId']].append({'sample':sl['handle'],'name':sl['Name'],'station':float(sl['Station']),'areas':a,'poligonos':poly,'views':views[sid]})
    except Exception as exc:
        errors.append({'sample':sl['handle'],'name':sl['Name'],'erro':str(exc)})
result=[]
summary=[]
for gid,records in groups.items():
    records.sort(key=lambda r:r['station'])
    # Nao atravessar uma secao que falhou: exigir todos os samples intermediarios.
    all_samples=sorted([s for s in samples.values() if s['GroupId']==gid],key=lambda s:float(s['Station']))
    position={s['handle']:i for i,s in enumerate(all_samples)}
    total=[0.0]*4
    for prev,cur in zip(records,records[1:]):
        if position[cur['sample']]-position[prev['sample']]!=1:
            cur['intervalo_pendente']=True
            continue
        v=volumes([prev,cur])
        total=[a+b for a,b in zip(total,v)]
    summary.append({'group':gid,'secoes':len(records),'totais_m3':total})
    result.extend(records)
payload={'regra':'100% no primeiro metro da envoltoria superior; 95% no excedente','categorias':CATEGORIAS,'secoes':result,'grupos':summary,'erros':errors}
json.dump(payload,open(os.path.join(ROOT,'resultado.json'),'w',encoding='utf-8'),ensure_ascii=False)
with open(os.path.join(ROOT,'quantitativos.csv'),'w',newline='',encoding='utf-8-sig') as f:
    w=csv.writer(f,delimiter=';');w.writerow(['Grupo','Secao','Progressiva m']+[x+' m2' for x in CATEGORIAS]+[x+' m3 intervalo anterior' for x in CATEGORIAS])
    for gid,records in groups.items():
        for r in records:
            w.writerow([gid,r['name'],str(r['station']).replace('.',',')]+[format(x,'.6f').replace('.',',') for x in r['areas']]+[format(x,'.6f').replace('.',',') for x in r.get('volumes_intervalo',[0]*4)])
print(json.dumps({'grupos':summary,'erros':errors[:8],'total_erros':len(errors),'total_secoes':len(result)},ensure_ascii=False,indent=2))
