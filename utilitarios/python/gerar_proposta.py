# -*- coding: utf-8 -*-
from docx import Document
from docx.shared import Pt, RGBColor, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

AZUL = RGBColor(0x1F, 0x4E, 0x79)
CINZA = RGBColor(0x59, 0x59, 0x59)
HEADER_FILL = "1F4E79"
LINHA_FILL = "D9E2F3"

doc = Document()

# Fonte padrao
normal = doc.styles['Normal']
normal.font.name = 'Calibri'
normal.font.size = Pt(11)

# Margens
for s in doc.sections:
    s.top_margin = Cm(2)
    s.bottom_margin = Cm(2)
    s.left_margin = Cm(2.2)
    s.right_margin = Cm(2.2)

def set_cell_bg(cell, fill):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement('w:shd')
    shd.set(qn('w:val'), 'clear')
    shd.set(qn('w:color'), 'auto')
    shd.set(qn('w:fill'), fill)
    tcPr.append(shd)

def add_heading(num, text):
    p = doc.add_paragraph()
    p.space_before = Pt(10)
    r = p.add_run(f"{num}.  {text}")
    r.bold = True
    r.font.size = Pt(13)
    r.font.color.rgb = AZUL
    pPr = p._p.get_or_add_pPr()
    pBdr = OxmlElement('w:pBdr')
    bottom = OxmlElement('w:bottom')
    bottom.set(qn('w:val'), 'single')
    bottom.set(qn('w:sz'), '6')
    bottom.set(qn('w:space'), '2')
    bottom.set(qn('w:color'), '1F4E79')
    pBdr.append(bottom)
    pPr.append(pBdr)
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    return p

def add_bullet(text, level=0):
    p = doc.add_paragraph(style='List Bullet')
    p.paragraph_format.left_indent = Cm(0.8 + level*0.6)
    p.paragraph_format.space_after = Pt(2)
    runs = text.split('**')
    bold = False
    for seg in runs:
        if seg:
            r = p.add_run(seg)
            r.bold = bold
        bold = not bold
    return p

def add_item(code, text):
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Cm(0.8)
    p.paragraph_format.space_after = Pt(2)
    r = p.add_run(f"{code}  ")
    r.bold = True
    r.font.color.rgb = AZUL
    p.add_run(text)
    return p

# ---------- CABECALHO / TITULO ----------
t = doc.add_paragraph()
t.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = t.add_run("PROPOSTA COMERCIAL")
r.bold = True
r.font.size = Pt(20)
r.font.color.rgb = AZUL

sub = doc.add_paragraph()
sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = sub.add_run("Levantamento As-Built — Cadastral Planialtimétrico + As-Built Arquitetônico em BIM")
r.italic = True
r.font.size = Pt(11)
r.font.color.rgb = CINZA

meta = doc.add_paragraph()
meta.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = meta.add_run("Proposta nº 001/2026     |     Data: 16/06/2026     |     Validade: 30 dias")
r.font.size = Pt(10)
r.font.color.rgb = CINZA
doc.add_paragraph()

def dados_table(titulo, linhas):
    add_heading(titulo[0], titulo[1])
    tbl = doc.add_table(rows=0, cols=2)
    tbl.style = 'Table Grid'
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    for k, v in linhas:
        row = tbl.add_row().cells
        row[0].width = Cm(4.5)
        row[1].width = Cm(12)
        set_cell_bg(row[0], LINHA_FILL)
        pk = row[0].paragraphs[0]
        rk = pk.add_run(k); rk.bold = True; rk.font.size = Pt(10)
        pv = row[1].paragraphs[0]
        rv = pv.add_run(v); rv.font.size = Pt(10)
    return tbl

# ---------- 1. CONTRATANTE ----------
dados_table(("1", "CONTRATANTE"), [
    ("Razão Social", "Instituto Nacional de Processamento de Embalagens Vazias – INPEV"),
    ("Nome Fantasia", "INPEV – Instituto Nacional Proc. Embalagens Vazias"),
    ("CNPJ", "04.875.587/0009-90 (Filial)"),
    ("Endereço", "Rua Nove, nº 13 – Estância Serra Verde – São Joaquim de Bicas/MG"),
    ("CEP", "32.920-000"),
    ("Contato", "inpevfiscal@inpev.org.br — (11) 3069-4423 / (11) 3069-4427"),
])

# ---------- 2. CONTRATADA ----------
dados_table(("2", "CONTRATADA"), [
    ("Razão Social / Nome", "[ preencher ]"),
    ("CNPJ / CPF", "[ preencher ]"),
    ("Endereço", "[ preencher ]"),
    ("Contato", "isaquejoa@gmail.com — [ telefone ]"),
    ("Responsável Técnico", "[ nome ] — Eng./Arq. CREA/CAU nº [ ___ ]"),
])

