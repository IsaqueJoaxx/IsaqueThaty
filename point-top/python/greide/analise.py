"""Análise do levantamento e conferência do greide proposto.

As conferências recalculam tudo a partir das cotas da nota de serviço, sem usar as restrições
do otimizador, para que um erro no projeto do greide apareça aqui.
"""
import re

import numpy as np

from geometria import fmt_estaca
from otimizador import F_MAX

OK, ATENCAO, FALHA, INFO = "ok", "atencao", "falha", "info"
TOL_CAIMENTO = 0.05  # %, ruído do arredondamento ao mm em 3,5 m
NOMES = ["Acostamento LE", "Bordo LE", "Eixo", "Bordo LD", "Acostamento LD"]


def _estaca(s):
    return fmt_estaca(float(s))


def _mediana_movel(z, janela=5):
    meia = janela // 2
    q = np.pad(z, meia, mode="edge")
    return np.median(np.lib.stride_tricks.sliding_window_view(q, janela), axis=1)


def analisar_levantamento(res, descricao, curvatura):
    """O que o levantamento mostra antes de qualquer projeto."""
    p = res.parametros
    s, z = res.estacas, res.terreno
    h = float(np.median(np.diff(s)))
    limite_d2 = p.d2_mm_5m * (h / 5.0) ** 2
    eixo = z[:, 2]
    ok = np.isfinite(eixo)
    d2 = np.abs(np.diff(eixo[ok], 2)) * 1000
    largura = p.larg_pista
    caim_e = (z[:, 1] - z[:, 2]) / largura * 100
    caim_d = (z[:, 3] - z[:, 2]) / largura * 100
    rampa = np.diff(eixo) / np.diff(s) * 100

    # Candidatos a erro de levantamento: ponto que foge da vizinhança ao longo do eixo.
    esp = np.round(res.cotas_final - z, 3)
    esp_secao = np.nanmin(esp, axis=1)
    controle = esp_secao <= p.esp_min + 0.002
    picos = []
    for k in range(z.shape[1]):
        col = z[:, k]
        valido = np.isfinite(col)
        if valido.sum() < 8:
            continue
        idx = np.flatnonzero(valido)
        residuo = col[idx] - _mediana_movel(col[idx])
        mad = float(np.median(np.abs(residuo - np.median(residuo)))) * 1.4826
        corte = max(0.015, 5 * mad)
        for j in np.flatnonzero(np.abs(residuo) > corte):
            i = idx[j]
            vizinhos = controle[max(0, i - 1) : i + 2]
            picos.append(
                {
                    "estaca": float(s[i]),
                    "estaca_txt": _estaca(s[i]),
                    "ponto": NOMES[k],
                    "residuo_mm": round(float(residuo[j]) * 1000, 1),
                    "governa": bool(residuo[j] > 0 and vizinhos.any()),
                }
            )
    picos.sort(key=lambda q: (not q["governa"], -abs(q["residuo_mm"])))

    return {
        "fonte": descricao,
        "extensao_m": float(s[-1] - s[0]),
        "estacas": int(len(s)),
        "passo_m": h,
        "cotas_sem_dado": int(np.sum(~np.isfinite(z))),
        "cotas_interpoladas": int(np.sum(res.preenchido)),
        "rampa_max_pct": float(np.nanmax(np.abs(rampa))),
        "ondulacao_mm": {
            "limite": round(limite_d2, 3),
            "mediana": float(np.median(d2)),
            "p95": float(np.percentile(d2, 95)),
            "maxima": float(np.max(d2)),
            "pct_acima": float(np.mean(d2 > limite_d2) * 100),
        },
        "caimento_pct": {
            "LE": {"mediana": float(np.nanmedian(caim_e)), "min": float(np.nanmin(caim_e)), "max": float(np.nanmax(caim_e))},
            "LD": {"mediana": float(np.nanmedian(caim_d)), "min": float(np.nanmin(caim_d)), "max": float(np.nanmax(caim_d))},
        },
        "secoes_que_governam": int(np.sum(controle)),
        "picos_total": len(picos),
        "picos": picos[:12],
        "extensao_em_curva_m": float(np.sum(np.diff(s) * (curvatura[:-1] != 0))),
    }


