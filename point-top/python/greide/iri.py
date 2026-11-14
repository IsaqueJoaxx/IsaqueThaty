"""IRI por quarto de carro (Golden Car), a partir de perfil longitudinal.

Referência: Sayers, "On the Calculation of IRI from Longitudinal Road Profile", TRR 1501, 1995.
Parâmetros: k1 653, k2 63,3, c 6, mu 0,15; 80 km/h; média móvel de 250 mm; transição de estados.

Limites de amostragem usados, do mesmo artigo:
- até 0,30 m: IRI;
- de 0,30 a 0,60 m: Classe 2, com viés que pode passar de 10 %;
- acima de 0,60 m: o programa não calcula IRI.

Levantamento topográfico a cada 5 m não atende a esses limites. Para ele existe `parcela_ondas_longas`,
que usa o mesmo modelo, mas só enxerga ondas de 10 m ou mais. Não é IRI e nunca é apresentada como tal.
"""
import csv
from dataclasses import dataclass, field
from typing import List, Optional

import numpy as np
from scipy.interpolate import CubicSpline
from scipy.linalg import expm

K1, K2, C, MU = 653.0, 63.3, 6.0, 0.15
V = 80.0 / 3.6
BASE_MM = 0.25
DX_IRI = 0.30
DX_CLASSE2 = 0.60
RESOLUCAO_M = 0.0005
AQUECIMENTO_M = 20.0


def _matrizes():
    a = np.array(
        [
            [0.0, 1.0, 0.0, 0.0],
            [-K2, -C, K2, C],
            [0.0, 0.0, 0.0, 1.0],
            [K2 / MU, C / MU, -(K1 + K2) / MU, -C / MU],
        ]
    )
    b = np.array([0.0, 0.0, 0.0, K1 / MU])
    return a, b


def _transicao(dx):
    a, b = _matrizes()
    st = expm(a * dx / V)
    pr = np.linalg.solve(a, (st - np.eye(4)) @ b)
    return st, pr


def ganho(comprimento_onda):
    """Ganho analítico do modelo (sem discretização), de inclinação do perfil para IRI em m/m."""
    a, b = _matrizes()
    w = 2 * np.pi * V / comprimento_onda
    x = np.linalg.solve(1j * w * np.eye(4) - a, b)
    return abs(x[0] - x[2])


def inclinacao_retificada(y, dx):
    """Inclinação retificada amostra a amostra. O IRI em m/km é a média disso vezes 1000."""
    y = np.asarray(y, dtype=float)
    base = max(1, int(round(BASE_MM / dx)))
    n = len(y) - base
    k = max(1, int(round(11.0 / dx)))
    if n < 1 or len(y) <= k:
        raise ValueError("Perfil curto demais: são necessários mais de 11 m.")
    st, pr = _transicao(dx)
    s = st.tolist()
    p = pr.tolist()
    inicial = float((y[k] - y[0]) / (k * dx))
    z0, z1, z2, z3 = inicial, 0.0, inicial, 0.0
    entrada = ((y[base:] - y[:-base]) / (base * dx)).tolist()
    saida = np.empty(n)
    # Sequência de estados: laço simples para a matriz fixa ficar rápida também em perfis longos.
    s00, s01, s02, s03 = s[0]
    s10, s11, s12, s13 = s[1]
    s20, s21, s22, s23 = s[2]
    s30, s31, s32, s33 = s[3]
    p0, p1, p2, p3 = p
    for i, u in enumerate(entrada):
        a0 = s00 * z0 + s01 * z1 + s02 * z2 + s03 * z3 + p0 * u
        a1 = s10 * z0 + s11 * z1 + s12 * z2 + s13 * z3 + p1 * u
        a2 = s20 * z0 + s21 * z1 + s22 * z2 + s23 * z3 + p2 * u
        a3 = s30 * z0 + s31 * z1 + s32 * z2 + s33 * z3 + p3 * u
        z0, z1, z2, z3 = a0, a1, a2, a3
        saida[i] = abs(a0 - a2)
    return saida


def iri_global(y, dx):
    return float(np.mean(inclinacao_retificada(y, dx)) * 1000.0)


@dataclass
class Trilha:
    nome: str
    distancia: np.ndarray
    cota: np.ndarray


@dataclass
class PerfilIRI:
    trilhas: List[Trilha]
    dx: float
    avisos: List[str] = field(default_factory=list)
    classe: str = "iri"
    resolucao: float = 0.0
    trechos: int = 1


NOMES_DISTANCIA = {"distancia", "distância", "dist", "estaca", "sta", "station", "x", "km", "progressiva"}
NOMES_ESQ = {"le", "esq", "esquerda", "trilha_le", "left", "l", "te"}
NOMES_DIR = {"ld", "dir", "direita", "trilha_ld", "right", "r", "td"}
NOMES_COTA = {"cota", "z", "elev", "elevacao", "elevação", "perfil", "profile", "h"}


