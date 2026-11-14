"""Uso:  python -m tracado config.json"""
from __future__ import annotations

import json
import os
import sys
import time

import numpy as np

from . import avaliacao, busca, exportar, greide, referencia
from .config import Custos, Parametros, montar
from .restricoes import carregar
from .terreno import ler_asc, ler_landxml, ler_xyz, terreno_sintetico


def carregar_terreno(cfg: dict, base: str):
    t = cfg["terreno"]
    tipo = t.get("tipo", "landxml")
    arq = os.path.join(base, t["arquivo"]) if "arquivo" in t else None
    if tipo == "landxml":
        return ler_landxml(arq, float(t.get("dx", 5.0)), t.get("superficie"))
    if tipo == "asc":
        return ler_asc(arq)
    if tipo == "xyz":
        return ler_xyz(arq, float(t.get("dx", 5.0)))
    if tipo == "sintetico":
        return terreno_sintetico(**{k: v for k, v in t.items() if k != "tipo"})
    raise ValueError(f"tipo de terreno desconhecido: {tipo}")


def executar(cfg: dict, base: str = ".", log=print) -> list[dict]:
    t0 = time.time()
    prm = montar(Parametros, cfg.get("parametros"))
    cst = montar(Custos, cfg.get("custos"))
    T = carregar_terreno(cfg, base)
    A = tuple(map(float, cfg["inicio"]))
    B = tuple(map(float, cfg["fim"]))
    restr = carregar(cfg.get("restricoes"))
    if isinstance(cfg.get("restricoes"), str):
        restr = carregar(os.path.join(base, cfg["restricoes"]))
    saida = os.path.join(base, cfg.get("saida", "saida"))
    os.makedirs(saida, exist_ok=True)
    log(f"Terreno {T.nome}: {T.nx} x {T.ny} células de {T.dx:g} m, cotas {np.nanmin(T.z):.1f}–{np.nanmax(T.z):.1f}")

    C = busca.preparar(T, A, B, prm, restr)
    log(f"Preparação: {time.time() - t0:.1f} s — {int(C.ok.sum())} células no corredor de busca")
    t1 = time.time()
    alts = busca.gerar_alternativas(T, C, prm, cst, A, B, log=log)
    log(f"Busca: {time.time() - t1:.1f} s")
    res = []
    for a in alts:
        r = avaliacao.avaliar(T, C, a, prm, cst)
        pf = r["perfil"]
        r["pvis"] = greide.pvis(pf["s"], pf["greide"], 0.15)
        res.append(r)
    res.sort(key=lambda r: r["custo_total"])

    # eixo de referência (o do projetista), avaliado pelo mesmo critério
    rc = cfg.get("referencia")
    if rc:
        arq = os.path.join(base, rc.get("arquivo") or cfg["terreno"]["arquivo"])
        al, pv, nome_perfil, av = referencia.ler_alinhamento(arq, rc["alinhamento"])
        refs = [avaliacao.avaliar_alinhamento(T, C, al, prm, cst, f"REF {al.nome} · greide expedito",
                                              avisos=av)]
        if pv:
            refs.append(avaliacao.avaliar_alinhamento(
                T, C, al, prm, cst, f"REF {al.nome} · {nome_perfil}", avisos=av,
                greide_fixo=lambda s: referencia.cota_perfil(pv, s)))
        for r in refs:
            r["referencia"] = True
        res += refs

    exportar.landxml(os.path.join(saida, "tracados.xml"), res)
    exportar.csv_pis(os.path.join(saida, "pis.csv"), res)
    exportar.csv_comparativo(os.path.join(saida, "comparativo.csv"), res)
    exportar.figuras(saida, T, C, res, A, B, restr, cfg.get("titulo", "Traçado preliminar"))
    resumo = [{k: v for k, v in r.items() if k not in ("alinhamento", "perfil", "pvis")} for r in res]
    with open(os.path.join(saida, "resumo.json"), "w", encoding="utf-8") as f:
        json.dump({"parametros": prm.__dict__, "custos": cst.__dict__, "alternativas": resumo},
                  f, ensure_ascii=False, indent=2, default=float)

    larg = [max(len(l[c]) for l in exportar.tabela(res)) for c in range(len(exportar.COLUNAS))]
    for l in exportar.tabela(res):
        log("  ".join(v.rjust(w) for v, w in zip(l, larg)))
    for r in res:
        for a in r["avisos"]:
            log(f"  [{r['id']}] {a}")
        for t in r["travessias"]:
            log(f"  [{r['id']}] {t['tipo']} na estaca {t['estaca']:.0f} m, bacia {t['area_ha']:.1f} ha, "
                f"aterro no eixo {t['altura_aterro']:.1f} m")
    log(f"Total: {time.time() - t0:.1f} s. Saída em {saida}")
    return res


def main():
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(1)
    cam = sys.argv[1]
    cfg = json.load(open(cam, encoding="utf-8"))
    executar(cfg, base=os.path.dirname(os.path.abspath(cam)))


if __name__ == "__main__":
    main()
