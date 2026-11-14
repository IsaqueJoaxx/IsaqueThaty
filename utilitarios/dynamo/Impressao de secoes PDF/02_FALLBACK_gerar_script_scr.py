# -*- coding: utf-8 -*-
# =============================================================================
#  PLANO B - GERA UM ARQUIVO .SCR COM COMANDOS -PLOT
#  Dynamo for Civil 3D 2026  |  no "Python Script"  (CPython3)
# -----------------------------------------------------------------------------
#  Use este script se o PlotEngine (01_GERAR_PDF_secoes.py) travar ou der erro
#  dentro do Dynamo. Aqui o Dynamo NAO plota: ele so LE as molduras e escreve
#  um arquivo .scr. Depois voce roda no AutoCAD:
#
#        Comando:  SCRIPT     (ou _SCRIPT)
#        e seleciona o arquivo gerado.
#
#  E a rota mais estavel, porque quem plota e o proprio AutoCAD.
#
#  ENTRADAS DO NO (5 inputs):
#    IN[0]  string - nome do bloco da moldura
#    IN[1]  string - pasta de saida dos PDFs
#    IN[2]  string - nome do papel (como aparece no dialogo PLOT)
#                    ex: "ISO full bleed A4 (297.00 x 210.00 MM)"
#    IN[3]  string - TAG do atributo da estaca ("" = nomeia por indice)
#    IN[4]  string - prefixo do arquivo ("" para nenhum)
#
#  SAIDA: caminho do .scr gerado + previa das linhas
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
    OpenMode, BlockTable, BlockTableRecord, BlockReference, AttributeReference
)

# ---------------------------------------------------------------- parametros
DISPOSITIVO  = "DWG To PDF.pc3"
ESTILO_PLOT  = "."        # "." = nenhum estilo | ex: "monochrome.ctb"
ORIENTACAO   = "L"        # "L" paisagem | "P" retrato
MARGEM       = 0.0
TOL_LINHA    = 0.35


def limpar_nome(txt):
    txt = re.sub(r'[\\/:*?"<>|]', '_', str(txt).strip())
    return re.sub(r'\s+', '_', txt) or "SEM_NOME"


def chave_estaca(txt):
    m = re.search(r'(-?\d+)\s*\+\s*(\d+(?:[.,]\d+)?)', str(txt))
    if m:
        return float(m.group(1)) * 20.0 + float(m.group(2).replace(',', '.'))
    m = re.search(r'(-?\d+(?:[.,]\d+)?)', str(txt))
    return float(m.group(1).replace(',', '.')) if m else None


def main():
    nome_bloco  = IN[0] if len(IN) > 0 and IN[0] else "MOLDURA_A4"
    pasta_saida = IN[1] if len(IN) > 1 and IN[1] else r"C:\TEMP\PDF_SECOES"
    papel       = IN[2] if len(IN) > 2 and IN[2] else "ISO full bleed A4 (297.00 x 210.00 MM)"
    tag_estaca  = IN[3] if len(IN) > 3 and IN[3] else ""
    prefixo     = IN[4] if len(IN) > 4 and IN[4] else ""

    doc = Application.DocumentManager.MdiActiveDocument
    db = doc.Database

    molduras = []
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
                    nome = tr.GetObject(ent.DynamicBlockTableRecord,
                                        OpenMode.ForRead).Name
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

            molduras.append({
                "rotulo": rotulo,
                "x1": ext.MinPoint.X - MARGEM, "y1": ext.MinPoint.Y - MARGEM,
                "x2": ext.MaxPoint.X + MARGEM, "y2": ext.MaxPoint.Y + MARGEM,
                "cy": (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0,
                "cx": (ext.MinPoint.X + ext.MaxPoint.X) / 2.0,
                "alt": ext.MaxPoint.Y - ext.MinPoint.Y,
            })
        tr.Commit()
    finally:
        tr.Dispose()
        lock.Dispose()

    if not molduras:
        return ["Nenhuma moldura '%s' encontrada." % nome_bloco]

    # ------------------------------------------------------------- ordenacao
    if tag_estaca and all(chave_estaca(m["rotulo"]) is not None for m in molduras):
        molduras.sort(key=lambda m: chave_estaca(m["rotulo"]))
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

    if not os.path.isdir(pasta_saida):
        os.makedirs(pasta_saida)

    # ---------------------------------------------------------- monta o .scr
    linhas = ["FILEDIA", "0", "CMDDIA", "0", "BACKGROUNDPLOT", "0"]
    usados = {}

    for m in molduras:
        base = prefixo + limpar_nome(m["rotulo"])
        n = usados.get(base.upper(), 0) + 1
        usados[base.upper()] = n
        if n > 1:
            base = "%s_(%d)" % (base, n)
        pdf = os.path.join(pasta_saida, base + ".pdf")

        linhas += [
            "-PLOT",
            "Y",                                   # configuracao detalhada?
            "Model",                               # layout
            DISPOSITIVO,                           # dispositivo
            papel,                                 # papel
            "M",                                   # unidades: milimetros
            ORIENTACAO,                            # orientacao
            "N",                                   # de cabeca para baixo?
            "W",                                   # area de plotagem: janela
            "%.6f,%.6f" % (m["x1"], m["y1"]),      # canto inferior esquerdo
            "%.6f,%.6f" % (m["x2"], m["y2"]),      # canto superior direito
            "F",                                   # escala: ajustar (Fit)
            "C",                                   # deslocamento: centralizado
            "Y",                                   # plotar com estilos?
            ESTILO_PLOT,                           # tabela de estilos
            "Y",                                   # espessuras de linha
            "N",                                   # escalar espessuras
            "N",                                   # paper space primeiro
            "N",                                   # ocultar objetos
            pdf,                                   # arquivo de saida
            "N",                                   # salvar alteracoes no setup
            "Y",                                   # prosseguir
        ]

    linhas += ["FILEDIA", "1", "CMDDIA", "1"]

    scr = os.path.join(pasta_saida, "_PLOTAR_SECOES.scr")
    with open(scr, "w", encoding="utf-8") as f:
        f.write("\n".join(linhas) + "\n")

    return [
        "Arquivo de script gerado:",
        "   %s" % scr,
        "",
        "Molduras: %d   |   PDFs previstos: %d" % (len(molduras), len(molduras)),
        "",
        "COMO USAR:",
        "   1. No AutoCAD digite:  SCRIPT",
        "   2. Selecione o arquivo acima.",
        "   3. Aguarde. NAO mexa no AutoCAD durante a execucao.",
        "",
        "ATENCAO: a ordem das perguntas do comando -PLOT muda conforme a versao",
        "e o idioma. Se travar, rode -PLOT manualmente UMA vez, anote a sequencia",
        "real de respostas e ajuste a lista 'linhas' deste script.",
        "",
        "--- PREVIA (primeiras 30 linhas) ---",
    ] + linhas[:30]


try:
    OUT = main()
except Exception:
    OUT = ["ERRO:", traceback.format_exc()]
