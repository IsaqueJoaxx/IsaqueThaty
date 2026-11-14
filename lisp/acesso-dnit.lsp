# -*- coding: utf-8 -*-
# =====================================================================
#  ACESSO-DNIT  -  Dynamo for Civil 3D  -  node "Python Script"
#  Geometria de acessos de areas lindeiras a rodovias federais
#  Referencia: DNIT / IPR - Publicacao IPR-728, 2a edicao (2024)
#
#  ENTRADAS DO NODE (3 portas):
#    IN[0] pontos : lista com 4 pontos, NESTA ORDEM
#                   [0] eixo da RODOVIA - a montante do acesso
#                   [1] eixo da RODOVIA - a jusante (define o sentido do trafego)
#                   [2] eixo do ACESSO  - sobre a rodovia
#                   [3] eixo do ACESSO  - dentro da propriedade
#    IN[1] dados  : lista de parametros (ver DEFAULTS abaixo)
#    IN[2] criar  : True  -> cria as entidades no desenho
#                   False -> so devolve a geometria para o preview do Dynamo
#
#  SAIDA:
#    OUT[0] relatorio (texto)
#    OUT[1] curvas para preview
#    OUT[2] mensagem de status / erro
#
#  Compativel com IronPython2 e CPython3.
# =====================================================================

import math
import traceback

# ---------------------------------------------------------------------
#  CONFIGURACOES (ajuste aqui se quiser outro padrao)
# ---------------------------------------------------------------------
LAY_FMV   = "ACS-FAIXA-MUD-VEL"
LAY_BORDO = "ACS-BORDO-ACESSO"
LAY_GOTA  = "ACS-GOTA"
LAY_GIRO  = "ACS-PROJ-GIRO"
LAY_LIMPA = "ACS-LIMPA-RODAS"
COR = {LAY_FMV: 3, LAY_BORDO: 1, LAY_GOTA: 2, LAY_GIRO: 8, LAY_LIMPA: 30}

N3_L30   = 30.0    # trecho a 3,60 m antes/depois do eixo (Fig. 4 - nivel 3)
N3_TRANS = 15.0    # transicao 3,60 -> 2,50
N3_WRED  = 2.50    # largura reduzida do acostamento (nivel 3)

GOTA_ANG  = 5.5    # angulo do eixo da gota com o eixo secundario (5 a 6 graus)
GOTA_L1   = 10.0   # ponto do eixo secundario a 10 m do bordo (etapa II)
GOTA_L2   = 20.0   # ponto a 20 m do bordo (etapa VII)
GOTA_SW   = 1.50   # meia largura da gota (etapa IV)
GOTA_RF   = 0.75   # arredondamento das extremidades (etapas VI e IX)
GOTA_LADO = 1      # 1 ou -1: lado para o qual a gota se inclina

