# NÓ 4 – Criar Alinhamento Circular (eixo da pista anular) no Civil 3D
# Entradas: centro, r_eixo (= r_ilha + num_faixas*larg/2), nome_alinhamento, nome_site
# Cria um alinhamento circular com 1 curva de 360°

import clr
import math

clr.AddReference('acmgd')
clr.AddReference('acdbmgd')
clr.AddReference('AeccDbMgd')
clr.AddReference('AeccProjMgd')

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices   import OpenMode, ObjectId
from Autodesk.AutoCAD.Geometry           import Point3d
from Autodesk.Civil.ApplicationServices  import CivilApplication
from Autodesk.Civil.DatabaseServices     import (
    AlignmentCreationOptions, Alignment, AlignmentType,
    AlignmentSubEntityArc
)

# ── entradas ───────────────────────────────────────────────────────────────
centro     = IN[0]   # [x, y]
r_eixo     = IN[1]   # raio do eixo da pista anular
nome_align = IN[2]   # ex: "ROT-EIXO-PISTA"
nome_site  = IN[3]   # ex: "" (sem site) ou nome do site existente

cx, cy  = float(centro[0]), float(centro[1])
r       = float(r_eixo)

doc   = Application.DocumentManager.MdiActiveDocument
db    = doc.Database
civil = CivilApplication.ActiveDocument

result_id = None

with doc.LockDocument():
    with db.TransactionManager.StartTransaction() as tr:

        # obter/criar site (usar ObjectId.Null para sem site)
        site_id = ObjectId.Null
        if nome_site:
            site_col = civil.GetSiteIds()
            for sid in site_col:
                s = tr.GetObject(sid, OpenMode.ForRead)
                if s.Name == nome_site:
                    site_id = sid
                    break

        opts                    = AlignmentCreationOptions()
        opts.AlignmentType      = AlignmentType.Centerline
        opts.ConversionMethod   = None
        opts.CreateProfile      = False

        align_id = Alignment.Create(
            civil,
            nome_align,
            site_id,
            "Standard",        # estilo de alinhamento
            "Standard"         # estilo de rótulo
        )

        align = tr.GetObject(align_id, OpenMode.ForWrite)

        # arco de 360° = duas meias curvas de 180°
        # Civil 3D não aceita arco de 360° direto; usa-se dois arcos de 180°
        pi = math.pi
        # ponto inicial: leste (0°)
        p_start = Point3d(cx + r, cy, 0)
        p_mid1  = Point3d(cx,     cy + r, 0)   # topo (90°)
        p_end1  = Point3d(cx - r, cy, 0)        # oeste (180°)
        p_mid2  = Point3d(cx,     cy - r, 0)   # base (270°)

        ent = align.Entities
        arc1 = ent.AddFixedCurve(p_start, p_mid1,  p_end1, True)
        arc2 = ent.AddFixedCurve(p_end1,  p_mid2,  p_start, True)

        tr.Commit()
        result_id = str(align_id)

OUT = result_id
