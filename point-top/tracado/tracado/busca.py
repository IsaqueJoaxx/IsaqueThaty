"""Busca do traçado de menor custo em grade.

Estado = (célula, direção de chegada), 16 direções (inclui movimentos de cavalo,
dando passos de 26,6° e 18,4°). A mudança de direção por passo é limitada
(giro_max_grau) e paga peso_curva * Δθ², o que evita zigue-zague.

Custo da aresta (R$, índice):
  extensão  : L * custo.extensao
  greide    : rampa do terreno SUAVIZADO acima da rampa máxima vira altura
              equivalente de corte/aterro; acima de rampa_max + tolerância a aresta
              é proibida (força serpentina em vez de subir reto a encosta)
  relevo    : |terreno - terreno suavizado| = corte/aterro para regularizar o
              relevo local (grota, crista)
  meia-encosta: declividade transversal st gera seção mista ≈ b²·st/4 por metro;
              st acima de 1/talude_aterro → contenção (talude não fecha)
  drenagem  : entrar na faixa de um talvegue = bueiro (ou ponte, pela área);
              andar dentro da faixa paga custo.faixa_talvegue por metro
  restrições: polígono proibido = fora; penalidade = R$/m

As alternativas saem penalizando uma faixa em volta das já encontradas.
"""
from __future__ import annotations

import heapq
import math
from dataclasses import dataclass

import numpy as np
from scipy import ndimage

from . import hidrologia
from .config import Custos, Parametros
from .restricoes import rasterizar
from .terreno import Terreno

DIRS = [(1, 0), (2, 1), (1, 1), (1, 2), (0, 1), (-1, 2), (-1, 1), (-2, 1),
        (-1, 0), (-2, -1), (-1, -1), (-1, -2), (0, -1), (1, -2), (1, -1), (2, -1)]  # (dcol, dlin)
ANG = [math.atan2(dr, dc) for dc, dr in DIRS]
SEM_DIR = 16
NS = 17


def _fz(v: int) -> int:  # metade truncada para zero
    return int(v / 2)


def _fa(v: int) -> int:  # metade arredondada para longe do zero
    return int(math.copysign(math.ceil(abs(v) / 2), v)) if v else 0


@dataclass
class Campos:
    ok: np.ndarray        # célula utilizável
    zs: np.ndarray        # terreno suavizado (escala do greide)
    rel: np.ndarray       # |z - zs|
    gx: np.ndarray
    gy: np.ndarray
    acc: np.ndarray       # área de contribuição, m²
    talvegue: np.ndarray
    faixa: np.ndarray     # talvegue dilatado 1 célula
    acc_faixa: np.ndarray
    proibido: np.ndarray
    pen: np.ndarray       # R$/m das restrições
    a: int
    b: int
    borda: np.ndarray | None = None  # faixa junto à borda do MDT (penalizada)


def preparar(T: Terreno, A, B, prm: Parametros, restricoes: list[dict]) -> Campos:
    ia, ja = T.ij(*A)
    ib, jb = T.ij(*B)
    for nome, (i, j) in (("início", (ia, ja)), ("fim", (ib, jb))):
        if not T.dentro(i, j) or not np.isfinite(T.z[i, j]):
            raise ValueError(f"Ponto de {nome} fora do terreno")
    zs = T.suavizar(prm.sigma_suavizacao)
    rel = np.abs(T.z - zs)
    gx, gy = T.gradiente(T.suavizar(max(T.dx, 8.0)))
    acc = hidrologia.area_contribuicao(T.z, T.dx)
    talv = acc >= prm.area_bueiro_ha * 1e4
    faixa = ndimage.binary_dilation(talv, structure=np.ones((3, 3), bool))
    acc_faixa = ndimage.maximum_filter(np.where(talv, acc, 0.0), size=3)
    proib, pen = rasterizar(T, restricoes)
    if proib.any():  # o eixo fica a meia plataforma da área proibida
        proib = ndimage.distance_transform_edt(~proib) * T.dx < prm.plataforma / 2 + T.dx
    if proib[ia, ja] or proib[ib, jb]:
        raise ValueError("Início ou fim dentro de área proibida")
    X, Y = T.xy(*np.indices(T.z.shape))
    dab = math.dist(A, B)
    corr = np.hypot(X - A[0], Y - A[1]) + np.hypot(X - B[0], Y - B[1]) <= prm.fator_corredor * dab
    # a plataforma tem de caber dentro do MDT: perto da borda da superfície o passo é
    # penalizado (não proibido — A e B podem estar na borda, amarrando em via existente)
    n_er = int(math.ceil(prm.plataforma / 2 / T.dx)) + 1
    dentro_mdt = ndimage.binary_erosion(T.valido, iterations=n_er, border_value=0)
    r_ext = (n_er + 2) * T.dx
    perto = (np.hypot(X - A[0], Y - A[1]) <= r_ext) | (np.hypot(X - B[0], Y - B[1]) <= r_ext)
    ok = T.valido & corr & ~proib
    borda = T.valido & ~dentro_mdt & ~perto
    ok[ia, ja] = ok[ib, jb] = True
    return Campos(ok, zs, np.nan_to_num(rel), gx, gy, acc, talv, faixa, acc_faixa, proib, pen,
                  ia * T.nx + ja, ib * T.nx + jb, borda)


