# =============================================================================
#  IntersecaoCorredor.py  -  Dynamo for Civil 3D 2026 (no Python Script node)
# -----------------------------------------------------------------------------
#  Cria, para uma intersecao entre dois alinhamentos:
#   1. Localiza a JUNCAO (intersecao geometrica dos dois alinhamentos)
#   2. Cria um PERFIL DE GREIDE (layout) para cada via, com PVI na juncao
#      na COTA informada e rampas constantes a partir dela
#   3. Cria o CORREDOR com duas baselines (uma por via) usando um
#      ASSEMBLY ja existente no desenho
#   4. Cria a SUPERFICIE DO CORREDOR (links "Top") = superficie de projeto
#
#  COMO USAR NO DYNAMO:
#   - Abra o Dynamo (aba Manage > Dynamo) com o desenho ativo
#   - Insira um no "Python Script" (engine CPython3) e cole este codigo
#   - Conecte 7 entradas (nos "String"/"Number" ou "Code Block"):
#       IN[0] = nome do alinhamento da VIA 1          (ex: "AL-VIA-1")
#       IN[1] = nome do alinhamento da VIA 2          (ex: "AL-VIA-2")
#       IN[2] = cota de projeto da juncao             (ex: 712.350)
#       IN[3] = declividade long. VIA 1 em % (+ sobe a partir da juncao)
#       IN[4] = declividade long. VIA 2 em %
#       IN[5] = nome do assembly existente no desenho (ex: "PISTA-7m")
#       IN[6] = prefixo dos objetos criados           (ex: "INT-01")
#   - Rode o grafo; OUT devolve o log do processo
#
#  PRE-REQUISITOS:
#   - Os dois alinhamentos devem existir e se cruzar em planta
#     (crie-os a partir das polilinhas: Home > Alignment > Create from Objects)
#   - Um assembly montado no desenho (pista + acostamento + talude, etc.)
#
#  OBS: o corredor gerado e a base "vias cruzadas". Para refinar as areas de
#  concordancia (curb returns) use as breaklines geradas pela LISP INTERSECAO
#  ou edite as regioes do corredor. A superficie final de projeto pode ser a
#  uniao: superficie do corredor + 3DFACES da LISP.
# =============================================================================

import clr
clr.AddReference('AcMgd')
clr.AddReference('AcCoreMgd')
clr.AddReference('AcDbMgd')
clr.AddReference('AeccDbMgd')

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import OpenMode, Intersect
from Autodesk.AutoCAD.Geometry import Point3dCollection
from Autodesk.Civil.ApplicationServices import CivilApplication
from Autodesk.Civil.DatabaseServices import Profile
from System import IntPtr

# ----------------------------- entradas
NOME_AL1   = IN[0]
NOME_AL2   = IN[1]
COTA_JUNC  = float(IN[2])
GRADE1     = float(IN[3])   # %
GRADE2     = float(IN[4])   # %
NOME_ASSEM = IN[5]
PREFIXO    = IN[6] if IN[6] else "INT"

adoc   = Application.DocumentManager.MdiActiveDocument
civdoc = CivilApplication.ActiveDocument
db     = adoc.Database
log    = []


def achar_por_nome(ids, nome, tr):
    """Procura um objeto pelo nome em uma colecao de ObjectIds."""
    for oid in ids:
        obj = tr.GetObject(oid, OpenMode.ForRead)
        if obj.Name.upper() == nome.upper():
            return oid
    return None


def estaca_de(al, x, y):
    """Estaca do alinhamento mais proxima do ponto (x,y).
    pythonnet devolve os parametros ref como tupla."""
    try:
        res = al.StationOffset(x, y, 0.0, 0.0)
        return float(res[0]) if hasattr(res, '__len__') else float(res)
    except Exception:
        # fallback com tolerancia explicita
        res = al.StationOffset(x, y, 0.01, 0.0, 0.0)
        return float(res[0])


