"""Saídas: LandXML (alinhamentos + greide, importável no Civil 3D), CSVs e figuras."""
from __future__ import annotations

import csv
import datetime as dt
import math
from xml.sax.saxutils import escape

import numpy as np


def _ne(p) -> str:
    return f"{p[1]:.4f} {p[0]:.4f}"  # LandXML: Norte Leste


def landxml(caminho: str, resultados: list[dict], nome_conjunto: str = "Traçado preliminar"):
    agora = dt.datetime.now()
    L = ['<?xml version="1.0" encoding="UTF-8"?>',
         f'<LandXML xmlns="http://www.landxml.org/schema/LandXML-1.2" version="1.2" '
         f'date="{agora:%Y-%m-%d}" time="{agora:%H:%M:%S}">',
         '  <Units><Metric areaUnit="squareMeter" linearUnit="meter" volumeUnit="cubicMeter" '
         'temperatureUnit="celsius" pressureUnit="milliBars" angularUnit="decimal degrees" '
         'directionUnit="decimal degrees"/></Units>',
         '  <Application name="PROJETA Traçado Preliminar" manufacturer="Point Top" version="0.1"/>',
         f'  <Alignments name="{escape(nome_conjunto)}">']
    for r in resultados:
        if r.get("referencia"):
            continue
        al = r["alinhamento"]
        L.append(f'    <Alignment name="{escape(al.nome)}" length="{al.comprimento:.4f}" staStart="0.0000" '
                 f'desc="estudo preliminar — não é projeto">')
        L.append('      <CoordGeom>')
        for e in al.elementos:
            if e.tipo == "reta":
                L.append(f'        <Line length="{e.comprimento:.4f}"><Start>{_ne(e.p0)}</Start>'
                         f'<End>{_ne(e.p1)}</End></Line>')
            else:
                rot = "ccw" if e.delta > 0 else "cw"
                L.append(f'        <Curve rot="{rot}" crvType="arc" radius="{e.raio:.4f}" '
                         f'length="{e.comprimento:.4f}" delta="{math.degrees(abs(e.delta)):.6f}">'
                         f'<Start>{_ne(e.p0)}</Start><Center>{_ne(e.centro)}</Center>'
                         f'<End>{_ne(e.p1)}</End><PI>{_ne(e.pi)}</PI></Curve>')
        L.append('      </CoordGeom>')
        pv = r["pvis"]
        L.append(f'      <Profile name="{escape(al.nome)}">')
        L.append(f'        <ProfAlign name="{escape(al.nome)} - greide expedito">')
        for sta, el in pv:
            L.append(f'          <PVI>{sta:.4f} {el:.4f}</PVI>')
        L.append('        </ProfAlign>')
        L.append('      </Profile>')
        L.append('    </Alignment>')
    L += ['  </Alignments>', '</LandXML>']
    with open(caminho, "w", encoding="utf-8") as f:
        f.write("\n".join(L) + "\n")


def csv_pis(caminho: str, resultados: list[dict]):
    with open(caminho, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f, delimiter=";")
        w.writerow(["alternativa", "elemento", "E_PI", "N_PI", "deflexao_graus", "raio_m",
                    "tangente_m", "desenvolvimento_m", "estaca_PC_m", "estaca_PT_m"])
        for r in resultados:
            if r.get("referencia"):
                continue
            n = 0
            for e in r["alinhamento"].elementos:
                if e.tipo != "curva":
                    continue
                n += 1
                t = e.raio * math.tan(abs(e.delta) / 2)
                w.writerow([r["id"], f"PI-{n}", br(e.pi[0], 3), br(e.pi[1], 3),
                            br(math.degrees(e.delta), 3, sinal=True), br(e.raio, 2), br(t, 2),
                            br(e.comprimento, 2), br(e.est0, 2), br(e.est0 + e.comprimento, 2)])


def br(v, casas: int = 0, sinal: bool = False) -> str:
    """Número no formato brasileiro: 1.234.567,89"""
    if isinstance(v, float) and not math.isfinite(v):
        return "-"
    s = f"{v:{'+' if sinal else ''},.{casas}f}"
    return s.replace(",", "§").replace(".", ",").replace("§", ".")


