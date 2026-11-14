"""Modelo digital de terreno em grade regular e leitores de arquivo.

Convenções:
- Coordenadas em metros, x = E, y = N (projeção plana, ex.: SIRGAS 2000 / UTM).
- z[i, j]: linha i cresce para o norte (linha 0 = sul), coluna j cresce para leste.
- (x0, y0) é o CENTRO da célula (0, 0). Fora do terreno: NaN.
"""
from __future__ import annotations

import math
import xml.etree.ElementTree as ET
from dataclasses import dataclass

import numpy as np
from scipy import ndimage


@dataclass
class Terreno:
    x0: float
    y0: float
    dx: float
    z: np.ndarray
    nome: str = "terreno"

    @property
    def ny(self) -> int:
        return self.z.shape[0]

    @property
    def nx(self) -> int:
        return self.z.shape[1]

    @property
    def valido(self) -> np.ndarray:
        return np.isfinite(self.z)

    def xy(self, i, j):
        return self.x0 + np.asarray(j) * self.dx, self.y0 + np.asarray(i) * self.dx

    def ij(self, x: float, y: float) -> tuple[int, int]:
        return int(round((y - self.y0) / self.dx)), int(round((x - self.x0) / self.dx))

    def dentro(self, i: int, j: int) -> bool:
        return 0 <= i < self.ny and 0 <= j < self.nx

    def cota(self, x, y, grade: np.ndarray | None = None):
        """Cota interpolada (bilinear) em pontos quaisquer. NaN fora do terreno."""
        g = self.z if grade is None else grade
        x = np.asarray(x, float)
        y = np.asarray(y, float)
        fi = (y - self.y0) / self.dx
        fj = (x - self.x0) / self.dx
        return ndimage.map_coordinates(g, [np.atleast_1d(fi), np.atleast_1d(fj)], order=1,
                                       mode="constant", cval=np.nan).reshape(np.shape(x))

    def suavizar(self, sigma_m: float) -> np.ndarray:
        """Gaussiana com normalização (ignora NaN). sigma em metros."""
        s = max(sigma_m / self.dx, 1e-6)
        m = self.valido.astype(float)
        zf = np.where(self.valido, self.z, 0.0)
        num = ndimage.gaussian_filter(zf, s, mode="nearest")
        den = ndimage.gaussian_filter(m, s, mode="nearest")
        with np.errstate(invalid="ignore", divide="ignore"):
            out = num / den
        out[~self.valido] = np.nan
        return out

    def gradiente(self, grade: np.ndarray | None = None):
        """(dz/dx, dz/dy) em m/m."""
        g = self.z if grade is None else grade
        gf = np.where(np.isfinite(g), g, np.nanmean(g))
        gy, gx = np.gradient(gf, self.dx)
        return gx, gy


# ---------------------------------------------------------------- leitores

def _sem_ns(tag: str) -> str:
    return tag.split("}", 1)[-1]


def ler_landxml(caminho: str, dx: float, superficie: str | None = None) -> Terreno:
    """Lê uma superfície TIN de LandXML (Civil 3D: Exportar para LandXML) e rasteriza.

    Pontos em LandXML vêm como "N E Z".
    """
    from matplotlib.tri import LinearTriInterpolator, Triangulation

    # leitura em fluxo: arquivos do Civil 3D com tudo marcado passam de centenas de MB
    ids, xs, ys, zs = [], [], [], []
    faces = []
    dentro = achou = False
    nome_sup = None
    ctx = ET.iterparse(caminho, events=("start", "end"))
    _, raiz = next(ctx)
    for k, (ev, el) in enumerate(ctx):
        t = _sem_ns(el.tag)
        if ev == "start":
            if t == "Surface" and (superficie is None or el.get("name", "") == superficie):
                dentro, nome_sup = True, el.get("name", "")
            continue
        if dentro:
            if t == "P":
                n, e, z = (float(v) for v in el.text.split()[:3])
                ids.append(el.get("id"))
                xs.append(e)
                ys.append(n)
                zs.append(z)
            elif t == "F" and el.get("i") != "1":
                faces.append(el.text.split()[:3])
            elif t == "Surface":
                achou = True
                break
        el.clear()
        if k % 200000 == 0:
            raiz.clear()
    if not achou or not xs:
        raise ValueError(f"Superfície {superficie!r} não encontrada em {caminho}")
    idx = {pid: k for k, pid in enumerate(ids)}
    xs, ys, zs = np.array(xs), np.array(ys), np.array(zs)
    if faces:
        tri = np.array([[idx[a], idx[b], idx[c]] for a, b, c in faces], dtype=int)
    else:  # exportado só com pontos: triangula aqui (Delaunay)
        tri = Triangulation(xs, ys).triangles

    x0 = math.floor(xs.min() / dx) * dx
    y0 = math.floor(ys.min() / dx) * dx
    nx = int(math.ceil((xs.max() - x0) / dx)) + 1
    ny = int(math.ceil((ys.max() - y0) / dx)) + 1
    gx, gy = np.meshgrid(x0 + np.arange(nx) * dx, y0 + np.arange(ny) * dx)
    interp = LinearTriInterpolator(Triangulation(xs, ys, tri), zs)
    z = np.ma.filled(interp(gx, gy), np.nan).astype(float)
    return Terreno(x0, y0, dx, z, nome=nome_sup or "LandXML")


