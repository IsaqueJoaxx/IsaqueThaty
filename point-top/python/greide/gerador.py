"""Fluxo do gerador de greide: lê, analisa, projeta, confere e grava tudo em pasta exclusiva."""
import base64
import html
import json
import math
from datetime import datetime
from pathlib import Path
from tempfile import mkdtemp

import numpy as np

import analise
import iri as modulo_iri
import saidas
from geometria import Eixo
from otimizador import MODO_CONFORMACAO, MODO_TERRENO, Parametros, processar
from terreno import Levantamento

VERSAO = "0.1.0"
ROTULOS = {"ok": "OK", "atencao": "Atenção", "falha": "Falha", "info": "Info"}

# (mínimo, máximo) dos parâmetros numéricos aceitos. None no máximo: sem limite superior.
FAIXAS = {
    "passo": (1, 50), "sta_ini": (None, None), "sta_fim": (None, None), "larg_pista": (1, 15), "larg_acost": (0.1, 10),
    "esp_min": (0, 2), "inc_conf": (0, 10), "inc_final": (0, 10), "d2_mm_5m": (0.1, 100), "fres_max": (0, 5),
    "emax": (0, 12), "velocidade": (20, 140), "transicao": (5, 500),
}
LIMITES_PADRAO = {"rampa_max": 6.0, "rampa_min": 0.0, "k_convexa": 30.0, "k_concava": 30.0}


def montar_parametros(entrada):
    """Aceita só os parâmetros conhecidos e dentro da faixa."""
    p = Parametros()
    for chave, (minimo, maximo) in FAIXAS.items():
        v = entrada.get(chave)
        if v is None or v == "":
            continue
        if isinstance(v, bool) or not isinstance(v, (int, float)) or not math.isfinite(v):
            raise ValueError(f"Valor inválido em {chave}.")
        if (minimo is not None and v < minimo) or (maximo is not None and v > maximo):
            raise ValueError(f"{chave} fora da faixa aceita ({minimo} a {maximo}).")
        setattr(p, chave, float(v))
    p.inc_conf_acost = p.inc_conf
    modo = entrada.get("modo", MODO_TERRENO)
    if modo not in (MODO_TERRENO, MODO_CONFORMACAO):
        raise ValueError("Modo de projeto inválido.")
    p.modo = modo
    p.super_ativa = entrada.get("super_ativa") is True
    if p.sta_ini is not None and p.sta_fim is not None and p.sta_fim <= p.sta_ini:
        raise ValueError("A estaca final precisa ser maior que a inicial.")
    return p


def montar_limites(entrada):
    limites = dict(LIMITES_PADRAO)
    for chave in limites:
        v = entrada.get(chave)
        if v is None or v == "":
            continue
        if isinstance(v, bool) or not isinstance(v, (int, float)) or not math.isfinite(v) or v < 0:
            raise ValueError(f"Valor inválido em {chave}.")
        limites[chave] = float(v)
    return limites


def _limpo(v):
    """Deixa o resultado aceitável em JSON: sem NaN e sem tipos do NumPy."""
    if isinstance(v, dict):
        return {k: _limpo(x) for k, x in v.items()}
    if isinstance(v, (list, tuple)):
        return [_limpo(x) for x in v]
    if isinstance(v, np.ndarray):
        return [_limpo(x) for x in v.tolist()]
    if isinstance(v, (np.floating, float)):
        v = float(v)
        return v if math.isfinite(v) else None
    if isinstance(v, np.integer):
        return int(v)
    if isinstance(v, np.bool_):
        return bool(v)
    return v


def _serie(res):
    n = len(res.estacas)
    i = np.unique(np.linspace(0, n - 1, min(n, 1500)).astype(int))
    esp = res.cotas_final - res.terreno
    return {
        "estaca": np.round(res.estacas[i], 3),
        "terreno": np.round(res.terreno[i, 2], 3),
        "conformacao": np.round(res.cotas_conf[i, 2], 3),
        "final": np.round(res.cotas_final[i, 2], 3),
        "espessura_min": np.round(np.nanmin(esp[i], axis=1), 3),
    }


