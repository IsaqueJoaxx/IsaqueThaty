#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
greide_auto.py — Geração automática de greide (perfil de projeto)
=================================================================

Estratégia: "seguir o terreno respeitando a rampa máxima".
A linha de greide parte de uma cota inicial informada e acompanha o perfil
natural do terreno por trechos retos (tangentes), com a rampa de cada trecho
ajustada por mínimos quadrados ao terreno e truncada na rampa máxima.
Nos PIVs entram curvas verticais parabólicas simétricas dimensionadas pelo
K mínimo da velocidade diretriz.

Entradas aceitas
----------------
  * CSV do terreno:  estaca/progressiva ; cota      (ver --csv)
  * LandXML exportado do Civil 3D (alinhamento + perfil de terreno)

Saídas
------
  * <saida>_pvis.csv       lista de PVIs (estaca, cota, rampa entrada/saída, A, K, L)
  * <saida>_pvis.txt       formato "estação cota" para o comando
                           "Create Profile from File" do Civil 3D (só tangentes)
  * <saida>_greide.xml     LandXML com o perfil de projeto (tangentes + curvas)
  * <saida>_relatorio.txt  memória de cálculo, verificações e corte/aterro
  * <saida>_greide.png     gráfico terreno x greide (se matplotlib disponível)

Uso rápido
----------
  python greide_auto.py --csv terreno.csv --cota-inicial 745.20 --rampa-max 8 --vel 40
  python greide_auto.py --landxml eixo.xml --alinhamento "Rua A" --cota-inicial 745.20 \
                        --rampa-max 8 --vel 40 --injetar

