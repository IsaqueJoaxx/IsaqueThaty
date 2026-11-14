"""Conferências independentes do protótipo.  Uso: python testes.py [pasta_saida]"""
import math
import sys
import xml.etree.ElementTree as ET

import numpy as np

from tracado import geometria, greide
from tracado.terreno import Terreno


def teste_secao_plana():
    # terreno plano em z=0, plataforma 8 m, talude corte 1:1, aterro 1,5:1
    T = Terreno(0.0, 0.0, 1.0, np.zeros((200, 200)))
    s = np.array([0.0]); x = np.array([100.0]); y = np.array([100.0])
    ux = np.array([1.0]); uy = np.array([0.0])
    c = greide.secoes(T, s, x, y, ux, uy, np.array([-2.0]), 8.0, 1.0, 1.5, 40)[0]
    a = greide.secoes(T, s, x, y, ux, uy, np.array([+2.0]), 8.0, 1.0, 1.5, 40)[0]
    assert abs(c["corte"] - (8 * 2 + 1.0 * 4)) < 0.05 and c["aterro"] < 1e-9, c
    assert abs(a["aterro"] - (8 * 2 + 1.5 * 4)) < 0.05 and a["corte"] < 1e-9, a
    assert abs(c["pe_esq"] - 6.0) < 0.01 and abs(a["pe_dir"] - 7.0) < 0.01
    print("ok  seção em terreno plano: corte 20,0 m² / aterro 22,0 m², pés a 6,0 e 7,0 m")


def teste_secao_mista():
    # terreno com declividade transversal 20% e plataforma na cota do eixo: seção mista
    yy, xx = np.indices((200, 200)).astype(float)
    T = Terreno(0.0, 0.0, 1.0, 0.2 * (yy - 100))  # sobe para o norte (esquerda de quem vai para leste)
    r = greide.secoes(T, np.array([0.0]), np.array([100.0]), np.array([100.0]),
                      np.array([1.0]), np.array([0.0]), np.array([0.0]), 8.0, 1.0, 1.5, 40)[0]
    # metade esquerda: triângulo na plataforma 4*0,8/2 = 1,6 + talude de corte: pé onde 0,2y = (y-4)/1 → y=5
    corte_esp = 1.6 + 0.5 * 1.0 * 0.8
    # metade direita: 1,6 + talude de aterro: -0,2y = -(y-4)/1,5 → y = 4/(1-0,3)= 5,714
    ye = 4 / (1 - 0.2 * 1.5)
    aterro_esp = 1.6 + 0.5 * (ye - 4) * 0.8
    assert abs(r["corte"] - corte_esp) < 0.03, (r, corte_esp)
    assert abs(r["aterro"] - aterro_esp) < 0.03, (r, aterro_esp)
    print(f"ok  seção mista 20%: corte {r['corte']:.3f} (esperado {corte_esp:.3f}), "
          f"aterro {r['aterro']:.3f} (esperado {aterro_esp:.3f}) m²")


def teste_greide():
    s = np.arange(0, 1001, 5.0)
    z = 100 + 30 * np.sin(s / 120) + 0.02 * s
    p, _ = greide.greide_expedito(s, z, 0.06, 15)
    r = np.abs(np.diff(p) / np.diff(s))
    assert r.max() <= 0.06 + 1e-6, r.max()
    assert abs(p[0] - z[0]) < 1e-9 and abs(p[-1] - z[-1]) < 0.05
    print(f"ok  greide: rampa máx. {r.max() * 100:.3f}% (limite 6%), pontas presas "
          f"(erro no fim {abs(p[-1] - z[-1]) * 100:.1f} cm)")


def teste_curvas():
    P = np.array([[0, 0], [100, 0], [130, 25], [260, 40], [300, 160], [420, 160]], float)
    Q, R, av = geometria.ajustar_curvas(P, 60, 120, tangente_min=20)
    al = geometria.construir("t", Q, R)
    curvas = [e for e in al.elementos if e.tipo == "curva"]
    for a, b in zip(al.elementos, al.elementos[1:]):
        assert not (a.tipo == "curva" and b.tipo == "curva"), "curvas sem tangente entre elas"
    for e in al.elementos[1:-1]:
        if e.tipo == "reta":
            assert e.comprimento >= 20 - 1e-6, e.comprimento
    assert min(R) >= 60 - 1e-9
    for a, b in zip(al.elementos, al.elementos[1:]):
        assert np.allclose(a.p1, b.p0, atol=1e-6)
    for e in al.elementos:
        if e.tipo == "curva":
            assert abs(math.dist(e.centro, e.p0) - e.raio) < 1e-6
            assert abs(math.dist(e.centro, e.p1) - e.raio) < 1e-6
    s, x, y, ux, uy = al.amostrar(1.0)
    passos = np.hypot(np.diff(x), np.diff(y))
    assert np.all(passos <= np.diff(s) + 1e-6) and abs(s[-1] - al.comprimento) < 1e-6
    print(f"ok  curvas: {len(Q)} PIs, raios {', '.join(f'{r:.0f}' for r in R)} m, "
          f"elementos contínuos, extensão {al.comprimento:.2f} m {'; ' + '; '.join(av) if av else ''}")


def teste_landxml(arquivo):
    raiz = ET.parse(arquivo).getroot()
    ns = {"l": "http://www.landxml.org/schema/LandXML-1.2"}
    for al in raiz.iterfind(".//l:Alignment", ns):
        ant = None
        soma = 0.0
        for el in al.find("l:CoordGeom", ns):
            n0, e0 = map(float, el.find("l:Start", ns).text.split())
            n1, e1 = map(float, el.find("l:End", ns).text.split())
            if ant is not None:
                assert math.hypot(n0 - ant[0], e0 - ant[1]) < 1e-3, "elementos descontínuos"
            ant = (n1, e1)
            L = float(el.get("length"))
            soma += L
            if el.tag.endswith("Curve"):
                nc, ec = map(float, el.find("l:Center", ns).text.split())
                Rr = float(el.get("radius"))
                assert abs(math.hypot(n0 - nc, e0 - ec) - Rr) < 1e-3
                assert abs(math.hypot(n1 - nc, e1 - ec) - Rr) < 1e-3
                assert abs(Rr * math.radians(float(el.get("delta"))) - L) < 1e-3
            else:
                assert abs(math.hypot(n1 - n0, e1 - e0) - L) < 1e-3
        assert abs(soma - float(al.get("length"))) < 1e-2
        pv = [list(map(float, p.text.split())) for p in al.iterfind(".//l:PVI", ns)]
        rampas = [abs((b[1] - a[1]) / (b[0] - a[0])) for a, b in zip(pv, pv[1:])]
        print(f"ok  LandXML {al.get('name')}: {soma:.2f} m contínuos, {len(pv)} PVIs, "
              f"rampa máx. entre PVIs {max(rampas) * 100:.2f}%")


if __name__ == "__main__":
    teste_secao_plana()
    teste_secao_mista()
    teste_greide()
    teste_curvas()
    if len(sys.argv) > 1:
        teste_landxml(sys.argv[1] + "/tracados.xml")
