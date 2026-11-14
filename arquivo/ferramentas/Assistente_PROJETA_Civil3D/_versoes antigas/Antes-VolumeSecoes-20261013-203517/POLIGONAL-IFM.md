# POLIGONAL — IFM

## Atualização Prévia 14

Inclui **Conferir apoios e preparar nomes…**: valida a disponibilidade dos apoios extremos e sugere associações dos nomes compostos pela sequência. As associações só são aplicadas depois da conferência explícita do usuário. O arquivo VLI resulta nas mesmas quatro propostas descritas abaixo.

O painel e o relatório deixam explícito que o cálculo entre M001 e M002, orientado pela ré inicial M002 e pela visada final M002 → M001, não exige uma nova ocupação de M001. A campanha independente de volta continua opcional.

O botão **Referência linear NBR 13133:2021 — 1:12.000** preenche apenas o denominador de precisão, após confirmação da aplicabilidade da edição/critério. Não preenche tolerância angular, não calcula fator UTM e não declara aprovação integral. Fonte conferida: item 5.6.6, página impressa 23, https://arquivos.ufrrj.br/arquivos/20230102302b983573184b44fe6044133/ABNT_NBR_13133_2021-1-1.pdf . A referência contratual anterior do DNIT à edição de 1994 continua distinta da referência linear opcional de 2021.

Fator 1 é mostrado como **sem redução horizontal adicional**, sem sugerir que já foi validado para UTM. A sequência de ocupações não permite determinar esse fator. Compilação e testes numéricos atualizados; interface ainda requer teste em sessão Civil 3D.

Prévia 13 (0.13.0), Civil 3D 2026 e 2027. Abra a aba Poligonal ou use PROJETAPOLIGONAL.

## Fluxo

1. Selecione a caderneta de ida. O importador reconhece a codificação M21 Topcon do POLIGONAL03CC.M21. A extensão .bat também pode ser selecionada, mas seu conteúdo é lido como texto e precisa ter essa codificação. Arquivos de comandos e outras codificações são recusados; nunca são executados.
2. Importe APOIO.txt ou cole o texto dos pontos conhecidos. Formatos: `Ponto;N;E;Cota` ou `Nome Nome N E Cota`. Números em metros, com decimal ponto ou decimal vírgula brasileiro.
3. Confira o percurso sugerido: `ré inicial > partida > intermediárias > chegada > vante final`. A ordem é editável. São necessárias coordenadas da partida, da ré inicial, da chegada e da vante final. Pontos conhecidos intermediários não são usados automaticamente como injunções.
4. Confira as associações de nomes compostos. Use `original=confirmado`, uma por linha. Nenhuma correção é implícita. Para o arquivo VLI há quatro hipóteses já usadas na memória anterior, que precisam de confirmação na caderneta: `T4\`T3=T3`, `T13\`T12=T12`, `T14\`T12=T12`, `T22\`T21=T21`.
5. Declare sistema/datum/fuso e fator horizontal aplicável. Não há transformação de coordenadas ou cálculo automático de fator combinado.
6. Se houver campanha independente de volta, importe seu arquivo e confira seu percurso próprio. Observações recíprocas no mesmo M21 não são apresentadas como duas campanhas independentes. A comparação utiliza os pontos comuns após ajuste de cada campanha, sem fundi-las automaticamente.
7. Clique Calcular / recalcular e ajustar. Mudanças invalidam o resultado anterior. Salve o relatório e a memória em uma nova pasta de revisão; os originais são preservados. O relatório HTML abre no navegador e pode ser impresso em PDF. CSVs contêm coordenadas e memória; JSON guarda entradas e auditoria das observações. Salvar e reabrir entradas permite recalcular relendo os arquivos de origem.

## Cálculo e limites

Médias das direções com passagem por 360°, distâncias e desníveis recíprocos, correção angular uniforme, Bowditch em N/E e compensação trigonométrica das cotas proporcional aos comprimentos. Não é mínimos quadrados. Repetições são preservadas; não identifica faces/séries nem rejeita observações estatisticamente. Não insere ou substitui pontos no DWG.

Suporta percursos com apoios e orientações conhecidos nas duas extremidades, incluindo o circuito simples VLI fechado por um lado conhecido. Estações repetidas em blocos ou no percurso são recusadas para não misturar campanhas. Registros não reconhecidos interrompem a importação. Observações fora do percurso são contabilizadas como não usadas; ficam no arquivo original e na auditoria. A diferença angular acima de um grau é bloqueio de dados incoerentes, não tolerância normativa. Resultados fora de tolerância informada podem ser calculados para diagnóstico, com a indicação NÃO ATENDE.

## Referência DNIT

DNIT IPR 726, 3ª edição, IS-204 item 2.2.2, página impressa 262 (página 265 do PDF). Remete à NBR 13133/94 e poligonal III P ou superior para o escopo ali definido. Referência oficial: https://www.gov.br/dnit/pt-br/assuntos/planejamento-e-pesquisa/ipr/coletanea-de-manuais/vigentes/ipr_726_3aedicao_2006.pdf

A aba recebe tolerância angular absoluta e denominador mínimo de precisão relativa informados pelo responsável. Zero significa NÃO AVALIADO. Esses campos não são rotulados como limites universais DNIT; classe, edição aplicável e critérios contratuais devem ser definidos. O relatório não certifica conformidade integral com DNIT/NBR. Nivelamento trigonométrico não substitui automaticamente o nivelamento geométrico/contranivelamento exigido pelo escopo. Os demais PDFs fornecidos não foram adotados como substitutos de norma topográfica.

## Validação

334 observações, 28 estações e 27 lados do POLIGONAL03CC.M21. Com as quatro associações acima: erro angular +73,373262 segundos, erro linear após correção angular 0,036594921 m. Coordenadas conferidas com a memória Resultado_conferido a 0,000002 m; apoios fixos, percurso inverso, nomes ambíguos, entradas inválidas e sinalização fora da tolerância testados. Compilação dos dois anos sem avisos/erros. A aba ainda requer teste interativo dentro do Civil 3D; não confundir teste numérico com validação normativa.
