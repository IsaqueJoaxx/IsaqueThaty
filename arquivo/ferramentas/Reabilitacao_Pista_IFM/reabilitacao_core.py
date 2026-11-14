"""REABILITAÇÃO DE PISTA — IFM: núcleo de cálculo (concepção out/2026).

Fluxo: eixo (LandXML ou provisório) + levantamento da pista existente
+ levantamento(s) após o rebaixo -> pontos de controle (eixo, 3,50, 5,50)
-> limite inferior por espessura -> greide otimizado (mínimo volume com
suavidade) -> IRI simulado -> nota de serviço.

Convenções: cotas e espessuras verticais, em metros. Afastamento positivo à
DIREITA do sentido crescente das estacas (LD), negativo à esquerda (LE).
Caimentos com sinal do eixo para fora (tangente = -2 %).
"""
from __future__ import annotations

import math
import re
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np

# --------------------------------------------------------------------------
# Leitura de números e do levantamento
# --------------------------------------------------------------------------

def parse_num(text: str) -> float:
    """Aceita 9608922.993 / 9.608.922.993 / 9.608.922,993 / 9608922,993.

    Com vírgula: vírgula é decimal e pontos são milhar. Sem vírgula e com mais
    de um ponto: os pontos são separadores e os 3 últimos dígitos são decimais
    (formato exportado pela coletora). Um único ponto é decimal.
    """
    s = text.strip().replace(' ', '')
    if not s:
        raise ValueError('valor vazio')
    if ',' in s:
        return float(s.replace('.', '').replace(',', '.'))
    if s.count('.') > 1:
        neg = s.startswith('-')
        d = s.lstrip('+-').replace('.', '')
        v = float(d[:-3] + '.' + d[-3:])
        return -v if neg else v
    return float(s)


@dataclass
class Ponto:
    numero: str
    e: float
    n: float
    z: float
    desc: str
    origem: str               # nome do arquivo
    estaca: float = math.nan
    afast: float = math.nan
    dist_eixo: float = math.nan
    papel: str = ''


def ler_levantamento(path: str | Path) -> tuple[list[Ponto], dict]:
    """Lê número;coord1;coord2;Z;descrição (separador ; ou ,).

    A ordem N/E é detectada pela grandeza (UTM sul: N > 1.000.000).
    """
    path = Path(path)
    raw = path.read_bytes()
    try:
        text = raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        text = raw.decode('latin-1')
    linhas = [ln for ln in text.splitlines() if ln.strip()]
    sep = ';' if linhas and linhas[0].count(';') >= 3 else ','
    pts, ordem, vistos = [], None, set()
    avisos = []
    for i, ln in enumerate(linhas, 1):
        f = [c.strip() for c in ln.split(sep)]
        if len(f) < 4:
            avisos.append(f'linha {i} ignorada (menos de 4 campos)')
            continue
        try:
            a, b, z = parse_num(f[1]), parse_num(f[2]), parse_num(f[3])
        except ValueError:
            if i == 1:
                continue  # cabeçalho
            raise ValueError(f'{path.name} linha {i}: número inválido: {ln!r}')
        o = 'NE' if a > 1_000_000 > b else ('EN' if b > 1_000_000 > a else None)
        if o is None:
            raise ValueError(f'{path.name} linha {i}: não identifiquei N/E em {ln!r}')
        if ordem is None:
            ordem = o
        elif o != ordem:
            raise ValueError(f'{path.name} linha {i}: ordem N/E mudou no meio do arquivo')
        e, n = (b, a) if o == 'NE' else (a, b)
        num = f[0]
        if num in vistos:
            avisos.append(f'número repetido {num} (mantido, com sufixo)')
            num = f'{num}#{i}'
        vistos.add(num)
        pts.append(Ponto(num, e, n, z, f[4] if len(f) > 4 else '', path.name))
    if not pts:
        raise ValueError(f'{path.name}: nenhum ponto lido')
    return pts, dict(arquivo=path.name, pontos=len(pts), ordem=ordem, separador=sep, avisos=avisos)


def secoes_por_sequencia(pts: list[Ponto], gap: float = 3.0) -> list[list[Ponto]]:
    """Agrupa pontos consecutivos (ordem de coleta) em seções transversais."""
    sec = [[pts[0]]]
    for a, b in zip(pts, pts[1:]):
        if math.hypot(b.e - a.e, b.n - a.n) > gap:
            sec.append([b])
        else:
            sec[-1].append(b)
    return sec

# --------------------------------------------------------------------------
# Eixo
# --------------------------------------------------------------------------

