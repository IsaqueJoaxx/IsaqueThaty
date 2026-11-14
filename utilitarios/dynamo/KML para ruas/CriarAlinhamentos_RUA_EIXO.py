# -*- coding: utf-8 -*-
# =============================================================================
#  CRIAR ALINHAMENTOS A PARTIR DOS EIXOS DO KML   -   Dynamo for Civil 3D
#  (companheiro do kml_para_ruas_dwg.py)
# =============================================================================
#
#  ENTRADAS (portas do nó Python Script, nessa ordem):
#
#   IN[0] RODAR               (bool) False = só diagnostica, não mexe no desenho
#                                    True  = cria os alinhamentos
#   IN[1] LAYER_EIXO          (str)  layer das polilinhas de eixo   (padrão RUA_EIXO)
#   IN[2] LAYER_ALINHAMENTO   (str)  layer dos alinhamentos, criado se não existir
#                                    (padrão RUA_ALINHAMENTO)
#   IN[3] ESTILO_ALINHAMENTO  (str)  parte do nome do estilo; vazio = primeiro da lista
#   IN[4] ESTILO_ROTULOS      (str)  parte do nome do conjunto de rótulos; vazio = primeiro
#   IN[5] SUFIXO              (str)  fim do nome do alinhamento (padrão "alinhamento")
#   IN[6] CURVAS              (bool) True = insere curvas entre tangentes com o raio
#                                    padrão do Civil 3D (padrão False)
#
#  NOME DO ALINHAMENTO:   "<código> <nome da rua> <sufixo>"
#        ex.: "01 Rua das Acácias alinhamento"
#  -> mesmo padrão do modo "codigo" da rotina CriarCorredoresEmLote.
#
#  O código e o nome vêm do XDATA "KML_RUA" que o kml_para_ruas_dwg.py grava em
#  cada eixo. Polilinha no layer sem esse XDATA vira "<próximo código> Eixo <sufixo>".
#  Alinhamento com o mesmo nome já existente é pulado: dá para rodar de novo sem
#  duplicar. As polilinhas de eixo são mantidas no desenho.
# =============================================================================

import clr
import re
import traceback
import unicodedata

for _ref in ("AcMgd", "AcCoreMgd", "AcDbMgd", "AecBaseMgd", "AecPropDataMgd", "AeccDbMgd"):
    clr.AddReference(_ref)

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import (OpenMode, SymbolUtilityServices, ObjectId,
                                               LayerTableRecord)
from Autodesk.AutoCAD.Colors import Color, ColorMethod
from Autodesk.Civil.ApplicationServices import CivilApplication
from Autodesk.Civil.DatabaseServices import Alignment, PolylineOptions

APPID = "KML_RUA"
INVALIDOS = re.compile(r'[<>/\\":;?*|,=`]')


# ----------------------------------------------------------------- utilidades
def entrada(i, padrao, vazio_vira_padrao=True):
    try:
        v = IN[i]
    except Exception:
        return padrao
    if v is None:
        return padrao
    if isinstance(padrao, bool):
        return bool(v)
    v = str(v).strip()
    if not v and vazio_vira_padrao:
        return padrao
    return v


def norm(s):
    s = unicodedata.normalize("NFKD", str(s or ""))
    return "".join(c for c in s if not unicodedata.combining(c)).lower().strip()


def limpar_nome(s):
    return re.sub(r"\s+", " ", INVALIDOS.sub("-", s)).strip()


def br(v, casas=2):
    return ("%.*f" % (casas, v)).replace(".", ",")


def codigo_do_nome(nome):
    m = re.match(r"\s*(\d+)", nome or "")
    return int(m.group(1)) if m else None


def listar_estilos(t, colecao):
    itens = []
    for sid in colecao:
        try:
            itens.append((sid, t.GetObject(sid, OpenMode.ForRead).Name))
        except Exception:
            pass
    return itens


def escolher_estilo(itens, filtro):
    """Exato > parcial > primeiro. Devolve (id, nome, aviso)."""
    if not itens:
        return None, None, "nenhum estilo no desenho"
    if filtro:
        f = norm(filtro)
        achados = [x for x in itens if norm(x[1]) == f] or [x for x in itens if f in norm(x[1])]
        if achados:
            return achados[0][0], achados[0][1], ""
        return itens[0][0], itens[0][1], "'%s' não encontrado; disponíveis: %s" % (
            filtro, " | ".join(x[1] for x in itens))
    return itens[0][0], itens[0][1], ""


