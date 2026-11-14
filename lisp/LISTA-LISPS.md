# Lista de rotinas LISP — D:\claude ai

| Arquivo | Comando(s) | O que faz |
|---|---|---|
| CONTORNO-CORREDOR.lsp | `CC` (alias `CONTORNO`) | Clica um ponto dentro do corredor e gera o contorno |
| CriarPolilinhaAoRedor.lsp | `CPAR` / `CPAR2` / `CPAR3D` | Polilinha ao redor de objeto; 2 = por offset; 3D = corredor Civil 3D |
| FAIXA3.lsp | `FAIXA3` / `FAIXA3AJUDA` | Cria terceira faixa (faixa adicional) a partir do eixo |
| Folha-Base.lsp | `FOLHABASE` (alias `FB`) | Propaga configurações do layout atual para as demais folhas |
| GotaTape.lsp | `GOTA` / `TAPER` | Gota de canalização e taper/faixa de mudança de velocidade (DNIT IPR-718). Carregar GT_DB.lsp antes |
| GT_DB.lsp | — (sem comando) | Banco de dados geométrico de interseções; carregar ANTES do GotaTape |
| GRPDESC.lsp | `GRPDESC` / `GRPDESCSEL` / `LSTDESC` | Grupos de pontos COGO por descrição; versão só do selecionado; lista descrições |
| IMPSECOES.lsp | `IMPSECOES` | Impressão em lote de seções (folhas A4) no Model Space |
| Inters3D.lsp | `INTERS3D` | Interseção viária paramétrica 3D |
| INTERSECAO.lsp | `INTERSECAO` / `INTERSECAOAJUDA` | Gerador de interseção rodoviária 3D a partir de dois eixos |
| LEGFIX.lsp | `LEGFIX` / `LEGINFO` / `LEGEXP` | Corrige rotação de rótulos exportados do Civil 3D; diagnóstico; explode blocos |
| LIMITE-CORREDOR.lsp | `CLC` / `CLCP` | Polilinha fechada de limite do corredor; CLCP a partir de 2 polilinhas |
| NSPDF.lsp | `NSPDF` / `NSLAYOUTS` / `NSLIMPAR` | Notas de serviço A4 em PDF único; cria layouts; limpa layouts |
| PDFSECOES.lsp | `PDFSECOES` | Gera PDF das seções selecionadas |
| QUADROCURVAS.lsp | `QUADROCURVAS` | Quadro de curvas horizontais do alinhamento |
| QuadroVolumes.lsp | `QVOL` | Quadro resumo de volumes (corte/aterro/líquido/área) |
| RETORNO_DNIT.lsp | `RETDNIT` | Geometria de retorno em rodovia (DNIT IPR-718, item 8.9) |
| ROTALONGADA.lsp | `ROTALONGADA` (alias `RTA`) / `RTANORMAS` | Rotatória alongada sobre o eixo; RTANORMAS imprime as tabelas adotadas |
| RotatoriaAlongada.lsp | `ROTALONGADA` / `ROASIMPLES` | Versão anterior da rotatória alongada — ATENÇÃO: `ROTALONGADA` conflita com o arquivo acima |
| ROTATORIO.LSP | `ROTATORIO` / `ROT-VIAS` / `ROT-FORMA` / `ROT-AJUDA` | Rotatória automática com módulo de vias de acesso |
| SEPARAPONTOS.lsp | `SEPPTS` | Importa pontos TXT/CSV e separa por código em layers e grupos |
| StreetView.lsp | `SV` / `SVMAPA` / `SVZONA` | Abre Street View / mapa no ponto picado; SVZONA redefine a zona UTM |
| TabelaCorteAterro.lsp | `TABCA` | Tabela de volumes de corte e aterro a partir dos textos de área |
| talude\talud.lsp | `TALUDE` | Desenho de talude (acompanha DrawTalud.fas) |
| unir_corredores.lsp | `UNIRCORREDORES` (alias `UNCOR`) | Une vários corredores do Civil 3D em um só |
| VolumeSeccoes.lsp | `VOLSEC` | Volume de seções pelo método da semidistância + planilha Excel |

**Obs.:** `acesso-dnit.lsp` tem extensão .lsp mas o conteúdo é Python para o nó Python Script do Dynamo for Civil 3D — não carrega com APPLOAD.

Carregamento: `APPLOAD` (ou arraste o arquivo para o AutoCAD).
