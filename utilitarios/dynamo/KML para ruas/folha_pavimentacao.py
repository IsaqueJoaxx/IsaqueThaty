#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
folha_pavimentacao.py
=====================
Monta a FOLHA (layout de impressão) de um desenho de ruas em DXF/DWG:

  * formato ABNT (A3/A2/A1/A0) e escala escolhidos sozinhos para caber a planta;
  * janela (viewport) travada na escala, com a planta centralizada;
  * malha de coordenadas UTM (linhas finas + rótulos E= / N=);
  * norte (NQ), datum, escala numérica e escala gráfica;
  * legenda, quadro de quantidades (MEIO-FIO m, SARJETA m, PAVIMENTO ASFÁLTICO m²),
    notas, quadro de revisões e carimbo;
  * acabamento do modelo para a escala: textos dos trechos, nome da rua e cotas
    redimensionados para o papel, cores/espessuras de impressão nos layers da via.

Os quantitativos saem da geometria do desenho:
  - MEIO-FIO = contorno da pista (layer de bordas/meio-fio) descontando as bocas nas
    pontas da via (bordas que passam pelas pontas do eixo);
  - SARJETA  = mesma extensão do meio-fio;
  - PAVIMENTO = área da hachura da pista (ou da área fechada pelas bordas).

Serve para os desenhos gerados por kml_para_pavimento.py (layers PAV_*) e por
rotinas com layers RUA_EIXO / RUA_BORDAS / RUA_PAVIMENTO; outros nomes podem ser
passados por --layer-meiofio, --layer-eixo e --layer-pavimento.

Uso:
  python folha_pavimentacao.py ruas.dxf
  python folha_pavimentacao.py ruas.dxf --rua "Rua Amazonita" --local "Várzea do Mainho"
  python folha_pavimentacao.py ruas.dxf --folha A1 --escala 750 --empresa "MINHA EMPRESA"
  python folha_pavimentacao.py               (abre janela para escolher o arquivo)

Saída (na pasta do arquivo): <nome>_folha.dxf (ou .dwg com ODA File Converter)
                             <nome>_folha.pdf  (conferência, se houver PyMuPDF/matplotlib)
