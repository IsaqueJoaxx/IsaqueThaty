"""Assistente do GERAR_NOTA_IFM.bat: arraste os levantamentos (e o eixo .xml) sobre o .bat.

Classificação pelo nome do arquivo:
  .xml                         -> eixo (LandXML)
  nome com ACOST ou FUNDO      -> levantamento depois do rebaixo (--fundo)
  nome com LEV ou PISTA        -> levantamento da pista (--pista)
  outros .csv/.txt             -> pergunta
"""
from __future__ import annotations

import os
import re
import sys
import traceback
from datetime import datetime
from pathlib import Path

AQUI = Path(__file__).resolve().parent
sys.path.insert(0, str(AQUI))

try:
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass


def perguntar(texto, validas=None, padrao=None):
    while True:
        r = input(texto).strip()
        if not r and padrao is not None:
            return padrao
        if validas is None or r.upper() in validas:
            return r.upper() if validas else r
        print('   Resposta não reconhecida, tente de novo.')


def escolher_arquivos():
    try:
        import tkinter as tk
        from tkinter import filedialog
        root = tk.Tk(); root.withdraw(); root.attributes('-topmost', True)
        nomes = filedialog.askopenfilenames(
            title='Escolha o levantamento da pista, os de acostamento e (opcional) o eixo XML',
            filetypes=[('Levantamentos e eixo', '*.csv *.txt *.xml'), ('Todos', '*.*')])
        root.destroy()
        return list(nomes)
    except Exception:
        return []


def main():
    print('=' * 70)
    print(' REABILITAÇÃO DE PISTA — IFM  •  gerar nota de serviço do greide')
    print('=' * 70)
    arquivos = [a for a in sys.argv[1:] if a.strip()]
    if not arquivos:
        print('Nenhum arquivo arrastado: abrindo a janela para escolher...')
        arquivos = escolher_arquivos()
    if not arquivos:
        print('Nenhum arquivo escolhido. Arraste os levantamentos sobre o GERAR_NOTA_IFM.bat.')
        return 1

    pista, fundos, eixo = [], [], None
    for a in arquivos:
        p = Path(a)
        if not p.is_file():
            print(f'   Ignorado (não é arquivo): {a}')
            continue
        nome = p.name.upper()
        if p.suffix.lower() == '.xml':
            if eixo:
                print(f'   Mais de um XML; usando {Path(eixo).name} e ignorando {p.name}')
                continue
            eixo = str(p)
        elif 'ACOST' in nome or 'FUNDO' in nome:
            fundos.append(str(p))
        elif 'LEV' in nome or 'PISTA' in nome:
            pista.append(str(p))
        else:
            r = perguntar(f'   {p.name}: é (P)ista ou (A)costamento/fundo depois do rebaixo? ', {'P', 'A'})
            (pista if r == 'P' else fundos).append(str(p))

    if len(pista) != 1:
        print(f'\nPreciso de exatamente 1 levantamento da pista (recebi {len(pista)}).')
        for i, f in enumerate(pista, 1):
            print(f'   {i}. {Path(f).name}')
        if not pista:
            return 1
        k = int(perguntar('   Qual é a pista? Número: ', {str(i) for i in range(1, len(pista) + 1)}))
        escolhida = pista[k - 1]
        fundos += [f for f in pista if f != escolhida]
        pista = [escolhida]

    print('\nArquivos:')
    print(f'   Pista ........: {Path(pista[0]).name}')
    for f in fundos:
        print(f'   Acostamento ..: {Path(f).name}')
    print(f'   Eixo .........: {Path(eixo).name if eixo else "NÃO INFORMADO -> eixo provisório pelo levantamento"}')

    args = ['--pista', pista[0]]
    for f in fundos:
        args += ['--fundo', f]

    if eixo:
        args += ['--eixo', eixo]
        from reabilitacao_core import ler_eixo_landxml
        try:
            ler_eixo_landxml(eixo)
        except ValueError as e:
            msg = str(e)
            if 'Nomes:' in msg:
                nomes = [n.strip() for n in msg.split('Nomes:')[1].split(',')]
                print('\nO XML tem mais de um alinhamento:')
                for i, n in enumerate(nomes, 1):
                    print(f'   {i}. {n}')
                k = int(perguntar('   Qual usar? Número: ', {str(i) for i in range(1, len(nomes) + 1)}))
                args += ['--eixo-nome', nomes[k - 1]]
            else:
                print(f'\nProblema no XML do eixo: {msg}')
                return 1
    else:
        m = re.search(r'(\d{5,7})', Path(pista[0]).stem)
        sugestao = m.group(1) if m else ''
        print('\nSem eixo XML: vou gerar um EIXO PROVISÓRIO (só para prévia).')
        while True:
            txt = perguntar(f'   Estaca inicial do levantamento em metros [{sugestao or "obrigatório"}]: ',
                            padrao=sugestao or None)
            try:
                float(txt.replace(',', '.'))
                break
            except (TypeError, ValueError):
                print('   Informe um número, ex.: 268630')
        args += ['--estaca-inicial', txt.replace(',', '.')]

    print('\nA pista foi levantada ANTES ou DEPOIS do rebaixo de 26 cm?')
    print('   A = antes  (fundo = cota levantada - 0,26)')
    print('   D = depois (pista já cortada: fundo = cota levantada)')
    estado = perguntar('   Resposta (A/D): ', {'A', 'D'})
    args += ['--pista-estado', 'antes' if estado == 'A' else 'depois']

    saida = AQUI / 'outputs' / f'{Path(pista[0]).stem}_{datetime.now():%Y%m%d-%H%M%S}'
    args += ['--saida', str(saida)]

    print('\nCalculando (alguns segundos)...\n')
    import projetar_greide
    resumo = projetar_greide.main(args)
    print('\nAlertas:')
    for a in resumo.get('alertas', []):
        print('   • ' + a)
    print(f'\nPronto. Arquivos em:\n   {saida}')
    try:
        os.startfile(saida / 'NOTA_SERVICO_GREIDE_IFM.xlsx')
        os.startfile(saida)
    except Exception:
        pass
    return 0


if __name__ == '__main__':
    try:
        code = main()
    except SystemExit as e:
        print(f'\nNão foi possível gerar: {e}')
        code = 1
    except Exception:
        print('\nErro inesperado:')
        traceback.print_exc()
        code = 1
    sys.exit(code)
