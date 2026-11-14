from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

wb = Workbook()
ws = wb.active
ws.title = "Orcamento"

FONT = "Arial"
BLUE = "0000FF"     # entradas / valores editaveis
BLACK = "000000"    # formulas
WHITE = "FFFFFF"
HEAD_BG = "1F2A37"  # cinza escuro
ACCENT = "F2A900"   # amarelo/laranja START
YELLOW = "FFFF00"   # preencher
LIGHT = "F3F4F6"

thin = Side(style="thin", color="BFBFBF")
border = Border(left=thin, right=thin, top=thin, bottom=thin)

# itens: (descricao, n_objetos, mat_unit, mo_unit)  -> n_servicos = n_objetos por padrao
itens = [
    ("PONTO ILUMINAÇÃO",                                122, 95.0, 75.0),
    ("PONTO TOMADAS",                                    94, 125.0, 80.0),
    ("PONTOS INTERRUPTOR",                               82, 75.0, 25.0),
    ("PONTOS ILUMINAÇÃO MOVEIS",                          5, 105.0, 95.0),
    ("PONTOS DE CORTINA ELETRICA",                       11, 125.0, 80.0),
    ("PONTOS CHUVEIRO ELETRICO 220V",                     5, 0.0, 1230.0),
    ("PONTOS CAMERA (CFTV - INFRA SECA COM GUIA)",        4, 405.0, 95.0),
    ("PONTOS AR CONDICIONADO",                            4, 550.0, 180.0),
]

# ---------- cabecalho ----------
ws.merge_cells("A1:I1")
c = ws["A1"]; c.value = "START - INSTALAÇÕES ELÉTRICAS"
c.font = Font(name=FONT, size=18, bold=True, color=WHITE)
c.alignment = Alignment(horizontal="center", vertical="center")
c.fill = PatternFill("solid", fgColor=HEAD_BG)
ws.row_dimensions[1].height = 30

ws.merge_cells("A2:I2")
c = ws["A2"]; c.value = "PLANILHA DE ORÇAMENTO"
c.font = Font(name=FONT, size=12, bold=True, color=HEAD_BG)
c.alignment = Alignment(horizontal="center", vertical="center")
c.fill = PatternFill("solid", fgColor=ACCENT)
ws.row_dimensions[2].height = 22

# ---------- titulos das colunas ----------
headers = ["ITEM", "DESCRIÇÃO", "Nº OBJETOS", "Nº SERVIÇOS",
           "VALOR UNIT.\nMATERIAL", "TOTAL\nMATERIAL",
           "VALOR UNIT.\nSERVIÇO (M.O)", "TOTAL\nSERVIÇO", "TOTAL\nMAT + M.O"]
hr = 4
for j, h in enumerate(headers, start=1):
    c = ws.cell(row=hr, column=j, value=h)
    c.font = Font(name=FONT, size=10, bold=True, color=WHITE)
    c.fill = PatternFill("solid", fgColor=HEAD_BG)
    c.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)
    c.border = border
ws.row_dimensions[hr].height = 32

money = 'R$ #,##0.00'

