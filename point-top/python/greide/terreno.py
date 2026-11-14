"""Leitura do levantamento da pista e amostragem das cotas nas seções.

Formatos aceitos:
- pontos em CSV/TXT (P, N, E, Z, D em qualquer ordem, com ou sem cabeçalho);
- superfície TIN em LandXML (exportada do Civil 3D);
- tabela de seções em CSV: estaca; le55; le35; eixo; ld35; ld55.
"""
import csv
import re
import xml.etree.ElementTree as ET

import numpy as np

_NUM = re.compile(r"^[+-]?\d+([.,]\d+)?([eE][+-]?\d+)?$")


def _delimitador(linhas):
    amostra = "\n".join(linhas[:30])
    for d in (";", "\t", "|"):
        if d in amostra:
            return d
    # Vírgula só separa colunas se não for decimal: precisa gerar ao menos 3 colunas.
    if "," in amostra:
        partes = [len(l.split(",")) for l in linhas[:30] if l.strip()]
        if partes and min(partes) >= 3:
            return ","
    return None


def _ler_tabela(caminho):
    with open(caminho, "r", encoding="utf-8-sig", errors="replace", newline="") as f:
        linhas = [l.rstrip("\r\n") for l in f if l.strip()]
    if not linhas:
        raise ValueError(f"Arquivo vazio: {caminho}")
    d = _delimitador(linhas)
    if d is None:
        linhas_col = [l.split() for l in linhas]
    else:
        linhas_col = [[c.strip() for c in r] for r in csv.reader(linhas, delimiter=d)]
    return linhas_col


def _num(txt):
    txt = txt.strip()
    if not _NUM.match(txt):
        return None
    return float(txt.replace(",", "."))


def _colunas_pontos(cab, dados):
    """Descobre as colunas N, E e Z pelo cabeçalho ou, sem ele, pela grandeza."""
    if cab:
        nomes = [c.strip().lower() for c in cab]

        def achar(opcoes):
            for i, c in enumerate(nomes):
                if c in opcoes:
                    return i
            return None

        n = achar({"n", "norte", "north", "northing", "y", "coord_n", "coordn"})
        e = achar({"e", "este", "leste", "east", "easting", "x", "coord_e", "coorde"})
        z = achar({"z", "cota", "h", "elev", "elevacao", "elevação", "altitude", "elevation"})
        if None not in (n, e, z):
            return n, e, z

    med = np.nanmedian(dados, axis=0)
    n_cand = [i for i, m in enumerate(med) if 1e6 <= abs(m) < 1.1e7]
    e_cand = [i for i, m in enumerate(med) if 1e5 <= abs(m) < 1e6]
    if not n_cand or not e_cand:
        raise ValueError(
            "Não identifiquei as colunas Norte e Este. Use cabeçalho N;E;Z "
            "ou coordenadas UTM completas."
        )
    n, e = n_cand[0], e_cand[0]
    z_cand = [
        i
        for i, m in enumerate(med)
        if i not in (n, e) and -500 < m < 9000 and not np.allclose(dados[:, i], np.round(dados[:, i]))
    ]
    if not z_cand:
        raise ValueError("Não identifiquei a coluna de cota. Use cabeçalho N;E;Z.")
    # Em PNEZD a cota vem depois das coordenadas.
    depois = [i for i in z_cand if i > max(n, e)]
    return n, e, (depois[0] if depois else z_cand[-1])


def _ler_pontos(caminho):
    linhas = _ler_tabela(caminho)
    cab = None
    if sum(_num(c) is not None for c in linhas[0]) < 3:
        cab = linhas[0]
        linhas = linhas[1:]
    ncol = max(len(l) for l in linhas[:200])
    dados = []
    descartadas = 0
    for l in linhas:
        v = [(_num(c) if i < len(l) else None) for i, c in enumerate(l[:ncol])]
        v += [None] * (ncol - len(v))
        if sum(x is not None for x in v) < 3:
            descartadas += 1
            continue
        dados.append([np.nan if x is None else x for x in v])
    if len(dados) < 3:
        raise ValueError("O arquivo de pontos tem menos de 3 pontos válidos.")
    dados = np.array(dados, dtype=float)
    # Colunas só de texto (ex.: descrição) viram NaN e saem da análise.
    usaveis = [i for i in range(dados.shape[1]) if np.isfinite(dados[:, i]).mean() > 0.9]
    cab_u = [cab[i] if cab and i < len(cab) else "" for i in usaveis] if cab else None
    d = dados[:, usaveis]
    n, e, z = _colunas_pontos(cab_u, d)
    xyz = d[:, [e, n, z]]
    xyz = xyz[np.isfinite(xyz).all(axis=1)]
    return xyz, descartadas


