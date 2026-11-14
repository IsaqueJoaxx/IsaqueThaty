import sys, openpyxl
wb = openpyxl.load_workbook(sys.argv[1])
ws = wb.worksheets[0]
print("dim", ws.dimensions)
def fmt(v):
    if v is None: return ""
    if isinstance(v, float): return f"{v:.3f}"
    return str(v)
for r in range(1, 6):
    print(r, " | ".join(fmt(c.value) for c in ws[r] if c.value is not None))
ini = int(sys.argv[2]) if len(sys.argv) > 2 else 6
fim = int(sys.argv[3]) if len(sys.argv) > 3 else 40
for r in range(ini, min(fim, ws.max_row) + 1):
    print(r, ";".join(fmt(c.value) for c in ws[r]))
o = wb.worksheets[1]
print("--- Origem")
for row in o.iter_rows(min_row=3, values_only=True):
    if row[0]: print(row[0], "=", (row[1] or "")[:400])