# ---------- linhas de dados ----------
r = hr + 1
first = r
for i, (desc, nobj, mat, mo) in enumerate(itens, start=1):
    ws.cell(row=r, column=1, value=i).font = Font(name=FONT, size=10)
    ws.cell(row=r, column=1).alignment = Alignment(horizontal="center")

    ws.cell(row=r, column=2, value=desc).font = Font(name=FONT, size=10)

    # Nº objetos (entrada azul)
    cobj = ws.cell(row=r, column=3, value=nobj)
    cobj.font = Font(name=FONT, size=10, color=BLUE)
    cobj.alignment = Alignment(horizontal="center")

    # Nº servicos = mesmo qtd por padrao, referenciando objetos (formula)
    cserv = ws.cell(row=r, column=4, value=f"=C{r}")
    cserv.font = Font(name=FONT, size=10, color=BLACK)
    cserv.alignment = Alignment(horizontal="center")

    # valor unit material (entrada azul; amarelo se faltando)
    cmat = ws.cell(row=r, column=5)
    cmat.number_format = money
    if mat is None:
        cmat.value = 0
        cmat.fill = PatternFill("solid", fgColor=YELLOW)
        cmat.font = Font(name=FONT, size=10, color=BLUE)
    else:
        cmat.value = mat
        cmat.font = Font(name=FONT, size=10, color=BLUE)

    # total material = objetos * unit
    ctm = ws.cell(row=r, column=6, value=f"=C{r}*E{r}")
    ctm.number_format = money
    ctm.font = Font(name=FONT, size=10, color=BLACK)

    # valor unit servico
    cmo = ws.cell(row=r, column=7)
    cmo.number_format = money
    if mo is None:
        cmo.value = 0
        cmo.fill = PatternFill("solid", fgColor=YELLOW)
        cmo.font = Font(name=FONT, size=10, color=BLUE)
    else:
        cmo.value = mo
        cmo.font = Font(name=FONT, size=10, color=BLUE)

    # total servico = servicos * unit
    cts = ws.cell(row=r, column=8, value=f"=D{r}*G{r}")
    cts.number_format = money
    cts.font = Font(name=FONT, size=10, color=BLACK)

    # total mat+mo
    ctot = ws.cell(row=r, column=9, value=f"=F{r}+H{r}")
    ctot.number_format = money
    ctot.font = Font(name=FONT, size=10, bold=True, color=BLACK)

    for j in range(1, 10):
        ws.cell(row=r, column=j).border = border
        if i % 2 == 0 and ws.cell(row=r, column=j).fill.fgColor.rgb in (None, "00000000"):
            ws.cell(row=r, column=j).fill = PatternFill("solid", fgColor=LIGHT)
    r += 1

last = r - 1

# ---------- linha de total ----------
tr = r
ws.cell(row=tr, column=1, value="TOTAL")
ws.merge_cells(start_row=tr, start_column=1, end_row=tr, end_column=2)
ws.cell(row=tr, column=1).font = Font(name=FONT, size=11, bold=True, color=WHITE)
ws.cell(row=tr, column=1).alignment = Alignment(horizontal="center", vertical="center")

ws.cell(row=tr, column=3, value=f"=SUM(C{first}:C{last})").font = Font(name=FONT, size=11, bold=True, color=WHITE)
ws.cell(row=tr, column=4, value=f"=SUM(D{first}:D{last})").font = Font(name=FONT, size=11, bold=True, color=WHITE)
ws.cell(row=tr, column=3).alignment = Alignment(horizontal="center")
ws.cell(row=tr, column=4).alignment = Alignment(horizontal="center")
ws.cell(row=tr, column=6, value=f"=SUM(F{first}:F{last})")
ws.cell(row=tr, column=8, value=f"=SUM(H{first}:H{last})")
ws.cell(row=tr, column=9, value=f"=SUM(I{first}:I{last})")
for j in (6, 8, 9):
    cc = ws.cell(row=tr, column=j)
    cc.number_format = money
    cc.font = Font(name=FONT, size=11, bold=True, color=WHITE)
for j in range(1, 10):
    cc = ws.cell(row=tr, column=j)
    cc.fill = PatternFill("solid", fgColor=HEAD_BG)
    cc.border = border
ws.row_dimensions[tr].height = 24

# ---------- nota ----------
nr = tr + 2
ws.cell(row=nr, column=1, value="Obs.: valores unitários em azul são editáveis. Células amarelas precisam ser preenchidas (sem preço de referência). 'Nº SERVIÇOS' inicia igual ao 'Nº OBJETOS' (=C) e pode ser ajustado.")
ws.merge_cells(start_row=nr, start_column=1, end_row=nr, end_column=9)
ws.cell(row=nr, column=1).font = Font(name=FONT, size=9, italic=True, color="555555")
ws.cell(row=nr, column=1).alignment = Alignment(horizontal="left", vertical="center", wrap_text=True)
ws.row_dimensions[nr].height = 28

# ---------- larguras ----------
widths = {"A": 7, "B": 42, "C": 12, "D": 13, "E": 14, "F": 15, "G": 15, "H": 15, "I": 16}
for col, w in widths.items():
    ws.column_dimensions[col].width = w

ws.sheet_view.showGridLines = False
ws.freeze_panes = "A5"

wb.save("orcamento_start.xlsx")
print("OK")