class _Tin:
    def __init__(self, xyz, triangulos, localizador, aresta_max, x0, y0, descricao):
        self.xyz = xyz
        self.tri = triangulos
        self.localizar = localizador
        self.aresta_max = aresta_max
        self.x0 = x0
        self.y0 = y0
        self.descricao = descricao

    def cotas(self, e, n):
        forma = np.shape(e)
        x = np.ravel(e) - self.x0
        y = np.ravel(n) - self.y0
        i = np.asarray(self.localizar(x, y), dtype=int)
        z = np.full(x.shape, np.nan)
        ok = i >= 0
        if not ok.any():
            return z.reshape(forma)
        t = self.tri[i[ok]]
        a, b, c = self.xyz[t[:, 0]], self.xyz[t[:, 1]], self.xyz[t[:, 2]]
        det = (b[:, 1] - c[:, 1]) * (a[:, 0] - c[:, 0]) + (c[:, 0] - b[:, 0]) * (a[:, 1] - c[:, 1])
        with np.errstate(divide="ignore", invalid="ignore"):
            l1 = ((b[:, 1] - c[:, 1]) * (x[ok] - c[:, 0]) + (c[:, 0] - b[:, 0]) * (y[ok] - c[:, 1])) / det
            l2 = ((c[:, 1] - a[:, 1]) * (x[ok] - c[:, 0]) + (a[:, 0] - c[:, 0]) * (y[ok] - c[:, 1])) / det
        zi = l1 * a[:, 2] + l2 * b[:, 2] + (1 - l1 - l2) * c[:, 2]
        aresta = np.maximum.reduce(
            [
                np.hypot(*(a - b)[:, :2].T),
                np.hypot(*(b - c)[:, :2].T),
                np.hypot(*(c - a)[:, :2].T),
            ]
        )
        # Triângulos muito finos na borda do levantamento dão cotas falsas: ficam sem dado.
        altura = np.abs(det) / np.maximum(aresta, 1e-9)
        zi[(aresta > self.aresta_max) | (altura < 0.02) | ~np.isfinite(zi)] = np.nan
        z[ok] = zi
        return z.reshape(forma)


def _tin_de_pontos(xyz, aresta_max, descricao):
    from scipy.spatial import Delaunay, QhullError

    # Pontos repetidos (mesmo E, N) atrapalham a triangulação.
    _, unicos = np.unique(np.round(xyz[:, :2], 3), axis=0, return_index=True)
    xyz = xyz[np.sort(unicos)]
    x0, y0 = xyz[:, 0].mean(), xyz[:, 1].mean()
    p = xyz.copy()
    p[:, 0] -= x0
    p[:, 1] -= y0
    try:
        tri = Delaunay(p[:, :2])
    except QhullError as exc:
        raise ValueError(f"Não consegui triangular os pontos: {exc}") from exc
    return _Tin(p, tri.simplices, lambda a, b: tri.find_simplex(np.column_stack([a, b])),
                aresta_max, x0, y0, descricao)


