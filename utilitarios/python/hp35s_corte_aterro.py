import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side

wb = openpyxl.Workbook()
ws = wb.active
ws.title = 'Corte e Aterro'

step_font   = Font(name='Arial', size=10, bold=True, color='00008B')
cmd_font    = Font(name='Arial', size=10)
note_font   = Font(name='Arial', size=9, italic=True)
corte_fill  = PatternFill('solid', start_color='FCE4D6')
aterro_fill = PatternFill('solid', start_color='E2EFDA')
result_fill = PatternFill('solid', start_color='FFF2CC')
thin = Side(style='thin')
border = Border(left=thin, right=thin, top=thin, bottom=thin)

ws.merge_cells('A1:G1')
ws['A1'] = 'CORTE E ATERRO COM ATUALIZACAO DE COTA DE PROJETO'
ws['A1'].font = Font(name='Arial', bold=True, size=13)
ws['A1'].alignment = Alignment(horizontal='center')

ws.merge_cells('A2:G2')
ws['A2'] = '(Calculadora HP 35S)  -  Label: F'
ws['A2'].font = Font(name='Arial', size=11, italic=True)
ws['A2'].alignment = Alignment(horizontal='center')

ws['A3'] = 'GTO .. PRGM'
ws['A3'].font = Font(name='Arial', bold=True, size=10)

ws.column_dimensions['A'].width = 8
ws.column_dimensions['B'].width = 18
ws.column_dimensions['C'].width = 8
ws.column_dimensions['D'].width = 18
ws.column_dimensions['E'].width = 2
ws.column_dimensions['F'].width = 38
ws.column_dimensions['G'].width = 22

headers = [('A','Passo'),('B','Instrucao'),('C','Passo'),('D','Instrucao'),('F','Descricao'),('G','Tecla / Resultado')]
for col, h in headers:
    cell = ws[col + '4']
    cell.value = h
    cell.font = Font(name='Arial', bold=True, size=10, color='FFFFFF')
    cell.fill = PatternFill('solid', start_color='4472C4')
    cell.alignment = Alignment(horizontal='center')
    cell.border = border

# (step1, cmd1, step2, cmd2, fill_key, description, result)
steps = [
    ('F001','LBL F',     'F022','VIEW M',      'corte_fill',  'M = Dist. do Offset (talude)',           'Visualiza'),
    ('F002','INPUT T',   'F023','INPUT D',      None,          'T ? Cota do Terreno',                   'R/S'),
    ('F003','INPUT P',   'F024','INPUT I',      None,          'D ? Distancia ao proximo ponto',         'R/S'),
    ('F004','RCL T',     'F025','RCL D',        None,          'I ? Inclinacao (%)',                     'R/S'),
    ('F005','RCL - P',   'F026','RCL x I',      None,          '',                                      ''),
    ('F006','STO C',     'F027','100',           None,          'C = T - P  (+corte / -aterro)',          ''),
    ('F007','X < 0?',    'F028','/',             None,          'Se aterro (C<0): pula F008',             ''),
    ('F008','GTO F017',  'F029','STO V',         None,          'Corte: vai para F017',                  ''),
    ('F009','RCL C',     'F030','INPUT B',      'aterro_fill', 'V = DeltaCota = D x I / 100',           ''),
    ('F010','+/-',       'F031','2',            'aterro_fill', 'B ? 1=Descida  /  2=Subida',            'R/S'),
    ('F011','STO C',     'F032','X != Y?',      'aterro_fill', 'Se B != 2 (Descida): pula F033',        ''),
    ('F012','VIEW C',    'F033','GTO F038',     'aterro_fill', 'B=2 (Subida): vai F038',                ''),
    ('F013','RCL C',     'F034','RCL P',        None,          'C = Altura do Aterro',                  'Visualiza'),
    ('F014','1.5',       'F035','RCL - V',      None,          '--- DESCIDA: P = P - V ---',            ''),
    ('F015','x',         'F036','STO P',        None,          '',                                      ''),
    ('F016','GTO F021',  'F037','GTO F041',     None,          'Offset Aterro = C x 1,5',               ''),
    ('F017','VIEW C',    'F038','RCL P',        'corte_fill',  '--- SUBIDA: P = P + V ---',             ''),
    ('F018','RCL C',     'F039','RCL + V',      'corte_fill',  'C = Altura do Corte',                   'Visualiza'),
    ('F019','1',         'F040','STO P',        'corte_fill',  '',                                      ''),
    ('F020','x',         'F041','VIEW P',       'result_fill', 'Offset Corte = C x 1',                  ''),
    ('F021','STO M',     'F042','GTO F001',     'result_fill', 'P = Nova Cota do Projeto',              'Visualiza'),
]