# ---------------------------------------------------------------------
#  TABELAS DO MANUAL
# ---------------------------------------------------------------------
# Tabela 6 - DESACELERACAO (niveis 3 e 4): vel -> (taper, {vc: total})
T6 = {
    40:  (40.0,  {0: 60,  20: 50,  30: 40}),
    50:  (45.0,  {0: 75,  20: 70,  30: 60,  40: 45}),
    60:  (55.0,  {0: 95,  20: 90,  30: 80,  40: 65,  50: 55}),
    70:  (60.0,  {0: 110, 20: 105, 30: 95,  40: 85,  50: 70,  60: 60}),
    80:  (70.0,  {0: 130, 20: 125, 30: 115, 40: 100, 50: 90,  60: 80,  70: 70}),
    90:  (80.0,  {0: 145, 20: 140, 30: 135, 40: 120, 50: 110, 60: 100, 70: 90,  80: 80}),
    100: (85.0,  {0: 170, 20: 165, 30: 155, 40: 145, 50: 135, 60: 120, 70: 100, 80: 85}),
    110: (90.0,  {0: 180, 20: 180, 30: 170, 40: 160, 50: 150, 60: 140, 70: 120, 80: 105}),
    120: (100.0, {0: 200, 20: 195, 30: 185, 40: 175, 50: 170, 60: 155, 70: 140, 80: 120}),
}
# Tabela 7 - ACELERACAO (niveis 3 e 4)
T7 = {
    40:  (40.0,  {0: 60,  20: 50,  30: 40}),
    50:  (45.0,  {0: 90,  20: 70,  30: 60,  40: 45}),
    60:  (55.0,  {0: 130, 20: 110, 30: 100, 40: 70,  50: 55}),
    70:  (60.0,  {0: 180, 20: 150, 30: 140, 40: 120, 50: 90,  60: 60}),
    80:  (70.0,  {0: 230, 20: 210, 30: 200, 40: 180, 50: 140, 60: 100, 70: 70}),
    90:  (80.0,  {0: 280, 20: 250, 30: 240, 40: 220, 50: 190, 60: 140, 70: 100, 80: 80}),
    100: (85.0,  {0: 340, 20: 310, 30: 290, 40: 280, 50: 240, 60: 200, 70: 170, 80: 110}),
    110: (90.0,  {0: 390, 20: 360, 30: 350, 40: 320, 50: 290, 60: 250, 70: 200, 80: 160}),
    120: (100.0, {0: 430, 20: 400, 30: 390, 40: 360, 50: 330, 60: 290, 70: 240, 80: 200}),
}
# Tabelas 2 e 3 - distancia minima de visibilidade
T2 = {50: 145, 60: 170, 70: 200, 80: 230, 90: 270, 100: 315, 110: 330, 120: 360}
T3 = {50: 195, 60: 235, 70: 275, 80: 315, 90: 355, 100: 390, 110: 430, 120: 470}
# Tabela 4 - ajuste da visibilidade pelo greide
T4 = {
    -6: {20: 1.1, 30: 1.1, 40: 1.1, 50: 1.1, 60: 1.1, 70: 1.1, 80: 1.2, 90: 1.2, 100: 1.2, 110: 1.2, 120: 1.2},
    -5: {20: 1.0, 30: 1.0, 40: 1.1, 50: 1.1, 60: 1.1, 70: 1.1, 80: 1.1, 90: 1.1, 100: 1.1, 110: 1.2, 120: 1.2},
    -4: {20: 1.0, 30: 1.0, 40: 1.0, 50: 1.1, 60: 1.1, 70: 1.1, 80: 1.1, 90: 1.1, 100: 1.1, 110: 1.1, 120: 1.1},
     0: {20: 1.0, 30: 1.0, 40: 1.0, 50: 1.0, 60: 1.0, 70: 1.0, 80: 1.0, 90: 1.0, 100: 1.0, 110: 1.0, 120: 1.0},
     4: {20: 1.0, 30: 1.0, 40: 1.0, 50: 1.0, 60: 0.9, 70: 0.9, 80: 0.9, 90: 0.9, 100: 0.9, 110: 0.9, 120: 0.9},
     5: {20: 1.0, 30: 1.0, 40: 1.0, 50: 0.9, 60: 0.9, 70: 0.9, 80: 0.9, 90: 0.9, 100: 0.9, 110: 0.9, 120: 0.9},
     6: {20: 1.0, 30: 1.0, 40: 0.9, 50: 0.9, 60: 0.9, 70: 0.9, 80: 0.9, 90: 0.9, 100: 0.9, 110: 0.9, 120: 0.9},
}
# Tabela 8 - fatores das faixas de mudanca de velocidade
# ACELERACAO: vel -> ({vc: fator ascendente}, fator descendente)
T8_34 = {
    40:  ({20: 1.2, 30: 1.2}, 0.70),
    50:  ({20: 1.2, 30: 1.2, 40: 1.2}, 0.70),
    60:  ({20: 1.3, 30: 1.3, 40: 1.3, 50: 1.4, 60: 1.4}, 0.70),
    70:  ({20: 1.3, 30: 1.3, 40: 1.3, 50: 1.4, 60: 1.4, 70: 1.5}, 0.65),
    80:  ({20: 1.4, 30: 1.4, 40: 1.4, 50: 1.5, 60: 1.5, 70: 1.5, 80: 1.6}, 0.65),
    90:  ({20: 1.4, 30: 1.4, 40: 1.4, 50: 1.5, 60: 1.5, 70: 1.5, 80: 1.6}, 0.60),
    100: ({20: 1.5, 30: 1.5, 40: 1.5, 50: 1.6, 60: 1.7, 70: 1.7, 80: 1.8}, 0.60),
    110: ({20: 1.5, 30: 1.5, 40: 1.5, 50: 1.6, 60: 1.7, 70: 1.7, 80: 1.8}, 0.60),
    120: ({20: 1.5, 30: 1.5, 40: 1.5, 50: 1.6, 60: 1.7, 70: 1.7, 80: 1.8}, 0.60),
}
T8_56 = {
    40:  ({20: 1.3, 30: 1.4}, 0.60),
    50:  ({20: 1.3, 30: 1.4, 40: 1.4}, 0.60),
    60:  ({20: 1.4, 30: 1.5, 40: 1.5, 50: 1.5}, 0.60),
    70:  ({20: 1.4, 30: 1.5, 40: 1.5, 50: 1.6, 60: 1.7}, 0.60),
    80:  ({20: 1.4, 30: 1.5, 40: 1.5, 50: 1.7, 60: 1.8, 70: 1.9}, 0.55),
    90:  ({20: 1.5, 30: 1.6, 40: 1.6, 50: 1.8, 60: 2.0, 70: 2.1, 80: 2.2}, 0.55),
    100: ({20: 1.6, 30: 1.7, 40: 1.7, 50: 1.9, 60: 2.2, 70: 2.4, 80: 2.5}, 0.50),
    110: ({20: 1.9, 30: 2.0, 40: 2.0, 50: 2.2, 60: 2.6, 70: 2.8, 80: 3.0}, 0.50),
    120: ({20: 2.0, 30: 2.1, 40: 2.3, 50: 2.5, 60: 3.0, 70: 3.2, 80: 3.5}, 0.50),
}
# Tabela 5 - distancia minima entre acessos / intersecoes / retornos
T5 = {
    "RURAL-SIMPLES-AB": (350, 400, 450), "RURAL-SIMPLES-CD": (400, 450, 500),
    "RURAL-DUPLA-AB": (300, 350, 400),   "RURAL-DUPLA-CD": (350, 400, 450),
    "URBANO-SIMPLES-AB": (250, 300, 350), "URBANO-SIMPLES-CD": (300, 350, 400),
    "URBANO-DUPLA-AB": (200, 250, 300),  "URBANO-DUPLA-CD": (250, 300, 350),
}
# Tabela 9 - raios minimos para bordos de conversao (circular simples / com taper)
T9 = {
    30:  {"VP": (18, None), "CO": (30, None), "SR": (60, None)},
    45:  {"VP": (15, None), "CO": (23, None), "SR": (53, 36)},
    60:  {"VP": (12, None), "CO": (18, None), "SR": (45, 29)},
    75:  {"VP": (11, 8),    "CO": (17, 14),   "SR": (None, 20)},
    90:  {"VP": (9, 6),     "CO": (15, 12),   "SR": (None, 18)},
    105: {"VP": (None, 6),  "CO": (None, 11), "SR": (None, 17)},
    120: {"VP": (None, 6),  "CO": (None, 9),  "SR": (None, 14)},
    135: {"VP": (None, 6),  "CO": (None, 9),  "SR": (None, 12)},
    150: {"VP": (None, 6),  "CO": (None, 9),  "SR": (None, 11)},
    180: {"VP": (None, 5),  "CO": (None, 9),  "SR": (None, 8)},
}

