#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
kml_para_pavimento.py
=====================
Lê um KML/KMZ com os EIXOS das ruas e desenha a pavimentação como nas pranchas
de projeto (ex.: DRE-PROF. RUA BRASIL - Esmeraldas):

  * hachura sólida cinza da pista, 3,50 m para cada lado do eixo (ajustável);
  * ruas unidas numa só mancha, com concordância (raio de meio-fio) nas esquinas;
  * meio-fio/sarjeta em laranja contornando a pista (pontas livres ficam abertas);
  * comprimento de cada trecho de meio-fio escrito ao lado ("66,11 m");
  * cota da largura da rua (7,00 m) no meio de cada eixo;
  * quadro de quantidades (área de pavimento, meio-fio, sarjeta).

Os eixos que terminam perto de outro eixo (tolerância --tol-ponta) são esticados
ou aparados até encostar nele, para as travessas ligarem certinho na rua principal.

Uso:
  python kml_para_pavimento.py ruas.kmz
  python kml_para_pavimento.py ruas.kml --largura 7 --raio 3 --altura-texto 1.5
  python kml_para_pavimento.py            (abre janela para escolher o arquivo)

Saída (na pasta do KML, ou em -o):
  <nome>_pavimento.dwg   (ou .dxf se o ODA File Converter não estiver instalado)
  <nome>_trechos.csv     comprimento de cada trecho de meio-fio

