# -*- coding: utf-8 -*-
# =====================================================================
#  GREIDE COLADO + CORREDOR DE RECAPEAMENTO
#  Civil 3D 2026/2027 - Dynamo (PythonNet3)
#
#  O que faz, na ordem:
#   1. Amostra a superficie da pista existente no eixo do alinhamento
#      e gera o GREIDE COLADO (desvio maximo no eixo = TOLERANCIA).
#   2. Gera o GREIDE DE PROJETO = greide colado + ALTURA (0,22 m):
#      mesmos PVIs e curvas, cotas + 0,22 -> paralelo exato.
#   3. Cria o corredor sobre o greide de projeto com a MONTAGEM
#      informada (11,00 m = 3,50 faixa + 2,00 acostamento por lado,
#      -2%), frequencia 1,00 m, e a superficie do corredor
#      (links Top, correcao de saliencia = links superiores,
#      limite = extensao do corredor).
#   4. Troca o perfil "projeto" da faixa de cotas do grafico do
#      perfil para o greide de projeto.
#
#  Entradas (nos do Dynamo):
#   IN[0] RODAR           False = so diagnostico (NAO altera o desenho)
#   IN[1] MODO            "tudo" | "greide" | "corredor"
#   IN[2] ALINHAMENTO     ex.: BR-222
#   IN[3] SUPERFICIE      superficie da pista existente
#   IN[4] MONTAGEM        montagem de 11,00 m
#   IN[5] ESTACA INICIAL  14078 | 14078+10 | 281+560 (km) | vazio = auto
#   IN[6] ESTACA FINAL    idem (o formato e deduzido pela faixa do eixo)
#   IN[7] TOLERANCIA (m)  desvio maximo greide x pista no eixo (0,01)
#   IN[8] PASSO (m)       amostragem da superficie no eixo (2)
#   IN[9] ALTURA (m)      greide de projeto acima do colado (0,22)
#
#  Nada e apagado. Se ja existir greide/corredor com o mesmo nome,
#  ele e reutilizado (apague ou renomeie para gerar de novo).
#  Nas curvas horizontais a declividade vem da SUPERELEVACAO do
#  alinhamento: sem superelevacao calculada, as curvas ficam a -2%.
#  Cada etapa roda numa transacao propria: se der erro, a etapa e
#  desfeita e nada dela fica no desenho.
# =====================================================================

# === ALGORITMO INICIO ===
import math
import bisect

ESTACA_M = 20.0


def fmt(v, n=3):
    return (("%%.%df" % n) % v).replace(".", ",")


def estaca_txt(sta):
    n = int(math.floor(sta / ESTACA_M + 1e-9))
    r = sta - n * ESTACA_M
    if r < 0.0:
        r = 0.0
    return "%d+%s" % (n, fmt(r, 2))


def km_txt(sta):
    km = int(math.floor(sta / 1000.0 + 1e-9))
    return "km %d+%s" % (km, fmt(sta - km * 1000.0, 2))


def interpretar_estaca(txt, s0, s1):
    """Aceita estaca de 20 m (14078, 14078+10), km+m (281+560) ou
    estacao em metros (281560). Escolhe a leitura que cai dentro do
    alinhamento [s0, s1]. Retorna (estacao_m, descricao) ou (None, msg)."""
    t = str(txt).strip().replace(" ", "").replace(",", ".")
    if t == "":
        return None, "vazio"
    try:
        if "+" in t:
            a, b = t.split("+", 1)
            a, b = float(a), float(b)
            cands = [("estaca de 20 m", a * ESTACA_M + b), ("km+m", a * 1000.0 + b)]
        else:
            v = float(t)
            cands = [("estaca de 20 m", v * ESTACA_M), ("estacao em metros", v)]
    except Exception:
        return None, "'%s' nao e uma estaca valida" % txt
    for nome, s in cands:
        if s0 - 0.001 <= s <= s1 + 0.001:
            return s, nome
    return None, "'%s' fica fora do alinhamento em qualquer leitura" % txt


def simplificar(pts, tol):
    """Douglas-Peucker com desvio VERTICAL. pts = [(estacao, cota)].
    Os PVIs escolhidos sao pontos reais da pista (amostras)."""
    n = len(pts)
    if n <= 2:
        return list(pts)
    manter = [False] * n
    manter[0] = manter[-1] = True
    pilha = [(0, n - 1)]
    while pilha:
        i, j = pilha.pop()
        if j <= i + 1:
            continue
        si, zi = pts[i]
        sj, zj = pts[j]
        g = (zj - zi) / (sj - si)
        dmax = -1.0
        kmax = -1
        for k in range(i + 1, j):
            s, z = pts[k]
            d = abs(z - (zi + g * (s - si)))
            if d > dmax:
                dmax, kmax = d, k
        if dmax > tol:
            manter[kmax] = True
            pilha.append((i, kmax))
            pilha.append((kmax, j))
    return [pts[k] for k in range(n) if manter[k]]