# ---------------------------------------------------------------------
#  ALGEBRA VETORIAL 2D
# ---------------------------------------------------------------------
def vadd(a, b):   return (a[0] + b[0], a[1] + b[1])
def vsub(a, b):   return (a[0] - b[0], a[1] - b[1])
def vmul(a, s):   return (a[0] * s, a[1] * s)
def vdot(a, b):   return a[0] * b[0] + a[1] * b[1]
def vlen(a):      return math.sqrt(a[0] * a[0] + a[1] * a[1])
def vperp(a):     return (-a[1], a[0])

def vnorm(a):
    l = vlen(a)
    if l < 1e-12:
        return (0.0, 0.0)
    return (a[0] / l, a[1] / l)

def lint(p1, d1, p2, d2):
    """Intersecao de duas retas ponto+direcao."""
    den = d1[0] * d2[1] - d1[1] * d2[0]
    if abs(den) < 1e-10:
        return None
    w = vsub(p2, p1)
    t = (w[0] * d2[1] - w[1] * d2[0]) / den
    return vadd(p1, vmul(d1, t))

def sol2(a1, c1, a2, c2):
    """Resolve a1.C = c1 e a2.C = c2 (a1, a2 = normais)."""
    den = a1[0] * a2[1] - a1[1] * a2[0]
    if abs(den) < 1e-10:
        return None
    return ((c1 * a2[1] - c2 * a1[1]) / den, (a1[0] * c2 - a2[0] * c1) / den)