def ler_asc(caminho: str) -> Terreno:
    """ESRI ASCII Grid (.asc)."""
    cab = {}
    with open(caminho, encoding="utf-8") as f:
        for _ in range(6):
            pos = f.tell()
            linha = f.readline().split()
            if not linha or not linha[0][0].isalpha():
                f.seek(pos)
                break
            cab[linha[0].lower()] = float(linha[1])
        dados = np.loadtxt(f)
    dx = cab["cellsize"]
    nd = cab.get("nodata_value", -9999)
    z = np.where(dados == nd, np.nan, dados)[::-1].astype(float)
    x0 = cab["xllcenter"] if "xllcenter" in cab else cab["xllcorner"] + dx / 2
    y0 = cab["yllcenter"] if "yllcenter" in cab else cab["yllcorner"] + dx / 2
    return Terreno(x0, y0, dx, z, nome=caminho)


def ler_xyz(caminho: str, dx: float) -> Terreno:
    """Pontos E,N,Z (CSV ou espaço), regulares ou dispersos; interpolação linear."""
    from scipy.interpolate import griddata

    txt = open(caminho, encoding="utf-8").read().replace(";", " ").replace(",", " ")
    linhas = [l.split() for l in txt.splitlines() if l.strip()]
    nums = []
    for l in linhas:
        try:
            nums.append([float(v) for v in l[:3]])
        except ValueError:
            continue  # cabeçalho
    a = np.array(nums)
    x0 = math.floor(a[:, 0].min() / dx) * dx
    y0 = math.floor(a[:, 1].min() / dx) * dx
    nx = int(math.ceil((a[:, 0].max() - x0) / dx)) + 1
    ny = int(math.ceil((a[:, 1].max() - y0) / dx)) + 1
    gx, gy = np.meshgrid(x0 + np.arange(nx) * dx, y0 + np.arange(ny) * dx)
    z = griddata(a[:, :2], a[:, 2], (gx, gy), method="linear")
    return Terreno(x0, y0, dx, z.astype(float), nome=caminho)


def terreno_sintetico(dx: float = 10.0, largura: float = 1600.0, altura: float = 1100.0,
                      x0: float = 703000.0, y0: float = 7668000.0, semente: int = 7) -> Terreno:
    """Relevo ondulado de teste: encosta regional, espigão N-S com sela ao sul,
    um vale encaixado e morrotes aleatórios."""
    rng = np.random.default_rng(semente)
    nx = int(largura / dx) + 1
    ny = int(altura / dx) + 1
    X, Y = np.meshgrid(np.arange(nx) * dx, np.arange(ny) * dx)
    z = 700.0 + 0.015 * X + 0.01 * Y
    # espigão N-S em x ~ 850 m, 55 m de altura, com sela em y ~ 230 m
    alt_esp = 55.0 - 38.0 * np.exp(-((Y - 230.0) / 120.0) ** 2)
    z += alt_esp * np.exp(-((X - 850.0) / 140.0) ** 2)
    # vale encaixado NE-SW cortando a metade oeste
    d = (X - 0.55 * Y - 330.0) / math.hypot(1, 0.55)
    z -= 14.0 * np.exp(-(d / 35.0) ** 2)
    # morrotes
    for _ in range(14):
        cx, cy = rng.uniform(0, largura), rng.uniform(0, altura)
        r, h = rng.uniform(60, 160), rng.uniform(-10, 18)
        z += h * np.exp(-(((X - cx) ** 2 + (Y - cy) ** 2) / r ** 2))
    z += ndimage.gaussian_filter(rng.normal(0, 1.2, z.shape), 1.5)
    return Terreno(x0, y0, dx, z, nome="sintético")