# ----------------------------------------------------------------- principal
def executar():
    rodar = entrada(0, False)
    layer_eixo = entrada(1, "RUA_EIXO")
    layer_ali = entrada(2, "RUA_ALINHAMENTO")
    filtro_estilo = entrada(3, "", vazio_vira_padrao=False)
    filtro_rotulo = entrada(4, "", vazio_vira_padrao=False)
    sufixo = entrada(5, "alinhamento", vazio_vira_padrao=False)
    curvas = entrada(6, False)

    rel = ["=== CRIAR ALINHAMENTOS  |  %s ===" % (
        "CRIANDO (RODAR = True)" if rodar else "DIAGNÓSTICO (RODAR = False) - nada foi alterado")]

    adoc = Application.DocumentManager.MdiActiveDocument
    db = adoc.Database
    civdoc = CivilApplication.ActiveDocument

    with adoc.LockDocument():
        with db.TransactionManager.StartTransaction() as t:

            # --- alinhamentos que já existem (nomes e códigos)
            ids_ali = []
            for getter in ("GetAlignmentIds", "GetSitelessAlignmentIds"):
                try:
                    ids_ali.extend(list(getattr(civdoc, getter)()))
                except Exception:
                    pass
            existentes = {}
            for aid in ids_ali:
                try:
                    n = t.GetObject(aid, OpenMode.ForRead).Name
                    existentes[norm(n)] = n
                except Exception:
                    pass
            codigos_existentes = {}
            for n in existentes.values():
                c = codigo_do_nome(n)
                if c is not None:
                    codigos_existentes.setdefault(c, n)

            # --- eixos no model space
            ms = t.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead)
            eixos, layers_com_pline = [], set()
            for oid in ms:
                try:
                    if oid.ObjectClass.DxfName != "LWPOLYLINE":
                        continue
                    pl = t.GetObject(oid, OpenMode.ForRead)
                    layers_com_pline.add(pl.Layer)
                    if norm(pl.Layer) != norm(layer_eixo):
                        continue
                    nome, cod, larg = None, None, None
                    rb = pl.GetXDataForApplication(APPID)
                    if rb is not None:
                        for tv in rb.AsArray():
                            tc = int(tv.TypeCode)
                            if tc == 1000 and nome is None:
                                nome = str(tv.Value)
                            elif tc == 1070 and cod is None:
                                cod = int(tv.Value)
                            elif tc == 1040 and larg is None:
                                larg = float(tv.Value)
                    eixos.append({"id": oid, "nome": nome, "cod": cod, "larg": larg,
                                  "comp": float(pl.Length), "nv": int(pl.NumberOfVertices),
                                  "handle": str(pl.Handle)})
                except Exception as ex:
                    rel.append("ERRO lendo um objeto do model space: %s" % ex)

            rel.append("Layer dos eixos: %s  ->  %d polilinha(s)" % (layer_eixo, len(eixos)))
            if not eixos:
                rel.append("Nenhuma polilinha no layer '%s'. Layers com polilinhas no desenho: %s"
                           % (layer_eixo, " | ".join(sorted(layers_com_pline)) or "(nenhum)"))
                return rel

            # --- eixos sem XDATA recebem os próximos códigos livres
            usados = [e["cod"] for e in eixos if e["cod"] is not None] + list(codigos_existentes)
            prox = max(usados + [0]) + 1
            for e in sorted([e for e in eixos if e["cod"] is None], key=lambda e: e["handle"]):
                e["cod"], e["nome"] = prox, e["nome"] or "Eixo"
                prox += 1
            sem_xdata = sum(1 for e in eixos if e["larg"] is None)
            if sem_xdata:
                rel.append("  %d eixo(s) sem dados do KML: nomeados como 'Eixo' com códigos novos."
                           % sem_xdata)
            eixos.sort(key=lambda e: (e["cod"], e["nome"]))

            # --- layer dos alinhamentos
            lt = t.GetObject(db.LayerTableId, OpenMode.ForRead)
            layer_id = None
            for lid in lt:
                try:
                    if norm(t.GetObject(lid, OpenMode.ForRead).Name) == norm(layer_ali):
                        layer_id = lid
                        break
                except Exception:
                    pass
            if layer_id is not None:
                rel.append("Layer dos alinhamentos: %s (já existe)" % layer_ali)
            elif rodar:
                lt.UpgradeOpen()
                ltr = LayerTableRecord()
                ltr.Name = layer_ali
                try:
                    ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 1)
                except Exception:
                    pass
                layer_id = lt.Add(ltr)
                t.AddNewlyCreatedDBObject(ltr, True)
                rel.append("Layer dos alinhamentos: %s (criado)" % layer_ali)
            else:
                rel.append("Layer dos alinhamentos: %s (será criado)" % layer_ali)

            # --- estilos
            est_id, est_nome, est_aviso = escolher_estilo(
                listar_estilos(t, civdoc.Styles.AlignmentStyles), filtro_estilo)
            rot_id, rot_nome, rot_aviso = escolher_estilo(
                listar_estilos(t, civdoc.Styles.LabelSetStyles.AlignmentLabelSetStyles), filtro_rotulo)
            rel.append("Estilo: %s   |   Rótulos: %s   |   Curvas entre tangentes: %s"
                       % (est_nome, rot_nome, "sim" if curvas else "não"))
            for aviso in (est_aviso, rot_aviso):
                if aviso:
                    rel.append("  AVISO: " + aviso)
            if est_id is None or rot_id is None:
                rel.append("Sem estilo de alinhamento/rótulo no desenho. Rode num desenho criado "
                           "a partir do seu template do Civil 3D.")
                return rel
            rel.append("-" * 60)

            # --- criação
            criados = pulados = erros = 0
            conflito = False
            for e in eixos:
                nome_ali = limpar_nome("%02d %s %s" % (e["cod"], e["nome"], sufixo))
                info = "L=%s m, %d vért." % (br(e["comp"]), e["nv"])
                if norm(nome_ali) in existentes:
                    rel.append("PULOU  %s   (já existe)" % nome_ali)
                    pulados += 1
                    continue
                dono = codigos_existentes.get(e["cod"])
                if dono and norm(dono) != norm(nome_ali):
                    rel.append("  AVISO: código %02d já é usado por '%s'" % (e["cod"], dono))
                    conflito = True
                if not rodar:
                    rel.append("CRIAR  %s   (%s)" % (nome_ali, info))
                    existentes[norm(nome_ali)] = nome_ali
                    criados += 1
                    continue
                try:
                    opts = PolylineOptions()
                    opts.PlineId = e["id"]
                    opts.AddCurvesBetweenTangents = bool(curvas)
                    opts.EraseExistingEntities = False
                    aid = Alignment.Create(civdoc, opts, nome_ali, ObjectId.Null,
                                           layer_id, est_id, rot_id)
                    try:
                        al = t.GetObject(aid, OpenMode.ForRead)
                        info = "L=%s m, %d elemento(s)" % (br(al.Length), al.Entities.Count)
                    except Exception:
                        pass
                    rel.append("OK     %s   (%s)" % (nome_ali, info))
                    existentes[norm(nome_ali)] = nome_ali
                    codigos_existentes.setdefault(e["cod"], nome_ali)
                    criados += 1
                except Exception as ex:
                    rel.append("ERRO   %s   -> %s" % (nome_ali, str(ex).strip().splitlines()[0]))
                    erros += 1

            t.Commit()

    rel.append("-" * 60)
    rel.append("%d %s, %d pulado(s), %d erro(s)" % (
        criados, "criado(s)" if rodar else "a criar", pulados, erros))
    if conflito:
        rel.append("Códigos repetidos confundem o modo 'codigo' da rotina de corredores: gere o KML "
                   "de novo com --codigo-inicial acima do maior código já usado no desenho.")
    if not rodar and criados:
        rel.append("Confira a lista e rode de novo com RODAR = True.")
    return rel


try:
    OUT = executar()
except Exception:
    OUT = ["ERRO GERAL:"] + traceback.format_exc().splitlines()
