"""Do caminho em grade para um eixo de projeto: PIs (Douglas-Peucker), curvas
circulares com R >= raio_min encaixadas nas tangentes disponíveis, e o
alinhamento resultante (retas + arcos) com estaqueamento."""
from __future__ import annotations

import math
from dataclasses import dataclass, field

import numpy as np


def douglas_peucker(p: np.ndarray, eps: float) -> np.ndarray:
    n = len(p)
    if n < 3:
        return p.copy()
    manter = np.zeros(n, bool)
    manter[0] = manter[-1] = True
    pilha = [(0, n - 1)]
    while pilha:
        a, b = pilha.pop()
        if b <= a + 1:
            continue
        v = p[b] - p[a]
        nv = np.hypot(*v)
        w = p[a + 1:b] - p[a]
        d = np.abs(v[0] * w[:, 1] - v[1] * w[:, 0]) / nv if nv > 0 else np.hypot(w[:, 0], w[:, 1])
        k = int(np.argmax(d))
        if d[k] > eps:
            m = a + 1 + k
            manter[m] = True
            pilha += [(a, m), (m, b)]
    return p[manter]


def _unit(v):
    n = math.hypot(*v)
    return v / n


def _deflexao(a, b, c) -> float:
    """Ângulo com sinal (rad), + = à esquerda (anti-horário)."""
    u, w = _unit(b - a), _unit(c - b)
    return math.atan2(u[0] * w[1] - u[1] * w[0], u[0] * w[0] + u[1] * w[1])


def _intersecao(p1, d1, p2, d2):
    den = d1[0] * d2[1] - d1[1] * d2[0]
    if abs(den) < 1e-12:
        return None, None, None
    q = p2 - p1
    t = (q[0] * d2[1] - q[1] * d2[0]) / den
    s = (q[0] * d1[1] - q[1] * d1[0]) / den
    return p1 + t * d1, t, s


def ajustar_curvas(pis: np.ndarray, rmin: float, rdes: float, tangente_min: float = 0.0,
                   ang_min_grau: float = 0.5, max_iter: int = 500):
    """Ajusta raios e, quando não cabem curvas de raio mínimo entre dois PIs,
    funde os PIs (mesmo sentido) ou remove o de menor deflexão (sentidos opostos).
    Entre duas curvas consecutivas fica pelo menos 'tangente_min' de reta.
    Retorna (PIs, raios das curvas internas, avisos)."""
    P = [np.asarray(p, float) for p in pis]
    avisos = []
    for _ in range(max_iter):
        # remove PIs quase colineares
        mudou = True
        while mudou and len(P) > 2:
            mudou = False
            for k in range(1, len(P) - 1):
                if abs(_deflexao(P[k - 1], P[k], P[k + 1])) < math.radians(ang_min_grau) \
                        or math.dist(P[k - 1], P[k]) < 1e-6:
                    del P[k]
                    mudou = True
                    break
        n = len(P)
        if n <= 2:
            return np.array(P), [], avisos
        D = [_deflexao(P[k - 1], P[k], P[k + 1]) for k in range(1, n - 1)]
        Lseg = [math.dist(P[k], P[k + 1]) - (tangente_min if 1 <= k <= n - 3 else 0.0)
                for k in range(n - 1)]
        R = [rdes] * (n - 2)

        def T(m):
            return R[m] * math.tan(abs(D[m]) / 2)

        for _ in range(60):
            alterou = False
            for k in range(n - 1):
                ms = [m for m in (k - 1, k) if 0 <= m < n - 2]  # curvas nas pontas do segmento k
                soma = sum(T(m) for m in ms)
                if soma > Lseg[k] + 1e-9:
                    fator = max(Lseg[k], 0.0) / soma
                    for m in ms:
                        novo = max(rmin, R[m] * fator)
                        if novo < R[m] - 1e-9:
                            R[m] = novo
                            alterou = True
            if not alterou:
                break
        viol = []
        for k in range(n - 1):
            ms = [m for m in (k - 1, k) if 0 <= m < n - 2]
            exc = sum(T(m) for m in ms) - Lseg[k]
            if exc > 1e-6:
                viol.append((exc, k, ms))
        if not viol:
            for m, d in enumerate(D):
                if abs(d) > math.radians(150):
                    avisos.append(f"curva em grampo no PI {m + 1} (deflexão {math.degrees(abs(d)):.0f}°)")
            return np.array(P), R, avisos
        _, k, ms = max(viol)
        if len(ms) == 2 and D[ms[0]] * D[ms[1]] > 0:
            # funde os dois PIs na interseção das tangentes externas
            a, b = k, k + 1
            X, t, s = _intersecao(P[a], _unit(P[a] - P[a - 1]), P[b], _unit(P[b] - P[b + 1]))
            if X is not None and t > 0 and s > 0:
                P[a:b + 1] = [X]
                continue
        if len(ms) == 2:
            alvo = ms[0] if abs(D[ms[0]]) < abs(D[ms[1]]) else ms[1]
        else:
            alvo = ms[0]
        del P[alvo + 1]
    avisos.append("ajuste de curvas não convergiu: raios mínimos adotados sem verificação de tangente")
    return np.array(P), [rmin] * (len(P) - 2), avisos