def criar_perfil(nome, al_id, tr, estaca_junc, cota, grade_pct):
    """Perfil de layout com 3 PVIs: inicio, juncao (cota dada) e fim.
    Rampas simetricas a partir da juncao (grade_pct em %)."""
    al = tr.GetObject(al_id, OpenMode.ForRead)
    layer_id = db.Clayer
    style_id = civdoc.Styles.ProfileStyles[0]
    label_id = civdoc.Styles.LabelSetStyles.ProfileLabelSetStyles[0]
    pid  = Profile.CreateByLayout(nome, al_id, layer_id, style_id, label_id)
    prof = tr.GetObject(pid, OpenMode.ForWrite)
    s0, s1 = al.StartingStation, al.EndingStation
    g = grade_pct / 100.0
    prof.PVIs.AddPVI(s0, cota + g * abs(s0 - estaca_junc))
    prof.PVIs.AddPVI(estaca_junc, cota)
    prof.PVIs.AddPVI(s1, cota + g * abs(s1 - estaca_junc))
    return pid


with adoc.LockDocument():
    with db.TransactionManager.StartTransaction() as tr:
        try:
            # ---------- 1. localizar objetos
            al1_id = achar_por_nome(civdoc.GetAlignmentIds(), NOME_AL1, tr)
            al2_id = achar_por_nome(civdoc.GetAlignmentIds(), NOME_AL2, tr)
            asm_id = achar_por_nome(civdoc.AssemblyCollection, NOME_ASSEM, tr)
            if not al1_id: raise Exception("Alinhamento nao encontrado: " + NOME_AL1)
            if not al2_id: raise Exception("Alinhamento nao encontrado: " + NOME_AL2)
            if not asm_id: raise Exception("Assembly nao encontrado: " + NOME_ASSEM)
            al1 = tr.GetObject(al1_id, OpenMode.ForRead)
            al2 = tr.GetObject(al2_id, OpenMode.ForRead)
            log.append("Alinhamentos e assembly localizados.")

            # ---------- 2. juncao (intersecao geometrica)
            pts = Point3dCollection()
            al1.IntersectWith(al2, Intersect.OnBothOperands,
                              pts, IntPtr.Zero, IntPtr.Zero)
            if pts.Count == 0:
                raise Exception("Os alinhamentos nao se cruzam em planta.")
            pj = pts[0]
            est1 = estaca_de(al1, pj.X, pj.Y)
            est2 = estaca_de(al2, pj.X, pj.Y)
            log.append("Juncao: X=%.3f Y=%.3f | est. via1=%.2f | est. via2=%.2f"
                       % (pj.X, pj.Y, est1, est2))

            # ---------- 3. perfis de greide
            p1_id = criar_perfil(PREFIXO + "-GREIDE-V1", al1_id, tr,
                                 est1, COTA_JUNC, GRADE1)
            p2_id = criar_perfil(PREFIXO + "-GREIDE-V2", al2_id, tr,
                                 est2, COTA_JUNC, GRADE2)
            log.append("Perfis criados com PVI na juncao (cota %.3f)." % COTA_JUNC)

            # ---------- 4. corredor com duas baselines
            cor_id = civdoc.CorridorCollection.Add(PREFIXO + "-CORREDOR")
            cor = tr.GetObject(cor_id, OpenMode.ForWrite)

            bl1 = cor.Baselines.Add(PREFIXO + "-BL-V1", al1_id, p1_id)
            bl1.BaselineRegions.Add(PREFIXO + "-RG-V1", asm_id,
                                    al1.StartingStation, al1.EndingStation)
            bl2 = cor.Baselines.Add(PREFIXO + "-BL-V2", al2_id, p2_id)
            bl2.BaselineRegions.Add(PREFIXO + "-RG-V2", asm_id,
                                    al2.StartingStation, al2.EndingStation)
            log.append("Corredor criado com 2 baselines.")

            # ---------- 5. superficie do corredor (links Top)
            try:
                csurf = cor.CorridorSurfaces.Add(PREFIXO + "-SUP-PROJETO")
                csurf.AddLinkCode("Top", True)
                log.append("Superficie de projeto criada (links Top).")
            except Exception as ex:
                log.append("Superficie do corredor nao criada automaticamente: "
                           + str(ex))
                log.append("Crie manualmente em: propriedades do corredor > "
                           "aba Surfaces > '+' > Add link code 'Top'.")

            cor.Rebuild()
            tr.Commit()
            log.append("CONCLUIDO. Revise as regioes na area de concordancia.")
        except Exception as ex:
            log.append("ERRO: " + str(ex))

OUT = log