def fillet(pc, u1, u2, r):
    """Concordancia de raio r entre duas semirretas que saem de pc."""
    u1, u2 = vnorm(u1), vnorm(u2)
    c = max(-1.0, min(1.0, vdot(u1, u2)))
    a2 = math.acos(c) / 2.0
    if a2 < 1e-3 or a2 > math.pi / 2.0 - 1e-3:
        return None
    t = r / math.tan(a2)
    bis = vnorm(vadd(u1, u2))
    return (vadd(pc, vmul(u1, t)), vadd(pc, vmul(u2, t)), vadd(pc, vmul(bis, r / math.sin(a2))))

def cci(c1, r1, c2, r2):
    """Intersecao de dois circulos."""
    d = vlen(vsub(c2, c1))
    if d > r1 + r2 or d < abs(r1 - r2) or d < 1e-9:
        return None
    a = (r1 * r1 - r2 * r2 + d * d) / (2.0 * d)
    h = math.sqrt(max(0.0, r1 * r1 - a * a))
    ex = vnorm(vsub(c2, c1))
    ey = vperp(ex)
    pm = vadd(c1, vmul(ex, a))
    return (vadd(pm, vmul(ey, h)), vsub(pm, vmul(ey, h)))

def tanp(q, c, r):
    """Pontos de tangencia de um ponto externo q ao circulo (c, r)."""
    d = vlen(vsub(q, c))
    if d <= r + 1e-9:
        return None
    ang = math.atan2(q[1] - c[1], q[0] - c[0])
    b = math.acos(r / d)
    return ((c[0] + r * math.cos(ang + b), c[1] + r * math.sin(ang + b)),
            (c[0] + r * math.cos(ang - b), c[1] + r * math.sin(ang - b)))

def perto(cands, ref):
    return min(cands, key=lambda p: vlen(vsub(p, ref)))

def bulge(c, p1, p2):
    """Bulge do menor arco de p1 a p2 com centro c."""
    a1 = math.atan2(p1[1] - c[1], p1[0] - c[0])
    a2 = math.atan2(p2[1] - c[1], p2[0] - c[0])
    d = a2 - a1
    while d > math.pi:
        d -= 2.0 * math.pi
    while d < -math.pi:
        d += 2.0 * math.pi
    return math.tan(d / 4.0)

# ---------------------------------------------------------------------
#  CONSULTA AS TABELAS
# ---------------------------------------------------------------------
def snap(v, chaves):
    return min(chaves, key=lambda k: abs(k - v))

def snap_vc(vc):
    return snap(vc, [0, 20, 30, 40, 50, 60, 70, 80])

def comp_tabela(tab, v, vc):
    taper, col = tab[v]
    ordem = [vc] + [k for k in sorted(col.keys(), reverse=True) if k < vc] + [0]
    for k in ordem:
        if k in col:
            return taper, float(col[k]), k
    return taper, float(col[0]), 0

def f_desac(g):
    if 3.0 <= g < 5.0:   return 0.90
    if g >= 5.0:         return 0.80
    if -5.0 < g <= -3.0: return 1.20
    if g <= -5.0:        return 1.35
    return 1.0

def f_acel(g, v, vc):
    if abs(g) < 3.0:
        return 1.0
    tab = T8_34 if abs(g) < 5.0 else T8_56
    asc, desc = tab[v]
    if g < 0:
        return desc
    vck = snap_vc(max(vc, 20))
    while vck > 20 and vck not in asc:
        vck -= 10
    return asc.get(vck, 1.0)

def d_visib(v, especial=False):
    tab = T3 if especial else T2
    return float(tab[snap(v, list(tab.keys()))])

def f_visib(g, v):
    lin = min(T4.keys(), key=lambda k: abs(k - (0 if abs(g) < 3.5 else g)))
    col = snap(v, list(T4[lin].keys()))
    return T4[lin][col]