# ---------- 3. OBJETO ----------
add_heading("3", "OBJETO")
doc.add_paragraph(
    "Execução de levantamento As-Built da central existente do INPEV em São Joaquim de Bicas/MG, "
    "contemplando levantamento topográfico planialtimétrico cadastral georreferenciado e modelagem "
    "arquitetônica “como construído” (as-built) em ambiente BIM, com entrega de plantas, cortes, "
    "fachadas, modelo 3D, memorial descritivo e quadros de áreas/quantitativos da edificação e do terreno existentes."
)

# ---------- 4. ESCOPO ----------
add_heading("4", "ESCOPO DETALHADO")

p = doc.add_paragraph(); r = p.add_run("4.1  Mobilização e Vistoria Técnica"); r.bold = True
add_item("4.1.1", "Mobilização / desmobilização de equipe e equipamentos até o local.")
add_item("4.1.2", "Vistoria técnica para levantamento cadastral de toda a edificação existente, incluindo arruamentos, muros, cercas/alambrados das divisas de perímetro, portões de acesso e paisagismo.")
add_item("4.1.3", "Medições em campo e análise das condições existentes.")
add_item("4.1.4", "Registro fotográfico completo (edificação, terreno e entorno).")

p = doc.add_paragraph(); r = p.add_run("4.2  Levantamento Topográfico Planialtimétrico Cadastral"); r.bold = True
p2 = doc.add_paragraph(); p2.paragraph_format.left_indent = Cm(0.8)
r = p2.add_run("Conforme ABNT NBR 13133:2025 e ABNT NBR 14645-1:2001 (Versão Corrigida)."); r.italic = True; r.font.size = Pt(9); r.font.color.rgb = CINZA
add_item("4.2.1", "Levantamento planimétrico e altimétrico georreferenciado do terreno.")
add_item("4.2.2", "Perfil do terreno, curvas de nível e cotas altimétricas.")
add_item("4.2.3", "Detalhamento do terreno: construções existentes com medidas lineares e volumétricas.")
add_item("4.2.4", "Vias de acesso, guias, sarjetas, hidrografia, vegetação e interferências existentes.")
add_item("4.2.5", "Planta planialtimétrica com plantas horizontais, perfil do terreno e plano vertical.")

p = doc.add_paragraph(); r = p.add_run("4.3  As-Built Arquitetônico (modelagem em BIM)"); r.bold = True
itens43 = [
    ("4.3.1", "Planta de Implantação com todo o layout (edificações + terreno existente)."),
    ("4.3.2", "Plantas, cortes e fachadas com cotas de dimensões, pé-direito, níveis de piso e detalhes técnicos."),
    ("4.3.3", "Plantas de layouts internos dos ambientes."),
    ("4.3.4", "Plantas de forro."),
    ("4.3.5", "Plantas de cobertura."),
    ("4.3.6", "Plantas de piso."),
    ("4.3.7", "Plantas de pavimentação."),
    ("4.3.8", "Plantas de pontos elétricos (tomadas / lâmpadas / interruptores / quadros elétricos)."),
    ("4.3.9", "Plantas de pontos hidráulicos (torneiras / chuveiros / pias / bacias sanitárias / ralos / caixas de esgoto / bocas de lobo)."),
    ("4.3.10", "Planta com locação de rede de ar comprimido existente."),
    ("4.3.11", "Planta de layout PPCI existente (extintores / luminárias de emergência / sinalização de emergência / hidrantes)."),
    ("4.3.12", "Detalhamento dos elementos construtivos com dimensões (estruturas com seções e tipos de materiais)."),
    ("4.3.13", "Planta de Implantação Geral incluindo ampliação, com quadros de áreas."),
    ("4.3.14", "Vistas e layouts 3D."),
]
for c, t_ in itens43:
    add_item(c, t_)

p = doc.add_paragraph(); r = p.add_run("4.4  Documentação Técnica e Quantitativos"); r.bold = True
add_item("4.4.1", "Memorial descritivo com dados técnicos, fotos e tabela de coordenadas georreferenciadas.")
add_item("4.4.2", "Quadro de áreas.")
add_item("4.4.3", "Lista de quantitativos: piso de concreto, pavimentação, piso cerâmico, revestimento cerâmico de paredes, forros e cobertura (metragens).")
add_item("4.4.4", "Emissão de ART (Anotação de Responsabilidade Técnica).")

p = doc.add_paragraph(); r = p.add_run("4.5  Entregáveis (formatos)"); r.bold = True
doc.add_paragraph(
    "Arquivos digitais em DWG, PDF e KML, modelo BIM (.rvt / IFC), memorial descritivo e relatório "
    "fotográfico, conforme premissas."
).paragraph_format.left_indent = Cm(0.8)

