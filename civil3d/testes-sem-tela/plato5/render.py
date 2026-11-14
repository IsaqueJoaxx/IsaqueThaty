import fitz, sys
doc = fitz.open(r"C:\Temp\nsteste\plato5\previa.pdf")
p = doc[0]
r = p.rect
p.get_pixmap(dpi=110).save(r"C:\Temp\nsteste\plato5\previa.png")
# recortes: centro (platô 1) e noroeste (platô 2), em fração da página
def corte(nome, fx0, fy0, fx1, fy1, dpi):
    clip = fitz.Rect(r.x0 + r.width * fx0, r.y0 + r.height * fy0, r.x0 + r.width * fx1, r.y0 + r.height * fy1)
    p.get_pixmap(dpi=dpi, clip=clip).save(nome)
corte(r"C:\Temp\nsteste\plato5\zoom1.png", 0.30, 0.22, 0.70, 0.78, 260)
corte(r"C:\Temp\nsteste\plato5\zoom2.png", 0.02, 0.02, 0.30, 0.40, 330)
print(r)
