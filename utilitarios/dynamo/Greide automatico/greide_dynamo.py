# -*- coding: utf-8 -*-
# =============================================================================
#  GREIDE AUTOMATICO - Dynamo para Civil 3D
#  Cole este codigo em um no "Python Script" (motor CPython3).
#  Nao precisa de nenhuma entrada ligada: ajuste os parametros abaixo.
#
#  O que faz: le o perfil do terreno do alinhamento, gera o greide seguindo
#  o terreno com a rampa truncada na rampa maxima, dimensiona as curvas
#  verticais parabolicas simetricas por K minimo e cria o perfil de projeto.
#
#  Desenvolvido para Isaque Ferraz Moreira.
# =============================================================================

# ------------------------- PARAMETROS ----------------------------------------
NOME_ALINHAMENTO = ""        # "" = usa o primeiro alinhamento do desenho
NOME_PERFIL_TERRENO = ""     # "" = detecta sozinho (prefere ProfileType EG)

COTA_INICIAL    = None       # None = usa a cota do terreno na estaca inicial
RAMPA_MAXIMA    = 8.0        # %
RAMPA_MINIMA    = 0.5        # % (0 = sem rampa minima)
VELOCIDADE      = 40.0       # km/h  -> define o K minimo
TANGENTE_MINIMA = 40.0       # m entre PIVs
TANGENTE_MAXIMA = 300.0      # m; impede tangentes quilometricas em eixos longos
TOLERANCIA      = 0.20       # m de desvio aceito em relacao ao terreno
PASSO           = 10.0       # m de amostragem
MAX_PONTOS      = 600        # teto de leituras (protege contra travamento)
L_CURVA_FIXO    = 0.0        # m; 0 = calcula L = K x A

NOME_GREIDE      = "GREIDE-AUTO"
CRIAR_NO_DESENHO = True      # False = so calcula e grava os arquivos
GRAVAR_ARQUIVOS  = True      # CSV dos PIVs + TXT do "Create Profile from File"
# -----------------------------------------------------------------------------

import clr
import math
import os

clr.AddReference('AcMgd')
clr.AddReference('AcCoreMgd')
clr.AddReference('AcDbMgd')
clr.AddReference('AecBaseMgd')
clr.AddReference('AeccDbMgd')

from Autodesk.AutoCAD.ApplicationServices import Application
from Autodesk.AutoCAD.DatabaseServices import OpenMode, ObjectId
from Autodesk.AutoCAD.Geometry import Point2d
from Autodesk.Civil.ApplicationServices import CivilApplication
from Autodesk.Civil.DatabaseServices import Profile, ProfileType

log = []


def L(msg):
    log.append(str(msg))


# =============================================================================
# 1. Tabelas de K minimo (parada) - AASHTO metrico / DNIT
# =============================================================================

K_CONVEXA = {30: 2.0, 40: 4.0, 50: 7.0, 60: 11.0, 70: 17.0,
             80: 26.0, 90: 39.0, 100: 52.0, 110: 74.0, 120: 95.0}
K_CONCAVA = {30: 6.0, 40: 9.0, 50: 13.0, 60: 18.0, 70: 23.0,
             80: 30.0, 90: 38.0, 100: 45.0, 110: 55.0, 120: 63.0}


def interp(tab, v):
    ks = sorted(tab)
    if v <= ks[0]:
        return tab[ks[0]]
    if v >= ks[-1]:
        return tab[ks[-1]]
    for a, b in zip(ks, ks[1:]):
        if a <= v <= b:
            return tab[a] + (v - a) / (b - a) * (tab[b] - tab[a])
    return tab[ks[-1]]


def fmt_est(s, corda=20.0):
    n = int(math.floor(s / corda))
    return "%d+%06.3f" % (n, s - n * corda)


# =============================================================================
# 2. Geracao do greide (mesmo algoritmo validado do greide_auto.py)
# =============================================================================

class PVI(object):
    def __init__(self, est, cota):
        self.est = est
        self.cota = cota
        self.g_in = 0.0
        self.g_out = 0.0
        self.Lc = 0.0
        self.L_teorico = 0.0
        self.K_min = 0.0
        self.tipo = "-"

    @property
    def A(self):
        return (self.g_out - self.g_in) * 100.0

    @property
    def K_real(self):
        return self.Lc / abs(self.A) if abs(self.A) > 1e-9 else 0.0


