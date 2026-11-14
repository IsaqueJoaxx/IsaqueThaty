"""Geometria em metros. Categorias de compactacao, sem fator volumetrico.
Entrada: segmentos [(offset0, cota0, offset1, cota1), ...].
100%: primeiro metro abaixo da envoltoria superior entre as superficies.
95%: excedente. Corte: natural acima do projeto; aterro: projeto acima.
"""
import math

CATEGORIAS = ('Corte 95%', 'Corte 100%', 'Aterro 95%', 'Aterro 100%')

def segmentos(pontos):
    result = []
    for i, p in enumerate(pontos):
        j = int(p['to'])
        if j < 0:
            continue
        if j >= len(pontos) or j == i:
            raise ValueError('Segmento de secao invalido')
        q = pontos[j]
        a, b = (float(p['x']), float(p['y'])), (float(q['x']), float(q['y']))
        if not all(math.isfinite(v) for v in a + b):
            raise ValueError('Coordenada nao finita')
        if b[0] < a[0]:
            a, b = b, a
        if b[0] - a[0] > 1e-9:
            result.append(a + b)
    result.sort()
    for a, b in zip(result, result[1:]):
        if a[2] > b[0] + 1e-7:
            raise ValueError('Secao com segmentos sobrepostos')
    return result

def cota(seg, x):
    return seg[1] + (seg[3]-seg[1])*(x-seg[0])/(seg[2]-seg[0])

def separar(natural, projeto, limite=1.0):
    if not math.isfinite(limite) or limite <= 0:
        raise ValueError('Limite deve ser positivo')
    areas = [0.0]*4
    poligonos = []
    largura = 0.0
    for n in natural:
        for p in projeto:
            a, b = max(n[0], p[0]), min(n[2], p[2])
            if b-a <= 1e-9:
                continue
            largura += b-a
            da, db = cota(p,a)-cota(n,a), cota(p,b)-cota(n,b)
            xs = [a,b]
            if abs(db-da)>1e-12:
                for h in (-limite,0.0,limite):
                    t=(h-da)/(db-da)
                    if 1e-10<t<1-1e-10:
                        xs.append(a+(b-a)*t)
            xs.sort()
            for x,y in zip(xs,xs[1:]):
                nn=[cota(n,x),cota(n,y)]
                pp=[cota(p,x),cota(p,y)]
                fill=sum(pp)>sum(nn)
                low=nn if fill else pp
                high=pp if fill else nn
                split=[max(low[k],high[k]-limite) for k in (0,1)]
                for pct,bottom,top in ((95,low,split),(100,split,high)):
                    area=(y-x)*sum(top[k]-bottom[k] for k in (0,1))/2
                    if area<=1e-10:
                        continue
                    idx=(2 if fill else 0)+(1 if pct==100 else 0)
                    areas[idx]+=area
                    poligonos.append({'categoria':idx,'pontos':[[x,bottom[0]],[y,bottom[1]],[y,top[1]],[x,top[0]]]})
    if largura<=1e-8:
        raise ValueError('Superficies sem sobreposicao na secao')
    return areas,poligonos

def volumes(registros):
    """Media das areas nas extremidades; nunca liga grupos diferentes ou falhas."""
    totals=[0.0]*4
    for prev,cur in zip(registros,registros[1:]):
        ds=cur['station']-prev['station']
        if ds<=0:
            raise ValueError('Progressivas devem ser estritamente crescentes')
        v=[(a+b)*ds/2 for a,b in zip(prev['areas'],cur['areas'])]
        cur['volumes_intervalo']=v
        totals=[a+b for a,b in zip(totals,v)]
    return totals

