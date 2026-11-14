# -*- coding: utf-8 -*-
# =============================================================================
#  PASSO 0 - INSPECIONAR O DESENHO
#  Dynamo for Civil 3D 2026  |  no  "Python Script"  (CPython3)
# -----------------------------------------------------------------------------
#  Rode este script ANTES do principal. Ele devolve:
#    1. Os blocos existentes no Model Space + quantidade de insercoes
#    2. Os TAGs de atributo de cada bloco (para achar o campo da estaca)
#    3. Os dispositivos de plotagem instalados
#    4. Os nomes CANONICOS de midia (papel) do "DWG To PDF.pc3"
#
#  ENTRADAS: nenhuma (pode deixar o no sem inputs)
#  SAIDA   : lista de texto - conecte num "Watch"
# =============================================================================

import clr
import traceback

clr.AddReference('AcMgd')
clr.AddReference('AcCoreMgd')
clr.AddReference('AcDbMgd')

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import (
    OpenMode, BlockTable, BlockTableRecord, BlockReference,
    AttributeReference, PlotSettings, PlotSettingsValidator
)

# Nome do dispositivo cujas midias queremos listar
DEVICE = "DWG To PDF.pc3"

out = []

try:
    doc = Application.DocumentManager.MdiActiveDocument
    db = doc.Database

    # ---------------------------------------------------------------- BLOCOS
    lock = doc.LockDocument()
    tr = db.TransactionManager.StartTransaction()
    try:
        bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead)
        ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead)

        contagem = {}
        tags = {}

        for oid in ms:
            ent = tr.GetObject(oid, OpenMode.ForRead)
            if not isinstance(ent, BlockReference):
                continue

            # nome real (resolve blocos dinamicos)
            nome = ent.Name
            try:
                if ent.IsDynamicBlock:
                    dyn = tr.GetObject(ent.DynamicBlockTableRecord, OpenMode.ForRead)
                    nome = dyn.Name
            except Exception:
                pass

            contagem[nome] = contagem.get(nome, 0) + 1

            if nome not in tags:
                lst = []
                try:
                    for aid in ent.AttributeCollection:
                        att = tr.GetObject(aid, OpenMode.ForRead)
                        if isinstance(att, AttributeReference):
                            lst.append("%s = %s" % (att.Tag, att.TextString))
                except Exception:
                    pass
                tags[nome] = lst

        out.append("=== BLOCOS NO MODEL SPACE (nome | qtd) ===")
        for nome in sorted(contagem, key=lambda k: -contagem[k]):
            out.append("  %-40s  %d" % (nome, contagem[nome]))

        out.append("")
        out.append("=== ATRIBUTOS (1a insercao de cada bloco) ===")
        for nome in sorted(contagem, key=lambda k: -contagem[k]):
            if tags.get(nome):
                out.append("  [%s]" % nome)
                for t in tags[nome]:
                    out.append("      %s" % t)

        tr.Commit()
    finally:
        tr.Dispose()
        lock.Dispose()

    # ------------------------------------------------------------- PLOTAGEM
    out.append("")
    out.append("=== DISPOSITIVOS DE PLOTAGEM ===")
    psv = PlotSettingsValidator.Current
    for d in psv.GetPlotDeviceList():
        out.append("  %s" % d)

    out.append("")
    out.append("=== MIDIAS (papeis) DE '%s' ===" % DEVICE)
    ps = PlotSettings(True)          # True = Model Space
    psv.SetPlotConfigurationName(ps, DEVICE, None)
    psv.RefreshLists(ps)
    for canonico in psv.GetCanonicalMediaNameList(ps):
        try:
            local = psv.GetLocaleMediaName(ps, canonico)
        except Exception:
            local = ""
        if "A4" in canonico.upper() or "A4" in local.upper():
            out.append("  >> %-55s   (%s)" % (canonico, local))
    out.append("")
    out.append("  --- todos os demais ---")
    for canonico in psv.GetCanonicalMediaNameList(ps):
        if "A4" not in canonico.upper():
            out.append("     %s" % canonico)

except Exception:
    out.append("ERRO:")
    out.append(traceback.format_exc())

OUT = out