def cota_terreno(pts, s):
    if s <= pts[0][0]:
        return pts[0][1]
    if s >= pts[-1][0]:
        return pts[-1][1]
    lo, hi = 0, len(pts) - 1
    while hi - lo > 1:
        m = (lo + hi) // 2
        if pts[m][0] <= s:
            lo = m
        else:
            hi = m
    a, b = pts[lo], pts[hi]
    if b[0] <= a[0]:
        return a[1]
    return a[1] + (s - a[0]) / (b[0] - a[0]) * (b[1] - a[1])


def rampa(pts, i0, j, s0, z0, imax, imin, sinal):
    num = den = 0.0
    for k in range(i0, j + 1):
        ds = pts[k][0] - s0
        if ds > 1e-9:
            num += ds * (pts[k][1] - z0)
            den += ds * ds
    g = num / den if den > 1e-9 else 0.0
    if abs(g) > imax:
        g = math.copysign(imax, g)
    if imin > 0.0 and abs(g) < imin:
        ref = g if abs(g) > 1e-12 else (sinal if sinal else 1.0)
        g = math.copysign(imin, ref)
    return g


def desvio(pts, i0, j, s0, z0, g):
    d = 0.0
    for k in range(i0, j + 1):
        v = abs((z0 + g * (pts[k][0] - s0)) - pts[k][1])
        if v > d:
            d = v
    return d


def gerar_pvis(pts, cota0, imax, imin, ltmin, tol, ltmax=1e9):
    n = len(pts)
    avisos = []
    pvis = [PVI(pts[0][0], cota0)]
    i0 = 0
    sinal = 0.0
    while i0 < n - 1:
        s0 = pts[i0][0]
        z0 = pvis[-1].cota
        jbest = None
        gbest = 0.0
        j0 = None
        dev0 = 0.0
        g0 = 0.0
        j = i0 + 1
        while j <= n - 1 and pts[j][0] - s0 <= ltmax:
            if pts[j][0] - s0 >= ltmin - 1e-6:
                g = rampa(pts, i0, j, s0, z0, imax, imin, sinal)
                dv = desvio(pts, i0, j, s0, z0, g)
                if j0 is None:
                    j0, dev0, g0 = j, dv, g
                if dv <= tol:
                    jbest, gbest = j, g
                elif jbest is not None:
                    break
            j += 1

        if jbest is None and j0 is not None:
            lim = min(tol * 3.0, max(tol, dev0 * 1.35))
            jbest, gbest = j0, g0
            j = j0 + 1
            while j <= n - 1 and pts[j][0] - s0 <= ltmax:
                g = rampa(pts, i0, j, s0, z0, imax, imin, sinal)
                dv = desvio(pts, i0, j, s0, z0, g)
                if dv <= lim:
                    jbest, gbest = j, g
                    j += 1
                else:
                    break
            avisos.append("Est. %s: terreno nao cabe na tolerancia; tangente de %.0f m."
                          % (fmt_est(s0), pts[jbest][0] - s0))

        if jbest is None:
            jbest = n - 1
            gbest = rampa(pts, i0, jbest, s0, z0, imax, imin, sinal)

        if (pts[n - 1][0] - pts[jbest][0]) < ltmin - 1e-6:
            jbest = n - 1
            gbest = rampa(pts, i0, jbest, s0, z0, imax, imin, sinal)

        s1 = pts[jbest][0]
        z1 = z0 + gbest * (s1 - s0)
        pvis[-1].g_out = gbest
        novo = PVI(s1, z1)
        novo.g_in = gbest
        novo.g_out = gbest
        pvis.append(novo)
        if abs(gbest) > 1e-12:
            sinal = math.copysign(1.0, gbest)
        i0 = jbest

    pvis[0].g_in = pvis[0].g_out
    pvis[-1].g_out = pvis[-1].g_in
    return pvis, avisos


def dimensionar_curvas(pvis, vel, l_fixo=0.0):
    avisos = []
    kc = interp(K_CONVEXA, vel)
    kv = interp(K_CONCAVA, vel)
    lmin = max(l_fixo, 0.6 * vel)
    for p in pvis[1:-1]:
        A = p.A
        if abs(A) < 0.05:
            p.Lc = 0.0
            continue
        p.tipo = "convexa" if A < 0 else "concava"
        p.K_min = kc if A < 0 else kv
        p.L_teorico = l_fixo if l_fixo > 0 else max(p.K_min * abs(A), lmin)
        p.Lc = p.L_teorico

    for _ in range(30):
        mudou = False
        for a, b in zip(pvis, pvis[1:]):
            T = b.est - a.est
            need = a.Lc / 2.0 + b.Lc / 2.0
            if need > 0 and need > T - 1e-6:
                f = (T * 0.995) / need
                if a.Lc > 0:
                    a.Lc *= f
                if b.Lc > 0:
                    b.Lc *= f
                mudou = True
        if not mudou:
            break

    for p in pvis[1:-1]:
        if 0 < p.Lc < p.L_teorico - 1e-6 and p.K_real < p.K_min - 1e-6:
            avisos.append("Est. %s: K resultante %.1f abaixo do minimo %.1f. REVISAR."
                          % (fmt_est(p.est), p.K_real, p.K_min))
    return avisos