Requisitos: pip install ezdxf shapely pyproj
DWG: ODA File Converter (gratuito). Sem ele sai DXF, que abre direto no Civil 3D.
"""
from __future__ import annotations

import argparse
import csv
import math
import os
import re
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

try:
    import ezdxf
    import numpy as np
    import shapely
    from ezdxf import colors, zoom
    from ezdxf.enums import TextEntityAlignment
    from ezdxf.lldxf import const as dxfconst
    from pyproj import Transformer
    from shapely.geometry import LineString, MultiLineString, MultiPolygon, Point, Polygon
    from shapely.ops import substring, unary_union
except ImportError as e:  # pragma: no cover
    sys.exit(f"Falta biblioteca: {e.name}.  Instale com:  pip install ezdxf shapely pyproj")

QS = 16                    # segmentos por quarto de círculo nas concordâncias
COR_PAV = (98, 99, 101)    # cinza da hachura (igual à prancha)
VERSOES_DWG = ["ACAD2018", "ACAD2013", "ACAD2010", "ACAD2007", "ACAD2004", "ACAD2000"]

LAYERS = {
    #  nome               cor ACI  linetype      espessura (1/100 mm)
    "PAV_HACHURA":       (8,   "Continuous", -3),
    "PAV_MEIO-FIO":      (30,  "Continuous", 50),
    "PAV_EIXO":          (1,   "CENTER",     -3),
    "PAV_COTA_TRECHO":   (7,   "Continuous", -3),
    "PAV_COTA_LARGURA":  (7,   "Continuous", -3),
    "PAV_QUADRO":        (7,   "Continuous", -3),
}


# --------------------------------------------------------------------------- #
#  KML / KMZ
# --------------------------------------------------------------------------- #
def _tag(el) -> str:
    return el.tag.rsplit("}", 1)[-1] if isinstance(el.tag, str) else ""


def ler_kml_bytes(caminho: Path) -> bytes:
    if caminho.suffix.lower() == ".kmz":
        with zipfile.ZipFile(caminho) as z:
            kmls = [n for n in z.namelist() if n.lower().endswith(".kml")]
            if not kmls:
                raise ValueError("o KMZ não tem nenhum .kml dentro")
            return z.read(next((n for n in kmls if Path(n).name.lower() == "doc.kml"), kmls[0]))
    return caminho.read_bytes()


def _pts_coordinates(el):
    for c in el.iter():
        if _tag(c) == "coordinates":
            pts = []
            for tupla in re.sub(r",\s+", ",", (c.text or "").strip()).split():
                v = tupla.split(",")
                try:
                    pts.append((float(v[0]), float(v[1])))
                except (ValueError, IndexError):
                    pass
            return pts
    return []


def _pts_track(el):
    pts = []
    for c in el.iter():
        if _tag(c) == "coord":
            v = (c.text or "").split()
            try:
                pts.append((float(v[0]), float(v[1])))
            except (ValueError, IndexError):
                pass
    return pts


def extrair_eixos(kml: bytes):
    """[(nome, [(lon, lat), ...]), ...]  — uma entrada por LineString."""
    raiz = ET.fromstring(kml)
    eixos, sem_nome = [], 0
    for pm in raiz.iter():
        if _tag(pm) != "Placemark":
            continue
        nome = next(((f.text or "").strip() for f in pm if _tag(f) == "name"), "")
        partes = []
        for el in pm.iter():
            t = _tag(el)
            pts = _pts_coordinates(el) if t == "LineString" else _pts_track(el) if t == "Track" else None
            if pts and len(pts) >= 2:
                partes.append(pts)
        if not partes:
            continue
        if not nome:
            sem_nome += 1
            nome = f"Rua {sem_nome:02d}"
        for k, p in enumerate(partes):
            eixos.append((nome if len(partes) == 1 else f"{nome} ({k + 1})", p))
    return eixos


def epsg_utm(lon: float, lat: float) -> int:
    fuso = int((lon + 180) // 6) + 1
    if lat < 0:
        return 31960 + fuso if 17 <= fuso <= 25 else 32700 + fuso
    return 31954 + fuso if 11 <= fuso <= 22 else 32600 + fuso


def projetar(eixos_ll, epsg):
    todos = [p for _, pts in eixos_ll for p in pts]
    if epsg is None:
        epsg = epsg_utm(sum(p[0] for p in todos) / len(todos), sum(p[1] for p in todos) / len(todos))
    tr = Transformer.from_crs(4326, epsg, always_xy=True)
    saida = []
    for nome, pts in eixos_ll:
        xs, ys = tr.transform([p[0] for p in pts], [p[1] for p in pts])
        limpos = [(xs[0], ys[0])]
        for q in zip(xs[1:], ys[1:]):
            if math.dist(q, limpos[-1]) > 1e-3:
                limpos.append(q)
        if len(limpos) >= 2:
            saida.append((nome, LineString(limpos)))
    return epsg, saida


# --------------------------------------------------------------------------- #
#  Geometria
# --------------------------------------------------------------------------- #
def _pontos(geom):
    if geom.is_empty:
        return []
    if geom.geom_type == "Point":
        return [geom]
    if hasattr(geom, "geoms"):
        return [p for g in geom.geoms for p in _pontos(g)]
    if geom.geom_type in ("LineString", "LinearRing"):
        return [Point(c) for c in geom.coords]
    return []


def _ponta(ls: LineString, fim: bool):
    cs = list(ls.coords)
    a, b = (cs[-2], cs[-1]) if fim else (cs[1], cs[0])
    d = math.dist(a, b)
    return np.array(b), (np.array(b) - np.array(a)) / d


def _fechado(ls: LineString, tol=0.05) -> bool:
    return len(ls.coords) > 3 and math.dist(ls.coords[0], ls.coords[-1]) <= tol


def ajustar_pontas(linhas, tol):
    """Estica/apara cada ponta que está a menos de `tol` de outro eixo.
    Retorna (linhas ajustadas, lista de pontas livres [(ponto, direção)], log)."""
    linhas = list(linhas)
    log = []
    for i in range(len(linhas)):
        for fim in (False, True):
            ls = linhas[i]
            outros = [linhas[j] for j in range(len(linhas)) if j != i]
            if not outros or _fechado(ls):
                continue
            p, u = _ponta(ls, fim)
            P = Point(p)
            # 1) passou do outro eixo (sobra perto da ponta) -> apara no cruzamento
            melhor = None
            for o in outros:
                for q in _pontos(ls.intersection(o)):
                    s = ls.project(q)
                    sobra = (ls.length - s) if fim else s
                    if 1e-4 < sobra <= tol and (melhor is None or sobra < melhor[0]):
                        melhor = (sobra, s)
            if melhor is not None:
                sobra, s = melhor
                ls = substring(ls, 0, s) if fim else substring(ls, s, ls.length)
                linhas[i] = ls
                log.append(f"aparada {sobra:.2f} m")
                continue
            # 2) já encosta em outro eixo
            dmin = min(o.distance(P) for o in outros)
            if dmin <= 1e-3 or dmin > tol:
                continue
            # 3) não chega -> estica na direção da ponta; se não acertar, liga no ponto mais próximo
            raio = LineString([tuple(p), tuple(p + u * tol * 1.5)])
            alvos = [q for o in outros for q in _pontos(raio.intersection(o))]
            if alvos:
                q = min(alvos, key=lambda q: P.distance(q))
            else:
                o = min(outros, key=lambda o: o.distance(P))
                q = o.interpolate(o.project(P))
            cs = list(ls.coords)
            cs = cs + [(q.x, q.y)] if fim else [(q.x, q.y)] + cs
            linhas[i] = LineString(cs)
            log.append(f"esticada {P.distance(q):.2f} m")
    # pontas livres (não encostam em nenhum outro eixo)
    livres = []
    for i, ls in enumerate(linhas):
        outros = [linhas[j] for j in range(len(linhas)) if j != i]
        if _fechado(ls):          # rotatória / quadra fechada: não tem ponta
            continue
        for fim in (False, True):
            p, u = _ponta(ls, fim)
            if not outros or min(o.distance(Point(p)) for o in outros) > 1e-3:
                livres.append((p, u))
    return linhas, livres, log


def montar_pista(linhas, meia, raio):
    faixas = [ls.buffer(meia, cap_style="flat", join_style="mitre", mitre_limit=5.0) for ls in linhas]
    pista = unary_union(faixas)
    if raio > 0:   # fechamento morfológico: arredonda só os cantos internos (esquinas)
        pista = pista.buffer(raio, quad_segs=QS, join_style="round").buffer(
            -raio, quad_segs=QS, join_style="round")
    pista = pista.buffer(0)
    return [pista] if isinstance(pista, Polygon) else list(getattr(pista, "geoms", []))


def meios_fios(poligonos, pontas_livres, meia, tol=0.02):
    """Contorno da pista menos as bocas das pontas livres -> lista de LineStrings."""
    bocas = []
    for p, u in pontas_livres:
        n = np.array([-u[1], u[0]])
        bocas.append(LineString([tuple(p - n * meia), tuple(p + n * meia)]))

    def na_boca(a, b):
        for bo in bocas:
            if bo.distance(Point(a)) <= tol and bo.distance(Point(b)) <= tol:
                return True
        return False

    linhas = []
    for pol in poligonos:
        for anel in [pol.exterior, *pol.interiors]:
            cs = list(anel.coords)[:-1]
            n = len(cs)
            arestas = [(cs[k], cs[(k + 1) % n]) for k in range(n)]
            boca = [na_boca(a, b) for a, b in arestas]
            if not any(boca):
                linhas.append(LineString(cs + [cs[0]]))
                continue
            k0 = boca.index(True)            # começa logo depois de uma boca
            atual = []
            for k in range(1, n + 1):
                idx = (k0 + k) % n
                if boca[idx]:
                    if atual:
                        linhas.append(LineString(atual))
                    atual = []
                else:
                    a, b = arestas[idx]
                    if not atual:
                        atual = [a]
                    atual.append(b)
            if atual:
                linhas.append(LineString(atual))
    return [l for l in linhas if l.length > 1e-3]


def dividir_trechos(mf: LineString, eixos, meia, raio, passo=0.10, minimo=1.0, tol=0.03):
    """Divide o meio-fio em trechos, como nas pranchas:
    - cada lado reto pertence ao eixo do qual está a meia-largura;
    - o arco da esquina vai inteiro para a rua que TERMINA ali (a travessa);
    - se as duas ruas passam direto (cruzamento), o arco é dividido no meio."""
    L = mf.length
    n = max(int(L / passo), 2)
    ss = np.linspace(0, L, n + 1)
    pts = shapely.points([mf.interpolate(s).coords[0] for s in ss])
    dist = np.vstack([shapely.distance(pts, e) for e in eixos])
    perto = np.argmin(dist, axis=0)
    dono = np.where(np.abs(dist[perto, np.arange(len(ss))] - meia) <= tol, perto, -1)
    reto = dono != -1                     # amostras no lado reto (para posicionar o texto)
    original = dono.copy()

    def vizinho(i, passo_i):
        """primeiro dono definido andando a partir de i (dá a volta se for anel)."""
        for _ in range(len(original)):
            if 0 <= i < len(original):
                if original[i] != -1:
                    return original[i]
            elif mf.is_ring:
                i %= len(original)
                continue
            else:
                return -1
            i += passo_i
        return -1

    # trechos de arco (dono = -1): decide pela rua que termina na esquina
    pontas = [(np.array(e.coords[0]), np.array(e.coords[-1])) for e in eixos]
    k = 0
    while k < len(dono):
        if dono[k] != -1:
            k += 1
            continue
        j = k
        while j < len(dono) and dono[j] == -1:
            j += 1
        A = vizinho(k - 1, -1)
        B = vizinho(j, +1)
        if A == -1 or B == -1 or A == B:
            dono[k:j] = A if A != -1 else B
        else:
            meio = np.array(pts[(k + j) // 2].coords[0])
            dA = min(np.linalg.norm(meio - q) for q in pontas[A])
            dB = min(np.linalg.norm(meio - q) for q in pontas[B])
            alcance = meia + raio + 2.0
            if min(dA, dB) <= alcance and abs(dA - dB) > 1.0:
                dono[k:j] = A if dA < dB else B
            else:
                dono[k:(k + j) // 2] = A
                dono[(k + j) // 2:j] = B
        k = j
    if (dono == -1).all():
        dono[:] = perto
    # pedaços [início, fim, eixo dono]
    pedacos = []
    ini = 0.0
    for k in range(1, len(ss)):
        if dono[k] != dono[k - 1]:
            corte = (ss[k] + ss[k - 1]) / 2
            pedacos.append([ini, corte, int(dono[k - 1])])
            ini = corte
    pedacos.append([ini, L, int(dono[-1])])

    def juntar_iguais(pp):
        saida = [pp[0]]
        for p in pp[1:]:
            if p[2] == saida[-1][2]:
                saida[-1][1] = p[1]
            else:
                saida.append(p)
        return saida

    # pedaço curto (empate de distância, ruído) vai para o vizinho; depois junta vizinhos da mesma rua
    mudou = True
    while mudou and len(pedacos) > 1:
        mudou = False
        for k, (a, b, _) in enumerate(pedacos):
            if b - a < minimo:
                ant = pedacos[k - 1] if k > 0 else None
                prox = pedacos[k + 1] if k + 1 < len(pedacos) else None
                if prox is None or (ant is not None and (ant[1] - ant[0]) >= (prox[1] - prox[0])):
                    ant[1] = b
                else:
                    prox[0] = a
                pedacos.pop(k)
                pedacos = juntar_iguais(pedacos)
                mudou = True
                break
    pedacos = juntar_iguais(pedacos)
    if mf.is_ring and len(pedacos) > 1 and pedacos[0][2] == pedacos[-1][2]:
        # num anel o ponto inicial é arbitrário: primeiro e último pedaço da mesma rua são um trecho só
        b0 = pedacos[0][1]
        pedacos = pedacos[1:-1] + [[pedacos[-1][0], L + b0, pedacos[-1][2]]]
    saida = []
    for a, b, _ in pedacos:
        if b <= L + 1e-9:
            seg = substring(mf, a, min(b, L))
        else:   # trecho que passa pelo início do anel
            c1 = list(substring(mf, a, L).coords)
            c2 = list(substring(mf, 0, b - L).coords)
            seg = LineString(c1 + c2[1:])
        # posição do texto: meio da maior parte reta do trecho (fora do arco da esquina)
        est = np.arange(a, b, passo)
        idx = np.clip(np.round((est % L) / (L / n)).astype(int), 0, len(reto) - 1)
        r = reto[idx]
        melhor, ini = (0, 0, len(est)), None
        for q, v in enumerate(list(r) + [False]):
            if v and ini is None:
                ini = q
            elif not v and ini is not None:
                if q - ini > melhor[0]:
                    melhor = (q - ini, ini, q)
                ini = None
        s_txt = ((melhor[1] + melhor[2]) / 2 * passo) if melhor[0] > 0 else seg.length / 2
        saida.append((seg, min(max(s_txt, 0.0), seg.length)))
    return saida


# --------------------------------------------------------------------------- #
#  Desenho
# --------------------------------------------------------------------------- #
def _br(v: float, casas: int = 2) -> str:
    s = f"{v:,.{casas}f}"
    return s.replace(",", "X").replace(".", ",").replace("X", ".")


def _rot_legivel(graus: float) -> float:
    g = graus % 360
    return g - 180 if 90 < g <= 270 else g


def novo_desenho(epsg, altura, largura):
    doc = ezdxf.new("R2018", setup=True)
    doc.units = ezdxf.units.M
    doc.header["$INSUNITS"] = 6
    doc.header["$MEASUREMENT"] = 1
    doc.header["$LWDISPLAY"] = 1
    doc.header.custom_vars.append("SRC", f"EPSG:{epsg}")
    if "ARIAL" not in doc.styles:
        doc.styles.add("ARIAL", font="arial.ttf")
    for nome, (cor, lt, lw) in LAYERS.items():
        doc.layers.add(nome, color=cor, linetype=lt if lt in doc.linetypes else "Continuous", lineweight=lw)
    ds = doc.dimstyles.new("COTA_RUA")
    hd = min(altura * 0.8, largura / 5.0)      # o texto "7,00 m" tem que caber dentro da rua
    ds.dxf.dimtxsty = "ARIAL"
    ds.dxf.dimtxt = hd
    ds.dxf.dimasz = hd * 0.6
    ds.dxf.dimexe = hd * 0.3
    ds.dxf.dimexo = 0.0
    ds.dxf.dimgap = hd * 0.25
    ds.dxf.dimdec = 2
    ds.dxf.dimdsep = ord(",")
    ds.dxf.dimpost = "<> m"
    ds.dxf.dimtad = 1
    ds.dxf.dimtih = 0
    ds.dxf.dimtoh = 0
    ds.dxf.dimlunit = 2
    ds.dxf.dimzin = 0          # mostra 7,00 (não suprime zeros)
    ds.dxf.dimtfill = 1        # fundo do texto com a cor de fundo (legível sobre a hachura)
    return doc


def _lado_de_fora(pista, p, n, d=0.3):
    """devolve n apontando para fora da pista."""
    return n if not pista.covers(Point(p + n * d)) else -n


def desenhar(doc, nomes, eixos, poligonos, mfs_trechos, meia, altura, cota_largura):
    msp = doc.modelspace()
    pista = unary_union(poligonos)
    txt = {"style": "ARIAL"}

    # hachura
    for pol in poligonos:
        h = msp.add_hatch(dxfattribs={"layer": "PAV_HACHURA"})
        h.set_solid_fill(color=8, rgb=COR_PAV)
        h.paths.add_polyline_path(list(pol.exterior.coords)[:-1], is_closed=True,
                                  flags=dxfconst.BOUNDARY_PATH_EXTERNAL)
        for anel in pol.interiors:
            h.paths.add_polyline_path(list(anel.coords)[:-1], is_closed=True,
                                      flags=dxfconst.BOUNDARY_PATH_OUTERMOST)
        h.dxf.hatch_style = 0
        try:
            h.set_seed_points([pol.representative_point().coords[0]])
        except Exception:
            pass
    # leva a hachura para trás de tudo
    try:
        msp.set_redraw_order([(h.dxf.handle, f"{i + 1:X}") for i, h in enumerate(msp.query("HATCH"))])
    except Exception:
        pass

    # eixos
    for ls in eixos:
        msp.add_lwpolyline(list(ls.coords), dxfattribs={"layer": "PAV_EIXO"})

    # meio-fio + comprimento de cada trecho
    linhas_csv = []
    for i_mf, (mf, trechos) in enumerate(mfs_trechos, 1):
        msp.add_lwpolyline(list(mf.coords)[:-1] if mf.is_ring else list(mf.coords), close=mf.is_ring,
                           dxfattribs={"layer": "PAV_MEIO-FIO"})
        for seg, s_txt in trechos:
            L = seg.length
            p = np.array(seg.interpolate(s_txt).coords[0])
            a = np.array(seg.interpolate(max(s_txt - 0.5, 0)).coords[0])
            b = np.array(seg.interpolate(min(s_txt + 0.5, L)).coords[0])
            t = (b - a) / (np.linalg.norm(b - a) or 1)
            nrm = _lado_de_fora(pista, p, np.array([-t[1], t[0]]))
            pos = p + nrm * (altura * 0.35 + altura / 2)
            rot = _rot_legivel(math.degrees(math.atan2(t[1], t[0])))
            msp.add_text(f"{_br(L)} m", height=altura, rotation=rot,
                         dxfattribs={**txt, "layer": "PAV_COTA_TRECHO"}).set_placement(
                tuple(pos), align=TextEntityAlignment.MIDDLE_CENTER)
            j = int(np.argmin([e.distance(Point(p)) for e in eixos]))
            ini, fim = seg.coords[0], seg.coords[-1]
            linhas_csv.append([len(linhas_csv) + 1, i_mf, nomes[j], L, ini[0], ini[1], fim[0], fim[1]])

    # cota da largura: no ponto mais perto do meio do eixo onde a seção é só a rua
    # (os dois lados caem no meio-fio e não há outra rua por perto)
    if cota_largura:
        borda = unary_union([pol.boundary for pol in poligonos])
        for i, ls in enumerate(eixos):
            if ls.length < 4 * meia:
                continue
            outros = [e for j, e in enumerate(eixos) if j != i]
            L, meio = ls.length, ls.length / 2
            for k in range(0, int(L / 2.0) + 1):
                achou = False
                for s in ((meio + k * 2.0, meio - k * 2.0) if k else (meio,)):
                    if not (meia < s < L - meia):
                        continue
                    p = np.array(ls.interpolate(s).coords[0])
                    a = np.array(ls.interpolate(max(s - 0.5, 0)).coords[0])
                    b = np.array(ls.interpolate(min(s + 0.5, L)).coords[0])
                    t = (b - a) / np.linalg.norm(b - a)
                    n = np.array([-t[1], t[0]])
                    p1, p2 = p + n * meia, p - n * meia
                    if borda.distance(Point(p1)) > 0.03 or borda.distance(Point(p2)) > 0.03:
                        continue
                    if outros and min(o.distance(Point(p)) for o in outros) < meia + altura * 3:
                        continue
                    ang = math.degrees(math.atan2(*(p2 - p1)[::-1])) % 360
                    if 90 < ang <= 270:          # mantém o texto da cota de pé
                        p1, p2 = p2, p1
                    dim = msp.add_aligned_dim(p1=tuple(p1), p2=tuple(p2), distance=0.0, dimstyle="COTA_RUA",
                                              dxfattribs={"layer": "PAV_COTA_LARGURA"})
                    dim.render()
                    achou = True
                    break
                if achou:
                    break
    return linhas_csv


def quadro(msp, x0, y0, altura, area, mf_total):
    linhas = [("PAVIMENTO", f"{_br(area)}", "m²"),
              ("MEIO-FIO", f"{_br(mf_total)}", "m"),
              ("SARJETA", f"{_br(mf_total)}", "m")]
    h = altura * 2.0
    larg = [altura * 16, altura * 10, altura * 4]
    W = sum(larg)
    lay = {"layer": "PAV_QUADRO"}
    n = len(linhas) + 2
    for k in range(n + 1):
        msp.add_line((x0, y0 - k * h), (x0 + W, y0 - k * h), dxfattribs=lay)
    msp.add_line((x0, y0), (x0, y0 - n * h), dxfattribs=lay)
    msp.add_line((x0 + W, y0), (x0 + W, y0 - n * h), dxfattribs=lay)
    for xk in (larg[0], larg[0] + larg[1]):
        msp.add_line((x0 + xk, y0 - h), (x0 + xk, y0 - n * h), dxfattribs=lay)
    att = {"style": "ARIAL", "layer": "PAV_QUADRO"}
    msp.add_text("PAVIMENTAÇÃO", height=altura * 1.1, dxfattribs=att).set_placement(
        (x0 + W / 2, y0 - h / 2), align=TextEntityAlignment.MIDDLE_CENTER)
    cab = ("SERVIÇOS", "QUANTIDADES", "UND.")
    xs = [x0, x0 + larg[0], x0 + larg[0] + larg[1], x0 + W]
    for c in range(3):
        msp.add_text(cab[c], height=altura, dxfattribs=att).set_placement(
            ((xs[c] + xs[c + 1]) / 2, y0 - 1.5 * h), align=TextEntityAlignment.MIDDLE_CENTER)
    for r, lin in enumerate(linhas):
        y = y0 - (2.5 + r) * h
        msp.add_text(lin[0], height=altura, dxfattribs=att).set_placement(
            ((xs[0] + xs[1]) / 2, y), align=TextEntityAlignment.MIDDLE_CENTER)
        msp.add_text(lin[1], height=altura, dxfattribs=att).set_placement(
            (xs[2] - altura * 0.6, y), align=TextEntityAlignment.MIDDLE_RIGHT)
        msp.add_text(lin[2], height=altura, dxfattribs=att).set_placement(
            ((xs[2] + xs[3]) / 2, y), align=TextEntityAlignment.MIDDLE_CENTER)


# --------------------------------------------------------------------------- #
#  DXF -> DWG
# --------------------------------------------------------------------------- #
def achar_oda(informado):
    if informado:
        p = Path(informado)
        return p if p.exists() else None
    cand = []
    if os.name == "nt":
        for base in (os.environ.get("ProgramFiles", r"C:\Program Files"),
                     os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")):
            cand += sorted(Path(base, "ODA").glob("ODAFileConverter*/ODAFileConverter.exe"), reverse=True)
    elif sys.platform == "darwin":
        cand.append(Path("/Applications/ODAFileConverter.app/Contents/MacOS/ODAFileConverter"))
    else:
        w = shutil.which("ODAFileConverter")
        if w:
            cand.append(Path(w))
    return next((c for c in cand if c.exists()), None)


def para_dwg(oda, dxf: Path, destino: Path, versao):
    tmp_in, tmp_out = Path(tempfile.mkdtemp()), Path(tempfile.mkdtemp())
    shutil.copy2(dxf, tmp_in / dxf.name)
    cmd = [str(oda), str(tmp_in), str(tmp_out), versao, "DWG", "0", "1", "*.dxf"]
    kw = {"capture_output": True, "timeout": 600}
    if os.name == "nt":
        si = subprocess.STARTUPINFO()
        si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        kw["startupinfo"] = si
    elif sys.platform.startswith("linux") and not os.environ.get("DISPLAY") and shutil.which("xvfb-run"):
        cmd = ["xvfb-run", "-a"] + cmd
    subprocess.run(cmd, check=False, **kw)
    gerado = tmp_out / (dxf.stem + ".dwg")
    ok = gerado.exists()
    if ok:
        shutil.move(str(gerado), destino)
    shutil.rmtree(tmp_in, ignore_errors=True)
    shutil.rmtree(tmp_out, ignore_errors=True)
    return ok


# --------------------------------------------------------------------------- #
#  Principal
# --------------------------------------------------------------------------- #
def escolher_arquivo_gui():
    try:
        import tkinter as tk
        from tkinter import filedialog
        root = tk.Tk()
        root.withdraw()
        c = filedialog.askopenfilename(title="KML/KMZ com os eixos das ruas",
                                       filetypes=[("Google Earth", "*.kml *.kmz"), ("Todos", "*.*")])
        root.destroy()
        return c or None
    except Exception:
        return None


def main(argv=None):
    ap = argparse.ArgumentParser(description="KML/KMZ de eixos -> pavimento hachurado com meio-fio e cotas.")
    ap.add_argument("entrada", nargs="?", help=".kml ou .kmz (vazio = abre janela)")
    ap.add_argument("-o", "--saida", help="pasta de saída (padrão: pasta do KML)")
    ap.add_argument("--largura", type=float, default=7.0, help="largura total da pista em m (padrão 7 = 3,50 p/ lado)")
    ap.add_argument("--raio", type=float, default=3.0, help="raio de concordância nas esquinas em m (padrão 3, como na prancha; 0 = canto vivo)")
    ap.add_argument("--tol-ponta", type=float, default=8.0,
                    help="distância máx. para esticar/aparar a ponta de um eixo até outro (padrão 8 m)")
    ap.add_argument("--altura-texto", type=float, default=1.5, help="altura dos textos em m (padrão 1,5, como na prancha)")
    ap.add_argument("--sem-cota-largura", action="store_true", help="não cota a largura das ruas")
    ap.add_argument("--epsg", type=int, help="força o sistema (ex.: 31983 = SIRGAS 2000 / UTM 23S)")
    ap.add_argument("--versao", default="ACAD2018", choices=VERSOES_DWG)
    ap.add_argument("--oda", help="caminho do ODAFileConverter")
    ap.add_argument("--so-dxf", action="store_true", help="não converte para DWG")
    a = ap.parse_args(argv)

    entrada = a.entrada or escolher_arquivo_gui()
    if not entrada:
        ap.print_help()
        return 1
    entrada = Path(entrada)
    if not entrada.exists():
        print(f"Arquivo não encontrado: {entrada}")
        return 1
    saida = Path(a.saida) if a.saida else entrada.parent
    saida.mkdir(parents=True, exist_ok=True)
    meia = a.largura / 2

    eixos_ll = extrair_eixos(ler_kml_bytes(entrada))
    if not eixos_ll:
        print("Nenhuma linha (LineString) no arquivo.")
        return 1
    epsg, eixos = projetar(eixos_ll, a.epsg)
    nomes = [n for n, _ in eixos]
    linhas, livres, log = ajustar_pontas([ls for _, ls in eixos], a.tol_ponta)
    print(f"{len(linhas)} eixo(s) em {entrada.name}  |  SRC EPSG:{epsg}  |  largura {_br(a.largura)} m"
          f"  |  raio {_br(a.raio)} m")
    if log:
        print(f"  pontas ajustadas: {len(log)} ({'; '.join(log[:6])}{' ...' if len(log) > 6 else ''})")

    poligonos = montar_pista(linhas, meia, a.raio)
    mfs = meios_fios(poligonos, livres, meia)
    mfs_trechos = [(mf, dividir_trechos(mf, linhas, meia, a.raio)) for mf in mfs]

    doc = novo_desenho(epsg, a.altura_texto, a.largura)
    linhas_csv = desenhar(doc, nomes, linhas, poligonos, mfs_trechos, meia, a.altura_texto,
                          not a.sem_cota_largura)
    area = sum(p.area for p in poligonos)
    mf_total = sum(mf.length for mf in mfs)
    xmin, ymin, xmax, ymax = unary_union(poligonos).bounds
    quadro(doc.modelspace(), xmin, ymin - a.altura_texto * 6, a.altura_texto, area, mf_total)
    zoom.extents(doc.modelspace(), factor=1.05)

    base = f"{entrada.stem}_pavimento"
    dxf = saida / f"{base}.dxf"
    doc.saveas(dxf)
    final = dxf
    if not a.so_dxf:
        oda = achar_oda(a.oda)
        if oda and para_dwg(oda, dxf, saida / f"{base}.dwg", a.versao):
            dxf.unlink()
            final = saida / f"{base}.dwg"
        elif oda is None:
            print("  ODA File Converter não encontrado: saída em DXF (abre direto no Civil 3D).")
        else:
            print("  O ODA não converteu; ficou o DXF.")

    with open(saida / f"{entrada.stem}_trechos.csv", "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f, delimiter=";")
        w.writerow(["Trecho", "Meio-fio", "Rua mais próxima", "Comprimento (m)", "Início E", "Início N", "Fim E", "Fim N"])
        for l in linhas_csv:
            w.writerow(l[:3] + [_br(l[3])] + [_br(v, 3) for v in l[4:]])

    print(f"  pavimento {_br(area)} m²  |  meio-fio {_br(mf_total)} m em {len(linhas_csv)} trecho(s)")
    print(f"Pronto: {final}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
