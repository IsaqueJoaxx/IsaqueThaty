import json,uuid,copy,os,ast
ROOT=os.path.dirname(__file__)
src=json.load(open(r'D:\PROJETA ENGENHARIA\Rotina_Nota_Servico\COGO_Points_Nota_Excel.dyn',encoding='utf-8-sig'))
uid=lambda:uuid.uuid4().hex
code=open(os.path.join(ROOT,'compactacao.py'),encoding='utf-8').read()+'\n'+open(os.path.join(ROOT,'dynamo_adapter.py'),encoding='utf-8').read()
ast.parse(code)
open(os.path.join(ROOT,'Compactacao_Dynamo.py'),'w',encoding='utf-8').write(code)
py=copy.deepcopy(next(n for n in src['Nodes'] if n['NodeType']=='PythonScriptNode'))
py['Id']=uid();py['Code']=code;py['Engine']='PythonNet3';py['Inputs']=py['Inputs'][:6]
for i,port in enumerate(py['Inputs']):port['Id']=uid();port['Name']='IN[%s]'%i
py['Outputs'][0]['Id']=uid()
graph=copy.deepcopy(src);graph['Uuid']=str(uuid.uuid4());graph['Name']='Compactacao_95_100_GO220';graph['Description']='Divide corte e aterro entre 100% (primeiro metro) e 95% (excedente), por secao.'
graph['Nodes']=[];graph['Connectors']=[];graph['Inputs']=[];graph['Outputs']=[];graph['Bindings']=[]
graph['View']['NodeViews']=[];graph['View']['Annotations']=[];graph['View']['X']=0;graph['View']['Y']=0;graph['View']['Zoom']=.65
graph['View']['Dynamo']['RunType']='Manual'
spec=[('BooleanInputNode',True,'1. Executar calculo'),('BooleanInputNode',False,'2. Aplicar preenchimentos e tabelas'),('StringInputNode','SEÇÕES - GO-220 Executivo R01 (ETAPA 1)','3. Grupo de secoes'),('StringInputNode','GO-220_Prim_Natural Caiapo','4. Terreno natural'),('StringInputNode','GO-220 Executivo R01 - TOP','5. Projeto'),('NumberInputNode',1.0,'6. Limite em metros')]
for i,(kind,val,label) in enumerate(spec):
    node=copy.deepcopy(next(n for n in src['Nodes'] if n['NodeType']==kind));node['Id']=uid();node['Outputs'][0]['Id']=uid();node['InputValue']=val
    graph['Nodes'].append(node)
    graph['Connectors'].append({'Start':node['Outputs'][0]['Id'],'End':py['Inputs'][i]['Id'],'Id':uid(),'IsHidden':'False'})
    graph['View']['NodeViews'].append({'Id':node['Id'],'Name':label,'IsSetAsInput':True,'IsSetAsOutput':False,'Excluded':False,'ShowGeometry':False,'X':0.0,'Y':i*130.0})
    graph['Inputs'].append({'Id':node['Id'],'Name':label,'Type':{'BooleanInputNode':'boolean','StringInputNode':'string','NumberInputNode':'number'}[kind],'Type2':{'BooleanInputNode':'boolean','StringInputNode':'string','NumberInputNode':'number'}[kind],'Value':str(val).lower() if isinstance(val,bool) else str(val),'Description':label})
graph['Nodes'].append(py);graph['View']['NodeViews'].append({'Id':py['Id'],'Name':'Calcular e aplicar compactacao','IsSetAsInput':False,'IsSetAsOutput':True,'Excluded':False,'ShowGeometry':False,'X':650.0,'Y':200.0})
json.dump(graph,open(os.path.join(ROOT,'Compactacao_95_100_GO220.dyn'),'w',encoding='utf-8'),ensure_ascii=False,indent=2)
print('Rotina Python e grafo Dynamo gerados; sintaxe Python conferida.')
