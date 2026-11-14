# -*- coding: utf-8 -*-
# =============================================================================
#  CRIACAO DE CORREDORES EM LOTE  -  Dynamo for Civil 3D  (no Python Script)
#  Premissas do projeto:
#    - UMA unica assembly (secao tipo) serve todas as ruas
#    - cada alinhamento ja tem seu proprio greide, com tipo "Projeto" (FG)
#    - ajustes de intersecao sao feitos manualmente depois
# =============================================================================
#
#  ENTRADAS (portas do no Python Script, nessa ordem):
#
#   IN[0]  RODAR           (bool)   False = so simula e reporta / True = cria
#   IN[1]  NOME_ASSEMBLY   (string) trecho do nome da secao tipo.
#                                   vazio = usa a unica assembly do desenho
#   IN[2]  CORREDOR_UNICO  (bool)   True  = 1 corredor com N baselines
#                                   False = 1 corredor por rua
#   IN[3]  NOME_BASE       (string) nome do corredor (ou prefixo, se um por rua)
#
#  SAIDA: relatorio linha a linha (lista de texto)
# =============================================================================

# ------------------------------------------------------- ajustes finos
FILTRO_NOME    = ""     # so processa alinhamentos que contenham este texto ("" = todos)
IGNORAR_OFFSET = True   # ignora alinhamentos Offset / CurbReturn (gerados pelo Civil)
TOL_ESTACA     = 0.01   # comprimento minimo de regiao, em unidades do desenho

import clr
import re
import unicodedata

clr.AddReference('AcMgd')
clr.AddReference('AcDbMgd')
clr.AddReference('AcCoreMgd')
clr.AddReference('AeccDbMgd')

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import OpenMode, SymbolUtilityServices
from Autodesk.Civil.ApplicationServices import CivilApplication
from Autodesk.Civil.DatabaseServices import Assembly

# ProfileType mora em Autodesk.Civil; algumas versoes reexportam em DatabaseServices
try:
    from Autodesk.Civil import ProfileType
except ImportError:
    from Autodesk.Civil.DatabaseServices import ProfileType

try:
    from Autodesk.Civil import AlignmentType
except ImportError:
    AlignmentType = None

# ------------------------------------------------------- entradas
RODAR          = IN[0] if len(IN) > 0 and IN[0] is not None else False
NOME_ASSEMBLY  = (IN[1] if len(IN) > 1 and IN[1] else "").strip()
CORREDOR_UNICO = IN[2] if len(IN) > 2 and IN[2] is not None else True
NOME_BASE      = (IN[3] if len(IN) > 3 and IN[3] else "CORREDOR").strip()

log = []

# ------------------------------------------------------- utilidades
try:
    TEXTO = unicode          # IronPython 2.7
except NameError:
    TEXTO = str              # CPython3


def normaliza(txt):
    """Minusculas, sem acento, espacos colapsados."""
    if txt is None:
        return ""
    t = unicodedata.normalize('NFKD', TEXTO(txt))
    t = u"".join(c for c in t if not unicodedata.combining(c))
    return re.sub(r'\s+', ' ', t).strip().lower()


def codigo(nome):
    """Numero NO INICIO do nome. '01 Rua X' -> '1'. 'Rua 25 de Marco' -> None."""
    m = re.match(r'^\s*(\d+)', nome or "")
    return str(int(m.group(1))) if m else None


def rotulo(nome):
    """Nome limpo da rua: tira o codigo inicial e a palavra-chave final."""
    n = re.sub(r'^\s*\d+\s*[-_.)]?\s*', '', nome or "")
    n = re.sub(r'(?i)\s*(alinhamento|alinham|align|eixo)\s*$', '', n)
    return n.strip() or (nome or "").strip()


def unico(nome, usados):
    """Garante nome inedito dentro do conjunto 'usados'."""
    base, i, n = nome, 1, nome
    while n in usados:
        i += 1
        n = u"%s (%d)" % (base, i)
    usados.add(n)
    return n


