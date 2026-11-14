"""Avalia cada alternativa já com a geometria final (retas + curvas) sobre o MDT
real — os números do relatório vêm daqui, não do custo da busca."""
from __future__ import annotations

import math

import numpy as np

from . import geometria, greide
from .config import Custos, Parametros


def _celulas(T, x, y):
    ii = np.clip(np.round((y - T.y0) / T.dx).astype(int), 0, T.ny - 1)
    jj = np.clip(np.round((x - T.x0) / T.dx).astype(int), 0, T.nx - 1)
    return ii, jj


def geometria_final(T, C, alt: dict, prm: Parametros):
    """Simplifica e encaixa curvas. Se o eixo resultante cortar área proibida ou sair do
    MDT, tenta de novo com tolerância menor e depois com raio desejado = raio mínimo."""
    eps0 = prm.tolerancia_dp or 1.2 * T.dx
    melhor = None
    for rdes in (prm.raio_desejado, prm.raio_min):
        for f in (1.0, 0.6, 0.35, 0.2):
            pis = geometria.douglas_peucker(alt["pontos"], eps0 * f)
            P, R, avisos = geometria.ajustar_curvas(pis, prm.raio_min, rdes, prm.tangente_min)
            al = geometria.construir(alt["id"], P, R)
            s, x, y, *_ = al.amostrar(min(prm.passo_amostragem, T.dx / 2))
            ii, jj = _celulas(T, x, y)
            fora = C.proibido[ii, jj] | ~np.isfinite(T.cota(x, y))  # área proibida ou fora do MDT
            invade = float(np.sum(np.gradient(s)[fora])) if len(s) > 1 else 0.0
            if melhor is None or invade < melhor[0] - 1e-9:
                melhor = (invade, al, avisos)
            if invade == 0:
                return al, avisos
    return melhor[1], melhor[2]


def avaliar(T, C, alt: dict, prm: Parametros, cst: Custos) -> dict:
    al, avisos = geometria_final(T, C, alt, prm)
    return avaliar_alinhamento(T, C, al, prm, cst, alt["id"], alt["sobreposicao"], avisos)


def avaliar_alinhamento(T, C, al, prm: Parametros, cst: Custos, ident: str, sobreposicao=None,
                        avisos=(), greide_fixo=None) -> dict:
    """greide_fixo: função s -> cota (greide de projeto); sem ela usa o greide expedito."""
    avisos = list(avisos)
    s, x, y, ux, uy = al.amostrar(prm.passo_amostragem)
    z = T.cota(x, y)
    if greide_fixo is None:
        p, fora = greide.greide_expedito(s, z, prm.rampa_max, prm.k_vertical)
        if abs(p[-1] - z[-1]) > 0.10:
            avisos.append(f"greide não amarra no fim ({p[-1] - z[-1]:+.2f} m): o desnível pede rampa média "
                          f"acima da máxima nessa extensão")
    else:
        p, fora = greide_fixo(s), ~np.isfinite(z)
    if fora.any():
        avisos.append(f"{int(fora.sum())} estacas fora do terreno (cota interpolada)")
    sec = greide.secoes(T, s, x, y, ux, uy, p, prm.plataforma, prm.talude_corte,
                        prm.talude_aterro, prm.meia_largura_secao)
    vc, va, ac, aa = greide.volumes(s, sec)
    ds = np.gradient(s)
    nao_fecha = float(sum(d for d, r in zip(ds, sec) if not r["fecha"] and not r["sem_mdt"]))
    sem_mdt = float(sum(d for d, r in zip(ds, sec) if r["sem_mdt"]))
    if sem_mdt > 0:
        avisos.append(f"{sem_mdt:.0f} m com a seção saindo da área levantada (volume subestimado aí)")

    rampas = np.diff(p) / np.diff(s)
    h = p - z

    # travessias de talvegue ao longo do eixo final
    ii, jj = _celulas(T, x, y)
    na_faixa = C.faixa[ii, jj]
    trav = []
    k = 0
    while k < len(s):
        if na_faixa[k]:
            k0 = k
            while k < len(s) and na_faixa[k]:
                k += 1
            seg = slice(k0, k)
            area = float(np.max(C.acc_faixa[ii[seg], jj[seg]])) / 1e4
            kmax = k0 + int(np.argmax(C.acc_faixa[ii[seg], jj[seg]]))
            tipo = "ponte" if area >= prm.area_ponte_ha else "bueiro"
            trav.append({"estaca": float(s[kmax]), "area_ha": area, "tipo": tipo,
                         "altura_aterro": float(max(np.nan_to_num(h[kmax]), 0.0)),
                         "extensao_na_faixa": float(s[min(k, len(s) - 1)] - s[k0])})
        else:
            k += 1
    invade = float(np.sum(ds[C.proibido[ii, jj]]))
    if invade > 0:
        avisos.append(f"eixo final passa {invade:.0f} m dentro de área proibida (o ajuste de curvas cortou caminho)")
    pen_rest = float(np.sum(ds * C.pen[ii, jj]))

    nb = sum(t["tipo"] == "bueiro" for t in trav)
    npn = sum(t["tipo"] == "ponte" for t in trav)
    faixa_m = float(sum(t["extensao_na_faixa"] for t in trav))
    custo = {
        "extensao": cst.extensao * al.comprimento,
        "terraplenagem": cst.terraplenagem * (vc + va),
        "bueiros": cst.bueiro * nb,
        "pontes": cst.ponte * npn,
        "contencao": cst.contencao * nao_fecha,
        "faixa_talvegue": cst.faixa_talvegue * faixa_m,
        "restricoes": pen_rest,
    }
    raios = [e.raio for e in al.elementos if e.tipo == "curva"]
    return {
        "id": ident,
        "alinhamento": al,
        "avisos": avisos,
        "sobreposicao": sobreposicao,
        "extensao_m": al.comprimento,
        "n_curvas": len(raios),
        "raio_min_m": min(raios) if raios else math.inf,
        "curvas_abaixo_desejado": sum(r < prm.raio_desejado - 1e-6 for r in raios),
        "rampa_max_pct": float(np.max(np.abs(rampas)) * 100) if len(rampas) else 0.0,
        "corte_m3": vc,
        "aterro_m3": va,
        "balanco_m3": vc - va,
        "corte_max_m": float(max(np.nanmax(-h), 0.0)) if np.isfinite(h).any() else math.nan,
        "aterro_max_m": float(max(np.nanmax(h), 0.0)) if np.isfinite(h).any() else math.nan,
        "talude_nao_fecha_m": nao_fecha,
        "secao_fora_mdt_m": sem_mdt,
        "travessias": trav,
        "n_bueiros": nb,
        "n_pontes": npn,
        "custo": custo,
        "custo_total": sum(custo.values()),
        "perfil": {"s": s, "x": x, "y": y, "terreno": z, "greide": p, "area_corte": ac, "area_aterro": aa},
    }
