import sys, openpyxl
wb = openpyxl.load_workbook(sys.argv[1])
ws = wb.worksheets[0]
print("dim", ws.dimensions, "papel", ws.page_setup.paperSize)
h4 = [c.value for c in ws[4]]
h5 = [c.value for c in ws[5]]
grupo = None
cols = []
for i, (a, b) in enumerate(zip(h4, h5)):
    if a: grupo = a
    cols.append(f"{grupo}|{b or ''}")
print(" ; ".join(f"{i+1}:{c}" for i, c in enumerate(cols)))
alvos = sys.argv[2:]
for r in ws.iter_rows(min_row=6, values_only=True):
    est = next((v for v in r if isinstance(v, str) and "+" in v), "")
    if any(est.startswith(a) for a in alvos):
        print(est, " ; ".join(f"{cols[i].split('|')[0][:10]}/{cols[i].split('|')[1][:9]}={v}" for i, v in enumerate(r) if v is not None and not (isinstance(v, str) and "+" in v)))
o = wb.worksheets[1]
for row in o.iter_rows(min_row=3, values_only=True):
    if row[0] and ("Terrapl" in row[0] or "Aviso" in row[0] or "Banq" in row[0] or "Offset" in row[0]): print(row[0], "=", row[1])
