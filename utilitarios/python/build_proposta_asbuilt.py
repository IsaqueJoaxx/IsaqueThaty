# -*- coding: utf-8 -*-
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from datetime import datetime

OUT = r"D:\Claude ai\PROPOSTA AS BUILT - inpEV Sao Joaquim de Bicas.xlsx"

FONT  = "Arial"
DARK  = "153D64"   # azul inpEV
BLUE  = "2E75B6"
LT    = "D6E4F0"
STRIPE= "EEF4FB"
WHITE = "FFFFFF"
YEL   = "FFF2CC"   # campos a preencher
GREY  = "F2F2F2"

thin = Side(style="thin", color="B0B0B0")
B = Border(left=thin, right=thin, top=thin, bottom=thin)

def f(sz=10, b=False, c="000000", i=False): return Font(name=FONT, size=sz, bold=b, color=c, italic=i)
def fill(h): return PatternFill("solid", fgColor=h)

wb = Workbook(); ws = wb.active; ws.title = "Proposta AS BUILT"
ws.sheet_view.showGridLines = False
money = 'R$ #,##0.00'

# larguras
for col,w in zip("ABCDEF",[6,52,8,7,16,18]): ws.column_dimensions[col].width = w

r = 1
def merge(a,b_): ws.merge_cells(f"{a}{r}:{b_}{r}")

# ───────── CABEÇALHO PROPONENTE ─────────
merge("A","F")
c=ws.cell(r,1,"FERNANDES TOPOGRAFIA & AGRIMENSURA"); c.font=f(16,True,WHITE); c.alignment=Alignment("center","center"); c.fill=fill(DARK)
ws.row_dimensions[r].height=28; r+=1
merge("A","F")
c=ws.cell(r,1,"Jonathan Fernandes  •  (31) 99927-4089  •  fernandestopoagri@gmail.com  •  @fernandestopoagri")
c.font=f(9,False,WHITE); c.alignment=Alignment("center","center"); c.fill=fill(BLUE)
ws.row_dimensions[r].height=16; r+=1
r+=1

# ───────── TÍTULO ─────────
merge("A","F")
c=ws.cell(r,1,"PROPOSTA COMERCIAL — SERVIÇO DE AS BUILT (COMO CONSTRUÍDO) EM BIM")
c.font=f(12,True,DARK); c.alignment=Alignment("center","center"); c.fill=fill(LT); c.border=B
ws.row_dimensions[r].height=24; r+=1
merge("A","F")
c=ws.cell(r,1,f"Proposta nº 2026-001  •  Data: {datetime.now().strftime('%d/%m/%Y')}  •  Validade: 15 dias")
c.font=f(9,False,"555555",True); c.alignment=Alignment("center","center")
ws.row_dimensions[r].height=14; r+=1
r+=1

# ───────── DADOS DO CLIENTE / OBRA ─────────
def secao(titulo):
    global r
    merge("A","F")
    c=ws.cell(r,1,titulo); c.font=f(11,True,WHITE); c.fill=fill(DARK); c.alignment=Alignment("left","center"); c.border=B
    c.alignment=Alignment(horizontal="left",vertical="center",indent=1)
    ws.row_dimensions[r].height=20; r+=1

def linha(rotulo,valor):
    global r
    c=ws.cell(r,1,rotulo); c.font=f(10,True,DARK); c.alignment=Alignment("left","center",indent=1)
    ws.merge_cells(f"A{r}:B{r}")
    ws.merge_cells(f"C{r}:F{r}")
    v=ws.cell(r,3,valor); v.font=f(10); v.alignment=Alignment("left","center",indent=1)
    for col in range(1,7): ws.cell(r,col).border=B
    ws.row_dimensions[r].height=18; r+=1

secao("CLIENTE / CONTRATANTE")
linha("Razão social","Instituto Nacional de Processamento de Embalagens Vazias — inpEV")
linha("CNPJ","04.875.587/0009-90 (filial)  •  Associação Privada")
linha("Solicitante","Vanessa Restivo — Suprimentos  •  vanessa.oliveira@inpev.org.br")
linha("Observação fiscal","inpEV é NÃO CONTRIBUINTE do ICMS (comprovante CNPJ anexo ao pedido)")
r+=1