COLUNAS = [("id", "Alternativa", None), ("extensao_m", "Extensão (m)", 0),
           ("n_curvas", "Curvas", None), ("raio_min_m", "Raio mín. (m)", 0),
           ("rampa_max_pct", "Rampa máx. greide (%)", 2),
           ("corte_m3", "Corte (m³)", 0), ("aterro_m3", "Aterro (m³)", 0),
           ("balanco_m3", "Corte - aterro (m³)", "+0"),
           ("corte_max_m", "Corte máx. eixo (m)", 1), ("aterro_max_m", "Aterro máx. eixo (m)", 1),
           ("talude_nao_fecha_m", "Talude não fecha (m)", 0),
           ("n_bueiros", "Bueiros", None), ("n_pontes", "Pontes", None),
           ("custo_total", "Índice de custo (R$)", 0), ("sobreposicao", "Sobreposição", "%")]


def _fmt(v, f):
    if v is None:
        return "-"
    if f is None:
        return str(v)
    if f == "%":
        return f"{v * 100:.0f}%"
    if f == "+0":
        return br(v, 0, sinal=True)
    return br(v, f)


def tabela(resultados: list[dict]) -> list[list[str]]:
    linhas = [[c[1] for c in COLUNAS]]
    for r in resultados:
        linhas.append([_fmt(r[k], f) for k, _, f in COLUNAS])
    return linhas


def csv_comparativo(caminho: str, resultados: list[dict]):
    with open(caminho, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f, delimiter=";")
        for l in tabela(resultados):
            w.writerow(l)


def _janela(T, resultados, A, B):
    """Recorte do mapa: envoltória das alternativas + margem (o MDT pode ser bem maior)."""
    xs = np.concatenate([r["perfil"]["x"] for r in resultados] + [[A[0], B[0]]])
    ys = np.concatenate([r["perfil"]["y"] for r in resultados] + [[A[1], B[1]]])
    m = max(150.0, 0.25 * max(xs.max() - xs.min(), ys.max() - ys.min()))
    j0 = max(0, int((xs.min() - m - T.x0) / T.dx))
    j1 = min(T.nx, int((xs.max() + m - T.x0) / T.dx) + 2)
    i0 = max(0, int((ys.min() - m - T.y0) / T.dx))
    i1 = min(T.ny, int((ys.max() + m - T.y0) / T.dx) + 2)
    return slice(i0, i1), slice(j0, j1)