@dataclass
class Eixo:
    nome: str
    e: np.ndarray          # vértices densos
    n: np.ndarray
    est: np.ndarray        # estaca acumulada de cada vértice (m)
    fonte: str
    curvas: list = field(default_factory=list)   # (est_ini, est_fim, raio, lado) lado: +1 curva p/ direita

    def ponto(self, s):
        s = np.asarray(s, float)
        return np.interp(s, self.est, self.e), np.interp(s, self.est, self.n)

    def rumo(self, s):
        """Vetor unitário tangente na estaca s."""
        s = np.atleast_1d(np.asarray(s, float))
        i = np.clip(np.searchsorted(self.est, s) - 1, 0, len(self.est) - 2)
        dx, dy = self.e[i + 1] - self.e[i], self.n[i + 1] - self.n[i]
        L = np.hypot(dx, dy)
        return dx / L, dy / L

    def projetar(self, e, n, chunk=2000):
        """Estaca, afastamento (+ direita) e distância de cada ponto ao eixo."""
        e = np.asarray(e, float); n = np.asarray(n, float)
        ax, ay = self.e[:-1], self.n[:-1]
        dx, dy = np.diff(self.e), np.diff(self.n)
        L2 = dx * dx + dy * dy
        out_s = np.empty(len(e)); out_o = np.empty(len(e)); out_d = np.empty(len(e))
        for k in range(0, len(e), chunk):
            pe, pn = e[k:k + chunk, None], n[k:k + chunk, None]
            t = np.clip(((pe - ax) * dx + (pn - ay) * dy) / L2, 0, 1)
            fx, fy = ax + t * dx, ay + t * dy
            d2 = (pe - fx) ** 2 + (pn - fy) ** 2
            j = np.argmin(d2, axis=1)
            r = np.arange(len(j))
            tt = t[r, j]
            L = np.sqrt(L2[j])
            de, dn = pe[:, 0] - fx[r, j], pn[:, 0] - fy[r, j]
            out_s[k:k + chunk] = self.est[j] + tt * L
            out_o[k:k + chunk] = (de * dy[j] - dn * dx[j]) / L
            out_d[k:k + chunk] = np.sqrt(d2[r, j])
        return out_s, out_o, out_d


def eixo_provisorio(pts: list[Ponto], est_inicial: float, passo: float = 1.0,
                    largura_min: float = 5.0) -> tuple[Eixo, dict]:
    """Eixo provisório = linha média suavizada dos pontos médios entre bordos.

    Só para desenvolvimento/prévia: deve ser substituído pelo eixo oficial.
    """
    from scipy.interpolate import make_smoothing_spline
    mids = []
    for s in secoes_por_sequencia(pts):
        if len(s) < 3:
            continue
        xy = np.array([(p.e, p.n) for p in s])
        dmat = np.hypot(xy[:, None, 0] - xy[None, :, 0], xy[:, None, 1] - xy[None, :, 1])
        i, j = np.unravel_index(np.argmax(dmat), dmat.shape)
        if dmat[i, j] < largura_min:
            continue
        mids.append((xy[i] + xy[j]) / 2)
    m = np.array(mids)
    c = np.r_[0, np.cumsum(np.hypot(*np.diff(m, axis=0).T))]
    keep = np.r_[True, np.diff(c) > 0.5]
    m, c = m[keep], c[keep]
    se, sn = make_smoothing_spline(c, m[:, 0]), make_smoothing_spline(c, m[:, 1])
    cc = np.arange(0, c[-1] + 1e-9, passo)
    E, N = se(cc), sn(cc)
    est = est_inicial + np.r_[0, np.cumsum(np.hypot(np.diff(E), np.diff(N)))]
    res = np.hypot(m[:, 0] - se(c), m[:, 1] - sn(c))
    eixo = Eixo('EIXO PROVISÓRIO (levantamento)', E, N, est, 'provisório')
    return eixo, dict(secoes_usadas=len(m), residuo_medio_m=float(res.mean()),
                      residuo_max_m=float(res.max()), comprimento_m=float(est[-1] - est[0]))


LX = '{http://www.landxml.org/schema/LandXML-1.2}'