def custos_arestas(T: Terreno, C: Campos, prm: Parametros, cst: Custos):
    ny, nx = T.z.shape
    sm = (prm.talude_corte + prm.talude_aterro) / 2
    b = prm.plataforma
    lim = prm.rampa_max + prm.tolerancia_rampa
    st_lim = 1.0 / prm.talude_aterro
    zs = np.nan_to_num(C.zs, nan=-1e9)
    custos, comp = [], []
    for dc, dr in DIRS:
        hyp = math.hypot(dc, dr)
        L = T.dx * hyp
        i0, i1 = max(0, -dr), ny - max(0, dr)
        j0, j1 = max(0, -dc), nx - max(0, dc)

        def f(arr, oi, oj):
            return arr[i0 + oi:i1 + oi, j0 + oj:j1 + oj]

        S = lambda a: f(a, 0, 0)
        Tg = lambda a: f(a, dr, dc)
        Ma = lambda a: f(a, _fz(dr), _fa(dc))
        Mb = lambda a: f(a, _fa(dr), _fz(dc))

        ok = S(C.ok) & Tg(C.ok) & Ma(C.ok) & Mb(C.ok)
        g = np.abs(Tg(zs) - S(zs)) / L
        ok &= g <= lim
        hg = np.maximum(0.0, g - prm.rampa_max) * prm.sigma_suavizacao
        hr = (S(C.rel) + Tg(C.rel) + Ma(C.rel) + Mb(C.rel)) / 4
        h = hg + hr
        ux, uy = dc / hyp, dr / hyp
        gxm = (S(C.gx) + Tg(C.gx)) / 2
        gym = (S(C.gy) + Tg(C.gy)) / 2
        st = np.abs(gxm * uy - gym * ux)
        vol = b * h + sm * h * h + b * b * st / 4
        c = L * (cst.extensao + cst.terraplenagem * vol)
        c = c + L * cst.contencao * (st > st_lim)
        c = c + L * cst.faixa_talvegue * Tg(C.faixa)
        toca = Tg(C.faixa) | Ma(C.faixa) | Mb(C.faixa)
        entra = toca & ~S(C.faixa)
        accm = np.maximum(np.maximum(Tg(C.acc_faixa), Ma(C.acc_faixa)), Mb(C.acc_faixa))
        c = c + entra * np.where(accm >= prm.area_ponte_ha * 1e4, cst.ponte, cst.bueiro)
        c = c + L * Tg(C.pen)
        if C.borda is not None:
            c = c + L * cst.extensao * prm.penal_borda * Tg(C.borda)
        full = np.full((ny, nx), np.inf)
        full[i0:i1, j0:j1] = np.where(ok, c, np.inf)
        custos.append(full.ravel().tolist())
        comp.append(L)
    return custos, comp


def tabela_giro(prm: Parametros):
    lim = math.radians(prm.giro_max_grau) + 1e-9
    tab = []
    for din in range(16):
        linha = []
        for dout in range(16):
            d = abs((ANG[dout] - ANG[din] + math.pi) % (2 * math.pi) - math.pi)
            if d <= lim:
                linha.append((dout, prm.peso_curva * d * d))
        tab.append(linha)
    tab.append([(d, 0.0) for d in range(16)])  # partida: qualquer direção
    return tab


def buscar(custos, comp, trans, nx, inicio, fim, h, pen=None):
    """A* sobre (célula, direção). Retorna (células, custo) ou None."""
    INF = math.inf
    n = len(h)
    g = [INF] * (n * NS)
    pai = [-1] * (n * NS)
    offs = [dr * nx + dc for dc, dr in DIRS]
    s0 = inicio * NS + SEM_DIR
    g[s0] = 0.0
    heap = [(h[inicio], 0.0, s0)]
    pop, push = heapq.heappop, heapq.heappush
    while heap:
        _, gc, s = pop(heap)
        if gc > g[s]:
            continue
        c = s // NS
        if c == fim:
            cel = []
            while s != -1:
                cel.append(s // NS)
                s = pai[s]
            return cel[::-1], gc
        din = s - c * NS
        for dout, tc in trans[din]:
            ce = custos[dout][c]
            if ce == INF:
                continue
            t = c + offs[dout]
            ng = gc + ce + tc
            if pen is not None:
                ng += pen[t] * comp[dout]
            ns = t * NS + dout
            if ng < g[ns]:
                g[ns] = ng
                pai[ns] = s
                push(heap, (ng + h[t], ng, ns))
    return None


def gerar_alternativas(T: Terreno, C: Campos, prm: Parametros, cst: Custos, A, B, log=print):
    custos, comp = custos_arestas(T, C, prm, cst)
    trans = tabela_giro(prm)
    X, Y = T.xy(*np.indices(T.z.shape))
    h = (cst.extensao * np.hypot(X - B[0], Y - B[1])).ravel().tolist()
    pen = np.zeros(T.z.size)
    alts = []
    for k in range(prm.n_alternativas):
        r = buscar(custos, comp, trans, T.nx, C.a, C.b, h, pen.tolist() if k else None)
        if r is None:
            log(f"  alternativa {k + 1}: sem caminho viável (rampa/restrições)")
            break
        cel, custo = r
        cel = np.array(cel)
        sobre = float(np.mean(pen[cel] > 0)) if k else 0.0
        mask = np.zeros(T.z.shape, bool)
        mask.flat[cel] = True
        dist = ndimage.distance_transform_edt(~mask) * T.dx
        pen += (dist <= prm.dist_penalizacao).ravel() * prm.fator_penalizacao * cst.extensao
        ii, jj = np.divmod(cel, T.nx)
        xs, ys = T.xy(ii, jj)
        pts = np.column_stack([xs, ys]).astype(float)
        pts[0], pts[-1] = A, B
        alts.append({"id": f"ALT-{k + 1}", "celulas": cel, "pontos": pts,
                     "custo_busca": custo, "sobreposicao": sobre})
        log(f"  alternativa {k + 1}: {len(cel)} células, sobreposição com as anteriores {sobre:.0%}")
    return alts
