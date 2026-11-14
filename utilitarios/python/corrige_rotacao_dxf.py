#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
corrige_rotacao_dxf.py
======================
Corrige a rotacao de textos (TEXT / MTEXT / ATTRIB) em arquivos DXF
exportados do Civil 3D, sem precisar abrir o AutoCAD.

Serve para lote: pode rodar em uma pasta inteira de DXF de uma vez.

Instalacao:
    pip install ezdxf

Uso tipico:
    # um arquivo, tudo na horizontal, explodindo os blocos de rotulo
    python corrige_rotacao_dxf.py projeto.dxf --explodir

    # uma pasta inteira, mantendo o alinhamento mas virando o que esta invertido
    python corrige_rotacao_dxf.py C:/projetos/dxf --modo legivel --explodir

    # so ver o que seria alterado, sem gravar nada
    python corrige_rotacao_dxf.py projeto.dxf --explodir --simular

Observacao sobre DWG:
    ezdxf le apenas DXF. Se o arquivo for DWG, exporte como DXF no
    Civil 3D/AutoCAD (SAVEAS -> DXF) ou converta com o ODA File Converter.
"""

from __future__ import annotations

import argparse
import fnmatch
import sys
from pathlib import Path

try:
    import ezdxf
except ImportError:  # pragma: no cover
    sys.exit("ezdxf nao encontrado. Instale com:  pip install ezdxf")


TIPOS_TEXTO = {"TEXT", "MTEXT", "ATTRIB"}

# Blocos anonimos gerados pelo EXPORTTOAUTOCAD chamam-se *U1, *U2, ...
# Em fnmatch o '*' e curinga, entao '[*]' representa o asterisco literal.
PADRAO_ANONIMO = "[*]U*"


# --------------------------------------------------------------------------
# angulos
# --------------------------------------------------------------------------

def angulo_legivel(ang: float) -> float:
    """Devolve o angulo equivalente 'legivel em planta' (entre -90 e +90)."""
    a = ang % 360.0
    if 90.0 < a <= 270.0:
        a = (a + 180.0) % 360.0
    return a


def novo_angulo(atual: float, modo: str, alvo: float) -> float:
    if modo == "legivel":
        return angulo_legivel(atual)
    return alvo


# --------------------------------------------------------------------------
# explosao dos blocos de rotulo
# --------------------------------------------------------------------------

def explodir_blocos(layout, padrao: str, passadas: int = 3) -> int:
    """
    Explode as insercoes cujo nome casa com `padrao`.
    Repete algumas vezes para pegar blocos aninhados.
    Blocos COM atributo nao sao explodidos (o texto seria perdido);
    esses ja sao tratados diretamente por corrigir_textos().
    """
    total = 0
    for _ in range(passadas):
        alvos = [
            ins for ins in layout.query("INSERT")
            if fnmatch.fnmatch(ins.dxf.name, padrao) and not ins.attribs
        ]
        if not alvos:
            break
        for ins in alvos:
            try:
                ins.explode()
                total += 1
            except Exception as exc:
                print(f"      ! nao explodiu {ins.dxf.name}: {exc}")
    return total


# --------------------------------------------------------------------------
# correcao da rotacao
# --------------------------------------------------------------------------

def corrigir_textos(container, modo: str, alvo: float, simular: bool) -> int:
    """Percorre um layout (ou definicao de bloco) e ajusta a rotacao."""
    n = 0
    for e in container:
        tipo = e.dxftype()

        # atributos vivem pendurados no INSERT, nao na query direta
        if tipo == "INSERT":
            for att in e.attribs:
                n += _ajustar(att, modo, alvo, simular)
            continue

        if tipo not in TIPOS_TEXTO:
            continue

        n += _ajustar(e, modo, alvo, simular)
    return n


def _ajustar(e, modo: str, alvo: float, simular: bool) -> int:
    try:
        atual = float(e.dxf.rotation)
    except Exception:
        atual = 0.0

    novo = novo_angulo(atual, modo, alvo)

    if abs((atual % 360.0) - (novo % 360.0)) < 1e-9:
        return 0

    if not simular:
        # No MTEXT o vetor de direcao (grupo 11) tem prioridade sobre a
        # rotacao (grupo 50); precisa ser descartado para o angulo valer.
        if e.dxftype() == "MTEXT" and e.dxf.hasattr("text_direction"):
            e.dxf.discard("text_direction")
        e.dxf.rotation = novo
    return 1


# --------------------------------------------------------------------------
# processamento de arquivo
# --------------------------------------------------------------------------

def processar(caminho: Path, args) -> None:
    print(f"\n>> {caminho.name}")
    try:
        doc = ezdxf.readfile(str(caminho))
    except Exception as exc:
        print(f"   ! erro ao abrir: {exc}")
        return

    explodidos = 0
    corrigidos = 0

    layouts = [doc.modelspace()] + [lay for lay in doc.layouts if lay.name != "Model"]

    for lay in layouts:
        if args.explodir and not args.simular:
            explodidos += explodir_blocos(lay, args.padrao, args.passadas)
        corrigidos += corrigir_textos(lay, args.modo, args.angulo, args.simular)

    # opcional: mexer tambem dentro das definicoes de bloco que nao foram
    # explodidas (cuidado: afeta todas as insercoes daquele bloco)
    if args.blocos and not args.simular:
        for bloco in doc.blocks:
            if bloco.name.startswith(("*Model_Space", "*Paper_Space")):
                continue
            corrigidos += corrigir_textos(bloco, args.modo, args.angulo, False)

    print(f"   blocos explodidos: {explodidos}")
    print(f"   textos ajustados : {corrigidos}")

    if args.simular:
        print("   (simulacao - nada foi gravado)")
        return

    destino = caminho.with_name(caminho.stem + args.sufixo + ".dxf")
    try:
        doc.saveas(str(destino))
        print(f"   salvo em: {destino.name}")
    except Exception as exc:
        print(f"   ! erro ao salvar: {exc}")


# --------------------------------------------------------------------------
# CLI
# --------------------------------------------------------------------------

def main() -> None:
    p = argparse.ArgumentParser(
        description="Corrige a rotacao de textos em DXF exportados do Civil 3D."
    )
    p.add_argument("entrada", help="arquivo .dxf ou pasta com varios .dxf")
    p.add_argument("--modo", choices=["horizontal", "legivel", "angulo"],
                   default="horizontal",
                   help="horizontal = tudo em 0; legivel = so vira o que esta "
                        "de cabeca para baixo; angulo = forca --angulo")
    p.add_argument("--angulo", type=float, default=0.0,
                   help="angulo em graus quando --modo angulo (padrao 0)")
    p.add_argument("--explodir", action="store_true",
                   help="explode os blocos de rotulo antes de girar o texto")
    p.add_argument("--padrao", default=PADRAO_ANONIMO,
                   help=f"padrao dos nomes de bloco a explodir (padrao: {PADRAO_ANONIMO})")
    p.add_argument("--passadas", type=int, default=3,
                   help="quantas passadas de explosao para blocos aninhados")
    p.add_argument("--blocos", action="store_true",
                   help="tambem ajusta texto dentro das definicoes de bloco "
                        "(afeta todas as insercoes daquele bloco)")
    p.add_argument("--sufixo", default="_corrigido",
                   help="sufixo do arquivo de saida (padrao: _corrigido)")
    p.add_argument("--simular", action="store_true",
                   help="apenas relata, sem alterar nem gravar")
    args = p.parse_args()

    entrada = Path(args.entrada)
    if entrada.is_dir():
        arquivos = sorted(
            f for f in entrada.glob("*.dxf")
            if not f.stem.endswith(args.sufixo)
        )
    elif entrada.is_file():
        arquivos = [entrada]
    else:
        sys.exit(f"Caminho nao encontrado: {entrada}")

    if not arquivos:
        sys.exit("Nenhum .dxf encontrado.")

    print(f"Modo: {args.modo}   |   arquivos: {len(arquivos)}")
    for f in arquivos:
        processar(f, args)
    print("\nConcluido.")


if __name__ == "__main__":
    main()
