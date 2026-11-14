"""Saídas da REABILITAÇÃO DE PISTA — IFM: planilha Excel e LandXML."""
from __future__ import annotations

import math
from xml.sax.saxutils import escape

import numpy as np

OFFS = (-5.50, -3.50, 0.0, 3.50, 5.50)
NOMES = {-5.50: 'LE 5,50', -3.50: 'LE 3,50', 0.0: 'EIXO', 3.50: 'LD 3,50', 5.50: 'LD 5,50'}


def fmt_estaca(m: float) -> str:
    km = int(m // 1000)
    r = m - km * 1000
    return f'{km}+{r:06.2f}'.replace('.', ',')


def gravar_excel(path, linhas, resumo, pista, fundo_pts):
    from openpyxl import Workbook
    from openpyxl.formatting.rule import CellIsRule, FormulaRule
    from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
    from openpyxl.utils import get_column_letter as L

    AZUL, CINZA, CLARO = '193B62', '596477', 'E6EDF4'
    f_base = Font(name='Arial', size=9, color='243247')
    f_head = Font(name='Arial', size=9, bold=True, color='FFFFFF')
    f_tit = Font(name='Arial', size=14, bold=True, color=AZUL)
    f_sub = Font(name='Arial', size=9, italic=True, color=CINZA)
    fill_h = PatternFill('solid', fgColor=AZUL)
    fill_h2 = PatternFill('solid', fgColor='2E5A88')
    fill_prev = PatternFill('solid', fgColor='FFF4D6')
    fill_ok = PatternFill('solid', fgColor='E3F1E6')
    fino = Side(style='thin', color='BBC4D0')
    borda = Border(left=fino, right=fino, top=fino, bottom=fino)
    centro = Alignment(horizontal='center', vertical='center', wrap_text=True)

    wb = Workbook()
    ws = wb.active
    ws.title = 'NOTA DE SERVIÇO'
    prov = resumo['eixo']['fonte'].startswith('PROV')
    ws['A1'] = 'REABILITAÇÃO DE PISTA — IFM  •  NOTA DE SERVIÇO DO GREIDE'
    ws['A1'].font = f_tit
    p = resumo['parametros']
    ws['A2'] = (f"Eixo: {resumo['eixo']['nome']} ({resumo['eixo']['fonte']})  •  Rebaixo {p['rebaixo_m']:.2f} m  •  "
                f"Espessura mín. {p['espessura_min_m']:.2f} m (eixo, 3,50 e 5,50)  •  Caimento {p['caimento_pct']:.1f} %  •  "
                f"V = {p['velocidade_kmh']:.0f} km/h, e máx. {p['emax_pct']:.0f} %  •  {p['criterio_suavidade']}")
    ws['A2'].font = f_sub
    legenda = ('Fundo = levantamento após rebaixo (acostamento) ou pista levantada já cortada.'
               if p['pista_estado'] == 'depois' else
               'Fundo MEDIDO = levantamento após rebaixo; PREVISTO (amarelo) = pista levantada − rebaixo.')
    ws['A3'] = ('PRÉVIA — EIXO PROVISÓRIO: NÃO LIBERAR PARA CAMPO' if prov else
                'Conferir antes de liberar para campo.') + '  •  ' + legenda
    ws['A3'].font = Font(name='Arial', size=9, bold=True, color='B42318' if prov else CINZA)

    # cabeçalho em 2 linhas
    r1, r2, first = 5, 6, 7
    fixos = ['Estaca', 'Progressiva (m)', 'Greide eixo (m)', 'Caimento LE (%)', 'Caimento LD (%)']
    for j, t in enumerate(fixos, 1):
        ws.cell(r1, j, t); ws.merge_cells(start_row=r1, start_column=j, end_row=r2, end_column=j)
    col = len(fixos) + 1
    mapa = {}
    for o in OFFS:
        ws.cell(r1, col, NOMES[o] + (' (afast. ' + f'{o:+.2f}'.replace('.', ',') + ' m)' if o else ''))
        ws.merge_cells(start_row=r1, start_column=col, end_row=r1, end_column=col + 3)
        for k, t in enumerate(['Pista levantada', 'Fundo (terreno)', 'Projeto', 'Espessura']):
            ws.cell(r2, col + k, t)
        mapa[o] = col
        col += 4
    ws.cell(r1, col, 'Folga mín. sobre a espessura (m)'); ws.merge_cells(start_row=r1, start_column=col, end_row=r2, end_column=col)
    ws.cell(r1, col + 1, 'Espessura mín. contínua 0,5 m (m)'); ws.merge_cells(start_row=r1, start_column=col + 1, end_row=r2, end_column=col + 1)
    ws.cell(r1, col + 2, 'Controla'); ws.merge_cells(start_row=r1, start_column=col + 2, end_row=r2, end_column=col + 2)
    ultima = col + 2
    for rr in (r1, r2):
        for j in range(1, ultima + 1):
            c = ws.cell(rr, j)
            c.font = f_head; c.fill = fill_h if rr == r1 else fill_h2; c.alignment = centro; c.border = borda
    ws.row_dimensions[r1].height = 28; ws.row_dimensions[r2].height = 28

    esp_min = resumo['parametros']['espessura_min_m']
    ws.cell(4, ultima - 3, 'Espessura mínima exigida (m):').font = Font(name='Arial', size=9, bold=True)
    ws.cell(4, ultima - 3).alignment = Alignment(horizontal='right')
    ws.merge_cells(start_row=4, start_column=ultima - 3, end_row=4, end_column=ultima - 1)
    ws.cell(4, ultima, esp_min).font = Font(name='Arial', size=9, bold=True, color=AZUL)
    ESP = f'${L(ultima)}$4'

    for i, row in enumerate(linhas):
        r = first + i
        ws.cell(r, 1, fmt_estaca(row['estaca']))
        ws.cell(r, 2, row['estaca'])
        ws.cell(r, 3, round(row['greide'], 3))
        ws.cell(r, 4, row['q_le_pct'])
        ws.cell(r, 5, row['q_ld_pct'])
        esp_cells = []
        for o in OFFS:
            c0 = mapa[o]
            tag = NOMES[o]
            ex, fu, fo = row[f'{tag}|existente'], row[f'{tag}|fundo'], row[f'{tag}|fonte']
            ws.cell(r, c0, None if ex is None else round(ex, 3))
            cf = ws.cell(r, c0 + 1, None if fu is None else round(fu, 3))
            if fu is not None and str(fo).startswith('PREVISTO'):
                cf.fill = fill_prev
            if o == 0:
                ws.cell(r, c0 + 2, f'=C{r}')
            else:
                q = 'D' if o < 0 else 'E'
                ws.cell(r, c0 + 2, f'=C{r}+{q}{r}/100*{abs(o):.2f}')
            if fu is None:
                ws.cell(r, c0 + 3, None)
            else:
                ws.cell(r, c0 + 3, f'={L(c0 + 2)}{r}-{L(c0 + 1)}{r}')
                esp_cells.append(f'{L(c0 + 3)}{r}')
        if esp_cells:
            ws.cell(r, col, f'=ROUND(MIN({",".join(esp_cells)})-{ESP},4)')
        mc = row['min_continua']
        ws.cell(r, col + 1, None if mc is None else round(mc, 3))
        ws.cell(r, col + 2, row['controla'])
        for j in range(1, ultima + 1):
            c = ws.cell(r, j)
            c.font = f_base; c.border = borda
            c.alignment = Alignment(horizontal='center')
        ws.cell(r, 2).number_format = '0.00'
        for j in (3,):
            ws.cell(r, j).number_format = '0.000'
        for j in (4, 5):
            ws.cell(r, j).number_format = '0.00'
        for o in OFFS:
            for k in range(4):
                ws.cell(r, mapa[o] + k).number_format = '0.000'
        ws.cell(r, col).number_format = '0.000'
        ws.cell(r, col + 1).number_format = '0.000'
    last = first + len(linhas) - 1
    # formatação condicional: espessura abaixo do mínimo
    vermelho = PatternFill('solid', fgColor='F8D7DA')
    for o in OFFS:
        rng = f'{L(mapa[o] + 3)}{first}:{L(mapa[o] + 3)}{last}'
        top = f'{L(mapa[o] + 3)}{first}'
        ws.conditional_formatting.add(rng, FormulaRule(formula=[f'AND(ISNUMBER({top}),{top}<{ESP}-0.0005)'], fill=vermelho,
                                                       font=Font(name='Arial', size=9, bold=True, color='B42318')))
    t1, t2 = f'{L(col)}{first}', f'{L(col + 1)}{first}'
    ws.conditional_formatting.add(f'{L(col)}{first}:{L(col)}{last}',
                                  FormulaRule(formula=[f'AND(ISNUMBER({t1}),{t1}<-0.0005)'], fill=vermelho))
    ws.conditional_formatting.add(f'{L(col + 1)}{first}:{L(col + 1)}{last}',
                                  FormulaRule(formula=[f'AND(ISNUMBER({t2}),{t2}<{ESP}-0.0005)'],
                                              fill=PatternFill('solid', fgColor='FCE8C8')))
    larg = {1: 12, 2: 11, 3: 10, 4: 9, 5: 9}
    for j in range(1, ultima + 1):
        ws.column_dimensions[L(j)].width = larg.get(j, 9.5)
    ws.column_dimensions[L(col + 2)].width = 10
    ws.freeze_panes = ws.cell(first, 3)
    ws.sheet_view.zoomScale = 90
    ws.print_title_rows = f'{r1}:{r2}'
    ws.page_setup.orientation = 'landscape'
    ws.page_setup.paperSize = ws.PAPERSIZE_A3
    ws.page_setup.fitToWidth = 1; ws.page_setup.fitToHeight = 0
    ws.sheet_properties.pageSetUpPr.fitToPage = True

    # ---------------- RESUMO
    rs = wb.create_sheet('RESUMO')
    rs.column_dimensions['A'].width = 46; rs.column_dimensions['B'].width = 22
    for j in 'CDEFG':
        rs.column_dimensions[j].width = 16
    rs['A1'] = 'REABILITAÇÃO DE PISTA — IFM  •  RESUMO'; rs['A1'].font = f_tit
    r = 3

    def bloco(titulo):
        nonlocal r
        rs.cell(r, 1, titulo).font = Font(name='Arial', size=10, bold=True, color='FFFFFF')
        for j in range(1, 8):
            rs.cell(r, j).fill = fill_h
        r += 1

    def linha(k, v, fmt=None):
        nonlocal r
        rs.cell(r, 1, k).font = f_base
        c = rs.cell(r, 2, v); c.font = Font(name='Arial', size=9, bold=True, color='243247')
        if fmt:
            c.number_format = fmt
        r += 1

    bloco('Dados de entrada')
    linha('Eixo', f"{resumo['eixo']['nome']} — {resumo['eixo']['fonte']}")
    linha('Estacas analisadas', f"{fmt_estaca(resumo['estacas']['inicio'])} a {fmt_estaca(resumo['estacas']['fim'])} "
                                f"({resumo['estacas']['quantidade']} estacas a cada {p['passo_m']:.0f} m)")
    linha('Levantamento da pista', f"{resumo['pista']['arquivo']} — {resumo['pista']['pontos']} pontos")
    for f in resumo['fundos']:
        linha(f"Levantamento após rebaixo ({f['lado']})", f"{f['arquivo']} — {f['pontos']} pontos, "
              f"{f['referencias']} referências originais, {f['fora_do_eixo']} fora do eixo")
    r += 1
    bloco('Parâmetros')
    for k, v in [('Rebaixo (m)', p['rebaixo_m']), ('Espessura mínima (m)', p['espessura_min_m']),
                 ('Folga construtiva (m)', p['folga_m']), ('Caimento em tangente (%)', -p['caimento_pct']),
                 ('Velocidade de projeto (km/h)', p['velocidade_kmh']), ('Superelevação máxima (%)', p['emax_pct']),
                 ('Raio mínimo calculado (m)', round(p['raio_min_m'], 1)),
                 ('Raio acima do qual não se superelevou (m)', p['raio_dispensa_superelevacao_m']),
                 ('Rampa relativa máx. na transição (%)', p['rampa_relativa_max_pct']),
                 ('Suavidade: variação máx. de rampa entre estacas (mm)', round(p['suavidade_mm'], 2)),
                 ('Meta de IRI teórico do greide, qualquer 100 m (m/km)', p['iri_alvo']),
                 ('Estado da pista levantada', 'antes do rebaixo (fundo previsto = cota − rebaixo)'
                  if p['pista_estado'] == 'antes' else 'já cortada (fundo = cota levantada)'),
                 ('Espessura exigida também entre controles (a cada 0,50 m)', 'sim' if p['controle_continuo'] else 'não (só eixo, 3,50 e 5,50)')]:
        linha(k, v)
    r += 1
    bloco('Resultado do greide adotado')
    linha('Volume da camada onde o fundo é conhecido (m³)', round(resumo['volume_m3'], 1), '#,##0.0')
    linha('Volume mínimo teórico (greide = limite, sem suavizar) (m³)', round(resumo['volume_limite_inferior_m3'], 1), '#,##0.0')
    linha('Espessura mínima nos controles (m)', round(resumo['espessura_minima_controles'], 3), '0.000')
    ip = resumo['iri']['projeto_por_trilha']; ie = resumo['iri']['existente_por_trilha_indicativo']
    for o in sorted(ip, key=float):
        linha(f'IRI do greide projetado — trilha {float(o):+.1f} m (m/km)', round(ip[o], 2), '0.00')
    for o in sorted(ie, key=float):
        linha(f'IRI da pista existente — trilha {float(o):+.1f} m (m/km, indicativo*)', round(ie[o], 2), '0.00')
    rs.cell(r, 1, '* Levantamento com seções a ~5 m não capta ondas curtas: IRI existente subestimado; serve só para comparar.').font = f_sub
    r += 2
    bloco('Alternativas de suavidade (volume x IRI)')
    hd = ['Variação máx. de rampa (mm/5 m)', 'Volume (m³)', 'Excesso médio sobre o limite (cm)', 'IRI greide, eixo (m/km)', 'IRI máx. em 100 m, pior trilha']
    for j, t in enumerate(hd, 1):
        c = rs.cell(r, j, t); c.font = Font(name='Arial', size=9, bold=True); c.fill = PatternFill('solid', fgColor=CLARO); c.alignment = centro
    r += 1
    for al in resumo['alternativas']:
        vals = [al['suavidade_mm'], al.get('volume_m3'), al.get('excesso_medio_cm'), al.get('iri_greide_m_km'), al.get('iri_greide_max_100m')]
        for j, v in enumerate(vals, 1):
            c = rs.cell(r, j, None if v is None else round(v, 2)); c.font = f_base; c.alignment = Alignment(horizontal='center')
        if abs(al['suavidade_mm'] - p['suavidade_mm']) < 1e-9:
            for j in range(1, 6):
                rs.cell(r, j).fill = fill_ok
        r += 1
    r += 1
    if resumo['curvas']:
        bloco('Curvas com superelevação')
        for cv in resumo['curvas']:
            linha(f"{fmt_estaca(cv['inicio'])} a {fmt_estaca(cv['fim'])} — R ≈ {cv['raio_m']:.0f} m à {cv['lado']}",
                  f"e = {cv['superelevacao_pct']:.2f} %, transição {cv['transicao_m']:.0f} m")
        r += 1
    if resumo['coerencia']:
        bloco('Conferência pista x levantamento após rebaixo')
        for cc in resumo['coerencia']:
            linha(f"{cc['controle']} ({cc['estacas']})", f"mediana {cc['rebaixo_medido_mediana']:+.3f} m "
                  f"(p10 {cc['p10']:+.3f}, p90 {cc['p90']:+.3f})")
        r += 1
    bloco('Alertas')
    for al in resumo['alertas']:
        c = rs.cell(r, 1, '• ' + al); c.font = Font(name='Arial', size=9, color='B42318')
        rs.merge_cells(start_row=r, start_column=1, end_row=r, end_column=7)
        c.alignment = Alignment(wrap_text=True, vertical='top')
        rs.row_dimensions[r].height = 30
        r += 1

    # ---------------- IRI 100 m
    wi = wb.create_sheet('IRI 100 m')
    for j, t in enumerate(['Início', 'Fim', 'IRI teórico do greide, pior trilha (m/km)'], 1):
        c = wi.cell(1, j, t); c.font = f_head; c.fill = fill_h; c.alignment = centro
    for i, x in enumerate(resumo['iri']['projeto_por_100m'], 2):
        wi.cell(i, 1, fmt_estaca(x['inicio'])); wi.cell(i, 2, fmt_estaca(x['fim']))
        wi.cell(i, 3, round(x['iri'], 3)).number_format = '0.00'
    for j, w in zip('ABC', (14, 14, 34)):
        wi.column_dimensions[j].width = w

    # ---------------- PONTOS
    wp = wb.create_sheet('PONTOS')
    hd = ['Arquivo', 'Número', 'Este (m)', 'Norte (m)', 'Cota (m)', 'Descrição', 'Estaca', 'Progressiva (m)', 'Afastamento (m)', 'Papel']
    for j, t in enumerate(hd, 1):
        c = wp.cell(1, j, t); c.font = f_head; c.fill = fill_h; c.alignment = centro
    for i, pt in enumerate(list(pista) + list(fundo_pts), 2):
        fora = pt.papel == 'FORA DO EIXO'
        vals = [pt.origem, pt.numero, round(pt.e, 3), round(pt.n, 3), round(pt.z, 3), pt.desc,
                None if fora else fmt_estaca(pt.estaca), None if fora else round(pt.estaca, 3),
                None if fora else round(pt.afast, 3), pt.papel]
        for j, v in enumerate(vals, 1):
            wp.cell(i, j, v)
    for j, w in enumerate((30, 9, 13, 13, 9, 9, 12, 13, 13, 22), 1):
        wp.column_dimensions[L(j)].width = w
    wp.freeze_panes = 'A2'
    wp.auto_filter.ref = f'A1:J{len(pista) + len(fundo_pts) + 1}'
    wb.save(path)


def gravar_landxml(path, eixo, est, G, resumo, q_le, q_ld):
    """LandXML 1.2: alinhamento (linhas), perfil de projeto (PVIs) e bordos 3D (PlanFeatures) + superfície do topo."""
    agora = resumo['gerado_em']
    nome = 'GREIDE IFM'
    # alinhamento: reamostra o eixo a cada 5 m (ou mantém os vértices do XML original)
    s_al = np.unique(np.r_[np.arange(eixo.est[0], eixo.est[-1], 5.0), eixo.est[-1]])
    E, N = eixo.ponto(s_al)
    out = ['<?xml version="1.0" encoding="UTF-8"?>',
           '<LandXML xmlns="http://www.landxml.org/schema/LandXML-1.2" version="1.2" '
           f'date="{agora[:10]}" time="{agora[11:19]}">',
           '<Units><Metric areaUnit="squareMeter" linearUnit="meter" volumeUnit="cubicMeter" '
           'temperatureUnit="celsius" pressureUnit="milliBars" angularUnit="decimal degrees" directionUnit="decimal degrees"/></Units>',
           '<Project name="REABILITACAO DE PISTA - IFM"/>',
           '<Application name="Reabilitacao_Pista_IFM" version="0.2"/>',
           '<Alignments name="IFM">',
           f'<Alignment name="{escape(eixo.nome)}" length="{s_al[-1] - s_al[0]:.4f}" staStart="{s_al[0]:.4f}">',
           '<CoordGeom>']
    for i in range(len(s_al) - 1):
        out.append(f'<Line length="{s_al[i + 1] - s_al[i]:.4f}"><Start>{N[i]:.4f} {E[i]:.4f}</Start>'
                   f'<End>{N[i + 1]:.4f} {E[i + 1]:.4f}</End></Line>')
    out.append('</CoordGeom>')
    out.append(f'<Profile name="{nome}"><ProfAlign name="{nome}">')
    for s, g in zip(est, G):
        out.append(f'<PVI>{s:.4f} {g:.4f}</PVI>')
    out.append('</ProfAlign></Profile>')
    out.append('</Alignment></Alignments>')
    # bordos 3D
    out.append('<PlanFeatures name="BORDOS PROJETO IFM">')
    tx, ty = eixo.rumo(est)
    Ee, Ne = eixo.ponto(est)
    linhas3d = {}
    for o in OFFS:
        qq = np.asarray(q_le if o < 0 else q_ld, float)
        z = G + qq * abs(o) if o else G
        # normal à direita: (ty, -tx)
        xe = Ee + o * ty; yn = Ne - o * tx
        linhas3d[o] = (xe, yn, z)
        segs = ''.join(f'<Line><Start>{yn[i]:.4f} {xe[i]:.4f} {z[i]:.4f}</Start>'
                       f'<End>{yn[i + 1]:.4f} {xe[i + 1]:.4f} {z[i + 1]:.4f}</End></Line>'
                       for i in range(len(z) - 1))
        out.append(f'<PlanFeature name="{NOMES[o]} PROJETO"><CoordGeom>{segs}</CoordGeom></PlanFeature>')
    out.append('</PlanFeatures>')
    # superfície do topo
    out.append('<Surfaces><Surface name="TOPO PROJETO IFM"><Definition surfType="TIN"><Pnts>')
    pid = {}
    k = 1
    for j, o in enumerate(OFFS):
        xe, yn, z = linhas3d[o]
        for i in range(len(est)):
            pid[(i, j)] = k
            out.append(f'<P id="{k}">{yn[i]:.4f} {xe[i]:.4f} {z[i]:.4f}</P>')
            k += 1
    out.append('</Pnts><Faces>')
    for i in range(len(est) - 1):
        for j in range(len(OFFS) - 1):
            a, b, c, d = pid[(i, j)], pid[(i + 1, j)], pid[(i + 1, j + 1)], pid[(i, j + 1)]
            out.append(f'<F>{a} {b} {c}</F>')
            out.append(f'<F>{a} {c} {d}</F>')
    out.append('</Faces></Definition></Surface></Surfaces>')
    out.append('</LandXML>')
    path.write_text('\n'.join(out), encoding='utf-8')