# Este trecho e anexado ao motor geometrico compactacao.py no no Python.
# IN[0] executar; IN[1] aplicar preenchimentos/tabelas; IN[2] grupo;
# IN[3] terreno; IN[4] projeto; IN[5] limite (m).
# Desenho em metros. Nao salva o DWG automaticamente.
def executar_dynamo(entradas):
    if not entradas[0]:
        return 'Ative Executar. Use modo Manual no Dynamo.'
    import clr
    for lib in ('AcMgd','AcDbMgd','AeccDbMgd'):
        clr.AddReference(lib)
    from Autodesk.AutoCAD.ApplicationServices import Application
    from Autodesk.AutoCAD.DatabaseServices import OpenMode,BlockTableRecord,LayerTableRecord,Solid,Table,CellAlignment,CellRange
    from Autodesk.AutoCAD.Geometry import Point3d
    from Autodesk.AutoCAD.Colors import Color,ColorMethod
    from System import Int16
    doc=Application.DocumentManager.MdiActiveDocument
    if doc is None:
        raise ValueError('Abra o DWG no Civil 3D.')
    aplicar=bool(entradas[1])
    grupo,nome_n,nome_p=map(str,entradas[2:5])
    limite=float(entradas[5])
    db=doc.Database
    lock=doc.LockDocument()
    tr=db.TransactionManager.StartTransaction()
    try:
        bt=tr.GetObject(db.BlockTableId,OpenMode.ForRead)
        ms=tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForRead)
        entities=[tr.GetObject(oid,OpenMode.ForRead) for oid in ms]
        def localizar(tipo,nome):
            matches=[e for e in entities if e.GetType().Name==tipo and e.Name==nome]
            if len(matches)!=1:
                raise ValueError('Esperado um '+tipo+' com nome: '+nome)
            return matches[0]
        n=localizar('TinSurface',nome_n)
        p=localizar('TinSurface',nome_p)
        g=localizar('SampleLineGroup',grupo)
        records=[]
        view_records=[]
        for sid in g.GetSampleLineIds():
            sl=tr.GetObject(sid,OpenMode.ForRead)
            def ler(surface):
                secid=sl.GetSectionId(surface.ObjectId)
                if secid.IsNull:
                    raise ValueError('Superficie nao amostrada em '+sl.Name)
                sec=tr.GetObject(secid,OpenMode.ForRead)
                pts=[{'x':float(pt.Location.X),'y':float(pt.Location.Y),'to':int(pt.SegmentTo)} for pt in sec.SectionPoints]
                return segmentos(pts)
            a,poly=separar(ler(n),ler(p),limite)
            r={'name':sl.Name,'station':float(sl.Station),'areas':a,'poligonos':poly}
            records.append(r)
            for vid in sl.GetSectionViewIds():
                view_records.append((tr.GetObject(vid,OpenMode.ForRead),r))
        records.sort(key=lambda r:r['station'])
        total=volumes(records)
        criados=0
        if aplicar:
            # Camadas exclusivas desta rotina. Reexecucao substitui a saida anterior.
            owned=('COMP_95','COMP_100','COMP_TABELA')
            lt=tr.GetObject(db.LayerTableId,OpenMode.ForWrite)
            for name,index in zip(owned,(30,5,7)):
                if not lt.Has(name):
                    layer=LayerTableRecord();layer.Name=name
                    layer.Color=Color.FromColorIndex(ColorMethod.ByAci,Int16(index))
                    lt.Add(layer);tr.AddNewlyCreatedDBObject(layer,True)
            for e in entities:
                if e.Layer in owned:
                    e.UpgradeOpen();e.Erase()
            ms.UpgradeOpen()
            def add(e):
                ms.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,True)
            def xy(view,x,z):
                result=view.FindXYAtOffsetAndElevation(float(x),float(z),0.0,0.0)
                if not isinstance(result,tuple) or len(result)!=3 or not result[0]:
                    raise ValueError('Nao foi possivel localizar ponto na vista '+view.Name)
                return Point3d(float(result[1]),float(result[2]),0.0)
            for view,r in view_records:
                for poly in r['poligonos']:
                    pts=[]
                    for x,z in poly['pontos']:
                        if x<view.OffsetLeft-1e-6 or x>view.OffsetRight+1e-6 or z<view.ElevationMin-1e-6 or z>view.ElevationMax+1e-6:
                            raise ValueError('Vista recorta a secao: '+view.Name)
                        pts.append(xy(view,max(view.OffsetLeft,min(x,view.OffsetRight)),max(view.ElevationMin,min(z,view.ElevationMax))))
                    e=Solid(pts[0],pts[1],pts[3],pts[2]);e.Layer=owned[0] if poly['categoria']%2==0 else owned[1]
                    add(e);criados+=1
                top=xy(view,view.OffsetRight,view.ElevationMax)
                bottom=xy(view,view.OffsetLeft,view.ElevationMin)
                width=abs(top.X-bottom.X);row=max(.18,width/55);tw=max(3,width*.23)
                t=Table();t.SetDatabaseDefaults(db);t.TableStyle=db.Tablestyle
                t.SetSize(6,2);t.UnmergeCells(CellRange.Create(t,0,0,0,1));t.SetRowHeight(row);t.SetColumnWidth(tw/2)
                t.Position=Point3d(top.X+width*.035,top.Y,0);t.Layer=owned[2]
                for i in range(6):
                    for j in range(2):
                        t.Cells[i,j].TextHeight=row*.65
                        t.Cells[i,j].Alignment=CellAlignment.MiddleCenter
                        t.Cells[i,j].ContentColor=Color.FromColorIndex(ColorMethod.ByAci,Int16(7))
                t.Cells[0,0].TextString='Estaca';t.Cells[0,1].TextString=('{:.3f}'.format(r['station']/20).rstrip('0').rstrip('.')).replace('.',',')
                t.MergeCells(CellRange.Create(t,1,0,1,1));t.Cells[1,0].TextString='Área (m²)'
                for i,name in enumerate(CATEGORIAS):
                    t.Cells[i+2,0].TextString=name;t.Cells[i+2,1].TextString=('{:.2f}'.format(r['areas'][i])).replace('.',',')
                t.GenerateLayout();add(t)
            # As tabelas nativas continuam com seus materiais originais.
            # No DWG entregue elas estao ocultas. Nao altera listas QTO.
        tr.Commit()
        return {'Secoes':len(records),'Categorias':list(CATEGORIAS),'Totais m3':total,
                'Areas por secao':[[r['name'],r['station']]+r['areas'] for r in records],
                'Volumes entre secoes':[[r['name']]+r.get('volumes_intervalo',[0.0]*4) for r in records],
                'Solidos criados':criados,'Aviso':'Saida grafica estatica. Reexecute apos alterar superficies. O DWG nao foi salvo automaticamente.'}
    finally:
        tr.Dispose();lock.Dispose()

if 'IN' in globals():
    OUT=executar_dynamo(IN)
