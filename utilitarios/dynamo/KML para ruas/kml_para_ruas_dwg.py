#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
kml_para_ruas_dwg.py
====================
Transforma cada linha de um KML/KMZ em uma rua de largura fixa (padrão 7,00 m)
e grava um DWG por rua.

Cada LineString do KML é tratada como EIXO da rua. Para cada uma o script:
  1. lê o KML/KMZ (pastas, MultiGeometry, gx:Track e nome do Placemark);
  2. projeta as coordenadas para UTM SIRGAS 2000 (fuso detectado sozinho ou
     informado com --epsg);
  3. desenha eixo, bordos a ±largura/2, contorno fechado da pista e estacas;
  4. grava um DXF por rua e converte tudo para DWG com o ODA File Converter.

Layers criados:
  RUA_EIXO      eixo (vermelho, CENTER)
  RUA_BORDO     bordo esquerdo e direito (amarelo)
  RUA_PISTA     contorno fechado da pista (desligado; serve para área/hachura)
  RUA_HACHURA   hachura da pista (só com --hachura)
  RUA_ESTACAS   marcação de estacas
  RUA_TEXTO     código + nome da rua e textos das estacas

Cada eixo recebe um código (01, 02, ...) e leva em XDATA (app "KML_RUA") o nome,
o código e a largura. A rotina Dynamo CriarAlinhamentos_RUA_EIXO.dyn lê esses dados
e cria um alinhamento do Civil 3D por eixo, já no padrão "01 Rua X alinhamento".

Uso:
  python kml_para_ruas_dwg.py ruas.kmz
  python kml_para_ruas_dwg.py ruas.kml -o saida --largura 7 --estaca 20 --epsg 31983
  python kml_para_ruas_dwg.py            (abre uma janela para escolher o arquivo)

Requisitos:
  pip install ezdxf shapely pyproj
  ODA File Converter (gratuito): https://www.opendesign.com/guestfiles/oda_file_converter
  Sem o ODA instalado o script entrega os DXF, que abrem direto no AutoCAD/Civil 3D.
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
from dataclasses import dataclass, field
from pathlib import Path

try:
    import ezdxf
    from ezdxf import zoom
    from ezdxf.enums import TextEntityAlignment
    from ezdxf.lldxf import const as dxfconst
    from pyproj import Transformer
    from shapely.geometry import LineString, MultiLineString, MultiPolygon, Polygon
except ImportError as e:  # pragma: no cover
    sys.exit(f"Falta biblioteca: {e.name}.  Instale com:  pip install ezdxf shapely pyproj")


APPID = "KML_RUA"   # XDATA do eixo: 1000 nome | 1070 código | 1040 largura

VERSOES_DWG = ["ACAD2018", "ACAD2013", "ACAD2010", "ACAD2007", "ACAD2004", "ACAD2000"]

LAYERS = {
    #  nome          cor ACI  linetype
    "RUA_EIXO":    (1,  "CENTER"),
    "RUA_BORDO":   (2,  "Continuous"),
    "RUA_PISTA":   (8,  "Continuous"),
    "RUA_HACHURA": (254, "Continuous"),
    "RUA_ESTACAS": (4,  "Continuous"),
    "RUA_TEXTO":   (7,  "Continuous"),
}


# --------------------------------------------------------------------------- #
#  Leitura do KML / KMZ
# --------------------------------------------------------------------------- #
@dataclass
class Rua:
    nome: str
    partes_lonlat: list[list[tuple[float, float]]]
    partes_xy: list[list[tuple[float, float]]] = field(default_factory=list)
    codigos: list[int] = field(default_factory=list)
    arquivo: str = ""


def _tag(el) -> str:
    return el.tag.rsplit("}", 1)[-1] if isinstance(el.tag, str) else ""


def ler_kml_bytes(caminho: Path) -> bytes:
    if caminho.suffix.lower() == ".kmz":
        with zipfile.ZipFile(caminho) as z:
            kmls = [n for n in z.namelist() if n.lower().endswith(".kml")]
            if not kmls:
                raise ValueError("o KMZ não tem nenhum .kml dentro")
            principal = next((n for n in kmls if Path(n).name.lower() == "doc.kml"), kmls[0])
            return z.read(principal)
    return caminho.read_bytes()