def _cota_parabola(s, sk, zk, L, g1, g2):
    x = s - (sk - L / 2.0)
    return zk - g1 * L / 2.0 + g1 * x + (g2 - g1) / (2.0 * L) * x * x


def curvas_verticais(pvis, amostras, tol, l_min):
    """Parabola simetrica em cada PVI interno, com o maior comprimento
    que mantem todas as amostras dentro da tolerancia e nao invade as
    curvas vizinhas. Retorna [(indice_pvi, L, g1, g2)]."""
    sts = [a[0] for a in amostras]
    res = []
    for k in range(1, len(pvis) - 1):
        s0, z0 = pvis[k - 1]
        sk, zk = pvis[k]
        s2, z2 = pvis[k + 1]
        t1 = sk - s0
        t2 = s2 - sk
        g1 = (zk - z0) / t1
        g2 = (z2 - zk) / t2
        A = abs(g2 - g1)
        if A < 1e-6:
            continue
        L = min(8.0 * tol / A, 0.95 * t1, 0.95 * t2)
        aceito = None
        while L >= l_min:
            L = math.floor(L * 10.0) / 10.0
            if L < l_min:
                break
            lo = bisect.bisect_left(sts, sk - L / 2.0)
            hi = bisect.bisect_right(sts, sk + L / 2.0)
            ok = True
            for j in range(lo, hi):
                s, z = amostras[j]
                if abs(_cota_parabola(s, sk, zk, L, g1, g2) - z) > tol:
                    ok = False
                    break
            if ok:
                aceito = L
                break
            L = L * 0.5
        if aceito is not None:
            res.append((k, aceito, g1, g2))
    return res


class GreideSimulado(object):
    """Cota do greide (tangentes + parabolas) sem tocar no desenho."""

    def __init__(self, pvis, curvas):
        self.pvis = pvis
        self.sts = [p[0] for p in pvis]
        self.cmap = dict((k, (L, g1, g2)) for (k, L, g1, g2) in curvas)

    def cota(self, s):
        n = len(self.pvis)
        i = bisect.bisect_right(self.sts, s) - 1
        i = max(0, min(i, n - 2))
        for k in (i, i + 1):
            c = self.cmap.get(k)
            if c:
                L, g1, g2 = c
                sk, zk = self.pvis[k]
                if sk - L / 2.0 <= s <= sk + L / 2.0:
                    return _cota_parabola(s, sk, zk, L, g1, g2)
        s0, z0 = self.pvis[i]
        s1, z1 = self.pvis[i + 1]
        return z0 + (z1 - z0) / (s1 - s0) * (s - s0)


def estatisticas(amostras, greide, tol):
    dmax = 0.0
    smax = None
    soma = 0.0
    fora = 0
    for s, z in amostras:
        d = greide.cota(s) - z
        soma += abs(d)
        if abs(d) > tol + 1e-9:
            fora += 1
        if abs(d) > abs(dmax):
            dmax, smax = d, s
    return dmax, smax, soma / max(1, len(amostras)), fora
# === ALGORITMO FIM ===


# ============================ CIVIL 3D ================================
import os
import traceback
import clr

clr.AddReference("AcMgd")
clr.AddReference("AcCoreMgd")
clr.AddReference("AcDbMgd")
clr.AddReference("AecBaseMgd")
clr.AddReference("AecPropDataMgd")
clr.AddReference("AeccDbMgd")

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import OpenMode, ObjectId
from Autodesk.AutoCAD.Geometry import Point2d
from Autodesk.Civil.ApplicationServices import CivilApplication
import Autodesk.Civil as CIVIL
import Autodesk.Civil.DatabaseServices as C3D


def _entrada(i, padrao):
    try:
        v = IN[i]
    except Exception:
        return padrao
    if v is None:
        return padrao
    if isinstance(v, str) and v.strip() == "":
        return padrao
    return v


