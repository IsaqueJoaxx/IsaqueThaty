"""Projeto dos greides por programação linear.

Greide final: o menor volume possível, com a espessura mínima em todos os pontos da
seção e curvatura vertical limitada. Conformação: o greide que mais cola na pista, com
a seção tipo pedida e sempre abaixo do greide final menos a espessura mínima.
"""
from dataclasses import dataclass, field
from typing import List, Optional

import numpy as np
from scipy import sparse
from scipy.optimize import linprog

# Coeficiente de atrito transversal (f máx.) por velocidade, em km/h.
F_MAX = {30: 0.20, 40: 0.18, 50: 0.16, 60: 0.15, 70: 0.15, 80: 0.14, 90: 0.14, 100: 0.13, 110: 0.12, 120: 0.11}

MODO_TERRENO = "terreno"
MODO_CONFORMACAO = "conformacao"


@dataclass
class Parametros:
    passo: float = 5.0
    sta_ini: Optional[float] = None
    sta_fim: Optional[float] = None
    larg_pista: float = 3.50
    larg_acost: float = 2.00
    esp_min: float = 0.23
    inc_conf: float = 3.0  # % de caimento da pista na conformação
    inc_conf_acost: float = 3.0  # % de caimento do acostamento na conformação
    inc_final: float = 2.0  # % de caimento no corredor final
    d2_mm_5m: float = 2.5  # variação máxima de cota entre lances de 5 m (segunda diferença)
    modo: str = MODO_TERRENO
    peso_aterro: float = 1.0
    peso_fresagem: float = 1.0
    fres_max: Optional[float] = None
    super_ativa: bool = False
    emax: float = 6.0
    velocidade: float = 80.0
    transicao: float = 60.0
    lacuna_max: float = 50.0
    aresta_max: float = 50.0

    @property
    def afastamentos(self):
        b = self.larg_pista
        a = self.larg_pista + self.larg_acost
        return np.array([-a, -b, 0.0, b, a])

    @property
    def pesos_secao(self):
        """Pesos de integração da seção (trapézios); a soma é a largura total."""
        b, c = self.larg_pista, self.larg_acost
        return np.array([c / 2, (c + b) / 2, b, (c + b) / 2, c / 2])

    @property
    def k_equivalente(self):
        """K (m/%) equivalente ao limite de segunda diferença."""
        return 25.0 / (100.0 * self.d2_mm_5m / 1000.0)


@dataclass
class Resultado:
    estacas: np.ndarray
    afastamentos: np.ndarray
    terreno: np.ndarray
    preenchido: np.ndarray
    greide_conf: np.ndarray
    greide_final: np.ndarray
    cotas_conf: np.ndarray
    cotas_final: np.ndarray
    parametros: Parametros
    avisos: List[str] = field(default_factory=list)
    resumo: dict = field(default_factory=dict)


def blocos_validos(estacas, valido, lacuna_max):
    """Trechos contínuos com dados, separados por falhas maiores que lacuna_max."""
    idx = np.flatnonzero(valido)
    if idx.size == 0:
        return []
    cortes = np.flatnonzero(np.diff(estacas[idx]) > lacuna_max)
    inicios = np.r_[idx[0], idx[cortes + 1]]
    fins = np.r_[idx[cortes], idx[-1]]
    return list(zip(inicios.tolist(), fins.tolist()))


def preencher_falhas(estacas, z):
    """Interpola ao longo do eixo as falhas internas de cada linha da seção."""
    z = z.copy()
    falhas = ~np.isfinite(z)
    for k in range(z.shape[1]):
        ok = np.isfinite(z[:, k])
        if ok.sum() >= 2 and (~ok).any():
            z[~ok, k] = np.interp(estacas[~ok], estacas[ok], z[ok, k], left=np.nan, right=np.nan)
    return z, falhas & np.isfinite(z)


