# -*- coding: utf-8 -*-
# =============================================================================
#  CONCORDANCIAS DE ESQUINA (curb returns)  -  Dynamo for Civil 3D
#
#  ENTRADAS:
#    IN[0]  RODAR   (bool)    False = simula / True = cria
#    IN[1]  LIMITE  (string)  quantas criar. "1" = so a primeira (teste),
#                             vazio ou "0" = todas.
#
#  Usa Alignment.CreateConnectedAlignment, que e o mecanismo nativo de
#  concordancia. NAO cria objeto Intersecao (a API nao permite).
# =============================================================================

RAIO         = 2.0        # raio da concordancia (m)
OFFSET_BORDA = 4.2407     # eixo -> face do meio-fio (3.6007 pista + 0.64 guia)
TOL_PONTA    = 1.0
PREFIXO      = u"CONC"
RELATORIO    = r"D:\Claude ai\Dynamo rotinas\rotina corredores\concordancias.txt"

import clr
import io

clr.AddReference('AcMgd')
clr.AddReference('AcDbMgd')
clr.AddReference('AcCoreMgd')
clr.AddReference('AeccDbMgd')

from System import IntPtr
from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import OpenMode, Intersect, ObjectId
from Autodesk.AutoCAD.Geometry import Point3dCollection
from Autodesk.Civil.ApplicationServices import CivilApplication
import Autodesk.Civil.DatabaseServices as CDS
from Autodesk.Civil.DatabaseServices import ConnectedAlignmentParams

try:
    from Autodesk.Civil import CurbReturnCurveGroupType
except ImportError:
    CurbReturnCurveGroupType = None
try:
    from Autodesk.Civil import AlignmentType
except ImportError:
    AlignmentType = None

RODAR = IN[0] if len(IN) > 0 and IN[0] is not None else False
try:
    LIMITE = int((IN[1] or "0").strip()) if len(IN) > 1 else 0
except Exception:
    LIMITE = 0

log = []
criadas = 0


def st_off(a, p):
    for args in ((p.X, p.Y), (p.X, p.Y, 0.0, 0.0)):
        try:
            r = a.StationOffset(*args)
            if r is not None:
                return float(r[0]), float(r[1])
        except Exception:
            pass
    return None, None


def eh_offset(a):
    if AlignmentType is None:
        return False
    try:
        return a.AlignmentType in (AlignmentType.Offset, AlignmentType.CurbReturn)
    except Exception:
        return False


adoc = Application.DocumentManager.MdiActiveDocument
civdoc = CivilApplication.ActiveDocument

with adoc.LockDocument():
    db = adoc.Database
    with db.TransactionManager.StartTransaction() as t:

        eixos = []
        for aid in civdoc.GetAlignmentIds():
            a = t.GetObject(aid, OpenMode.ForRead)
            if eh_offset(a):
                continue
            eixos.append({'id': aid, 'nome': a.Name, 'obj': a})

        log.append(u"=== %d eixos ===" % len(eixos))

        # ------------------------------------------------ esquinas
        esquinas = []
        for i in range(len(eixos)):
            for j in range(i + 1, len(eixos)):
                A, B = eixos[i], eixos[j]
                pts = Point3dCollection()
                ok = False
                for modo in (Intersect.OnBothOperands, Intersect.ExtendBoth):
                    try:
                        pts.Clear()
                        A['obj'].IntersectWith(B['obj'], modo, pts,
                                               IntPtr.Zero, IntPtr.Zero)
                        if pts.Count > 0:
                            ok = True
                            break
                    except Exception:
                        pass
                if not ok:
                    continue
                for p in pts:
                    stA, _ = st_off(A['obj'], p)
                    stB, _ = st_off(B['obj'], p)
                    if stA is None or stB is None:
                        continue

                    def dentro(e, s):
                        return (e['obj'].StartingStation - TOL_PONTA <= s
                                <= e['obj'].EndingStation + TOL_PONTA)

                    if dentro(A, stA) and dentro(B, stB):
                        esquinas.append((A, stA, B, stB))

        log.append(u"=== %d esquina(s) ===" % len(esquinas))
        log.append(u"")

        # estilo/rotulo herdados do primeiro eixo, quando existirem
        def id_ou_null(obj, prop):
            try:
                v = getattr(obj, prop)
                return v if v is not None else ObjectId.Null
            except Exception:
                return ObjectId.Null

        grupo_arco = None
        if CurbReturnCurveGroupType is not None:
            try:
                grupo_arco = CurbReturnCurveGroupType.Arc
            except Exception:
                grupo_arco = None

        n_conc = 0
        for A, stA, B, stB in esquinas:

            def ramos(e, s):
                r = []
                if s > e['obj'].StartingStation + TOL_PONTA:
                    r.append(('antes', -1))
                if s < e['obj'].EndingStation - TOL_PONTA:
                    r.append(('depois', +1))
                return r

            for lado_a, sa in ramos(A, stA):
                for lado_b, sb in ramos(B, stB):
                    n_conc += 1
                    if LIMITE and n_conc > LIMITE:
                        continue

                    nome = u"%s %s(%s) x %s(%s)" % (PREFIXO, A['nome'], lado_a,
                                                    B['nome'], lado_b)
                    log.append(u"[%02d] %s" % (n_conc, nome))
                    log.append(u"      %s est %.3f off %+.4f  |  %s est %.3f off %+.4f"
                               % (A['nome'], stA, sa * OFFSET_BORDA,
                                  B['nome'], stB, sb * OFFSET_BORDA))

                    if not RODAR:
                        log.append(u"      (simulacao)")
                        log.append(u"")
                        continue

                    try:
                        prm = ConnectedAlignmentParams()
                        prm.IncomingParentAlignmentId = A['id']
                        prm.IncomingParentAlignmentStation = stA
                        prm.OutgoingParentAlignmentId = B['id']
                        prm.OutgoingParentAlignmentStation = stB
                        prm.OffsetIn = sa * OFFSET_BORDA
                        prm.OffsetOut = sb * OFFSET_BORDA
                        prm.CurveRadius = RAIO
                        prm.GreaterThan180 = False
                        if grupo_arco is not None:
                            prm.CurveGroupType = grupo_arco

                        novo = CDS.Alignment.CreateConnectedAlignment(
                            nome,
                            ObjectId.Null,                       # site
                            ObjectId.Null,                       # layer
                            id_ou_null(A['obj'], 'StyleId'),     # estilo
                            ObjectId.Null,                       # conjunto de rotulos
                            prm)
                        criadas += 1
                        log.append(u"      CRIADA: %s" % novo)
                    except Exception as e:
                        log.append(u"      FALHOU: %s" % e)
                    log.append(u"")

        log.append(u"=== %d concordancia(s) possiveis / %d criada(s) ==="
                   % (n_conc, criadas))
        if not RODAR:
            log.append(u"--- SIMULACAO: ligue RODAR para criar. ---")
        if LIMITE:
            log.append(u"--- LIMITE = %d (modo teste). Zere para criar todas. ---"
                       % LIMITE)

        t.Commit()

try:
    f = io.open(RELATORIO, 'w', encoding='utf-8')
    f.write(u"\n".join(log))
    f.close()
except Exception:
    pass

OUT = log