def _tabela(cab, linhas):
    h = "".join(f"<th>{html.escape(analise._virgula(str(c)))}</th>" for c in cab)
    b = "".join("<tr>" + "".join(f"<td>{html.escape('' if c is None else analise._virgula(str(c)))}</td>" for c in l) + "</tr>" for l in linhas)
    return f"<table><thead><tr>{h}</tr></thead><tbody>{b}</tbody></table>"


def _relatorio(r, pasta):
    a = r["levantamento"]
    t = r["resumo"]
    partes = [
        "<!doctype html><meta charset='utf-8'><title>Greide — POINT TOP</title>",
        "<style>body{font:14px Arial;margin:32px;color:#18334a}h1{font-size:22px}h2{font-size:16px;margin-top:26px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ccd;padding:6px 8px;text-align:left;vertical-align:top}th{background:#eef3f7}.aviso{background:#fff5d6;padding:8px 12px}img{max-width:100%}@media print{body{margin:10px}}</style>",
        "<h1>Gerador de greide — POINT TOP</h1>",
    ]
    if r["sintetico"]:
        partes.append("<p class='aviso'><b>DADOS SINTÉTICOS.</b> Exemplo para conhecer a função. Não representa nenhuma obra.</p>")
    partes.append(f"<p>Situação das conferências: <b>{ROTULOS[r['situacao']]}</b>. Gerado em {html.escape(r['data'].replace('T', ' às '))}. Rascunho técnico: a validação do projeto é do responsável.</p>")
    partes.append("<h2>Resultado</h2>" + _tabela(["Grandeza", "Valor"], [
        ["Extensão", f"{t['extensao']:.0f} m em {t['n_estacas']} estacas"],
        ["Espessura mínima, média e máxima", f"{t['esp_min']:.3f} / {t['esp_media']:.3f} / {t['esp_max']:.3f} m"],
        ["Volume do terreno ao greide final", f"{t['vol_final_terreno']:.1f} m³ (mínimo teórico {t['vol_teorico']:.1f} m³)"],
        ["Conformação", f"aterro {t['vol_conf_aterro']:.1f} m³; fresagem {t['vol_conf_fresagem']:.1f} m³"],
    ]))
    partes.append("<h2>Conferências</h2>" + _tabela(["Item", "Limite", "Valor", "Situação", "Local", "Nota"], [
        [i["item"], i["limite"], i["valor"], ROTULOS[i["situacao"]], i["local"], i["nota"]] for i in r["verificacoes"]
    ]))
    o = a["ondulacao_mm"]
    partes.append("<h2>Levantamento</h2>" + _tabela(["Grandeza", "Valor"], [
        ["Fonte", a["fonte"]],
        ["Cotas sem dado e interpoladas", f"{a['cotas_sem_dado']} e {a['cotas_interpoladas']}"],
        [f"Variação de cota entre lances, no eixo (limite {o['limite']:.2f} mm)", f"mediana {o['mediana']:.1f}; P95 {o['p95']:.1f}; máxima {o['maxima']:.1f} mm; {o['pct_acima']:.0f} % acima do limite"],
        ["Caimento medido LE e LD (medianas)", f"{a['caimento_pct']['LE']['mediana']:.2f} % e {a['caimento_pct']['LD']['mediana']:.2f} %"],
        ["Seções que governam o greide", str(a["secoes_que_governam"])],
    ]))
    if a["picos"]:
        partes.append(f"<h2>Pontos suspeitos ({a['picos_total']} no total)</h2>" + _tabela(["Estaca", "Ponto", "Desvio da vizinhança (mm)", "Governa o greide"], [
            [q["estaca_txt"], q["ponto"], q["residuo_mm"], "Sim" if q["governa"] else "Não"] for q in a["picos"]
        ]))
    partes.append("<h2>Irregularidade</h2>")
    i = r["iri"]
    if i["status"] == "calculado":
        partes.append(f"<p>IRI de perfil longitudinal, com pontos a cada {analise._virgula(format(i['dx'], '.2f'))} m. Médio {analise._virgula(format(i['media'], '.2f'))} m/km. {html.escape(i['metodo'])}.</p>")
        partes.append(_tabela(["Início (m)", "Fim (m)", "IRI médio (m/km)", "Observação"], [
            [f"{s['inicio']:.0f}", f"{s['fim']:.0f}", "—" if s["media"] is None else f"{s['media']:.2f}",
             "Falha de dados" if not s["valido"] else ("Acima do limite" if s["excede"] else ("Sem trecho de aquecimento" if s["aquecimento"] else ""))]
            for s in i["segmentos"]
        ]))
        partes += [f"<p class='aviso'>{html.escape(x)}</p>" for x in i["avisos"]]
    else:
        partes.append(f"<p class='aviso'>{html.escape(i['motivo'])}</p>")
    pl = r["ondas_longas"]
    if pl:
        partes.append("<h3>Ondas longas, a partir das estacas</h3><p>Isto <b>não é IRI</b>. É a resposta do mesmo modelo a ondas de "
                      f"{pl['onda_minima_m']:.0f} m ou mais, em m/km, para comparar a pista existente com o greide no eixo. Ondas menores não existem nestes dados, e a pista pronta terá também as ondas curtas da execução: o IRI medido depois será maior.</p>")
        partes.append(_tabela(["Perfil", "Média", "Máxima por segmento"], [[n, f"{pl[c]['media']:.2f}", f"{pl[c]['maximo']:.2f}"] for n, c in (("Pista existente", "terreno"), ("Conformação", "conformacao"), ("Greide final", "final"))]))
    png = pasta / "Perfil.png"
    if png.exists():
        partes.append("<h2>Perfil</h2><img alt='Perfil longitudinal e espessuras' src='data:image/png;base64," + base64.b64encode(png.read_bytes()).decode() + "'>")
    if r["avisos"]:
        partes.append("<h2>Avisos</h2><ul>" + "".join(f"<li>{html.escape(x)}</li>" for x in r["avisos"]) + "</ul>")
    (pasta / "relatorio.html").write_text("".join(partes), encoding="utf-8")


