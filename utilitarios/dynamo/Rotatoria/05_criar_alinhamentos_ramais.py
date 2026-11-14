# NÓ 5 – Criar Alinhamentos de Ramal para cada Acesso
# Entradas: acessos (do nó 02), nome_base, civil_doc_handle
# Cria um alinhamento reto para cada ramal (do limite externo até a pista)

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
from Autodesk.Civil.DatabaseServices     import Alignment, AlignmentType

# ── entradas ───────────────────────────────────────────────────────────────
acessos    = IN[0]   # lista de dicts do nó 02
nome_base  = IN[1]   # ex: "ROT-RAMAL"

doc   = Application.DocumentManager.MdiActiveDocument
db    = doc.Database
civil = CivilApplication.ActiveDocument

ids_criados = []

with doc.LockDocument():
    with db.TransactionManager.StartTransaction() as tr:

        for ac in acessos:
            idx  = ac["indice"]
            nome = "{}-{:02d}".format(nome_base, idx + 1)

            # eixo do ramal = linha do ponto externo até o centro do acesso na pista
            p_ext   = Point3d(ac["esq_externo"][0]/2 + ac["dir_externo"][0]/2,
                              ac["esq_externo"][1]/2 + ac["dir_externo"][1]/2, 0)
            p_int   = Point3d(ac["centro"][0], ac["centro"][1], 0)

            align_id = Alignment.Create(
                civil,
                nome,
                ObjectId.Null,  # sem site
                "Standard",
                "Standard"
            )

            align = tr.GetObject(align_id, OpenMode.ForWrite)
            align.Entities.AddFixedLine(p_ext, p_int)

            ids_criados.append(str(align_id))

        tr.Commit()

OUT = ids_criados