def _coords_linestring(el) -> list[tuple[float, float]]:
    for c in el.iter():
        if _tag(c) == "coordinates":
            texto = re.sub(r",\s+", ",", (c.text or "").strip())
            pts = []
            for tupla in texto.split():
                v = tupla.split(",")
                if len(v) >= 2:
                    try:
                        pts.append((float(v[0]), float(v[1])))
                    except ValueError:
                        pass
            return pts
    return []


def _coords_track(el) -> list[tuple[float, float]]:
    pts = []
    for c in el.iter():
        if _tag(c) == "coord":
            v = (c.text or "").split()
            if len(v) >= 2:
                try:
                    pts.append((float(v[0]), float(v[1])))
                except ValueError:
                    pass
    return pts


def extrair_ruas(kml: bytes) -> tuple[list[Rua], int]:
    """Devolve as ruas (uma por Placemark com linha) e quantas geometrias
    não lineares (pontos, polígonos) foram ignoradas."""
    raiz = ET.fromstring(kml)
    ruas: list[Rua] = []
    ignoradas = 0
    for pm in raiz.iter():
        if _tag(pm) != "Placemark":
            continue
        nome = ""
        for filho in pm:
            if _tag(filho) == "name":
                nome = (filho.text or "").strip()
                break
        partes = []
        for el in pm.iter():
            t = _tag(el)
            if t == "LineString":
                pts = _coords_linestring(el)
            elif t == "Track":
                pts = _coords_track(el)
            else:
                if t in ("Point", "Polygon"):
                    ignoradas += 1
                continue
            if len(pts) >= 2:
                partes.append(pts)
        if partes:
            ruas.append(Rua(nome=nome, partes_lonlat=partes))
    return ruas, ignoradas


