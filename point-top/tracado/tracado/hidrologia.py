"""Drenagem a partir do MDT: preenchimento de depressões (priority-flood),
direção D8 e área de contribuição. Serve para localizar talvegues e contar
travessias (bueiro/ponte) — é indicador de estudo, não hidrologia de projeto."""
from __future__ import annotations

import heapq
import math

import numpy as np

_VIZ = [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]


def preencher(z: np.ndarray, eps: float = 1e-4) -> np.ndarray:
    ny, nx = z.shape
    ok = np.isfinite(z)
    zf = np.where(ok, z, np.inf)
    fechado = ~ok
    heap = []
    for i in range(ny):
        for j in range(nx):
            if not ok[i, j]:
                continue
            borda = i in (0, ny - 1) or j in (0, nx - 1)
            if not borda:
                for di, dj in _VIZ:
                    if not ok[i + di, j + dj]:
                        borda = True
                        break
            if borda:
                heapq.heappush(heap, (zf[i, j], i, j))
                fechado[i, j] = True
    while heap:
        zc, i, j = heapq.heappop(heap)
        for di, dj in _VIZ:
            a, b = i + di, j + dj
            if 0 <= a < ny and 0 <= b < nx and not fechado[a, b]:
                fechado[a, b] = True
                if zf[a, b] <= zc:
                    zf[a, b] = zc + eps
                heapq.heappush(heap, (zf[a, b], a, b))
    zf[~ok] = np.nan
    return zf


def area_contribuicao(z: np.ndarray, dx: float) -> np.ndarray:
    """Área de contribuição D8 em m² por célula."""
    zf = preencher(z)
    ny, nx = zf.shape
    ok = np.isfinite(zf)
    recv = np.full(ny * nx, -1, dtype=np.int64)
    melhor = np.zeros((ny, nx))
    zp = np.pad(np.where(ok, zf, np.inf), 1, constant_values=np.inf)
    for di, dj in _VIZ:
        viz = zp[1 + di:1 + di + ny, 1 + dj:1 + dj + nx]
        dec = (zf - viz) / (dx * math.hypot(di, dj))
        m = ok & np.isfinite(viz) & (dec > melhor)
        melhor[m] = dec[m]
        ii, jj = np.nonzero(m)
        recv[ii * nx + jj] = (ii + di) * nx + (jj + dj)
    acc = np.where(ok, dx * dx, 0.0).ravel()
    ordem = np.argsort(-np.where(ok, zf, -np.inf).ravel(), kind="stable")
    for c in ordem:
        r = recv[c]
        if r >= 0:
            acc[r] += acc[c]
    return acc.reshape(ny, nx)