def coleta_assemblies(t, db, civdoc):
    """Lista (nome, ObjectId) das secoes tipo do desenho.

    CivilDocument NAO tem GetAssemblyIds em todas as versoes: assembly e uma
    entidade do model space. Tenta a API direta e cai para a varredura.
    """
    try:
        res = []
        for sid in civdoc.GetAssemblyIds():
            res.append((t.GetObject(sid, OpenMode.ForRead).Name, sid))
        return res
    except Exception:
        pass

    res = []
    ms = t.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead)
    for oid in ms:
        try:
            # AeccDbAssembly passa; AeccDbSubassembly cai no isinstance
            if 'assembly' not in oid.ObjectClass.Name.lower():
                continue
            s = t.GetObject(oid, OpenMode.ForRead)
            if isinstance(s, Assembly):
                res.append((s.Name, oid))
        except Exception:
            pass
    return res


def eh_offset(a):
    """True se o alinhamento for Offset ou CurbReturn (gerado automaticamente)."""
    if AlignmentType is None:
        return False
    try:
        return a.AlignmentType in (AlignmentType.Offset, AlignmentType.CurbReturn)
    except Exception:
        return False


# ------------------------------------------------------- documento
adoc   = Application.DocumentManager.MdiActiveDocument
civdoc = CivilApplication.ActiveDocument

ids_criados = []

