# -*- coding: utf-8 -*-
# =============================================================================
#  AMARRA GREIDES  -  Dynamo for Civil 3D
#  Ajusta o PVI da ponta de cada rua secundaria para a cota do greide da
#  rua principal na esquina, eliminando o degrau vertical.
#
#  ENTRADAS:
#    IN[0]  RODAR  (bool)    False = simula e reporta / True = aplica
#    IN[1]  MODO   (string)  "amarrar" (padrao)  ou  "restaurar"
#
#  Grava backup das cotas originais. "restaurar" desfaz usando esse backup.
#  Quem e principal e quem e secundaria e decidido pela geometria: na esquina,
#  o eixo que CONTINUA para os dois lados e a principal; o que TERMINA ali e a
#  secundaria. Se os dois continuarem (cruzamento real), a rotina nao mexe.
# =============================================================================

# ------------------------------------------------------ parametros
TOL_PONTA    = 1.0     # folga para considerar que o eixo termina na esquina
TOL_IGUAL    = 0.005   # diferenca abaixo disso ja esta amarrado, nao mexe
TOL_PVI      = 2.0     # distancia maxima da ponta ate o PVI a mover
LIMITE_RAMPA = 0.08    # avisa se a rampa resultante passar disso (8%)
BACKUP       = r"D:\Claude ai\Dynamo rotinas\rotina corredores\backup_greides.txt"
RELATORIO    = r"D:\Claude ai\Dynamo rotinas\rotina corredores\amarracao.txt"

import clr
import io
import re

clr.AddReference('AcMgd')
clr.AddReference('AcDbMgd')
clr.AddReference('AcCoreMgd')
clr.AddReference('AeccDbMgd')

from System import IntPtr
from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import OpenMode, Intersect
from Autodesk.AutoCAD.Geometry import Point3dCollection
from Autodesk.Civil.ApplicationServices import CivilApplication

try:
    from Autodesk.Civil import ProfileType
except ImportError:
    from Autodesk.Civil.DatabaseServices import ProfileType
try:
    from Autodesk.Civil import AlignmentType
except ImportError:
    AlignmentType = None

RODAR = IN[0] if len(IN) > 0 and IN[0] is not None else False
MODO  = (IN[1] if len(IN) > 1 and IN[1] else "amarrar").strip().lower()

log = []
backup = []


def st_off(a, p):
    for args in ((p.X, p.Y), (p.X, p.Y, 0.0, 0.0)):
        try:
            r = a.StationOffset(*args)
            if r is not None:
                return float(r[0]), float(r[1])
        except Exception:
            pass
    return None, None


def greide_fg(t, a):
    for pid in a.GetProfileIds():
        p = t.GetObject(pid, OpenMode.ForRead)
        if p.ProfileType == ProfileType.FG:
            return pid, p
    return None, None


def eh_offset(a):
    if AlignmentType is None:
        return False
    try:
        return a.AlignmentType in (AlignmentType.Offset, AlignmentType.CurbReturn)
    except Exception:
        return False


def pvi_da_ponta(prof, est):
    """PVI mais proximo da estacao dada, dentro de TOL_PVI."""
    melhor, dmin = None, None
    for pvi in prof.PVIs:
        d = abs(float(pvi.Station) - est)
        if dmin is None or d < dmin:
            melhor, dmin = pvi, d
    if melhor is not None and dmin is not None and dmin <= TOL_PVI:
        return melhor, dmin
    return None, dmin


def rampa(pvi):
    """Declividade do trecho que sai/chega no PVI, o que existir."""
    for attr in ('GradeOut', 'GradeIn'):
        try:
            g = float(getattr(pvi, attr))
            if g == g:  # nao NaN
                return g, attr
        except Exception:
            pass
    return None, None


adoc = Application.DocumentManager.MdiActiveDocument
civdoc = CivilApplication.ActiveDocument
alterados = 0