def ler_eixo_landxml(path: str | Path, nome: str | None = None, passo: float = 0.5) -> Eixo:
    """LandXML: Alignment (Line/Curve/Spiral aproximada) ou polilinha (PntList2D/3D)."""
    raw = Path(path).read_bytes()
    if b'<!DOCTYPE' in raw.upper() or b'<!ENTITY' in raw.upper():
        raise ValueError('DTD/entidades não são aceitos no XML.')
    root = ET.fromstring(raw)
    ns = root.tag.split('}')[0] + '}' if root.tag.startswith('{') else ''
    als = root.findall(f'.//{ns}Alignment')
    if nome:
        als = [a for a in als if a.get('name') == nome]
    if not als:
        raise ValueError('Nenhum Alignment no XML' + (f' com nome {nome}' if nome else ''))
    if len(als) > 1:
        raise ValueError('Mais de um alinhamento; informe --eixo-nome. Nomes: ' +
                         ', '.join(a.get('name', '?') for a in als))
    al = als[0]
    sta0 = float(al.get('staStart', 0))

    def ne(node):  # LandXML: N E
        v = [float(x) for x in node.text.split()]
        return v[1], v[0]
    E, N, curvas = [], [], []
    s = sta0
    cg = al.find(f'{ns}CoordGeom')
    if cg is not None and len(cg):
        for el in cg:
            kind = el.tag.split('}')[-1]
            if kind == 'Line':
                a, b = ne(el.find(f'{ns}Start')), ne(el.find(f'{ns}End'))
                L = math.dist(a, b)
                k = max(1, int(math.ceil(L / passo)))
                t = np.linspace(0, 1, k + 1)
                E += list(a[0] + t * (b[0] - a[0])); N += list(a[1] + t * (b[1] - a[1]))
                s += L
            elif kind == 'Curve':
                a, b, c = ne(el.find(f'{ns}Start')), ne(el.find(f'{ns}End')), ne(el.find(f'{ns}Center'))
                R = float(el.get('radius'))
                sign = 1 if el.get('rot') == 'ccw' else -1
                a0 = math.atan2(a[1] - c[1], a[0] - c[0]); a1 = math.atan2(b[1] - c[1], b[0] - c[0])
                sweep = ((a1 - a0) * sign) % (2 * math.pi)
                L = R * sweep
                k = max(2, int(math.ceil(L / passo)))
                ang = a0 + sign * np.linspace(0, sweep, k + 1)
                E += list(c[0] + R * np.cos(ang)); N += list(c[1] + R * np.sin(ang))
                curvas.append((s, s + L, R, -sign))  # cw (sign -1) = curva à direita
                s += L
            elif kind == 'Spiral':
                a, b = ne(el.find(f'{ns}Start')), ne(el.find(f'{ns}End'))
                # aproximação por corda; transições exigem conferência
                E += [a[0], b[0]]; N += [a[1], b[1]]
                s += float(el.get('length', math.dist(a, b)))
            else:
                raise ValueError('Elemento de eixo não suportado: ' + kind)
    else:
        pl = al.find(f'.//{ns}PntList2D') or al.find(f'.//{ns}PntList3D')
        if pl is None:
            raise ValueError('Alinhamento sem CoordGeom nem lista de pontos.')
        v = [float(x) for x in pl.text.split()]
        step = 3 if pl.tag.endswith('3D') else 2
        for i in range(0, len(v), step):
            N.append(v[i]); E.append(v[i + 1])
    E, N = np.array(E), np.array(N)
    keep = np.r_[True, np.hypot(np.diff(E), np.diff(N)) > 1e-6]
    E, N = E[keep], N[keep]
    est = sta0 + np.r_[0, np.cumsum(np.hypot(np.diff(E), np.diff(N)))]
    return Eixo(al.get('name', 'EIXO'), E, N, est, f'LandXML {Path(path).name}', curvas)


def curvatura_estimada(eixo: Eixo, base: float = 60.0):
    """Raio aproximado ao longo do eixo (para eixo provisório)."""
    s = np.arange(eixo.est[0] + base, eixo.est[-1] - base, 10.0)
    tx0, ty0 = eixo.rumo(s - base / 2)
    tx1, ty1 = eixo.rumo(s + base / 2)
    dth = np.arctan2(tx0 * ty1 - ty0 * tx1, tx0 * tx1 + ty0 * ty1)
    with np.errstate(divide='ignore'):
        R = np.where(np.abs(dth) > 1e-9, base / np.abs(dth), np.inf)
    lado = np.where(dth < 0, 1, -1)  # giro horário = curva à direita
    return s, R, lado

# --------------------------------------------------------------------------
# Superfícies (TIN em estaca x afastamento) e amostragem dos controles
# --------------------------------------------------------------------------