def _numero(t):
    t = t.strip().replace(",", ".")
    try:
        v = float(t)
    except ValueError:
        return None
    return v if np.isfinite(v) else None


def ler_tabela(caminho):
    """Devolve distância e colunas de cota, com ou sem cabeçalho. Separador ; , tab ou espaço."""
    with open(caminho, encoding="utf-8-sig", errors="replace", newline="") as f:
        linhas = [l.strip() for l in f if l.strip()]
    if len(linhas) < 3:
        raise ValueError("O arquivo do perfil tem menos de 3 linhas.")
    amostra = linhas[min(1, len(linhas) - 1)]
    if ";" in amostra:
        sep = ";"
    elif "\t" in amostra:
        sep = "\t"
    elif "," in amostra and len(amostra.split(",")) in (2, 3) and "." in amostra:
        sep = ","
    else:
        sep = None
    partes = [next(csv.reader([l], delimiter=sep)) if sep else l.split() for l in linhas]
    primeira = [c.strip().lower() for c in partes[0]]
    cab = primeira if any(_numero(c) is None for c in primeira) else None
    corpo = partes[1:] if cab else partes
    ncol = len(corpo[0])
    if ncol < 2:
        raise ValueError("O perfil precisa de ao menos duas colunas: distância e cota.")
    dados = []
    ignoradas = 0
    for l in corpo:
        v = [_numero(c) for c in l[:ncol]] + [None] * (ncol - len(l))
        if v[0] is None or all(x is None for x in v[1:]):
            ignoradas += 1
            continue
        dados.append([np.nan if x is None else x for x in v])
    if len(dados) < 3:
        raise ValueError("O perfil tem menos de 3 linhas numéricas.")
    d = np.array(dados, dtype=float)
    i_dist, colunas = 0, {}
    if cab:
        for i, c in enumerate(cab):
            if c in NOMES_DISTANCIA:
                i_dist = i
        for i, c in enumerate(cab):
            if i == i_dist:
                continue
            if c in NOMES_ESQ:
                colunas["Trilha esquerda"] = i
            elif c in NOMES_DIR:
                colunas["Trilha direita"] = i
            elif c in NOMES_COTA:
                colunas["Perfil"] = i
    if not colunas:
        restantes = [i for i in range(d.shape[1]) if i != i_dist]
        if len(restantes) >= 2:
            colunas = {"Trilha esquerda": restantes[0], "Trilha direita": restantes[1]}
        else:
            colunas = {"Perfil": restantes[0]}
    return d[:, i_dist], {k: d[:, i] for k, i in colunas.items()}, ignoradas


def preparar(caminho, unidade="m"):
    """Lê o perfil e confere se ele serve para IRI. Lança ValueError quando não serve."""
    if unidade not in ("m", "mm"):
        raise ValueError("Unidade do perfil deve ser m ou mm.")
    dist, cotas, ignoradas = ler_tabela(caminho)
    ordem = np.argsort(dist)
    dist = dist[ordem]
    fator = 0.001 if unidade == "mm" else 1.0
    passos = np.diff(dist)
    if np.any(passos <= 0):
        raise ValueError("O perfil tem distâncias repetidas.")
    dx = float(np.median(passos))
    avisos = []
    if ignoradas:
        avisos.append(f"{ignoradas} linhas do perfil foram ignoradas por não serem numéricas.")
    if dx > DX_CLASSE2:
        raise ValueError(
            f"IRI não calculado: o perfil tem pontos a cada {dx:.2f} m, e o limite é {DX_CLASSE2:.2f} m. "
            "O IRI depende de ondas de 1 a 30 m e precisa de perfil denso (perfilômetro ou nível e mira a cada 0,25 m)."
        )
    classe = "iri" if dx <= DX_IRI + 1e-9 else "classe2"
    if classe == "classe2":
        avisos.append(f"Pontos a cada {dx:.2f} m: resultado de Classe 2, com viés que pode passar de 10 %.")
    irregular = np.abs(passos - dx) > 0.02 * dx
    falhas = passos > 3 * dx
    if falhas.any():
        avisos.append(f"{int(falhas.sum())} falhas maiores que {3 * dx:.2f} m no perfil: os segmentos que as contêm ficam sem IRI.")
    if irregular.mean() > 0.05 and not falhas.all():
        avisos.append("Espaçamento irregular em mais de 5 % dos pontos: o perfil foi reamostrado por interpolação linear.")
    grade = np.arange(dist[0], dist[-1] + dx / 2, dx)
    trilhas = []
    resol = []
    for nome, z in cotas.items():
        z = z[ordem] * fator
        ok = np.isfinite(z)
        if ok.sum() < 3:
            raise ValueError(f"A coluna {nome} não tem cotas suficientes.")
        if (~ok).sum():
            avisos.append(f"{int((~ok).sum())} cotas vazias em {nome} foram interpoladas.")
        unicos = np.unique(np.round(z[ok], 6))
        if len(unicos) > 1:
            resol.append(float(np.min(np.diff(unicos))))
        trilhas.append(Trilha(nome, grade, np.interp(grade, dist[ok], z[ok])))
    resolucao = max(resol) if resol else 0.0
    if resolucao > RESOLUCAO_M + 1e-6:
        avisos.append(
            f"Resolução aparente das cotas: {resolucao * 1000:.1f} mm. A referência indica 0,5 mm ou melhor; em pista lisa o resultado perde confiança."
        )
    return PerfilIRI(trilhas, dx, avisos, classe, resolucao, 1 + int(falhas.sum())), dist, falhas