def _virgula(texto):
    return re.sub(r"(?<=\d)\.(?=\d)", ",", texto) if texto else texto


def _item(nome, limite, valor, situacao, local=None, nota=None):
    return {"item": nome, "limite": _virgula(limite), "valor": _virgula(valor), "situacao": situacao, "local": local, "nota": _virgula(nota)}


def _caimento(cotas, largura):
    return (cotas[:, 1] - cotas[:, 2]) / largura * 100, (cotas[:, 3] - cotas[:, 2]) / largura * 100


def verificar(res, curvatura, limites):
    """Lista de conferências do greide proposto. Limites: rampa_max, rampa_min, k_convexa, k_concava."""
    p = res.parametros
    s = res.estacas
    h = float(np.median(np.diff(s)))
    itens = []

    esp = np.round(res.cotas_final - res.terreno, 3)
    i, k = np.unravel_index(np.nanargmin(esp), esp.shape)
    v = float(esp[i, k])
    itens.append(_item("Espessura mínima sobre a pista existente", f"≥ {p.esp_min:.3f} m", f"{v:.3f} m",
                       OK if v >= p.esp_min - 1e-9 else FALHA, f"{_estaca(s[i])} · {NOMES[k]}"))
    sobre = np.round(res.cotas_final - res.cotas_conf, 3)
    i2, k2 = np.unravel_index(np.argmin(sobre), sobre.shape)
    v2 = float(sobre[i2, k2])
    itens.append(_item("Espessura mínima sobre a conformação", f"≥ {p.esp_min:.3f} m", f"{v2:.3f} m",
                       OK if v2 >= p.esp_min - 1e-9 else FALHA, f"{_estaca(s[i2])} · {NOMES[k2]}"))
    i3, k3 = np.unravel_index(np.nanargmax(esp), esp.shape)
    itens.append(_item("Espessura máxima", "informativo", f"{float(esp[i3, k3]):.3f} m", INFO, f"{_estaca(s[i3])} · {NOMES[k3]}"))

    g = res.cotas_final[:, 2]
    rampa = np.diff(g) / np.diff(s) * 100
    j = int(np.argmax(np.abs(rampa)))
    v = float(abs(rampa[j]))
    itens.append(_item("Rampa máxima do greide", f"≤ {limites['rampa_max']:.2f} %", f"{v:.2f} %",
                       OK if v <= limites["rampa_max"] else FALHA, _estaca(s[j])))
    if limites["rampa_min"] > 0:
        baixa = np.abs(rampa) < limites["rampa_min"]
        ext = float(np.sum(np.diff(s)[baixa]))
        itens.append(_item("Rampa mínima, para escoamento", f"≥ {limites['rampa_min']:.2f} %", f"{ext:.0f} m abaixo",
                           OK if ext == 0 else ATENCAO, _estaca(s[int(np.argmax(baixa))]) if ext else None))

    limite_d2 = p.d2_mm_5m * (h / 5.0) ** 2
    d2_calc = np.abs(np.diff(res.greide_final, 2)) * 1000
    j = int(np.argmax(d2_calc))
    v = float(d2_calc[j])
    itens.append(_item(f"Variação de cota entre lances de {h:g} m, greide calculado", f"≤ {limite_d2:.2f} mm", f"{v:.2f} mm",
                       OK if v <= limite_d2 + 1e-6 else FALHA, _estaca(s[j + 1])))
    d2_nota = np.abs(np.diff(g, 2)) * 1000
    j = int(np.argmax(d2_nota))
    v = float(d2_nota[j])
    itens.append(_item(f"Variação de cota entre lances de {h:g} m, cotas da nota (mm)", f"≤ {limite_d2:.2f} mm", f"{v:.2f} mm",
                       OK if v <= limite_d2 + 1e-6 else ATENCAO, _estaca(s[j + 1]),
                       None if v <= limite_d2 + 1e-6 else "Efeito do arredondamento das cotas ao milímetro."))

    variacao = np.diff(np.diff(res.greide_final) / np.diff(s) * 100)
    with np.errstate(divide="ignore", invalid="ignore"):
        k_local = h / np.abs(variacao)
    for nome, sinal, chave in (("convexa", variacao < -1e-9, "k_convexa"), ("côncava", variacao > 1e-9, "k_concava")):
        if sinal.any():
            pos = np.flatnonzero(sinal)
            j = int(pos[np.argmin(k_local[pos])])
            v = float(k_local[j])
            itens.append(_item(f"K mínimo em curva {nome}", f"≥ {limites[chave]:.0f} m/%", f"{v:.0f} m/%",
                               OK if v >= limites[chave] else FALHA, _estaca(s[j + 1])))

    for rotulo, cotas, alvo in (("final", res.cotas_final, p.inc_final), ("conformação", res.cotas_conf, p.inc_conf)):
        e, d = _caimento(cotas, p.larg_pista)
        if p.super_ativa:
            v = float(max(np.max(np.abs(e)), np.max(np.abs(d))))
            lim = max(p.emax, alvo)
            itens.append(_item(f"Caimento transversal máximo, {rotulo}", f"≤ {lim:.2f} %", f"{v:.2f} %",
                               OK if v <= lim + TOL_CAIMENTO else FALHA))
        else:
            desvio = np.maximum(np.abs(e + alvo), np.abs(d + alvo))
            j = int(np.argmax(desvio))
            itens.append(_item(f"Caimento transversal da pista, {rotulo}", f"-{alvo:.2f} % nos dois lados", f"desvio de {float(desvio[j]):.3f} %",
                               OK if desvio[j] <= TOL_CAIMENTO else FALHA, _estaca(s[j])))

    em_curva = curvatura != 0
    if em_curva.any():
        raio = float(np.min(1.0 / np.abs(curvatura[em_curva])))
        v_kmh = float(p.velocidade)
        f = F_MAX.get(int(round(v_kmh)), 0.14)
        rmin = v_kmh * v_kmh / (127.0 * (p.emax / 100.0 + f))
        itens.append(_item(f"Raio mínimo do eixo para {v_kmh:.0f} km/h", f"≥ {rmin:.0f} m", f"{raio:.0f} m",
                           OK if raio >= rmin else FALHA, _estaca(s[em_curva][int(np.argmin(1.0 / np.abs(curvatura[em_curva])))]),
                           f"e máx. {p.emax:g} %, f {f:g}"))
        if not p.super_ativa:
            ext = float(np.sum(np.diff(s) * em_curva[:-1]))
            itens.append(_item("Superelevação", "conferir", f"desligada, com {ext:.0f} m em curva", ATENCAO, None,
                               "O caimento normal foi mantido nas curvas. Ligue a superelevação se o projeto pedir."))

    dif = res.cotas_conf[:, 1:4] - res.terreno[:, 1:4]
    if np.isfinite(dif).any():
        fres = float(-np.nanmin(dif))
        i4, k4 = np.unravel_index(np.nanargmin(dif), dif.shape)
        itens.append(_item("Fresagem máxima na conformação", f"≤ {p.fres_max:.3f} m" if p.fres_max is not None else "informativo",
                           f"{max(fres, 0):.3f} m",
                           INFO if p.fres_max is None else (OK if fres <= p.fres_max + 0.001 else FALHA),
                           f"{_estaca(s[i4])} · {NOMES[k4 + 1]}"))

    sem = int(np.sum(~np.isfinite(res.terreno)))
    if sem:
        itens.append(_item("Cotas sem levantamento", "0", str(sem), ATENCAO, None, "Nesses pontos a espessura não foi conferida."))
    inter = int(np.sum(res.preenchido))
    if inter:
        itens.append(_item("Cotas interpoladas", "0", str(inter), ATENCAO, None, "Confira em campo antes de usar a nota nesses pontos."))
    return itens


def situacao_geral(itens):
    estados = {i["situacao"] for i in itens}
    if FALHA in estados:
        return FALHA
    return ATENCAO if ATENCAO in estados else OK