RODAR = bool(_entrada(0, False))
MODO = str(_entrada(1, "tudo")).strip().lower()
ALINH = str(_entrada(2, "BR-222")).strip()
SUPERF = str(_entrada(3, "")).strip()
MONTAGEM = str(_entrada(4, "")).strip()
EST_INI = str(_entrada(5, "")).strip()
EST_FIM = str(_entrada(6, "")).strip()
TOL = float(_entrada(7, 0.01))
PASSO = float(_entrada(8, 2.0))
ALTURA = float(_entrada(9, 0.22))

# ---- ajustes finos -------------------------------------------------
CURVAS = True             # concordancias verticais limitadas pela TOLERANCIA
L_MIN_CURVA = 2.0         # m - curvas menores que isso sao omitidas
FREQUENCIA = 1.0          # m - frequencia do corredor
ESTILO_SUP = "TRI_PTO_BRD"
ATUALIZAR_FAIXA = True    # faixa COTAS TERRENO/PROJETO -> greide de projeto
NOME_COLADO = "GREIDE-COLADO-" + ALINH
NOME_PROJ = "GREIDE-PROJ-" + ALINH
NOME_CORREDOR = ALINH + "-RECAP"
NOME_SUP_COR = "SUP-" + ALINH + "-RECAP"

REL = []


def log(*partes):
    REL.append(" ".join([str(p) for p in partes]))


def erro_curto(e):
    tb = traceback.format_exc().strip().splitlines()
    return "%s | %s" % (e, " / ".join(tb[-3:]))


def enum(tipo, membro):
    for ns in (C3D, CIVIL):
        try:
            return getattr(getattr(ns, tipo), membro)
        except Exception:
            pass
    raise Exception("enum %s.%s nao encontrado" % (tipo, membro))


def abrir(t, oid, escrita=False):
    return t.GetObject(oid, OpenMode.ForWrite if escrita else OpenMode.ForRead)


def achar(t, ids, nome, parcial=False):
    """Nome exato (sem diferenciar maiusculas). Com parcial=True, aceita
    tambem um trecho do nome, desde que so um objeto combine."""
    alvo = nome.strip().upper()
    parecidos = []
    for oid in ids:
        try:
            o = abrir(t, oid)
            n = str(o.Name).strip().upper()
            if n == alvo:
                return o
            if parcial and alvo and alvo in n:
                parecidos.append(o)
        except Exception:
            pass
    if len(parecidos) == 1:
        log("  ('%s' entendido como '%s')" % (nome, parecidos[0].Name))
        return parecidos[0]
    return None


def listar(t, ids, limite=40):
    out = []
    for oid in ids:
        try:
            out.append(str(abrir(t, oid).Name))
        except Exception:
            pass
    if len(out) > limite:
        return out[:limite] + ["... (+%d)" % (len(out) - limite)]
    return out


def xy(al, sta, off=0.0):
    r = al.PointLocation(float(sta), float(off), 0.0, 0.0)
    if isinstance(r, tuple):
        nums = [v for v in r if isinstance(v, float)]
        if len(nums) >= 2:
            return nums[-2], nums[-1]
    raise Exception("PointLocation: retorno inesperado %r" % (r,))


def extensao(sup):
    try:
        e = sup.GeometricExtents
        return (e.MinPoint.X, e.MinPoint.Y, e.MaxPoint.X, e.MaxPoint.Y)
    except Exception:
        return None


def cota_sup(sup, ext, x, y):
    if ext is not None and not (ext[0] <= x <= ext[2] and ext[1] <= y <= ext[3]):
        return None
    try:
        z = float(sup.FindElevationAtXY(x, y))
    except Exception:
        return None
    if math.isnan(z) or abs(z) > 1.0e6:
        return None
    return z


def cota_no_eixo(al, sup, ext, sta, off=0.0):
    try:
        x, y = xy(al, sta, off)
    except Exception:
        return None
    return cota_sup(sup, ext, x, y)


def trechos_cobertos(al, sup, ext):
    s0 = al.StartingStation
    s1 = al.EndingStation
    n = int(math.floor((s1 - s0) / ESTACA_M))
    trechos = []
    atual = None
    for i in range(n + 1):
        s = s0 + i * ESTACA_M
        if cota_no_eixo(al, sup, ext, s) is not None:
            if atual is None:
                atual = [s, s]
            else:
                atual[1] = s
        elif atual is not None:
            trechos.append(atual)
            atual = None
    if atual is not None:
        trechos.append(atual)
    return trechos


def estender_pontas(al, sup, ext, a, b):
    s = a
    for i in range(1, int(ESTACA_M) + 1):
        c = a - i
        if c < al.StartingStation or cota_no_eixo(al, sup, ext, c) is None:
            break
        s = c
    a2 = s
    s = b
    for i in range(1, int(ESTACA_M) + 1):
        c = b + i
        if c > al.EndingStation or cota_no_eixo(al, sup, ext, c) is None:
            break
        s = c
    return a2, s