with adoc.LockDocument():
    db = adoc.Database

    # =========================================================== leitura
    with db.TransactionManager.StartTransaction() as t:

        # ---------------------------------------- assembly unica
        asms = coleta_assemblies(t, db, civdoc)

        asm_id, asm_nome = None, None

        if not asms:
            log.append(u"ERRO  nenhuma secao tipo (assembly) no desenho.")
        elif NOME_ASSEMBLY:
            alvo = normaliza(NOME_ASSEMBLY)
            cand = [x for x in asms if alvo in normaliza(x[0])]
            if len(cand) == 1:
                asm_nome, asm_id = cand[0]
            elif not cand:
                log.append(u"ERRO  nenhuma assembly contem '%s'. Disponiveis: %s"
                           % (NOME_ASSEMBLY, u", ".join(x[0] for x in asms)))
            else:
                log.append(u"ERRO  '%s' casa com %d assemblies: %s"
                           % (NOME_ASSEMBLY, len(cand),
                              u", ".join(x[0] for x in cand)))
        elif len(asms) == 1:
            asm_nome, asm_id = asms[0]
        else:
            log.append(u"ERRO  ha %d assemblies no desenho. Preencha NOME_ASSEMBLY. "
                       u"Disponiveis: %s" % (len(asms), u", ".join(x[0] for x in asms)))

        if asm_id is not None:
            log.append(u"Secao tipo: %s" % asm_nome)
            try:
                if t.GetObject(asm_id, OpenMode.ForRead).GetSubassemblyIds().Count == 0:
                    log.append(u"AVISO  a assembly '%s' esta vazia (sem subassemblies)."
                               % asm_nome)
            except Exception:
                pass

        # ---------------------------------------- alinhamentos e greides
        pares = []
        filtro = normaliza(FILTRO_NOME)

        for aid in civdoc.GetAlignmentIds():
            a = t.GetObject(aid, OpenMode.ForRead)
            nome_ali = a.Name

            if filtro and filtro not in normaliza(nome_ali):
                continue
            if IGNORAR_OFFSET and eh_offset(a):
                log.append(u"IGNORA %s: alinhamento Offset/CurbReturn." % nome_ali)
                continue

            # ---- greide: perfil de PROJETO (FG) do proprio alinhamento
            fg = []
            for pid in a.GetProfileIds():
                p = t.GetObject(pid, OpenMode.ForRead)
                if p.ProfileType == ProfileType.FG:
                    fg.append((p.Name, pid, p.StartingStation, p.EndingStation))

            if not fg:
                log.append(u"PULOU  %s: sem perfil de projeto (FG)." % nome_ali)
                continue

            if len(fg) > 1:
                pref = [c for c in fg
                        if re.search(r'greide|projeto', normaliza(c[0]))] or fg
                escolhido = pref[0]
                log.append(u"AVISO  %s: %d perfis FG, usando '%s'."
                           % (nome_ali, len(fg), escolhido[0]))
            else:
                escolhido = fg[0]

            gre_nome, gre_id, gre_ini, gre_fim = escolhido

            # ---- regiao limitada a intersecao alinhamento x greide
            ini = max(a.StartingStation, gre_ini)
            fim = min(a.EndingStation,  gre_fim)

            if fim - ini < TOL_ESTACA:
                log.append(u"PULOU  %s: greide '%s' nao cobre o alinhamento "
                           u"(ali %.2f-%.2f / greide %.2f-%.2f)."
                           % (nome_ali, gre_nome, a.StartingStation,
                              a.EndingStation, gre_ini, gre_fim))
                continue

            if (a.StartingStation < gre_ini - TOL_ESTACA or
                    a.EndingStation > gre_fim + TOL_ESTACA):
                log.append(u"AVISO  %s: greide mais curto que o eixo, regiao "
                           u"limitada a %.2f - %.2f." % (nome_ali, ini, fim))

            pares.append({
                'cod':      codigo(nome_ali) or '--',
                'rua':      rotulo(nome_ali),
                'ali_nome': nome_ali,
                'ali_id':   aid,
                'gre_nome': gre_nome,
                'gre_id':   gre_id,
                'ini':      ini,
                'fim':      fim,
            })

        # ---------------------------------------- relatorio previo
        log.insert(0, u"=== %d rua(s) prontas para virar corredor ===" % len(pares))
        for p in pares:
            log.append(u"  [%s] %s  |  greide: %s  |  est %.2f a %.2f"
                       % (p['cod'], p['rua'], p['gre_nome'], p['ini'], p['fim']))

        # ---------------------------------------- criacao
        if RODAR and pares and asm_id is not None:

            existentes = set()
            for cid in civdoc.CorridorCollection:
                try:
                    existentes.add(t.GetObject(cid, OpenMode.ForRead).Name)
                except Exception:
                    pass

            def add_region(baseline, nome_reg, ini, fim):
                """Cobre variacoes de assinatura entre versoes do Civil 3D."""
                try:
                    return baseline.BaselineRegions.Add(nome_reg, asm_id, ini, fim)
                except Exception as e:
                    log.append(u"AVISO  regiao '%s' com estacas falhou (%s); "
                               u"tentando sem estacas." % (nome_reg, e))
                    return baseline.BaselineRegions.Add(nome_reg, asm_id)

            if CORREDOR_UNICO:
                nome_corr = unico(NOME_BASE, existentes)
                cid = civdoc.CorridorCollection.Add(nome_corr)
                corr = t.GetObject(cid, OpenMode.ForWrite)

                usados_bl = set()
                ok = 0
                for p in pares:
                    bl_nome = unico(u"BL %s %s" % (p['cod'], p['rua']), usados_bl)
                    try:
                        bl = corr.Baselines.Add(bl_nome, p['ali_id'], p['gre_id'])
                        add_region(bl, u"RG %s %s" % (p['cod'], p['rua']),
                                   p['ini'], p['fim'])
                        ok += 1
                    except Exception as e:
                        log.append(u"ERRO   %s: %s" % (p['ali_nome'], e))

                ids_criados.append(cid)
                log.append(u"OK  corredor '%s' com %d de %d baselines."
                           % (nome_corr, ok, len(pares)))

            else:
                for p in pares:
                    nome_corr = unico(u"%s %s %s" % (NOME_BASE, p['cod'], p['rua']),
                                      existentes)
                    try:
                        cid = civdoc.CorridorCollection.Add(nome_corr)
                        corr = t.GetObject(cid, OpenMode.ForWrite)
                        bl = corr.Baselines.Add(u"BL %s" % p['rua'],
                                                p['ali_id'], p['gre_id'])
                        add_region(bl, u"RG %s" % p['rua'], p['ini'], p['fim'])
                        ids_criados.append(cid)
                        log.append(u"OK  %s" % nome_corr)
                    except Exception as e:
                        log.append(u"ERRO   %s: %s" % (p['ali_nome'], e))

        elif not RODAR:
            log.append(u"--- MODO SIMULACAO: ligue RODAR para criar de fato. ---")

        t.Commit()

    # =========================================================== rebuild
    # feito depois do commit: corredor com muitas baselines costuma falhar
    # o rebuild enquanto a transacao de criacao ainda esta aberta
    if ids_criados:
        with db.TransactionManager.StartTransaction() as t2:
            for cid in ids_criados:
                try:
                    c = t2.GetObject(cid, OpenMode.ForWrite)
                    c.Rebuild()
                    log.append(u"REBUILD ok: %s" % c.Name)
                except Exception as e:
                    log.append(u"AVISO  rebuild falhou: %s" % e)
            t2.Commit()

OUT = log
