"""Eixo de referência (o do projetista) lido de LandXML, para comparar com as
alternativas pelo mesmo critério. Lê retas e curvas circulares; espiral entra
como corda (com aviso). Lê também o primeiro perfil de projeto (PVIs e curvas
verticais parabólicas)."""
from __future__ import annotations

import math
import xml.etree.ElementTree as ET

import numpy as np

from .geometria import Alinhamento, Elemento


def _pt(el, nome):
    f = el.find("{*}" + nome)
    if f is None or not (f.text or "").split():
        return None
    n, e = (float(v) for v in f.text.split()[:2])
    return np.array([e, n])  # LandXML: Norte Leste


def ler_alinhamento(caminho: str, nome: str):
    """Retorna (Alinhamento, PVIs [(estaca, cota, L_curva)], nome do perfil, avisos)."""
    al = Alinhamento(nome)
    pvis, avisos = [], []
    perfil = None
    dentro = em_prof = achou = False
    sta0 = 0.0
    est = 0.0
    ctx = ET.iterparse(caminho, events=("start", "end"))
    _, raiz = next(ctx)
    for k, (ev, el) in enumerate(ctx):
        t = el.tag.split("}")[-1]
        if ev == "start":
            if t == "Alignment" and el.get("name") == nome:
                dentro = True
                sta0 = float(el.get("staStart") or 0.0)
            elif dentro and t == "ProfAlign" and perfil is None:
                em_prof, perfil = True, el.get("name")
            continue
        if dentro:
            if t in ("Line", "Curve", "Spiral") and not em_prof:
                p0, p1 = _pt(el, "Start"), _pt(el, "End")
                if t == "Line":
                    L = math.dist(p0, p1)
                    al.elementos.append(Elemento("reta", p0, p1, L, est))
                elif t == "Curve":
                    c = _pt(el, "Center")
                    r = float(el.get("radius") or math.dist(c, p0))
                    sg = 1.0 if el.get("rot", "ccw") == "ccw" else -1.0
                    a0 = math.atan2(*(p0 - c)[::-1])
                    a1 = math.atan2(*(p1 - c)[::-1])
                    d = (sg * (a1 - a0)) % (2 * math.pi)
                    L = r * d
                    al.elementos.append(Elemento("curva", p0, p1, L, est, c, r, sg * d, _pt(el, "PI")))
                else:
                    L = math.dist(p0, p1)
                    al.elementos.append(Elemento("reta", p0, p1, L, est))
                    avisos.append("espiral aproximada por corda")
                est += L
            elif em_prof and t in ("PVI", "ParaCurve", "CircCurve"):
                sta, cota = (float(v) for v in el.text.split()[:2])
                Lc = float(el.get("length") or 0.0) if t != "PVI" else 0.0
                pvis.append((sta - sta0, cota, Lc))
            elif t == "ProfAlign":
                em_prof = False
            elif t == "Alignment":
                achou = True
                break
        if not dentro:  # dentro do eixo os filhos (Start, End...) ainda serão lidos pelo pai
            el.clear()
            if k % 200000 == 0:
                raiz.clear()
    if not achou or not al.elementos:
        raise ValueError(f"Alinhamento {nome!r} não encontrado em {caminho}")
    return al, pvis, perfil, avisos


def cota_perfil(pvis, s):
    """Cota do greide de projeto nas estacas s (tangentes + parábolas simétricas)."""
    st = np.array([p[0] for p in pvis], float)
    el = np.array([p[1] for p in pvis], float)
    z = np.interp(s, st, el)
    for i in range(1, len(pvis) - 1):
        L = pvis[i][2]
        if L <= 0:
            continue
        g1 = (el[i] - el[i - 1]) / (st[i] - st[i - 1])
        g2 = (el[i + 1] - el[i]) / (st[i + 1] - st[i])
        pcv = st[i] - L / 2
        m = (s >= pcv) & (s <= st[i] + L / 2)
        x = s[m] - pcv
        z[m] = el[i] - g1 * L / 2 + g1 * x + (g2 - g1) / (2 * L) * x ** 2
    return z