def cota_greide(pvis, s):
    if s <= pvis[0].est:
        return pvis[0].cota + pvis[0].g_out * (s - pvis[0].est)
    if s >= pvis[-1].est:
        return pvis[-1].cota + pvis[-1].g_in * (s - pvis[-1].est)
    for p in pvis[1:-1]:
        if p.Lc > 0 and abs(s - p.est) <= p.Lc / 2.0:
            pcv = p.est - p.Lc / 2.0
            zp = p.cota - p.g_in * p.Lc / 2.0
            x = s - pcv
            A = p.g_out - p.g_in
            return zp + p.g_in * x + (A / (2.0 * p.Lc)) * x * x
    for a, b in zip(pvis, pvis[1:]):
        if a.est <= s <= b.est:
            return a.cota + a.g_out * (s - a.est)
    return pvis[-1].cota


# =============================================================================
# 3. Civil 3D
# =============================================================================

def escolher_terreno(t, align, nome_pedido):
    """Escolhe o perfil de terreno. Regra 1: ProfileType EG. Regra 2: nome.
    Regra 3: a estaca inicial do perfil tem que bater com a do alinhamento."""
    ss_al = align.StartingStation
    cands = []
    for pid in align.GetProfileIds():
        p = t.GetObject(pid, OpenMode.ForRead)
        nome = p.Name
        try:
            eh_eg = (p.ProfileType == ProfileType.EG)
        except Exception:
            eh_eg = False
        try:
            ss = p.StartingStation
        except Exception:
            ss = None

        nota = 0
        up = nome.upper()
        if eh_eg:
            nota += 100
        for kw in ("PRIMITIV", "EG", "TERR", "EXIST", "NATUR", "SUPERF", "ORIGINAL"):
            if kw in up:
                nota += 20
                break
        for kw in ("VERTICAL", "GREIDE", "PROJET", "LAYOUT"):
            if kw in up:
                nota -= 60
                break
        # o perfil tem que comecar junto com o alinhamento
        if ss is not None and abs(ss - ss_al) > 1.0:
            nota -= 200
        if nome_pedido and nome_pedido.strip().upper() == up:
            nota += 1000

        L("   perfil: %-45s  EG=%-5s  ss=%s  nota=%d"
          % (nome[:45], eh_eg, ("%.3f" % ss) if ss is not None else "?", nota))
        cands.append((nota, p, nome))

    if not cands:
        return None, None
    cands.sort(key=lambda c: -c[0])
    if cands[0][0] <= -100:
        return None, None
    return cands[0][1], cands[0][2]


def ler_terreno(prof, s0, s1, passo, maxp):
    np_ = int((s1 - s0) / max(passo, 0.1))
    if np_ > maxp:
        passo = (s1 - s0) / float(maxp)
    pts = []
    s = s0
    while s < s1 - 1e-6:
        try:
            z = prof.ElevationAt(s)
            if z is not None:
                pts.append((s, z))
        except Exception:
            pass
        s += passo
    try:
        z = prof.ElevationAt(s1)
        if z is not None:
            pts.append((s1, z))
    except Exception:
        pass
    # ordena e tira repetidos
    pts.sort()
    out = []
    for p in pts:
        if not out or p[0] - out[-1][0] > 1e-4:
            out.append(p)
    return out


def primeiro_estilo(colecao, preferidos=()):
    """As colecoes de estilo do Civil 3D nao aceitam indice inteiro no Python.
    Busca por nome preferido e, se nao achar, pega o primeiro iterando."""
    for nome in preferidos:
        try:
            return colecao[nome]
        except Exception:
            pass
    try:
        for oid in colecao:
            return oid
    except Exception:
        pass
    try:
        return colecao[0]
    except Exception:
        pass
    return ObjectId.Null


def tentar(chamadas, nome):
    """Executa a primeira chamada que funcionar. Devolve (ok, resultado, assinatura)."""
    erros = []
    for i, fn in enumerate(chamadas):
        try:
            return True, fn(), i
        except Exception as e:
            erros.append("  [%s #%d] %s" % (nome, i, str(e)[:120]))
    for e in erros:
        L(e)
    return False, None, -1


