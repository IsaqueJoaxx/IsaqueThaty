"""Restrições em polígono: 'proibido' (a busca não entra) e 'penalidade' (R$/m)."""
from __future__ import annotations

import json

import numpy as np
from matplotlib.path import Path

from .terreno import Terreno


def carregar(itens: list | str | None) -> list[dict]:
    """Aceita lista no próprio config ou caminho de GeoJSON (Polygon/MultiPolygon).
    No GeoJSON, properties.tipo = 'proibido' | 'penalidade' e properties.custo_m."""
    if not itens:
        return []
    if isinstance(itens, str):
        gj = json.load(open(itens, encoding="utf-8"))
        out = []
        for f in gj.get("features", []):
            p = f.get("properties", {}) or {}
            g = f["geometry"]
            aneis = [g["coordinates"][0]] if g["type"] == "Polygon" else [c[0] for c in g["coordinates"]]
            for anel in aneis:
                out.append({"tipo": p.get("tipo", "proibido"), "custo_m": p.get("custo_m", 0.0),
                            "nome": p.get("nome", ""), "poligono": anel})
        return out
    return list(itens)


def rasterizar(T: Terreno, restricoes: list[dict]):
    """Retorna (proibido: bool[ny,nx], penalidade R$/m: float[ny,nx])."""
    proib = np.zeros(T.z.shape, bool)
    pen = np.zeros(T.z.shape)
    if not restricoes:
        return proib, pen
    X, Y = T.xy(*np.indices(T.z.shape))
    pts = np.column_stack([X.ravel(), Y.ravel()])
    for r in restricoes:
        dentro = Path(np.asarray(r["poligono"], float)).contains_points(pts).reshape(T.z.shape)
        if r.get("tipo", "proibido") == "proibido":
            proib |= dentro
        else:
            pen += dentro * float(r.get("custo_m", 0.0))
    return proib, pen