@dataclass
class Elemento:
    tipo: str                      # 'reta' | 'curva'
    p0: np.ndarray
    p1: np.ndarray
    comprimento: float
    est0: float = 0.0
    centro: np.ndarray | None = None
    raio: float = 0.0
    delta: float = 0.0             # rad, + = anti-horário
    pi: np.ndarray | None = None


@dataclass
class Alinhamento:
    nome: str
    elementos: list = field(default_factory=list)
    pis: np.ndarray | None = None
    raios: list = field(default_factory=list)

    @property
    def comprimento(self) -> float:
        return sum(e.comprimento for e in self.elementos)

    def amostrar(self, passo: float):
        """Estacas a cada 'passo' (mais início, fim e pontos notáveis).
        Retorna s, x, y, ux, uy (tangente unitária)."""
        S, X, Y, UX, UY = [], [], [], [], []
        for e in self.elementos:
            n = max(1, int(math.ceil(e.comprimento / passo)))
            # estacas inteiras do passo dentro do elemento + extremos
            locais = np.unique(np.concatenate([
                [0.0],
                np.arange(math.ceil(e.est0 / passo) * passo, e.est0 + e.comprimento, passo) - e.est0,
                [e.comprimento]]))
            locais = locais[(locais >= -1e-9) & (locais <= e.comprimento + 1e-9)]
            if e.tipo == "reta":
                u = _unit(e.p1 - e.p0)
                for l in locais:
                    q = e.p0 + u * l
                    S.append(e.est0 + l); X.append(q[0]); Y.append(q[1]); UX.append(u[0]); UY.append(u[1])
            else:
                sg = 1.0 if e.delta > 0 else -1.0
                r0 = e.p0 - e.centro
                a0 = math.atan2(r0[1], r0[0])
                for l in locais:
                    a = a0 + sg * l / e.raio
                    q = e.centro + e.raio * np.array([math.cos(a), math.sin(a)])
                    t = sg * np.array([-math.sin(a), math.cos(a)])
                    S.append(e.est0 + l); X.append(q[0]); Y.append(q[1]); UX.append(t[0]); UY.append(t[1])
        S = np.array(S)
        _, idx = np.unique(np.round(S, 6), return_index=True)
        return S[idx], np.array(X)[idx], np.array(Y)[idx], np.array(UX)[idx], np.array(UY)[idx]


def construir(nome: str, P: np.ndarray, R: list) -> Alinhamento:
    al = Alinhamento(nome, pis=np.asarray(P), raios=list(R))
    cur = np.asarray(P[0], float)
    est = 0.0
    for k in range(1, len(P) - 1):
        uin, uout = _unit(P[k] - P[k - 1]), _unit(P[k + 1] - P[k])
        d = math.atan2(uin[0] * uout[1] - uin[1] * uout[0], float(uin @ uout))
        r = R[k - 1]
        t = r * math.tan(abs(d) / 2)
        pc, pt = P[k] - uin * t, P[k] + uout * t
        if math.dist(cur, pc) > 1e-6:
            L = math.dist(cur, pc)
            al.elementos.append(Elemento("reta", cur, pc, L, est))
            est += L
        nl = np.array([-uin[1], uin[0]])
        centro = pc + nl * r * (1 if d > 0 else -1)
        L = r * abs(d)
        al.elementos.append(Elemento("curva", pc, pt, L, est, centro, r, d, np.asarray(P[k], float)))
        est += L
        cur = pt
    fim = np.asarray(P[-1], float)
    if math.dist(cur, fim) > 1e-6:
        al.elementos.append(Elemento("reta", cur, fim, math.dist(cur, fim), est))
    return al