# --------------------------------------------------------------------------- #
#  Projeção
# --------------------------------------------------------------------------- #
def epsg_utm(lon: float, lat: float) -> int:
    """EPSG UTM SIRGAS 2000 do fuso (cai para WGS 84 fora da faixa do SIRGAS)."""
    fuso = int((lon + 180) // 6) + 1
    if lat < 0:
        return 31960 + fuso if 17 <= fuso <= 25 else 32700 + fuso
    return 31954 + fuso if 11 <= fuso <= 22 else 32600 + fuso


def _sem_repetidos(pts, tol=1e-3):
    saida = [pts[0]]
    for p in pts[1:]:
        if math.dist(p, saida[-1]) > tol:
            saida.append(p)
    return saida


def projetar(ruas: list[Rua], epsg: int | None) -> tuple[int, set[int]]:
    todos = [p for r in ruas for parte in r.partes_lonlat for p in parte]
    lon_m = sum(p[0] for p in todos) / len(todos)
    lat_m = sum(p[1] for p in todos) / len(todos)
    if epsg is None:
        epsg = epsg_utm(lon_m, lat_m)
    fusos = {epsg_utm(*p) for p in todos}
    tr = Transformer.from_crs(4326, epsg, always_xy=True)
    for r in ruas:
        r.partes_xy = []
        for parte in r.partes_lonlat:
            xs, ys = tr.transform([p[0] for p in parte], [p[1] for p in parte])
            pts = _sem_repetidos(list(zip(xs, ys)))
            if len(pts) >= 2:
                r.partes_xy.append(pts)
    return epsg, fusos


# --------------------------------------------------------------------------- #
#  Desenho
# --------------------------------------------------------------------------- #
def _br(v: float, casas: int = 2) -> str:
    return f"{v:.{casas}f}".replace(".", ",")


def _rot_legivel(graus: float) -> tuple[float, bool]:
    """Normaliza o ângulo para o texto não ficar de cabeça para baixo.
    Retorna (rotação, virou)."""
    g = graus % 360
    if 90 < g <= 270:
        return g - 180, True
    return g, False


def _linhas(geom):
    if geom.is_empty:
        return []
    if isinstance(geom, LineString):
        return [geom]
    if isinstance(geom, MultiLineString):
        return list(geom.geoms)
    return [g for g in getattr(geom, "geoms", []) if isinstance(g, LineString)]


def _poligonos(geom):
    if isinstance(geom, Polygon):
        return [geom]
    if isinstance(geom, MultiPolygon):
        return list(geom.geoms)
    return []


def novo_desenho(epsg: int, altura_texto: float):
    doc = ezdxf.new("R2018", setup=True)
    doc.units = ezdxf.units.M
    doc.header["$INSUNITS"] = 6
    doc.header["$MEASUREMENT"] = 1
    doc.header["$LTSCALE"] = 1.0
    doc.header.custom_vars.append("SRC", f"EPSG:{epsg}")
    if "ARIAL" not in doc.styles:
        doc.styles.add("ARIAL", font="arial.ttf")
    if APPID not in doc.appids:
        doc.appids.add(APPID)
    for nome, (cor, lt) in LAYERS.items():
        lay = doc.layers.add(nome, color=cor, linetype=lt if lt in doc.linetypes else "Continuous")
        if nome == "RUA_PISTA":
            lay.off()
    return doc


def desenhar_rua(msp, rua: Rua, largura: float, intervalo_estaca: float,
                 altura_texto: float, hachura: bool) -> dict:
    meia = largura / 2.0
    comp_total = 0.0
    area_total = 0.0
    n_estacas = 0
    txt = {"style": "ARIAL", "layer": "RUA_TEXTO"}

    for i_parte, pts in enumerate(rua.partes_xy):
        eixo = LineString(pts)
        L = eixo.length
        comp_total += L

        cod = rua.codigos[i_parte]
        nome_parte = rua.nome if len(rua.partes_xy) == 1 else f"{rua.nome} trecho {i_parte + 1}"

        # eixo + XDATA (nome, código, largura) — lido pela rotina Dynamo de alinhamentos
        pl = msp.add_lwpolyline(pts, close=False, dxfattribs={"layer": "RUA_EIXO"})
        pl.set_xdata(APPID, [(1000, nome_parte), (1070, cod), (1040, largura)])

        # bordos (+ = esquerda no sentido do eixo, - = direita)
        for d in (meia, -meia):
            bordo = eixo.offset_curve(d, join_style="mitre", mitre_limit=5.0)
            for ln in _linhas(bordo):
                msp.add_lwpolyline(list(ln.coords), close=ln.is_ring,
                                   dxfattribs={"layer": "RUA_BORDO"})

        # contorno fechado da pista (área / hachura)
        pista = eixo.buffer(meia, cap_style="flat", join_style="mitre", mitre_limit=5.0)
        for pol in _poligonos(pista):
            area_total += pol.area
            aneis = [pol.exterior] + list(pol.interiors)
            for anel in aneis:
                msp.add_lwpolyline(list(anel.coords)[:-1], close=True,
                                   dxfattribs={"layer": "RUA_PISTA"})
            if hachura:
                h = msp.add_hatch(color=254, dxfattribs={"layer": "RUA_HACHURA"})
                h.paths.add_polyline_path(list(pol.exterior.coords)[:-1], is_closed=True,
                                          flags=dxfconst.BOUNDARY_PATH_EXTERNAL)
                for anel in pol.interiors:
                    h.paths.add_polyline_path(list(anel.coords)[:-1], is_closed=True,
                                              flags=dxfconst.BOUNDARY_PATH_OUTERMOST)

        # estacas
        if intervalo_estaca > 0:
            dists = [k * intervalo_estaca for k in range(int(L // intervalo_estaca) + 1)]
            if L - dists[-1] > 0.01:
                dists.append(L)
            for d in dists:
                p = eixo.interpolate(d)
                a = eixo.interpolate(max(d - 0.5, 0.0))
                b = eixo.interpolate(min(d + 0.5, L))
                ang = math.atan2(b.y - a.y, b.x - a.x)
                perp = ang + math.pi / 2           # aponta para a esquerda
                ux, uy = math.cos(perp), math.sin(perp)
                r = meia + 1.0
                msp.add_line((p.x - ux * r, p.y - uy * r), (p.x + ux * r, p.y + uy * r),
                             dxfattribs={"layer": "RUA_ESTACAS"})
                inteira = int(round(d / intervalo_estaca, 6) // 1)
                resto = d - inteira * intervalo_estaca
                rotulo = f"E{inteira}" if resto < 0.005 else f"E{inteira}+{_br(resto)}"
                rot, virou = _rot_legivel(math.degrees(perp))
                pos = (p.x + ux * (r + 0.5), p.y + uy * (r + 0.5))
                al = TextEntityAlignment.MIDDLE_RIGHT if virou else TextEntityAlignment.MIDDLE_LEFT
                msp.add_text(rotulo, height=altura_texto * 0.8, rotation=rot,
                             dxfattribs=txt).set_placement(pos, align=al)
                n_estacas += 1

        # código + nome no meio do trecho, lado direito, alinhado com o eixo
        pm = eixo.interpolate(L / 2)
        a = eixo.interpolate(max(L / 2 - 0.5, 0.0))
        b = eixo.interpolate(min(L / 2 + 0.5, L))
        ang = math.atan2(b.y - a.y, b.x - a.x)
        ux, uy = math.cos(ang - math.pi / 2), math.sin(ang - math.pi / 2)
        dist = meia + altura_texto * 1.5
        pos = (pm.x + ux * dist, pm.y + uy * dist)
        rot, _ = _rot_legivel(math.degrees(ang))
        msp.add_text(f"{cod:02d}  {nome_parte}", height=altura_texto * 1.3, rotation=rot,
                     dxfattribs=txt).set_placement(pos, align=TextEntityAlignment.MIDDLE_CENTER)
        dist2 = dist + altura_texto * 2.2
        pos2 = (pm.x + ux * dist2, pm.y + uy * dist2)
        msp.add_text(f"L = {_br(L)} m   |   largura {_br(largura)} m",
                     height=altura_texto * 0.8, rotation=rot,
                     dxfattribs=txt).set_placement(pos2, align=TextEntityAlignment.MIDDLE_CENTER)

    return {"comprimento": comp_total, "area": area_total, "estacas": n_estacas}


# --------------------------------------------------------------------------- #
#  Conversão DXF -> DWG (ODA File Converter)
# --------------------------------------------------------------------------- #
def achar_oda(informado: str | None) -> Path | None:
    if informado:
        p = Path(informado)
        return p if p.exists() else None
    candidatos: list[Path] = []
    if os.name == "nt":
        def chave(p: Path):
            return tuple(int(n) for n in re.findall(r"\d+", p.parent.name))
        for base in (os.environ.get("ProgramFiles", r"C:\Program Files"),
                     os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")):
            achados = list(Path(base, "ODA").glob("ODAFileConverter*/ODAFileConverter.exe"))
            candidatos += sorted(achados, key=chave, reverse=True)
    elif sys.platform == "darwin":
        candidatos.append(Path("/Applications/ODAFileConverter.app/Contents/MacOS/ODAFileConverter"))
    else:
        w = shutil.which("ODAFileConverter")
        if w:
            candidatos.append(Path(w))
        candidatos.append(Path("/usr/bin/ODAFileConverter"))
    return next((c for c in candidatos if c.exists()), None)


def converter_para_dwg(oda: Path, pasta_dxf: Path, pasta_dwg: Path, versao: str) -> None:
    cmd = [str(oda), str(pasta_dxf), str(pasta_dwg), versao, "DWG", "0", "1", "*.dxf"]
    kw = {"capture_output": True, "timeout": 900}
    if os.name == "nt":
        si = subprocess.STARTUPINFO()
        si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        si.wShowWindow = 0  # SW_HIDE
        kw["startupinfo"] = si
    elif sys.platform.startswith("linux") and not os.environ.get("DISPLAY") and shutil.which("xvfb-run"):
        cmd = ["xvfb-run", "-a"] + cmd
    subprocess.run(cmd, check=False, **kw)


# --------------------------------------------------------------------------- #
#  Principal
# --------------------------------------------------------------------------- #
_PROIBIDOS = re.compile(r'[<>:"/\\|?*\x00-\x1f]')


def nome_arquivo(nome: str, usados: set[str]) -> str:
    base = _PROIBIDOS.sub("_", nome).strip(" .")[:80] or "Rua"
    cand, k = base, 2
    while cand.lower() in usados:
        cand = f"{base} ({k})"
        k += 1
    usados.add(cand.lower())
    return cand


def escolher_arquivo_gui() -> str | None:
    try:
        import tkinter as tk
        from tkinter import filedialog
        root = tk.Tk()
        root.withdraw()
        caminho = filedialog.askopenfilename(
            title="Escolha o KML/KMZ com os eixos das ruas",
            filetypes=[("Google Earth", "*.kml *.kmz"), ("Todos", "*.*")])
        root.destroy()
        return caminho or None
    except Exception:
        return None


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(
        description="Gera uma rua (eixo + bordos) para cada linha de um KML/KMZ e salva um DWG por rua.")
    ap.add_argument("entrada", nargs="?", help="arquivo .kml ou .kmz (vazio = abre janela)")
    ap.add_argument("-o", "--saida", help="pasta de saída (padrão: <nome do kml>_ruas)")
    ap.add_argument("--largura", type=float, default=7.0, help="largura da pista em metros (padrão 7)")
    ap.add_argument("--estaca", type=float, default=20.0, help="intervalo das estacas em m; 0 desliga (padrão 20)")
    ap.add_argument("--epsg", type=int, help="força o sistema de coordenadas (ex.: 31983 = SIRGAS 2000 / UTM 23S)")
    ap.add_argument("--altura-texto", type=float, default=1.5, help="altura base dos textos em m (padrão 1,5)")
    ap.add_argument("--codigo-inicial", type=int, default=1,
                    help="primeiro código das ruas (01, 02...); use p/ não repetir códigos já usados no projeto")
    ap.add_argument("--hachura", action="store_true", help="hachura cinza na pista")
    ap.add_argument("--geral", action="store_true", help="também gera um arquivo com todas as ruas juntas")
    ap.add_argument("--versao", default="ACAD2018", choices=VERSOES_DWG, help="versão do DWG (padrão ACAD2018)")
    ap.add_argument("--oda", help="caminho do ODAFileConverter, se não for achado sozinho")
    ap.add_argument("--manter-dxf", action="store_true", help="guarda os DXF numa subpasta 'dxf'")
    ap.add_argument("--so-dxf", action="store_true", help="não converte; entrega só DXF")
    a = ap.parse_args(argv)

    entrada = a.entrada or escolher_arquivo_gui()
    if not entrada:
        ap.print_help()
        return 1
    entrada = Path(entrada)
    if not entrada.exists():
        print(f"Arquivo não encontrado: {entrada}")
        return 1
    if a.largura <= 0:
        print("A largura precisa ser maior que zero.")
        return 1

    saida = Path(a.saida) if a.saida else entrada.with_name(f"{entrada.stem}_ruas")
    saida.mkdir(parents=True, exist_ok=True)

    # 1. leitura
    ruas, ignoradas = extrair_ruas(ler_kml_bytes(entrada))
    if not ruas:
        print("Nenhuma linha (LineString) encontrada no arquivo.")
        return 1

    # 2. projeção
    epsg, fusos = projetar(ruas, a.epsg)
    ruas = [r for r in ruas if r.partes_xy]
    print(f"{len(ruas)} rua(s) lida(s) de {entrada.name}  |  SRC: EPSG:{epsg}")
    if ignoradas:
        print(f"  {ignoradas} ponto(s)/polígono(s) ignorado(s) — só linhas viram rua.")
    if len(fusos) > 1 and a.epsg is None:
        print(f"  Atenção: as linhas cruzam fusos UTM {sorted(fusos)}; tudo foi projetado em EPSG:{epsg}.")

    usados: set[str] = set()
    cod = a.codigo_inicial
    for i, r in enumerate(ruas, 1):
        if not r.nome:
            r.nome = f"Rua {i:02d}"
        r.arquivo = nome_arquivo(r.nome, usados)
        r.codigos = list(range(cod, cod + len(r.partes_xy)))   # um código por eixo
        cod += len(r.partes_xy)

    # 3. desenho -> DXF
    converter = not a.so_dxf
    oda = achar_oda(a.oda) if converter else None
    if converter and oda is None:
        print("  ODA File Converter não encontrado: gerando DXF (abre direto no AutoCAD/Civil 3D).")
        print("  Para DWG, instale o ODA (gratuito) ou informe o caminho com --oda.")
        converter = False

    if converter:
        pasta_dxf = saida / "dxf" if a.manter_dxf else Path(tempfile.mkdtemp(prefix="ruas_dxf_"))
        pasta_dxf.mkdir(parents=True, exist_ok=True)
        for velho in pasta_dxf.glob("*.dxf"):
            velho.unlink()
    else:
        pasta_dxf = saida

    linhas_csv = []
    geral = novo_desenho(epsg, a.altura_texto) if a.geral else None
    for r in ruas:
        doc = novo_desenho(epsg, a.altura_texto)
        msp = doc.modelspace()
        info = desenhar_rua(msp, r, a.largura, a.estaca, a.altura_texto, a.hachura)
        zoom.extents(msp, factor=1.1)
        doc.saveas(pasta_dxf / f"{r.arquivo}.dxf")
        if geral is not None:
            desenhar_rua(geral.modelspace(), r, a.largura, a.estaca, a.altura_texto, a.hachura)
        ini, fim = r.partes_xy[0][0], r.partes_xy[-1][-1]
        linhas_csv.append([", ".join(f"{c:02d}" for c in r.codigos), r.nome, r.arquivo,
                           _br(info["comprimento"]), _br(a.largura),
                           _br(info["area"]), info["estacas"],
                           _br(ini[0], 3), _br(ini[1], 3), _br(fim[0], 3), _br(fim[1], 3)])
        print(f"  {r.codigos[0]:02d}  {r.nome:<36s} L = {_br(info['comprimento']):>10s} m   área = {_br(info['area']):>11s} m²")

    if geral is not None:
        zoom.extents(geral.modelspace(), factor=1.05)
        geral.saveas(pasta_dxf / "_TODAS_AS_RUAS.dxf")

    # 4. DXF -> DWG
    ext = "dxf"
    if converter:
        print(f"Convertendo para DWG ({a.versao}) com {oda} ...")
        converter_para_dwg(oda, pasta_dxf, saida, a.versao)
        esperados = [r.arquivo for r in ruas] + (["_TODAS_AS_RUAS"] if geral is not None else [])
        faltando = [n for n in esperados if not (saida / f"{n}.dwg").exists()]
        if faltando:
            print(f"  O ODA não gerou {len(faltando)} arquivo(s); copiando os DXF correspondentes:")
            for n in faltando:
                shutil.copy2(pasta_dxf / f"{n}.dxf", saida / f"{n}.dxf")
                print(f"    {n}.dxf")
        ext = "dwg"
        if not a.manter_dxf:
            shutil.rmtree(pasta_dxf, ignore_errors=True)

    for ln in linhas_csv:
        ln[2] = f"{ln[2]}.{ext}" if (saida / f"{ln[2]}.{ext}").exists() else f"{ln[2]}.dxf"
    with open(saida / "resumo_ruas.csv", "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f, delimiter=";")
        w.writerow(["Código", "Rua", "Arquivo", "Comprimento eixo (m)", "Largura (m)", "Área pista (m²)",
                    "Estacas", "Início E", "Início N", "Fim E", "Fim N"])
        w.writerows(linhas_csv)

    print(f"Pronto: {len(ruas)} arquivo(s) em {saida.resolve()}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
