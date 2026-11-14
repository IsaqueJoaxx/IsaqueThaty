"""Gera um exemplo SINTÉTICO completo: eixo, levantamento e perfil para IRI. Nada aqui vem de obra."""
import math
from pathlib import Path

import numpy as np

from geometria import Eixo

XML = """<?xml version="1.0" encoding="utf-8"?>
<LandXML xmlns="http://www.landxml.org/schema/LandXML-1.2" version="1.2">
<Alignments><Alignment name="EIXO_SINTETICO" length="{L:.3f}" staStart="0">
<CoordGeom>
<Line length="800"><Start>{n0:.4f} {e0:.4f}</Start><End>{n1:.4f} {e1:.4f}</End></Line>
<Curve rot="cw" length="{la:.4f}" radius="{r:.1f}"><Start>{n1:.4f} {e1:.4f}</Start><Center>{nc:.4f} {ec:.4f}</Center><End>{n2:.4f} {e2:.4f}</End></Curve>
<Line length="800"><Start>{n2:.4f} {e2:.4f}</Start><End>{n3:.4f} {e3:.4f}</End></Line>
</CoordGeom>
</Alignment></Alignments>
</LandXML>
"""
ESTACA_PICO = 600.0
AFASTAMENTOS = np.array([-7.5, -5.5, -3.5, -2.2, 0.0, 2.2, 3.5, 5.5, 7.5])


def _cota(sta, af):
    z = 500.0 + 6.0 * np.sin(sta / 900.0) + 1.5 * np.sin(sta / 240.0 + 1.0)
    z = z + 0.03 * np.sin(sta / 31.0) + 0.012 * np.sin(sta / 11.0 + 0.7)
    pista = np.minimum(np.abs(af), 3.5)
    z = z - 0.024 * pista - 0.012 * np.exp(-(((np.abs(af) - 2.2) / 0.5) ** 2))
    fora = np.clip(np.abs(af) - 3.5, 0, None)
    return z - 0.10 * fora


def gerar(pasta):
    pasta = Path(pasta)
    pasta.mkdir(parents=True, exist_ok=True)
    e0, n0 = 600000.0, 7800000.0
    r = 500.0
    la = 400.0
    e1, n1 = e0 + 800.0, n0
    ec, nc = e1, n1 - r
    ang = la / r
    e2, n2 = ec + r * math.sin(ang), nc + r * math.cos(ang)
    te, tn = math.cos(ang), -math.sin(ang)
    e3, n3 = e2 + 800.0 * te, n2 + 800.0 * tn
    eixo = pasta / "eixo_SINTETICO.xml"
    eixo.write_text(XML.format(L=1600.0 + la, la=la, r=r, n0=n0, e0=e0, n1=n1, e1=e1, nc=nc, ec=ec, n2=n2, e2=e2, n3=n3, e3=e3), encoding="utf-8")

    ex = Eixo(eixo)
    estacas = ex.grade(5.0)
    e, n = ex.deslocados(estacas, AFASTAMENTOS)
    z = _cota(estacas[:, None], AFASTAMENTOS[None, :])
    gerador = np.random.default_rng(11)
    z = z + gerador.normal(0, 0.002, z.shape)
    # Erro de levantamento proposital: um ponto do eixo 8 cm acima da pista.
    i = int(np.searchsorted(estacas, ESTACA_PICO))
    z[i, int(np.argmin(np.abs(AFASTAMENTOS)))] += 0.08
    lev = pasta / "levantamento_SINTETICO.csv"
    with lev.open("w", encoding="utf-8") as f:
        f.write("P;N;E;Z;D\n")
        pid = 1
        for a in range(len(estacas)):
            for b in range(len(AFASTAMENTOS)):
                f.write(f"{pid};{n[a, b]:.3f};{e[a, b]:.3f};{z[a, b]:.3f};SINTETICO\n")
                pid += 1

    # Perfil denso: ruído branco integrado, que tem a forma espectral típica de pavimento.
    dx = 0.25
    x = np.arange(0.0, 1000.0, dx)
    linhas = ["distancia;LE;LD"]
    trilhas = [np.cumsum(gerador.normal(0, 0.00028, x.size)) for _ in range(2)]
    tendencia = 2.0 * np.sin(x / 300.0)
    for k in range(x.size):
        linhas.append(f"{x[k]:.2f};{500 + tendencia[k] + trilhas[0][k]:.5f};{500 + tendencia[k] + trilhas[1][k]:.5f}")
    perfil = pasta / "perfil_IRI_SINTETICO.csv"
    perfil.write_text("\n".join(linhas) + "\n", encoding="utf-8")
    return eixo, lev, perfil
