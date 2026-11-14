"""Greide expedito e seções transversais sobre o MDT.

Greide: mínimos quadrados da diferença greide - terreno, com |rampa| <= rampa_max
em cada trecho e pontas presas no terreno (amarração com a via existente).
Sem curvas verticais — é greide de estudo, para estimar volumes e comparar
alternativas, não greide de projeto.

Seção: plataforma horizontal de largura b na cota do greide, talude de corte
1:talude_corte e de aterro 1:talude_aterro (H:V), pé/crista procurado no MDT
amostrado na normal ao eixo."""
from __future__ import annotations

import numpy as np
from scipy.optimize import lsq_linear

from .geometria import douglas_peucker


def greide_expedito(s: np.ndarray, z: np.ndarray, rampa_max: float, k_vertical: float = 15.0,
                    peso_pontas: float = 1e3):
    """k_vertical (m/%): a variação de rampa entre estacas é penalizada de forma que
    variar no ritmo de uma curva com esse K custe o mesmo que 0,5 m de afastamento
    do terreno. É suavização, não verificação de K."""
    z = np.asarray(z, float).copy()
    ruim = ~np.isfinite(z)
    if ruim.all():
        raise ValueError("eixo fora do terreno")
    if ruim.any():
        z[ruim] = np.interp(s[ruim], s[~ruim], z[~ruim])
    ds = np.diff(s)
    n = len(ds)
    A = np.tril(np.ones((n, n)))
    b = z[1:] - z[0]
    if k_vertical and n > 2:
        # variação de rampa: (d[i+1]/ds[i+1] - d[i]/ds[i]); ritmo de referência ds_med/(100 K)
        dsm = (ds[1:] + ds[:-1]) / 2
        ref = dsm / (100.0 * k_vertical)
        w = 0.5 / ref
        Dg = np.zeros((n - 1, n))
        idx = np.arange(n - 1)
        Dg[idx, idx] = -w / ds[:-1]
        Dg[idx, idx + 1] = w / ds[1:]
        A = np.vstack([A, Dg])
        b = np.append(b, np.zeros(n - 1))
    # amarração do fim
    A = np.vstack([A, peso_pontas * np.ones(n)])
    b = np.append(b, peso_pontas * (z[-1] - z[0]))
    lim = rampa_max * ds
    sol = lsq_linear(A, b, bounds=(-lim, lim), method="bvls" if n <= 600 else "trf")
    p = np.concatenate([[z[0]], z[0] + np.cumsum(sol.x)])
    return p, ruim


def pvis(s, p, tol: float = 0.05):
    pts = douglas_peucker(np.column_stack([s, p]), tol)
    return pts


def _lado(zg: np.ndarray, y: np.ndarray, p: float, b2: float, sc: float, sa: float):
    """Uma metade da seção. zg, y crescentes a partir do eixo. Retorna
    (área de corte, área de aterro, afastamento do pé/crista ou None se não fecha,
    True se a seção saiu do MDT antes de fechar)."""
    dy = y[1] - y[0]
    corte = aterro = 0.0
    k_b = int(round(b2 / dy))
    plat = zg[:k_b + 1] - p
    if not np.all(np.isfinite(plat)):
        return np.nan, np.nan, None, True
    pos = np.clip(plat, 0, None)
    neg = np.clip(-plat, 0, None)
    corte += np.trapezoid(pos, dx=dy)
    aterro += np.trapezoid(neg, dx=dy)
    if zg[k_b] >= p:  # corte: talude subindo até encontrar o terreno
        tal = p + (y[k_b:] - b2) / sc
        dif = zg[k_b:] - tal
        alvo = "corte"
    else:
        tal = p - (y[k_b:] - b2) / sa
        dif = tal - zg[k_b:]
        alvo = "aterro"
    area = 0.0
    for k in range(1, len(dif)):
        d0, d1 = dif[k - 1], dif[k]
        if not np.isfinite(d1):
            return corte, aterro, None, True
        if d1 <= 0:
            fr = d0 / (d0 - d1) if d0 > d1 else 0.0
            area += 0.5 * max(d0, 0) * fr * dy
            pe = y[k_b + k - 1] + fr * dy
            if alvo == "corte":
                corte += area
            else:
                aterro += area
            return corte, aterro, pe, False
        area += 0.5 * (max(d0, 0) + d1) * dy
    if alvo == "corte":
        corte += area
    else:
        aterro += area
    return corte, aterro, None, False


def secoes(T, s, x, y, ux, uy, p, plataforma, sc, sa, meia_largura, dy=0.5):
    off = np.arange(0.0, meia_largura + dy / 2, dy)
    nlx, nly = -uy, ux  # normal à esquerda
    res = []
    for k in range(len(s)):
        lados = []
        for sinal in (1.0, -1.0):
            xs = x[k] + sinal * off * nlx[k]
            ys = y[k] + sinal * off * nly[k]
            zg = T.cota(xs, ys)
            lados.append(_lado(zg, off, p[k], plataforma / 2, sc, sa))
        (c1, a1, pe1, f1), (c2, a2, pe2, f2) = lados
        res.append({"corte": c1 + c2, "aterro": a1 + a2,
                    "fecha": pe1 is not None and pe2 is not None,
                    "sem_mdt": f1 or f2,
                    "pe_esq": pe1, "pe_dir": pe2})
    return res


def volumes(s, sec):
    ac = np.array([r["corte"] for r in sec])
    aa = np.array([r["aterro"] for r in sec])
    ds = np.diff(s)
    vc = float(np.nansum((ac[1:] + ac[:-1]) / 2 * ds))
    va = float(np.nansum((aa[1:] + aa[:-1]) / 2 * ds))
    return vc, va, ac, aa
