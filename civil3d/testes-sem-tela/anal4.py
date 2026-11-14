import csv, sys, collections
arq = r"C:\Temp\nsteste\dump_teste.csv"
rows = list(csv.DictReader(open(arq, encoding="utf-8"), delimiter=";"))
print("pontos", len(rows))
cb = collections.Counter((r["corredor"], r["baseline"]) for r in rows)
for k, v in cb.items(): print("corredor/baseline", k, v)
reg = collections.Counter((r["corredor"], r["regiao"], r["assembly"]) for r in rows)
for k, v in list(reg.items())[:30]: print("  regiao", k, v)
# cÃ³digos por lado
lado = collections.defaultdict(collections.Counter)
for r in rows:
    off = float(r["afast"]); s = "E" if off < -0.001 else ("D" if off > 0.001 else "0")
    for c in (r["codigos"] or "(sem)").split("|"):
        lado[c][s] += 1
print("--- codigos (E, 0, D)")
for c, v in sorted(lado.items()):
    print(f"  {c:28s} E={v['E']:7d} 0={v['0']:6d} D={v['D']:7d}")