Autor: rotina desenvolvida para Isaque Ferraz Moreira.
Licença de uso livre.
"""

from __future__ import annotations

import argparse
import csv
import math
import os
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from typing import List, Optional, Sequence, Tuple

LX_NS = "http://www.landxml.org/schema/LandXML-1.2"


# ==========================================================================
# 1. Tabelas normativas
# ==========================================================================

# K mínimo (parada) — valores de projeto AASHTO (métrico), coerentes com o
# Manual de Projeto Geométrico do DNIT para as curvas convexas.
# Confira sempre contra a norma/classe da via do seu projeto.
K_CONVEXA = {30: 2.0, 40: 4.0, 50: 7.0, 60: 11.0, 70: 17.0,
             80: 26.0, 90: 39.0, 100: 52.0, 110: 74.0, 120: 95.0}

K_CONCAVA = {30: 6.0, 40: 9.0, 50: 13.0, 60: 18.0, 70: 23.0,
             80: 30.0, 90: 38.0, 100: 45.0, 110: 55.0, 120: 63.0}

# Coeficiente de atrito longitudinal para a distância de visibilidade de parada
F_LONG = {30: 0.40, 40: 0.38, 50: 0.35, 60: 0.33, 70: 0.31,
          80: 0.30, 90: 0.30, 100: 0.29, 110: 0.28, 120: 0.28}


def _interp_tabela(tab: dict, v: float) -> float:
    """Interpola linearmente uma tabela {velocidade: valor}."""
    chaves = sorted(tab)
    if v <= chaves[0]:
        return tab[chaves[0]]
    if v >= chaves[-1]:
        return tab[chaves[-1]]
    for a, b in zip(chaves, chaves[1:]):
        if a <= v <= b:
            t = (v - a) / (b - a)
            return tab[a] + t * (tab[b] - tab[a])
    return tab[chaves[-1]]


def dist_parada(v: float, rampa: float = 0.0) -> float:
    """Distância de visibilidade de parada (m). v em km/h, rampa em decimal."""
    f = _interp_tabela(F_LONG, v)
    denom = max(0.05, f + rampa)
    return 0.278 * v * 2.5 + (v ** 2) / (254.0 * denom)


def k_convexa_formula(v: float) -> float:
    """K convexa pela DVP (h1 = 1,08 m; h2 = 0,60 m)."""
    s = dist_parada(v)
    return s ** 2 / 658.0


def k_concava_formula(v: float) -> float:
    """K côncava pelo critério do farol (h = 0,60 m; alfa = 1 grau)."""
    s = dist_parada(v)
    return s ** 2 / (120.0 + 3.5 * s)


# ==========================================================================
# 2. Estruturas
# ==========================================================================

@dataclass
class PVI:
    est: float          # estação/progressiva (m)
    cota: float         # cota do PIV (m)
    g_in: float = 0.0   # rampa de entrada (decimal, +subida)
    g_out: float = 0.0  # rampa de saída  (decimal)
    L: float = 0.0      # comprimento da curva vertical (m)
    L_teorico: float = 0.0
    K_min: float = 0.0
    tipo: str = "-"     # "convexa" | "concava" | "-"
    avisos: List[str] = field(default_factory=list)

    @property
    def A(self) -> float:
        """Diferença algébrica de rampas, em %."""
        return (self.g_out - self.g_in) * 100.0

    @property
    def K_real(self) -> float:
        return self.L / abs(self.A) if abs(self.A) > 1e-9 else float("inf")


@dataclass
class Terreno:
    est: List[float]
    cota: List[float]
    nome: str = "TERRENO"

    def __post_init__(self):
        pares = sorted(zip(self.est, self.cota))
        self.est = [p[0] for p in pares]
        self.cota = [p[1] for p in pares]

    def cota_em(self, s: float) -> float:
        """Cota do terreno interpolada linearmente."""
        e, z = self.est, self.cota
        if s <= e[0]:
            return z[0]
        if s >= e[-1]:
            return z[-1]
        lo, hi = 0, len(e) - 1
        while hi - lo > 1:
            mid = (lo + hi) // 2
            if e[mid] <= s:
                lo = mid
            else:
                hi = mid
        t = (s - e[lo]) / (e[hi] - e[lo]) if e[hi] > e[lo] else 0.0
        return z[lo] + t * (z[hi] - z[lo])


# ==========================================================================
# 3. Leitura de dados
# ==========================================================================

_RE_ESTACA = re.compile(r"^\s*(-?\d+)\s*\+\s*(\d+(?:[.,]\d+)?)\s*$")


def parse_estaca(txt: str, corda: float = 20.0) -> float:
    """Converte '10+5,00' -> 205.0 m. Aceita também número puro em metros."""
    txt = str(txt).strip()
    m = _RE_ESTACA.match(txt)
    if m:
        return int(m.group(1)) * corda + float(m.group(2).replace(",", "."))
    return float(txt.replace(",", "."))


def fmt_estaca(s: float, corda: float = 20.0) -> str:
    n = int(math.floor(s / corda))
    r = s - n * corda
    return f"{n}+{r:06.3f}"


def ler_csv(caminho: str, corda: float = 20.0) -> Terreno:
    """Lê CSV/TXT com duas colunas: estaca(ou metros) e cota.

    Aceita ';' , ',' ou tabulação/espaço como separador e ignora cabeçalho.
    """
    est: List[float] = []
    cota: List[float] = []
    with open(caminho, "r", encoding="utf-8-sig", errors="replace") as fh:
        for linha in fh:
            linha = linha.strip()
            if not linha or linha.startswith(("#", "//")):
                continue
            if ";" in linha:
                partes = linha.split(";")
            elif "\t" in linha:
                partes = linha.split("\t")
            elif linha.count(",") >= 1 and linha.count(".") >= 1:
                partes = linha.split(",")
            else:
                partes = linha.replace(",", ".").split()
            if len(partes) < 2:
                continue
            try:
                s = parse_estaca(partes[0], corda)
                z = float(str(partes[1]).replace(",", "."))
            except ValueError:
                continue  # cabeçalho
            est.append(s)
            cota.append(z)
    if len(est) < 2:
        raise SystemExit(f"CSV '{caminho}' nao tem pontos suficientes.")
    return Terreno(est, cota)


def _tag(nome: str) -> str:
    return f"{{{LX_NS}}}{nome}"


def ler_landxml(caminho: str, nome_alinh: Optional[str] = None,
                nome_perfil: Optional[str] = None
                ) -> Tuple[Terreno, ET.ElementTree, ET.Element]:
    """Lê o perfil de terreno (ProfSurf) de um LandXML exportado do Civil 3D."""
    ET.register_namespace("", LX_NS)
    tree = ET.parse(caminho)
    raiz = tree.getroot()

    alinhs = raiz.iter(_tag("Alignment"))
    alvo = None
    for a in alinhs:
        if nome_alinh is None or a.get("name", "") == nome_alinh:
            alvo = a
            break
    if alvo is None:
        nomes = [a.get("name") for a in raiz.iter(_tag("Alignment"))]
        raise SystemExit(f"Alinhamento '{nome_alinh}' nao encontrado. Disponiveis: {nomes}")

    surf = None
    for prof in alvo.iter(_tag("Profile")):
        for ps in prof.iter(_tag("ProfSurf")):
            if nome_perfil is None or ps.get("name", "") == nome_perfil \
               or prof.get("name", "") == nome_perfil:
                surf = ps
                break
        if surf is not None:
            break
    if surf is None:
        raise SystemExit("Nenhum perfil de terreno (ProfSurf) encontrado no LandXML.")

    est: List[float] = []
    cota: List[float] = []
    for pl in surf.iter(_tag("PntList2D")):
        vals = [float(x) for x in (pl.text or "").split()]
        for i in range(0, len(vals) - 1, 2):
            est.append(vals[i])
            cota.append(vals[i + 1])
    if len(est) < 2:
        raise SystemExit("ProfSurf sem pontos suficientes.")
    return Terreno(est, cota, surf.get("name", "TERRENO")), tree, alvo


# ==========================================================================
# 4. Geração do greide
# ==========================================================================

def _rampa_ajustada(terr: Terreno, amostras: Sequence[float],
                    i0: int, j: int, s0: float, z0: float,
                    i_max: float, i_min: float, sinal_ant: float) -> float:
    """Rampa de mínimos quadrados entre os índices i0..j, ancorada em (s0,z0),
    truncada em [i_min, i_max] em módulo."""
    num = den = 0.0
    for k in range(i0, j + 1):
        s = amostras[k]
        ds = s - s0
        if ds <= 0:
            continue
        num += ds * (terr.cota_em(s) - z0)
        den += ds * ds
    g = num / den if den > 1e-9 else 0.0

    if abs(g) > i_max:
        g = math.copysign(i_max, g)
    if i_min > 0.0 and abs(g) < i_min:
        s_ref = g if abs(g) > 1e-12 else (sinal_ant if sinal_ant != 0 else 1.0)
        g = math.copysign(i_min, s_ref)
    return g


def _desvio_max(terr: Terreno, amostras: Sequence[float],
                i0: int, j: int, s0: float, z0: float, g: float) -> float:
    dmax = 0.0
    for k in range(i0, j + 1):
        s = amostras[k]
        d = abs((z0 + g * (s - s0)) - terr.cota_em(s))
        if d > dmax:
            dmax = d
    return dmax


def gerar_pvis(terr: Terreno, cota_inicial: float, i_max: float,
               i_min: float = 0.0, lt_min: float = 40.0,
               tol: float = 0.20, passo: float = 10.0,
               est_ini: Optional[float] = None,
               est_fim: Optional[float] = None,
               tol_relax: float = 0.35) -> Tuple[List[PVI], List[str]]:
    """Gera a poligonal do greide seguindo o terreno com rampa limitada.

    i_max, i_min em decimal (0.08 = 8 %).
    tol  = desvio vertical máximo aceito entre a tangente e o terreno (m).
    lt_min = comprimento mínimo de tangente entre PIVs (m).
    """
    s_ini = terr.est[0] if est_ini is None else est_ini
    s_fim = terr.est[-1] if est_fim is None else est_fim

    n = max(2, int(round((s_fim - s_ini) / passo)) + 1)
    amostras = [s_ini + k * (s_fim - s_ini) / (n - 1) for k in range(n)]

    avisos: List[str] = []
    pvis: List[PVI] = [PVI(est=s_ini, cota=cota_inicial)]
    i0 = 0
    sinal_ant = 0.0

    while i0 < n - 1:
        s0 = amostras[i0]
        z0 = pvis[-1].cota

        # 1a passada: maior tangente que respeita a tolerancia
        jbest: Optional[int] = None
        gbest = 0.0
        j0: Optional[int] = None
        dev0 = 0.0
        g0 = 0.0
        j = i0 + 1
        while j <= n - 1:
            if amostras[j] - s0 >= lt_min - 1e-6:
                g = _rampa_ajustada(terr, amostras, i0, j, s0, z0, i_max, i_min, sinal_ant)
                dev = _desvio_max(terr, amostras, i0, j, s0, z0, g)
                if j0 is None:
                    j0, dev0, g0 = j, dev, g
                if dev <= tol:
                    jbest, gbest = j, g
                elif jbest is not None:
                    break
            j += 1

        # 2a passada: o terreno nao cabe na tolerancia nem na tangente minima.
        # Em vez de picotar o greide no comprimento minimo, estende a tangente
        # enquanto o desvio nao piorar significativamente (greide mais calmo).
        if jbest is None and j0 is not None:
            limite = max(tol, dev0 * (1.0 + tol_relax))
            jbest, gbest = j0, g0
            j = j0 + 1
            while j <= n - 1:
                g = _rampa_ajustada(terr, amostras, i0, j, s0, z0, i_max, i_min, sinal_ant)
                dev = _desvio_max(terr, amostras, i0, j, s0, z0, g)
                if dev <= limite:
                    jbest, gbest = j, g
                    j += 1
                else:
                    break
            avisos.append(
                f"Est. {fmt_estaca(s0)}: terreno nao cabe na tolerancia de {tol:.2f} m; "
                f"tangente de {amostras[jbest]-s0:.0f} m adotada com desvio de "
                f"{_desvio_max(terr, amostras, i0, jbest, s0, z0, gbest):.2f} m.")

        if jbest is None:  # chegou ao fim sem fechar lt_min
            jbest = n - 1
            gbest = _rampa_ajustada(terr, amostras, i0, jbest, s0, z0, i_max, i_min, sinal_ant)

        # se o resto até o fim for curto demais, estende esta tangente até o fim
        if (amostras[n - 1] - amostras[jbest]) < lt_min - 1e-6:
            jbest = n - 1
            gbest = _rampa_ajustada(terr, amostras, i0, jbest, s0, z0, i_max, i_min, sinal_ant)

        s1 = amostras[jbest]
        z1 = z0 + gbest * (s1 - s0)
        pvis[-1].g_out = gbest
        novo = PVI(est=s1, cota=z1, g_in=gbest, g_out=gbest)
        pvis.append(novo)
        sinal_ant = gbest if abs(gbest) > 1e-12 else sinal_ant
        i0 = jbest

    pvis[0].g_in = pvis[0].g_out
    pvis[-1].g_out = pvis[-1].g_in
    return pvis, avisos


def amarrar_final(pvis: List[PVI], cota_final: float, i_max: float) -> List[str]:
    """Força a cota do último PIV (amarração com projeto existente/greide vizinho)."""
    avisos: List[str] = []
    a, b = pvis[-2], pvis[-1]
    g = (cota_final - a.cota) / (b.est - a.est)
    if abs(g) > i_max + 1e-9:
        avisos.append(
            f"Amarracao final exige rampa de {g*100:.3f} %, acima da maxima "
            f"({i_max*100:.3f} %). Rampa aplicada mesmo assim - revisar o trecho.")
    b.cota = cota_final
    a.g_out = g
    b.g_in = b.g_out = g
    return avisos


# ==========================================================================
# 5. Curvas verticais
# ==========================================================================

def dimensionar_curvas(pvis: List[PVI], vel: float,
                       k_conv: Optional[float] = None,
                       k_conc: Optional[float] = None,
                       l_min_abs: float = 0.0,
                       a_min: float = 0.05) -> List[str]:
    """Dimensiona as parábolas simétricas por K mínimo e resolve sobreposições."""
    avisos: List[str] = []
    kc = k_conv if k_conv is not None else _interp_tabela(K_CONVEXA, vel)
    kv = k_conc if k_conc is not None else _interp_tabela(K_CONCAVA, vel)
    l_min = max(l_min_abs, 0.6 * vel)   # DNIT: L >= 0,6 V (conforto/estetica)

    for p in pvis[1:-1]:
        A = p.A
        if abs(A) < a_min:
            p.L = 0.0
            p.tipo = "-"
            p.avisos.append("A desprezivel: sem curva")
            continue
        p.tipo = "convexa" if A < 0 else "concava"
        p.K_min = kc if A < 0 else kv
        p.L_teorico = max(p.K_min * abs(A), l_min)
        p.L = p.L_teorico

    # --- resolve sobreposição de curvas nas tangentes ---------------------
    for _ in range(30):
        mudou = False
        for a, b in zip(pvis, pvis[1:]):
            T = b.est - a.est
            need = a.L / 2.0 + b.L / 2.0
            if need > T - 1e-6 and need > 0:
                fator = (T * 0.995) / need
                if a.L > 0:
                    a.L *= fator
                if b.L > 0:
                    b.L *= fator
                mudou = True
        if not mudou:
            break

    for p in pvis[1:-1]:
        if p.L > 0 and p.L < p.L_teorico - 1e-6:
            if p.K_real < p.K_min - 1e-6:
                avisos.append(
                    f"Est. {fmt_estaca(p.est)}: curva reduzida de {p.L_teorico:.1f} m "
                    f"para {p.L:.1f} m por falta de tangente - K resultante "
                    f"{p.K_real:.1f} ABAIXO do K min {p.K_min:.1f}. REVISAR.")
            else:
                avisos.append(
                    f"Est. {fmt_estaca(p.est)}: curva reduzida de {p.L_teorico:.1f} m "
                    f"para {p.L:.1f} m (abaixo do L min de conforto de {l_min:.0f} m), "
                    f"mas o K min ({p.K_min:.1f}) continua atendido.")
    return avisos


def cota_greide(pvis: List[PVI], s: float) -> float:
    """Cota do greide em uma estação, já considerando as parábolas."""
    if s <= pvis[0].est:
        return pvis[0].cota + pvis[0].g_out * (s - pvis[0].est)
    if s >= pvis[-1].est:
        return pvis[-1].cota + pvis[-1].g_in * (s - pvis[-1].est)

    for p in pvis[1:-1]:
        if p.L > 0 and abs(s - p.est) <= p.L / 2.0:
            pcv = p.est - p.L / 2.0
            z_pcv = p.cota - p.g_in * p.L / 2.0
            x = s - pcv
            A = p.g_out - p.g_in
            return z_pcv + p.g_in * x + (A / (2.0 * p.L)) * x * x

    for a, b in zip(pvis, pvis[1:]):
        if a.est <= s <= b.est:
            return a.cota + a.g_out * (s - a.est)
    return pvis[-1].cota


# ==========================================================================
# 6. Saídas
# ==========================================================================

def escrever_csv_pvis(caminho: str, pvis: List[PVI], corda: float) -> None:
    with open(caminho, "w", newline="", encoding="utf-8-sig") as fh:
        w = csv.writer(fh, delimiter=";")
        w.writerow(["PIV", "Estaca", "Progressiva (m)", "Cota (m)",
                    "Rampa entrada (%)", "Rampa saida (%)", "A (%)",
                    "Tipo", "K min", "L (m)", "K real",
                    "PCV (m)", "PTV (m)"])
        for i, p in enumerate(pvis, 1):
            pcv = p.est - p.L / 2.0 if p.L else ""
            ptv = p.est + p.L / 2.0 if p.L else ""
            w.writerow([
                i, fmt_estaca(p.est, corda), f"{p.est:.3f}", f"{p.cota:.3f}",
                f"{p.g_in*100:.3f}", f"{p.g_out*100:.3f}", f"{p.A:.3f}",
                p.tipo, f"{p.K_min:.1f}" if p.K_min else "",
                f"{p.L:.2f}" if p.L else "",
                f"{p.K_real:.1f}" if p.L else "",
                f"{pcv:.3f}" if pcv != "" else "",
                f"{ptv:.3f}" if ptv != "" else "",
            ])


def escrever_txt_pvis(caminho: str, pvis: List[PVI]) -> None:
    """Formato do 'Create Profile from File' do Civil 3D: estacao cota."""
    with open(caminho, "w", encoding="ascii") as fh:
        for p in pvis:
            fh.write(f"{p.est:.4f} {p.cota:.4f}\n")


def landxml_profile_elem(pvis: List[PVI], nome: str) -> ET.Element:
    prof = ET.Element(_tag("Profile"), {"name": nome, "desc": "Greide automatico"})
    pa = ET.SubElement(prof, _tag("ProfAlign"), {"name": nome})
    for p in pvis:
        if p.L > 0:
            e = ET.SubElement(pa, _tag("ParaCurve"), {"length": f"{p.L:.4f}"})
        else:
            e = ET.SubElement(pa, _tag("PVI"))
        e.text = f"{p.est:.4f} {p.cota:.4f}"
    return prof


def escrever_landxml(caminho: str, pvis: List[PVI], nome_perfil: str,
                     tree: Optional[ET.ElementTree] = None,
                     alinh: Optional[ET.Element] = None,
                     terr: Optional[Terreno] = None,
                     nome_alinh: str = "EIXO") -> None:
    ET.register_namespace("", LX_NS)
    prof = landxml_profile_elem(pvis, nome_perfil)

    if tree is not None and alinh is not None:
        alinh.append(prof)
        tree.write(caminho, encoding="UTF-8", xml_declaration=True)
        return

    # LandXML autônomo (sem geometria do eixo) — usar apenas se o eixo já existe
    raiz = ET.Element(_tag("LandXML"), {
        "version": "1.2",
        "date": "2000-01-01", "time": "00:00:00",
        "language": "Portuguese",
        "readOnly": "false",
    })
    ET.SubElement(raiz, _tag("Units")).append(
        ET.Element(_tag("Metric"), {
            "areaUnit": "squareMeter", "linearUnit": "meter",
            "volumeUnit": "cubicMeter", "temperatureUnit": "celsius",
            "pressureUnit": "milliBars", "angularUnit": "decimal degrees",
            "directionUnit": "decimal degrees"}))
    ET.SubElement(raiz, _tag("Project"), {"name": "Greide automatico"})
    ET.SubElement(raiz, _tag("Application"), {"name": "greide_auto.py", "version": "1.0"})
    aligns = ET.SubElement(raiz, _tag("Alignments"), {"name": "Greide"})
    s0 = pvis[0].est
    comp = pvis[-1].est - pvis[0].est
    al = ET.SubElement(aligns, _tag("Alignment"),
                       {"name": nome_alinh, "length": f"{comp:.4f}",
                        "staStart": f"{s0:.4f}"})
    al.append(prof)
    ET.ElementTree(raiz).write(caminho, encoding="UTF-8", xml_declaration=True)


def relatorio(pvis: List[PVI], terr: Terreno, params: dict,
              avisos: List[str], corda: float) -> str:
    L: List[str] = []
    ad = L.append
    ad("=" * 74)
    ad("  GREIDE AUTOMATICO - MEMORIA DE CALCULO")
    ad("=" * 74)
    ad("")
    ad("PARAMETROS")
    for k, v in params.items():
        ad(f"  {k:.<34} {v}")
    ad("")
    vel = params.get("Velocidade diretriz (km/h)", 40)
    try:
        vel = float(vel)
    except (TypeError, ValueError):
        vel = 40.0
    ad("K MINIMOS ADOTADOS (tabela) x FORMULA (DVP)")
    ad(f"  Convexa .... tabela {_interp_tabela(K_CONVEXA, vel):6.1f}   "
       f"formula {k_convexa_formula(vel):6.1f}")
    ad(f"  Concava .... tabela {_interp_tabela(K_CONCAVA, vel):6.1f}   "
       f"formula {k_concava_formula(vel):6.1f}")
    ad(f"  DVP para V = {vel:.0f} km/h: {dist_parada(vel):.1f} m")
    ad("")
    ad("PIVs")
    ad("  #   Estaca        Prog.(m)     Cota      i_ent%   i_sai%     A%   Tipo      L(m)   K")
    for i, p in enumerate(pvis, 1):
        ad(f"  {i:<3} {fmt_estaca(p.est, corda):<13} {p.est:9.3f} {p.cota:9.3f} "
           f"{p.g_in*100:8.3f} {p.g_out*100:8.3f} {p.A:7.3f}  {p.tipo:<9} "
           f"{p.L:7.2f} {(p.K_real if p.L else 0):6.1f}")
    ad("")

    # verificações
    ad("VERIFICACOES")
    i_max = params.get("_i_max", 0.08)
    pior = max((abs(p.g_out) for p in pvis[:-1]), default=0.0)
    ad(f"  Rampa maxima utilizada ......... {pior*100:.3f} %  "
       f"(limite {i_max*100:.3f} %) -> {'OK' if pior <= i_max + 1e-6 else 'EXCEDE'}")
    tmin = min((b.est - a.est for a, b in zip(pvis, pvis[1:])), default=0.0)
    ad(f"  Menor tangente entre PIVs ...... {tmin:.2f} m")
    ok_sobrep = all((a.L / 2 + b.L / 2) <= (b.est - a.est) + 1e-3
                    for a, b in zip(pvis, pvis[1:]))
    ad(f"  Sobreposicao de curvas ......... {'nenhuma' if ok_sobrep else 'HA SOBREPOSICAO'}")
    ad("")

    # corte/aterro
    passo = 5.0
    s = pvis[0].est
    corte = aterro = 0.0
    cmax = amax = 0.0
    s_cmax = s_amax = s
    npt = 0
    while s <= pvis[-1].est + 1e-6:
        d = cota_greide(pvis, s) - terr.cota_em(s)
        if d < 0:
            corte += -d
            if -d > cmax:
                cmax, s_cmax = -d, s
        else:
            aterro += d
            if d > amax:
                amax, s_amax = d, s
        npt += 1
        s += passo
    ad("CORTE / ATERRO (diferenca greide - terreno, sem secao tipo)")
    ad(f"  Corte maximo ..... {cmax:6.3f} m  na est. {fmt_estaca(s_cmax, corda)}")
    ad(f"  Aterro maximo .... {amax:6.3f} m  na est. {fmt_estaca(s_amax, corda)}")
    ad(f"  Altura media de corte ... {corte/max(1,npt):6.3f} m")
    ad(f"  Altura media de aterro .. {aterro/max(1,npt):6.3f} m")
    ad("")
    if avisos:
        ad("AVISOS")
        for a in avisos:
            ad(f"  ! {a}")
    else:
        ad("AVISOS: nenhum.")
    ad("")
    ad("Obs.: os K minimos sao de distancia de visibilidade de PARADA. Para")
    ad("      ultrapassagem, cruzamentos ou drenagem em corte, revise os valores.")
    return "\n".join(L)


def plotar(caminho: str, pvis: List[PVI], terr: Terreno) -> bool:
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
    except Exception:
        return False
    s0, s1 = pvis[0].est, pvis[-1].est
    xs = [s0 + i * (s1 - s0) / 600.0 for i in range(601)]
    zt = [terr.cota_em(x) for x in xs]
    zg = [cota_greide(pvis, x) for x in xs]

    fig, (ax, ax2) = plt.subplots(2, 1, figsize=(13, 7), sharex=True,
                                  gridspec_kw={"height_ratios": [3, 1]})
    ax.plot(xs, zt, lw=1.4, color="#8a6d3b", label="Terreno")
    ax.plot(xs, zg, lw=2.0, color="#c0392b", label="Greide")
    ax.plot([p.est for p in pvis], [p.cota for p in pvis], "o", ms=4,
            color="#2c3e50", label="PIV (poligonal)")
    ax.grid(alpha=.3)
    ax.legend(loc="best")
    ax.set_ylabel("Cota (m)")
    ax.set_title("Greide automatico x terreno natural")

    dif = [g - t for g, t in zip(zg, zt)]
    ax2.fill_between(xs, dif, 0, where=[d > 0 for d in dif], color="#e67e22", alpha=.6, label="Aterro")
    ax2.fill_between(xs, dif, 0, where=[d < 0 for d in dif], color="#2980b9", alpha=.6, label="Corte")
    ax2.axhline(0, color="k", lw=.8)
    ax2.grid(alpha=.3)
    ax2.set_ylabel("Greide - terreno (m)")
    ax2.set_xlabel("Progressiva (m)")
    ax2.legend(loc="best", fontsize=8)
    fig.tight_layout()
    fig.savefig(caminho, dpi=130)
    plt.close(fig)
    return True


# ==========================================================================
# 7. CLI
# ==========================================================================

def main(argv: Optional[Sequence[str]] = None) -> int:
    ap = argparse.ArgumentParser(
        description="Geracao automatica de greide seguindo o terreno com rampa maxima.")
    src = ap.add_mutually_exclusive_group(required=True)
    src.add_argument("--csv", help="CSV/TXT do terreno (estaca;cota)")
    src.add_argument("--landxml", help="LandXML exportado do Civil 3D")

    ap.add_argument("--alinhamento", help="nome do alinhamento no LandXML")
    ap.add_argument("--perfil-terreno", help="nome do perfil de terreno no LandXML")
    ap.add_argument("--nome-greide", default="GREIDE-AUTO", help="nome do perfil de projeto")
    ap.add_argument("--injetar", action="store_true",
                    help="grava o greide dentro do proprio LandXML de entrada (recomendado)")

    ap.add_argument("--cota-inicial", type=float, default=None,
                    help="cota do greide na estaca inicial (padrao: cota do terreno)")
    ap.add_argument("--cota-final", type=float, default=None,
                    help="amarra a cota do ultimo PIV (projeto existente/greide vizinho)")
    ap.add_argument("--rampa-max", type=float, required=True, help="rampa maxima em %%")
    ap.add_argument("--rampa-min", type=float, default=0.0,
                    help="rampa minima em %% para drenagem (0 = sem minima)")
    ap.add_argument("--vel", type=float, default=40.0, help="velocidade diretriz (km/h)")
    ap.add_argument("--k-convexa", type=float, default=None, help="forca o K convexa")
    ap.add_argument("--k-concava", type=float, default=None, help="forca o K concava")
    ap.add_argument("--l-min-curva", type=float, default=0.0,
                    help="comprimento minimo absoluto de curva vertical (m)")
    ap.add_argument("--tangente-min", type=float, default=40.0,
                    help="comprimento minimo de tangente entre PIVs (m)")
    ap.add_argument("--tolerancia", type=float, default=0.20,
                    help="desvio vertical maximo aceito em relacao ao terreno (m)")
    ap.add_argument("--passo", type=float, default=10.0, help="passo de amostragem (m)")
    ap.add_argument("--corda", type=float, default=20.0, help="corda da estaca (m)")
    ap.add_argument("--est-inicial", type=float, default=None)
    ap.add_argument("--est-final", type=float, default=None)
    ap.add_argument("--saida", default=None, help="prefixo dos arquivos de saida")
    a = ap.parse_args(argv)

    tree = alinh = None
    if a.csv:
        terr = ler_csv(a.csv, a.corda)
        base = a.saida or os.path.splitext(a.csv)[0]
        nome_alinh = a.alinhamento or "EIXO"
    else:
        terr, tree, alinh = ler_landxml(a.landxml, a.alinhamento, a.perfil_terreno)
        base = a.saida or os.path.splitext(a.landxml)[0] + "_greide"
        nome_alinh = alinh.get("name", "EIXO")

    cota0 = a.cota_inicial
    if cota0 is None:
        cota0 = terr.cota_em(a.est_inicial if a.est_inicial is not None else terr.est[0])

    i_max = abs(a.rampa_max) / 100.0
    i_min = abs(a.rampa_min) / 100.0
    if i_min >= i_max:
        raise SystemExit("rampa-min deve ser menor que rampa-max.")

    pvis, av1 = gerar_pvis(terr, cota0, i_max, i_min, a.tangente_min,
                           a.tolerancia, a.passo, a.est_inicial, a.est_final)
    av0: List[str] = []
    if a.cota_final is not None:
        av0 = amarrar_final(pvis, a.cota_final, i_max)
    av2 = dimensionar_curvas(pvis, a.vel, a.k_convexa, a.k_concava, a.l_min_curva)
    avisos = av1 + av0 + av2

    params = {
        "Cota inicial (m)": f"{cota0:.3f}",
        "Rampa maxima (%)": f"{a.rampa_max:.3f}",
        "Rampa minima (%)": f"{a.rampa_min:.3f}",
        "Velocidade diretriz (km/h)": f"{a.vel:.0f}",
        "Tangente minima (m)": f"{a.tangente_min:.1f}",
        "Tolerancia ao terreno (m)": f"{a.tolerancia:.2f}",
        "Passo de amostragem (m)": f"{a.passo:.1f}",
        "Tipo de curva": "parabolica simetrica",
        "Extensao (m)": f"{pvis[-1].est - pvis[0].est:.2f}",
        "Numero de PIVs": len(pvis),
        "_i_max": i_max,
    }

    f_csv = base + "_pvis.csv"
    f_txt = base + "_pvis.txt"
    f_xml = base + ".xml" if (a.injetar and tree is not None) else base + "_greide.xml"
    f_rel = base + "_relatorio.txt"
    f_png = base + "_greide.png"

    escrever_csv_pvis(f_csv, pvis, a.corda)
    escrever_txt_pvis(f_txt, pvis)
    escrever_landxml(f_xml, pvis, a.nome_greide,
                     tree if a.injetar else None,
                     alinh if a.injetar else None,
                     terr, nome_alinh)
    rel = relatorio(pvis, terr, params, avisos, a.corda)
    with open(f_rel, "w", encoding="utf-8") as fh:
        fh.write(rel)
    tem_png = plotar(f_png, pvis, terr)

    print(rel)
    print("\nARQUIVOS GERADOS")
    print(f"  {f_csv}")
    print(f"  {f_txt}   (Civil 3D: Create Profile from File - so tangentes)")
    print(f"  {f_xml}   (Civil 3D: Insert > LandXML - tangentes + curvas)")
    print(f"  {f_rel}")
    if tem_png:
        print(f"  {f_png}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
