import csv, collections, sys
arq = r"C:\Temp\nsteste\dump_teste.csv"
sec = collections.defaultdict(list)
for r in csv.DictReader(open(arq, encoding="utf-8"), delimiter=";"):
    sec[float(r["estaca"])].append((int(r["i"]), r["codigos"], float(r["afast"]), float(r["cota"])))
def mostra(e, lado):
    pts = sec[e]
    sel = [p for p in pts if (p[2] > 0.001 if lado == "D" else p[2] < -0.001)]
    sel.sort(key=lambda p: abs(p[2]))
    print(f"== estaca {e} lado {lado} ({len(sel)} pontos; total na seÃ§Ã£o {len(pts)})")
    for i, c, a, z in sel:
        print(f"   i={i:3d} afast={a:9.3f} cota={z:9.3f}  {c}")
alvo = sys.argv[1:]
achou = {}
for e, pts in sorted(sec.items()):
    cods = set(c for p in pts for c in p[1].split("|"))
    for t in alvo:
        if t in cods and t not in achou: achou[t] = e
print(achou)
for t, e in achou.items():
    mostra(e, "D" if "DIREITO" in t else "E")

