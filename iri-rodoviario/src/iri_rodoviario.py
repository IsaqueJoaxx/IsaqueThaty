# -*- coding: utf-8 -*-
"""
IRI RODOVIÁRIO — conformação da pista existente + camada final de 23 cm
=========================================================================

Entrada
  1) Eixo em LandXML (exportado do Civil 3D), estacas em metros.
  2) Levantamento da pista em CSV: número;N;E;cota;código  (eixo e bordos, seções a cada 5 m).

O que o programa faz
  Etapa 1 — CONFORMAÇÃO (nota de baixo): calcula uma superfície regular sobre a pista
  existente, com 3,50 m para cada lado, usando o mínimo de material (fresagem + enchimento)
  e sem ondulação (IRI teórico abaixo do alvo). O caimento da conformação pode ficar um pouco
  diferente do final (ex.: 1,3% ou 1,6%) quando isso economiza material no total.
  Abertura do acostamento: a base do acostamento (3,50 a 5,50 m) sai do bordo conformado,
  paralela à superfície final.
  Etapa 2 — FINAL +0,23 (nota de cima): superfície final com 5,50 m para cada lado, 2% para
  cada lado (superelevação nas curvas pelo critério DNIT), com no mínimo 0,23 m de espessura
  sobre a conformação no eixo, a 3,50 m e a 5,50 m.

Saídas (pasta de resultado)
  - Excel com Resumo, Custos, Nota Conformação, Nota Final +23 e IRI por 100 m
  - Linhas 3D (DXF) da superfície final, da conformação e da pista existente, com as seções
  - Pontos PNEZD (CSV) da conformação e do final para o Civil 3D
  - LandXML com o eixo e os dois perfis (CONFORMACAO e FINAL)
  - Gráfico do perfil (PNG)

Uso
  python iri_rodoviario.py EIXO.xml LEVANTAMENTO.csv [--saida PASTA] [--ini 268635] [--fim 272055]
  Sem argumentos, o programa abre janelas para escolher os arquivos.

Requisitos: Python 3.9+, numpy, scipy (1.6+), openpyxl; matplotlib é opcional (só o gráfico)
  python -m pip install numpy scipy openpyxl matplotlib
"""
import argparse
import math
import os
import sys
import time
import traceback
import xml.etree.ElementTree as ET
from dataclasses import dataclass

# console do Windows: evita erro com acentos e símbolos (m³, →) quando a saída é redirecionada
for _fluxo in (sys.stdout, sys.stderr):
    try:
        _fluxo.reconfigure(encoding='utf-8', errors='replace')
    except Exception:
        pass


def _pausar_se_duplo_clique():
    """Quando o programa é aberto com duplo clique, segura a janela para dar tempo de ler."""
    if os.environ.get('IRI_BAT'):
        return  # o .bat já segura a janela
    aberto_pelo_windows = getattr(sys, 'frozen', False) or len(sys.argv) <= 1
    if aberto_pelo_windows and sys.stdin is not None and sys.stdin.isatty():
        try:
            input('\nPressione Enter para fechar...')
        except Exception:
            pass


try:
    import numpy as np
    import scipy
    from scipy import sparse
    from scipy.interpolate import CubicSpline
    from scipy.linalg import expm
    from scipy.optimize import linprog
    from scipy.spatial import cKDTree
    import openpyxl  # noqa: F401
except ImportError as _erro:
    print('Falta uma biblioteca do Python:', _erro)
    print('Instale com o comando abaixo (no Prompt de Comando) e rode de novo:')
    print('    python -m pip install numpy scipy openpyxl matplotlib')
    _pausar_se_duplo_clique()
    sys.exit(1)

if tuple(int(p) for p in scipy.__version__.split('.')[:2]) < (1, 6):
    print(f'O scipy instalado ({scipy.__version__}) é antigo; precisa da versão 1.6 ou mais nova.')
    print('Atualize com:    python -m pip install --upgrade scipy')
    _pausar_se_duplo_clique()
    sys.exit(1)

VERSAO = '2.3'


# ============================================================================ PARÂMETROS
@dataclass
class Parametros:
    # --- geometria da seção
    meia_pista: float = 3.50            # m, do eixo ao bordo da pista
    acostamento: float = 2.00           # m, além do bordo (total 5,50 por lado)
    caimento: float = 0.02              # caimento normal da superfície final (2%)
    espessura: float = 0.23             # m, espessura mínima no eixo, 3,50 e 5,50
    intervalo: float = 5.0              # m, espaçamento das estacas das notas
    # --- conformação
    desvio_caimento_menos: float = 0.010  # a conformação pode ficar até 1% mais deitada que o final
    desvio_caimento_mais: float = 0.010   # ... ou até 1% mais inclinada
    var_caimento_max: float = 0.0025      # variação máx. do caimento da conformação a cada 5 m
    fresagem_max: float = None            # m, profundidade máxima de fresagem (None = sem limite)
    # --- custos relativos por m³ (1 = só volume; troque pelo custo unitário real se quiser)
    custo_enchimento: float = 1.0
    custo_fresagem: float = 1.0
    custo_concreto: float = 1.0
    # --- preços unitários (R$/m³) para a aba Custos; 0 = não informado
    preco_enchimento: float = 0.0
    preco_fresagem: float = 0.0
    preco_concreto: float = 0.0
    # --- suavidade
    iri_alvo: float = 0.5               # m/km, IRI teórico máximo do greide em cada 100 m
    k_min: float = 20.0                 # m/%, K mínimo das curvas verticais
    # --- superelevação (DNIT)
    superelevar: bool = True
    velocidade: float = 80.0            # km/h
    e_max: float = 0.06
    f_max: float = 0.14
    r_dispensa: float = 3200.0          # m, raio acima do qual a superelevação é dispensada
    rampa_relativa: float = 0.005       # 1:200
    transicao_min: float = 45.0         # m
    # --- leitura do levantamento
    passo_y: float = 0.5                # m, discretização transversal da pista existente
    extrap_borda_max: float = 0.6       # m, quanto a seção pode ser estendida além do último ponto
    afastamento_max_ponto: float = 6.0  # m, pontos mais longe do eixo são ignorados


# arquivo de parâmetros ao lado do programa (.py ou .exe) — editável no Bloco de Notas
ARQ_PARAMETROS = 'IRI_PARAMETROS.txt'
_DESCRICOES = [
    ('Geometria da seção', None),
    ('meia_pista', 'm, do eixo ao bordo da pista'),
    ('acostamento', 'm, além do bordo (3,50 + 2,00 = 5,50 por lado)'),
    ('caimento', 'caimento normal da superfície final (0,02 = 2%)'),
    ('espessura', 'm, espessura mínima da camada final no eixo, a 3,50 e a 5,50'),
    ('intervalo', 'm, espaçamento das estacas nas notas'),
    ('Conformação', None),
    ('desvio_caimento_menos', 'quanto a conformação pode ficar mais deitada que o final (0,010 = 1%)'),
    ('desvio_caimento_mais', 'quanto a conformação pode ficar mais inclinada que o final'),
    ('var_caimento_max', 'variação máxima do caimento da conformação a cada estaca'),
    ('fresagem_max', 'm, profundidade máxima de fresagem (vazio = sem limite)'),
    ('Custos relativos por m³ (1 = só volume; pode colocar o custo unitário real)', None),
    ('custo_enchimento', 'material da conformação'),
    ('custo_fresagem', 'fresagem'),
    ('custo_concreto', 'camada final'),
    ('Preços unitários (R$/m³) — aba Custos. 0 = em branco. Preenchidos os três, a otimização usa os preços', None),
    ('preco_enchimento', 'R$/m³ do material da conformação (por tonelada: multiplique pela densidade)'),
    ('preco_fresagem', 'R$/m³ de fresagem'),
    ('preco_concreto', 'R$/m³ da camada final'),
    ('Suavidade', None),
    ('iri_alvo', 'm/km, IRI teórico máximo do greide em cada 100 m'),
    ('k_min', 'm/%, K mínimo das curvas verticais'),
    ('Superelevação (DNIT)', None),
    ('superelevar', 'sim / não'),
    ('velocidade', 'km/h'),
    ('e_max', 'superelevação máxima (0,06 = 6%)'),
    ('f_max', 'atrito transversal máximo'),
    ('r_dispensa', 'm, raio acima do qual não superelevar'),
    ('rampa_relativa', 'rampa de superelevação (0,005 = 1:200)'),
    ('transicao_min', 'm, comprimento mínimo de transição'),
    ('Leitura do levantamento', None),
    ('passo_y', 'm, discretização transversal da pista existente'),
    ('extrap_borda_max', 'm, quanto a seção pode ser estendida além do último ponto medido'),
    ('afastamento_max_ponto', 'm, pontos mais longe do eixo são ignorados'),
]


def pasta_programa():
    if getattr(sys, 'frozen', False):  # .exe
        return os.path.dirname(os.path.abspath(sys.executable))
    return os.path.dirname(os.path.abspath(__file__))


def _valor_txt(v):
    if v is None:
        return ''
    if isinstance(v, bool):
        return 'sim' if v else 'não'
    return f'{v:g}'.replace('.', ',')


def escrever_parametros(caminho, P=None):
    P = P or Parametros()
    linhas = [f'# IRI RODOVIÁRIO v{VERSAO} — parâmetros do cálculo',
              '# Mude o valor depois do "=" e rode o programa de novo. Vírgula ou ponto servem de decimal.',
              '# Para voltar ao padrão, apague este arquivo: ele é recriado na próxima vez.', '']
    for chave, desc in _DESCRICOES:
        if desc is None:
            linhas += ['', f'# --- {chave}']
        else:
            linhas.append(f'{chave:<24}= {_valor_txt(getattr(P, chave)):<10}# {desc}')
    with open(caminho, 'w', encoding='utf-8') as f:
        f.write('\n'.join(linhas) + '\n')


def ler_parametros(caminho, log=print):
    """Lê o arquivo de parâmetros; cria com os valores padrão se não existir."""
    P = Parametros()
    if not os.path.exists(caminho):
        try:
            escrever_parametros(caminho, P)
            log(f'Arquivo de parâmetros criado com os valores padrão: {caminho}')
        except OSError:
            pass
        return P
    tipos = {k: type(v) for k, v in vars(Parametros()).items()}
    with open(caminho, encoding='utf-8-sig', errors='replace') as f:
        for n, linha in enumerate(f, start=1):
            linha = linha.split('#', 1)[0].strip()
            if not linha or '=' not in linha:
                continue
            chave, valor = (t.strip() for t in linha.split('=', 1))
            if chave not in tipos:
                log(f'Aviso: parâmetro desconhecido na linha {n} de {ARQ_PARAMETROS}: "{chave}" (ignorado)')
                continue
            try:
                if chave == 'fresagem_max':
                    setattr(P, chave, None if valor.lower() in ('', 'vazio', 'nenhum', 'none') else _numero(valor))
                elif tipos[chave] is bool:
                    setattr(P, chave, valor.lower() in ('sim', 's', 'true', '1', 'yes'))
                else:
                    setattr(P, chave, _numero(valor))
            except ValueError:
                log(f'Aviso: valor inválido na linha {n} de {ARQ_PARAMETROS}: "{valor}" (mantido o padrão)')
    return P