def amostrar(al, sup, ext, a, b, passo):
    estacoes = [a]
    k = int(math.floor(a / passo)) + 1
    while k * passo < b - 0.01:
        if k * passo > a + 0.01:
            estacoes.append(k * passo)
        k += 1
    estacoes.append(b)
    pts = []
    falhas = []
    for s in estacoes:
        z = cota_no_eixo(al, sup, ext, s)
        if z is None:
            falhas.append(s)
        else:
            pts.append((s, z))
    return pts, falhas


def info_super(al):
    try:
        curvas = al.SuperelevationCurves
        try:
            n = int(curvas.Count)
        except Exception:
            n = len(curvas)
    except Exception:
        return "nao foi possivel ler"
    if n == 0:
        return "NENHUMA calculada -> nas curvas o corredor fica a -2% ate voce calcular"
    return "%d curva(s) com superelevacao -> o corredor segue a superelevacao nelas" % n


def gravar_csv(pvis, curvas):
    try:
        pasta = os.path.join(os.path.expanduser("~"), "Documents")
        if not os.path.isdir(pasta):
            pasta = os.path.expanduser("~")
        seguro = "".join(c if (c.isalnum() or c in "-_") else "_" for c in ALINH)
        cam = os.path.join(pasta, "greide_colado_%s.csv" % seguro)
        cmap = dict((k, L) for (k, L, g1, g2) in curvas)
        f = open(cam, "w", encoding="utf-8-sig")
        try:
            f.write("PVI;ESTACA;ESTACAO (m);COTA COLADO;COTA PROJETO (+%s);RAMPA SEGUINTE (%%);CURVA L (m)\n" % fmt(ALTURA, 2))
            for i, (s, z) in enumerate(pvis):
                rampa = ""
                if i < len(pvis) - 1:
                    rampa = fmt((pvis[i + 1][1] - z) / (pvis[i + 1][0] - s) * 100.0, 3)
                f.write("%d;%s;%s;%s;%s;%s;%s\n" % (
                    i + 1, estaca_txt(s), fmt(s, 3), fmt(z, 3), fmt(z + ALTURA, 3),
                    rampa, fmt(cmap[i], 1) if i in cmap else ""))
        finally:
            f.close()
        return cam
    except Exception as e:
        return "nao gravado (%s)" % e


def resumo_montagem(t, asm):
    linhas = []
    chaves_uteis = ("width", "slope", "side", "super", "largura", "pendente", "lado")
    try:
        for g in asm.Groups:
            grp = abrir(t, g) if isinstance(g, ObjectId) else g
            for sid in grp.GetSubassemblyIds():
                sa = abrir(t, sid)
                itens = []
                for col in (sa.ParamsDouble, sa.ParamsLong, sa.ParamsString):
                    for prm in col:
                        chave = str(getattr(prm, "Key", getattr(prm, "DisplayName", "?")))
                        if any(w in chave.lower() for w in chaves_uteis):
                            itens.append("%s=%s" % (chave, prm.Value))
                linhas.append("    - %s: %s" % (sa.Name, "; ".join(itens[:8])))
    except Exception as e:
        linhas.append("    (nao consegui ler as subassemblies: %s)" % e)
    return linhas


# ------------------------------ ETAPAS -------------------------------