def figuras(dir_saida: str, T, C, resultados, A, B, restricoes, titulo: str):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.colors import LightSource, LinearSegmentedColormap

    cores = ["#d62728", "#1f77b4", "#2ca02c", "#9467bd", "#ff7f0e", "#8c564b"]
    si, sj = _janela(T, resultados, A, B)
    z = T.z[si, sj]
    ok = np.isfinite(z)
    x0, y0 = T.x0 + sj.start * T.dx, T.y0 + si.start * T.dx
    ny, nx = z.shape
    ext = [x0 - T.dx / 2, x0 + (nx - 0.5) * T.dx, y0 - T.dx / 2, y0 + (ny - 0.5) * T.dx]
    zf = np.where(ok, z, np.nanmin(z))
    ls = LightSource(azdeg=315, altdeg=45)
    relevo = LinearSegmentedColormap.from_list("relevo", plt.cm.terrain(np.linspace(0.28, 0.95, 64)))
    rgb = ls.shade(zf, cmap=relevo, vert_exag=2, blend_mode="soft", vmin=np.nanmin(z), vmax=np.nanmax(z))
    rgb[~ok] = 1.0
    fig, ax = plt.subplots(figsize=(11, 11 * ny / nx + 0.8))
    ax.imshow(rgb, origin="lower", extent=ext)
    X, Y = np.meshgrid(x0 + np.arange(nx) * T.dx, y0 + np.arange(ny) * T.dx)
    zmin, zmax = np.nanmin(z), np.nanmax(z)
    passo = 1 if zmax - zmin < 60 else 5
    niv = np.arange(math.floor(zmin / passo) * passo, zmax + passo, passo)
    ax.contour(X, Y, z, levels=niv, colors="k", linewidths=0.25, alpha=0.5)
    ax.contour(X, Y, z, levels=niv[niv % (5 * passo) == 0], colors="k", linewidths=0.6, alpha=0.7)
    tv = np.ma.masked_where(~C.talvegue[si, sj], np.ones_like(z))
    ax.imshow(tv, origin="lower", extent=ext, cmap="Blues", vmin=0, vmax=1.3, alpha=0.9)
    for r in restricoes:
        pg = np.asarray(r["poligono"])
        ax.fill(pg[:, 0], pg[:, 1], facecolor="none", edgecolor="k",
                hatch="xx" if r.get("tipo", "proibido") == "proibido" else "..", linewidth=1)
    k = 0
    ref_desenhada = False
    for r in resultados:
        pf = r["perfil"]
        if r.get("referencia"):
            if not ref_desenhada:
                ax.plot(pf["x"], pf["y"], "--", color="k", lw=2.2,
                        label=f'{r["alinhamento"].nome} (seu eixo)  {r["extensao_m"]:.0f} m')
                ref_desenhada = True
            continue
        c = cores[k % len(cores)]
        k += 1
        ax.plot(pf["x"], pf["y"], color=c, lw=2.4, label=f'{r["id"]}  {r["extensao_m"]:.0f} m')
        P = r["alinhamento"].pis
        ax.plot(P[:, 0], P[:, 1], ":", color=c, lw=0.8)
        for t in r["travessias"]:
            kk = int(np.argmin(np.abs(pf["s"] - t["estaca"])))
            ax.plot(pf["x"][kk], pf["y"][kk], "o", ms=7, mfc="white", mec=c, mew=2)
    ax.plot(*A, "s", ms=10, color="k"); ax.annotate("A", A, xytext=(6, 6), textcoords="offset points", weight="bold")
    ax.plot(*B, "s", ms=10, color="k"); ax.annotate("B", B, xytext=(6, 6), textcoords="offset points", weight="bold")
    ax.set_xlim(ext[0], ext[1]); ax.set_ylim(ext[2], ext[3])
    ax.set_aspect("equal")
    ax.ticklabel_format(useOffset=False, style="plain")
    ax.tick_params(labelsize=7)
    ax.set_xlabel("E (m)"); ax.set_ylabel("N (m)")
    ax.set_title(titulo + f"\n○ travessia de talvegue   azul = talvegues   pontilhado = linha de PIs   "
                 f"curvas de nível a {passo} m", fontsize=10)
    ax.legend(loc="upper left", fontsize=9, framealpha=0.9)
    fig.tight_layout()
    fig.savefig(f"{dir_saida}/mapa.png", dpi=140)
    plt.close(fig)

    n = len(resultados)
    fig, axs = plt.subplots(n, 1, figsize=(11, 2.6 * n + 0.4), squeeze=False)
    k = 0
    for i, r in enumerate(resultados):
        ax = axs[i, 0]
        pf = r["perfil"]
        if r.get("referencia"):
            c = "k"
        else:
            c = cores[k % len(cores)]
            k += 1
        ax.fill_between(pf["s"], pf["terreno"], pf["greide"], where=pf["greide"] >= pf["terreno"],
                        color="#9ecae1", alpha=0.6, label="aterro")
        ax.fill_between(pf["s"], pf["terreno"], pf["greide"], where=pf["greide"] < pf["terreno"],
                        color="#fdae6b", alpha=0.6, label="corte")
        ax.plot(pf["s"], pf["terreno"], color="#6b4f2a", lw=1, label="terreno")
        ax.plot(pf["s"], pf["greide"], color=c, lw=1.8, label="greide")
        for t in r["travessias"]:
            ax.axvline(t["estaca"], color="#3182bd", lw=0.8, ls="--")
        ax.set_title(f'{r["id"]} — {br(r["extensao_m"])} m, rampa máx. {br(r["rampa_max_pct"], 1)}%, '
                     f'corte {br(r["corte_m3"])} m³ / aterro {br(r["aterro_m3"])} m³', fontsize=9)
        ax.set_ylabel("cota (m)", fontsize=8)
        ax.tick_params(labelsize=7)
        ax.grid(alpha=0.3)
        if i == 0:
            ax.legend(fontsize=7, ncol=4, loc="best")
    axs[-1, 0].set_xlabel("estaca (m)")
    fig.tight_layout()
    fig.savefig(f"{dir_saida}/perfis.png", dpi=130)
    plt.close(fig)
