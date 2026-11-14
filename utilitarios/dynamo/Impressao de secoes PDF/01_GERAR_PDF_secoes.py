# -*- coding: utf-8 -*-
# =============================================================================
#  GERADOR DE PDF - SECOES TRANSVERSAIS / NOTAS DE SERVICO DER-SP
#  Dynamo for Civil 3D 2026  |  no "Python Script"  (CPython3)
# -----------------------------------------------------------------------------
#  Plota cada moldura A4 (BlockReference no Model Space) num PDF separado,
#  usando area de janela (Window) + ajustar a folha (Scale to Fit).
#
#  ENTRADAS DO NO (crie 6 inputs, nesta ordem):
#    IN[0]  string  - nome do bloco da moldura          ex: "MOLDURA_A4"
#    IN[1]  string  - pasta de saida                    ex: "C:\\OBRA\\PDF"
#    IN[2]  string  - nome canonico da midia (papel)
#                     ex: "ISO_full_bleed_A4_(297.00_x_210.00_MM)"
#    IN[3]  string  - TAG do atributo com a estaca      ex: "ESTACA"
#                     ("" = nomeia por indice: SEC_0001, SEC_0002 ...)
#    IN[4]  string  - prefixo do arquivo                ex: "NS_"  ou  ""
#    IN[5]  bool    - EXECUTAR (Boolean). False = simulacao, nao plota nada.
#
#  SAIDA: relatorio (lista de texto) -> ligue num no "Watch"
# =============================================================================

import clr
import os
import re
import traceback

clr.AddReference('AcMgd')
clr.AddReference('AcCoreMgd')
clr.AddReference('AcDbMgd')

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import (
    OpenMode, BlockTable, BlockTableRecord, BlockReference, AttributeReference,
    LayoutManager, PlotSettings, PlotSettingsValidator,
    PlotType, StdScaleType, PlotRotation, PlotPaperUnit
)
from Autodesk.AutoCAD.Geometry import Extents2d, Point2d
from Autodesk.AutoCAD.PlottingServices import (
    PlotFactory, PlotInfo, PlotInfoValidator, PlotProgressDialog,
    PlotPageInfo, ProcessPlotState, MatchingPolicy, PlotMessageIndex
)

# =============================================================================
#  PARAMETROS FIXOS (ajuste se precisar)
# =============================================================================
DISPOSITIVO = "DWG To PDF.pc3"
ESTILO_PLOT = ""      # ex: "monochrome.ctb"  |  "" = nao altera
MARGEM      = 0.0     # folga em unidades de desenho ao redor da moldura
TOL_LINHA   = 0.35    # tolerancia (fracao da altura) p/ agrupar molduras na mesma linha


# =============================================================================
#  UTILITARIOS
# =============================================================================
def limpar_nome(txt):
    txt = str(txt).strip()
    txt = re.sub(r'[\\/:*?"<>|]', '_', txt)
    txt = re.sub(r'\s+', '_', txt)
    return txt if txt else "SEM_NOME"


def chave_estaca(txt):
    """'120+10.00' -> 2410.0  (para ordenar). None se nao for numerico."""
    m = re.search(r'(-?\d+)\s*\+\s*(\d+(?:[.,]\d+)?)', str(txt))
    if m:
        return float(m.group(1)) * 20.0 + float(m.group(2).replace(',', '.'))
    m = re.search(r'(-?\d+(?:[.,]\d+)?)', str(txt))
    if m:
        return float(m.group(1).replace(',', '.'))
    return None


def dims_da_midia(nome_midia):
    """Extrai (largura, altura) em mm do nome canonico. None se nao achar."""
    m = re.search(r'(\d+(?:\.\d+)?)\s*[xX]\s*(\d+(?:\.\d+)?)', nome_midia)
    return (float(m.group(1)), float(m.group(2))) if m else None


