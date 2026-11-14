from pathlib import Path
import re, html
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, KeepTogether
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
import pypdfium2 as pdfium
from PIL import Image,ImageOps,ImageDraw

root=Path(__file__).parent
source=(root/'MANUAL-PRJTERRA.md').read_text(encoding='utf-8')
pdfmetrics.registerFont(TTFont('ArialIFM',r'C:\Windows\Fonts\arial.ttf'))
pdfmetrics.registerFont(TTFont('ArialIFM-Bold',r'C:\Windows\Fonts\arialbd.ttf'))
pdfmetrics.registerFontFamily('ArialIFM',normal='ArialIFM',bold='ArialIFM-Bold',italic='ArialIFM',boldItalic='ArialIFM-Bold')
styles=getSampleStyleSheet()
styles.add(ParagraphStyle(name='BodyIFM',fontName='ArialIFM',fontSize=10.5,leading=14.3,spaceAfter=8,textColor=colors.HexColor('#243340')))
styles.add(ParagraphStyle(name='TitleIFM',parent=styles['BodyIFM'],fontName='ArialIFM-Bold',fontSize=23,leading=27,spaceAfter=14,textColor=colors.HexColor('#123c45')))
styles.add(ParagraphStyle(name='H2IFM',parent=styles['BodyIFM'],fontName='ArialIFM-Bold',fontSize=17,leading=21,spaceBefore=8,spaceAfter=12,keepWithNext=True,textColor=colors.HexColor('#123c45')))
styles.add(ParagraphStyle(name='H3IFM',parent=styles['BodyIFM'],fontName='ArialIFM-Bold',fontSize=12,leading=16,spaceBefore=9,spaceAfter=6,keepWithNext=True))
styles.add(ParagraphStyle(name='CellIFM',parent=styles['BodyIFM'],fontSize=9.1,leading=12,spaceAfter=0))
styles.add(ParagraphStyle(name='ListIFM',parent=styles['BodyIFM'],leftIndent=15,firstLineIndent=-15,spaceAfter=6))
def fmt(s):
    s=html.escape(s)
    s=re.sub(r'\*\*(.*?)\*\*',r'<b>\1</b>',s)
    s=re.sub(r'`(.*?)`',r'<font name="ArialIFM-Bold">\1</font>',s)
    return s

class ManualDoc(SimpleDocTemplate):
    def afterFlowable(self,flowable):
        if isinstance(flowable,Paragraph) and flowable.style.name in ['TitleIFM','H2IFM']:
            txt=flowable.getPlainText()
            key='bookmark'+str(self.seq.nextf('bookmark'))
            self.canv.bookmarkPage(key)
            self.canv.addOutlineEntry(txt,key,level=0,closed=False)

def footer(canvas,doc):
    canvas.saveState()
    canvas.setFont('ArialIFM',8)
    canvas.setFillColor(colors.HexColor('#55666e'))
    canvas.drawString(43, A4[1]-29,'DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM | Manual do PRJTERRA')
    canvas.drawString(43,26,'Versão 0.1.0 • 15/10/2026')
    canvas.drawRightString(A4[0]-43,26,str(doc.page))
    canvas.restoreState()

