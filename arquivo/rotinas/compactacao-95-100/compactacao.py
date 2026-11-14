"""Geometria em metros. Categorias de compactacao, sem fator volumetrico.
Entrada: segmentos [(offset0, cota0, offset1, cota1), ...].
100%: primeiro metro abaixo da envoltoria superior entre as superficies.
95%: excedente. Corte: natural acima do projeto; aterro: projeto acima.
"""
import math

CATEGORIAS = ('Corte 95%', 'Corte 100%', 'Aterro 95%', 'Aterro 100%')

def segmentos(pontos):
    result = []
    for i, p in enumerate(pontos):
        j = int(p['to'])
        if j < 0:
            continue
        if j >= len(pontos) or j == i:
            raise ValueError('Segmento de secao invalido')
        q = pontos[j]
        a, b = (float(p['x']), float(p['y'])), (float(q['x']), float(q['y']))
        if not all(math.isfinite(v) for v in a + b):
            raise ValueError('Coordenada nao finita')
        if b[0] < a[0]:
            a, b = b, a
        if b[0] - a[0] > 1e-9:
            result.append(a + b)
    result.sort()
    for a, b in zip(result, result[1:]):
        if a[2] > b[0] + 1e-7:
            raise ValueError('Secao com segmentos sobrepostos')
    return result

def cota(seg, x):
    return seg[1] + (seg[3]-seg[1])*(x-seg[0])/(seg[2]-seg[0])

def separar(natural, projeto, limite=1.0):
    if not math.isfinite(limite) or limite <= 0:
        raise ValueError('Limite deve ser positivo')
    areas = [0.0]*4
    poligonos = []
    largura = 0.0
    for n in natural:
        for p in projeto:
            a, b = max(n[0], p[0]), min(n[2], p[2])
            if b-a <= 1e-9:
                continue
            largura += b-a
            da, db = cota(p,a)-cota(n,a), cota(p,b)-cota(n,b)
            xs = [a,b]
            if abs(db-da)>1e-12:
                for h in (-limite,0.0,limite):
                    t=(h-da)/(db-da)
                    if 1e-10<t<1-1e-10:
                        xs.append(a+(b-a)*t)
            xs.sort()
            for x,y in zip(xs,xs[1:]):
                nn=[cota(n,x),cota(n,y)]
                pp=[cota(p,x),cota(p,y)]
                fill=sum(pp)>sum(nn)
                low=nn if fill else pp
                high=pp if fill else nn
                split=[max(low[k],high[k]-limite) for k in (0,1)]
                for pct,bottom,top in ((95,low,split),(100,split,high)):
                    area=(y-x)*sum(top[k]-bottom[k] for k in (0,1))/2
                    if area<=1e-10:
                        continue
                    idx=(2 if fill else 0)+(1 if pct==100 else 0)
                    areas[idx]+=area
                    poligonos.append({'categoria':idx,'pontos':[[x,bottom[0]],[y,bottom[1]],[y,top[1]],[x,top[0]]]})
    if largura<=1e-8:
        raise ValueError('Superficies sem sobreposicao na secao')
    return areas,poligonos

def volumes(registros):
    """Media das areas nas extremidades; nunca liga grupos diferentes ou falhas."""
    totals=[0.0]*4
    for prev,cur in zip(registros,registros[1:]):
        ds=cur['station']-prev['station']
        if ds<=0:
            raise ValueError('Progressivas devem ser estritamente crescentes')
        v=[(a+b)*ds/2 for a,b in zip(prev['areas'],cur['areas'])]
        cur['volumes_intervalo']=v
        totals=[a+b for a,b in zip(totals,v)]
    return totals