def calcular(caminho, segmento=100.0, unidade="m", limite: Optional[float] = None):
    """IRI por segmento e por trilha. Segmento em metros; limite em m/km, opcional."""
    if not np.isfinite(segmento) or segmento < 20:
        raise ValueError("O segmento de IRI deve ter ao menos 20 m.")
    perfil, dist_original, falhas = preparar(caminho, unidade)
    inicio_falha = dist_original[:-1][falhas]
    fim_falha = dist_original[1:][falhas]
    t0 = perfil.trilhas[0].distancia
    extensao = float(t0[-1] - t0[0])
    if extensao < segmento:
        raise ValueError(f"O perfil tem {extensao:.1f} m, menos que um segmento de {segmento:.0f} m.")
    n_seg = int(extensao // segmento)
    segmentos = []
    respostas = {t.nome: inclinacao_retificada(t.cota, perfil.dx) for t in perfil.trilhas}
    for i in range(n_seg):
        a, b = t0[0] + i * segmento, t0[0] + (i + 1) * segmento
        linha = {"inicio": float(a), "fim": float(b), "aquecimento": bool(a - t0[0] < AQUECIMENTO_M)}
        valido = not np.any((inicio_falha < b) & (fim_falha > a))
        linha["valido"] = bool(valido)
        valores = {}
        for t in perfil.trilhas:
            r = respostas[t.nome]
            m = (t.distancia[: len(r)] >= a) & (t.distancia[: len(r)] < b)
            valores[t.nome] = float(np.mean(r[m]) * 1000.0) if valido and m.any() else None
        linha["iri"] = valores
        vals = [v for v in valores.values() if v is not None]
        linha["media"] = float(np.mean(vals)) if vals else None
        linha["excede"] = bool(limite is not None and linha["media"] is not None and linha["media"] > limite)
        segmentos.append(linha)
    validos = [s["media"] for s in segmentos if s["media"] is not None]
    if not validos:
        raise ValueError("Nenhum segmento do perfil ficou sem falha de dados.")
    return {
        "classe": perfil.classe,
        "dx": perfil.dx,
        "resolucao_m": perfil.resolucao,
        "trilhas": [t.nome for t in perfil.trilhas],
        "extensao": extensao,
        "segmento": float(segmento),
        "limite": limite,
        "media": float(np.mean(validos)),
        "maximo": float(np.max(validos)),
        "minimo": float(np.min(validos)),
        "acima_do_limite": sum(1 for s in segmentos if s["excede"]),
        "segmentos": segmentos,
        "avisos": perfil.avisos,
        "metodo": "Golden Car, 80 km/h, transição de estados (Sayers, TRR 1501, 1995)",
    }


def parcela_ondas_longas(estacas, cotas, segmento=100.0):
    """Resposta do modelo a um perfil de estaqueamento esparso, reamostrado por spline a cada 0,25 m.

    Não é IRI: ondas menores que duas vezes o passo não existem nos dados. Serve só para comparar
    pista existente e greide no mesmo eixo, e dá um piso para o que a geometria impõe à pista pronta.
    """
    s = np.asarray(estacas, dtype=float)
    z = np.asarray(cotas, dtype=float)
    ok = np.isfinite(z)
    if ok.sum() < 6:
        return None
    s, z = s[ok], z[ok]
    if s[-1] - s[0] < max(segmento, 30.0):
        return None
    grade = np.arange(s[0], s[-1], 0.25)
    y = CubicSpline(s, z - z.mean())(grade)
    r = inclinacao_retificada(y, 0.25)
    d = grade[: len(r)]
    n = int((d[-1] - d[0]) // segmento) or 1
    valores = []
    for i in range(n):
        a, b = d[0] + i * segmento, d[0] + (i + 1) * segmento
        m = (d >= a) & (d < b)
        if m.any():
            valores.append({"inicio": float(a), "fim": float(b), "valor": float(np.mean(r[m]) * 1000.0)})
    uteis = [v["valor"] for v in valores]
    return {"media": float(np.mean(r) * 1000.0), "maximo": float(max(uteis)), "segmentos": valores, "onda_minima_m": float(2 * np.median(np.diff(s)))}
