"""Terraplanagem restrita a plataforma; nao redesenha taludes."""
import json,csv,os
from collections import defaultdict
from compactacao import segmentos,separar,volumes,cota,CATEGORIAS
ROOT=os.path.dirname(__file__)
data=json.load(open(os.path.join(ROOT,'inspecao','entidades.json'),encoding='utf-8'))
pairs=defaultdict(dict)
for r in data:
    if not r['type'].endswith('.Section'):
        continue
    if r['Name'].startswith('GO-220_Prim_Natural Caiapo-'):
        pairs[r['SampleLineId']]['natural']=r
    elif 'GO-220 Executivo R01 - TOP-' in r['Name']:
        pairs[r['SampleLineId']]['projeto']=r

def recortar(segs,shift=0):
    result=[]
    for s in segs:
        a,b=max(-6.7,s[0]),min(6.7,s[2])
        if b-a>1e-9:
            result.append((a,cota(s,a)+shift,b,cota(s,b)+shift))
    if abs(sum(s[2]-s[0] for s in result)-13.4)>1e-5:
        raise ValueError('A superficie nao cobre toda a plataforma de 13,40 m')
    return result

sections=[]
for pair in pairs.values():
    if len(pair)!=2:
        raise ValueError('Falta uma superficie na secao')
    n,p=pair['natural'],pair['projeto']
    a,poly=separar(recortar(segmentos(n['points'])),recortar(segmentos(p['points']),-1.0))
    sections.append({'name':p['Name'],'station':float(p['Station']),'areas':a})
sections.sort(key=lambda r:r['station'])
summary=[]
details=[]
for label,start,end in [('0 a 300',0.0,6000.0),('350 a 400',7000.0,8000.0)]:
    subset=[dict(r) for r in sections if start<=r['station']<=end]
    if not subset or subset[0]['station']!=start or subset[-1]['station']!=end:
        raise ValueError('Falta secao de limite no trecho '+label)
    total=volumes(subset)
    summary.append({'trecho':label,'secoes':len(subset),'volumes_m3':dict(zip(CATEGORIAS,total))})
    details.extend((label,r) for r in subset)
totals={cat:sum(r['volumes_m3'][cat] for r in summary) for cat in CATEGORIAS}
payload={'escopo':'Somente plataforma -6,70 a +6,70 m; fundo = TOP -1,00 m em cota vertical. Taludes fora da plataforma nao incluidos nem redesenhados.',
         'referencia_anterior':'Os arquivos GO-220_Compactacao_95_100.dwg e quantitativos.csv usam TOP e nao representam esta revisao.',
         'trechos':summary,'totais_m3':totals,'subleito':'Pendente confirmar posicao dos 20 cm em relacao ao pacote de 1 m.'}
json.dump(payload,open(os.path.join(ROOT,'medicao_fundo_plataforma.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=2)
def num(x):return format(x,'.3f').replace('.',',')
with open(os.path.join(ROOT,'medicao_fundo_plataforma_resumo.csv'),'w',encoding='utf-8-sig',newline='') as f:
    w=csv.writer(f,delimiter=';');w.writerow(['Trecho - somente plataforma']+[cat+' (m3)' for cat in CATEGORIAS])
    for r in summary:w.writerow([r['trecho']]+[num(r['volumes_m3'][cat]) for cat in CATEGORIAS])
    w.writerow(['TOTAL']+[num(totals[cat]) for cat in CATEGORIAS])
with open(os.path.join(ROOT,'medicao_fundo_plataforma_secoes.csv'),'w',encoding='utf-8-sig',newline='') as f:
    w=csv.writer(f,delimiter=';');w.writerow(['Trecho','Estaca (20 m)','Progressiva m']+[cat+' m2' for cat in CATEGORIAS]+[cat+' m3 intervalo anterior' for cat in CATEGORIAS])
    for label,r in details:w.writerow([label,num(r['station']/20),num(r['station'])]+[num(x) for x in r['areas']]+[num(x) for x in r.get('volumes_intervalo',[0]*4)])
print(json.dumps(payload,ensure_ascii=False,indent=2))
