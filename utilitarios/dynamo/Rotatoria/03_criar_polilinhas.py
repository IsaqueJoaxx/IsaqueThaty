# NÓ 3 – Criar Polilinhas no AutoCAD (círculos + bordas de acesso)
# Entradas: centro, raios [r_ilha, r_pista_externa, r_total], acessos, layer_prefix
# Usa: Autodesk.AutoCAD via Dynamo

import clr
import math

clr.AddReference('acmgd')
clr.AddReference('acdbmgd')
clr.AddReference('AeccDbMgd')

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import (
    Transaction, Circle, Polyline, LayerTable, LayerTableRecord,
    OpenMode, ObjectId
)
from Autodesk.AutoCAD.Geometry import Point3d, Vector3d
from Autodesk.AutoCAD.Colors import Color

# ── entradas ───────────────────────────────────────────────────────────────
centro       = IN[0]   # [x, y]
raios        = IN[1]   # [r_ilha, r_ext_pista, r_total]
acessos      = IN[2]   # lista de dicts do nó 02
layer_prefix = IN[3]   # ex: "ROT"

cx, cy = float(centro[0]), float(centro[1])
r_ilha, r_pista, r_total = (float(r) for r in raios)

doc   = Application.DocumentManager.MdiActiveDocument
db    = doc.Database
ids   = []

def garantir_layer(tr, lt, nome, cor_idx):
    if not lt.Has(nome):
        lr = LayerTableRecord()
        lr.Name  = nome
        lr.Color = Color.FromColorIndex(
            Autodesk.AutoCAD.Colors.ColorMethod.ByAci, cor_idx)
        lt.UpgradeOpen()
        lt.Add(lr)
        tr.AddNewlyCreatedDBObject(lr, True)

def add_circle(tr, space, cx, cy, raio, layer):
    c        = Circle()
    c.Center = Point3d(cx, cy, 0)
    c.Radius = raio
    c.Layer  = layer
    space.AppendEntity(c)
    tr.AddNewlyCreatedDBObject(c, True)
    return c.ObjectId

def add_pline_fechada(tr, space, pontos, layer):
    pl = Polyline()
    for i, (x, y) in enumerate(pontos):
        pl.AddVertexAt(i, Autodesk.AutoCAD.Geometry.Point2d(x, y), 0, 0, 0)
    pl.Closed = True
    pl.Layer  = layer
    space.AppendEntity(pl)
    tr.AddNewlyCreatedDBObject(pl, True)
    return pl.ObjectId

with doc.LockDocument():
    with db.TransactionManager.StartTransaction() as tr:
        lt    = tr.GetObject(db.LayerTableId, OpenMode.ForRead)
        space = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite)

        L_ILHA  = layer_prefix + "_ILHA_CENTRAL"
        L_PISTA = layer_prefix + "_PISTA_ANULAR"
        L_BORDA = layer_prefix + "_BORDA_EXTERNA"
        L_ACES  = layer_prefix + "_ACESSO"

        garantir_layer(tr, lt, L_ILHA,  1)   # vermelho
        garantir_layer(tr, lt, L_PISTA, 3)   # verde
        garantir_layer(tr, lt, L_BORDA, 5)   # azul
        garantir_layer(tr, lt, L_ACES,  4)   # ciano

        # círculo ilha central
        ids.append(add_circle(tr, space, cx, cy, r_ilha,  L_ILHA))
        # borda interna da pista
        ids.append(add_circle(tr, space, cx, cy, r_pista, L_PISTA))
        # borda externa total
        ids.append(add_circle(tr, space, cx, cy, r_total, L_BORDA))

        # polilinhas de cada acesso
        for ac in acessos:
            pts = [
                ac["esq_interno"],
                ac["esq_externo"],
                ac["dir_externo"],
                ac["dir_interno"],
            ]
            ids.append(add_pline_fechada(tr, space, pts, L_ACES))

        tr.Commit()

OUT = [str(oid) for oid in ids]
