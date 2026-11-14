import json, os, subprocess
from datetime import datetime
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side


DATA_PATH = r"D:\Claude ai\vol_temp.json"
OUT_PATH  = r"D:\Claude ai\volume_seccoes.xlsx"

with open(DATA_PATH, encoding="utf-8") as f:
    data = json.load(f)

wb = Workbook()
ws = wb.active
ws.title = "Volume Seções"

FONT     = "Arial"
DARK     = "1B3A6B"
BLUE     = "2E75B6"
LT_BLUE  = "D6E4F0"
STRIPE   = "EBF3FB"
WHITE    = "FFFFFF"
YELLOW   = "FFF2CC"

def side(color="AAAAAA"): return Side(style="thin", color=color)
def brd(c="AAAAAA"): return Border(left=side(c), right=side(c), top=side(c), bottom=side(c))
def fill(hex_): return PatternFill("solid", fgColor=hex_)
def font(size=10, bold=False, color=None, italic=False):
    return Font(name=FONT, size=size, bold=bold,
                color=color or "000000", italic=italic)

def hcell(ws, row, col, value, bg, fc=WHITE, sz=10, bold=True, wrap=False, halign="center"):
    c = ws.cell(row=row, column=col, value=value)
    c.font    = font(sz, bold, fc)
    c.fill    = fill(bg)
    c.alignment = Alignment(horizontal=halign, vertical="center", wrap_text=wrap)
    c.border  = brd("888888")
    return c

# ── Linha 1: título ─────────────────────────────────────────────────────────
ws.merge_cells("A1:E1")
hcell(ws, 1, 1, "CÁLCULO DE VOLUME — MÉTODO DA SEMIDISTÂNCIA",
      DARK, WHITE, sz=13, bold=True)
ws.row_dimensions[1].height = 30

# ── Linha 2: fórmula ─────────────────────────────────────────────────────────
ws.merge_cells("A2:E2")
hcell(ws, 2, 1, "Fórmula:  V_i  =  Área_i  ×  Semidistância_i",
      BLUE, WHITE, sz=10, bold=False, halign="center")
ws.row_dimensions[2].height = 18

# ── Linha 3: data ─────────────────────────────────────────────────────────
ws.merge_cells("A3:E3")
c = ws.cell(row=3, column=1,
            value=f"Gerado em: {datetime.now().strftime('%d/%m/%Y  %H:%M')}")
c.font      = font(9, False, "555555", italic=True)
c.alignment = Alignment(horizontal="right", vertical="center")
ws.row_dimensions[3].height = 14

# ── Linha 4: espaço ──────────────────────────────────────────────────────────
ws.row_dimensions[4].height = 6

# ── Linha 5: cabeçalhos da tabela ────────────────────────────────────────────
HR = 5
HEADERS = ["Nº", "Estaca / Seção", "Área (m²)", "Semidistância (m)", "Volume (m³)"]
for j, h in enumerate(HEADERS, 1):
    hcell(ws, HR, j, h, DARK, WHITE, sz=10, bold=True, wrap=True)
ws.row_dimensions[HR].height = 30

# ── Linhas de dados ──────────────────────────────────────────────────────────
r     = HR + 1
first = r
FMT_M  = '#,##0.0000'
FMT_SD = '#,##0.000'
FMT_V  = '#,##0.000'

for item in data:
    bg = WHITE if item["n"] % 2 == 1 else STRIPE

    # Nº
    c = ws.cell(row=r, column=1, value=item["n"])
    c.alignment = Alignment(horizontal="center", vertical="center")

    # Estaca
    c = ws.cell(row=r, column=2, value=item["sta"])
    c.alignment = Alignment(horizontal="left", vertical="center")

    # Área
    c = ws.cell(row=r, column=3, value=item["area"])
    c.number_format = FMT_M

    # Semidistância
    c = ws.cell(row=r, column=4, value=item["sd"])
    c.number_format = FMT_SD

    # Volume como fórmula Excel
    c = ws.cell(row=r, column=5, value=f"=C{r}*D{r}")
    c.number_format = FMT_V

    for j in range(1, 6):
        cc = ws.cell(row=r, column=j)
        cc.font   = font(10)
        cc.fill   = fill(bg)
        cc.border = brd()
        if not cc.alignment or cc.alignment.horizontal is None:
            cc.alignment = Alignment(vertical="center")
    ws.row_dimensions[r].height = 18
    r += 1

last = r - 1

# ── Linha de total ───────────────────────────────────────────────────────────
tr = r
ws.merge_cells(start_row=tr, start_column=1, end_row=tr, end_column=2)
hcell(ws, tr, 1, "VOLUME TOTAL", DARK, WHITE, sz=11, bold=True)
ws.cell(row=tr, column=1).alignment = Alignment(horizontal="center", vertical="center")

c = ws.cell(row=tr, column=3, value=f"=SUM(C{first}:C{last})")
c.number_format = FMT_M

ws.cell(row=tr, column=4, value="")  # sem soma de semidist

c = ws.cell(row=tr, column=5, value=f"=SUM(E{first}:E{last})")
c.number_format = FMT_V

for j in range(1, 6):
    cc = ws.cell(row=tr, column=j)
    cc.fill   = fill(DARK)
    cc.font   = font(11, True, WHITE)
    cc.border = brd("000000")
    cc.alignment = Alignment(horizontal="center", vertical="center")
ws.row_dimensions[tr].height = 28

# ── Legenda ──────────────────────────────────────────────────────────────────
nr = tr + 2
ws.merge_cells(start_row=nr, start_column=1, end_row=nr, end_column=5)
c = ws.cell(row=nr, column=1,
            value="Obs.: Volumes calculados pelo Método da Semidistância — V = Área × Semidistância. "
                  "Coluna 'Volume' usa fórmula Excel (C×D), recalcula ao editar.")
c.font      = font(9, False, "555555", italic=True)
c.alignment = Alignment(horizontal="left", vertical="center", wrap_text=True)
ws.row_dimensions[nr].height = 28

# ── Larguras das colunas ──────────────────────────────────────────────────────
for col, w in zip("ABCDE", [6, 24, 15, 18, 16]):
    ws.column_dimensions[col].width = w

ws.sheet_view.showGridLines = False
ws.freeze_panes = f"A{HR+1}"

wb.save(OUT_PATH)
os.startfile(OUT_PATH)