def _gravar_iri(i, caminho):
    linhas = ["inicio_m;fim_m;" + ";".join(i["trilhas"]) + ";media;observacao"]
    for s in i["segmentos"]:
        vals = [s["iri"].get(t) for t in i["trilhas"]] + [s["media"]]
        obs = "falha de dados" if not s["valido"] else ("acima do limite" if s["excede"] else ("sem aquecimento" if s["aquecimento"] else ""))
        linhas.append(f"{s['inicio']:.2f};{s['fim']:.2f};" + ";".join("" if v is None else f"{v:.3f}" for v in vals) + f";{obs}")
    caminho.write_text("\n".join(linhas).replace(".", ",") + "\n", encoding="utf-8-sig")


def executar(eixo, levantamento, saida, parametros=None, limites=None, perfil_iri=None, opcoes_iri=None, sintetico=False, progresso=lambda v: None):
    """Executa tudo. Devolve (pasta, resultado, arquivos)."""
    p = montar_parametros(parametros or {})
    lim = montar_limites(limites or {})
    op = opcoes_iri or {}
    saida = Path(saida)
    saida.mkdir(parents=True, exist_ok=True)
    pasta = Path(mkdtemp(prefix=datetime.now().strftime("Greide_%Y%m%d_%H%M%S_"), dir=saida))
    try:
        progresso(3)
        ex = Eixo(eixo)
        lev = Levantamento(levantamento)
        progresso(15)
        if ex.velocidade and "velocidade" not in (parametros or {}):
            p.velocidade = ex.velocidade
        res = processar(ex, lev, p, log=lambda _: None)
        progresso(55)
        _, _, _, _, k = ex.pontos(res.estacas)
        avisos = list(res.avisos)
        if ex.descontinuidade_max > 0.01:
            avisos.append(f"O eixo tem descontinuidade de {ex.descontinuidade_max:.3f} m entre elementos.")
        verificacoes = analise.verificar(res, k, lim)
        levant = analise.analisar_levantamento(res, lev.descricao, k)
        progresso(65)

        if perfil_iri:
            try:
                dados_iri = modulo_iri.calcular(perfil_iri, op.get("segmento", 100.0), op.get("unidade", "m"), op.get("limite"))
                dados_iri["status"] = "calculado"
                _gravar_iri(dados_iri, pasta / "iri.csv")
            except ValueError as exc:
                dados_iri = {"status": "nao_calculado", "motivo": str(exc)}
        else:
            dados_iri = {
                "status": "sem_dados",
                "motivo": "IRI não calculado: nenhum perfil longitudinal foi informado. O levantamento por estacas "
                          f"(a cada {levant['passo_m']:g} m) não serve para IRI, que precisa de pontos a cada 0,30 m ou menos.",
            }
        terreno_eixo = res.terreno[:, 2]
        longas = {
            "terreno": modulo_iri.parcela_ondas_longas(res.estacas, terreno_eixo),
            "conformacao": modulo_iri.parcela_ondas_longas(res.estacas, res.cotas_conf[:, 2]),
            "final": modulo_iri.parcela_ondas_longas(res.estacas, res.cotas_final[:, 2]),
        }
        ondas = None
        if all(longas.values()):
            ondas = {**longas, "onda_minima_m": longas["final"]["onda_minima_m"]}
        progresso(72)

        rotulo = "DADOS SINTÉTICOS" if sintetico else None
        geradores = [
            ("Notas_de_servico.xlsx", lambda c: saidas.criar_excel(res, c, rotulo)),
            ("Greides_e_corredor.xml", lambda c: saidas.criar_landxml(res, ex, c)),
            ("Corredor_3D.dxf", lambda c: saidas.criar_dxf(res, ex, c)),
            ("Pontos_PNEZD.csv", lambda c: saidas.criar_pontos(res, ex, c)),
            ("Perfil.png", lambda c: saidas.figura_perfil(res, rotulo).savefig(c)),
        ]
        for n, (nome, funcao) in enumerate(geradores):
            try:
                funcao(str(pasta / nome))
            except Exception as exc:  # Uma saída com problema não pode esconder as outras.
                avisos.append(f"{nome} não foi gerado: {exc}")
            progresso(72 + 4 * (n + 1))

        resultado = _limpo({
            "assistente": "Gerador de greide — POINT TOP",
            "versao": VERSAO,
            "data": datetime.now().isoformat(timespec="seconds"),
            "sintetico": sintetico,
            "eixo": {"nome": ex.nome, "inicio": ex.sta_ini, "fim": ex.sta_fim, "sistema": ex.sistema},
            "parametros": {k_: getattr(p, k_) for k_ in ("passo", "larg_pista", "larg_acost", "esp_min", "inc_conf", "inc_final", "d2_mm_5m", "modo", "fres_max", "super_ativa", "emax", "velocidade")},
            "limites": lim,
            "resumo": res.resumo,
            "situacao": analise.situacao_geral(verificacoes),
            "verificacoes": verificacoes,
            "levantamento": levant,
            "iri": dados_iri,
            "ondas_longas": ondas,
            "serie": _serie(res),
            "avisos": avisos,
            "ressalva": "Rascunho técnico. Volumes geométricos, sem empolamento ou compactação. A validação do projeto é do responsável.",
        })
        (pasta / "resultado.json").write_text(json.dumps(resultado, ensure_ascii=False, indent=1, allow_nan=False), encoding="utf-8")
        _relatorio(resultado, pasta)
        progresso(100)
        arquivos = sorted(a.name for a in pasta.iterdir() if a.is_file())
        return pasta, resultado, arquivos
    except Exception as exc:
        (pasta / "ERRO.txt").write_text(f"{exc}\nProcessamento incompleto. Não usar os arquivos desta pasta.", encoding="utf-8")
        raise
