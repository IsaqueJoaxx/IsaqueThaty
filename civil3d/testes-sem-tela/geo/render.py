import fitz, sys
doc = fitz.open(sys.argv[1])
p = doc[0]
p.get_pixmap(dpi=int(sys.argv[3]) if len(sys.argv) > 3 else 120).save(sys.argv[2])
print(p.rect)
