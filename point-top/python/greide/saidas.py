"""Geração das notas de serviço, do corredor e dos arquivos para o Civil 3D."""
import copy
import csv
import os
import xml.etree.ElementTree as ET

import numpy as np

from geometria import fmt_estaca
from otimizador import MODO_TERRENO

NOMES = ["Acost. LE", "Bordo LE", "Eixo", "Bordo LD", "Acost. LD"]
# Ordem das colunas pedida para a nota: eixo, bordos, acostamentos.
ORDEM = [2, 1, 3, 0, 4]
ABA_CONF = "NS Conformação"
ABA_FINAL = "NS Final"


def _col(i):
    """Índice base 0 para letra da coluna."""
    s = ""
    i += 1
    while i:
        i, r = divmod(i - 1, 26)
        s = chr(65 + r) + s
    return s


def _vol(s, area):
    return float(np.sum((area[1:] + area[:-1]) * np.diff(s)) / 2)


def criar_excel(res, caminho, rotulo=None):
    import xlsxwriter

    p = res.parametros
    s = res.estacas
    n = len(s)
    wb = xlsxwriter.Workbook(caminho, {"nan_inf_to_errors": False})
    wb.set_calc_mode("auto")

    base = {"font_name": "Arial", "font_size": 9}
    f = lambda **k: wb.add_format({**base, **k})
    titulo = f(bold=True, font_size=13)
    sub = f(italic=True, font_color="#555555")
    alerta = f(bold=True, font_color="#C00000")
    grupo = f(bold=True, align="center", valign="vcenter", bg_color="#1F3864", font_color="white", border=1)
    cab = f(bold=True, align="center", valign="vcenter", bg_color="#D9E2F3", border=1, text_wrap=True)
    txt = f(border=1, align="center")
    num0 = f(border=1, num_format="0", align="center")
    cota = f(border=1, num_format="0.000")
    cota_azul = f(border=1, num_format="0.000", font_color="#0000FF", italic=True)
    esp = f(border=1, num_format="0.000", bg_color="#F2F2F2")
    area = f(border=1, num_format="0.000")
    rot = f(bold=True, bg_color="#D9E2F3", border=1)
    celula = f(border=1)
    entrada = f(border=1, font_color="#0000FF")
    entrada3 = f(border=1, font_color="#0000FF", num_format="0.000")
    res3 = f(border=1, num_format="0.000")
    res1 = f(border=1, num_format="#,##0.0")
    res2p = f(border=1, num_format="0.00")
    quebra = f(text_wrap=True, valign="top")
    vermelho = wb.add_format({"bg_color": "#F8CBAD", "font_color": "#9C0006", "bold": True})
    verde = wb.add_format({"font_color": "#006100"})

    resumo = wb.add_worksheet("Resumo")
    ws_conf = wb.add_worksheet(ABA_CONF)
    ws_final = wb.add_worksheet(ABA_FINAL)
    ws_crit = wb.add_worksheet("Pontos críticos")

    # Células de parâmetro usadas pelas fórmulas das notas.
    ref_esp = "Resumo!$B$6"
    ref_pista = "Resumo!$B$7"
    ref_acost = "Resumo!$B$8"

    pesos = p.pesos_secao
    t = res.terreno
    pf = res.cotas_final
    pc = res.cotas_conf
    ini = 5  # primeira linha de dados, em numeração do Excel
    fim = ini + n - 1

    def cabecalho(ws, nome, rotulo_dif, extras):
        ws.write(0, 0, nome, titulo)
        linha2 = rotulo if rotulo else "Cotas em metros. Espessura/diferença = projeto − terreno."
        ws.write(1, 0, linha2, alerta if rotulo else sub)
        ws.merge_range(2, 0, 3, 0, "Estaca (m)", cab)
        ws.merge_range(2, 1, 3, 1, "km+m", cab)
        c = 2
        grupos = ["Eixo", "Bordo 3,50 m — LE", "Bordo 3,50 m — LD", "Acostamento 2,00 m — LE", "Acostamento 2,00 m — LD"]
        for g in grupos:
            ws.merge_range(2, c, 2, c + 2, g, grupo)
            ws.write(3, c, "Terreno", cab)
            ws.write(3, c + 1, "Projeto", cab)
            ws.write(3, c + 2, rotulo_dif, cab)
            c += 3
        for x in extras:
            ws.merge_range(2, c, 3, c, x, cab)
            c += 1
        ws.set_row(3, 38)
        ws.set_column(0, 0, 9)
        ws.set_column(1, 1, 11)
        ws.set_column(2, 16, 8.5)
        ws.set_column(17, 24, 11)
        ws.freeze_panes(4, 2)
        ws.set_landscape()
        ws.set_paper(9)
        ws.fit_to_pages(1, 0)
        ws.repeat_rows(0, 3)

    def linhas_cota(ws, cotas):
        for i in range(n):
            lin = ini - 1 + i
            ws.write_number(lin, 0, s[i], num0)
            ws.write_string(lin, 1, fmt_estaca(s[i]), txt)
            c = 2
            for k in ORDEM:
                if np.isfinite(t[i, k]):
                    ws.write_number(lin, c, t[i, k], cota_azul if res.preenchido[i, k] else cota)
                else:
                    ws.write_blank(lin, c, None, cota)
                ws.write_number(lin, c + 1, cotas[i, k], cota)
                a, b = _col(c), _col(c + 1)
                r = lin + 1
                valor = round(cotas[i, k] - t[i, k], 3) if np.isfinite(t[i, k]) else ""
                ws.write_formula(lin, c + 2, f'=IF({a}{r}="","",ROUND({b}{r}-{a}{r},3))', esp, valor)
                c += 3

    # Nota da conformação.
    cabecalho(ws_conf, "Nota de serviço — Conformação", "Dif. (+ aterro / − fresa)",
              ["Área aterro pista (m²)", "Área fresagem pista (m²)"])
    linhas_cota(ws_conf, pc)
    dif = pc - t
    b_ = p.larg_pista
    ar_aterro = np.round(
        b_ * np.clip(np.round(dif[:, 2], 3), 0, None)
        + b_ / 2 * (np.clip(np.round(dif[:, 1], 3), 0, None) + np.clip(np.round(dif[:, 3], 3), 0, None)), 6)
    ar_fresa = np.round(
        b_ * np.clip(-np.round(dif[:, 2], 3), 0, None)
        + b_ / 2 * (np.clip(-np.round(dif[:, 1], 3), 0, None) + np.clip(-np.round(dif[:, 3], 3), 0, None)), 6)
    for i in range(n):
        r = ini + i
        ws_conf.write_formula(
            r - 1, 17,
            f"={ref_pista}*MAX(N(E{r}),0)+{ref_pista}/2*(MAX(N(H{r}),0)+MAX(N(K{r}),0))",
            area, ar_aterro[i])
        ws_conf.write_formula(
            r - 1, 18,
            f"=-{ref_pista}*MIN(N(E{r}),0)-{ref_pista}/2*(MIN(N(H{r}),0)+MIN(N(K{r}),0))",
            area, ar_fresa[i])

    # Nota final.
    cabecalho(ws_final, "Nota de serviço — Greide final (espessura mínima 0,23 m)", "Espessura",
              ["Espessura mínima na seção", "Mín. sobre a conformação", "Área terreno a projeto (m²)",
               "Área conformação a projeto (m²)", "Situação"])
    linhas_cota(ws_final, pf)
    esp_v = np.round(pf - t, 3)
    conf_q = f"'{ABA_CONF}'!"
    colp = ["D", "G", "J", "M", "P"]  # colunas de projeto, na ordem eixo, bordos, acostamentos
    cole = ["E", "H", "K", "N", "Q"]
    ws_final.conditional_format(ini - 1, 17, fim - 1, 17, {
        "type": "formula", "criteria": f"=AND(ISNUMBER(R{ini}),R{ini}<{ref_esp})", "format": vermelho})
    ws_final.conditional_format(ini - 1, 21, fim - 1, 21, {
        "type": "cell", "criteria": "==", "value": '"ABAIXO"', "format": vermelho})
    for i in range(n):
        r = ini + i
        vmin = float(np.nanmin(esp_v[i]))
        difs = ",".join(f"{c}{r}-{conf_q}{c}{r}" for c in colp)
        vsob = float(np.round(np.min(pf[i] - pc[i]), 3))
        ws_final.write_formula(r - 1, 17, "=MIN(" + ",".join(f"{c}{r}" for c in cole) + ")", esp, vmin)
        ws_final.write_formula(r - 1, 18, f"=ROUND(MIN({difs}),3)", esp, vsob)
        ar_t = float(np.nansum(np.nan_to_num(esp_v[i]) * pesos))
        ws_final.write_formula(
            r - 1, 19,
            f"={ref_pista}*N(E{r})+({ref_pista}+{ref_acost})/2*(N(H{r})+N(K{r}))+{ref_acost}/2*(N(N{r})+N(Q{r}))",
            area, ar_t)
        d = pf[i] - pc[i]
        ar_c = float(np.sum(d * pesos))
        ws_final.write_formula(
            r - 1, 20,
            f"={ref_pista}*(D{r}-{conf_q}D{r})+({ref_pista}+{ref_acost})/2*(G{r}-{conf_q}G{r}+J{r}-{conf_q}J{r})"
            f"+{ref_acost}/2*(M{r}-{conf_q}M{r}+P{r}-{conf_q}P{r})",
            area, ar_c)
        ws_final.write_formula(r - 1, 21, f'=IF(R{r}<{ref_esp},"ABAIXO","OK")', txt,
                               "ABAIXO" if vmin < p.esp_min else "OK")
    ws_final.conditional_format(ini - 1, 21, fim - 1, 21, {
        "type": "cell", "criteria": "==", "value": '"OK"', "format": verde})

    # Resumo.
    resumo.set_landscape()
    resumo.set_paper(9)
    resumo.fit_to_pages(1, 0)
    ws_crit.set_paper(9)
    resumo.set_column(0, 0, 44)
    resumo.set_column(1, 1, 14)
    resumo.set_column(2, 2, 70)
    resumo.write(0, 0, "Greide BR-222 — Resumo", titulo)
    resumo.write(1, 0, rotulo if rotulo else f"Trecho {fmt_estaca(s[0])} a {fmt_estaca(s[-1])}", alerta if rotulo else sub)
    resumo.write(3, 0, "Parâmetros", grupo)
    resumo.write(3, 1, "Valor", grupo)
    resumo.write(3, 2, "Origem", grupo)
    modo_txt = ("Terreno: projeto − terreno >= mínimo nos 5 pontos" if p.modo == MODO_TERRENO
                else "Conformação: projeto − conformação >= mínimo (fresagem reduz material)")
    parametros = [
        ("Passo entre estacas (m)", p.passo, "Ajustável no aplicativo.", entrada),
        ("Espessura mínima (m)", p.esp_min, "Pedido: 0,23 m no eixo, a 3,50 m e a 5,50 m.", entrada3),
        ("Largura da pista por lado (m)", p.larg_pista, "Pedido.", entrada),
        ("Largura do acostamento por lado (m)", p.larg_acost, "Pedido.", entrada),
        ("Caimento da conformação: pista (%)", p.inc_conf, "Pedido: −3 %.", entrada),
        ("Caimento da conformação: acostamento (%)", p.inc_conf_acost, "Adotado igual ao da pista.", entrada),
        ("Caimento do corredor final (%)", p.inc_final, "Pedido: −2 % até 5,50 m.", entrada),
        ("Variação máx. entre lances de 5 m (mm)", p.d2_mm_5m,
         f"Regra de ±2,5 mm a cada 5 m aplicada à segunda diferença do greide: K ≈ {p.k_equivalente:.0f} m/%.", entrada),
        ("Superelevação", "Ligada" if p.super_ativa else "Desligada",
         f"emáx {p.emax:g} %, V {p.velocidade:g} km/h." if p.super_ativa else "Pedido: seção com caimento constante.", celula),
    ]
    lin = 4
    for nome, valor, origem, fmt in parametros:
        resumo.write(lin, 0, nome, rot)
        resumo.write(lin, 1, valor, fmt)
        resumo.write(lin, 2, origem, celula)
        lin += 1
    resumo.write(lin, 0, "Referência da espessura", rot)
    resumo.merge_range(lin, 1, lin, 2, modo_txt, celula)
    lin += 2

    resumo.write(lin, 0, "Resultados", grupo)
    resumo.write(lin, 1, "Valor", grupo)
    resumo.write(lin, 2, "Observação", grupo)
    lin += 1
    fq = f"'{ABA_FINAL}'!"
    cq = conf_q

    def soma_vol(q, coluna):
        return (f"=SUMPRODUCT({q}$A${ini + 1}:$A${fim}-{q}$A${ini}:$A${fim - 1},"
                f"{q}${coluna}${ini + 1}:${coluna}${fim}+{q}${coluna}${ini}:${coluna}${fim - 1})/2")

    ext = float(s[-1] - s[0])
    a_t = np.array([np.nansum(np.nan_to_num(esp_v[i]) * pesos) for i in range(n)])
    a_c = np.sum((pf - pc) * pesos, axis=1)
    v_t = _vol(s, a_t)
    v_c = _vol(s, a_c)
    v_at = _vol(s, ar_aterro)
    v_fr = _vol(s, ar_fresa)
    n_baixo = int(np.sum(np.nanmin(esp_v, axis=1) < p.esp_min))
    linhas = [
        ("Extensão (m)", f"={fq}$A${fim}-{fq}$A${ini}", ext, res1, "Da primeira à última estaca calculada."),
        ("Número de estacas", f"=COUNT({fq}$A${ini}:$A${fim})", n, celula, ""),
        ("Espessura mínima encontrada (m)", f"=MIN({fq}$R${ini}:$R${fim})", float(np.nanmin(esp_v)), res3,
         "Projeto − terreno, nos 5 pontos de todas as estacas."),
        ("Espessura mín. sobre a conformação (m)", f"=MIN({fq}$S${ini}:$S${fim})",
         float(np.round(np.min(pf - pc), 3)), res3, ""),
        ("Estacas abaixo do mínimo", f'=COUNTIF({fq}$V${ini}:$V${fim},"ABAIXO")', n_baixo, celula, ""),
        ("Espessura média sobre o terreno (m)", None, None, res3, "Volume ÷ (extensão × 11,00 m)."),
        ("Volume terreno a projeto, 11,00 m (m³)", soma_vol(fq, "T"), v_t, res1, "Média das áreas, sem correção de curva."),
        ("Volume conformação a projeto, 11,00 m (m³)", soma_vol(fq, "U"), v_c, res1, ""),
        ("Conformação: aterro na pista, 7,00 m (m³)", soma_vol(cq, "R"), v_at, res1, "Só onde projeto > terreno."),
        ("Conformação: fresagem na pista, 7,00 m (m³)", soma_vol(cq, "S"), v_fr, res1, "Só onde projeto < terreno."),
        ("Variação máx. no greide final (mm/lance)", None, res.resumo["d2_final_max_mm"], res2p,
         "Segunda diferença do greide calculado. Nas cotas da nota, o arredondamento ao mm pode somar até 2 mm."),
        ("Variação máx. na conformação (mm/lance)", None, res.resumo["d2_conf_max_mm"], res2p,
         "Segunda diferença do greide calculado."),
    ]
    linha_vol = lin + 6 + 1  # linha do Excel do volume terreno a projeto
    linha_ext = lin + 1
    for nome, formula, valor, fmt, obs in linhas:
        resumo.write(lin, 0, nome, rot)
        if nome.startswith("Espessura média"):
            larg = f"(2*({ref_pista}+{ref_acost}))"
            resumo.write_formula(lin, 1, f"=B{linha_vol}/(B{linha_ext}*{larg})", fmt, v_t / (ext * pesos.sum()))
        elif formula:
            resumo.write_formula(lin, 1, formula, fmt, valor)
        else:
            resumo.write_number(lin, 1, valor, fmt)
        resumo.write(lin, 2, obs, celula)
        lin += 1

    if res.avisos:
        lin += 1
        resumo.write(lin, 0, "Avisos", grupo)
        lin += 1
        for a in res.avisos:
            resumo.merge_range(lin, 0, lin, 2, a, quebra)
            resumo.set_row(lin, 28)
            lin += 1

    # Pontos críticos: onde o terreno mais força o greide para cima.
    ws_crit.write(0, 0, "Pontos a conferir no levantamento", titulo)
    ws_crit.write(1, 0, "Maiores espessuras. Um ponto errado de levantamento pode levantar o greide nas estacas vizinhas.", sub)
    for c, h in enumerate(["Estaca (m)", "km+m", "Ponto", "Terreno", "Projeto", "Espessura"]):
        ws_crit.write(3, c, h, cab)
    ws_crit.set_column(0, 5, 12)
    esp_max = np.nanmax(esp_v, axis=1)
    ordem = np.argsort(-esp_max)
    usados = []
    for i in ordem:
        if all(abs(s[i] - s[j]) >= 50 for j in usados):
            usados.append(int(i))
        if len(usados) == 30:
            break
    for lin_c, i in enumerate(usados, start=4):
        k = int(np.nanargmax(esp_v[i]))
        ws_crit.write_number(lin_c, 0, s[i], num0)
        ws_crit.write_string(lin_c, 1, fmt_estaca(s[i]), txt)
        ws_crit.write_string(lin_c, 2, NOMES[k], txt)
        ws_crit.write_number(lin_c, 3, t[i, k], cota)
        ws_crit.write_number(lin_c, 4, pf[i, k], cota)
        ws_crit.write_formula(lin_c, 5, f"=ROUND(E{lin_c + 1}-D{lin_c + 1},3)", esp, float(esp_v[i, k]))
    wb.close()


