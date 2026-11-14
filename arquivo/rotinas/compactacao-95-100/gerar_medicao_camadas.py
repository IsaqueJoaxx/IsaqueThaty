import os,json,csv
from openpyxl import Workbook
from openpyxl.styles import Font,PatternFill,Alignment
ROOT=os.path.dirname(__file__)
earth=json.load(open(os.path.join(ROOT,'medicao_fundo_plataforma.json'),encoding='utf-8'))
wb=Workbook();ws=wb.active;ws.title='Premissas'
notes=[['MEDICAO GO-220 - PLATAFORMA'],
['Trechos','Estacas 0 a 300 e 350 a 400; estacas de 20 m. Intervalo 300 a 350 excluido.'],
['Largura horizontal',13.4],['Espessura vertical total considerada',1.0],
['Base (m)',0.2],['Sub-base (m)',0.2],['Reforco (m)',0.4],['Subleito (m)',0.2],
['Referencia','Terraplanagem entre terreno natural e TOP -1,00 m, somente entre offsets -6,70 e +6,70 m.'],
['Limite','Taludes fora da plataforma nao incluidos nem redesenhados.'],
['Revestimento','Nao foi acrescentada espessura de asfalto. Se TOP for a superficie acabada do asfalto e este ficar acima do pacote de 1 m, a cota de fundo precisa de revisao.'],
['Camadas','Volume geometrico = largura horizontal x comprimento x espessura vertical; sem fatores volumetricos.'],
['Posicao do subleito','Faixa inferior de 20 cm do pacote de 1 m, conforme interpretacao registrada.'],
['Compactacao','95% e 100% sao categorias solicitadas, nao fatores de multiplicacao do volume.'],
['Volumes de terraplanagem','Media das areas das secoes existentes multiplicada pela distancia entre progressivas.'],
['Comparacao','O DWG entregue anteriormente usa TOP, sem descontar o pacote. Nao representa esta revisao.'],
['Nao somar','Volumes das camadas e da terraplanagem representam servicos diferentes. Nao somar como um unico movimento de terra.']]
for row in notes:ws.append(row)
ws.column_dimensions['A'].width=32;ws.column_dimensions['B'].width=110
for row in ws:
    for cell in row:cell.alignment=Alignment(vertical='top',wrap_text=True)
for i in range(9,18):ws.row_dimensions[i].height=42

te=wb.create_sheet('Terraplanagem')
te.append(['Trecho','Corte 95% (m3)','Corte 100% (m3)','Aterro 95% (m3)','Aterro 100% (m3)'])
cats=['Corte 95%','Corte 100%','Aterro 95%','Aterro 100%']
for r in earth['trechos']:te.append([r['trecho']]+[r['volumes_m3'][c] for c in cats])
te.append(['TOTAL']+['=SUM(%s2:%s3)'%(c,c) for c in 'BCDE'])

cam=wb.create_sheet('Camadas')
cam.append(['Trecho','Comprimento (m)','Largura (m)','Area em planta (m2)','Base 0,20 m (m3)','Sub-base 0,20 m (m3)','Reforco 0,40 m (m3)','Subleito 0,20 m (m3)'])
for i,(label,length) in enumerate([('0 a 300',6000),('350 a 400',1000)],2):
    cam.append([label,length,13.4,'=B%d*C%d'%(i,i),'=D%d*0.2'%i,'=D%d*0.2'%i,'=D%d*0.4'%i,'=D%d*0.2'%i])
cam.append(['TOTAL']+['=SUM(%s2:%s3)'%(c,c) for c in 'BCDEFGH'])
cam['C4']=13.4
sub=wb.create_sheet('Subleito')
sub.append(['Trecho','Comprimento (m)','Largura (m)','Espessura (m)','Area em planta (m2)','Volume (m3)'])
for i,(label,length) in enumerate([('0 a 300',6000),('350 a 400',1000)],2):
    sub.append([label,length,13.4,.2,'=B%d*C%d'%(i,i),'=E%d*D%d'%(i,i)])
sub.append(['TOTAL','=SUM(B2:B3)',13.4,.2,'=SUM(E2:E3)','=SUM(F2:F3)'])
det=wb.create_sheet('Secoes terraplanagem')
with open(os.path.join(ROOT,'medicao_fundo_plataforma_secoes.csv'),encoding='utf-8-sig') as f:
    for i,row in enumerate(csv.reader(f,delimiter=';')):
        det.append(row if i==0 else row[:1]+[float(v.replace(',','.')) for v in row[1:]])
for sheet in wb:
    sheet.freeze_panes='B2'
    if sheet.title!='Premissas':
        sheet.auto_filter.ref=sheet.dimensions
        for col in sheet.columns:
            sheet.column_dimensions[col[0].column_letter].width=25
    for cell in sheet[1]:
        cell.fill=PatternFill('solid',fgColor='164B70');cell.font=Font(color='FFFFFF',bold=True);cell.alignment=Alignment(wrap_text=True,vertical='center')
    sheet.row_dimensions[1].height=36
    for row in sheet.iter_rows(min_row=2):
        for cell in row:
            if cell.data_type=='n' or cell.data_type=='f':cell.number_format='#,##0.000'
wb.save(os.path.join(ROOT,'Medicao_GO220_0a300_350a400.xlsx'))
out={'trechos':[{'trecho':label,'comprimento_m':length,'largura_m':13.4,'subleito_m3':length*13.4*.2,'area_planta_m2':length*13.4,'base_m3':length*13.4*.2,'subbase_m3':length*13.4*.2,'reforco_m3':length*13.4*.4} for label,length in [('0 a 300',6000),('350 a 400',1000)]]}
json.dump(out,open(os.path.join(ROOT,'medicao_camadas.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=2)
print(json.dumps(out,ensure_ascii=False,indent=2))