with adoc.LockDocument():
    db = adoc.Database
    with db.TransactionManager.StartTransaction() as t:

        # ------------------------------------------------ modo restaurar
        if MODO == "restaurar":
            log.append(u"=== MODO RESTAURAR ===")
            try:
                linhas = io.open(BACKUP, 'r', encoding='utf-8').read().split(u"\n")
            except Exception as e:
                linhas = []
                log.append(u"ERRO  nao consegui ler o backup: %s" % e)

            mapa = {}
            for ln in linhas:
                m = re.match(r'^([^\t]+)\t([-\d.]+)\t([-\d.]+)$', ln.strip())
                if m:
                    mapa.setdefault(m.group(1), []).append(
                        (float(m.group(2)), float(m.group(3))))

            for aid in civdoc.GetAlignmentIds():
                a = t.GetObject(aid, OpenMode.ForRead)
                if a.Name not in mapa:
                    continue
                pid, prof = greide_fg(t, a)
                if prof is None:
                    continue
                prof = t.GetObject(pid, OpenMode.ForWrite)
                for est, cota in mapa[a.Name]:
                    pvi, d = pvi_da_ponta(prof, est)
                    if pvi is None:
                        log.append(u"PULOU  %s: PVI da est %.3f nao localizado."
                                   % (a.Name, est))
                        continue
                    atual = float(pvi.Elevation)
                    if abs(atual - cota) < 1e-6:
                        log.append(u"       %s est %.3f ja esta em %.3f."
                                   % (a.Name, est, cota))
                        continue
                    log.append(u"VOLTA  %s est %.3f: %.3f -> %.3f"
                               % (a.Name, est, atual, cota))
                    if RODAR:
                        pvi.Elevation = cota
                        alterados += 1
            if not RODAR:
                log.append(u"--- SIMULACAO: ligue RODAR para aplicar. ---")
            t.Commit()

        else:
            # -------------------------------------------- coleta eixos
            eixos = []
            for aid in civdoc.GetAlignmentIds():
                a = t.GetObject(aid, OpenMode.ForRead)
                if eh_offset(a):
                    continue
                pid, prof = greide_fg(t, a)
                eixos.append({'id': aid, 'nome': a.Name, 'obj': a,
                              'gid': pid, 'gre': prof})

            log.append(u"=== %d eixos ===" % len(eixos))

            # -------------------------------------------- esquinas
            pares = []
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
                            pares.append((A, stA, B, stB))

            log.append(u"=== %d esquina(s) ===" % len(pares))
            log.append(u"")

            for A, stA, B, stB in pares:

                def continua(e, s):
                    return (s > e['obj'].StartingStation + TOL_PONTA and
                            s < e['obj'].EndingStation - TOL_PONTA)

                cA, cB = continua(A, stA), continua(B, stB)

                if cA and cB:
                    log.append(u"PULOU  %s x %s: cruzamento real, os dois eixos "
                               u"continuam. Decisao sua." % (A['nome'], B['nome']))
                    continue
                if not cA and not cB:
                    log.append(u"PULOU  %s x %s: os dois terminam na esquina."
                               % (A['nome'], B['nome']))
                    continue

                pri, stp = (A, stA) if cA else (B, stB)
                sec, sts = (B, stB) if cA else (A, stA)

                if pri['gre'] is None or sec['gre'] is None:
                    log.append(u"PULOU  %s x %s: falta greide FG."
                               % (pri['nome'], sec['nome']))
                    continue

                try:
                    alvo = float(pri['gre'].ElevationAt(stp))
                except Exception as e:
                    log.append(u"PULOU  %s @ %.2f: cota indisponivel (%s)."
                               % (pri['nome'], stp, e))
                    continue

                pvi, dist = pvi_da_ponta(sec['gre'], sts)
                if pvi is None:
                    log.append(u"PULOU  %s: nenhum PVI a menos de %.1f m da "
                               u"est %.3f (mais proximo: %s)."
                               % (sec['nome'], TOL_PVI, sts,
                                  (u"%.2f m" % dist) if dist is not None else u"?"))
                    continue

                atual = float(pvi.Elevation)
                dif = alvo - atual
                est_pvi = float(pvi.Station)

                backup.append(u"%s\t%.4f\t%.4f" % (sec['nome'], est_pvi, atual))

                if abs(dif) < TOL_IGUAL:
                    log.append(u"OK     %-18s est %8.3f  ja amarrado (%.3f)."
                               % (sec['nome'], est_pvi, atual))
                    continue

                g_ant, qual = rampa(pvi)

                log.append(u"AJUSTA %-18s est %8.3f   %.3f -> %.3f  (%+.3f m)"
                           % (sec['nome'], est_pvi, atual, alvo, dif))
                log.append(u"           principal: %s @ est %.3f" % (pri['nome'], stp))

                if RODAR:
                    prof_w = t.GetObject(sec['gid'], OpenMode.ForWrite)
                    pvi_w, _ = pvi_da_ponta(prof_w, sts)
                    if pvi_w is None:
                        log.append(u"           ERRO: PVI sumiu ao abrir p/ escrita.")
                        continue
                    pvi_w.Elevation = alvo
                    alterados += 1
                    g_dep, qual2 = rampa(pvi_w)
                    if g_ant is not None and g_dep is not None:
                        log.append(u"           rampa %s: %.3f%% -> %.3f%%"
                                   % (qual2 or qual, g_ant * 100.0, g_dep * 100.0))
                        if abs(g_dep) > LIMITE_RAMPA:
                            log.append(u"           *** ATENCAO: rampa resultante "
                                       u"%.2f%% acima do limite de %.1f%%. ***"
                                       % (g_dep * 100.0, LIMITE_RAMPA * 100.0))
                elif g_ant is not None:
                    log.append(u"           rampa atual %s: %.3f%%"
                               % (qual, g_ant * 100.0))
                log.append(u"")

            if not RODAR:
                log.append(u"--- SIMULACAO: nada foi alterado. "
                           u"Ligue RODAR para aplicar. ---")

            t.Commit()

    # ---------------------------------------------------- rebuild corredores
    if RODAR and alterados:
        with db.TransactionManager.StartTransaction() as t2:
            for cid in civdoc.CorridorCollection:
                try:
                    c = t2.GetObject(cid, OpenMode.ForWrite)
                    c.Rebuild()
                    log.append(u"REBUILD ok: %s" % c.Name)
                except Exception as e:
                    log.append(u"AVISO rebuild: %s" % e)
            t2.Commit()

log.append(u"")
log.append(u"=== %d PVI(s) alterado(s) ===" % alterados)

# ---------------------------------------------------------- arquivos
if MODO != "restaurar" and backup:
    try:
        f = io.open(BACKUP, 'w', encoding='utf-8')
        f.write(u"\n".join(backup))
        f.close()
        log.append(u"backup das cotas originais: %s" % BACKUP)
    except Exception as e:
        log.append(u"AVISO  falhou gravar backup: %s" % e)

try:
    f = io.open(RELATORIO, 'w', encoding='utf-8')
    f.write(u"\n".join(log))
    f.close()
except Exception:
    pass

OUT = log
