"""Leitura do eixo em LandXML (linhas e arcos) e cálculo de pontos por estaca."""
import math
import xml.etree.ElementTree as ET

import numpy as np


def _tag(el):
    return el.tag.split("}")[-1]


def _ne(texto):
    """LandXML grava 'Norte Este'. Devolve (E, N)."""
    n, e = texto.split()[:2]
    return float(e), float(n)


class Eixo:
    """Eixo horizontal. Coordenadas internas em (E, N); estacas em metros."""

    def __init__(self, caminho):
        self.caminho = str(caminho)
        raiz = ET.parse(self.caminho).getroot()
        self.xml_raiz = raiz
        alinhamento = next((el for el in raiz.iter() if _tag(el) == "Alignment"), None)
        if alinhamento is None:
            raise ValueError("O XML não tem nenhum <Alignment>.")
        self.xml_alinhamento = alinhamento
        self.nome = alinhamento.get("name", "EIXO")
        self.sta_ini = float(alinhamento.get("staStart", "0"))
        self.velocidade = None
        for el in alinhamento.iter():
            if _tag(el) == "Property" and el.get("label") == "speed":
                self.velocidade = float(el.get("value"))
                break
        self.sistema = None
        for el in raiz.iter():
            if _tag(el) == "CoordinateSystem":
                self.sistema = el.get("desc")
                break

        coord = next((el for el in alinhamento if _tag(el) == "CoordGeom"), None)
        if coord is None:
            raise ValueError("O alinhamento não tem <CoordGeom>.")

        tipos, ini, fim, centro, raio, sentido, compr = [], [], [], [], [], [], []
        for el in coord:
            t = _tag(el)
            if t not in ("Line", "Curve"):
                raise ValueError(
                    f"Elemento <{t}> não suportado. Exporte o eixo só com linhas e arcos."
                )
            filhos = {_tag(f): f for f in el}
            s = _ne(filhos["Start"].text)
            e = _ne(filhos["End"].text)
            if t == "Line":
                comp = math.dist(s, e)
                tipos.append(0)
                centro.append((np.nan, np.nan))
                raio.append(np.inf)
                sentido.append(0.0)
            else:
                c = _ne(filhos["Center"].text)
                r = math.dist(c, s)
                sg = 1.0 if el.get("rot", "ccw").lower() == "ccw" else -1.0
                a0 = math.atan2(s[1] - c[1], s[0] - c[0])
                a1 = math.atan2(e[1] - c[1], e[0] - c[0])
                delta = (sg * (a1 - a0)) % (2 * math.pi)
                comp = delta * r
                tipos.append(1)
                centro.append(c)
                raio.append(r)
                sentido.append(sg)
            ini.append(s)
            fim.append(e)
            compr.append(comp)

        if not tipos:
            raise ValueError("O alinhamento não tem geometria.")
        self.tipo = np.array(tipos)
        self.ini = np.array(ini)
        self.fim = np.array(fim)
        self.centro = np.array(centro)
        self.raio = np.array(raio)
        self.sentido = np.array(sentido)
        self.compr = np.array(compr)
        self.cum = np.concatenate([[0.0], np.cumsum(self.compr)])
        self.comprimento = float(self.cum[-1])
        self.sta_fim = self.sta_ini + self.comprimento
        self.descontinuidade_max = float(
            np.max(np.hypot(*(self.ini[1:] - self.fim[:-1]).T)) if len(tipos) > 1 else 0.0
        )

    def pontos(self, estacas):
        """Para cada estaca devolve (E, N, te, tn, curvatura com sinal: + = esquerda)."""
        estacas = np.asarray(estacas, dtype=float)
        s = estacas - self.sta_ini
        if np.any(s < -1e-6) or np.any(s > self.comprimento + 1e-6):
            raise ValueError("Estaca fora do eixo.")
        idx = np.clip(np.searchsorted(self.cum, s, side="right") - 1, 0, len(self.tipo) - 1)
        t = s - self.cum[idx]

        e = np.empty_like(s)
        n = np.empty_like(s)
        te = np.empty_like(s)
        tn = np.empty_like(s)
        k = np.zeros_like(s)

        linha = self.tipo[idx] == 0
        i = idx[linha]
        d = self.fim[i] - self.ini[i]
        d = d / np.hypot(d[:, 0], d[:, 1])[:, None]
        e[linha] = self.ini[i, 0] + d[:, 0] * t[linha]
        n[linha] = self.ini[i, 1] + d[:, 1] * t[linha]
        te[linha] = d[:, 0]
        tn[linha] = d[:, 1]

        arco = ~linha
        i = idx[arco]
        r = self.raio[i]
        sg = self.sentido[i]
        c = self.centro[i]
        a0 = np.arctan2(self.ini[i, 1] - c[:, 1], self.ini[i, 0] - c[:, 0])
        ang = a0 + sg * t[arco] / r
        e[arco] = c[:, 0] + r * np.cos(ang)
        n[arco] = c[:, 1] + r * np.sin(ang)
        te[arco] = -sg * np.sin(ang)
        tn[arco] = sg * np.cos(ang)
        k[arco] = sg / r
        return e, n, te, tn, k

    def deslocados(self, estacas, afastamentos):
        """Coordenadas nas estacas e afastamentos (negativo = esquerda, positivo = direita).

        Devolve E e N com forma (n_estacas, n_afastamentos).
        """
        e, n, te, tn, _ = self.pontos(estacas)
        af = np.asarray(afastamentos, dtype=float)[None, :]
        # A normal à direita do sentido de avanço é (tn, -te).
        return e[:, None] + tn[:, None] * af, n[:, None] - te[:, None] * af

    def grade(self, passo, ini=None, fim=None):
        """Estacas múltiplas do passo dentro do intervalo pedido."""
        a = self.sta_ini if ini is None else max(ini, self.sta_ini)
        b = self.sta_fim if fim is None else min(fim, self.sta_fim)
        i0 = math.ceil(a / passo - 1e-9)
        i1 = math.floor(b / passo + 1e-9)
        if i1 < i0:
            raise ValueError("Intervalo de estacas vazio.")
        return np.arange(i0, i1 + 1) * passo


def fmt_estaca(sta):
    """251100 -> '251+100,00' (km + metros)."""
    km = int(sta // 1000)
    m = sta - km * 1000
    return f"{km}+{m:06.2f}".replace(".", ",")