def criar_landxml(res, eixo, caminho):
    """LandXML com o eixo, os dois greides e as superfícies do corredor."""
    uri = "http://www.landxml.org/schema/LandXML-1.2"
    ET.register_namespace("", uri)
    raiz = ET.Element(f"{{{uri}}}LandXML", {"version": "1.2", "language": "English", "readOnly": "false",
                                            "date": "2026-01-01", "time": "00:00:00"})
    q = lambda t: f"{{{uri}}}{t}"
    for el in eixo.xml_raiz:
        if el.tag.split("}")[-1] in ("Units", "CoordinateSystem"):
            raiz.append(copy.deepcopy(el))
    ET.SubElement(raiz, q("Application"), {"name": "Greide BR-222", "manufacturer": "Point Top", "version": "1.0"})
    alins = ET.SubElement(raiz, q("Alignments"), {"name": ""})
    alin = copy.deepcopy(eixo.xml_alinhamento)
    perfil = ET.Element(q("Profile"), {"name": eixo.nome})
    # Greide sem o arredondamento da nota, para o ruído de 1 mm não virar curvatura no Civil 3D.
    for nome, cotas in (("GREIDE_FINAL", res.greide_final), ("GREIDE_CONFORMACAO", res.greide_conf)):
        pa = ET.SubElement(perfil, q("ProfAlign"), {"name": nome})
        for sta, z in zip(res.estacas, cotas):
            ET.SubElement(pa, q("PVI")).text = f"{sta:.3f} {z:.5f}"
    filhos = list(alin)
    pos = next(i for i, c in enumerate(filhos) if c.tag.split("}")[-1] == "CoordGeom") + 1
    alin.insert(pos, perfil)
    alins.append(alin)

    supers = ET.SubElement(raiz, q("Surfaces"))
    e, n_ = eixo.deslocados(res.estacas, res.afastamentos)
    ns = len(res.estacas)
    for nome, cotas in (("CORREDOR_FINAL", res.cotas_final), ("CONFORMACAO", res.cotas_conf)):
        su = ET.SubElement(supers, q("Surface"), {"name": nome})
        de = ET.SubElement(su, q("Definition"), {"surfType": "TIN"})
        pts = ET.SubElement(de, q("Pnts"))
        for i in range(ns):
            for k in range(5):
                ET.SubElement(pts, q("P"), {"id": str(i * 5 + k + 1)}).text = (
                    f"{n_[i, k]:.3f} {e[i, k]:.3f} {cotas[i, k]:.3f}")
        fs = ET.SubElement(de, q("Faces"))
        for i in range(ns - 1):
            for k in range(4):
                a = i * 5 + k + 1
                b = a + 1
                c = a + 5
                d = c + 1
                ET.SubElement(fs, q("F")).text = f"{a} {b} {d}"
                ET.SubElement(fs, q("F")).text = f"{a} {d} {c}"
    ET.ElementTree(raiz).write(caminho, encoding="utf-8", xml_declaration=True)


