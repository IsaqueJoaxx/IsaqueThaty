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