class Superficie:
    """TIN no espaço (estaca, afastamento), com extrapolação lateral limitada."""

    def __init__(self, nome, s, o, z, extrap_max=0.6):
        from scipy.spatial import Delaunay
        self.nome = nome
        self.s, self.o, self.z = map(lambda a: np.asarray(a, float), (s, o, z))
        self.extrap_max = extrap_max
        pts = np.c_[self.s, self.o * 10.0]   # anisotropia: evita triângulos longos no sentido longitudinal
        self.tri = Delaunay(pts)
        self.smin, self.smax = self.s.min(), self.s.max()

    def amostrar(self, s, o, janela=3.0):
        """Retorna (z, situação). situação: 'interpolado', 'extrapolado', ou '' (sem dado)."""
        s = np.atleast_1d(np.asarray(s, float)); o = np.broadcast_to(np.asarray(o, float), s.shape)
        q = np.c_[s, o * 10.0]
        simp = self.tri.find_simplex(q)
        z = np.full(len(s), np.nan); sit = np.array([''] * len(s), dtype=object)
        ok = simp >= 0
        if ok.any():
            T = self.tri.transform[simp[ok]]
            b = np.einsum('ijk,ik->ij', T[:, :2, :], q[ok] - T[:, 2, :])
            bc = np.c_[b, 1 - b.sum(1)]
            verts = self.tri.simplices[simp[ok]]
            # triângulos muito longos (lacuna de levantamento) não valem
            ss = self.s[verts]
            span = ss.max(1) - ss.min(1)
            zz = (self.z[verts] * bc).sum(1)
            good = span <= 12.0
            idx = np.where(ok)[0]
            z[idx[good]] = zz[good]; sit[idx[good]] = 'interpolado'
        # extrapolação lateral: usa os 2 pontos mais externos do mesmo lado, perto da estaca
        for k in np.where(np.isnan(z))[0]:
            m = np.abs(self.s - s[k]) <= janela
            if m.sum() < 2:
                continue
            ss, oo, zz = self.s[m], self.o[m], self.z[m]
            lado = np.sign(o[k]) if o[k] != 0 else 1
            sel = (oo * lado) >= 0
            if sel.sum() < 2:
                continue
            ss, oo, zz = ss[sel], oo[sel], zz[sel]
            ext = np.argsort(-oo * lado)
            o1, z1 = oo[ext[0]], zz[ext[0]]
            if abs(o[k]) <= abs(o1):
                continue  # está dentro mas caiu em lacuna: não inventa
            if abs(abs(o[k]) - abs(o1)) > self.extrap_max:
                continue
            # inclinação da faixa externa (pontos a até 2 m do mais externo)
            mm = np.abs(oo - o1) <= 2.0
            if mm.sum() >= 2 and np.ptp(oo[mm]) > 0.3:
                slope = np.polyfit(oo[mm], zz[mm], 1)[0]
            else:
                continue
            z[k] = z1 + slope * (o[k] - o1); sit[k] = 'extrapolado'
        return z, sit


def separar_referencias(pts: list[Ponto], faixa=0.5, salto_min=0.15, salto_max=0.45, gap=3.0):
    """Em levantamento pós-rebaixo: ponto alto colado ao eixo = superfície original (referência).

    Regra da especificação de 20/10/2026 (parâmetros expostos, revisáveis).
    """
    for sec in _grupos_estaca(pts):
        perto = [p for p in sec if abs(p.afast) <= faixa]
        resto = [p for p in sec if abs(p.afast) > faixa]
        for p in sec:
            p.papel = 'FUNDO MEDIDO'
        if len(perto) < 1:
            continue
        base = [p.z for p in sorted(sec, key=lambda p: abs(p.afast))[:3]]
        zref = min(base)
        for p in perto:
            if salto_min <= p.z - zref <= salto_max:
                p.papel = 'REFERÊNCIA ORIGINAL'
    return pts


def _grupos_estaca(pts, gap=2.0):
    g = []
    for p in sorted(pts, key=lambda p: p.estaca):
        if not g or p.estaca - g[-1][-1].estaca > gap:
            g.append([])
        g[-1].append(p)
    return g

# --------------------------------------------------------------------------
# IRI (modelo de quarto de carro — ASTM E1926 / Sayers 1995)
# --------------------------------------------------------------------------