def criar_dxf(res, eixo, caminho):
    """Polilinhas 3D do corredor e da conformação, com seções a cada 20 m."""
    import ezdxf

    doc = ezdxf.new("R2013")
    msp = doc.modelspace()
    e, n_ = eixo.deslocados(res.estacas, res.afastamentos)
    cores = {"FINAL": 1, "CONFORMACAO": 3}
    for etapa, cotas in (("FINAL", res.cotas_final), ("CONFORMACAO", res.cotas_conf)):
        doc.layers.add(f"{etapa}_LINHAS", color=cores[etapa])
        doc.layers.add(f"{etapa}_SECOES", color=8)
        for k in range(5):
            pts = list(zip(e[:, k], n_[:, k], cotas[:, k]))
            msp.add_polyline3d(pts, dxfattribs={"layer": f"{etapa}_LINHAS"})
        for i, sta in enumerate(res.estacas):
            if abs(sta / 20 - round(sta / 20)) < 1e-6:
                pts = list(zip(e[i], n_[i], cotas[i]))
                msp.add_polyline3d(pts, dxfattribs={"layer": f"{etapa}_SECOES"})
    doc.saveas(caminho)


def criar_pontos(res, eixo, caminho):
    """Pontos PNEZD para importar ou locar. Separados por vírgula e com ponto decimal."""
    e, n_ = eixo.deslocados(res.estacas, res.afastamentos)
    siglas = ["AL", "BL", "EX", "BD", "AD"]
    with open(caminho, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        pid = 1
        for etapa, cotas in (("F", res.cotas_final), ("C", res.cotas_conf)):
            for i, sta in enumerate(res.estacas):
                for k in range(5):
                    w.writerow([pid, f"{n_[i, k]:.3f}", f"{e[i, k]:.3f}", f"{cotas[i, k]:.3f}",
                                f"{etapa}_{siglas[k]}_{sta:.0f}"])
                    pid += 1


def figura_perfil(res, rotulo=None, figura=None):
    """Perfil longitudinal e espessuras."""
    from matplotlib.figure import Figure

    s = res.estacas
    passo = max(1, len(s) // 6000)
    fig = figura if figura is not None else Figure(figsize=(12, 6.5), dpi=130)
    fig.clear()
    a1, a2 = fig.subplots(2, 1, sharex=True, height_ratios=[2, 1.2])
    a1.plot(s[::passo], res.terreno[::passo, 2], color="#777777", lw=0.8, label="Terreno no eixo")
    a1.plot(s[::passo], res.cotas_conf[::passo, 2], color="#2E8B57", lw=1.1, label="Conformação")
    a1.plot(s[::passo], res.cotas_final[::passo, 2], color="#C00000", lw=1.1, label="Greide final")
    a1.set_ylabel("Cota no eixo (m)")
    a1.legend(loc="best", fontsize=8)
    a1.grid(alpha=0.3)
    t = "Perfil longitudinal e espessuras"
    a1.set_title(f"{t} — {rotulo}" if rotulo else t, fontsize=10)
    esp = res.cotas_final - res.terreno
    a2.plot(s[::passo], esp[::passo, 2], lw=0.8, color="#C00000", label="Eixo")
    a2.plot(s[::passo], np.nanmax(esp[::passo][:, [1, 3]], axis=1), lw=0.6, color="#1F77B4", label="Bordo (maior)")
    a2.plot(s[::passo], np.nanmin(esp[::passo], axis=1), lw=0.8, color="#2E8B57", label="Mínima na seção")
    a2.axhline(res.parametros.esp_min, color="k", ls="--", lw=0.8, label=f"Mínimo {res.parametros.esp_min:.2f} m")
    a2.set_ylabel("Espessura (m)")
    a2.set_xlabel("Estaca (m)")
    a2.legend(loc="upper right", fontsize=7, ncol=4)
    a2.grid(alpha=0.3)
    fig.tight_layout()
    return fig


def exportar_tudo(res, eixo, pasta, rotulo=None, log=print):
    os.makedirs(pasta, exist_ok=True)
    arquivos = {}

    def salvar(chave, nome, funcao):
        caminho = os.path.join(pasta, nome)
        log(f"Gerando {nome}...")
        funcao(caminho)
        arquivos[chave] = caminho

    salvar("excel", "Notas_de_servico.xlsx", lambda c: criar_excel(res, c, rotulo))
    salvar("landxml", "Greides_e_corredor.xml", lambda c: criar_landxml(res, eixo, c))
    salvar("dxf", "Corredor_3D.dxf", lambda c: criar_dxf(res, eixo, c))
    salvar("pontos", "Pontos_PNEZD.csv", lambda c: criar_pontos(res, eixo, c))
    salvar("perfil", "Perfil.png", lambda c: figura_perfil(res, rotulo).savefig(c))
    return arquivos