def _tin_landxml(caminho, aresta_max):
    from matplotlib.tri import Triangulation

    ids, coords, faces = [], [], []
    nome = None
    for _, el in ET.iterparse(caminho, events=("end",)):
        t = el.tag.split("}")[-1]
        if t == "P" and el.text:
            partes = el.text.split()
            if len(partes) >= 3 and el.get("id") is not None:
                ids.append(int(el.get("id")))
                n, e, z = map(float, partes[:3])
                coords.append((e, n, z))
        elif t == "F" and el.text:
            if el.get("i") != "1":
                faces.append(tuple(int(v) for v in el.text.split()[:3]))
        elif t == "Surface":
            nome = el.get("name")
            el.clear()
    if not coords:
        return None
    xyz = np.array(coords, dtype=float)
    if not faces:
        return _tin_de_pontos(xyz, aresta_max, "superfície LandXML (sem faces, retriangulada)")
    mapa = np.full(max(ids) + 1, -1, dtype=int)
    mapa[np.array(ids)] = np.arange(len(ids))
    tri = mapa[np.array(faces)]
    tri = tri[(tri >= 0).all(axis=1)]
    x0, y0 = xyz[:, 0].mean(), xyz[:, 1].mean()
    p = xyz.copy()
    p[:, 0] -= x0
    p[:, 1] -= y0
    try:
        malha = Triangulation(p[:, 0], p[:, 1], tri)
        loc = malha.get_trifinder()
    except Exception:
        return _tin_de_pontos(xyz, aresta_max, f"superfície LandXML {nome} (retriangulada)")
    return _Tin(p, tri, lambda a, b: loc(a, b), aresta_max, x0, y0, f"superfície LandXML {nome}")


def _ler_secoes(caminho):
    """Tabela: estaca; le55; le35; eixo; ld35; ld55. Devolve None se não for esse formato."""
    linhas = _ler_tabela(caminho)
    cab = [c.strip().lower().replace(",", "").replace(".", "").replace("_", "") for c in linhas[0]]
    chaves = ["estaca", "le55", "le35", "eixo", "ld35", "ld55"]
    if not all(k in cab for k in chaves):
        return None
    col = [cab.index(k) for k in chaves]
    out = []
    for l in linhas[1:]:
        txt = l[col[0]].strip()
        if "+" in txt:
            km, m = txt.split("+")
            sta = float(km) * 1000 + float(m.replace(",", "."))
        else:
            sta = _num(txt)
        if sta is None:
            continue
        linha = [sta]
        for c in col[1:]:
            v = _num(l[c]) if c < len(l) else None
            linha.append(np.nan if v is None else v)
        out.append(linha)
    if len(out) < 2:
        raise ValueError("A tabela de seções tem menos de 2 estacas válidas.")
    a = np.array(out, dtype=float)
    return a[np.argsort(a[:, 0])]


class Levantamento:
    """Fonte de cotas: TIN (pontos/superfície) ou tabela de seções."""

    def __init__(self, caminho, aresta_max=50.0):
        self.caminho = str(caminho)
        self.secoes = None
        self.tin = None
        minusc = self.caminho.lower()
        if minusc.endswith(".xml"):
            self.tin = _tin_landxml(self.caminho, aresta_max)
            if self.tin is None:
                raise ValueError(
                    "Esse XML não tem superfície (<Surface> com pontos). O arquivo do eixo não "
                    "serve como levantamento."
                )
            self.descricao = f"{self.tin.descricao}: {len(self.tin.xyz)} pontos"
        else:
            self.secoes = _ler_secoes(self.caminho)
            if self.secoes is not None:
                self.descricao = f"tabela de seções: {len(self.secoes)} estacas"
            else:
                xyz, descartadas = _ler_pontos(self.caminho)
                self.tin = _tin_de_pontos(xyz, aresta_max, "pontos")
                self.descricao = f"pontos: {len(xyz)} lidos"
                if descartadas:
                    self.descricao += f", {descartadas} linhas ignoradas"

    def amostrar(self, eixo, estacas, afastamentos):
        if self.tin is not None:
            e, n = eixo.deslocados(estacas, afastamentos)
            return self.tin.cotas(e, n)
        if len(afastamentos) != 5:
            raise ValueError("A tabela de seções só serve para a seção com 5 pontos.")
        s = self.secoes
        z = np.column_stack(
            [np.interp(estacas, s[:, 0], s[:, c], left=np.nan, right=np.nan) for c in range(1, 6)]
        )
        return z