def inclinacoes(n_pct, n_acost_pct, curvatura, p):
    """Declividade de cada ponto (m/m, positivo = sobe para fora do eixo)."""
    n = n_pct / 100.0
    na = n_acost_pct / 100.0
    theta_sinal = np.zeros_like(curvatura)
    if p.super_ativa:
        emax = p.emax / 100.0
        v = float(p.velocidade)
        f = F_MAX.get(int(round(v)), 0.14)
        rmin = v * v / (127.0 * (emax + f))
        with np.errstate(divide="ignore"):
            r = np.where(curvatura != 0, 1.0 / np.abs(curvatura), np.inf)
        x = np.minimum(rmin / r, 1.0)
        e_alvo = emax * (2 * x - x * x)
        # Abaixo do abaulamento normal não há superelevação.
        e_alvo = np.where(e_alvo >= n, e_alvo, 0.0)
        passo_jan = max(1, int(round(p.transicao / p.passo)))
        if passo_jan % 2 == 0:
            passo_jan += 1
        janela = np.ones(passo_jan) / passo_jan
        theta_sinal = np.convolve(e_alvo * np.sign(curvatura), janela, mode="same")
    theta = np.abs(theta_sinal)
    externo = np.where(theta <= n, -n + 2 * theta, theta)
    interno = np.where(theta <= n, -n, -theta)
    # Curva à esquerda (sinal +): esquerda é o lado interno.
    esquerda = np.where(theta_sinal > 0, interno, np.where(theta_sinal < 0, externo, -n))
    direita = np.where(theta_sinal > 0, externo, np.where(theta_sinal < 0, interno, -n))
    fator = np.clip(1.0 - theta / max(n, 1e-9), 0.0, 1.0)
    dif = (na - n) * fator
    return esquerda, direita, esquerda - dif, direita - dif


def desniveis(n_pct, n_acost_pct, curvatura, p):
    """Desnível de cada ponto da seção em relação ao eixo: forma (n, 5)."""
    esq, dir_, esq_a, dir_a = inclinacoes(n_pct, n_acost_pct, curvatura, p)
    b, c = p.larg_pista, p.larg_acost
    return np.column_stack(
        [esq * b + esq_a * c, esq * b, np.zeros_like(esq), dir_ * b, dir_ * b + dir_a * c]
    )


def _matriz_curvatura(n, deslocamento=0, total=None):
    total = n if total is None else total
    i = np.arange(n - 2)
    linhas = np.r_[i, i, i]
    colunas = np.r_[i, i + 1, i + 2] + deslocamento
    valores = np.r_[np.ones(n - 2), -2 * np.ones(n - 2), np.ones(n - 2)]
    return sparse.csr_matrix((valores, (linhas, colunas)), shape=(n - 2, total))


def _pesos_longitudinais(s):
    d = np.diff(s)
    return np.r_[d[0] / 2, (d[:-1] + d[1:]) / 2, d[-1] / 2]


def _resolver(c, a_ub, b_ub, limites, etapa):
    r = linprog(c, A_ub=a_ub, b_ub=b_ub, bounds=limites, method="highs")
    if r.status != 0:
        raise RuntimeError(f"A otimização de {etapa} não convergiu: {r.message}")
    return r.x


def greide_envelope(s, inferior, delta):
    """Menor greide com g >= inferior e |segunda diferença| <= delta."""
    n = len(s)
    z0 = float(np.mean(inferior))
    a = _matriz_curvatura(n)
    a_ub = sparse.vstack([a, -a]).tocsr()
    b_ub = np.full(2 * (n - 2), delta)
    limites = np.column_stack([inferior - z0, np.full(n, np.inf)])
    return _resolver(_pesos_longitudinais(s), a_ub, b_ub, limites, "greide final") + z0


def greide_colado(s, alvos, pesos, delta, peso_aterro, peso_fresagem, teto=None, piso=None):
    """Greide que minimiza o desvio absoluto em relação aos alvos de cada ponto da pista.

    alvos: cota do eixo que zeraria o desvio em cada ponto (n, k).
    teto, piso: limites de cota do eixo, por estaca.
    """
    n, k = alvos.shape
    z0 = float(np.nanmean(alvos))
    b = alvos - z0
    ordem = np.argsort(b, axis=1)
    bs = np.take_along_axis(b, ordem, axis=1)
    ps = np.broadcast_to(pesos, (n, k))
    ps = np.take_along_axis(ps, ordem, axis=1)

    linhas, colunas, valores, lado = [], [], [], []
    linha = 0
    idx = np.arange(n)
    for r in range(k + 1):
        inclinacao = peso_aterro * ps[:, :r].sum(axis=1) - peso_fresagem * ps[:, r:].sum(axis=1)
        intercepto = (
            -peso_aterro * (ps[:, :r] * bs[:, :r]).sum(axis=1)
            + peso_fresagem * (ps[:, r:] * bs[:, r:]).sum(axis=1)
        )
        # inclinacao*g - t <= -intercepto
        lin = linha + idx
        linhas += [lin, lin]
        colunas += [idx, n + idx]
        valores += [inclinacao, -np.ones(n)]
        lado.append(-intercepto)
        linha += n
    desvio = sparse.csr_matrix(
        (np.concatenate(valores), (np.concatenate(linhas), np.concatenate(colunas))),
        shape=(linha, 2 * n),
    )
    a = _matriz_curvatura(n, total=2 * n)
    a_ub = sparse.vstack([desvio, a, -a]).tocsr()
    b_ub = np.r_[np.concatenate(lado), np.full(2 * (n - 2), delta)]

    inf_g = np.full(n, -np.inf) if piso is None else piso - z0
    sup_g = np.full(n, np.inf) if teto is None else teto - z0
    if piso is not None and teto is not None:
        inf_g = np.minimum(inf_g, sup_g)
    limites = np.vstack(
        [np.column_stack([inf_g, sup_g]), np.column_stack([np.full(n, -np.inf), np.full(n, np.inf)])]
    )
    c = np.r_[np.zeros(n), _pesos_longitudinais(s)]
    return _resolver(c, a_ub, b_ub, limites, "conformação")[:n] + z0