def planejar(t, civdoc):
    plano = {"al": None, "sup": None, "asm": None, "pvis": None}
    al = achar(t, civdoc.GetAlignmentIds(), ALINH, True)
    if al is None:
        log("Alinhamento '%s' nao encontrado. Existentes: %s" % (ALINH, ", ".join(listar(t, civdoc.GetAlignmentIds()))))
        return None
    plano["al"] = al.ObjectId
    log("Alinhamento: %s (estaca %s a %s = %s a %s)" % (
        al.Name, estaca_txt(al.StartingStation), estaca_txt(al.EndingStation),
        km_txt(al.StartingStation), km_txt(al.EndingStation)))
    log("Superelevacao: " + info_super(al))
    log("Perfis do alinhamento: " + (", ".join(listar(t, al.GetProfileIds())) or "(nenhum)"))

    sup = None
    if SUPERF:
        sup = achar(t, civdoc.GetSurfaceIds(), SUPERF, True)
        if sup is None:
            log("Superficie '%s' nao encontrada. Existentes: %s" % (SUPERF, ", ".join(listar(t, civdoc.GetSurfaceIds()))))
            return None
        plano["sup"] = sup.ObjectId

    if MODO in ("tudo", "greide"):
        if sup is None:
            log("Informe SUPERFICIE (pista existente). Superficies no desenho: " + ", ".join(listar(t, civdoc.GetSurfaceIds())))
            return None
        ext = extensao(sup)
        a = b = None
        for rotulo, txt in (("ESTACA INICIAL", EST_INI), ("ESTACA FINAL", EST_FIM)):
            if not txt:
                continue
            s, como = interpretar_estaca(txt, al.StartingStation, al.EndingStation)
            if s is None:
                log("%s: %s. Alinhamento vai de %s a %s." % (
                    rotulo, como, estaca_txt(al.StartingStation), estaca_txt(al.EndingStation)))
                return None
            log("%s '%s' lida como %s -> estaca %s (%s)" % (rotulo, txt, como, estaca_txt(s), km_txt(s)))
            if rotulo == "ESTACA INICIAL":
                a = s
            else:
                b = s
        if a is not None and b is not None and b < a:
            a, b = b, a
        if a is None or b is None:
            trechos = trechos_cobertos(al, sup, ext)
            if not trechos:
                log("A superficie '%s' nao cobre o eixo de %s." % (sup.Name, al.Name))
                return None
            log("Trechos do eixo sobre a superficie: " + "; ".join(
                "%s a %s" % (estaca_txt(x), estaca_txt(y)) for x, y in trechos[:10]))
            maior = max(trechos, key=lambda r: r[1] - r[0])
            aa, bb = estender_pontas(al, sup, ext, maior[0], maior[1])
            if a is None:
                a = aa
            if b is None:
                b = bb
        a = max(a, al.StartingStation)
        b = min(b, al.EndingStation)
        if b - a < 2 * PASSO:
            log("Trecho curto demais: %s a %s." % (estaca_txt(a), estaca_txt(b)))
            return None
        amostras, falhas = amostrar(al, sup, ext, a, b, PASSO)
        if falhas:
            log("Atencao: %d ponto(s) do eixo fora da superficie foram ignorados (ex.: %s)." % (
                len(falhas), ", ".join(estaca_txt(s) for s in falhas[:5])))
        if len(amostras) < 3:
            log("Amostras insuficientes no trecho.")
            return None
        a, b = amostras[0][0], amostras[-1][0]
        pvis = simplificar(amostras, TOL)
        curvas = curvas_verticais(pvis, amostras, TOL, L_MIN_CURVA) if CURVAS else []
        dmax, smax, dmed, fora = estatisticas(amostras, GreideSimulado(pvis, curvas), TOL)
        log("")
        log("GREIDE COLADO (simulado): estaca %s a %s (%s a %s) = %s m" % (
            estaca_txt(a), estaca_txt(b), km_txt(a), km_txt(b), fmt(b - a, 2)))
        log("  %d amostras a cada %s m | tolerancia %s m" % (len(amostras), fmt(PASSO, 1), fmt(TOL, 3)))
        log("  %d PVIs | %d concordancias verticais" % (len(pvis), len(curvas)))
        log("  desvio maximo %s m em %s | desvio medio %s m | %d amostra(s) acima da tolerancia" % (
            fmt(dmax, 3), estaca_txt(smax) if smax is not None else "-", fmt(dmed, 4), fora))
        log("  greide de projeto = colado + %s m (paralelo)" % fmt(ALTURA, 2))
        log("  Tabela completa de PVIs: " + gravar_csv(pvis, curvas))
        cmap = dict((k, L) for (k, L, g1, g2) in curvas)
        log("  Primeiros PVIs (estaca | cota colado | cota projeto | curva):")
        for i, (s, z) in enumerate(pvis[:20]):
            log("   %3d  %s  %s  %s  %s" % (i + 1, estaca_txt(s), fmt(z, 3), fmt(z + ALTURA, 3),
                                        ("L=%s" % fmt(cmap[i], 1)) if i in cmap else ""))
        if len(pvis) > 20:
            log("   ... (+%d no CSV)" % (len(pvis) - 20))
        plano.update(pvis=pvis, curvas=curvas, amostras=amostras)

    if MODO in ("tudo", "corredor"):
        log("")
        if not MONTAGEM:
            log("Informe MONTAGEM. Montagens no desenho: " + (", ".join(listar(t, civdoc.AssemblyCollection)) or "(nenhuma)"))
        else:
            asm = achar(t, civdoc.AssemblyCollection, MONTAGEM, True)
            if asm is None:
                log("Montagem '%s' nao encontrada. Existentes: %s" % (
                    MONTAGEM, ", ".join(listar(t, civdoc.AssemblyCollection)) or "(nenhuma)"))
            else:
                plano["asm"] = asm.ObjectId
                log("Montagem '%s' (confira larguras 3,50 + 2,00 e pendentes -2%%):" % asm.Name)
                for linha in resumo_montagem(t, asm):
                    log(linha)
        log("Corredores existentes: " + (", ".join(listar(t, civdoc.CorridorCollection)) or "(nenhum)"))
    return plano