# ============================================================================ EIXO
def _ne(txt):
    n, e = (float(v) for v in txt.split())
    return e, n  # (x=E, y=N)


class Eixo:
    """Eixo horizontal do LandXML. Afastamento + = lado direito (LD), − = esquerdo (LE)."""

    def __init__(self, caminho_xml, nome=None):
        self.arvore = ET.parse(caminho_xml)
        raiz = self.arvore.getroot()
        self.ns = raiz.tag[: raiz.tag.index('}') + 1] if raiz.tag.startswith('{') else ''
        ns = self.ns
        als = list(raiz.iter(ns + 'Alignment'))
        if not als:
            raise ValueError('Nenhum Alignment encontrado no XML.')
        al = next((a for a in als if a.get('name') == nome), als[0]) if nome else als[0]
        self.elemento_xml = al
        self.nome = al.get('name')
        self.est_inicial = float(al.get('staStart', 0.0))
        self.elementos = []
        s = self.est_inicial
        for el in al.find(ns + 'CoordGeom'):
            tag = el.tag.replace(ns, '')
            if tag == 'Line':
                p0 = np.array(_ne(el.find(ns + 'Start').text))
                p1 = np.array(_ne(el.find(ns + 'End').text))
                L = float(np.hypot(*(p1 - p0)))
                self.elementos.append(dict(tipo='reta', p0=p0, s0=s, L=L, u=(p1 - p0) / L))
                s += L
            elif tag == 'Curve':
                p0 = np.array(_ne(el.find(ns + 'Start').text))
                c = np.array(_ne(el.find(ns + 'Center').text))
                R = float(el.get('radius'))
                L = float(el.get('length'))
                sentido = 1.0 if el.get('rot') == 'ccw' else -1.0
                a0 = math.atan2(p0[1] - c[1], p0[0] - c[0])
                self.elementos.append(dict(tipo='curva', p0=p0, c=c, R=R, s0=s, L=L,
                                           sentido=sentido, a0=a0))
                s += L
            elif tag == 'Spiral':
                raise NotImplementedError('O eixo tem espiral (Spiral); esta versão lê só retas e curvas.')
        self.est_final = s
        amostras, idx = [], []
        for i, e in enumerate(self.elementos):
            for t in np.linspace(0, e['L'], max(2, int(e['L'] / 2.0) + 1)):
                amostras.append(self._ponto_elem(e, t, 0.0))
                idx.append(i)
        self._arvore = cKDTree(np.array(amostras))
        self._idx = np.array(idx)
        self._s0 = np.array([e['s0'] for e in self.elementos])

    @staticmethod
    def _ponto_elem(e, t, d):
        if e['tipo'] == 'reta':
            u = e['u']
            return e['p0'] + u * t + np.array([u[1], -u[0]]) * d
        ang = e['a0'] + e['sentido'] * t / e['R']
        r = e['R'] + e['sentido'] * d
        return e['c'] + r * np.array([math.cos(ang), math.sin(ang)])

    @staticmethod
    def _proj_elem(e, p):
        if e['tipo'] == 'reta':
            v = p - e['p0']
            t = float(v @ e['u'])
            d = float(-(e['u'][0] * v[1] - e['u'][1] * v[0]))
        else:
            v = p - e['c']
            r = float(np.hypot(*v))
            dang = (math.atan2(v[1], v[0]) - e['a0']) * e['sentido']
            dang = (dang + math.pi) % (2 * math.pi) - math.pi
            t = dang * e['R']
            d = e['sentido'] * (r - e['R'])
        return t, d, max(0.0, -t, t - e['L'])

    def estaca_afastamento(self, xy):
        xy = np.atleast_2d(xy)
        _, viz = self._arvore.query(xy, k=6)
        est, afa = np.empty(len(xy)), np.empty(len(xy))
        for k, p in enumerate(xy):
            cands = sorted(set(c2 for c in set(self._idx[viz[k]]) for c2 in (c - 1, c, c + 1)
                               if 0 <= c2 < len(self.elementos)))
            melhor = None
            for c in cands:
                e = self.elementos[c]
                t, d, fora = self._proj_elem(e, p)
                chave = (round(fora, 6), abs(d))
                if melhor is None or chave < melhor[0]:
                    melhor = (chave, e['s0'] + min(max(t, 0.0), e['L']), d)
            est[k], afa[k] = melhor[1], melhor[2]
        return est, afa

    def ponto(self, estaca, afastamento=0.0):
        estaca = np.atleast_1d(np.asarray(estaca, float))
        afastamento = np.broadcast_to(np.asarray(afastamento, float), estaca.shape)
        out = np.empty((len(estaca), 2))
        for k, (s, d) in enumerate(zip(estaca, afastamento)):
            i = int(np.clip(np.searchsorted(self._s0, s, side='right') - 1, 0, len(self._s0) - 1))
            e = self.elementos[i]
            out[k] = self._ponto_elem(e, s - e['s0'], d)
        return out


def _br(v, casas):
    return f'{v:.{casas}f}'.replace('.', ',')


def formata_estaca(m):
    km = int(math.floor(m / 1000.0 + 1e-9))
    resto = m - km * 1000.0
    if abs(resto - round(resto)) < 0.005:
        return f"{km}+{int(round(resto)):03d}"
    return f"{km}+{resto:06.2f}".replace('.', ',')


# ============================================================================ LEVANTAMENTO
def _numero(txt):
    """Aceita '9.607.235.497' (milhar com ponto), '9607235,497' e '9607235.497'."""
    t = txt.strip().replace(' ', '')
    if ',' in t:
        return float(t.replace('.', '').replace(',', '.'))
    partes = t.split('.')
    if len(partes) > 2:
        return float(''.join(partes[:-1]) + '.' + partes[-1])
    return float(t)


def ler_levantamento(caminho):
    num, N, E, Z, cod = [], [], [], [], []
    with open(caminho, encoding='utf-8-sig', errors='replace') as f:
        for linha in f:
            linha = linha.strip()
            if not linha:
                continue
            sep = ';' if ';' in linha else ('\t' if '\t' in linha else ',')
            c = [v.strip() for v in linha.split(sep)]
            if len(c) < 4:
                continue
            try:
                n, e, z = _numero(c[1]), _numero(c[2]), _numero(c[3])
            except ValueError:
                continue
            num.append(c[0]); N.append(n); E.append(e); Z.append(z)
            cod.append(c[4] if len(c) > 4 else '')
    if not num:
        raise ValueError('Nenhum ponto lido do levantamento. Formato esperado: número;N;E;cota;código')
    return dict(num=np.array(num), N=np.array(N), E=np.array(E), Z=np.array(Z), cod=np.array(cod))


class Existente:
    """Pista existente montada pelas seções levantadas (interpolação linear entre pontos e seções)."""

    def __init__(self, estaca, afast, cota, folga_secao=1.0, extrap_max=0.6, buraco_max=12.0):
        o = np.argsort(estaca)
        est, afa, z = estaca[o], afast[o], cota[o]
        self.extrap_max, self.buraco_max = extrap_max, buraco_max
        self.secoes, self.n_descartadas = [], 0
        for g in np.split(np.arange(len(est)), np.where(np.diff(est) > folga_secao)[0] + 1):
            a = afa[g]
            if len(g) < 3 or a.min() > -1.0 or a.max() < 1.0 or np.abs(a).min() > 1.5:
                self.n_descartadas += 1
                continue
            oo = np.argsort(a)
            self.secoes.append(dict(est=float(est[g].mean()), a=a[oo], z=z[g][oo]))
        if len(self.secoes) < 4:
            raise ValueError('Poucas seções completas no levantamento (precisa de eixo e dos dois lados).')
        self.es = np.array([s['est'] for s in self.secoes])

    @staticmethod
    def _perfil(sec, y):
        a, z = sec['a'], sec['z']
        out = np.interp(y, a, z)
        m = y < a[0]
        if m.any():
            j = np.where(a >= a[0] + 0.8)[0]
            j = j[0] if len(j) else len(a) - 1
            out[m] = z[0] + (z[j] - z[0]) / (a[j] - a[0]) * (y[m] - a[0])
        m = y > a[-1]
        if m.any():
            j = np.where(a <= a[-1] - 0.8)[0]
            j = j[-1] if len(j) else 0
            out[m] = z[-1] + (z[j] - z[-1]) / (a[j] - a[-1]) * (y[m] - a[-1])
        return out

    def grade(self, estacas, y):
        estacas, y = np.asarray(estacas, float), np.asarray(y, float)
        M = np.array([self._perfil(s, y) for s in self.secoes])
        cobre = np.array([[(s['a'][0] - self.extrap_max) <= yy <= (s['a'][-1] + self.extrap_max)
                           for yy in y] for s in self.secoes])
        out = np.full((len(estacas), len(y)), np.nan)
        for k in range(len(y)):
            ok = cobre[:, k]
            es, zk = self.es[ok], M[ok, k]
            if len(es) < 2:
                continue
            j = np.searchsorted(es, estacas)
            j0, j1 = np.clip(j - 1, 0, len(es) - 1), np.clip(j, 0, len(es) - 1)
            vale = ((estacas >= es[0] - 0.5) & (estacas <= es[-1] + 0.5)
                    & ((es[j1] - es[j0]) <= self.buraco_max))
            out[vale, k] = np.interp(estacas[vale], es, zk)
        return out


# ============================================================================ IRI
def _matrizes_iri(dx, v=80 / 3.6):
    k1, k2, c, mu = 653.0, 63.3, 6.0, 0.15
    A = np.array([[0, 1, 0, 0], [-k2, -c, k2, c], [0, 0, 0, 1],
                  [k2 / mu, c / mu, -(k1 + k2) / mu, -c / mu]], float)
    B = np.array([0, 0, 0, k1 / mu], float)
    st = expm(A * dx / v)
    return st, np.linalg.solve(A, (st - np.eye(4)) @ B)


_IRI_CACHE = {}