# ---------------------------------------------------------------------
#  CONSTRUCAO DA GEOMETRIA
#  Cada "shape" e um dicionario: layer, verts, bulges, closed
# ---------------------------------------------------------------------
def lado(pI, dR, nR, dA, eA, sgn, hw, ha, wf, rc, ltot, xtap, n3, lr):
    """Taper + mesa + raio de concordancia + bordo do acesso, em UMA polilinha."""
    d = vmul(dR, sgn)
    pO = vadd(pI, vmul(nR, hw + wf))          # linha externa da faixa
    pA = vadd(pI, vmul(eA, sgn * ha))         # bordo do acesso deste lado
    pc = lint(pO, d, pA, dA)
    if pc is None:
        return None, None
    fl = fillet(pc, d, dA, rc)
    if fl:
        t1, t2, cf = fl
    else:
        t1, t2, cf = pc, pc, None

    st = vdot(vsub(t1, pI), d)
    ymesa = max(0.0, ltot - xtap)
    send = st + ymesa
    so = [(st, wf)]
    w = wf
    if n3 and st < N3_L30 and send > N3_L30 + N3_TRANS:
        so.append((N3_L30, wf))
        so.append((N3_L30 + N3_TRANS, N3_WRED))
        w = N3_WRED
    so.append((send, w))
    so.append((send + xtap, 0.0))

    verts = [vadd(pI, vadd(vmul(d, s), vmul(nR, o))) for (s, o) in reversed(so)]
    bul = [0.0] * len(verts)
    if cf:
        bul[-1] = bulge(cf, t1, t2)           # arco do raio de bordo
    tb = hw / vdot(dA, nR)                    # ate o bordo, ao longo do acesso
    pend = vadd(pA, vmul(dA, tb + lr))
    verts.append(t2); bul.append(0.0)
    verts.append(pend); bul.append(0.0)
    return {"layer": LAY_FMV, "verts": verts, "bulges": bul, "closed": False}, pend

def lado_oposto(pI, dR, nR, dA, eA, sgn, hw, ha, wf, ltot, xtap, n3):
    d = vmul(dR, sgn)
    st = ha
    ymesa = max(0.0, ltot - xtap)
    send = st + ymesa
    so = [(st, 0.0), (st, wf)]
    w = wf
    if n3 and st < N3_L30 and send > N3_L30 + N3_TRANS:
        so.append((N3_L30, wf))
        so.append((N3_L30 + N3_TRANS, N3_WRED))
        w = N3_WRED
    so.append((send, w))
    so.append((send + xtap, 0.0))
    verts = [vadd(pI, vadd(vmul(d, s), vmul(nR, o))) for (s, o) in so]
    return {"layer": LAY_FMV, "verts": verts, "bulges": [0.0] * len(verts), "closed": False}

def gota(pI, nR, dA, eA, hw, dgot, rg, sentido):
    """Gota de canalizacao - etapas I a IX do item 4.3 do manual."""
    out = []
    tb = hw / vdot(dA, nR)
    pB = vadd(pI, vmul(dA, tb))                       # eixo do acesso x bordo
    pRef = vadd(pB, vmul(nR, -dgot))                  # linha de tangencia dos arcos
    p = vadd(pB, vmul(dA, GOTA_L1))                   # etapa II
    b = math.radians(GOTA_ANG) * (1 if sentido else 0)
    uG = vnorm(vadd(vmul(dA, math.cos(b)), vmul(eA, (sentido or 0) * math.sin(b))))  # etapa III
    nG = vperp(uG)
    pPL = vsub(p, vmul(nG, GOTA_SW))                  # etapa IV
    pPR = vadd(p, vmul(nG, GOTA_SW))
    q = vadd(p, vmul(uG, (GOTA_L2 - GOTA_L1) / math.cos(b)))   # etapa VII

    c1 = sol2(nR, vdot(nR, pRef) + rg, nG, vdot(nG, pPL) + rg)  # etapa V
    c2 = sol2(nR, vdot(nR, pRef) + rg, nG, vdot(nG, pPR) - rg)
    if c1 is None or c2 is None:
        return out, "nao foi possivel construir a gota"

    tp1 = vsub(c1, vmul(nG, rg))
    tp2 = vadd(c2, vmul(nG, rg))
    tr1 = vsub(c1, vmul(nR, rg))
    tr2 = vsub(c2, vmul(nR, rg))
    nos = cci(c1, rg, c2, rg)
    if nos is None:
        return out, "os arcos da gota nao se interceptam - revise o raio"
    # nariz = intersecao dos dois arcos mais proxima da rodovia
    nariz = min(nos, key=lambda pp: vdot(vsub(pp, pB), dA))

    # etapa VI - arredondamento superior (nariz)
    cn_cands = cci(c1, rg - GOTA_RF, c2, rg - GOTA_RF)
    if cn_cands is None:
        return out, "raio da gota incompativel com o arredondamento de 0,75 m"
    cn = min(cn_cands, key=lambda pp: vlen(vsub(pp, nariz)))
    tn1 = vadd(c1, vmul(vnorm(vsub(cn, c1)), rg))
    tn2 = vadd(c2, vmul(vnorm(vsub(cn, c2)), rg))

    # etapa VIII - retas do ponto inferior tangentes aos arcos
    a = tanp(q, c1, rg)
    tg1 = perto(a, tp1) if a else tp1
    a = tanp(q, c2, rg)
    tg2 = perto(a, tp2) if a else tp2

    # etapa IX - arredondamento inferior
    fb = fillet(q, vsub(tg1, q), vsub(tg2, q), GOTA_RF)
    if fb is None:
        fb = (q, q, q)

    verts = [tn1, tg1, fb[0], fb[1], tg2, tn2]
    bul = [bulge(c1, tn1, tg1), 0.0,
           bulge(fb[2], fb[0], fb[1]), 0.0,
           bulge(c2, tg2, tn2), bulge(cn, tn2, tn1)]
    out.append({"layer": LAY_GOTA, "verts": verts, "bulges": bul, "closed": True})
    # projecao de giro (bordos internos das faixas de giro a esquerda)
    out.append({"layer": LAY_GIRO, "verts": [tr1, tp1], "bulges": [bulge(c1, tr1, tp1), 0.0], "closed": False})
    out.append({"layer": LAY_GIRO, "verts": [tr2, tp2], "bulges": [bulge(c2, tr2, tp2), 0.0], "closed": False})
    return out, None