def estilos_perfil(t, civdoc, al):
    layer = al.LayerId
    estilo = None
    try:
        fg = enum("ProfileType", "FG")
        for pid in al.GetProfileIds():
            p = abrir(t, pid)
            if p.ProfileType == fg:
                estilo = p.StyleId
                layer = p.LayerId
                break
    except Exception:
        pass
    if estilo is None:
        estilo = civdoc.Styles.ProfileStyles[0]
    try:
        rot = civdoc.Styles.LabelSetStyles.ProfileLabelSetStyles[0]
    except Exception:
        rot = ObjectId.Null
    return layer, estilo, rot


def criar_perfil(t, civdoc, al, nome, pvis, curvas, dz):
    existente = achar(t, al.GetProfileIds(), nome)
    if existente is not None:
        log("  '%s' ja existe -> reutilizado (apague ou renomeie para gerar de novo)." % nome)
        return existente
    layer, estilo, rot = estilos_perfil(t, civdoc, al)
    pid = C3D.Profile.CreateByLayout(nome, al.ObjectId, layer, estilo, rot)
    p = abrir(t, pid, True)
    tangentes = []
    for i in range(len(pvis) - 1):
        pa = Point2d(pvis[i][0], pvis[i][1] + dz)
        pb = Point2d(pvis[i + 1][0], pvis[i + 1][1] + dz)
        tangentes.append(p.Entities.AddFixedTangent(pa, pb))
    crista = enum("VerticalCurveType", "Crest")
    sag = enum("VerticalCurveType", "Sag")
    ok = 0
    falhas = 0
    ultimo = ""
    for (k, L, g1, g2) in curvas:
        try:
            p.Entities.AddFreeSymmetricParabolaByLength(
                tangentes[k - 1].EntityId, tangentes[k].EntityId,
                crista if g2 < g1 else sag, float(L), True)
            ok += 1
        except Exception as e:
            falhas += 1
            ultimo = str(e)
    extra = (" (%d curvas falharam: %s)" % (falhas, ultimo)) if falhas else ""
    log("  '%s' criado: %d PVIs, %d curvas%s." % (nome, len(pvis), ok, extra))
    return p


def conferir_perfil(p, amostras, dz):
    dmax = 0.0
    smax = None
    soma = 0.0
    n = 0
    for s, z in amostras:
        try:
            zp = p.ElevationAt(s)
        except Exception:
            continue
        d = zp - (z + dz)
        soma += abs(d)
        n += 1
        if abs(d) > abs(dmax):
            dmax, smax = d, s
    if n == 0:
        return "nao foi possivel conferir"
    return "desvio max %s m em %s, medio %s m (%d pontos)" % (
        fmt(dmax, 3), estaca_txt(smax) if smax is not None else "-", fmt(soma / n, 4), n)


def etapa_greides(t, civdoc, plano):
    log("")
    log("ETAPA GREIDES")
    al = abrir(t, plano["al"])
    p1 = criar_perfil(t, civdoc, al, NOME_COLADO, plano["pvis"], plano["curvas"], 0.0)
    p2 = criar_perfil(t, civdoc, al, NOME_PROJ, plano["pvis"], plano["curvas"], ALTURA)
    log("  Conferencia colado x pista existente: " + conferir_perfil(p1, plano["amostras"], 0.0))
    log("  Conferencia projeto x (pista + %s): %s" % (fmt(ALTURA, 2), conferir_perfil(p2, plano["amostras"], ALTURA)))
    return p2.ObjectId


def ajustar_frequencia(reg):
    aas = reg.AppliedAssemblySetting
    feitos = []
    for nome in ("FrequencyAlongTangents", "FrequencyAlongCurves", "FrequencyAlongSpirals",
                 "FrequencyAlongProfileCurves", "FrequencyAlongTargetCurves"):
        try:
            setattr(aas, nome, FREQUENCIA)
            feitos.append(nome.replace("FrequencyAlong", ""))
        except Exception:
            pass
    try:
        reg.AppliedAssemblySetting = aas
    except Exception:
        pass
    try:
        conf = fmt(reg.AppliedAssemblySetting.FrequencyAlongTangents, 2)
    except Exception:
        conf = "?"
    log("  Frequencia %s m aplicada em: %s (tangentes agora = %s m)" % (fmt(FREQUENCIA, 2), ", ".join(feitos), conf))