# =============================================================================
# 4. Execucao
# =============================================================================

adoc = Application.DocumentManager.MdiActiveDocument
civdoc = CivilApplication.ActiveDocument
db = adoc.Database

pvis = []
resumo = ""

try:
  with adoc.LockDocument():
    with db.TransactionManager.StartTransaction() as t:

          # --- alinhamento ----------------------------------------------------
          alids = list(civdoc.GetAlignmentIds())
          if not alids:
              raise Exception("O desenho nao tem nenhum alinhamento.")
          align = None
          for aid in alids:
              a = t.GetObject(aid, OpenMode.ForRead)
              if (not NOME_ALINHAMENTO) or a.Name.strip().upper() == NOME_ALINHAMENTO.strip().upper():
                  align = a
                  alignId = aid
                  break
          if align is None:
              raise Exception("Alinhamento '%s' nao encontrado. Existem: %s"
                              % (NOME_ALINHAMENTO,
                                 [t.GetObject(x, OpenMode.ForRead).Name for x in alids]))

          s0 = align.StartingStation
          s1 = align.EndingStation
          L("ALINHAMENTO: %s   est. %s a %s  (%.2f m)"
            % (align.Name, fmt_est(s0), fmt_est(s1), s1 - s0))

          # --- perfil do terreno ----------------------------------------------
          L("")
          L("PERFIS DO ALINHAMENTO:")
          prof, nome_prof = escolher_terreno(t, align, NOME_PERFIL_TERRENO)
          if prof is None:
              raise Exception("Nenhum perfil de terreno valido neste alinhamento.")
          L("")
          L("TERRENO ADOTADO: %s" % nome_prof)

          pts = ler_terreno(prof, s0, s1, PASSO, MAX_PONTOS)
          if len(pts) < 3:
              raise Exception("Nao consegui ler pontos do perfil '%s'." % nome_prof)
          L("Pontos lidos: %d   (est. %s a %s)"
            % (len(pts), fmt_est(pts[0][0]), fmt_est(pts[-1][0])))

          # --- calculo ---------------------------------------------------------
          cota0 = COTA_INICIAL if COTA_INICIAL is not None else pts[0][1]
          imax = abs(RAMPA_MAXIMA) / 100.0
          imin = abs(RAMPA_MINIMA) / 100.0
          if imin >= imax:
              imin = 0.0

          pvis, av1 = gerar_pvis(pts, cota0, imax, imin, TANGENTE_MINIMA, TOLERANCIA,
                                 TANGENTE_MAXIMA)
          av2 = dimensionar_curvas(pvis, VELOCIDADE, L_CURVA_FIXO)
          avisos = av1 + av2

          L("")
          L("K minimo adotado: convexa %.1f | concava %.1f | L min (0,6V) %.1f m"
            % (interp(K_CONVEXA, VELOCIDADE), interp(K_CONCAVA, VELOCIDADE), 0.6 * VELOCIDADE))
          L("")
          L(" #   Estaca         Cota      i_ent%   i_sai%      A%   Tipo       L(m)     K")
          for i, p in enumerate(pvis, 1):
              L("%3d  %-13s %9.3f %8.3f %8.3f %8.3f   %-9s %7.2f %6.1f"
                % (i, fmt_est(p.est), p.cota, p.g_in * 100, p.g_out * 100,
                   p.A, p.tipo, p.Lc, p.K_real))

          pior = max([abs(p.g_out) for p in pvis[:-1]] or [0.0])
          tmin = min([b.est - a.est for a, b in zip(pvis, pvis[1:])] or [0.0])
          cmax = amax = 0.0
          scm = sam = pts[0][0]
          for (s, z) in pts:
              d = cota_greide(pvis, s) - z
              if d < 0 and -d > cmax:
                  cmax, scm = -d, s
              if d > 0 and d > amax:
                  amax, sam = d, s
          L("")
          L("Rampa maxima utilizada : %.3f %%  (limite %.3f %%) -> %s"
            % (pior * 100, RAMPA_MAXIMA, "OK" if pior <= imax + 1e-6 else "EXCEDE"))
          L("Menor tangente ........ : %.2f m" % tmin)
          L("Corte maximo .......... : %.3f m na est. %s" % (cmax, fmt_est(scm)))
          L("Aterro maximo ......... : %.3f m na est. %s" % (amax, fmt_est(sam)))
          if avisos:
              L("AVISOS:")
              for a in avisos:
                  L("  ! " + a)
          else:
              L("AVISOS: nenhum.")

          # --- arquivos --------------------------------------------------------
          if GRAVAR_ARQUIVOS:
              try:
                  pasta = os.path.dirname(adoc.Name)
                  base = os.path.join(pasta, "".join(
                      c if c.isalnum() or c in " -_" else "_" for c in align.Name))
                  with open(base + "_greide_pvis.csv", "w") as f:
                      f.write("PIV;Estaca;Progressiva(m);Cota(m);i_ent(%);i_sai(%);"
                              "A(%);Tipo;Kmin;L(m);Kreal;PCV;PTV\n")
                      for i, p in enumerate(pvis, 1):
                          f.write("%d;%s;%.3f;%.3f;%.3f;%.3f;%.3f;%s;%.1f;%.2f;%.1f;%s;%s\n"
                                  % (i, fmt_est(p.est), p.est, p.cota, p.g_in * 100,
                                     p.g_out * 100, p.A, p.tipo, p.K_min, p.Lc, p.K_real,
                                     ("%.3f" % (p.est - p.Lc / 2)) if p.Lc else "",
                                     ("%.3f" % (p.est + p.Lc / 2)) if p.Lc else ""))
                  with open(base + "_greide_pvis.txt", "w") as f:
                      for p in pvis:
                          f.write("%.4f %.4f\n" % (p.est, p.cota))
                  with open(base + "_terreno.csv", "w") as f:
                      f.write("estaca;cota\n")
                      for (s, z) in pts:
                          f.write("%.4f;%.4f\n" % (s, z))
                  L("")
                  L("Arquivos gravados em: " + pasta)
              except Exception as e:
                  L("Nao consegui gravar os arquivos: %s" % e)

          # --- cria o perfil no desenho ----------------------------------------
          if CRIAR_NO_DESENHO:
              L("")
              L("CRIANDO O PERFIL DE PROJETO...")
              estilo = primeiro_estilo(civdoc.Styles.ProfileStyles,
                                       ("Greide", "Design Profile", "Basico", "Standard"))
              rotulos = primeiro_estilo(civdoc.Styles.LabelSetStyles.ProfileLabelSetStyles,
                                        ("_Sem rotulos", "_No Labels", "Standard"))
              camada = db.Clayer
              L("  estilo=%s | conjunto de rotulos=%s"
                % (estilo.IsNull and "NULO" or "ok", rotulos.IsNull and "NULO" or "ok"))

              ok, pid, qual = tentar([
                  lambda: Profile.CreateByLayout(NOME_GREIDE, alignId, camada, estilo, rotulos),
                  lambda: Profile.CreateByLayout(NOME_GREIDE, alignId, camada, estilo),
              ], "CreateByLayout")

              if not ok:
                  L("*** Nao consegui criar o perfil. Use o LandXML ou o TXT gerado.")
              else:
                  novo = t.GetObject(pid, OpenMode.ForWrite)
                  ents = novo.Entities

                  # tangentes entre PIVs consecutivos
                  criadas = []
                  for a, b in zip(pvis, pvis[1:]):
                      ok2, e, _ = tentar([
                          lambda: ents.AddFixedTangent(Point2d(a.est, a.cota),
                                                       Point2d(b.est, b.cota)),
                      ], "AddFixedTangent")
                      if ok2:
                          criadas.append(e)
                  L("Tangentes criadas: %d de %d" % (len(criadas), len(pvis) - 1))

                  # curvas verticais nos PIVs internos
                  ncurv = 0
                  for k in range(1, len(pvis) - 1):
                      p = pvis[k]
                      if p.Lc <= 0 or k > len(criadas) - 1:
                          continue
                      e1 = criadas[k - 1]
                      e2 = criadas[k]
                      ok3, _, assinatura = tentar([
                          lambda: ents.AddFreeSymmetricParabolaByLength(
                              e1.EntityId, e2.EntityId, p.Lc),
                          lambda: ents.AddFreeSymmetricParabolaByLength(
                              e1.EntityId, e2.EntityId, p.Lc, True),
                          lambda: ents.AddFreeSymmetricParabolaByLength(e1, e2, p.Lc),
                      ], "Curva est. " + fmt_est(p.est))
                      if ok3:
                          ncurv += 1
                  L("Curvas verticais criadas: %d de %d"
                    % (ncurv, len([p for p in pvis[1:-1] if p.Lc > 0])))
                  if ncurv == 0:
                      L("*** As curvas nao entraram pela API. O greide ficou so com as")
                      L("    tangentes. Use o LandXML do greide_auto.py para ter as curvas.")

          t.Commit()
except Exception as _e:
    import traceback
    L("")
    L("*** A ROTINA PAROU: %s" % _e)
    L(traceback.format_exc())

OUT = "\n".join(log)