# ---------------------------------------------------------------------
#  LEITURA DAS ENTRADAS
# ---------------------------------------------------------------------
def pt2d(p):
    """Aceita ponto do Dynamo, lista/tupla ou objeto com X/Y."""
    if hasattr(p, "X"):
        return (float(p.X), float(p.Y))
    return (float(p[0]), float(p[1]))

DEFAULTS = [
    80,        # 0  velocidade diretriz (km/h)
    "B",       # 1  tipo do acesso: A, B, C ou D
    False,     # 2  acesso residencial (so vale para o tipo A)
    "Simples", # 3  tipo de rodovia: Simples ou Dupla
    "Rural",   # 4  localizacao: Rural ou Urbano
    3.60,      # 5  distancia do eixo ao bordo da pista (m)
    7.00,      # 6  largura total do acesso (m)
    20.0,      # 7  raio dos bordos de conversao (m)
    0.0,       # 8  greide da rodovia (%) - positivo = ascendente
    20.0,      # 9  velocidade de seguranca da curva de SAIDA (km/h)
    20.0,      # 10 velocidade de seguranca da curva de ENTRADA (km/h)
    0.0,       # 11 largura da faixa de mudanca de velocidade (0 = automatico)
    20.0,      # 12 extensao do limpa-rodas (m)
    True,      # 13 gerar gota / ilha
    15.0,      # 14 raio dos arcos de giro da gota (12 ou 15)
    True,      # 15 gerar faixas no lado oposto (pista simples)
    0.0,       # 16 afastamento da linha de tangencia da gota p/ dentro da pista (m)
               #    0 = bordo da pista | ~3,60 = bordo da faixa central de giro a esquerda
]

# ---------------------------------------------------------------------
#  PROGRAMA
# ---------------------------------------------------------------------
relatorio = []
curvas = []
status = "ok"