def criar_superficie_corredor(civdoc, cor):
    for cs in cor.CorridorSurfaces:
        if str(cs.Name).strip().upper() == NOME_SUP_COR.upper():
            log("  Superficie '%s' ja existe no corredor -> mantida." % NOME_SUP_COR)
            return cs
    cs = None
    try:
        estilo = civdoc.Styles.SurfaceStyles[ESTILO_SUP]
        cs = cor.CorridorSurfaces.Add(NOME_SUP_COR, estilo)
    except Exception:
        log("  (estilo '%s' nao aplicado; usando o padrao)" % ESTILO_SUP)
        cs = cor.CorridorSurfaces.Add(NOME_SUP_COR)
    cs.AddLinkCode("Top", True)
    try:
        cs.OverhangCorrection = enum("OverhangCorrectionType", "TopLinks")
    except Exception as e:
        log("  (correcao de saliencia nao aplicada: %s)" % e)
    try:
        cs.Boundaries.AddCorridorExtentsBoundary("EXTENSAO DO CORREDOR")
    except Exception as e:
        log("  (limite pela extensao do corredor nao aplicado: %s)" % e)
    log("  Superficie '%s': links Top (como linha de ruptura), saliencia = links superiores." % NOME_SUP_COR)
    return cs


def atualizar_faixas(t, al, prof):
    fg = enum("ProfileType", "FG")
    trocas = 0
    for pvid in al.GetProfileViewIds():
        try:
            pv = abrir(t, pvid, True)
            itens = pv.Bands.GetBottomBandItems()
            mudou = False
            for it in itens:
                try:
                    p2 = it.Profile2Id
                    if p2.IsNull or p2 == prof.ObjectId:
                        continue
                    if abrir(t, p2).ProfileType == fg:
                        it.Profile2Id = prof.ObjectId
                        mudou = True
                except Exception:
                    pass
            if mudou:
                pv.Bands.SetBottomBandItems(itens)
                trocas += 1
        except Exception as e:
            log("  (faixa do grafico nao atualizada: %s)" % e)
    return trocas