def volume(s, espessuras, pesos):
    """Volume pela média das áreas. NaN conta como zero."""
    area = np.nansum(np.nan_to_num(espessuras) * pesos, axis=1)
    return float(np.sum((area[1:] + area[:-1]) * np.diff(s)) / 2)


def projetar(estacas, terreno, curvatura, p, avisos=None, preenchido=None):
    """Calcula os dois greides. Terreno: forma (n, 5), com cotas arredondadas ao mm."""
    avisos = [] if avisos is None else avisos
    s = np.asarray(estacas, dtype=float)
    n = len(s)
    if n < 4:
        raise ValueError("São necessárias ao menos 4 estacas.")
    h = float(np.median(np.diff(s)))
    delta = (p.d2_mm_5m / 1000.0) * (h / 5.0) ** 2

    d_conf = desniveis(p.inc_conf, p.inc_conf_acost, curvatura, p)
    d_final = desniveis(p.inc_final, p.inc_final, curvatura, p)
    pista = [1, 2, 3]
    a = p.larg_pista
    pesos_pista = np.array([a / 2, a, a / 2])
    alvos = terreno[:, pista] - d_conf[:, pista]
    alvos_cheios = np.where(np.isfinite(alvos), alvos, np.nanmean(alvos, axis=1)[:, None])
    # Limite de fresagem: o eixo não pode descer mais que fres_max abaixo do ponto mais alto.
    piso = None if p.fres_max is None else np.nanmax(alvos, axis=1) - p.fres_max

    e = p.esp_min
    if p.modo == MODO_TERRENO:
        inferior = np.nanmax(terreno - d_final + e, axis=1)
        if piso is not None:
            # Com limite de fresagem, o final precisa dar espaço à conformação.
            inferior = np.maximum(inferior, piso + np.max(d_conf - d_final + e, axis=1))
        g_final = greide_envelope(s, inferior, delta)
        # Conformação sempre pelo menos a espessura mínima abaixo do final, em todos os pontos.
        teto = np.min(g_final[:, None] + d_final - d_conf - e, axis=1)
        g_conf = greide_colado(
            s, alvos_cheios, pesos_pista, delta, p.peso_aterro, p.peso_fresagem, teto=teto, piso=piso
        )
    elif p.modo == MODO_CONFORMACAO:
        g_conf = greide_colado(
            s, alvos_cheios, pesos_pista, delta, p.peso_aterro, p.peso_fresagem, piso=piso
        )
        inferior = np.max(g_conf[:, None] + d_conf - d_final + e, axis=1)
        g_final = greide_envelope(s, inferior, delta)
    else:
        raise ValueError(f"Modo desconhecido: {p.modo}")

    cotas_conf = np.round(g_conf[:, None] + d_conf, 3)
    cotas_final = np.round(g_final[:, None] + d_final, 3)
    # O arredondamento ao mm não pode tirar 1 mm da espessura sobre a conformação.
    if p.modo == MODO_TERRENO:
        cotas_conf = np.minimum(cotas_conf, np.round(cotas_final - e, 3))
    else:
        cotas_final = np.maximum(cotas_final, np.round(cotas_conf + e, 3))
    res = Resultado(
        estacas=s,
        afastamentos=p.afastamentos,
        terreno=terreno,
        preenchido=np.zeros_like(terreno, bool) if preenchido is None else preenchido,
        greide_conf=g_conf,
        greide_final=g_final,
        cotas_conf=cotas_conf,
        cotas_final=cotas_final,
        parametros=p,
        avisos=avisos,
    )
    res.resumo = resumir(res)
    return res