try:
    pontos = IN[0]
    dados = list(IN[1]) if len(IN) > 1 and IN[1] else []
    criar = bool(IN[2]) if len(IN) > 2 else False

    d = list(DEFAULTS)
    for i, v in enumerate(dados):
        if i < len(d) and v is not None:
            d[i] = v

    vel   = int(snap(float(d[0]), sorted(T6.keys())))
    tipo  = str(d[1]).strip().upper()[:1]
    resid = bool(d[2])
    pista = str(d[3]).strip().upper()[:1]          # S ou D
    local = str(d[4]).strip().upper()[:1]          # R ou U
    hw    = float(d[5])
    ha    = float(d[6]) / 2.0
    rc    = float(d[7])
    greide = float(d[8])
    vcs   = float(d[9])
    vce   = float(d[10])
    lr    = float(d[12])
    faz_gota = bool(d[13])
    rg    = float(d[14])
    faz_op = bool(d[15])

    nivel = 1 if (tipo == "A" and resid) else (2 if tipo in ("A", "B") else (3 if tipo == "C" else 4))
    veic = "VP" if tipo in ("A", "B") else ("CO" if tipo == "C" else "SR")
    wf = float(d[11]) if float(d[11]) > 0 else (2.50 if nivel == 1 else 3.60)

    # ---- comprimentos ------------------------------------------------
    if nivel <= 2:
        xd, xa = 15.0, 15.0
        ld, la = 30.0, 30.0
        fd, fa = 1.0, 1.0
        vcs_u, vce_u = 0, 0
    else:
        xd, bruto_d, vcs_u = comp_tabela(T6, vel, snap_vc(vcs))
        xa, bruto_a, vce_u = comp_tabela(T7, vel, snap_vc(vce))
        fd = f_desac(greide)
        fa = f_acel(greide, vel, vce)
        ld = max(xd, bruto_d * fd)
        la = max(xa, bruto_a * fa)

    # ---- versores ----------------------------------------------------
    pRm, pRj = pt2d(pontos[0]), pt2d(pontos[1])
    pAr, pAp = pt2d(pontos[2]), pt2d(pontos[3])
    dR = vnorm(vsub(pRj, pRm))                 # sentido do trafego
    dA = vnorm(vsub(pAp, pAr))                 # do eixo para a propriedade
    pI = lint(pRm, dR, pAr, dA)
    if pI is None:
        raise ValueError("Os dois eixos sao paralelos - confira a ordem dos 4 pontos.")
    nR = vperp(dR)
    if vdot(nR, dA) < 0:
        nR = vmul(nR, -1.0)                    # normal apontando para o acesso
    eA = vperp(dA)
    if vdot(eA, dR) < 0:
        eA = vmul(eA, -1.0)                    # perpendicular ao acesso, para jusante
    ang = math.degrees(math.acos(max(-1.0, min(1.0, vdot(dA, vmul(dR, -1.0))))))

    # ---- geometria ---------------------------------------------------
    shapes = []
    n3 = (nivel == 3)
    s1, p1 = lado(pI, dR, nR, dA, eA, -1, hw, ha, wf, rc, ld, xd, n3, lr)   # desaceleracao
    s2, p2 = lado(pI, dR, nR, dA, eA,  1, hw, ha, wf, rc, la, xa, n3, lr)   # aceleracao
    if s1: shapes.append(s1)
    if s2: shapes.append(s2)
    if p1 and p2:
        shapes.append({"layer": LAY_LIMPA, "verts": [p1, p2], "bulges": [0.0, 0.0], "closed": False})

    if pista == "S" and faz_op:
        nOp = vmul(nR, -1.0)
        shapes.append(lado_oposto(pI, dR, nOp, dA, eA,  1, hw, ha, wf, ld, xd, n3))
        shapes.append(lado_oposto(pI, dR, nOp, dA, eA, -1, hw, ha, wf, la, xa, n3))

    if faz_gota:
        gs, msg = gota(pI, nR, dA, eA, hw, float(d[16]), rg, GOTA_LADO if pista == "S" else 0)
        shapes.extend(gs)
        if msg:
            status = "aviso: " + msg

    # ---- preview no Dynamo -------------------------------------------
    try:
        import clr
        clr.AddReference("ProtoGeometry")
        from Autodesk.DesignScript.Geometry import Point as DPoint, Line as DLine, Arc as DArc
        for sh in shapes:
            vs = sh["verts"] + ([sh["verts"][0]] if sh["closed"] else [])
            for i in range(len(vs) - 1):
                a, bpt = vs[i], vs[i + 1]
                bg = sh["bulges"][i] if i < len(sh["bulges"]) else 0.0
                if vlen(vsub(bpt, a)) < 1e-9:
                    continue
                if abs(bg) < 1e-9:
                    curvas.append(DLine.ByStartPointEndPoint(
                        DPoint.ByCoordinates(a[0], a[1], 0), DPoint.ByCoordinates(bpt[0], bpt[1], 0)))
                else:
                    ch = vlen(vsub(bpt, a))
                    mid = vmul(vadd(a, bpt), 0.5)
                    per = vperp(vnorm(vsub(bpt, a)))
                    mid = vsub(mid, vmul(per, bg * ch / 2.0))
                    curvas.append(DArc.ByThreePoints(
                        DPoint.ByCoordinates(a[0], a[1], 0),
                        DPoint.ByCoordinates(mid[0], mid[1], 0),
                        DPoint.ByCoordinates(bpt[0], bpt[1], 0)))
    except Exception:
        status += " | preview indisponivel: " + traceback.format_exc().splitlines()[-1]

    # ---- criacao no desenho ------------------------------------------
    criadas = 0
    if criar:
        import clr
        clr.AddReference("AcMgd")
        clr.AddReference("AcCoreMgd")
        clr.AddReference("AcDbMgd")
        from Autodesk.AutoCAD.DatabaseServices import (
            Polyline, BlockTable, BlockTableRecord, LayerTable, LayerTableRecord, OpenMode)
        from Autodesk.AutoCAD.Geometry import Point2d
        from Autodesk.AutoCAD.Colors import Color, ColorMethod
        try:
            from Autodesk.AutoCAD.ApplicationServices.Core import Application
        except Exception:
            from Autodesk.AutoCAD.ApplicationServices import Application
        import System

        adoc = Application.DocumentManager.MdiActiveDocument
        db = adoc.Database
        lock = adoc.LockDocument()
        tr = db.TransactionManager.StartTransaction()
        try:
            bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead)
            btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite)
            lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead)
            for nome in set([s["layer"] for s in shapes]):
                if not lt.Has(nome):
                    lt.UpgradeOpen()
                    ltr = LayerTableRecord()
                    ltr.Name = nome
                    try:
                        ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, System.Int16(COR.get(nome, 7)))
                    except Exception:
                        pass
                    lt.Add(ltr)
                    tr.AddNewlyCreatedDBObject(ltr, True)
            for sh in shapes:
                pl = Polyline()
                pl.SetDatabaseDefaults()
                for i, v in enumerate(sh["verts"]):
                    bg = sh["bulges"][i] if i < len(sh["bulges"]) else 0.0
                    pl.AddVertexAt(i, Point2d(v[0], v[1]), bg, 0.0, 0.0)
                pl.Closed = sh["closed"]
                pl.Layer = sh["layer"]
                btr.AppendEntity(pl)
                tr.AddNewlyCreatedDBObject(pl, True)
                criadas += 1
            tr.Commit()
        finally:
            tr.Dispose()
            lock.Dispose()

    # ---- relatorio ---------------------------------------------------
    dv = d_visib(vel) * f_visib(greide, vel)
    dve = d_visib(vel, True) * f_visib(greide, vel)
    chave = ("RURAL" if local == "R" else "URBANO") + ("-SIMPLES-" if pista == "S" else "-DUPLA-") + \
            ("AB" if tipo in ("A", "B") else "CD")
    d5 = T5.get(chave)
    r9 = T9.get(int(snap(ang, list(T9.keys()))), {}).get(veic, (None, None))

    R = relatorio.append
    R("ACESSO - DNIT/IPR-728 (2a ed. 2024)")
    R("-" * 58)
    R("Tipo do acesso ............ {0}   (veiculo critico {1})".format(tipo, veic))
    R("Nivel da faixa mud. vel ... {0}".format(nivel))
    R("Velocidade diretriz ....... {0} km/h    greide {1:.1f} %".format(vel, greide))
    R("Angulo de conversao ....... {0:.1f} graus".format(ang))
    R("Intersecao dos eixos ...... {0:.3f} , {1:.3f}".format(pI[0], pI[1]))
    R("")
    R("DESACELERACAO  taper {0:.2f} + mesa {1:.2f} = {2:.2f} m   (fator greide {3:.2f})".format(
        xd, ld - xd, ld, fd))
    R("ACELERACAO     taper {0:.2f} + mesa {1:.2f} = {2:.2f} m   (fator greide {3:.2f})".format(
        xa, la - xa, la, fa))
    if nivel >= 3:
        R("Colunas usadas nas Tabelas 6 e 7: saida {0} km/h / entrada {1} km/h".format(vcs_u, vce_u))
    R("Largura da faixa .......... {0:.2f} m{1}".format(
        wf, "  (3,60 nos 30 m centrais e 2,50 no restante)" if nivel == 3 else ""))
    R("Limpa-rodas ............... {0:.2f} m".format(lr))
    R("")
    R("Dist. min. de visibilidade  {0:.0f} m   (veiculos especiais {1:.0f} m)".format(dv, dve))
    R("Dist. min. a ponte/viaduto  {0:.0f} m".format(max(200.0, dv)))
    if d5:
        R("Dist. min. entre acessos .. A/B {0} m | C/D {1} m | E,F,int.,retorno {2} m".format(*d5))
    if r9[0]:
        R("Raio min. Tab.9 circular .. {0} m".format(r9[0]))
    if r9[1]:
        R("Raio min. Tab.9 c/ taper .. {0} m".format(r9[1]))
    R("-" * 58)
    R("{0} polilinha(s) criada(s) no desenho.".format(criadas) if criar
      else "Modo preview - ligue 'criar' para gravar no desenho.")

except Exception:
    status = "ERRO:\n" + traceback.format_exc()
    relatorio.append(status)

OUT = ["\n".join(relatorio), curvas, status]