story=[];web=[];toc=[];chapter=0
lines=source.splitlines();i=0
while i<len(lines):
    line=lines[i].strip()
    if not line:i+=1;continue
    if line=='<!-- page -->':
        story.append(PageBreak());web.append('</section><section>');i+=1;continue
    if line.startswith('|'):
        rows=[]
        while i<len(lines) and lines[i].strip().startswith('|'):
            cells=[c.strip() for c in lines[i].strip().strip('|').split('|')]
            if not all(re.fullmatch(r':?-+:?',c) for c in cells):rows.append(cells)
            i+=1
        cols=len(rows[0]); width=A4[0]-86
        widths=[width*.29,width*.28,width*.43] if cols==3 else [width*.40,width*.60]
        data=[[Paragraph(fmt(c),styles['CellIFM']) for c in row] for row in rows]
        table=Table(data,colWidths=widths,repeatRows=1,hAlign='LEFT')
        table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#e7f0f0')),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),6),('BOTTOMPADDING',(0,0),(-1,-1),6),('LINEBELOW',(0,0),(-1,0),.6,colors.HexColor('#7c9c9f')),('LINEBELOW',(0,1),(-1,-1),.3,colors.HexColor('#d8e1e3'))]))
        story.extend([table,Spacer(1,10)])
        web.append('<table>'+''.join('<tr>'+''.join(f'<{"th" if j==0 else "td"}>{fmt(c)}</{"th" if j==0 else "td"}>' for c in row)+'</tr>' for j,row in enumerate(rows))+'</table>')
        continue
    if line.startswith('# '):
        text=line[2:];story.append(Paragraph(fmt(text),styles['TitleIFM']));web.append('<h1>'+fmt(text)+'</h1>')
    elif line.startswith('## '):
        text=line[3:];story.append(Paragraph(fmt(text),styles['H2IFM']));chapter+=1;key=f's{chapter}';web.append(f'<h2 id="{key}">'+fmt(text)+'</h2>');toc.append((key,text))
    elif line.startswith('### '):
        text=line[4:];story.append(Paragraph(fmt(text),styles['H3IFM']));web.append('<h3>'+fmt(text)+'</h3>')
    elif re.match(r'^\d+\. ',line):
        story.append(Paragraph(fmt(line),styles['ListIFM']));web.append('<p class="step">'+fmt(line)+'</p>')
    else:
        story.append(Paragraph(fmt(line),styles['BodyIFM']));web.append('<p>'+fmt(line)+'</p>')
    i+=1

pdfpath=root/'Manual_PRJTERRA_IFM.pdf'
doc=ManualDoc(str(pdfpath),pagesize=A4,rightMargin=43,leftMargin=43,topMargin=47,bottomMargin=47,title='Manual de uso do PRJTERRA — DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM',author='PROJETA ENGENHARIA')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
css='''body{margin:0;background:#f2f5f5;color:#243340;font:16px/1.65 "Segoe UI",Arial,sans-serif}nav{position:fixed;left:0;top:0;bottom:0;width:245px;overflow:auto;background:#123c45;color:white;padding:22px;box-sizing:border-box}nav strong{display:block;margin-bottom:20px}nav a{display:block;color:#e2eeee;text-decoration:none;font-size:13px;margin:10px 0}main{max-width:900px;margin-left:270px;padding:35px}section{background:white;padding:30px 40px;margin-bottom:24px;border-radius:6px}h1{font-size:32px;line-height:1.2}h1,h2{color:#123c45}h2{scroll-margin-top:20px}h3{font-size:18px;margin-top:24px}table{width:100%;border-collapse:collapse;margin:20px 0;font-size:14px}td,th{padding:10px;text-align:left;vertical-align:top;border-bottom:1px solid #d8e1e3}th{background:#e7f0f0}.step{padding-left:20px;text-indent:-20px}button{padding:10px 18px;margin-bottom:20px;cursor:pointer}@media(max-width:900px){nav{position:static;width:auto;max-height:260px}main{margin:0;padding:10px}section{padding:22px;overflow:auto}}@media print{nav,button{display:none}body{background:white;font-size:10.5pt}main{margin:0;padding:0;max-width:none}section{padding:0;break-after:page;border-radius:0}table{font-size:9pt}h2,h3{break-after:avoid}}'''
htmlpath=root/'Manual_PRJTERRA_IFM.html'
htmlpath.write_text('<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Manual PRJTERRA — IFM</title><style>'+css+'</style><nav><strong>DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM</strong>'+''.join(f'<a href="#{k}">{html.escape(t)}</a>' for k,t in toc)+'</nav><main><button onclick="window.print()">Imprimir manual</button><section>'+''.join(web)+'</section></main></html>',encoding='utf-8')
qa=root/'manual_qa';qa.mkdir(exist_ok=True)
pdf=pdfium.PdfDocument(str(pdfpath));thumbs=[]
for n in range(len(pdf)):
    page=pdf[n];im=page.render(scale=1.2).to_pil();im.save(qa/f'pagina-{n+1:02}.png')
    small=im.copy();small.thumbnail((300,425));tile=Image.new('RGB',(320,455),'#ccd4d7');tile.paste(small,((320-small.width)//2,8));ImageDraw.Draw(tile).text((12,435),f'Página {n+1}',fill='black');thumbs.append(tile)
contact=Image.new('RGB',(320*4,455*((len(thumbs)+3)//4)),'white')
for n,im in enumerate(thumbs):contact.paste(im,((n%4)*320,(n//4)*455))
contact.save(qa/'contato.png')
print(f'PDF: {len(pdf)} páginas. HTML: {htmlpath}.')