def iri(perfil, dx=0.25, trecho=100.0):
    """IRI (quarto-de-carro padrão, 80 km/h). Retorna (IRI total, IRI por trecho) em m/km."""
    y = np.asarray(perfil, float)
    n11 = int(round(11 / dx))
    if len(y) < n11 + 2:
        return float('nan'), np.array([])
    if dx not in _IRI_CACHE:
        _IRI_CACHE[dx] = _matrizes_iri(dx)
    st, pr = _IRI_CACHE[dx]
    z = np.array([(y[n11] - y[0]) / 11.0, 0.0, (y[n11] - y[0]) / 11.0, 0.0])
    incl = np.diff(y) / dx
    rs = np.empty(len(incl))
    for i, g in enumerate(incl):
        z = st @ z + pr * g
        rs[i] = abs(z[0] - z[2])
    n = int(round(trecho / dx))
    por = np.array([rs[k:k + n].mean() * 1000.0 for k in range(0, len(rs), n) if len(rs[k:k + n]) > n // 2])
    return rs.mean() * 1000.0, por


TRILHAS = (-2.65, -0.85, 0.85, 2.65)  # trilhas de roda (faixa de 3,50 m, rodas a 0,90 m do centro)


def iri_superficie(estacas, eixo_cota, cL, cR, trilhas=TRILHAS):
    """IRI das trilhas de roda de uma superfície definida pelo eixo e caimentos por lado."""
    s = np.arange(estacas[0], estacas[-1], 0.25)
    cs = CubicSpline(estacas, eixo_cota)
    res = {}
    for yy in trilhas:
        z = cs(s) - np.interp(s, estacas, cL if yy < 0 else cR) * abs(yy)
        res[yy] = iri(z)
    return s, res


# ============================================================================ SUPERELEVAÇÃO
def taxa_superelevacao(R, P):
    rmin = P.velocidade ** 2 / (127.0 * (P.e_max + P.f_max))
    if R >= P.r_dispensa:
        return None
    if R <= rmin:
        return P.e_max
    return max(P.e_max * (2 * rmin / R - (rmin / R) ** 2), P.caimento)


def curvas_horizontais(eixo, P):
    grupos, atual = [], None
    for e in eixo.elementos:
        if e['tipo'] == 'curva':
            if atual is not None and atual['sentido'] == e['sentido'] and abs(atual['fim'] - e['s0']) < 0.01:
                atual['elems'].append(e); atual['fim'] = e['s0'] + e['L']
            else:
                atual = dict(sentido=e['sentido'], ini=e['s0'], fim=e['s0'] + e['L'], elems=[e])
                grupos.append(atual)
        else:
            atual = None
    curvas = []
    for g in grupos:
        R = min(e['R'] for e in g['elems'])
        e_sup = taxa_superelevacao(R, P)
        if e_sup is None:
            continue
        circ = [x for x in g['elems'] if x['R'] <= R * 1.02]
        ts, sc = g['ini'], circ[0]['s0']
        cs, st = circ[-1]['s0'] + circ[-1]['L'], g['fim']
        le_min = max(P.transicao_min, e_sup * P.meia_pista / P.rampa_relativa)
        if sc - ts < 1.0:   # curva sem transição: 60% na tangente, 40% na curva
            ts, sc = sc - 0.6 * le_min, sc + 0.4 * le_min
        if st - cs < 1.0:
            cs, st = cs - 0.4 * le_min, cs + 0.6 * le_min
        curvas.append(dict(R=R, e=e_sup, ts=ts, sc=sc, cs=cs, st=st,
                           lado_interno='LD' if g['sentido'] < 0 else 'LE'))
    return curvas


def caimentos_finais(estacas, eixo, P):
    """Caimento final por lado (+ = cai para fora do eixo; − = sobe para fora)."""
    qL = np.full(len(estacas), P.caimento)
    qR = np.full(len(estacas), P.caimento)
    curvas = curvas_horizontais(eixo, P) if P.superelevar else []
    c0 = P.caimento
    for cv in curvas:
        e = cv['e']
        # giro em torno do eixo: lado externo vai de +c0 a −e com taxa constante
        L_in = max((cv['sc'] - cv['ts']) * (c0 + e) / e, (c0 + e) * P.meia_pista / P.rampa_relativa)
        L_out = max((cv['st'] - cv['cs']) * (c0 + e) / e, (c0 + e) * P.meia_pista / P.rampa_relativa)
        a, b = cv['sc'] - L_in, cv['cs'] + L_out
        cv['inicio_giro'], cv['fim_giro'] = a, b
        for i, s in enumerate(estacas):
            if s <= a or s >= b:
                continue
            if s < cv['sc']:
                ext = c0 - (c0 + e) * (s - a) / L_in
            elif s <= cv['cs']:
                ext = -e
            else:
                ext = -e + (c0 + e) * (s - cv['cs']) / L_out
            inn = max(c0, -ext)
            if cv['lado_interno'] == 'LD':
                qL[i], qR[i] = ext, inn
            else:
                qL[i], qR[i] = inn, ext
    return qL, qR, curvas


# ============================================================================ OTIMIZAÇÃO
def _dif(n, ordem):
    coef = {1: [-1, 1], 2: [1, -2, 1], 3: [-1, 3, -3, 1]}[ordem]
    m = n - ordem
    rows = np.repeat(np.arange(m), len(coef))
    cols = (np.arange(m)[:, None] + np.arange(len(coef))[None, :]).ravel()
    return sparse.csr_matrix((np.tile(coef, m), (rows, cols)), shape=(m, n))


def resolver(X, y, qL, qR, P, mu):
    """Programação linear.

    Variáveis por estaca: c (cota da conformação no eixo), f (cota final no eixo),
    aL, aR (caimentos da conformação). Minimiza
        custo_ench·enchimento + custo_fres·fresagem + custo_conc·excesso de concreto
        + mu·Σ|Δ³c| + mu·Σ|Δ³f| + 10·mu·Σ|Δ²a|
    com espessura ≥ 0,23 no eixo e nos bordos e K mínimo nas curvas verticais.
    """
    n, K = X.shape
    h = np.full(n, P.intervalo); h[0] = h[-1] = P.intervalo / 2
    dy = np.diff(y)
    W = np.zeros(K); W[:-1] += dy / 2; W[1:] += dy / 2
    valid = np.argwhere(np.isfinite(X))
    nv = len(valid)
    ii, kk = valid[:, 0], valid[:, 1]
    iC, iF, iL, iR = 0, n, 2 * n, 3 * n
    iP = 4 * n; iM = iP + nv
    nt, n2 = n - 3, n - 2
    iTc = iM + nv; iTf = iTc + nt; iVL = iTf + nt; iVR = iVL + n2
    N = iVR + n2
    rest = N - 4 * n

    cst = np.zeros(N)
    area = h[ii] * W[kk]
    # só a proporção entre os custos importa: normaliza para a suavização ter a mesma escala
    esc = max(P.custo_enchimento, P.custo_fresagem, P.custo_concreto, 1e-12)
    k_ench, k_fres, k_conc = P.custo_enchimento / esc, P.custo_fresagem / esc, P.custo_concreto / esc
    cst[iP:iP + nv] = k_ench * area
    cst[iM:iM + nv] = k_fres * area
    larg = P.meia_pista + P.acostamento
    coef_a = P.meia_pista ** 2 / 2 + P.acostamento * P.meia_pista
    cst[iF:iF + n] += k_conc * h * 2 * larg
    cst[iC:iC + n] -= k_conc * h * 2 * larg
    cst[iL:iL + n] += k_conc * h * coef_a
    cst[iR:iR + n] += k_conc * h * coef_a
    cst[iTc:iTc + nt] = mu
    cst[iTf:iTf + nt] = mu
    cst[iVL:iVL + n2] = 10 * mu
    cst[iVR:iVR + n2] = 10 * mu

    r = np.arange(nv)
    absy = np.abs(y[kk])
    m0 = absy > 0
    lado = np.where(y[kk] < 0, iL, iR)
    Aeq = sparse.csr_matrix((np.concatenate([np.ones(nv), -absy[m0], -np.ones(nv), np.ones(nv)]),
                             (np.concatenate([r, r[m0], r, r]),
                              np.concatenate([iC + ii, lado[m0] + ii[m0], iP + r, iM + r]))),
                            shape=(nv, N))
    beq = X[ii, kk]

    I = sparse.identity(n, format='csr')
    Z = lambda m, k: sparse.csr_matrix((m, k))

    def linha(Mc, Mf, ML, MR, extra=None):
        m = Mc.shape[0]
        return sparse.hstack([Mc, Mf, ML, MR, extra if extra is not None else Z(m, rest)], format='csr')

    def sel(start, m):
        return sparse.csr_matrix((np.ones(m), (np.arange(m), start - 4 * n + np.arange(m))), shape=(m, rest))

    blocos, b = [], []

    def add(Mt, bb):
        blocos.append(Mt); b.append(np.asarray(bb, float))

    e0, w = P.espessura, P.meia_pista
    add(linha(I, -I, Z(n, n), Z(n, n)), np.full(n, -e0))
    add(linha(I, -I, -w * I, Z(n, n)), -e0 - w * qL)
    add(linha(I, -I, Z(n, n), -w * I), -e0 - w * qR)
    D3, D2, D1 = _dif(n, 3), _dif(n, 2), _dif(n, 1)
    kap = P.intervalo ** 2 / (100.0 * P.k_min)
    for s_ in (1, -1):
        add(linha(s_ * D3, Z(nt, n), Z(nt, n), Z(nt, n), -sel(iTc, nt)), np.zeros(nt))
        add(linha(Z(nt, n), s_ * D3, Z(nt, n), Z(nt, n), -sel(iTf, nt)), np.zeros(nt))
        add(linha(Z(n2, n), Z(n2, n), s_ * D2, Z(n2, n), -sel(iVL, n2)), np.zeros(n2))
        add(linha(Z(n2, n), Z(n2, n), Z(n2, n), s_ * D2, -sel(iVR, n2)), np.zeros(n2))
        add(linha(s_ * D2, Z(n2, n), Z(n2, n), Z(n2, n)), np.full(n2, kap))
        add(linha(Z(n2, n), s_ * D2, Z(n2, n), Z(n2, n)), np.full(n2, kap))
        add(linha(Z(n - 1, n), Z(n - 1, n), s_ * D1, Z(n - 1, n)), np.full(n - 1, P.var_caimento_max))
        add(linha(Z(n - 1, n), Z(n - 1, n), Z(n - 1, n), s_ * D1), np.full(n - 1, P.var_caimento_max))
    Aub = sparse.vstack(blocos, format='csr')
    bub = np.concatenate(b)

    lb = np.full(N, -np.inf); ub = np.full(N, np.inf)
    lb[iL:iL + n] = qL - P.desvio_caimento_menos; ub[iL:iL + n] = qL + P.desvio_caimento_mais
    lb[iR:iR + n] = qR - P.desvio_caimento_menos; ub[iR:iR + n] = qR + P.desvio_caimento_mais
    lb[iP:] = 0.0
    if P.fresagem_max is not None:
        ub[iM:iM + nv] = P.fresagem_max
    res = linprog(cst, A_ub=Aub, b_ub=bub, A_eq=Aeq, b_eq=beq, bounds=np.c_[lb, ub], method='highs')
    if res.status != 0:
        raise RuntimeError('A otimização não encontrou solução: ' + str(res.message))
    x = res.x
    return dict(c=x[iC:iC + n], f=x[iF:iF + n], aL=x[iL:iL + n], aR=x[iR:iR + n])


def avaliar(est, X, y, qL, qR, sol, P):
    c, f, aL, aR = sol['c'], sol['f'], sol['aL'], sol['aR']
    w, ac = P.meia_pista, P.acostamento
    dev = c[:, None] - np.where(y < 0, aL[:, None], aR[:, None]) * np.abs(y)[None, :] - X
    dy = np.diff(y); W = np.zeros(len(y)); W[:-1] += dy / 2; W[1:] += dy / 2
    h = np.full(len(est), P.intervalo); h[0] = h[-1] = P.intervalo / 2
    ench = np.nansum(np.clip(dev, 0, None) * W, 1) * h
    fres = np.nansum(np.clip(-dev, 0, None) * W, 1) * h
    e0 = f - c
    eL = (f - qL * w) - (c - aL * w)
    eR = (f - qR * w) - (c - aR * w)
    conc = ((e0 + eL) / 2 * w + eL * ac + (e0 + eR) / 2 * w + eR * ac) * h
    _, ir = iri_superficie(est, f, qL, qR)
    iri100 = np.max([v[1] for v in ir.values()], axis=0)
    return dict(dev=dev, ench=ench, fres=fres, e0=e0, eL=eL, eR=eR, conc=conc,
                iri_max100=float(iri100.max()), iri_trilhas=ir)


def otimizar(est, X, y, qL, qR, P, log=print):
    """Escolhe a menor suavização que deixa o IRI teórico ≤ alvo em todos os trechos de 100 m."""
    cache = {}

    def roda(mu):
        if mu not in cache:
            t = time.time()
            sol = resolver(X, y, qL, qR, P, mu)
            av = avaliar(est, X, y, qL, qR, sol, P)
            cache[mu] = (sol, av)
            log(f"   suavização {mu:9.3f}: IRI máx/100 m = {av['iri_max100']:.2f} m/km, "
                f"enchimento {av['ench'].sum():.1f} m³, fresagem {av['fres'].sum():.1f} m³ ({time.time() - t:.1f} s)")
        return cache[mu]

    lo, hi = math.log10(0.01), math.log10(1000.0)
    sol, av = roda(10 ** lo)
    if av['iri_max100'] <= P.iri_alvo:
        return sol, av, 10 ** lo
    sol, av = roda(10 ** hi)
    if av['iri_max100'] > P.iri_alvo:
        log('   atenção: nem a suavização máxima atingiu o IRI alvo; usando a mais suave.')
        return sol, av, 10 ** hi
    for _ in range(9):
        mid = (lo + hi) / 2
        _, a2 = roda(10 ** mid)
        if a2['iri_max100'] <= P.iri_alvo:
            hi = mid
        else:
            lo = mid
    sol, av = roda(10 ** hi)
    return sol, av, 10 ** hi


# ============================================================================ SAÍDAS
def gravar_pontos(caminho, eixo, est, cotas_por_afast, prefixo):
    """PNEZD: número,N,E,cota,descrição."""
    n = 1
    with open(caminho, 'w', encoding='utf-8', newline='') as f:
        for i, s in enumerate(est):
            for d, nome, cotas in cotas_por_afast:
                e_, n_ = eixo.ponto(s, d)[0]
                f.write(f"{n},{n_:.3f},{e_:.3f},{cotas[i]:.3f},{prefixo}_{nome}_{formata_estaca(s)}\n")
                n += 1


def gravar_landxml(caminho, eixo, est, perfis):
    ns = eixo.ns.strip('{}')
    ET.register_namespace('', ns)
    raiz = ET.Element(f'{{{ns}}}LandXML', dict(version='1.2', date=time.strftime('%Y-%m-%d'),
                                              time=time.strftime('%H:%M:%S')))
    orig = eixo.arvore.getroot()
    for tag in ('Units', 'CoordinateSystem'):
        el = orig.find(eixo.ns + tag)
        if el is not None:
            raiz.append(el)
    als = ET.SubElement(raiz, f'{{{ns}}}Alignments', name='')
    al = ET.fromstring(ET.tostring(eixo.elemento_xml))
    for p in al.findall(f'{{{ns}}}Profile'):
        al.remove(p)
    prof = ET.SubElement(al, f'{{{ns}}}Profile', name=eixo.nome)
    for nome, cotas in perfis:
        pa = ET.SubElement(prof, f'{{{ns}}}ProfAlign', name=nome)
        for s, z in zip(est, cotas):
            ET.SubElement(pa, f'{{{ns}}}PVI').text = f"{s:.3f} {z:.4f}"
    als.append(al)
    ET.ElementTree(raiz).write(caminho, encoding='utf-8', xml_declaration=True)


CAMADAS_DXF = [  # nome, cor ACI, descrição
    ('IRI_FINAL', 1, 'superfície final +0,23: eixo, 3,50 e 5,50 (LE e LD)'),
    ('IRI_FINAL_SECOES', 30, 'seções da superfície final em cada estaca'),
    ('IRI_CONFORMACAO', 5, 'conformação: eixo, 3,50 e base do acostamento a 5,50'),
    ('IRI_CONF_SECOES', 140, 'seções da conformação em cada estaca'),
    ('IRI_EXISTENTE', 8, 'pista existente (levantamento) no eixo e a 3,50'),
    ('IRI_ESTACAS', 7, 'texto das estacas (a cada 20 m)'),
]


def gravar_dxf(caminho, eixo, est, X, y, R, P, passo_texto=20.0):
    """Linhas 3D (POLYLINE 3D, DXF R12) para conferência no Civil 3D/AutoCAD."""
    w, ac = P.meia_pista, P.acostamento
    c, f, aL, aR, qL, qR = R['c'], R['f'], R['aL'], R['aR'], R['qL'], R['qR']
    lados = [('LE550', -(w + ac)), ('LE350', -w), ('EIXO', 0.0), ('LD350', w), ('LD550', w + ac)]
    z_fin = {'LE550': f - qL * (w + ac), 'LE350': f - qL * w, 'EIXO': f,
             'LD350': f - qR * w, 'LD550': f - qR * (w + ac)}
    z_con = {'LE550': c - aL * w - qL * ac, 'LE350': c - aL * w, 'EIXO': c,
             'LD350': c - aR * w, 'LD550': c - aR * w - qR * ac}
    xy = {nome: eixo.ponto(est, d) for nome, d in lados}
    linhas = []

    def g(cod, val):
        linhas.append(f'{cod:>3}')
        linhas.append(str(val))

    def num(v):
        return f'{v:.4f}'

    def poli(camada, pts):
        if len(pts) < 2:
            return
        g(0, 'POLYLINE'); g(8, camada); g(66, 1); g(10, '0.0'); g(20, '0.0'); g(30, '0.0'); g(70, 8)
        for x_, y_, z_ in pts:
            g(0, 'VERTEX'); g(8, camada); g(10, num(x_)); g(20, num(y_)); g(30, num(z_)); g(70, 32)
        g(0, 'SEQEND'); g(8, camada)

    todos = np.vstack(list(xy.values()))
    zs = np.concatenate([z_fin['EIXO'], z_con['EIXO']])
    g(0, 'SECTION'); g(2, 'HEADER')
    g(9, '$ACADVER'); g(1, 'AC1009')
    g(9, '$EXTMIN'); g(10, num(todos[:, 0].min())); g(20, num(todos[:, 1].min())); g(30, num(np.nanmin(zs) - 1))
    g(9, '$EXTMAX'); g(10, num(todos[:, 0].max())); g(20, num(todos[:, 1].max())); g(30, num(np.nanmax(zs) + 1))
    g(0, 'ENDSEC')
    g(0, 'SECTION'); g(2, 'TABLES')
    g(0, 'TABLE'); g(2, 'LTYPE'); g(70, 1)
    g(0, 'LTYPE'); g(2, 'CONTINUOUS'); g(70, 0); g(3, 'Solid line'); g(72, 65); g(73, 0); g(40, '0.0')
    g(0, 'ENDTAB')
    g(0, 'TABLE'); g(2, 'LAYER'); g(70, len(CAMADAS_DXF))
    for nome, cor, _ in CAMADAS_DXF:
        g(0, 'LAYER'); g(2, nome); g(70, 0); g(62, cor); g(6, 'CONTINUOUS')
    g(0, 'ENDTAB')
    g(0, 'ENDSEC')
    g(0, 'SECTION'); g(2, 'ENTITIES')
    for nome, _ in lados:
        poli('IRI_FINAL', [(p[0], p[1], z) for p, z in zip(xy[nome], z_fin[nome])])
        poli('IRI_CONFORMACAO', [(p[0], p[1], z) for p, z in zip(xy[nome], z_con[nome])])
    for i in range(len(est)):
        poli('IRI_FINAL_SECOES', [(xy[n_][i, 0], xy[n_][i, 1], z_fin[n_][i]) for n_, _ in lados])
        poli('IRI_CONF_SECOES', [(xy[n_][i, 0], xy[n_][i, 1], z_con[n_][i]) for n_, _ in lados])
    # pista existente: quebra a linha onde não há levantamento
    for nome, d in lados[1:4]:
        j = int(np.argmin(np.abs(y - d)))
        ze = X[:, j]
        trecho = []
        for i in range(len(est)):
            if np.isfinite(ze[i]):
                trecho.append((xy[nome][i, 0], xy[nome][i, 1], ze[i]))
            else:
                poli('IRI_EXISTENTE', trecho); trecho = []
        poli('IRI_EXISTENTE', trecho)
    # estacas
    for i, s in enumerate(est):
        if abs(s / passo_texto - round(s / passo_texto)) > 1e-6:
            continue
        p0, p1 = eixo.ponto([s - 0.5, s + 0.5], 0.0)
        ang = math.degrees(math.atan2(p1[1] - p0[1], p1[0] - p0[0])) - 90.0  # ao longo da seção, lendo para o LD
        pt = eixo.ponto(s, w + ac + 0.8)[0]
        g(0, 'TEXT'); g(8, 'IRI_ESTACAS'); g(10, num(pt[0])); g(20, num(pt[1])); g(30, num(z_fin['LD550'][i]))
        g(40, '0.50'); g(1, formata_estaca(s)); g(50, f'{ang:.4f}')
    g(0, 'ENDSEC'); g(0, 'EOF')
    with open(caminho, 'w', encoding='ascii', errors='replace', newline='\r\n') as fh:
        fh.write('\n'.join(linhas) + '\n')


def gravar_grafico(caminho, est, X, y, R, P):
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    j0 = int(np.argmin(np.abs(y)))
    fig, ax = plt.subplots(3, 1, figsize=(14, 9), sharex=True,
                           gridspec_kw=dict(height_ratios=[2, 1.3, 1]))
    km = est / 1000.0
    ax[0].plot(km, X[:, j0], color='#888888', lw=0.8, label='Pista existente (eixo)')
    ax[0].plot(km, R['c'], color='#1f6fb2', lw=1.2, label='Conformação (eixo)')
    ax[0].plot(km, R['f'], color='#c0392b', lw=1.2, label='Final +0,23 (eixo)')
    ax[0].set_ylabel('Cota (m)'); ax[0].legend(loc='best', fontsize=9); ax[0].grid(alpha=0.3)
    ax[0].set_title('Perfil longitudinal no eixo')
    for k, (yy, nome, cor) in enumerate([(-P.meia_pista, 'LE 3,50', '#2e8b57'), (0.0, 'Eixo', '#1f6fb2'),
                                          (P.meia_pista, 'LD 3,50', '#d35400')]):
        jj = int(np.argmin(np.abs(y - yy)))
        ax[1].plot(km, R['dev'][:, jj] * 100, lw=0.7, color=cor, label=nome)
    ax[1].axhline(0, color='k', lw=0.6)
    ax[1].set_ylabel('cm  (+ ench / − fres)'); ax[1].legend(loc='best', fontsize=9, ncol=3); ax[1].grid(alpha=0.3)
    ax[1].set_title('Conformação − pista existente')
    ax[2].plot(km, R['qL'] * 100, color='#2e8b57', lw=1, label='Final LE')
    ax[2].plot(km, R['qR'] * 100, color='#d35400', lw=1, label='Final LD')
    ax[2].plot(km, R['aL'] * 100, color='#2e8b57', lw=0.6, ls='--', label='Conformação LE')
    ax[2].plot(km, R['aR'] * 100, color='#d35400', lw=0.6, ls='--', label='Conformação LD')
    ax[2].set_ylabel('Caimento (%)'); ax[2].set_xlabel('km'); ax[2].grid(alpha=0.3)
    ax[2].legend(loc='best', fontsize=8, ncol=4)
    fig.tight_layout()
    fig.savefig(caminho, dpi=110)
    plt.close(fig)


def gravar_excel(caminho, est, X, y, R, P, info, grafico=None):
    from openpyxl import Workbook
    from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
    from openpyxl.utils import get_column_letter
    from openpyxl.formatting.rule import CellIsRule

    FONTE = 'Arial'
    f_base = Font(name=FONTE, size=10)
    f_neg = Font(name=FONTE, size=10, bold=True)
    f_tit = Font(name=FONTE, size=14, bold=True)
    f_inp = Font(name=FONTE, size=10, color='0000FF')
    f_lnk = Font(name=FONTE, size=10, color='008000')
    f_cab = Font(name=FONTE, size=10, bold=True, color='FFFFFF')
    p_cab = PatternFill('solid', fgColor='1F4E78')
    p_sub = PatternFill('solid', fgColor='D9E1F2')
    p_cinza = PatternFill('solid', fgColor='F2F2F2')
    p_verm = PatternFill('solid', fgColor='F8CBAD')
    fino = Side(style='thin', color='BFBFBF')
    borda = Border(left=fino, right=fino, top=fino, bottom=fino)
    centro = Alignment(horizontal='center', vertical='center', wrap_text=True)
    F3, F4, PCT, VOL = '0.000', '0.0000', '0.00%', '#,##0.00'

    wb = Workbook()
    rs = wb.active
    rs.title = 'Resumo'
    cu = wb.create_sheet('Custos')
    n1 = wb.create_sheet('Nota Conformação')
    n2 = wb.create_sheet('Nota Final +23')
    ti = wb.create_sheet('IRI por 100 m')
    N1, N2 = "'Nota Conformação'", "'Nota Final +23'"

    # ---------------------------------------------------------------- Resumo: parâmetros
    rs['B2'] = 'IRI RODOVIÁRIO — Conformação + camada final'; rs['B2'].font = f_tit
    rs['B3'] = (f"Eixo: {info['arq_eixo']}  |  Levantamento: {info['arq_lev']}  |  "
                f"{formata_estaca(est[0])} a {formata_estaca(est[-1])}  |  versão {VERSAO}  |  {time.strftime('%d/%m/%Y %H:%M')}")
    rs['B3'].font = Font(name=FONTE, size=9, italic=True)
    rs['B5'] = 'Parâmetros (azul = valor usado no cálculo)'; rs['B5'].font = f_neg
    params = [
        ('Meia pista (m)', P.meia_pista, '0.00'),
        ('Acostamento (m)', P.acostamento, '0.00'),
        ('Caimento normal final', P.caimento, PCT),
        ('Espessura mínima da camada final (m)', P.espessura, '0.000'),
        ('Intervalo das notas (m)', P.intervalo, '0.0'),
        ('Conformação: até quanto mais deitada que o final', P.desvio_caimento_menos, PCT),
        ('Conformação: até quanto mais inclinada que o final', P.desvio_caimento_mais, PCT),
        ('Fresagem máxima (m) — vazio = sem limite', P.fresagem_max, '0.000'),
        ('Custo relativo enchimento (por m³)', P.custo_enchimento, '0.00'),
        ('Custo relativo fresagem (por m³)', P.custo_fresagem, '0.00'),
        ('Custo relativo concreto (por m³)', P.custo_concreto, '0.00'),
        ('IRI teórico máximo em cada 100 m (m/km)', P.iri_alvo, '0.00'),
        ('K mínimo das curvas verticais (m/%)', P.k_min, '0.0'),
        ('Velocidade de projeto (km/h)', P.velocidade, '0'),
        ('Superelevação máxima', P.e_max, PCT),
    ]
    ref = {}
    for k, (nome, val, fmt) in enumerate(params):
        r = 6 + k
        rs.cell(r, 2, nome).font = f_base
        c = rs.cell(r, 3, val)
        c.font = f_inp; c.number_format = fmt; c.alignment = Alignment(horizontal='right')
        ref[nome] = f'Resumo!$C${r}'
    REF_W = ref['Meia pista (m)']
    REF_AC = ref['Acostamento (m)']
    REF_E = ref['Espessura mínima da camada final (m)']
    r = 6 + len(params) + 1
    rs.cell(r, 2, 'Os parâmetros acima são os que o programa usou; para recalcular com outros valores, '
                  f'mude-os no arquivo {ARQ_PARAMETROS} (ao lado do programa) e rode de novo.').font = Font(name=FONTE, size=9, italic=True)

    # ---------------------------------------------------------------- Nota Conformação
    n = len(est)
    L0 = 3
    h = np.full(n, P.intervalo); h[0] = h[-1] = P.intervalo / 2
    jL = int(np.argmin(np.abs(y + P.meia_pista))); j0 = int(np.argmin(np.abs(y))); jR = int(np.argmin(np.abs(y - P.meia_pista)))
    c, f, aL, aR, qL, qR = R['c'], R['f'], R['aL'], R['aR'], R['qL'], R['qR']
    w, ac = P.meia_pista, P.acostamento
    conf = dict(LE55=c - aL * w - qL * ac, LE35=c - aL * w, EIXO=c, LD35=c - aR * w, LD55=c - aR * w - qR * ac)
    fin = dict(LE55=f - qL * (w + ac), LE35=f - qL * w, EIXO=f, LD35=f - qR * w, LD55=f - qR * (w + ac))

    grupos1 = [('ESTACA', 1, 3), ('LE 5,50', 4, 4), ('LE 3,50', 5, 7), ('EIXO', 8, 10), ('LD 3,50', 11, 13),
               ('LD 5,50', 14, 14), ('CAIMENTO CONF.', 15, 16), ('VOLUMES (m³)', 17, 18)]
    sub1 = ['Estaca', 'Estaca (m)', 'Compr. (m)', 'Projeto', 'Existente', 'Projeto', 'Proj.−Exist.', 'Existente',
            'Projeto', 'Proj.−Exist.', 'Existente', 'Projeto', 'Proj.−Exist.', 'Projeto', 'LE', 'LD', 'Enchimento', 'Fresagem']
    for nome, a, b2 in grupos1:
        n1.merge_cells(start_row=1, start_column=a, end_row=1, end_column=b2)
        cc = n1.cell(1, a, nome); cc.font = f_cab; cc.fill = p_cab; cc.alignment = centro
        for col in range(a, b2 + 1):
            n1.cell(1, col).fill = p_cab; n1.cell(1, col).border = borda
    for k, t in enumerate(sub1, start=1):
        cc = n1.cell(2, k, t); cc.font = f_neg; cc.fill = p_sub; cc.alignment = centro; cc.border = borda
    for i in range(n):
        r = L0 + i
        vals = {1: formata_estaca(est[i]), 2: float(est[i]), 3: float(h[i]), 4: float(conf['LE55'][i]),
                6: float(conf['LE35'][i]), 9: float(conf['EIXO'][i]), 12: float(conf['LD35'][i]),
                14: float(conf['LD55'][i]), 17: float(R['ench'][i]), 18: float(R['fres'][i])}
        for col, jj in ((5, jL), (8, j0), (11, jR)):
            if np.isfinite(X[i, jj]):
                vals[col] = float(X[i, jj])
        for col, v in vals.items():
            n1.cell(r, col, v)
        n1.cell(r, 7, f'=IF(E{r}="","",F{r}-E{r})')
        n1.cell(r, 10, f'=IF(H{r}="","",I{r}-H{r})')
        n1.cell(r, 13, f'=IF(K{r}="","",L{r}-K{r})')
        n1.cell(r, 15, f'=(I{r}-F{r})/{REF_W}')
        n1.cell(r, 16, f'=(I{r}-L{r})/{REF_W}')
        for col in range(1, 19):
            cc = n1.cell(r, col)
            cc.font = f_base; cc.border = borda
            cc.number_format = {2: '0.00', 3: '0.0', 15: PCT, 16: PCT, 17: VOL, 18: VOL}.get(col, F3)
            if col in (5, 8, 11):
                cc.fill = p_cinza
    last1 = L0 + n - 1
    tot = last1 + 1
    n1.cell(tot, 1, 'TOTAL').font = f_neg
    for col, L_ in ((17, 'Q'), (18, 'R')):
        cc = n1.cell(tot, col, f'=SUM({L_}{L0}:{L_}{last1})'); cc.font = f_neg; cc.number_format = VOL; cc.border = borda
    larg1 = {1: 10, 2: 11, 3: 8}
    for col in range(1, 19):
        n1.column_dimensions[get_column_letter(col)].width = larg1.get(col, 12 if col >= 17 else 10.5)
    n1.freeze_panes = 'D3'

    # ---------------------------------------------------------------- Nota Final +23
    grupos2 = [('ESTACA', 1, 3), ('LE 5,50', 4, 6), ('LE 3,50', 7, 9), ('EIXO', 10, 12), ('LD 3,50', 13, 15),
               ('LD 5,50', 16, 18), ('CAIMENTO FINAL', 19, 20), ('CONCRETO', 21, 21)]
    sub2 = ['Estaca', 'Estaca (m)', 'Compr. (m)'] + ['Conform.', 'Final', 'Espessura'] * 5 + ['LE', 'LD', 'Volume (m³)']
    for nome, a, b2 in grupos2:
        n2.merge_cells(start_row=1, start_column=a, end_row=1, end_column=b2)
        cc = n2.cell(1, a, nome); cc.font = f_cab; cc.fill = p_cab; cc.alignment = centro
        for col in range(a, b2 + 1):
            n2.cell(1, col).fill = p_cab; n2.cell(1, col).border = borda
    for k, t in enumerate(sub2, start=1):
        cc = n2.cell(2, k, t); cc.font = f_neg; cc.fill = p_sub; cc.alignment = centro; cc.border = borda
    liga = {4: 'D', 7: 'F', 10: 'I', 13: 'L', 16: 'N'}
    finais = {5: fin['LE55'], 8: fin['LE35'], 11: fin['EIXO'], 14: fin['LD35'], 17: fin['LD55']}
    for i in range(n):
        r = L0 + i
        n2.cell(r, 1, f'={N1}!A{r}'); n2.cell(r, 2, f'={N1}!B{r}'); n2.cell(r, 3, f'={N1}!C{r}')
        for col, L_ in liga.items():
            n2.cell(r, col, f'={N1}!{L_}{r}')
            n2.cell(r, col + 1, float(finais[col + 1][i]))
            a1, a2 = get_column_letter(col + 1), get_column_letter(col)
            n2.cell(r, col + 2, f'={a1}{r}-{a2}{r}')
        n2.cell(r, 19, f'=(K{r}-H{r})/{REF_W}')
        n2.cell(r, 20, f'=(K{r}-N{r})/{REF_W}')
        n2.cell(r, 21, f'=((L{r}+I{r})/2*{REF_W}+(I{r}+F{r})/2*{REF_AC}'
                       f'+(L{r}+O{r})/2*{REF_W}+(O{r}+R{r})/2*{REF_AC})*C{r}')
        for col in range(1, 22):
            cc = n2.cell(r, col)
            cc.font = f_lnk if col in (1, 2, 3, 4, 7, 10, 13, 16) else f_base
            cc.border = borda
            cc.number_format = {2: '0.00', 3: '0.0', 19: PCT, 20: PCT, 21: VOL}.get(col, F3)
            if col in (6, 9, 12, 15, 18):
                cc.number_format = F4; cc.fill = p_cinza
    last2 = L0 + n - 1
    n2.cell(tot, 1, 'TOTAL').font = f_neg
    cc = n2.cell(tot, 21, f'=SUM(U{L0}:U{last2})'); cc.font = f_neg; cc.number_format = VOL; cc.border = borda
    n2.cell(tot + 1, 1, 'MÍNIMO').font = f_neg
    for col in (6, 9, 12, 15, 18):
        L_ = get_column_letter(col)
        cc = n2.cell(tot + 1, col, f'=MIN({L_}{L0}:{L_}{last2})'); cc.font = f_neg; cc.number_format = F4; cc.border = borda
    n2.conditional_formatting.add(f'F{L0}:R{last2}',
                                  CellIsRule(operator='lessThan', formula=[f'{REF_E}-0.00005'], fill=p_verm))
    for col in range(1, 22):
        n2.column_dimensions[get_column_letter(col)].width = {1: 10, 2: 11, 3: 8}.get(col, 10)
    n2.freeze_panes = 'D3'

    # ---------------------------------------------------------------- IRI por 100 m
    cab = ['Início', 'Fim', 'IRI existente (estimado)', 'IRI projeto — trilha mais rugosa', 'IRI projeto — média das trilhas']
    for k, t in enumerate(cab, start=1):
        cc = ti.cell(1, k, t); cc.font = f_cab; cc.fill = p_cab; cc.alignment = centro; cc.border = borda
    ie = np.mean([v[1] for v in info['iri_exist'].values()], axis=0)
    ip_max = np.max([v[1] for v in R['iri_trilhas'].values()], axis=0)
    ip_med = np.mean([v[1] for v in R['iri_trilhas'].values()], axis=0)
    m_ = min(len(ie), len(ip_max))
    for k in range(m_):
        r = 2 + k
        a = est[0] + 100 * k
        b2 = min(a + 100, est[-1])
        for col, v in enumerate([formata_estaca(a), formata_estaca(b2), ie[k], ip_max[k], ip_med[k]], start=1):
            cc = ti.cell(r, col, v if isinstance(v, str) else float(v)); cc.font = f_base; cc.border = borda
            if col > 2:
                cc.number_format = '0.00'
    rr = 2 + m_
    ti.cell(rr, 1, 'MÉDIA').font = f_neg
    ti.cell(rr + 1, 1, 'MÁXIMO').font = f_neg
    for col in (3, 4, 5):
        L_ = get_column_letter(col)
        cc = ti.cell(rr, col, f'=AVERAGE({L_}2:{L_}{rr - 1})'); cc.font = f_neg; cc.number_format = '0.00'
        cc = ti.cell(rr + 1, col, f'=MAX({L_}2:{L_}{rr - 1})'); cc.font = f_neg; cc.number_format = '0.00'
    ti.cell(rr + 3, 1, 'IRI em m/km pelo quarto-de-carro padrão (80 km/h), nas trilhas de roda a 0,85 e 2,65 m do eixo. '
                       'O existente é estimado ligando as seções a cada 5 m em linha reta; o IRI medido com perfilômetro tende a ser maior. '
                       'O projeto é o IRI teórico do greide contínuo (sem erro de execução).').font = Font(name=FONTE, size=9, italic=True)
    for col, wdt in enumerate([11, 11, 22, 28, 28], start=1):
        ti.column_dimensions[get_column_letter(col)].width = wdt
    ti.freeze_panes = 'A2'

    # ---------------------------------------------------------------- Custos
    RS = '"R$" #,##0.00'
    p_amar = PatternFill('solid', fgColor='FFFF00')
    cu['B2'] = 'Custo do novo greide × greide original'; cu['B2'].font = f_tit
    cu['B3'] = ('Greide original: camada de 0,23 m colocada direto sobre a pista existente (sem conformação). '
                'Novo greide: conformação da pista + camada de 0,23 m.')
    cu['B4'] = ('A abertura do acostamento (de 3,50 a 5,50 m) é igual nos dois casos e não entra na comparação. '
                'Volumes vêm das abas Nota Conformação e Nota Final +23.')
    for cel in ('B3', 'B4'):
        cu[cel].font = Font(name=FONTE, size=9, italic=True)
    cu['B6'] = 'Preços unitários — preencha as células amarelas'; cu['B6'].font = f_neg
    precos = [('Enchimento da conformação (R$/m³)', P.preco_enchimento),
              ('Fresagem (R$/m³)', P.preco_fresagem),
              ('Concreto da camada final (R$/m³)', P.preco_concreto)]
    for k, (nome, val) in enumerate(precos):
        rr_ = 7 + k
        cu.cell(rr_, 2, nome).font = f_base
        cc = cu.cell(rr_, 3, float(val) if val else None)
        cc.font = f_inp; cc.fill = p_amar; cc.number_format = RS; cc.border = borda
    cu['B10'] = ('Se o preço do material for por tonelada (ex.: CBUQ), multiplique pela densidade '
                 '(t/m³) antes de lançar. Ex.: R$ 500/t × 2,40 t/m³ = R$ 1.200/m³.')
    cu['B10'].font = Font(name=FONTE, size=9, italic=True)
    cab = ['Item', 'Unid.', 'Greide original', 'Novo greide', 'Diferença', 'Preço unit.',
           'Custo original', 'Custo novo', 'Aumento']
    for k, t in enumerate(cab):
        cc = cu.cell(12, 2 + k, t); cc.font = f_cab; cc.fill = p_cab; cc.alignment = centro; cc.border = borda
    EXT = f'({N1}!B{last1}-{N1}!B{L0})'
    itens = [('Enchimento da conformação', 0, f'={N1}!Q{tot}', '$C$7'),
             ('Fresagem', 0, f'={N1}!R{tot}', '$C$8'),
             ('Concreto da camada final', f'=2*({REF_W}+{REF_AC})*{REF_E}*{EXT}', f'={N2}!U{tot}', '$C$9')]
    for k, (nome, orig, novo, preco) in enumerate(itens):
        r_ = 13 + k
        vals = [nome, 'm³', orig, novo, f'=E{r_}-D{r_}', f'={preco}', f'=D{r_}*G{r_}', f'=E{r_}*G{r_}', f'=I{r_}-H{r_}']
        for j, v in enumerate(vals):
            cc = cu.cell(r_, 2 + j, v); cc.border = borda
            cc.font = f_lnk if j in (2, 3) and isinstance(v, str) else f_base
            cc.number_format = VOL if j in (2, 3, 4) else (RS if j >= 5 else 'General')
    cu.cell(16, 2, 'TOTAL').font = f_neg
    for j, L_ in ((7, 'H'), (8, 'I'), (9, 'J')):
        cc = cu.cell(16, 1 + j, f'=SUM({L_}13:{L_}15)'); cc.font = f_neg; cc.number_format = RS; cc.border = borda
    ind = [('Extensão (m)', f'={EXT}', '#,##0.00'),
           ('Aumento total do novo greide', '=J16', RS),
           ('Aumento em relação ao greide original', '=IF(H16=0,"",J16/H16)', PCT),
           ('Aumento por km', '=IF(C18=0,"",J16/(C18/1000))', RS),
           ('Conformação por km (enchimento + fresagem, m³/km)', '=IF(C18=0,"",(E13+E14)/(C18/1000))', VOL)]
    for k, (nome, fml, fmt) in enumerate(ind):
        r_ = 18 + k
        cu.cell(r_, 2, nome).font = f_neg if k == 1 else f_base
        cc = cu.cell(r_, 3, fml); cc.number_format = fmt; cc.font = f_neg if k == 1 else f_base
    cu['B24'] = 'Por trecho de 100 m (para conferência)'; cu['B24'].font = f_neg
    cab = ['De', 'Até', 'De (m)', 'Até (m)', 'Compr. (m)', 'Enchimento (m³)', 'Fresagem (m³)',
           'Concreto novo (m³)', 'Concreto original (m³)', 'Aumento']
    for k, t in enumerate(cab):
        cc = cu.cell(25, 2 + k, t); cc.font = f_cab; cc.fill = p_cab; cc.alignment = centro; cc.border = borda
    faixas = []
    ini_ = math.floor(est[0] / 100.0) * 100.0
    while ini_ <= est[-1] + 1e-6:
        fim_ = ini_ + 100.0
        if fim_ >= est[-1]:
            faixas.append((ini_, est[-1] + 0.001))
            break
        faixas.append((ini_, fim_))
        ini_ = fim_
    B1r, C1r, Q1r, R1r = (f'{N1}!${L_}${L0}:${L_}${last1}' for L_ in 'BCQR')
    B2r, U2r = (f'{N2}!${L_}${L0}:${L_}${last2}' for L_ in 'BU')
    for k, (a_, b_) in enumerate(faixas):
        r_ = 26 + k
        crit1 = f'{B1r},">="&D{r_},{B1r},"<"&E{r_}'
        crit2 = f'{B2r},">="&D{r_},{B2r},"<"&E{r_}'
        vals = [formata_estaca(max(a_, est[0])), formata_estaca(min(b_, est[-1])), float(a_), float(b_),
                f'=SUMIFS({C1r},{crit1})', f'=SUMIFS({Q1r},{crit1})', f'=SUMIFS({R1r},{crit1})',
                f'=SUMIFS({U2r},{crit2})', f'=2*({REF_W}+{REF_AC})*{REF_E}*F{r_}',
                f'=G{r_}*$C$7+H{r_}*$C$8+(I{r_}-J{r_})*$C$9']
        for j, v in enumerate(vals):
            cc = cu.cell(r_, 2 + j, v); cc.font = f_base; cc.border = borda
            cc.number_format = {2: '0.000', 3: '0.000', 4: '0.0', 9: RS}.get(j, VOL if j >= 5 else 'General')
    rt = 26 + len(faixas)
    cu.cell(rt, 2, 'TOTAL').font = f_neg
    for j in range(4, 10):
        L_ = get_column_letter(2 + j)
        cc = cu.cell(rt, 2 + j, f'=SUM({L_}26:{L_}{rt - 1})'); cc.font = f_neg; cc.border = borda
        cc.number_format = RS if j == 9 else ('0.0' if j == 4 else VOL)
    cu.cell(rt + 1, 2, 'Conferência: o total por trecho deve bater com o TOTAL de cima.').font = Font(name=FONTE, size=9, italic=True)
    for col, wdt in zip('ABCDEFGHIJK', [2, 46, 16, 15, 15, 14, 15, 16, 16, 18, 18]):
        cu.column_dimensions[col].width = wdt
    cu.column_dimensions['B'].width = 46

    # ---------------------------------------------------------------- Resumo: resultados
    r = 6 + len(params) + 3
    rs.cell(r, 2, 'Resultados').font = f_neg
    res_lin = [
        ('Extensão (m)', f'={N1}!B{last1}-{N1}!B{L0}', '#,##0.00'),
        ('Estacas na nota', f'=COUNT({N1}!B{L0}:B{last1})', '0'),
        ('Enchimento da conformação (m³)', f'={N1}!Q{tot}', VOL),
        ('Fresagem da conformação (m³)', f'={N1}!R{tot}', VOL),
        ('Concreto da camada final (m³)', f'={N2}!U{tot}', VOL),
        ('Concreto se a espessura fosse exatamente a mínima (m³)', f'=2*({REF_W}+{REF_AC})*{REF_E}*C{r + 1}', VOL),
        ('Excesso de concreto (m³)', f'=C{r + 5}-C{r + 6}', VOL),
        ('Espessura mínima — eixo (m)', f'={N2}!L{tot + 1}', F4),
        ('Espessura mínima — LE 3,50 / LE 5,50 (m)', f'=MIN({N2}!I{tot + 1},{N2}!F{tot + 1})', F4),
        ('Espessura mínima — LD 3,50 / LD 5,50 (m)', f'=MIN({N2}!O{tot + 1},{N2}!R{tot + 1})', F4),
        ('Fresagem máxima na seção (m)', float(np.nanmax(-R['dev'])), F3),
        ('Enchimento máximo na seção (m)', float(np.nanmax(R['dev'])), F3),
        ('IRI estimado da pista existente — média (m/km)', f"='IRI por 100 m'!C{rr}", '0.00'),
        ('IRI teórico do projeto — máximo em 100 m (m/km)', f"='IRI por 100 m'!D{rr + 1}", '0.00'),
        ('Aumento de custo do novo greide (preencha os preços na aba Custos)', '=Custos!C19', '"R$" #,##0.00'),
        ('Estacas com caimento da conformação diferente do final (> 0,1%)', int(np.sum(
            (np.abs(R['aL'] - R['qL']) > 0.001) | (np.abs(R['aR'] - R['qR']) > 0.001))), '0'),
    ]
    for k, (nome, val, fmt) in enumerate(res_lin, start=1):
        rs.cell(r + k, 2, nome).font = f_base
        cc = rs.cell(r + k, 3, val); cc.font = f_base; cc.number_format = fmt; cc.alignment = Alignment(horizontal='right')
    r = r + len(res_lin) + 2
    rs.cell(r, 2, 'Superelevação adotada (DNIT)').font = f_neg
    cab = ['Raio (m)', 'Taxa', 'Lado interno', 'Início do giro', 'TS', 'SC', 'CS', 'ST', 'Fim do giro']
    for k, t in enumerate(cab):
        cc = rs.cell(r + 1, 2 + k, t); cc.font = f_neg; cc.fill = p_sub; cc.border = borda; cc.alignment = centro
    if not info['curvas']:
        rs.cell(r + 2, 2, 'Nenhuma curva com raio menor que o de dispensa neste trecho.').font = f_base
    for k, cv in enumerate(info['curvas']):
        vals = [cv['R'], cv['e'], cv['lado_interno'], formata_estaca(cv['inicio_giro']), formata_estaca(cv['ts']),
                formata_estaca(cv['sc']), formata_estaca(cv['cs']), formata_estaca(cv['st']), formata_estaca(cv['fim_giro'])]
        for j, v in enumerate(vals):
            cc = rs.cell(r + 2 + k, 2 + j, v); cc.font = f_base; cc.border = borda
            cc.number_format = {0: '0.0', 1: PCT}.get(j, 'General')
    r = r + 3 + max(1, len(info['curvas']))
    rs.cell(r, 2, 'Avisos').font = f_neg
    for k, a in enumerate(info['avisos'] or ['Nenhum.']):
        rs.cell(r + 1 + k, 2, a).font = f_base
    r = r + 2 + max(1, len(info['avisos']))
    rs.cell(r, 2, 'Como ler').font = f_neg
    notas = [
        'Nota Conformação: cota a deixar na pista antes da camada final (nota de baixo). Dif. = projeto − existente '
        '(+ enchimento / − fresagem). A 5,50 m é a cota da base do acostamento aberto, paralela à superfície final.',
        'Nota Final +23: cota da superfície final (nota de cima) e espessura sobre a conformação em cada ponto. '
        'Célula vermelha = espessura abaixo da mínima.',
        'A cota existente a 3,50 m vem da extensão da seção levantada (bordos medidos a cerca de 3,10–3,30 m).',
        'Volumes calculados com a seção existente discretizada a cada 0,50 m e estacas a cada 5 m (média das áreas).',
    ]
    for k, t in enumerate(notas):
        rs.cell(r + 1 + k, 2, t).font = Font(name=FONTE, size=9)
    rs.column_dimensions['A'].width = 2
    rs.column_dimensions['B'].width = 62
    for col in 'CDEFGHIJ':
        rs.column_dimensions[col].width = 13
    for ws in (rs, cu, n1, n2, ti):
        ws.page_setup.orientation = 'landscape'
        ws.page_setup.paperSize = ws.PAPERSIZE_A4
        ws.page_setup.fitToWidth = 1
        ws.page_setup.fitToHeight = 0
        ws.sheet_properties.pageSetUpPr.fitToPage = True
        ws.page_margins.left = ws.page_margins.right = 0.4
    for ws in (n1, n2):
        ws.print_title_rows = '1:2'
    ti.print_title_rows = '1:1'
    if grafico and os.path.exists(grafico):
        try:
            from openpyxl.drawing.image import Image as XLImage
            img = XLImage(grafico)
            img.width, img.height = 1000, 643
            rs.add_image(img, 'L2')
        except Exception:
            pass
    wb.save(caminho)


# ============================================================================ PRINCIPAL
def processar(arq_eixo, arq_lev, saida=None, est_ini=None, est_fim=None, P=None, log=print):
    P = P or Parametros()
    t0 = time.time()
    log(f'IRI RODOVIÁRIO v{VERSAO}')
    eixo = Eixo(arq_eixo)
    log(f'Eixo "{eixo.nome}": {formata_estaca(eixo.est_inicial)} a {formata_estaca(eixo.est_final)}, {len(eixo.elementos)} elementos')
    lev = ler_levantamento(arq_lev)
    est_p, afa_p = eixo.estaca_afastamento(np.c_[lev['E'], lev['N']])
    perto = np.abs(afa_p) <= P.afastamento_max_ponto
    avisos = []
    if (~perto).sum():
        avisos.append(f'{(~perto).sum()} pontos a mais de {_br(P.afastamento_max_ponto, 1)} m do eixo foram ignorados.')
    ex = Existente(est_p[perto], afa_p[perto], lev['Z'][perto], extrap_max=P.extrap_borda_max)
    log(f'Levantamento: {len(lev["Z"])} pontos, {len(ex.secoes)} seções completas '
        f'({formata_estaca(ex.es[0])} a {formata_estaca(ex.es[-1])})')
    if ex.n_descartadas:
        avisos.append(f'{ex.n_descartadas} seção(ões) incompleta(s) (sem eixo ou sem um dos lados) não entraram no cálculo.')
    passo = P.intervalo
    ini = est_ini if est_ini is not None else math.ceil((ex.es[0] - 0.5) / passo) * passo
    fim = est_fim if est_fim is not None else math.floor((ex.es[-1] + 0.5) / passo) * passo
    est = np.arange(ini, fim + 1e-6, passo)
    y = np.round(np.arange(-P.meia_pista, P.meia_pista + 1e-9, P.passo_y), 6)
    X = ex.grade(est, y)
    sem = ~np.isfinite(X[:, int(np.argmin(np.abs(y)))])
    if sem.any():
        avisos.append(f'{sem.sum()} estaca(s) sem levantamento no eixo (a nota mostra só o projeto nelas).')
    gaps = np.diff(ex.es)
    for k in np.where(gaps > 7.5)[0]:
        avisos.append(f'Seções a {_br(gaps[k], 1)} m de distância entre {formata_estaca(ex.es[k])} e {formata_estaca(ex.es[k + 1])}.')
    qL, qR, curvas = caimentos_finais(est, eixo, P)
    curvas = [cv for cv in curvas if cv['fim_giro'] > est[0] and cv['inicio_giro'] < est[-1]]
    log(f'Estacas da nota: {len(est)} ({formata_estaca(est[0])} a {formata_estaca(est[-1])}), '
        f'{len(curvas)} curva(s) com superelevação')
    for cv in curvas:
        log(f'   R = {cv["R"]:.1f} m → {cv["e"] * 100:.2f}% para o {cv["lado_interno"]} '
            f'({formata_estaca(cv["inicio_giro"])} a {formata_estaca(cv["fim_giro"])})')

    # IRI estimado da pista existente (seções a cada 5 m)
    ex_tr = ex.grade(est, np.array(TRILHAS))
    iri_exist = {}
    s_ = np.arange(est[0], est[-1], 0.25)
    for k, yy in enumerate(TRILHAS):
        v = ex_tr[:, k]; ok = np.isfinite(v)
        iri_exist[yy] = iri(np.interp(s_, est[ok], v[ok]))
    precos = (P.preco_enchimento, P.preco_fresagem, P.preco_concreto)
    if all(p_ > 0 for p_ in precos):
        P.custo_enchimento, P.custo_fresagem, P.custo_concreto = precos
        log('Preços unitários informados: a otimização minimiza o custo em R$.')
    log('Otimizando a conformação:')
    sol, av, mu = otimizar(est, X, y, qL, qR, P, log=log)
    R = dict(sol); R.update(av); R['qL'], R['qR'] = qL, qR

    if av['iri_max100'] > P.iri_alvo + 0.01:
        avisos.append(f'O IRI teórico máximo ficou em {_br(av["iri_max100"], 2)} m/km, acima do alvo de '
                      f'{_br(P.iri_alvo, 2)} m/km.')
    esp_min = min(av['e0'].min(), av['eL'].min(), av['eR'].min())
    if esp_min < P.espessura - 1e-4:
        avisos.append(f'ATENÇÃO: espessura mínima de {_br(esp_min, 4)} m, abaixo de {_br(P.espessura, 3)} m.')
    fres_max = float(np.nanmax(-av['dev']))
    if fres_max > 0.05:
        i, j = np.unravel_index(np.nanargmax(-av['dev']), av['dev'].shape)
        avisos.append(f'Fresagem máxima de {_br(fres_max * 100, 1)} cm em {formata_estaca(est[i])}, '
                      f'{_br(abs(y[j]), 2)} m {"LE" if y[j] < 0 else ("LD" if y[j] > 0 else "no eixo")}. '
                      f'Se houver limite de fresagem, preencha fresagem_max no {ARQ_PARAMETROS}.')

    # saídas
    base = os.path.splitext(os.path.basename(arq_lev))[0].replace(' ', '_')
    saida = saida or os.path.join(os.path.dirname(os.path.abspath(arq_lev)), f'RESULTADO_{base}')
    os.makedirs(saida, exist_ok=True)
    nome = f'{formata_estaca(est[0])}_a_{formata_estaca(est[-1])}'.replace('+', '-')
    arq_png = os.path.join(saida, f'PERFIL_{nome}.png')
    arq_xlsx = os.path.join(saida, f'IRI_NOTAS_{nome}.xlsx')
    arq_pc = os.path.join(saida, f'PONTOS_CONFORMACAO_{nome}.csv')
    arq_pf = os.path.join(saida, f'PONTOS_FINAL_{nome}.csv')
    arq_xml = os.path.join(saida, f'PERFIS_{nome}.xml')
    arq_dxf = os.path.join(saida, f'LINHAS_3D_{nome}.dxf')
    w, ac = P.meia_pista, P.acostamento
    c, f, aL, aR = R['c'], R['f'], R['aL'], R['aR']
    conf = [(-(w + ac), 'LE550', c - aL * w - qL * ac), (-w, 'LE350', c - aL * w), (0.0, 'EIXO', c),
            (w, 'LD350', c - aR * w), (w + ac, 'LD550', c - aR * w - qR * ac)]
    fin = [(-(w + ac), 'LE550', f - qL * (w + ac)), (-w, 'LE350', f - qL * w), (0.0, 'EIXO', f),
           (w, 'LD350', f - qR * w), (w + ac, 'LD550', f - qR * (w + ac))]
    gravar_pontos(arq_pc, eixo, est, conf, 'CONF')
    gravar_pontos(arq_pf, eixo, est, fin, 'FINAL')
    gravar_landxml(arq_xml, eixo, est, [('CONFORMACAO', c), ('FINAL', f)])
    gravar_dxf(arq_dxf, eixo, est, X, y, R, P)
    try:
        gravar_grafico(arq_png, est, X, y, R, P)
    except ImportError:
        arq_png = None
        avisos.append('Gráfico do perfil não gerado: falta o matplotlib '
                      '(python -m pip install matplotlib). As notas saem normalmente.')
    except Exception as erro:
        arq_png = None
        avisos.append(f'Gráfico do perfil não gerado ({erro}). As notas saem normalmente.')
    info = dict(arq_eixo=os.path.basename(arq_eixo), arq_lev=os.path.basename(arq_lev), curvas=curvas,
                avisos=avisos, iri_exist=iri_exist, mu=mu)
    try:
        gravar_excel(arq_xlsx, est, X, y, R, P, info, grafico=arq_png)
    except PermissionError:
        # a planilha anterior está aberta no Excel: grava com outro nome
        arq_xlsx = arq_xlsx.replace('.xlsx', time.strftime('_%H%M%S.xlsx'))
        gravar_excel(arq_xlsx, est, X, y, R, P, info, grafico=arq_png)
        avisos.append('A planilha anterior estava aberta no Excel; a nova foi salva como '
                      + os.path.basename(arq_xlsx))

    log('')
    log(f'Enchimento: {av["ench"].sum():.1f} m³ | Fresagem: {av["fres"].sum():.1f} m³ | Concreto: {av["conc"].sum():.1f} m³')
    log(f'Espessura mínima: {esp_min:.4f} m | IRI teórico máx/100 m: {av["iri_max100"]:.2f} m/km '
        f'(existente estimado: {np.mean([v[0] for v in iri_exist.values()]):.2f} m/km)')
    for a in avisos:
        log('Aviso: ' + a)
    log(f'Planilha: {arq_xlsx}')
    log(f'Arquivos em: {saida}  ({time.time() - t0:.0f} s)')
    return dict(est=est, X=X, y=y, R=R, P=P, info=info, saida=saida,
                arquivos=[a for a in (arq_xlsx, arq_dxf, arq_pc, arq_pf, arq_xml, arq_png) if a],
                eixo=eixo, existente=ex)


def _escolher_arquivos():
    try:
        import tkinter as tk
        from tkinter import filedialog
    except ImportError:
        return None, None
    tk.Tk().withdraw()
    xml = filedialog.askopenfilename(title='Escolha o eixo (LandXML)', filetypes=[('LandXML', '*.xml'), ('Todos', '*.*')])
    if not xml:
        return None, None
    csv = filedialog.askopenfilename(title='Escolha o levantamento da pista (CSV)',
                                     filetypes=[('CSV/TXT', '*.csv *.txt'), ('Todos', '*.*')])
    return xml, csv


def main():
    ap = argparse.ArgumentParser(description='IRI RODOVIÁRIO — conformação + camada final de 23 cm')
    ap.add_argument('eixo', nargs='?', help='eixo em LandXML')
    ap.add_argument('levantamento', nargs='?', help='levantamento da pista (CSV número;N;E;cota;código)')
    ap.add_argument('--saida', help='pasta de resultado')
    ap.add_argument('--ini', type=float, help='estaca inicial em metros (ex.: 268635)')
    ap.add_argument('--fim', type=float, help='estaca final em metros (ex.: 272055)')
    ap.add_argument('--iri', type=float, help='IRI teórico máximo em cada 100 m (m/km)')
    ap.add_argument('--fresagem-max', type=float, help='profundidade máxima de fresagem (m)')
    a = ap.parse_args()
    xml, csv = a.eixo, a.levantamento
    # arquivos arrastados em cima do .exe chegam em qualquer ordem: separa pelo tipo
    if xml and csv and xml.lower().endswith(('.csv', '.txt')) and csv.lower().endswith('.xml'):
        xml, csv = csv, xml
    if not xml or not csv:
        xml, csv = _escolher_arquivos()
        if not xml or not csv:
            ap.print_help()
            _pausar_se_duplo_clique()
            sys.exit(1)
    P = ler_parametros(os.path.join(pasta_programa(), ARQ_PARAMETROS))
    if a.iri is not None:
        P.iri_alvo = a.iri
    if a.fresagem_max is not None:
        P.fresagem_max = a.fresagem_max
    try:
        processar(xml, csv, a.saida, a.ini, a.fim, P)
    except Exception:
        detalhe = traceback.format_exc()
        print('\nERRO — a planilha não foi gerada.\n')
        print(detalhe)
        log_erro = os.path.join(os.path.dirname(os.path.abspath(csv)), 'ERRO_IRI_RODOVIARIO.txt')
        try:
            with open(log_erro, 'w', encoding='utf-8') as f:
                f.write(f'IRI RODOVIÁRIO v{VERSAO} — {time.strftime("%d/%m/%Y %H:%M")}\n'
                        f'Eixo: {xml}\nLevantamento: {csv}\nPython {sys.version}\n\n{detalhe}')
            print('Detalhes salvos em:', log_erro)
        except Exception:
            pass
        _pausar_se_duplo_clique()
        sys.exit(1)
    _pausar_se_duplo_clique()


if __name__ == '__main__':
    main()