def resumir(res):
    p = res.parametros
    s = res.estacas
    pesos = p.pesos_secao
    esp = res.cotas_final - res.terreno
    sobre_conf = res.cotas_final - res.cotas_conf
    dif_conf = res.cotas_conf - res.terreno
    m_pista = np.array([0, p.larg_pista / 2, p.larg_pista, p.larg_pista / 2, 0.0])
    resumo = {
        "extensao": float(s[-1] - s[0]),
        "n_estacas": len(s),
        "esp_min": float(np.nanmin(np.round(esp, 3))),
        "esp_media": float(volume(s, esp, pesos) / max(1e-9, (s[-1] - s[0]) * pesos.sum())),
        "esp_max": float(np.nanmax(esp)),
        "sobre_conf_min": float(np.min(np.round(sobre_conf, 3))),
        "vol_final_terreno": volume(s, esp, pesos),
        "vol_final_conf": volume(s, sobre_conf, pesos),
        "vol_teorico": float((s[-1] - s[0]) * pesos.sum() * p.esp_min),
        "vol_conf_aterro": volume(s, np.clip(dif_conf, 0, None), m_pista),
        "vol_conf_fresagem": volume(s, np.clip(-dif_conf, 0, None), m_pista),
        "dif_conf_max": float(np.nanmax(dif_conf[:, 1:4])),
        "dif_conf_min": float(np.nanmin(dif_conf[:, 1:4])),
        "d2_final_max_mm": float(np.max(np.abs(np.diff(res.greide_final, 2))) * 1000),
        "d2_conf_max_mm": float(np.max(np.abs(np.diff(res.greide_conf, 2))) * 1000),
        "pontos_sem_dado": int(np.sum(~np.isfinite(res.terreno))),
        "pontos_interpolados": int(np.sum(res.preenchido)),
    }
    return resumo


def processar(eixo, levantamento, p, log=print):
    """Fluxo completo: amostra o levantamento e projeta o trecho contínuo principal."""
    avisos = []
    estacas = eixo.grade(p.passo, p.sta_ini, p.sta_fim)
    log(f"Amostrando {len(estacas)} estacas em 5 pontos por seção...")
    z = levantamento.amostrar(eixo, estacas, p.afastamentos)
    valido = np.isfinite(z[:, [1, 2, 3]]).all(axis=1)
    blocos = blocos_validos(estacas, valido, p.lacuna_max)
    if not blocos:
        raise ValueError(
            "O levantamento não cobre o eixo e os bordos em nenhuma estaca. Confira se as "
            "coordenadas estão no mesmo sistema do eixo (SIRGAS 2000 / UTM 23S) e se a "
            "ordem N/E está certa."
        )
    i0, i1 = max(blocos, key=lambda b: estacas[b[1]] - estacas[b[0]])
    if len(blocos) > 1:
        outros = [f"{estacas[a]:.0f} a {estacas[b]:.0f}" for a, b in blocos if (a, b) != (i0, i1)]
        amostra = "; ".join(outros[:6]) + (" ..." if len(outros) > 6 else "")
        avisos.append(
            f"O levantamento tem {len(blocos)} trechos separados. Calculei o maior; "
            f"para os outros, informe a estaca inicial e a final: {amostra}."
        )
    estacas = estacas[i0 : i1 + 1]
    z = z[i0 : i1 + 1]
    if len(estacas) < 4:
        raise ValueError("O trecho com dados tem menos de 4 estacas.")

    z = np.round(z, 3)
    z, preenchido = preencher_falhas(estacas, z)
    if preenchido.any():
        avisos.append(
            f"Cotas interpoladas ao longo do eixo por falta de levantamento: {int(preenchido.sum())} "
            "(em azul nas notas)."
        )
    faltando = ~np.isfinite(z)
    if faltando.any():
        por_ponto = faltando.sum(axis=0)
        avisos.append(
            f"Cotas sem terreno: {int(faltando.sum())} "
            f"(LE acost. {por_ponto[0]}, LE bordo {por_ponto[1]}, eixo {por_ponto[2]}, "
            f"LD bordo {por_ponto[3]}, LD acost. {por_ponto[4]}). Nelas a espessura não foi conferida."
        )
    _, _, _, _, k = eixo.pontos(estacas)
    log(f"Otimizando {len(estacas)} estacas: {estacas[0]:.0f} a {estacas[-1]:.0f}...")
    res = projetar(estacas, z, k, p, avisos, preenchido)
    for a in avisos:
        log("AVISO: " + a)
    return res