Requisitos: pip install ezdxf shapely   (opcional: pymupdf para o PDF)
"""
from __future__ import annotations

import argparse
import datetime as _dt
import math
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

try:
    import ezdxf
    import ezdxf.path as dxfpath
    from ezdxf import bbox, colors
    from ezdxf.enums import MTextEntityAlignment, TextEntityAlignment
    from ezdxf.lldxf import const
    from shapely.geometry import LineString, Point, Polygon
    from shapely.ops import unary_union
except ImportError as e:  # pragma: no cover
    _msg = (f"Falta a biblioteca '{e.name}' no Python que abriu este script:\n{sys.executable}\n\n"
            f"Instale com:\n  \"{sys.executable}\" -m pip install ezdxf shapely pyproj pymupdf")
    try:
        import tkinter as _tk
        from tkinter import messagebox as _mb
        _r = _tk.Tk(); _r.withdraw(); _mb.showerror("Folha de pavimentação", _msg); _r.destroy()
    except Exception:
        print(_msg)
        input("\nPressione Enter para fechar...")
    sys.exit(1)

# --------------------------------------------------------------------------- #
#  Parâmetros de folha (ABNT NBR 10068 / 10582)
# --------------------------------------------------------------------------- #
FORMATOS = {"A3": (420, 297), "A2": (594, 420), "A1": (841, 594), "A0": (1189, 841)}
MARGEM = {"A3": 7, "A2": 7, "A1": 10, "A0": 10}
MARGEM_ESQ = 25
COLUNA = {"A3": 160, "A2": 175, "A1": 180, "A0": 180}
PAPEL_PC3 = {f: f"ISO_full_bleed_{f}" for f in FORMATOS}      # o ezdxf completa "_(594.00_x_420.00_MM)"
ESCALAS = [100, 200, 250, 500, 750, 1000, 1250, 1500, 2000, 2500, 3000, 5000, 7500, 10000, 20000]
ESC_LEGIVEL = 1000            # preferir a menor folha que caiba até 1:1000

COR_FUNDO = (232, 232, 232)   # faixas de título
COR_TXT_SEC = 8               # rótulos dos campos (cinza)
NOMES_PADRAO_KML = {"caminho sem título", "caminho sem titulo", "untitled path", "untitled placemark"}

LAYERS_FOLHA = {
    #  nome              cor  espessura  plota
    "FOLHA_MOLDURA":    (7,   50, True),
    "FOLHA_LINHAS":     (7,   25, True),
    "FOLHA_FINAS":      (7,   13, True),
    "FOLHA_FUNDO":      (9,    0, True),
    "FOLHA_TEXTO":      (7,   18, True),
    "FOLHA_MALHA":      (252,  9, True),
    "FOLHA_MALHA_TXT":  (8,   13, True),
    "FOLHA_SIMBOLOS":   (7,   18, True),
    "FOLHA_VIEWPORT":   (6,    0, False),
}


# --------------------------------------------------------------------------- #
#  Utilidades
# --------------------------------------------------------------------------- #
def br(v: float, casas: int = 2) -> str:
    s = f"{v:,.{casas}f}"
    return s.replace(",", "X").replace(".", ",").replace("X", ".")


def br_int(v: float) -> str:
    return f"{int(round(v)):,d}".replace(",", ".")


def _norm(s: str) -> str:
    return s.upper().replace("-", "_").replace(" ", "_")


def _pts(entity, tol=0.01):
    try:
        return [(v.x, v.y) for v in dxfpath.make_path(entity).flattening(tol)]
    except Exception:
        return []


def _fechada(entity) -> bool:
    t = entity.dxftype()
    if t == "LWPOLYLINE":
        return bool(entity.closed)
    if t == "POLYLINE":
        return bool(entity.is_closed)
    if t == "CIRCLE":
        return True
    return False


# --------------------------------------------------------------------------- #
#  Leitura e detecção dos layers
# --------------------------------------------------------------------------- #
def abrir(caminho: Path, oda: str | None):
    if caminho.suffix.lower() == ".dxf":
        return ezdxf.readfile(caminho)
    if caminho.suffix.lower() != ".dwg":
        raise ValueError("use um arquivo .dxf ou .dwg")
    exe = achar_oda(oda)
    if exe is None:
        raise RuntimeError("para abrir DWG é preciso o ODA File Converter (ou salve como DXF no Civil 3D)")
    ent, sai = Path(tempfile.mkdtemp()), Path(tempfile.mkdtemp())
    shutil.copy2(caminho, ent / caminho.name)
    _rodar_oda(exe, ent, sai, "ACAD2018", "DXF")
    dxf = sai / (caminho.stem + ".dxf")
    if not dxf.exists():
        raise RuntimeError("o ODA não converteu o DWG")
    return ezdxf.readfile(dxf)


def detectar(doc, args):
    msp = doc.modelspace()
    por_layer: dict[str, list] = {}
    for e in msp:
        por_layer.setdefault(e.dxf.layer, []).append(e)

    def tem(lay, tipos):
        return any(e.dxftype() in tipos for e in por_layer.get(lay, []))

    linhas = ("LWPOLYLINE", "POLYLINE", "LINE", "ARC", "SPLINE")

    def escolher(forcado, chaves, tipos):
        if forcado:
            return [l for l in por_layer if _norm(l) in {_norm(x) for x in forcado.split(",")}]
        return [l for l in por_layer if any(k in _norm(l) for k in chaves) and tem(l, tipos)]

    d = {
        "meiofio": escolher(args.layer_meiofio, ("MEIO_FIO", "MEIOFIO", "BORDA", "GUIA", "SARJETA", "MEIO-FIO"), linhas),
        "eixo": escolher(args.layer_eixo, ("EIXO",), linhas),
        "pavimento": escolher(args.layer_pavimento, ("PAV", "HACH", "ASFALT", "PISTA"), ("HATCH",)),
        "ignorar": [l for l in por_layer if "QUADRO" in _norm(l) or "CARIMBO" in _norm(l)],
    }
    d["meiofio"] = [l for l in d["meiofio"] if l not in d["eixo"]]
    return d


# --------------------------------------------------------------------------- #
#  Quantitativos
# --------------------------------------------------------------------------- #
def _hatch_poligonos(h):
    """Polígonos da hachura respeitando furos (regra par/ímpar)."""
    geo = None
    for p in dxfpath.from_hatch(h):
        cs = [(v.x, v.y) for v in p.flattening(0.01)]
        if len(cs) < 3:
            continue
        pol = Polygon(cs).buffer(0)
        geo = pol if geo is None else geo.symmetric_difference(pol)
    return geo


def quantitativos(doc, lay):
    msp = doc.modelspace()
    # eixos e suas pontas
    eixos = []
    for e in msp:
        if e.dxf.layer in lay["eixo"] and e.dxftype() in ("LWPOLYLINE", "POLYLINE", "LINE", "ARC", "SPLINE"):
            cs = _pts(e)
            if len(cs) >= 2:
                eixos.append(LineString(cs))
    pontas = []
    for ls in eixos:
        if ls.coords[0] != ls.coords[-1]:
            pontas += [Point(ls.coords[0]), Point(ls.coords[-1])]

    meio_fio, bocas, contornos = 0.0, 0, []
    for e in msp:
        if e.dxf.layer not in lay["meiofio"]:
            continue
        cs = _pts(e)
        if len(cs) < 2:
            continue
        if _fechada(e):
            if cs[0] != cs[-1]:
                cs = cs + [cs[0]]
            contornos.append(Polygon(cs).buffer(0))
            for a, b in zip(cs, cs[1:]):
                seg = LineString([a, b])
                if pontas and min(p.distance(seg) for p in pontas) <= 0.15:
                    bocas += 1          # boca na ponta da via: não leva meio-fio
                    continue
                meio_fio += seg.length
        else:
            meio_fio += LineString(cs).length

    area_geo = None
    for h in msp.query("HATCH"):
        if h.dxf.layer in lay["pavimento"]:
            g = _hatch_poligonos(h)
            if g is not None and not g.is_empty:
                area_geo = g if area_geo is None else area_geo.union(g)
    origem_area = "hachura"
    if area_geo is None and contornos:
        area_geo, origem_area = unary_union(contornos), "contorno"
    area = area_geo.area if area_geo is not None else 0.0

    larguras = []
    for d in msp.query("DIMENSION"):
        try:
            larguras.append(round(d.get_measurement(), 2))
        except Exception:
            pass
    ext_eixo = sum(ls.length for ls in eixos)
    largura = max(set(larguras), key=larguras.count) if larguras else (area / ext_eixo if ext_eixo else 0)
    return {"meio_fio": meio_fio, "sarjeta": meio_fio, "area": area, "eixo": ext_eixo, "largura": largura,
            "bocas": bocas, "origem_area": origem_area, "pista": area_geo, "eixos": eixos}


# --------------------------------------------------------------------------- #
#  Escolha de formato e escala
# --------------------------------------------------------------------------- #
def area_mapa(formato):
    W, H = FORMATOS[formato]
    m = MARGEM[formato]
    x0, x1 = MARGEM_ESQ + 6, W - m - COLUNA[formato] - 6
    y0, y1 = m + 6, H - m - 6
    return x0, y0, x1, y1


def escolher_folha(w_m, h_m, formato, escala):
    def melhor_escala(f):
        x0, y0, x1, y1 = area_mapa(f)
        precisa = max(w_m * 1.08 * 1000 / (x1 - x0), h_m * 1.08 * 1000 / (y1 - y0))
        return next((e for e in ESCALAS if e >= precisa), None)

    if formato and escala:
        return formato, escala
    if formato:
        return formato, melhor_escala(formato) or ESCALAS[-1]
    if escala:
        for f in FORMATOS:
            x0, y0, x1, y1 = area_mapa(f)
            if w_m * 1000 / escala <= (x1 - x0) and h_m * 1000 / escala <= (y1 - y0):
                return f, escala
        return "A0", escala
    for f in FORMATOS:                       # menor folha que caiba até 1:1000
        e = melhor_escala(f)
        if e and e <= ESC_LEGIVEL:
            return f, e
    return "A1", melhor_escala("A1") or ESCALAS[-1]


# --------------------------------------------------------------------------- #
#  Acabamento do modelo (textos, cotas, layers) para a escala da folha
# --------------------------------------------------------------------------- #
RE_COMPR = re.compile(r"^\s*\d{1,3}(\.\d{3})*,\d{1,3}\s*m\s*$")


def _ler_estilos(doc):
    if "FOLHA_ARIAL" not in doc.styles:
        doc.styles.add("FOLHA_ARIAL", font="arial.ttf")
    if "FOLHA_ARIAL_N" not in doc.styles:
        doc.styles.add("FOLHA_ARIAL_N", font="arialbd.ttf")


def _rot_legivel(graus):
    g = graus % 360
    return g - 180 if 90 < g <= 270 else g


def acabamento_modelo(doc, lay, q, esc, nome_rua, log):
    msp = doc.modelspace()
    k = esc / 1000.0                       # m de modelo por mm de papel
    pista = q["pista"]
    borda = pista.boundary if pista is not None else None

    # layers da via: cores e espessuras de impressão
    if "DASHDOT" not in doc.linetypes:
        doc.linetypes.add("DASHDOT", pattern=[7.0, 5.0, -1.0, 0.0, -1.0], description="Traço ponto __ . __ .")
    for l in lay["meiofio"]:
        L = doc.layers.get(l); L.color = 30; L.dxf.lineweight = 35
    for l in lay["eixo"]:
        L = doc.layers.get(l); L.color = 1; L.dxf.lineweight = 18; L.dxf.linetype = "DASHDOT"
    for l in lay["pavimento"]:
        doc.layers.get(l).dxf.lineweight = 0

    # contorno fechado com bocas -> trechos abertos (as pontas da via ficam sem meio-fio, como no quantitativo)
    pontas = [Point(ls.coords[0]) for ls in q["eixos"] if ls.coords[0] != ls.coords[-1]] + \
             [Point(ls.coords[-1]) for ls in q["eixos"] if ls.coords[0] != ls.coords[-1]]
    abertas = 0
    for e in list(msp):
        if e.dxf.layer not in lay["meiofio"] or not _fechada(e) or not pontas:
            continue
        cs = _pts(e)
        if cs[0] != cs[-1]:
            cs = cs + [cs[0]]
        arestas = list(zip(cs, cs[1:]))
        boca = [min(pp.distance(LineString([a, b])) for pp in pontas) <= 0.15 for a, b in arestas]
        if not any(boca):
            continue
        k0 = boca.index(True)
        n = len(arestas)
        corridas, atual = [], []
        for i in range(1, n + 1):
            j = (k0 + i) % n
            if boca[j]:
                if atual:
                    corridas.append(atual)
                atual = []
            else:
                a, b = arestas[j]
                atual = atual or [a]
                atual.append(b)
        if atual:
            corridas.append(atual)
        at = {"layer": e.dxf.layer}
        for nome_at in ("color", "lineweight", "linetype"):
            if e.dxf.hasattr(nome_at):
                at[nome_at] = e.dxf.get(nome_at)
        for c in corridas:
            msp.add_lwpolyline(c, dxfattribs=at)
        msp.delete_entity(e)
        abertas += 1
    if abertas:
        log.append(f"{abertas} contorno(s) de meio-fio aberto(s) nas pontas da via (bocas sem meio-fio)")

    # textos de comprimento: 2,0 mm no papel, do lado de fora do meio-fio
    h_txt = 2.0 * k
    n_ajust = 0
    for t in list(msp.query("TEXT MTEXT")):
        s = t.dxf.text if t.dxftype() == "TEXT" else t.plain_text()
        ins = t.dxf.insert
        if s.strip().lower() in NOMES_PADRAO_KML and nome_rua:
            s = nome_rua
            if t.dxftype() == "TEXT":
                t.dxf.text = s
            else:
                t.text = s
        if RE_COMPR.match(s) and borda is not None and t.dxftype() == "TEXT":
            P = Point(ins.x, ins.y)
            sb = borda.project(P)
            p = borda.interpolate(sb)
            a = borda.interpolate(max(sb - 0.5, 0)); b = borda.interpolate(min(sb + 0.5, borda.length))
            tx, ty = b.x - a.x, b.y - a.y
            n = math.hypot(tx, ty) or 1
            tx, ty = tx / n, ty / n
            nx, ny = -ty, tx
            if pista.covers(Point(p.x + nx * 0.3, p.y + ny * 0.3)):
                nx, ny = -nx, -ny
            d = 0.45 * h_txt + h_txt / 2
            t.dxf.height = h_txt
            t.dxf.style = "FOLHA_ARIAL"
            t.dxf.rotation = _rot_legivel(math.degrees(math.atan2(ty, tx)))
            t.dxf.width = 1.0
            t.set_placement((p.x + nx * d, p.y + ny * d), align=TextEntityAlignment.MIDDLE_CENTER)
            n_ajust += 1
        elif t.dxftype() == "TEXT" and pista is not None and pista.buffer(1.0).covers(Point(ins.x, ins.y)) \
                and not RE_COMPR.match(s):
            # nome da rua dentro da pista: branco, no meio do trecho mais longo do eixo
            h_nome = min(2.5 * k, 0.42 * q["largura"]) if q["largura"] else 2.5 * k
            comp_txt = len(s) * 0.62 * h_nome
            obst = []
            for d_ in msp.query("DIMENSION"):
                try:
                    a_, b_ = d_.dxf.defpoint2, d_.dxf.defpoint3
                    obst.append(Point((a_.x + b_.x) / 2, (a_.y + b_.y) / 2))
                except Exception:
                    pass
            melhor = None
            for ls in q["eixos"]:
                vert = [Point(c) for c in ls.coords]
                for (x1, y1), (x2, y2) in zip(ls.coords, ls.coords[1:]):
                    Ls = math.dist((x1, y1), (x2, y2))
                    if Ls < comp_txt * 1.2:
                        continue
                    for f in [i / 20 for i in range(3, 18)]:
                        c = Point(x1 + (x2 - x1) * f, y1 + (y2 - y1) * f)
                        livre_ = min([o.distance(c) for o in obst + vert] or [1e9])
                        nota = livre_ - abs(f - 0.5) * 2          # prefere o meio do trecho
                        if melhor is None or nota > melhor[0]:
                            melhor = (nota, (c.x, c.y), math.degrees(math.atan2(y2 - y1, x2 - x1)))
            if melhor:
                ang = math.radians(melhor[2])
                desl = (q["largura"] / 4) if q["largura"] else 0      # no meio de uma faixa, fora do eixo
                pos = (melhor[1][0] - math.sin(ang) * desl, melhor[1][1] + math.cos(ang) * desl)
                rot = _rot_legivel(melhor[2])
            else:
                pos, rot = (ins.x, ins.y), t.dxf.rotation
            t.dxf.height = h_nome
            t.dxf.style = "FOLHA_ARIAL_N"
            t.dxf.rotation = rot
            t.dxf.width = 1.0
            t.rgb = (255, 255, 255)
            t.set_placement(pos, align=TextEntityAlignment.MIDDLE_CENTER)
            log.append(f"nome da rua: '{s}' ({br(h_nome / k, 1)} mm, branco sobre a pista)")
    if n_ajust:
        log.append(f"{n_ajust} texto(s) de trecho com 2,0 mm no papel, por fora do meio-fio")

    # cotas: estilo próprio na escala, texto com fundo
    dims = list(msp.query("DIMENSION"))
    if dims:
        larg_papel = (q["largura"] or 7.0) / k
        hd = min(1.8, larg_papel * 0.85 / 3.6) * k
        nome = f"FOLHA_COTA_{esc}"
        if nome not in doc.dimstyles:
            ds = doc.dimstyles.new(nome)
            ds.dxf.dimtxsty = "FOLHA_ARIAL"
            ds.dxf.dimtxt = hd
            ds.dxf.dimasz = hd * 0.7
            ds.dxf.dimexe = hd * 0.4
            ds.dxf.dimexo = 0.0
            ds.dxf.dimgap = hd * 0.25
            ds.dxf.dimdec = 2
            ds.dxf.dimdsep = ord(",")
            ds.dxf.dimtad = 1
            ds.dxf.dimtih = 0
            ds.dxf.dimtoh = 0
            ds.dxf.dimzin = 0
            ds.dxf.dimlunit = 2
            ds.dxf.dimtfill = 1
            ds.dxf.dimclrd = 7
            ds.dxf.dimclre = 7
        ok = 0
        for d in dims:
            try:
                d.dxf.dimstyle = nome
                d.discard_xdata("ACAD") if d.has_xdata("ACAD") else None
                if not d.dxf.get("text"):
                    d.dxf.text = "<> m"
                d.override().render()
                ok += 1
            except Exception:
                pass
        log.append(f"{ok} cota(s) de largura regeneradas com {br(hd / k, 1)} mm no papel")

    # hachura atrás de tudo
    try:
        msp.set_redraw_order([(h.dxf.handle, f"{i + 1:X}") for i, h in enumerate(msp.query("HATCH"))])
    except Exception:
        pass


# --------------------------------------------------------------------------- #
#  Folha (paper space)
# --------------------------------------------------------------------------- #
class Folha:
    def __init__(self, doc, psp):
        self.doc, self.psp = doc, psp

    # texto simples
    def txt(self, s, x, y, h, al="ML", negrito=False, rot=0.0, layer="FOLHA_TEXTO", cor=None):
        mapa = {"ML": TextEntityAlignment.MIDDLE_LEFT, "MC": TextEntityAlignment.MIDDLE_CENTER,
                "MR": TextEntityAlignment.MIDDLE_RIGHT, "BL": TextEntityAlignment.BOTTOM_LEFT,
                "BR": TextEntityAlignment.BOTTOM_RIGHT, "BC": TextEntityAlignment.BOTTOM_CENTER,
                "TL": TextEntityAlignment.TOP_LEFT, "TC": TextEntityAlignment.TOP_CENTER}
        at = {"style": "FOLHA_ARIAL_N" if negrito else "FOLHA_ARIAL", "layer": layer, "rotation": rot}
        if cor is not None:
            at["color"] = cor
        t = self.psp.add_text(s, height=h, dxfattribs=at)
        t.set_placement((x, y), align=mapa[al])
        return t

    def campo(self, rotulo, valor, x0, y0, x1, y1, h_val=2.5, negrito=True, al="ML"):
        """célula de carimbo: rótulo pequeno em cima, valor embaixo."""
        self.txt(rotulo, x0 + 1.5, y1 - 2.2, 1.6, "ML", cor=COR_TXT_SEC)
        if valor:
            if al == "MC":
                self.txt(valor, (x0 + x1) / 2, (y0 + y1) / 2 - 1.2, h_val, "MC", negrito)
            else:
                self.txt(valor, x0 + 1.5, (y0 + y1) / 2 - 1.2, h_val, "ML", negrito)

    def ret(self, x0, y0, x1, y1, layer="FOLHA_LINHAS"):
        self.psp.add_lwpolyline([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], close=True, dxfattribs={"layer": layer})

    def lin(self, a, b, layer="FOLHA_LINHAS"):
        self.psp.add_line(a, b, dxfattribs={"layer": layer})

    def fundo(self, x0, y0, x1, y1, rgb=COR_FUNDO):
        h = self.psp.add_hatch(dxfattribs={"layer": "FOLHA_FUNDO"})
        h.set_solid_fill(color=9, rgb=rgb)
        h.paths.add_polyline_path([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], is_closed=True)

    def solido(self, pts, layer="FOLHA_SIMBOLOS", rgb=None, cor=7):
        h = self.psp.add_hatch(dxfattribs={"layer": layer})
        h.set_solid_fill(color=cor, rgb=rgb)
        h.paths.add_polyline_path(pts, is_closed=True)

    def caixa_titulo(self, x0, y1, x1, titulo, h=6.0):
        """faixa de título de uma seção; devolve o y de baixo da faixa."""
        self.fundo(x0, y1 - h, x1, y1)
        self.lin((x0, y1 - h), (x1, y1 - h))
        self.txt(titulo, (x0 + x1) / 2, y1 - h / 2, 2.5, "MC", negrito=True)
        return y1 - h


def preparar_layers(doc):
    for nome, (cor, lw, plota) in LAYERS_FOLHA.items():
        if nome not in doc.layers:
            L = doc.layers.add(nome, color=cor)
        else:
            L = doc.layers.get(nome)
        L.dxf.lineweight = lw if lw else -3
        L.dxf.plot = 1 if plota else 0


def escolher_malha(esc):
    alvo = 50.0                                   # ~5 cm entre linhas no papel
    cands = [1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 5000]
    return min(cands, key=lambda g: abs(g * 1000 / esc - alvo) + (100 if g * 1000 / esc < 25 else 0))


def montar_folha(doc, formato, esc, centro, q, lay, info, log):
    W, H = FORMATOS[formato]
    m = MARGEM[formato]
    CW = COLUNA[formato]
    preparar_layers(doc)
    _ler_estilos(doc)

    nome_layout = info["layout"]
    if nome_layout in doc.layouts:
        doc.layouts.delete(nome_layout)
    psp = doc.layouts.new(nome_layout)
    psp.page_setup(size=(W, H), margins=(0, 0, 0, 0), units="mm", offset=(0, 0), rotation=0,
                   scale=(1, 1), name=PAPEL_PC3[formato], device="DWG To PDF.pc3")
    try:
        psp.set_plot_style("acad.ctb", show=True)
        psp.use_plot_styles(True)
    except Exception:
        pass
    for v in psp.viewports():
        if v.dxf.id == 1:
            v.dxf.layer = "0"          # o AutoCAD exige a viewport principal da folha no layer 0
    F = Folha(doc, psp)

    # ---------------- moldura
    bx0, by0, bx1, by1 = MARGEM_ESQ, m, W - m, H - m
    F.ret(0, 0, W, H, "FOLHA_FINAS")                    # linha de corte
    F.ret(bx0, by0, bx1, by1, "FOLHA_MOLDURA")
    cx0 = bx1 - CW                                       # coluna de informações
    F.lin((cx0, by0), (cx0, by1), "FOLHA_MOLDURA")

    # ---------------- janela da planta
    fx0, fy0, fx1, fy1 = area_mapa(formato)
    mx, my = centro
    kk = 1000.0 / esc                                    # mm de papel por metro
    pcx, pcy = (fx0 + fx1) / 2, (fy0 + fy1) / 2
    vp = psp.add_viewport(center=(pcx, pcy), size=(fx1 - fx0, fy1 - fy0), view_center_point=(mx, my),
                          view_height=(fy1 - fy0) / kk, dxfattribs={"layer": "FOLHA_VIEWPORT"})
    vp.dxf.flags = vp.dxf.flags | const.VSF_LOCK_ZOOM
    vp.frozen_layers = [l for l in lay["ignorar"]]
    F.ret(fx0, fy0, fx1, fy1, "FOLHA_LINHAS")

    # ---------------- malha de coordenadas
    g = escolher_malha(esc)
    e_min, e_max = mx - (pcx - fx0) / kk, mx + (fx1 - pcx) / kk
    n_min, n_max = my - (pcy - fy0) / kk, my + (fy1 - pcy) / kk
    hm = 1.8
    ocupados = []

    def livre(caixa):
        ax0, ay0, ax1, ay1 = caixa
        for bx0_, by0_, bx1_, by1_ in ocupados:
            if ax0 < bx1_ and bx0_ < ax1 and ay0 < by1_ and by0_ < ay1:
                return False
        ocupados.append(caixa)
        return True

    def rotulo(s, x, y, rot, al):
        t = psp.add_mtext(s, dxfattribs={"layer": "FOLHA_MALHA_TXT", "style": "FOLHA_ARIAL",
                                         "char_height": hm, "rotation": rot})
        t.set_location((x, y), attachment_point=al)
        t.set_bg_color("canvas", scale=1.25)

    larg_rot = lambda s: len(s) * hm * 0.62
    E = math.ceil(e_min / g) * g
    linhas_e = []
    while E < e_max:
        x = pcx + (E - mx) * kk
        if fx0 + 0.5 < x < fx1 - 0.5:
            F.lin((x, fy0), (x, fy1), "FOLHA_MALHA")
            linhas_e.append((x, E))
        E += g
    N = math.ceil(n_min / g) * g
    linhas_n = []
    while N < n_max:
        y = pcy + (N - my) * kk
        if fy0 + 0.5 < y < fy1 - 0.5:
            F.lin((fx0, y), (fx1, y), "FOLHA_MALHA")
            linhas_n.append((y, N))
        N += g
    for x, E in linhas_e:
        s = f"E={br_int(E)}"
        L = larg_rot(s)
        if x - hm - 1.2 > fx0 + 1 and x < fx1 - 1:
            if livre((x - hm - 1.0, fy0 + 1.5, x - 0.6, fy0 + 1.5 + L)):
                rotulo(s, x - 0.7, fy0 + 1.5, 90, MTextEntityAlignment.BOTTOM_LEFT)
            if livre((x - hm - 1.0, fy1 - 1.5 - L, x - 0.6, fy1 - 1.5)):
                rotulo(s, x - 0.7, fy1 - 1.5, 90, MTextEntityAlignment.BOTTOM_RIGHT)
    for y, N in linhas_n:
        s = f"N={br_int(N)}"
        L = larg_rot(s)
        if y + hm + 1.2 < fy1 - 1:
            if livre((fx0 + 1.5, y + 0.6, fx0 + 1.5 + L, y + hm + 1.0)):
                rotulo(s, fx0 + 1.5, y + 0.7, 0, MTextEntityAlignment.BOTTOM_LEFT)
            if livre((fx1 - 1.5 - L, y + 0.6, fx1 - 1.5, y + hm + 1.0)):
                rotulo(s, fx1 - 1.5, y + 0.7, 0, MTextEntityAlignment.BOTTOM_RIGHT)

    # ---------------- coluna: de cima para baixo
    x0, x1 = cx0, bx1
    pad = 0.0
    y = by1

    # (1) orientação: norte, datum, escala e escala gráfica
    h_or = 62
    yb = y - h_or
    F.ret(x0, yb, x1, y)
    yt = F.caixa_titulo(x0, y, x1, "ORIENTAÇÃO E ESCALA")
    ncx, ncy, r = x0 + 22, (yt + yb) / 2 - 1, 11
    psp.add_circle((ncx, ncy), r, dxfattribs={"layer": "FOLHA_SIMBOLOS"})
    tip, base_e, base_d, notch = (ncx, ncy + r + 3), (ncx - 5.5, ncy - r + 2), (ncx + 5.5, ncy - r + 2), (ncx, ncy - 3.5)
    F.solido([tip, notch, base_e])
    psp.add_lwpolyline([tip, base_d, notch, base_e, tip], dxfattribs={"layer": "FOLHA_SIMBOLOS"})
    F.txt("NQ", ncx, tip[1] + 3.2, 3.5, "MC", negrito=True)
    tx = x0 + 44
    F.txt(f"ESCALA 1:{br_int(esc)}", tx, yt - 7, 3.5, "ML", negrito=True)
    linhas_dat = [f"DATUM HORIZONTAL: {info['datum']}", f"PROJEÇÃO UTM - FUSO {info['fuso']}S",
                  f"MERIDIANO CENTRAL: {abs(info['mc'])}° W", "COORDENADAS EM METROS"]
    for i, s in enumerate(linhas_dat):
        F.txt(s, tx, yt - 13.5 - i * 4.0, 1.9, "ML")
    # escala gráfica: ~60 mm no papel
    alvo_m = 60 / kk
    passo = min([1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000], key=lambda v: abs(5 * v - alvo_m))
    comp = 5 * passo * kk
    sx0, sy0 = tx, yb + 9
    hb = 2.2
    for i in range(5):
        a, b = sx0 + i * passo * kk, sx0 + (i + 1) * passo * kk
        if i % 2 == 0:
            F.solido([(a, sy0), (b, sy0), (b, sy0 + hb), (a, sy0 + hb)])
        F.ret(a, sy0, b, sy0 + hb, "FOLHA_SIMBOLOS")
        F.txt(br_int(i * passo), a, sy0 + hb + 1.0, 1.6, "BC")
    F.txt(f"{br_int(5 * passo)} m", sx0 + comp, sy0 + hb + 1.0, 1.6, "BC")
    F.txt("ESCALA GRÁFICA", sx0, sy0 - 2.6, 1.6, "ML", cor=COR_TXT_SEC)
    y = yb - pad

    # (2) legenda
    itens = [("pav", "PAVIMENTO ASFÁLTICO"), ("mf", "MEIO-FIO E SARJETA"), ("eixo", "EIXO DA VIA"),
             ("cota", "LARGURA DA PISTA (COTA)"), ("malha", f"MALHA DE COORDENADAS ({br_int(g)} m)")]
    lh = 7.0
    h_leg = 6 + len(itens) * lh + 2
    yb = y - h_leg
    F.ret(x0, yb, x1, y)
    yt = F.caixa_titulo(x0, y, x1, "LEGENDA")
    for i, (tipo, desc) in enumerate(itens):
        cy = yt - 1 - (i + 0.5) * lh
        sa, sb = x0 + 6, x0 + 28
        if tipo == "pav":
            F.solido([(sa, cy - 2.2), (sb, cy - 2.2), (sb, cy + 2.2), (sa, cy + 2.2)], rgb=info["cor_pav"], cor=8)
            psp.add_line((sa, cy - 2.2), (sb, cy - 2.2), dxfattribs={"layer": "FOLHA_SIMBOLOS", "color": 30, "lineweight": 35})
            psp.add_line((sa, cy + 2.2), (sb, cy + 2.2), dxfattribs={"layer": "FOLHA_SIMBOLOS", "color": 30, "lineweight": 35})
        elif tipo == "mf":
            psp.add_line((sa, cy), (sb, cy), dxfattribs={"layer": "FOLHA_SIMBOLOS", "color": 30, "lineweight": 50})
        elif tipo == "eixo":
            psp.add_line((sa, cy), (sb, cy), dxfattribs={"layer": "FOLHA_SIMBOLOS", "color": 1, "lineweight": 18,
                                                         "linetype": "DASHDOT", "ltscale": 0.35})
        elif tipo == "cota":
            psp.add_line((sa + 3, cy - 2), (sa + 3, cy + 2), dxfattribs={"layer": "FOLHA_SIMBOLOS", "lineweight": 13})
            psp.add_line((sb - 3, cy - 2), (sb - 3, cy + 2), dxfattribs={"layer": "FOLHA_SIMBOLOS", "lineweight": 13})
            psp.add_line((sa + 3, cy - 1), (sb - 3, cy - 1), dxfattribs={"layer": "FOLHA_SIMBOLOS", "lineweight": 13})
            F.solido([(sa + 3, cy - 1), (sa + 5, cy - 0.4), (sa + 5, cy - 1.6)])
            F.solido([(sb - 3, cy - 1), (sb - 5, cy - 0.4), (sb - 5, cy - 1.6)])
            F.txt(f"{br(q['largura'])} m", (sa + sb) / 2, cy - 0.2, 1.4, "BC")
        elif tipo == "malha":
            psp.add_line((sa, cy), (sb, cy), dxfattribs={"layer": "FOLHA_MALHA"})
            psp.add_line(((sa + sb) / 2, cy - 2.4), ((sa + sb) / 2, cy + 2.4), dxfattribs={"layer": "FOLHA_MALHA"})
        F.txt(desc, sb + 5, cy, 2.0, "ML")
    y = yb - pad

    # (3) quadro de quantidades
    linhas_q = [("MEIO-FIO", "m", br(q["meio_fio"])), ("SARJETA", "m", br(q["sarjeta"])),
                ("PAVIMENTO ASFÁLTICO", "m²", br(q["area"]))]
    linhas_i = [("EXTENSÃO DO EIXO", "m", br(q["eixo"])), ("LARGURA DA PISTA", "m", br(q["largura"]))]
    rh, rh2, hh = 7.0, 5.5, 5.5
    h_q = 6 + hh + len(linhas_q) * rh + len(linhas_i) * rh2
    yb = y - h_q
    F.ret(x0, yb, x1, y)
    yt = F.caixa_titulo(x0, y, x1, "QUADRO DE QUANTIDADES")
    c1, c2 = x1 - 52, x1 - 36
    yh = yt - hh
    F.lin((x0, yh), (x1, yh))
    F.txt("SERVIÇO", x0 + 4, yh + hh / 2, 1.8, "ML", negrito=True)
    F.txt("UND.", (c1 + c2) / 2, yh + hh / 2, 1.8, "MC", negrito=True)
    F.txt("QUANTIDADE", (c2 + x1) / 2, yh + hh / 2, 1.8, "MC", negrito=True)
    yy = yh
    for s, u, v in linhas_q:
        F.txt(s, x0 + 4, yy - rh / 2, 2.3, "ML", negrito=True)
        F.txt(u, (c1 + c2) / 2, yy - rh / 2, 2.3, "MC")
        F.txt(v, x1 - 4, yy - rh / 2, 2.5, "MR", negrito=True)
        yy -= rh
        F.lin((x0, yy), (x1, yy), "FOLHA_FINAS")
    for s, u, v in linhas_i:
        F.txt(s, x0 + 4, yy - rh2 / 2, 1.8, "ML", cor=COR_TXT_SEC)
        F.txt(u, (c1 + c2) / 2, yy - rh2 / 2, 1.8, "MC", cor=COR_TXT_SEC)
        F.txt(v, x1 - 4, yy - rh2 / 2, 1.9, "MR", cor=COR_TXT_SEC)
        yy -= rh2
        if yy > yb + 0.1:
            F.lin((x0, yy), (x1, yy), "FOLHA_FINAS")
    F.lin((c1, yt), (c1, yb), "FOLHA_FINAS")
    F.lin((c2, yt), (c2, yb), "FOLHA_FINAS")
    y = yb - pad

    # (6) carimbo (fixo embaixo) e (5) revisões logo acima
    h_car = 76
    cy0, cy1 = by0, by0 + h_car
    F.ret(x0, cy0, x1, cy1, "FOLHA_MOLDURA")
    r1 = cy1 - 16; r2 = r1 - 18; r3 = r2 - 14; r4 = r3 - 14
    for yy_ in (r1, r2, r3, r4):
        F.lin((x0, yy_), (x1, yy_))
    xe = x0 + 58
    F.lin((xe, cy1), (xe, r1))
    F.campo("EMPRESA", info["empresa"], x0, r1, xe, cy1, 2.5)
    F.txt("PROJETO", xe + 1.5, cy1 - 2.2, 1.6, "ML", cor=COR_TXT_SEC)
    F.txt(info["projeto"], xe + 1.5, cy1 - 7.2, 3.0, "ML", negrito=True)
    F.txt(info["local"], xe + 1.5, cy1 - 12.4, 2.0, "ML")
    F.txt("TÍTULO DA FOLHA", x0 + 1.5, r1 - 2.2, 1.6, "ML", cor=COR_TXT_SEC)
    F.txt(info["titulo"], x0 + 1.5, r1 - 7.6, 3.2, "ML", negrito=True)
    F.txt(info["rua"].upper(), x0 + 1.5, r1 - 13.4, 2.5, "ML")
    xa, xb = x0 + 92, x0 + 128
    F.lin((xa, r2), (xa, r3)); F.lin((xb, r2), (xb, r3))
    F.campo("RESPONSÁVEL TÉCNICO", info["responsavel"], x0, r3, xa, r2, 2.2)
    F.campo("CREA / CAU", info["crea"], xa, r3, xb, r2, 2.2)
    F.campo("ASSINATURA", "", xb, r3, x1, r2)
    cols = [x0, x0 + 34, x0 + 62, x0 + 96, x0 + 118, x1]
    for xx in cols[1:-1]:
        F.lin((xx, r3), (xx, r4))
    F.campo("DESENHO", info["desenho"], cols[0], r4, cols[1], r3, 2.2)
    F.campo("DATA", info["data"], cols[1], r4, cols[2], r3, 2.2)
    F.campo("ESCALA", f"1:{br_int(esc)}", cols[2], r4, cols[3], r3, 2.8)
    F.campo("FORMATO", formato, cols[3], r4, cols[4], r3, 2.8)
    F.campo("FOLHA", info["folha"], cols[4], r4, cols[5], r3, 3.2, al="MC")
    xr = x1 - 30
    F.lin((xr, r4), (xr, cy0))
    F.campo("ARQUIVO", info["arquivo"], x0, cy0, xr, r4, 1.9, negrito=False)
    F.campo("REVISÃO", info["revisao"], xr, cy0, x1, r4, 3.2, al="MC")

    h_rev = 16
    ry0, ry1 = cy1, cy1 + h_rev
    F.ret(x0, ry0, x1, ry1)
    F.fundo(x0, ry1 - 5, x1, ry1)
    rc = [x0, x0 + 14, x0 + 104, x0 + 132, x1]
    F.lin((x0, ry1 - 5), (x1, ry1 - 5)); F.lin((x0, ry1 - 10.5), (x1, ry1 - 10.5), "FOLHA_FINAS")
    for xx in rc[1:-1]:
        F.lin((xx, ry0), (xx, ry1))
    for (a, b), s in zip(zip(rc, rc[1:]), ("REV.", "DESCRIÇÃO", "DATA", "POR")):
        F.txt(s, (a + b) / 2, ry1 - 2.5, 1.7, "MC", negrito=True)
    for (a, b), s in zip(zip(rc, rc[1:]), (info["revisao"], "EMISSÃO INICIAL", info["data"], info["desenho"])):
        F.txt(s, (a + b) / 2, ry1 - 7.75, 1.8, "MC")

    # (4) localização da via
    loc = info["localizacao"]
    if loc:
        h_loc = 6 + len(loc) * 5.2 + 1.5
        yb = y - h_loc
        F.ret(x0, yb, x1, y)
        yt = F.caixa_titulo(x0, y, x1, "LOCALIZAÇÃO DA VIA")
        for i, (rot_, val) in enumerate(loc):
            yy = yt - 3.4 - i * 5.2
            F.txt(rot_, x0 + 4, yy, 1.7, "ML", cor=COR_TXT_SEC)
            F.txt(val, x0 + 50, yy, 2.0, "ML")
        y = yb - pad

    # (5) notas (altura do conteúdo)
    notas = [
        "MEDIDAS EM METROS, EXCETO ONDE INDICADO.",
        f"COORDENADAS NO SISTEMA {info['datum']} / UTM FUSO {info['fuso']}S.",
        "MEIO-FIO: EXTENSÃO DO CONTORNO DA PISTA, DESCONTADAS AS BOCAS NAS EXTREMIDADES DA VIA."
        if q["bocas"] else "MEIO-FIO: EXTENSÃO DAS LINHAS DE MEIO-FIO DO DESENHO.",
        "SARJETA: MESMA EXTENSÃO DO MEIO-FIO.",
        f"PAVIMENTO: ÁREA {'DA HACHURA' if q['origem_area'] == 'hachura' else 'DELIMITADA PELO MEIO-FIO'}"
        " DA PISTA, SEM PERDAS.",
        "QUANTITATIVOS EXTRAÍDOS DO DESENHO; CONFERIR COM O PROJETO EXECUTIVO.",
    ]
    larg_util = x1 - x0 - 8
    por_linha = max(int(larg_util / (1.8 * 0.6)), 20)
    n_lin = sum(math.ceil((len(s_) + 3) / por_linha) for s_ in notas)
    h_not = 6 + 3 + n_lin * 1.8 * 1.25 * 1.7 + 4
    ry1_prev = by0 + 76 + pad + 16                       # topo do quadro de revisões
    if y - h_not - pad > ry1_prev + 8:
        yb = y - h_not
        F.ret(x0, yb, x1, y)
        yt = F.caixa_titulo(x0, y, x1, "NOTAS")
        texto = "\\P".join(f"{i}. {s_}" for i, s_ in enumerate(notas, 1))
        mt = psp.add_mtext(texto, dxfattribs={"layer": "FOLHA_TEXTO", "style": "FOLHA_ARIAL", "char_height": 1.8,
                                              "width": larg_util, "line_spacing_factor": 1.25})
        mt.set_location((x0 + 4, yt - 3), attachment_point=MTextEntityAlignment.TOP_LEFT)
        y = yb - pad
    else:
        log.append("pouco espaço na coluna: notas omitidas (use uma folha maior)")

    # (6) espaço reservado (aprovação) até o quadro de revisões
    if y - ry1_prev > 20:
        F.ret(x0, ry1_prev, x1, y)
        F.caixa_titulo(x0, y, x1, "ESPAÇO RESERVADO PARA APROVAÇÃO")

    try:
        doc.layouts.set_active_layout(nome_layout)
    except Exception:
        pass
    if "Layout1" in doc.layouts and len(doc.layouts.get("Layout1")) <= 1 and nome_layout != "Layout1":
        try:
            doc.layouts.delete("Layout1")
        except Exception:
            pass
    try:   # fundos cinza atrás das linhas e textos
        psp.set_redraw_order([(h.dxf.handle, f"{i + 1:X}") for i, h in enumerate(psp.query("HATCH"))])
    except Exception:
        pass
    doc.header["$LWDISPLAY"] = 1
    doc.header["$PSLTSCALE"] = 1
    return psp, g


# --------------------------------------------------------------------------- #
#  DWG / PDF
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


def achar_accoreconsole():
    """console do AutoCAD instalado (o mais novo): converte DXF -> DWG sem abrir a interface."""
    if os.name != "nt":
        return None
    base = Path(os.environ.get("ProgramFiles", r"C:\Program Files"), "Autodesk")
    achados = sorted(base.glob("AutoCAD 20*/accoreconsole.exe"), reverse=True)
    return achados[0] if achados else None


def dwg_via_accore(exe, dxf: Path, dwg: Path) -> bool:
    scr = Path(tempfile.mkdtemp()) / "salvar_dwg.scr"
    alvo = str(dwg).replace("\\", "/")
    scr.write_text(f'_.SAVEAS\n2018\n"{alvo}"\n_.QUIT\n_Y\n', encoding="cp1252", errors="replace")
    if dwg.exists():
        dwg.unlink()
    kw = {"capture_output": True, "timeout": 300}
    si = subprocess.STARTUPINFO()
    si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    kw["startupinfo"] = si
    try:
        subprocess.run([str(exe), "/i", str(dxf), "/s", str(scr)], check=False, **kw)
    except Exception:
        return False
    return dwg.exists()


def _rodar_oda(exe, ent, sai, versao, tipo):
    cmd = [str(exe), str(ent), str(sai), versao, tipo, "0", "1", "*.*"]
    kw = {"capture_output": True, "timeout": 600}
    if os.name == "nt":
        si = subprocess.STARTUPINFO()
        si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        kw["startupinfo"] = si
    elif sys.platform.startswith("linux") and not os.environ.get("DISPLAY") and shutil.which("xvfb-run"):
        cmd = ["xvfb-run", "-a"] + cmd
    subprocess.run(cmd, check=False, **kw)


def exportar_pdf(doc, psp, formato, destino: Path):
    W, H = FORMATOS[formato]
    from ezdxf.addons.drawing import Frontend, RenderContext
    from ezdxf.addons.drawing import layout as dl
    from ezdxf.addons.drawing.config import BackgroundPolicy, ColorPolicy, Configuration, LineweightPolicy
    cfg = Configuration(background_policy=BackgroundPolicy.WHITE, color_policy=ColorPolicy.COLOR,
                        lineweight_policy=LineweightPolicy.ABSOLUTE, lineweight_scaling=1.0,
                        min_lineweight=0.05)
    try:
        from ezdxf.addons.drawing import pymupdf
        import pymupdf as _  # noqa: F401
        be = pymupdf.PyMuPdfBackend()
        Frontend(RenderContext(doc), be, config=cfg).draw_layout(psp)
        destino.write_bytes(be.get_pdf_bytes(dl.Page(W, H, dl.Units.mm),
                                             settings=dl.Settings(fit_page=False, scale=1.0)))
        return True
    except ImportError:
        pass
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        from ezdxf.addons.drawing.matplotlib import MatplotlibBackend
        fig = plt.figure(figsize=(W / 25.4, H / 25.4))
        ax = fig.add_axes([0, 0, 1, 1])
        Frontend(RenderContext(doc), MatplotlibBackend(ax), config=cfg).draw_layout(psp, finalize=False)
        ax.set_xlim(0, W); ax.set_ylim(0, H); ax.set_aspect("equal"); ax.set_axis_off()
        fig.savefig(destino, dpi=200)
        plt.close(fig)
        return True
    except ImportError:
        return False


# --------------------------------------------------------------------------- #
#  Principal
# --------------------------------------------------------------------------- #
def _info_do_nome(stem: str):
    """'Rua_Amazonita_-_Varzea_do_Mainho_Val_Paraizo_ruas' -> ('Rua Amazonita', 'Varzea do Mainho Val Paraizo')"""
    s = re.sub(r"(_ruas|_pavimento|_folha)$", "", stem, flags=re.I).replace("_", " ")
    partes = [p.strip() for p in re.split(r"\s+-\s+", s) if p.strip()]
    rua = partes[0] if partes else s
    local = " - ".join(partes[1:]) if len(partes) > 1 else ""
    return re.sub(r"\s+", " ", rua), re.sub(r"\s+", " ", local)


def escolher_arquivo_gui():
    try:
        import tkinter as tk
        from tkinter import filedialog
        root = tk.Tk(); root.withdraw()
        c = filedialog.askopenfilename(title="Desenho das ruas (DXF/DWG)",
                                       filetypes=[("Desenho", "*.dxf *.dwg"), ("Todos", "*.*")])
        root.destroy()
        return c or None
    except Exception:
        return None


def main(argv=None):
    ap = argparse.ArgumentParser(description="Monta a folha de impressão (layout) com malha de coordenadas e quantitativos.")
    ap.add_argument("entrada", nargs="?", help="desenho .dxf ou .dwg (vazio = abre janela)")
    ap.add_argument("-o", "--saida", help="pasta de saída (padrão: a do desenho)")
    ap.add_argument("--folha", choices=list(FORMATOS), help="formato (padrão: automático)")
    ap.add_argument("--escala", type=int, help="denominador da escala, ex.: 1000 (padrão: automático)")
    ap.add_argument("--rua", help="nome da via (padrão: tirado do nome do arquivo)")
    ap.add_argument("--local", help="bairro / município")
    ap.add_argument("--projeto", default="PROJETO DE PAVIMENTAÇÃO")
    ap.add_argument("--titulo", default="PLANTA DE PAVIMENTAÇÃO E QUANTITATIVOS")
    ap.add_argument("--empresa", default="")
    ap.add_argument("--responsavel", default="")
    ap.add_argument("--crea", default="")
    ap.add_argument("--desenho", default="")
    ap.add_argument("--data", default=_dt.date.today().strftime("%d/%m/%Y"))
    ap.add_argument("--folha-num", default="01/01", help="numeração da folha (padrão 01/01)")
    ap.add_argument("--revisao", default="00")
    ap.add_argument("--fuso", type=int, default=23, help="fuso UTM (padrão 23)")
    ap.add_argument("--datum", default="SIRGAS 2000")
    ap.add_argument("--layer-meiofio", help="layer(s) do meio-fio/bordas, separados por vírgula")
    ap.add_argument("--layer-eixo", help="layer(s) do eixo")
    ap.add_argument("--layer-pavimento", help="layer(s) da hachura do pavimento")
    ap.add_argument("--manter-modelo", action="store_true", help="não mexe em textos/cotas/layers do modelo")
    ap.add_argument("--sem-pdf", action="store_true")
    ap.add_argument("--so-dxf", action="store_true", help="não gera DWG")
    ap.add_argument("--oda", help="caminho do ODAFileConverter")
    a = ap.parse_args(argv)

    entrada = a.entrada or escolher_arquivo_gui()
    if not entrada:
        ap.print_help(); return 1
    entrada = Path(entrada)
    if not entrada.exists():
        print(f"Arquivo não encontrado: {entrada}"); return 1
    saida = Path(a.saida) if a.saida else entrada.parent
    saida.mkdir(parents=True, exist_ok=True)

    doc = abrir(entrada, a.oda)
    lay = detectar(doc, a)
    print(f"{entrada.name}")
    print(f"  layers -> meio-fio: {lay['meiofio'] or '-'} | eixo: {lay['eixo'] or '-'} | pavimento: {lay['pavimento'] or '-'}")
    if not lay["meiofio"] and not lay["pavimento"]:
        print("  Não achei layer de meio-fio/bordas nem hachura de pavimento. Use --layer-meiofio / --layer-pavimento.")
        return 1
    q = quantitativos(doc, lay)

    # extensão do desenho (sem quadros/carimbos que já existam no modelo)
    msp = doc.modelspace()
    ents = [e for e in msp if e.dxf.layer not in lay["ignorar"]]
    ext = bbox.extents(ents, fast=True)
    if not ext.has_data:
        print("  Desenho vazio."); return 1
    w_m, h_m = ext.size.x, ext.size.y
    formato, esc = escolher_folha(w_m, h_m, a.folha, a.escala)
    centro = ((ext.extmin.x + ext.extmax.x) / 2, (ext.extmin.y + ext.extmax.y) / 2)

    rua_nome, local_nome = _info_do_nome(entrada.stem)
    rua = a.rua or rua_nome
    local = a.local if a.local is not None else local_nome
    cor_pav = (110, 110, 110)
    for h in msp.query("HATCH"):
        if h.dxf.layer in lay["pavimento"] and h.rgb:
            cor_pav = tuple(h.rgb); break
    base = re.sub(r"(_folha)$", "", entrada.stem) + "_folha"

    # localização: início/fim do eixo e centro em coordenadas geográficas
    loc = []
    if len(q["eixos"]) == 1:
        (xi, yi), (xf, yf) = q["eixos"][0].coords[0], q["eixos"][0].coords[-1]
        loc += [("INÍCIO DO EIXO", f"E={br(xi)}   N={br(yi)}"), ("FIM DO EIXO", f"E={br(xf)}   N={br(yf)}")]
    elif q["eixos"]:
        loc.append(("EIXOS NO DESENHO", f"{len(q['eixos'])} (EXTENSÃO TOTAL {br(q['eixo'])} m)"))
    try:
        from pyproj import Transformer
        tr = Transformer.from_crs(31960 + a.fuso if 17 <= a.fuso <= 25 else 32700 + a.fuso, 4674, always_xy=True)
        lon, lat = tr.transform(*centro)

        def gms(v, pos, neg):
            h_ = pos if v >= 0 else neg
            v = abs(v); g_ = int(v); m_ = int((v - g_) * 60); s_ = (v - g_ - m_ / 60) * 3600
            return (f"{g_}°{m_:02d}'{s_:04.1f}" + '"' + f" {h_}").replace(".", ",")
        loc.append(("CENTRO (LAT / LONG)", f"{gms(lat, 'N', 'S')}   {gms(lon, 'E', 'W')}"))
    except Exception:
        pass
    info = {"rua": rua, "local": local.upper(), "projeto": a.projeto.upper(), "titulo": a.titulo.upper(),
            "empresa": a.empresa.upper(), "responsavel": a.responsavel.upper(), "crea": a.crea.upper(),
            "desenho": a.desenho.upper(), "data": a.data, "folha": a.folha_num, "revisao": a.revisao,
            "datum": a.datum.upper(), "fuso": a.fuso, "mc": -183 + 6 * a.fuso, "cor_pav": cor_pav,
            "arquivo": base + ".dwg", "layout": "FOLHA " + a.folha_num.split("/")[0], "localizacao": loc}

    log = []
    _ler_estilos(doc)
    if not a.manter_modelo:
        acabamento_modelo(doc, lay, q, esc, rua, log)
    psp, g = montar_folha(doc, formato, esc, centro, q, lay, info, log)

    print(f"  folha {formato}  escala 1:{br_int(esc)}  malha {br_int(g)} m  (desenho {br(w_m, 1)} x {br(h_m, 1)} m)")
    print(f"  MEIO-FIO {br(q['meio_fio'])} m | SARJETA {br(q['sarjeta'])} m | PAVIMENTO {br(q['area'])} m²"
          f"  (eixo {br(q['eixo'])} m, largura {br(q['largura'])} m, bocas descontadas: {q['bocas']})")
    for s in log:
        print("  - " + s)

    dxf = saida / f"{base}.dxf"
    doc.saveas(dxf)
    final = dxf
    exe = achar_oda(a.oda)
    if exe:
        ent, sai = Path(tempfile.mkdtemp()), Path(tempfile.mkdtemp())
        shutil.copy2(dxf, ent / dxf.name)
        _rodar_oda(exe, ent, sai, "ACAD2018", "DWG")
        gerado = sai / f"{base}.dwg"
        if gerado.exists():
            shutil.move(str(gerado), saida / f"{base}.dwg")
            final = saida / f"{base}.dwg"
    elif not a.so_dxf and (acc := achar_accoreconsole()):
        print("  Gerando DWG pelo console do AutoCAD...")
        if dwg_via_accore(acc, dxf, saida / f"{base}.dwg"):
            final = saida / f"{base}.dwg"
        else:
            print("  (não foi possível gerar o DWG; use o DXF)")
    if not a.sem_pdf:
        pdf = saida / f"{base}.pdf"
        try:
            if exportar_pdf(doc, psp, formato, pdf):
                print(f"  PDF de conferência: {pdf.name}")
        except Exception as ex:
            print(f"  (PDF de conferência não gerado: {ex})")
    print(f"Pronto: {final}  ->  layout '{info['layout']}'")
    return 0


def _avisar(titulo, texto, erro=False):
    try:
        import tkinter as tk
        from tkinter import messagebox
        r = tk.Tk(); r.withdraw()
        (messagebox.showerror if erro else messagebox.showinfo)(titulo, texto)
        r.destroy()
        return True
    except Exception:
        return False


if __name__ == "__main__":
    interativo = len(sys.argv) == 1 or (len(sys.argv) == 2 and not sys.argv[1].startswith("-"))
    if not interativo:
        sys.exit(main())
    # duplo clique ou arquivo arrastado sobre o script: mostra o resultado numa janela
    import io
    import traceback
    buf = io.StringIO()

    class _Tee(io.TextIOBase):
        def write(self, t):
            if sys.__stdout__ is not None:
                sys.__stdout__.write(t)
            buf.write(t)
            return len(t)
    sys.stdout = _Tee()
    try:
        rc = main()
        texto = buf.getvalue().strip() or "Nada a fazer."
        if not _avisar("Folha de pavimentação", texto, erro=rc != 0):
            input("\nPressione Enter para fechar...")
    except SystemExit:
        pass
    except Exception:
        tb = traceback.format_exc()
        if not _avisar("Folha de pavimentação - erro", buf.getvalue() + "\n" + tb, erro=True):
            print(tb)
            input("\nPressione Enter para fechar...")