secao("DADOS DA OBRA")
linha("Objeto","AS BUILT da Central existente — levantamento cadastral e modelagem BIM")
linha("Central","São Joaquim de Bicas / MG")
linha("Endereço","Rua Nove, nº 13 — Estância Serra Verde — CEP 32.920-000")
linha("Área do terreno","565,77 m²")
linha("Área construída","144,00 m²")
r+=1

# ───────── TABELA DE PREÇOS ─────────
secao("PLANILHA DE PREÇOS")
hdr_r=r
for j,h in enumerate(["ITEM","DESCRIÇÃO DOS SERVIÇOS","UNID.","QTD","VALOR UNIT.","VALOR TOTAL"],1):
    c=ws.cell(r,j,h); c.font=f(10,True,WHITE); c.fill=fill(BLUE); c.alignment=Alignment("center","center",wrap_text=True); c.border=B
ws.row_dimensions[r].height=24; r+=1

itens=[
 ("1","Mobilização / Desmobilização de equipe e equipamentos","vb",1),
 ("2","Vistoria técnica e levantamento cadastral em campo (edificação, terreno, divisas, acessos, paisagismo) + registro fotográfico e medições","vb",1),
 ("3","Modelagem BIM e elaboração de plantas, cortes, fachadas, layouts, forro, cobertura, piso, pavimentação, pontos elétricos/hidráulicos, ar comprimido e PPCI (itens 01.4 a 01.17 do escopo)","vb",1),
 ("4","Memorial descritivo, quadro de áreas e lista de quantitativos (itens 01.18 a 01.20)","vb",1),
 ("5","ART — Anotação de Responsabilidade Técnica","vb",1),
]
first=r
for it in itens:
    bg = WHITE if (r-first)%2==0 else STRIPE
    ws.cell(r,1,it[0]).alignment=Alignment("center","center")
    ws.cell(r,2,it[1]).alignment=Alignment("left","center",wrap_text=True,indent=1)
    ws.cell(r,3,it[2]).alignment=Alignment("center","center")
    ws.cell(r,4,it[3]).alignment=Alignment("center","center")
    vu=ws.cell(r,5,0); vu.number_format=money; vu.fill=fill(YEL); vu.font=f(10,False,"0000FF")
    vt=ws.cell(r,6,f"=D{r}*E{r}"); vt.number_format=money
    for col in range(1,7):
        cc=ws.cell(r,col); cc.border=B
        if cc.font.size is None or cc.font.name!=FONT: cc.font=f(10)
        if col in (5,6) and bg!=WHITE and col!=5: pass
        if col!=5: cc.fill=fill(bg)
    ws.row_dimensions[r].height=34
    r+=1
last=r-1

# total
ws.merge_cells(f"A{r}:E{r}")
c=ws.cell(r,1,"VALOR TOTAL DA PROPOSTA"); c.font=f(11,True,WHITE); c.alignment=Alignment("right","center"); c.fill=fill(DARK)
t=ws.cell(r,6,f"=SUM(F{first}:F{last})"); t.number_format=money; t.font=f(11,True,WHITE); t.fill=fill(DARK)
for col in range(1,7): ws.cell(r,col).border=B
ws.row_dimensions[r].height=24; r+=1
merge("A","F")
c=ws.cell(r,1,"Preencha os valores na coluna amarela (VALOR UNIT.); o total calcula automaticamente.")
c.font=f(8,False,"888888",True); c.alignment=Alignment("left","center")
r+=2