# =============================================================================
#  COLETA DAS MOLDURAS
# =============================================================================
def coletar_molduras(doc, nome_bloco, tag_estaca):
    db = doc.Database
    achadas = []
    lock = doc.LockDocument()
    tr = db.TransactionManager.StartTransaction()
    try:
        bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead)
        ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead)

        idx = 0
        for oid in ms:
            ent = tr.GetObject(oid, OpenMode.ForRead)
            if not isinstance(ent, BlockReference):
                continue

            nome = ent.Name
            try:
                if ent.IsDynamicBlock:
                    dyn = tr.GetObject(ent.DynamicBlockTableRecord, OpenMode.ForRead)
                    nome = dyn.Name
            except Exception:
                pass

            if nome.upper() != nome_bloco.upper():
                continue

            try:
                ext = ent.GeometricExtents
            except Exception:
                continue

            rotulo = ""
            if tag_estaca:
                try:
                    for aid in ent.AttributeCollection:
                        att = tr.GetObject(aid, OpenMode.ForRead)
                        if isinstance(att, AttributeReference) and \
                           att.Tag.upper() == tag_estaca.upper():
                            rotulo = att.TextString
                            break
                except Exception:
                    pass

            idx += 1
            if not rotulo:
                rotulo = "SEC_%04d" % idx

            achadas.append({
                "rotulo": rotulo,
                "ext":  ext,
                "cx":   (ext.MinPoint.X + ext.MaxPoint.X) / 2.0,
                "cy":   (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0,
                "larg": ext.MaxPoint.X - ext.MinPoint.X,
                "alt":  ext.MaxPoint.Y - ext.MinPoint.Y,
            })

        tr.Commit()
    finally:
        tr.Dispose()
        lock.Dispose()
    return achadas


def ordenar(molduras, tag_estaca, log):
    tem_estaca = bool(tag_estaca) and \
        all(chave_estaca(m["rotulo"]) is not None for m in molduras)

    if tem_estaca:
        molduras.sort(key=lambda m: chave_estaca(m["rotulo"]))
        log.append("Ordenacao: por estaca (atributo '%s')." % tag_estaca)
    else:
        alt_med = sum(m["alt"] for m in molduras) / float(len(molduras))
        tol = alt_med * TOL_LINHA
        molduras.sort(key=lambda m: -m["cy"])
        linha, y_ref = 0, molduras[0]["cy"]
        for m in molduras:
            if abs(m["cy"] - y_ref) > tol:
                linha += 1
                y_ref = m["cy"]
            m["linha"] = linha
        molduras.sort(key=lambda m: (m["linha"], m["cx"]))
        log.append("Ordenacao: grade (esquerda->direita, cima->baixo).")
    return molduras


def definir_arquivos(molduras, pasta, prefixo):
    usados = {}
    for m in molduras:
        base = prefixo + limpar_nome(m["rotulo"])
        n = usados.get(base.upper(), 0) + 1
        usados[base.upper()] = n
        if n > 1:
            base = "%s_(%d)" % (base, n)
        m["arquivo"] = os.path.join(pasta, base + ".pdf")
    return molduras


# =============================================================================
#  PLOTAGEM DE UMA FOLHA
# =============================================================================
def plotar_uma(engine, doc, db, moldura, midia, rotacao):
    tr = db.TransactionManager.StartTransaction()
    try:
        btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead)
        layout = tr.GetObject(btr.LayoutId, OpenMode.ForRead)

        ps = PlotSettings(layout.ModelType)
        ps.CopyFrom(layout)
        psv = PlotSettingsValidator.Current

        psv.SetPlotConfigurationName(ps, DISPOSITIVO, midia)
        psv.RefreshLists(ps)
        psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters)

        e = moldura["ext"]
        janela = Extents2d(
            Point2d(e.MinPoint.X - MARGEM, e.MinPoint.Y - MARGEM),
            Point2d(e.MaxPoint.X + MARGEM, e.MaxPoint.Y + MARGEM))
        psv.SetPlotWindowArea(ps, janela)
        psv.SetPlotType(ps, PlotType.Window)

        psv.SetUseStandardScale(ps, True)
        psv.SetStdScaleType(ps, StdScaleType.ScaleToFit)
        psv.SetPlotCentered(ps, True)
        psv.SetPlotRotation(ps, rotacao)

        if ESTILO_PLOT:
            try:
                psv.SetCurrentStyleSheet(ps, ESTILO_PLOT)
            except Exception:
                pass

        pi = PlotInfo()
        pi.Layout = btr.LayoutId
        pi.OverrideSettings = ps

        piv = PlotInfoValidator()
        piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled
        piv.Validate(pi)

        ppd = PlotProgressDialog(False, 1, True)
        try:
            try:
                ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle,
                                      "Gerando PDF das secoes")
                ppd.set_PlotMsgString(PlotMessageIndex.SheetName,
                                      moldura["rotulo"])
                ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage,
                                      "Cancelar")
                ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage,
                                      "Pular folha")
            except Exception:
                pass

            ppd.LowerPlotProgressRange = 0
            ppd.UpperPlotProgressRange = 100
            ppd.PlotProgressPos = 0
            ppd.OnBeginPlot()
            ppd.IsVisible = True

            engine.BeginPlot(ppd, None)
            engine.BeginDocument(pi, doc.Name, None, 1, True, moldura["arquivo"])

            ppd.OnBeginSheet()
            ppd.LowerSheetProgressRange = 0
            ppd.UpperSheetProgressRange = 100
            ppd.SheetProgressPos = 0

            engine.BeginPage(PlotPageInfo(), pi, True, None)
            engine.BeginGenerateGraphics(None)
            engine.EndGenerateGraphics(None)
            engine.EndPage(None)

            ppd.SheetProgressPos = 100
            ppd.OnEndSheet()

            engine.EndDocument(None)
            ppd.PlotProgressPos = 100
            ppd.OnEndPlot()
            engine.EndPlot(None)
        finally:
            ppd.Destroy()
    finally:
        tr.Dispose()