def conferir_corredor(t, al, sup_id, cs, a, b):
    try:
        s_cor = abrir(t, cs.SurfaceId)
        e_cor = extensao(s_cor)
        sup = abrir(t, sup_id)
        e_sup = extensao(sup)
    except Exception as e:
        log("  (conferencia da superficie do corredor nao disponivel: %s)" % e)
        return
    n = max(1, min(80, int((b - a) // ESTACA_M)))
    log("  Espessura = superficie do corredor - pista existente (%d secoes):" % (n + 1))
    lidos = 0
    for off, nome in ((-5.45, "bordo acost. E"), (-3.5, "bordo faixa E"), (0.0, "eixo"),
                      (3.5, "bordo faixa D"), (5.45, "bordo acost. D")):
        difs = []
        for i in range(n + 1):
            s = a + (b - a) * i / float(n)
            z0 = cota_no_eixo(al, sup, e_sup, s, off)
            z1 = cota_no_eixo(al, s_cor, e_cor, s, off)
            if z0 is not None and z1 is not None:
                difs.append(z1 - z0)
        if difs:
            lidos += 1
            log("   %-15s min %s | medio %s | max %s m" % (
                nome, fmt(min(difs), 3), fmt(sum(difs) / len(difs), 3), fmt(max(difs), 3)))
        else:
            log("   %-15s (sem leitura)" % nome)
    if lidos == 0:
        log("  (superficie do corredor sem leitura - confira no desenho)")
        return
    log("  Largura da superficie do corredor (inclui taludes, se a montagem tiver):")
    for frac in (0.25, 0.5, 0.75):
        s = a + (b - a) * frac
        lados = []
        for sinal in (-1.0, 1.0):
            ultimo = None
            k = 1
            while k <= 600:
                off = sinal * k * 0.05
                if cota_no_eixo(al, s_cor, e_cor, s, off) is None:
                    break
                ultimo = abs(off)
                k += 1
            lados.append(fmt(ultimo, 2) if ultimo is not None else "?")
        log("   %s  E %s m | D %s m" % (estaca_txt(s), lados[0], lados[1]))
    log("  Pendentes transversais (faixa | acostamento), E = esquerda, D = direita:")
    for frac in (0.25, 0.5, 0.75):
        s = a + (b - a) * frac
        z = {}
        for off in (-5.45, -3.5, 0.0, 3.5, 5.45):
            z[off] = cota_no_eixo(al, s_cor, e_cor, s, off)
        try:
            e1 = (z[-3.5] - z[0.0]) / 3.5 * 100.0
            e2 = (z[-5.45] - z[-3.5]) / 1.95 * 100.0
            d1 = (z[3.5] - z[0.0]) / 3.5 * 100.0
            d2 = (z[5.45] - z[3.5]) / 1.95 * 100.0
            log("   %s  E %s%% | %s%%   D %s%% | %s%%" % (estaca_txt(s), fmt(e1, 2), fmt(e2, 2), fmt(d1, 2), fmt(d2, 2)))
        except Exception:
            log("   %s  (sem leitura)" % estaca_txt(s))


def etapa_corredor(t, civdoc, plano, prof_id):
    log("")
    log("ETAPA CORREDOR")
    if plano["asm"] is None:
        log("  Corredor NAO criado: falta a montagem (veja acima).")
        return None
    al = abrir(t, plano["al"])
    if prof_id is None:
        prof = achar(t, al.GetProfileIds(), NOME_PROJ)
        if prof is None:
            log("  '%s' nao existe: rode antes com MODO = greide." % NOME_PROJ)
            return None
    else:
        prof = abrir(t, prof_id)
    a = prof.StartingStation
    b = prof.EndingStation

    existente = achar(t, civdoc.CorridorCollection, NOME_CORREDOR)
    if existente is not None:
        log("  Corredor '%s' ja existe -> mantido sem alteracoes." % NOME_CORREDOR)
        return existente.ObjectId

    cid = civdoc.CorridorCollection.Add(NOME_CORREDOR)
    cor = abrir(t, cid, True)
    bl = cor.Baselines.Add("BL-" + al.Name, al.ObjectId, prof.ObjectId)
    reg = bl.BaselineRegions.Add("RECAP", plano["asm"], float(a), float(b))
    ajustar_frequencia(reg)
    cs = criar_superficie_corredor(civdoc, cor)
    cor.Rebuild()
    log("  Corredor '%s' criado: %s a %s, greide '%s'." % (NOME_CORREDOR, estaca_txt(a), estaca_txt(b), prof.Name))
    if ATUALIZAR_FAIXA:
        try:
            n = atualizar_faixas(t, al, prof)
            log("  Faixa de cotas atualizada em %d grafico(s) do perfil." % n)
        except Exception as e:
            log("  (faixa nao atualizada: %s)" % e)
    if plano["sup"] is not None:
        conferir_corredor(t, al, plano["sup"], cs, a, b)
    return cid


def em_transacao(db, func, *args):
    t = db.TransactionManager.StartTransaction()
    try:
        r = func(t, *args)
        t.Commit()
        return r
    except Exception as e:
        log("ERRO: " + erro_curto(e))
        log("  -> etapa desfeita, nada dela foi gravado.")
        try:
            t.Abort()
        except Exception:
            pass
        return None
    finally:
        t.Dispose()


def principal():
    adoc = Application.DocumentManager.MdiActiveDocument
    db = adoc.Database
    civdoc = CivilApplication.ActiveDocument
    log("GREIDE COLADO + CORREDOR RECAP | RODAR = %s | MODO = %s" % (RODAR, MODO))
    if MODO not in ("tudo", "greide", "corredor"):
        log("MODO invalido: use tudo, greide ou corredor.")
        return
    if not RODAR:
        log("RODAR = False -> so diagnostico, o desenho NAO e alterado.")
    trava = adoc.LockDocument()
    try:
        plano = em_transacao(db, lambda t: planejar(t, civdoc))
        if plano is None or not RODAR:
            return
        prof_id = None
        if MODO in ("tudo", "greide"):
            prof_id = em_transacao(db, lambda t: etapa_greides(t, civdoc, plano))
            if prof_id is None:
                return
        if MODO in ("tudo", "corredor"):
            em_transacao(db, lambda t: etapa_corredor(t, civdoc, plano, prof_id))
        log("")
        log("Fim. Nas curvas, calcule a superelevacao do alinhamento e reconstrua o corredor.")
    finally:
        trava.Dispose()


try:
    principal()
except Exception as e:
    log("ERRO GERAL: " + erro_curto(e))

OUT = REL