# ───────── ESCOPO / ENTREGÁVEIS ─────────
secao("ESCOPO CONTRATADO — ENTREGÁVEIS (conforme escopo inpEV / NBR 14645)")
escopo=[
 "01.1  Vistoria técnica e levantamento cadastral de toda a edificação existente (arruamentos, muros, cercas/alambrados de divisa, portões de acesso e paisagismo)",
 "01.2  Registro fotográfico",
 "01.3  Medições e análise das condições existentes",
 "01.4  Planta de implantação com todo o layout (edificações + terreno existente)",
 "01.5  Plantas, cortes e fachadas com cotas, pé-direito, níveis de piso e detalhes técnicos",
 "01.6  Plantas de layouts internos dos ambientes",
 "01.7  Plantas de forro",
 "01.8  Plantas de cobertura",
 "01.9  Plantas de piso",
 "01.10  Plantas de pavimentação",
 "01.11  Plantas de pontos elétricos (tomadas, lâmpadas, interruptores, quadros)",
 "01.12  Plantas de pontos hidráulicos (torneiras, chuveiros, pias, bacias, ralos, caixas de esgoto, bocas de lobo)",
 "01.13  Planta com locação da rede de ar comprimido existente",
 "01.14  Planta de layout PPCI (extintores, luminárias de emergência, sinalização, hidrantes)",
 "01.15  Detalhamento dos elementos construtivos com dimensões (estruturas: seções e materiais)",
 "01.16  Planta de implantação geral incluindo ampliação, com quadros de áreas",
 "01.17  Vistas e layouts 3D",
 "01.18  Memorial descritivo",
 "01.19  Quadro de áreas",
 "01.20  Lista de quantitativos (piso de concreto, pavimentação, piso/revestimento cerâmico, forros, cobertura)",
]
for e in escopo:
    merge("A","F")
    c=ws.cell(r,1,e); c.font=f(9); c.alignment=Alignment("left","center",wrap_text=True,indent=1); c.border=B
    ws.row_dimensions[r].height=24 if len(e)>95 else 16
    r+=1
r+=1

# ───────── NORMAS ─────────
secao("NORMAS E PREMISSAS TÉCNICAS")
normas=[
 "ABNT NBR 13133:2025 — Execução de levantamento topográfico — Procedimento",
 "ABNT NBR 14645-1:2001 — Elaboração do \"como construído\" (as built) para edificações — Parte 1: levantamento planialtimétrico e cadastral de imóvel urbanizado com área até 25.000 m²",
 "Projeto/modelo entregue em BIM, conforme exigência do escopo.",
]
for n in normas:
    merge("A","F")
    c=ws.cell(r,1,"•  "+n); c.font=f(9); c.alignment=Alignment("left","center",wrap_text=True,indent=1); c.border=B
    ws.row_dimensions[r].height=24
    r+=1
r+=1

# ───────── RESPONSABILIDADES DA CONTRATADA ─────────
secao("RESPONSABILIDADES DA CONTRATADA")
resp=[
 "Fornecer equipe especializada, equipamentos e ferramentas necessários",
 "Responsabilizar-se pela guarda dos equipamentos / ferramentas",
 "Fornecimento de água",
 "Fornecimento de fonte geradora de energia, se necessário",
 "Fornecimento de alimentação para a equipe",
 "Fornecimento de documentações técnicas",
 "Fornecimento dos arquivos digitais DWG, PDF e KML dos projetos, conforme premissas",
 "Fornecimento de EPI's e documentações de segurança",
]
for x in resp:
    merge("A","F")
    c=ws.cell(r,1,"•  "+x); c.font=f(9); c.alignment=Alignment("left","center",wrap_text=True,indent=1); c.border=B
    ws.row_dimensions[r].height=18
    r+=1
r+=1

# ───────── CONDIÇÕES COMERCIAIS ─────────
secao("CONDIÇÕES COMERCIAIS")
cond=[
 ("Condição de pagamento","30 DDL (30 dias após a data de faturamento). Não há adiantamento."),
 ("Validade da proposta","15 dias corridos a partir da data de emissão."),
 ("Prazo de execução","A combinar — preencher (ex.: __ dias úteis após autorização)."),
 ("Tributos","inpEV é não contribuinte do ICMS. Impostos conforme legislação vigente."),
 ("Não incluso","Despesas não previstas neste escopo, taxas de aprovação em órgãos e serviços adicionais."),
]
for rot,val in cond:
    ws.merge_cells(f"A{r}:B{r}"); ws.merge_cells(f"C{r}:F{r}")
    c=ws.cell(r,1,rot); c.font=f(10,True,DARK); c.alignment=Alignment("left","center",indent=1)
    v=ws.cell(r,3,val); v.font=f(10); v.alignment=Alignment("left","center",wrap_text=True,indent=1)
    for col in range(1,7): ws.cell(r,col).border=B
    ws.row_dimensions[r].height=28
    r+=1
r+=1

merge("A","F")
c=ws.cell(r,1,"Atenciosamente,  Fernandes Topografia & Agrimensura  —  Jonathan Fernandes")
c.font=f(10,True,DARK); c.alignment=Alignment("center","center")
ws.row_dimensions[r].height=22

wb.save(OUT)
print("OK ->", OUT)