def iri(dist, cota, dx=0.25, trecho=None):
    """IRI (m/km) de um perfil longitudinal.

    dist, cota em m. O perfil é reamostrado a dx (linear), suavizado por média
    móvel de 250 mm e passado no quarto de carro padrão a 80 km/h.
    trecho: comprimento dos segmentos de saída (m); None = trecho todo.
    Retorna lista de (inicio, fim, iri_m_km).
    """
    from scipy.linalg import expm
    d = np.asarray(dist, float); z = np.asarray(cota, float)
    ok = ~np.isnan(z)
    d, z = d[ok], z[ok]
    if len(d) < 3:
        return []
    x = np.arange(d[0], d[-1] + 1e-9, dx)
    y = np.interp(x, d, z)
    k = max(1, int(round(0.25 / dx)))
    if k > 1:
        y = np.convolve(y, np.ones(k) / k, mode='valid')
        x = x[:len(y)]
    k1, k2, mu, c = 653.2, 63.3, 0.15, 6.0
    A = np.array([[0, 1, 0, 0], [-k2, -c, k2, c], [0, 0, 0, 1], [k2 / mu, c / mu, -(k1 + k2) / mu, -c / mu]])
    B = np.array([0, 0, 0, k1 / mu])
    v = 80 / 3.6
    dt = dx / v
    ST = expm(A * dt)
    PR = np.linalg.solve(A, (ST - np.eye(4)) @ B)
    n11 = max(1, int(round(11.0 / dx)))
    i0 = min(n11, len(y) - 1)
    slope0 = (y[i0] - y[0]) / (x[i0] - x[0])
    st = np.array([slope0, 0, slope0, 0])
    slopes = np.diff(y) / dx
    rect = np.empty(len(slopes))
    for i, sl in enumerate(slopes):
        st = ST @ st + PR * sl
        rect[i] = abs(st[2] - st[0])
    xs = x[1:]
    out = []
    if trecho is None:
        out.append((float(x[0]), float(x[-1]), float(rect.mean() * 1000)))
    else:
        ini = x[0]
        while ini < x[-1] - 1e-6:
            fim = min(ini + trecho, x[-1])
            m = (xs > ini) & (xs <= fim)
            if m.sum() > 10:
                out.append((float(ini), float(fim), float(rect[m].mean() * 1000)))
            ini = fim
    return out

# --------------------------------------------------------------------------
# Otimização do greide
# --------------------------------------------------------------------------

def otimizar_greide(est, lim_inf, delta2=0.0025, rampa_max=0.07, fixos=None, peso=None):
    """Greide de menor volume acima do limite inferior, com suavidade.

    Variáveis G_i nas estacas (passo constante). Minimiza sum(peso_i * G_i)
    (o volume é linear em G porque o fundo é fixo), sujeito a:
      G_i >= lim_inf_i                               (espessura mínima)
      |G_{i+1} - 2 G_i + G_{i-1}| <= delta2          (suavidade: variação de rampa por passo)
      |G_{i+1} - G_i| <= rampa_max * passo
      G_i == fixos[i] (amarrações), se informado
    Retorna (G, status).
    """
    from scipy.optimize import linprog
    from scipy.sparse import diags, vstack, csr_matrix
    est = np.asarray(est, float); lb = np.asarray(lim_inf, float)
    n = len(est)
    if n < 3:
        raise ValueError('poucas estacas')
    h = np.diff(est)
    if np.ptp(h) > 1e-6:
        raise ValueError('estacas precisam ter passo constante')
    h = h[0]
    # estacas sem limite (sem dado) recebem o limite interpolado; marcadas fora daqui
    lbf = lb.copy()
    nan = np.isnan(lbf)
    if nan.all():
        raise ValueError('nenhum limite inferior')
    lbf[nan] = np.interp(est[nan], est[~nan], lbf[~nan])
    D2 = diags([np.ones(n - 2), -2 * np.ones(n - 2), np.ones(n - 2)], [0, 1, 2], shape=(n - 2, n)).tocsr()
    D1 = diags([-np.ones(n - 1), np.ones(n - 1)], [0, 1], shape=(n - 1, n)).tocsr()
    A = vstack([D2, -D2, D1, -D1]).tocsr()
    b = np.r_[np.full(2 * (n - 2), delta2), np.full(2 * (n - 1), rampa_max * h)]
    bounds = [(l, None) for l in lbf]
    Aeq = beq = None
    if fixos:
        rows, vals = [], []
        for i, v in fixos.items():
            r = np.zeros(n); r[i] = 1; rows.append(r); vals.append(v)
        Aeq, beq = csr_matrix(np.array(rows)), np.array(vals)
    c = np.ones(n) if peso is None else np.asarray(peso, float)
    res = linprog(c, A_ub=A, b_ub=b, A_eq=Aeq, b_eq=beq, bounds=bounds, method='highs')
    if res.status != 0:
        return None, res.message
    return res.x, 'ótimo'
