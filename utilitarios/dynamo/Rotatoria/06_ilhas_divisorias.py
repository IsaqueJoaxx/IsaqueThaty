# NÓ 6 – Ilhas Divisórias (Splitter Islands)
# Cria hachuras triangulares entre os ramais de entrada e saída
# Entradas: acessos, r_ilha, r_total, layer_ilhas

import clr
import math

clr.AddReference('acmgd')
clr.AddReference('acdbmgd')

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices   import (
    Transaction, Hatch, HatchLoopTypes, Polyline,
    LayerTable, LayerTableRecord, OpenMode
)
from Autodesk.AutoCAD.Geometry import Point2d, Point3d
from Autodesk.AutoCAD.Colors   import Color

# ── entradas ───────────────────────────────────────────────────────────────
acessos      = IN[0]
r_ilha       = float(IN[1])
r_total      = float(IN[2])
layer_ilhas  = IN[3]   # ex: "ROT_ILHA_DIV"

doc   = Application.DocumentManager.MdiActiveDocument
db    = doc.Database
ids   = []

def garantir_layer(tr, lt, nome, cor_idx):
    if not lt.Has(nome):
        lr       = LayerTableRecord()
        lr.Name  = nome
        lr.Color = Color.FromColorIndex(
            Autodesk.AutoCAD.Colors.ColorMethod.ByAci, cor_idx)
        lt.UpgradeOpen()
        lt.Add(lr)
        tr.AddNewlyCreatedDBObject(lr, True)

def add_pline(tr, space, pts, layer, fechada=True):
    pl = Polyline()
    for i, (x, y) in enumerate(pts):
        pl.AddVertexAt(i, Point2d(x, y), 0, 0, 0)
    pl.Closed = fechada
    pl.Layer  = layer
    space.AppendEntity(pl)
    tr.AddNewlyCreatedDBObject(pl, True)
    return pl.ObjectId

with doc.LockDocument():
    with db.TransactionManager.StartTransaction() as tr:
        lt    = tr.GetObject(db.LayerTableId, OpenMode.ForRead)
        space = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite)
        garantir_layer(tr, lt, layer_ilhas, 6)  # magenta

        n = len(acessos)
        for i in range(n):
            ac_cur  = acessos[i]
            ac_next = acessos[(i + 1) % n]

            ang_cur  = math.radians(ac_cur["angulo_graus"])
            ang_next = math.radians(ac_next["angulo_graus"])
            ang_mid  = (ac_cur["angulo_graus"] + ac_next["angulo_graus"]) / 2.0

            cx = ac_cur["centro"][0] - r_ilha * math.cos(ang_cur) + \
                 0  # origem = 0 porque os pontos já são absolutos
            # vértice da ilha: ponto na borda externa entre dois ramais
            # ângulo médio, raio entre r_ilha e r_total
            r_mid = (r_ilha + r_total) / 2.0 * 0.6

            # recalcula centro da rotatória a partir do acesso
            cx0 = ac_cur["centro"][0] - r_ilha * math.cos(ang_cur)  # aprox
            cy0 = ac_cur["centro"][1] - r_ilha * math.sin(ang_cur)

            am_rad = math.radians(ang_mid)
            ponta  = [cx0 + r_mid * math.cos(am_rad),
                      cy0 + r_mid * math.sin(am_rad)]

            pts_ilha = [
                ac_cur["dir_interno"],
                ponta,
                ac_next["esq_interno"],
            ]
            oid = add_pline(tr, space, pts_ilha, layer_ilhas)
            ids.append(str(oid))

        tr.Commit()

OUT = ids
