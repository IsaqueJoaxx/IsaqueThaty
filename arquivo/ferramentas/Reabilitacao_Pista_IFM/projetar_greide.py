"""REABILITAÇÃO DE PISTA — IFM: projeto do greide pela nova concepção (out/2026).

Uso (exemplo):
  python projetar_greide.py --pista LEV_pista.csv --fundo ACOST_ld.csv --fundo ACOST_le.csv ^
         --eixo EIXO.xml --saida Projeto_BR222_268 --rebaixo 0.26 --espessura 0.23

Sem --eixo, gera um EIXO PROVISÓRIO a partir do levantamento da pista
(somente para prévia; a estaca inicial vem de --estaca-inicial).

Entradas
  --pista  levantamento da pista existente (antes do rebaixo). Fundo PREVISTO = cota - rebaixo.
  --fundo  levantamento(s) depois do rebaixo (planilha de acostamento). Fundo MEDIDO.
           Pontos altos colados ao eixo (salto 0,15 a 0,45 m) = REFERÊNCIA ORIGINAL, não fundo.
Saídas (pasta --saida, nunca sobrescreve)
  NOTA_SERVICO_GREIDE_IFM.xlsx, GREIDE_IFM.xml (LandXML: eixo + perfil), controles.csv, resumo.json
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from datetime import datetime
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from reabilitacao_core import (Eixo, Superficie, curvatura_estimada, eixo_provisorio, iri,  # noqa: E402
                               ler_eixo_landxml, ler_levantamento, otimizar_greide, parse_num,
                               separar_referencias)
from saidas_ifm import fmt_estaca as fmt_est  # noqa: E402

OFFS = (-5.50, -3.50, 0.0, 3.50, 5.50)
NOMES = {-5.50: 'LE 5,50', -3.50: 'LE 3,50', 0.0: 'EIXO', 3.50: 'LD 3,50', 5.50: 'LD 5,50'}


# --------------------------------------------------------------------------
def superelevacao(estacas, eixo: Eixo, V, emax, caimento, R_dispensa, rampa_rel, larg):
    """Caimento de projeto por lado (q_le, q_ld), sinal do eixo para fora.

    Tangente: q = -caimento nos dois lados. Curva com R < R_dispensa: plano único
    com e = max(caimento, emax*(2*Rmin/R - (Rmin/R)^2)) (DNIT), girando no eixo;
    transição linear com rampa relativa máxima informada, 2/3 na tangente.
    """
    f = 0.19 - V / 1600.0
    Rmin = V * V / (127.0 * (emax + f))
    q_le = np.full(len(estacas), -caimento)
    q_ld = np.full(len(estacas), -caimento)
    curvas = []
    if eixo.curvas:
        for s0, s1, R, lado in eixo.curvas:
            if R < R_dispensa:
                curvas.append((s0, s1, R, lado))
    else:  # eixo provisório: curva estimada pela variação de rumo
        s, R, lado = curvatura_estimada(eixo)
        dentro = R < R_dispensa
        i = 0
        while i < len(s):
            if dentro[i]:
                j = i
                while j + 1 < len(s) and dentro[j + 1]:
                    j += 1
                Rm = float(np.percentile(R[i:j + 1], 25))
                ld = int(np.sign(np.sum(lado[i:j + 1]))) or 1
                curvas.append((float(s[i]), float(s[j]), Rm, ld))
                i = j + 1
            else:
                i += 1
    # junta curvas do mesmo lado separadas por menos de 80 m (ruído do eixo provisório)
    curvas.sort()
    juntas = []
    for cv in curvas:
        if juntas and cv[3] == juntas[-1][3] and cv[0] - juntas[-1][1] < 80:
            s0, s1, R0, l0 = juntas[-1]
            juntas[-1] = (s0, cv[1], min(R0, cv[2]), l0)
        else:
            juntas.append(cv)
    curvas = juntas
    det = []
    for s0, s1, R, lado in curvas:
        e = max(caimento, emax * (2 * Rmin / R - (Rmin / R) ** 2))
        e = min(e, emax)
        Lt = max(20.0, larg * (e + caimento) / rampa_rel)      # giro do lado externo de -c até +e
        a0, a1 = s0 - 2 * Lt / 3, s0 + Lt / 3                  # entrada
        b0, b1 = s1 - Lt / 3, s1 + 2 * Lt / 3                  # saída
        for k, st in enumerate(estacas):
            if st < a0 or st > b1:
                continue
            if st < a1:
                t = (st - a0) / (a1 - a0)
            elif st <= b0:
                t = 1.0
            else:
                t = 1 - (st - b0) / (b1 - b0)
            externo = -caimento + t * (e + caimento)
            interno = -caimento - t * (e - caimento)
            if lado > 0:   # curva à direita: LD é o lado interno
                q_ld[k], q_le[k] = interno, externo
            else:
                q_le[k], q_ld[k] = interno, externo
        det.append(dict(inicio=s0, fim=s1, raio_m=R, lado='direita' if lado > 0 else 'esquerda',
                        superelevacao_pct=e * 100, transicao_m=Lt, entrada=(a0, a1), saida=(b0, b1)))
    return q_le, q_ld, det, Rmin


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--pista', required=True)
    ap.add_argument('--fundo', action='append', default=[])
    ap.add_argument('--eixo')
    ap.add_argument('--eixo-nome')
    ap.add_argument('--estaca-inicial', type=float, help='estaca do 1º ponto quando não há eixo XML')
    ap.add_argument('--saida', required=True)
    ap.add_argument('--passo', type=float, default=5.0)
    ap.add_argument('--rebaixo', type=float, default=0.26)
    ap.add_argument('--espessura', type=float, default=0.23)
    ap.add_argument('--caimento', type=float, default=0.02)
    ap.add_argument('--velocidade', type=float, default=60.0)
    ap.add_argument('--emax', type=float, default=0.06)
    ap.add_argument('--raio-dispensa', type=float, default=1800.0)
    ap.add_argument('--rampa-relativa', type=float, default=0.006)
    ap.add_argument('--iri-alvo', type=float, default=1.0,
                    help='IRI teórico máximo do greide em qualquer trecho de 100 m (m/km). '
                         'Recebimento DNIT IS 22/2019: IRI <= 2,5 m/km medido; a meta deixa margem para a execução.')
    ap.add_argument('--suavidade-mm', type=float, default=None,
                    help='fixa a variação máxima de rampa entre estacas (mm, 2ª diferença) em vez de usar --iri-alvo')
    ap.add_argument('--alternativas-mm', default='2.5,5,7.5,10,15,20')
    ap.add_argument('--folga', type=float, default=0.0, help='folga construtiva somada à espessura mínima (m)')
    ap.add_argument('--pista-estado', choices=['antes', 'depois'], default='antes',
                    help='antes: pista levantada antes do rebaixo (fundo previsto = cota - rebaixo); '
                         'depois: pista já cortada (fundo = cota levantada)')
    ap.add_argument('--so-controles', action='store_true',
                    help='exige a espessura mínima só no eixo, 3,50 e 5,50 (padrão: a cada 0,50 m da seção)')
    ap.add_argument('--fixar', action='append', default=[], metavar='ESTACA=COTA',
                    help='amarra o greide do eixo numa cota (ex.: 271200=9.412). Pode repetir.')
    a = ap.parse_args(argv)
    a.controle_continuo = not a.so_controles

    out = Path(a.saida)
    if out.exists():
        raise SystemExit(f'A pasta {out} já existe: escolha outra (saídas anteriores são preservadas).')

    alertas = []
    # ---------------- leitura
    pista, info_p = ler_levantamento(a.pista)
    fundos = [ler_levantamento(f) for f in a.fundo]
    if a.eixo:
        eixo = ler_eixo_landxml(a.eixo, a.eixo_nome)
        info_eixo = dict(fonte=eixo.fonte, nome=eixo.nome, inicio=float(eixo.est[0]), fim=float(eixo.est[-1]))
    else:
        if a.estaca_inicial is None:
            raise SystemExit('Sem --eixo: informe --estaca-inicial para o eixo provisório.')
        eixo, inf = eixo_provisorio(pista, a.estaca_inicial)
        info_eixo = dict(fonte='PROVISÓRIO (linha média do levantamento)', nome=eixo.nome,
                         inicio=float(eixo.est[0]), fim=float(eixo.est[-1]), **inf)
        alertas.append('EIXO PROVISÓRIO gerado do levantamento: estacas e afastamentos são aproximados. '
                       'Substituir pelo eixo oficial (LandXML) antes de liberar para campo.')

    def projetar(pts):
        s, o, d = eixo.projetar([p.e for p in pts], [p.n for p in pts])
        for p, si, oi, di in zip(pts, s, o, d):
            p.estaca, p.afast, p.dist_eixo = float(si), float(oi), float(di)
            if si <= eixo.est[0] + 0.01 or si >= eixo.est[-1] - 0.01:
                p.papel = 'FORA DO EIXO'
    projetar(pista)
    for p in pista:
        if not p.papel:
            p.papel = 'PISTA EXISTENTE'
    lados = []
    for pts, inf in fundos:
        projetar(pts)
        dentro = [p for p in pts if p.papel != 'FORA DO EIXO']
        separar_referencias(dentro)
        med = float(np.median([p.afast for p in dentro])) if dentro else 0.0
        lado = 1 if med >= 0 else -1
        inf['lado'] = 'LD' if lado > 0 else 'LE'
        inf['fora_do_eixo'] = sum(p.papel == 'FORA DO EIXO' for p in pts)
        inf['referencias'] = sum(p.papel == 'REFERÊNCIA ORIGINAL' for p in pts)
        lados.append((lado, pts, inf))
        if inf['fora_do_eixo']:
            alertas.append(f"{inf['arquivo']}: {inf['fora_do_eixo']} pontos fora da extensão do eixo (ignorados).")
    n_fora = sum(p.papel == 'FORA DO EIXO' for p in pista)
    if n_fora:
        alertas.append(f'{info_p["arquivo"]}: {n_fora} pontos fora da extensão do eixo (ignorados).')

    # ---------------- superfícies
    pp = [p for p in pista if p.papel == 'PISTA EXISTENTE']
    S_ex = Superficie('pista existente', [p.estaca for p in pp], [p.afast for p in pp], [p.z for p in pp])
    S_f = {}
    refs = []
    for lado, pts, inf in lados:
        fp = [p for p in pts if p.papel == 'FUNDO MEDIDO' and p.afast * lado >= -0.5]
        refs += [p for p in pts if p.papel == 'REFERÊNCIA ORIGINAL']
        if len(fp) >= 3:
            S = Superficie(f'fundo {inf["lado"]}', [p.estaca for p in fp], [p.afast for p in fp], [p.z for p in fp])
            S_f.setdefault(lado, []).append(S)

    desconto = a.rebaixo if a.pista_estado == 'antes' else 0.0
    # ---------------- estacas e controles
    s_ini = math.ceil((min(p.estaca for p in pp)) / a.passo) * a.passo
    s_fim = math.floor((max(p.estaca for p in pp)) / a.passo) * a.passo
    est = np.arange(s_ini, s_fim + 1e-6, a.passo)
    n = len(est)
    C = {}   # por offset: dict de arrays
    for o in OFFS:
        z_ex, sit_ex = S_ex.amostrar(est, o)
        fundo = np.full(n, np.nan); fonte = np.array([''] * n, dtype=object)
        lado = 1 if o > 0 else -1
        if o == 0:
            cand = S_f.get(1, []) + S_f.get(-1, [])
        else:
            cand = S_f.get(lado, [])
        for S in cand:
            zf, sf = S.amostrar(est, o)
            m = np.isnan(fundo) & ~np.isnan(zf)
            fundo[m] = zf[m]; fonte[m] = np.where(sf[m] == 'extrapolado', 'MEDIDO (extrap.)', 'MEDIDO')
        m = np.isnan(fundo) & ~np.isnan(z_ex)
        fundo[m] = z_ex[m] - desconto
        rot = 'PREVISTO' if a.pista_estado == 'antes' else 'PISTA CORTADA'
        fonte[m] = np.where(sit_ex[m] == 'extrapolado', rot + ' (extrap.)', rot)
        fundo = np.round(fundo, 3)          # nota de serviço trabalha ao milímetro
        C[o] = dict(z_ex=z_ex, sit_ex=sit_ex, fundo=fundo, fonte=fonte)

    # coerência pista x fundo medido onde os dois existem
    coer = []
    for o in OFFS:
        c = C[o]
        m = np.char.startswith(c['fonte'].astype(str), 'MEDIDO') & ~np.isnan(c['z_ex'])
        if m.sum():
            dif = c['z_ex'][m] - c['fundo'][m]
            coer.append(dict(controle=NOMES[o], estacas=int(m.sum()), rebaixo_medido_mediana=float(np.median(dif)),
                             p10=float(np.percentile(dif, 10)), p90=float(np.percentile(dif, 90))))
            if abs(np.median(dif) - desconto) > 0.08:
                alertas.append(f'{NOMES[o]}: diferença pista − fundo medido = {np.median(dif):.3f} m (mediana, '
                               f'{m.sum()} estacas); esperado {desconto:.2f} m para pista "{a.pista_estado}" do corte. '
                               f'Conferir o estado do levantamento da pista.')
    if refs:
        zr_s = np.array([p.estaca for p in refs]); zr = np.array([p.z for p in refs])
        zex_r, _ = S_ex.amostrar(zr_s, np.array([p.afast for p in refs]))
        ok = ~np.isnan(zex_r)
        if ok.sum():
            d = zr[ok] - zex_r[ok]
            coer.append(dict(controle='REFERÊNCIA ORIGINAL x pista', estacas=int(ok.sum()),
                             rebaixo_medido_mediana=float(np.median(d)), p10=float(np.percentile(d, 10)),
                             p90=float(np.percentile(d, 90))))
            if np.median(d) > 0.10 and a.pista_estado == 'antes':
                alertas.append(f'Pontos altos colados ao eixo (referência original) estão {np.median(d):.3f} m '
                               f'ACIMA do levantamento da pista ({ok.sum()} pontos). Se eles são a superfície '
                               f'original, o levantamento da pista nesse trecho já é do fundo cortado.')

    # ---------------- caimentos de projeto
    q_le, q_ld, curvas, Rmin = superelevacao(est, eixo, a.velocidade / 1.0, a.emax, a.caimento,
                                              a.raio_dispensa, a.rampa_relativa, 3.50)
    if curvas and eixo.fonte.startswith('PROV') or (curvas and not eixo.curvas):
        alertas.append('Superelevação calculada sobre curva ESTIMADA do eixo provisório; recalcular com PC/PT do eixo oficial.')

    def q_de(o):
        return q_le if o < 0 else q_ld

    # ---------------- fundo na grade contínua (a cada 0,50 m), para conferir entre os controles
    grade = np.round(np.arange(-5.5, 5.5001, 0.5), 2)
    FZ = {}
    for o in grade:
        lado = 1 if o > 0 else -1
        fz = np.full(n, np.nan)
        for S in (S_f.get(lado, []) if o != 0 else S_f.get(1, []) + S_f.get(-1, [])):
            zf, _ = S.amostrar(est, o)
            mm_ = np.isnan(fz) & ~np.isnan(zf); fz[mm_] = zf[mm_]
        if abs(o) <= 3.5:
            ze, _ = S_ex.amostrar(est, o)
            mm_ = np.isnan(fz) & ~np.isnan(ze); fz[mm_] = ze[mm_] - desconto
        FZ[float(o)] = np.round(fz, 3)

    # ---------------- limite inferior por espessura
    esp = a.espessura + a.folga
    lb_offs = list(OFFS) if not a.controle_continuo else sorted(set(list(OFFS) + [float(o) for o in grade]))
    lb_parts = []
    for o in lb_offs:
        f = C[o]['fundo'] if o in C else FZ[o]
        lb_parts.append(f + esp - (q_de(o) * abs(o) if o else 0.0))
    LBm = np.vstack(lb_parts)
    LB = np.nanmax(np.where(np.isnan(LBm), -np.inf, LBm), axis=0)
    LB[np.isinf(LB)] = np.nan
    idx = np.argmax(np.where(np.isnan(LBm), -np.inf, LBm), axis=0)
    def _nome(o):
        return NOMES.get(o, f'{o:+.1f}'.replace('.', ','))
    controla = np.array([_nome(lb_offs[i]) if not np.isnan(LB[k]) else '' for k, i in enumerate(idx)], dtype=object)
    sem_lb = np.isnan(LB)
    if sem_lb.any():
        alertas.append(f'{sem_lb.sum()} estacas sem nenhum fundo (medido ou previsto): greide interpolado, espessura não verificada.')
    fixos = {}
    for fx in a.fixar:
        se, ze = fx.split('=')
        k = int(np.argmin(np.abs(est - parse_num(se))))
        if abs(est[k] - parse_num(se)) > a.passo / 2:
            raise SystemExit(f'--fixar {fx}: estaca fora do trecho')
        fixos[k] = parse_num(ze)
        if not np.isnan(LB[k]) and fixos[k] < LB[k] - 1e-4:
            alertas.append(f'Amarração {fmt_est(est[k])} = {fixos[k]:.3f} está abaixo do limite de espessura '
                           f'({LB[k]:.3f}): a espessura mínima não será atendida ali.')
            LB[k] = fixos[k]

    # ---------------- otimização + alternativas
    TRILHAS = (-2.6, -0.9, 0.0, 0.9, 2.6)

    def avaliar(G):
        topo = {o: (G if o == 0 else G + q_de(o) * abs(o)) for o in OFFS}
        espc = {o: topo[o] - C[o]['fundo'] for o in OFFS}
        # volume na faixa com fundo conhecido (trapézios entre controles, por lado), m3
        vol = 0.0
        for lado_offs in ((0.0, 3.50, 5.50), (0.0, -3.50, -5.50)):
            for o0, o1 in zip(lado_offs, lado_offs[1:]):
                e0, e1 = espc[o0], espc[o1]
                m = ~np.isnan(e0) & ~np.isnan(e1)
                vol += float(np.sum((e0[m] + e1[m]) / 2 * abs(o1 - o0)) * a.passo)
        iri_eixo = iri(est, G, trecho=None)
        seg = {}
        for o in TRILHAS:
            for ini, fim, v in iri(est, G + (q_de(o) * abs(o) if o else 0.0), trecho=100.0):
                key = (ini, fim)
                seg[key] = max(seg.get(key, 0.0), v)
        iri_100 = [(k[0], k[1], v) for k, v in sorted(seg.items())]
        return topo, espc, vol, (iri_eixo[0][2] if iri_eixo else float('nan')), iri_100

    cache = {}

    def projeto(mm):
        if mm not in cache:
            G, st = otimizar_greide(est, LB, delta2=mm / 1000.0, fixos=fixos)
            if G is None:
                cache[mm] = (None, st, None)
            else:
                r = avaliar(G)
                cache[mm] = (G, st, r)
        return cache[mm]

    if a.suavidade_mm is None:
        # maior variação de rampa (menor volume) cujo IRI teórico em 100 m fica dentro da meta
        lo, hi = 0.5, 40.0
        if projeto(lo)[0] is None:
            raise SystemExit('Otimização sem solução mesmo com suavidade máxima: ' + projeto(lo)[1])
        if max(x[2] for x in projeto(hi)[2][4]) <= a.iri_alvo:
            lo = hi
        else:
            for _ in range(14):
                mid = (lo + hi) / 2
                ok = projeto(mid)[0] is not None and max(x[2] for x in projeto(mid)[2][4]) <= a.iri_alvo
                lo, hi = (mid, hi) if ok else (lo, mid)
        delta_mm = round(lo, 2)
        criterio = (f'Menor volume com IRI teórico do greide ≤ {a.iri_alvo:.2f} m/km em todo trecho de 100 m '
                    f'(trilhas ±0,9 e ±2,6 m e eixo) → variação máx. de rampa {delta_mm:.2f} mm a cada {a.passo:.0f} m')
    else:
        delta_mm = a.suavidade_mm
        criterio = f'Variação máx. de rampa fixada em {delta_mm:.2f} mm a cada {a.passo:.0f} m'
    alts = []
    for mm in sorted(set([float(x) for x in a.alternativas_mm.split(',')] + [delta_mm])):
        G_, st, r = projeto(mm)
        if G_ is None:
            alts.append(dict(suavidade_mm=mm, status=st))
            continue
        alts.append(dict(suavidade_mm=mm, status=st, volume_m3=r[2],
                         excesso_medio_cm=float(np.nanmean(G_ - LB) * 100),
                         iri_greide_m_km=r[3], iri_greide_max_100m=max(x[2] for x in r[4]) if r[4] else None))
    G, st, r = projeto(delta_mm)
    if G is None:
        raise SystemExit('Otimização sem solução: ' + st)
    G = np.ceil(G * 1000 - 1e-6) / 1000      # greide ao milímetro, arredondado para cima
    topo, espc, vol, ir, ir100 = avaliar(G)
    # volume de referência: greide = limite inferior puro (sem suavidade)
    _, _, vol_lb, ir_lb, _ = avaliar(np.where(np.isnan(LB), np.interp(est, est[~sem_lb], LB[~sem_lb]), LB))

    # IRI da superfície existente (eixo e trilhas) — indicativo: levantamento a ~5 m
    iri_ex = {}
    for o in (-2.6, -0.9, 0.0, 0.9, 2.6):
        z, _ = S_ex.amostrar(est, o)
        r = iri(est, z, trecho=None)
        iri_ex[o] = r[0][2] if r else float('nan')
    iri_proj = {}
    for o in (-2.6, -0.9, 0.0, 0.9, 2.6):
        prof = G + (q_le if o < 0 else q_ld) * abs(o)
        r = iri(est, prof, trecho=None)
        iri_proj[o] = r[0][2] if r else float('nan')

    # verificação contínua entre controles (a cada 0,5 m)
    min_cont = np.full(n, np.nan); off_min = np.full(n, np.nan)
    for o, fz in FZ.items():
        t = G + (q_le if o < 0 else q_ld) * abs(o)
        e = t - fz
        better = ~np.isnan(e) & (np.isnan(min_cont) | (e < min_cont))
        min_cont[better] = e[better]; off_min[better] = o
    for o in (-3.50, 0.0, 3.50):
        falta = int(np.sum(np.isnan(C[o]['fundo'])))
        if falta:
            alertas.append(f'{NOMES[o]}: {falta} estacas sem levantamento (lacuna): espessura NÃO verificada ali; '
                           f'greide interpolado pelos vizinhos.')
    falta55 = {NOMES[o]: int(np.sum(np.isnan(C[o]['fundo']))) for o in (-5.50, 5.50)}
    alertas.append('Acostamento (5,50): sem levantamento após rebaixo em ' +
                   ', '.join(f'{k} {v} de {n}' for k, v in falta55.items()) +
                   ' estacas; ali a cota de projeto é dada pelo caimento e a espessura fica em branco.')
    n_cont = int(np.sum(min_cont < a.espessura - 0.0005))
    if n_cont:
        alertas.append(f'{n_cont} estacas com espessura < {a.espessura:.2f} m ENTRE os pontos de controle '
                       f'(verificação a cada 0,50 m), porque --so-controles exige só eixo, 3,50 e 5,50; ver coluna "mín. contínua".')

    # ---------------- gravação
    out.mkdir(parents=True)
    linhas = []
    for k in range(n):
        row = dict(estaca=float(est[k]), q_le_pct=float(q_le[k] * 100), q_ld_pct=float(q_ld[k] * 100),
                   limite_inferior=float(LB[k]) if not np.isnan(LB[k]) else None, controla=controla[k],
                   greide=float(G[k]), min_continua=None if np.isnan(min_cont[k]) else float(min_cont[k]),
                   off_min_continua=None if np.isnan(off_min[k]) else float(off_min[k]))
        e_, n_ = eixo.ponto(est[k])
        row.update(e_eixo=float(e_), n_eixo=float(n_))
        for o in OFFS:
            c = C[o]
            tag = NOMES[o]
            row[f'{tag}|existente'] = None if np.isnan(c['z_ex'][k]) else float(c['z_ex'][k])
            row[f'{tag}|fundo'] = None if np.isnan(c['fundo'][k]) else float(c['fundo'][k])
            row[f'{tag}|fonte'] = c['fonte'][k]
            row[f'{tag}|projeto'] = float(topo[o][k])
            row[f'{tag}|espessura'] = None if np.isnan(espc[o][k]) else float(espc[o][k])
        linhas.append(row)

    resumo = dict(
        gerado_em=datetime.now().isoformat(timespec='seconds'),
        eixo=info_eixo, pista=info_p, fundos=[inf for _, _, inf in lados],
        parametros=dict(passo_m=a.passo, rebaixo_m=a.rebaixo, espessura_min_m=a.espessura, folga_m=a.folga,
                        caimento_pct=a.caimento * 100, velocidade_kmh=a.velocidade, emax_pct=a.emax * 100,
                        raio_min_m=Rmin, raio_dispensa_superelevacao_m=a.raio_dispensa,
                        rampa_relativa_max_pct=a.rampa_relativa * 100, suavidade_mm=delta_mm, iri_alvo=a.iri_alvo,
                        criterio_suavidade=criterio,
                        pista_estado=a.pista_estado, controle_continuo=a.controle_continuo,
                        amarracoes={fmt_est(est[k]): v for k, v in fixos.items()}),
        estacas=dict(inicio=float(est[0]), fim=float(est[-1]), quantidade=n),
        curvas=curvas, coerencia=coer,
        volume_m3=vol, volume_limite_inferior_m3=vol_lb,
        iri=dict(projeto_por_trilha=iri_proj, existente_por_trilha_indicativo=iri_ex,
                 projeto_por_100m=[dict(inicio=x[0], fim=x[1], iri=x[2]) for x in ir100]),
        alternativas=alts, alertas=alertas,
        espessura_minima_controles=float(np.nanmin(np.vstack([espc[o] for o in OFFS]))),
    )
    (out / 'resumo.json').write_text(json.dumps(resumo, ensure_ascii=False, indent=2, default=float), encoding='utf-8')
    import csv
    with (out / 'controles.csv').open('w', encoding='utf-8-sig', newline='') as fh:
        w = csv.DictWriter(fh, fieldnames=list(linhas[0]), delimiter=';')
        w.writeheader(); w.writerows(linhas)
    from saidas_ifm import gravar_excel, gravar_landxml
    gravar_excel(out / 'NOTA_SERVICO_GREIDE_IFM.xlsx', linhas, resumo, pista, [p for _, pts, _ in lados for p in pts])
    gravar_landxml(out / 'GREIDE_IFM.xml', eixo, est, G, resumo, q_le, q_ld)
    print(json.dumps(dict(saida=str(out), estacas=n, volume_m3=round(vol, 1),
                          volume_limite_inferior_m3=round(vol_lb, 1),
                          espessura_min_controles=round(resumo['espessura_minima_controles'], 4),
                          iri_projeto=round(ir, 2), alertas=len(alertas)), ensure_ascii=False, indent=2))
    return resumo


if __name__ == '__main__':
    main()
