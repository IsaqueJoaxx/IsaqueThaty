"""Mede os trechos isoladamente, sem atravessar o intervalo excluido."""
import csv,json,os
from compactacao import volumes,CATEGORIAS
ROOT=os.path.dirname(__file__)
d=json.load(open(os.path.join(ROOT,'resultado.json'),encoding='utf-8'))
rows=sorted(d['secoes'],key=lambda r:r['station'])
res=[]
for nome,a,b in [('Estacas 0 a 300',0,6000),('Estacas 350 a 400',7000,8000)]:
    subset=[dict(r) for r in rows if a<=r['station']<=b]
    if not subset or subset[0]['station']!=a or subset[-1]['station']!=b:
        raise ValueError('Falta secao nos limites de '+nome)
    total=volumes(subset)
    res.append({'trecho':nome,'inicio_m':a,'fim_m':b,'secoes':len(subset),'volumes_m3':dict(zip(CATEGORIAS,total))})
totals={cat:sum(r['volumes_m3'][cat] for r in res) for cat in CATEGORIAS}
out={'terraplanagem':res,'total_terraplanagem_m3':totals,'subleito':'Pendente: espessura, largura/limites e unidade de medicao nao definidos.'}
json.dump(out,open(os.path.join(ROOT,'medicao_0_300_e_350_400.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=2)
with open(os.path.join(ROOT,'medicao_terraplanagem_0_300_e_350_400.csv'),'w',encoding='utf-8-sig',newline='') as f:
    w=csv.writer(f,delimiter=';');w.writerow(['Trecho']+[cat+' (m3)' for cat in CATEGORIAS])
    for r in res:
        w.writerow([r['trecho']]+[format(r['volumes_m3'][cat],'.3f').replace('.',',') for cat in CATEGORIAS])
    w.writerow(['TOTAL']+[format(totals[cat],'.3f').replace('.',',') for cat in CATEGORIAS])
print(json.dumps(out,ensure_ascii=False,indent=2))