fills = {'corte_fill': corte_fill, 'aterro_fill': aterro_fill, 'result_fill': result_fill}

for i, (s1,c1,s2,c2,fk,desc,res) in enumerate(steps):
    row = i + 5
    f = fills.get(fk)
    for col, val in [(1,s1),(2,c1),(3,s2),(4,c2)]:
        cell = ws.cell(row=row, column=col, value=val)
        cell.font = step_font if col in (1,3) else cmd_font
        cell.border = border
        if f:
            cell.fill = f
        cell.alignment = Alignment(horizontal='center' if col in (1,3) else 'left')
    ws.cell(row=row, column=6, value=desc).font = note_font
    ws.cell(row=row, column=7, value=res).font = note_font

lr = len(steps) + 7
ws.merge_cells(f'A{lr}:G{lr}')
ws.cell(row=lr, column=1).value = 'LEGENDA DE ENTRADA'
ws.cell(row=lr, column=1).font = Font(name='Arial', bold=True, size=10)

entries = [
    ('T ?', 'Cota do terreno no ponto atual'),
    ('P ?', 'Cota do projeto (1a vez: digitar; depois pressionar R/S para aceitar o valor atualizado)'),
    ('D ?', 'Distancia horizontal ate o proximo ponto (m)'),
    ('I ?', 'Inclinacao/Declividade (%)  -  sem sinal, apenas o valor numerico'),
    ('B ?', '1 = Descida (subtrai DeltaCota do projeto)    |    2 = Subida (soma DeltaCota ao projeto)'),
]
for j, (key, desc) in enumerate(entries):
    r = lr + 1 + j
    ws.cell(row=r, column=1, value=key).font = Font(name='Arial', bold=True, size=9)
    ws.merge_cells(f'B{r}:G{r}')
    ws.cell(row=r, column=2, value=desc).font = Font(name='Arial', size=9)

rr = lr + len(entries) + 2
ws.merge_cells(f'A{rr}:G{rr}')
ws.cell(row=rr, column=1).value = 'RESULTADOS'
ws.cell(row=rr, column=1).font = Font(name='Arial', bold=True, size=10)
results = [
    ('C =', 'Altura de corte (+) ou aterro (valor exibido positivo) em metros'),
    ('M =', 'Distancia do offset do talude (m): corte = C x 1  |  aterro = C x 1,5'),
    ('P =', 'Nova cota do projeto para o proximo ponto (atualizada automaticamente)'),
]
for j, (key, desc) in enumerate(results):
    r = rr + 1 + j
    ws.cell(row=r, column=1, value=key).font = Font(name='Arial', bold=True, size=9)
    ws.merge_cells(f'B{r}:G{r}')
    ws.cell(row=r, column=2, value=desc).font = Font(name='Arial', size=9)

nr = rr + len(results) + 2
ws.merge_cells(f'A{nr}:G{nr}')
ws.cell(row=nr, column=1).value = (
    'OBS: Iniciar com XEQ F ENTER. A cada iteracao o programa retorna ao inicio com P ja atualizado. '
    'Na 1a vez digite P; nas seguintes pressione R/S para aceitar o valor calculado ou sobrescreva se necessario.'
)
ws.cell(row=nr, column=1).font = Font(name='Arial', size=9, italic=True)

out = r'C:\Users\User\Downloads\HP35S_Corte_Aterro.xlsx'
wb.save(out)
print('Salvo:', out)