# ---------- 5. PREMISSAS ----------
add_heading("5", "PREMISSAS E RESPONSABILIDADES DA CONTRATADA")
for t_ in [
    "Fornecimento de equipe especializada, equipamentos e ferramentas necessárias.",
    "Responsabilidade pela guarda dos equipamentos/ferramentas em campo.",
    "Fornecimento de água, alimentação e fonte geradora de energia para a equipe, se necessário.",
    "Fornecimento de toda a documentação técnica e dos arquivos digitais (DWG, PDF, KML).",
    "Fornecimento de EPIs e documentação de segurança da equipe.",
]:
    add_bullet(t_)

# ---------- 6. RESPONSABILIDADES CONTRATANTE ----------
add_heading("6", "RESPONSABILIDADES DA CONTRATANTE")
for t_ in [
    "Liberação e acesso à área e às edificações nas datas agendadas.",
    "Disponibilização de informações e projetos existentes que auxiliem o levantamento.",
    "Indicação de preposto para acompanhamento e validação das etapas.",
]:
    add_bullet(t_)

# ---------- 7. PRAZO ----------
add_heading("7", "PRAZO")
doc.add_paragraph(
    "[ ____ ] dias úteis a partir da mobilização / liberação da área (sugestão: 25 a 35 dias úteis)."
)

# ---------- 8. VALOR E PAGAMENTO ----------
add_heading("8", "VALOR E CONDIÇÕES DE PAGAMENTO")
pv = doc.add_paragraph()
r = pv.add_run("Valor global: R$ 15.000,00 (quinze mil reais)")
r.bold = True; r.font.size = Pt(13); r.font.color.rgb = AZUL

tbl = doc.add_table(rows=1, cols=4)
tbl.style = 'Table Grid'
tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
hdr = tbl.rows[0].cells
for i, h in enumerate(["Parcela", "Marco", "%", "Valor"]):
    set_cell_bg(hdr[i], HEADER_FILL)
    pp = hdr[i].paragraphs[0]
    pp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    rr = pp.add_run(h); rr.bold = True; rr.font.color.rgb = RGBColor(0xFF,0xFF,0xFF); rr.font.size = Pt(10)
linhas = [
    ("1ª", "Na contratação (mobilização / início)", "30%", "R$ 4.500,00"),
    ("2ª", "Na entrega dos produtos", "50%", "R$ 7.500,00"),
    ("3ª", "Na aprovação final pela CONTRATANTE", "20%", "R$ 3.000,00"),
]
for ln in linhas:
    cells = tbl.add_row().cells
    for i, val in enumerate(ln):
        pp = cells[i].paragraphs[0]
        if i in (0,2,3): pp.alignment = WD_ALIGN_PARAGRAPH.CENTER
        rr = pp.add_run(val); rr.font.size = Pt(10)
        if i == 3: rr.bold = True
# total
cells = tbl.add_row().cells
set_cell_bg(cells[0], LINHA_FILL); set_cell_bg(cells[1], LINHA_FILL); set_cell_bg(cells[2], LINHA_FILL); set_cell_bg(cells[3], LINHA_FILL)
cells[0].paragraphs[0].add_run("")
pp = cells[1].paragraphs[0]; pp.alignment = WD_ALIGN_PARAGRAPH.RIGHT; rr = pp.add_run("TOTAL"); rr.bold = True; rr.font.size = Pt(10)
pp = cells[2].paragraphs[0]; pp.alignment = WD_ALIGN_PARAGRAPH.CENTER; rr = pp.add_run("100%"); rr.bold = True; rr.font.size = Pt(10)
pp = cells[3].paragraphs[0]; pp.alignment = WD_ALIGN_PARAGRAPH.CENTER; rr = pp.add_run("R$ 15.000,00"); rr.bold = True; rr.font.size = Pt(10)

pg = doc.add_paragraph()
r = pg.add_run("Forma de pagamento: PIX / transferência — dados: [ preencher ]. Impostos conforme legislação vigente.")
r.font.size = Pt(10); r.font.color.rgb = CINZA

# ---------- 9. VALIDADE ----------
add_heading("9", "VALIDADE")
doc.add_paragraph("Esta proposta tem validade de 30 (trinta) dias a contar da data de emissão.")

# ---------- ASSINATURA ----------
doc.add_paragraph(); doc.add_paragraph()
ass = doc.add_paragraph(); ass.alignment = WD_ALIGN_PARAGRAPH.CENTER
ass.add_run("_______________________________________________")
local = doc.add_paragraph(); local.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = local.add_run("[ Nome / Assinatura ] — Responsável Técnico"); r.bold = True
data = doc.add_paragraph(); data.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = data.add_run("São Joaquim de Bicas/MG, ____ de _______________ de 2026."); r.font.size = Pt(10); r.font.color.rgb = CINZA

out = r"D:\Claude ai\Proposta_AsBuilt_INPEV_Sao_Joaquim_de_Bicas.docx"
doc.save(out)
print("SALVO:", out)