# =============================================================================
#  PRINCIPAL
# =============================================================================
def main():
    log = []

    nome_bloco  = IN[0] if len(IN) > 0 and IN[0] else "MOLDURA_A4"
    pasta_saida = IN[1] if len(IN) > 1 and IN[1] else r"C:\TEMP\PDF_SECOES"
    midia       = IN[2] if len(IN) > 2 and IN[2] else "ISO_full_bleed_A4_(297.00_x_210.00_MM)"
    tag_estaca  = IN[3] if len(IN) > 3 and IN[3] else ""
    prefixo     = IN[4] if len(IN) > 4 and IN[4] else ""
    executar    = bool(IN[5]) if len(IN) > 5 else False

    doc = Application.DocumentManager.MdiActiveDocument
    db  = doc.Database

    # ---------------------------------------------------------------- coleta
    molduras = coletar_molduras(doc, nome_bloco, tag_estaca)
    if not molduras:
        return ["NENHUMA moldura encontrada com o nome '%s'." % nome_bloco,
                "Rode 00_INSPECIONAR_desenho.py para ver os nomes reais dos blocos."]

    molduras = ordenar(molduras, tag_estaca, log)
    molduras = definir_arquivos(molduras, pasta_saida, prefixo)

    # ------------------------------------------------------ rotacao do papel
    dm = dims_da_midia(midia)
    rotacao = PlotRotation.Degrees000
    if dm:
        papel_paisagem = dm[0] >= dm[1]
        frame_paisagem = molduras[0]["larg"] >= molduras[0]["alt"]
        if papel_paisagem != frame_paisagem:
            rotacao = PlotRotation.Degrees090
        log.append("Papel %.0f x %.0f mm | moldura %.2f x %.2f un | rotacao %s"
                   % (dm[0], dm[1], molduras[0]["larg"], molduras[0]["alt"],
                      "90" if rotacao == PlotRotation.Degrees090 else "0"))

    log.append("Molduras encontradas: %d" % len(molduras))
    log.append("Dispositivo: %s" % DISPOSITIVO)
    log.append("Midia: %s" % midia)
    log.append("Pasta de saida: %s" % pasta_saida)
    log.append("-" * 62)

    # ------------------------------------------------------------- simulacao
    if not executar:
        log.append(">>> MODO SIMULACAO. Ligue o Boolean (IN[5]) em True para plotar.")
        log.append("")
        for i, m in enumerate(molduras, 1):
            log.append("%4d | %-24s -> %s"
                       % (i, m["rotulo"], os.path.basename(m["arquivo"])))
        return log

    # -------------------------------------------------------------- plotagem
    if not os.path.isdir(pasta_saida):
        os.makedirs(pasta_saida)

    if PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting:
        return ["O AutoCAD ja esta em processo de plotagem.",
                "Aguarde terminar (ou finalize a plotagem em 2o plano) e rode de novo."]

    bg_antigo = Application.GetSystemVariable("BACKGROUNDPLOT")
    Application.SetSystemVariable("BACKGROUNDPLOT", 0)

    ok, falhas = 0, []
    lock = doc.LockDocument()
    engine = None
    try:
        LayoutManager.Current.CurrentLayout = "Model"
        engine = PlotFactory.CreatePublishEngine()

        for i, m in enumerate(molduras, 1):
            try:
                plotar_uma(engine, doc, db, m, midia, rotacao)
                ok += 1
                log.append("%4d/%d  OK     %s"
                           % (i, len(molduras), os.path.basename(m["arquivo"])))
            except Exception as ex:
                falhas.append((m["rotulo"], str(ex)))
                log.append("%4d/%d  FALHA  %s  ->  %s"
                           % (i, len(molduras), m["rotulo"], ex))
    except Exception:
        log.append("ERRO GERAL:")
        log.append(traceback.format_exc())
    finally:
        if engine is not None:
            try:
                engine.Destroy()
            except Exception:
                pass
        lock.Dispose()
        Application.SetSystemVariable("BACKGROUNDPLOT", bg_antigo)

    log.append("-" * 62)
    log.append("Concluido: %d PDF(s) gerados, %d falha(s)." % (ok, len(falhas)))
    for r, e in falhas:
        log.append("   FALHA em %s: %s" % (r, e))
    return log


try:
    OUT = main()
except Exception:
    OUT = ["ERRO NAO TRATADO:", traceback.format_exc()]
