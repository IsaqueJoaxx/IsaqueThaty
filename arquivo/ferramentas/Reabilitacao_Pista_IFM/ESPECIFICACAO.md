# REABILITAÇÃO DE PISTA — IFM

Especificação de trabalho para desenvolver a rotina solicitada. Não é nota de serviço liberada para campo e não altera nenhum desenho.

## Atualização prioritária — 20/10/2026

O usuário esclareceu que BR-222 é o centro da pista a construir, que a pista já foi cortada e está sendo refeita, e que o ponto alto próximo ao alinhamento representa o eixo da pista **sem o corte**. Portanto, esse ponto não deve ser removido como possível meio-fio/outlier apenas pelo salto de cota. O papel do levantamento mudou em relação à hipótese inicial de todos os pontos representarem a superfície anterior ao rebaixo.

**Não descontar automaticamente 0,26 m de todo ponto recebido.** Se o ponto mede o fundo atual já cortado, sua cota é o próprio fundo de controle e o topo deve ficar no mínimo 0,23 m acima. Para um ponto de referência original sem corte, o fundo original−0,26 m é apenas previsto, não medido. Identificar e registrar esses papéis separadamente antes do cálculo executivo. As fórmulas abaixo que descontam 0,26 m do levantamento aplicam-se somente aos pontos comprovadamente da superfície original.

Confirmação recebida em 20/10/2026: os pontos altos são referências originais e os baixos estão já cortados, incluindo ponto 06 (7,598 m) como referência original e ponto 05 (7,336 m) como fundo cortado. A descrição CT e a altura relativa, sozinhas, não justificam uma separação automática universal em arquivos futuros; preservar candidatos e casos ambíguos para revisão.

O usuário confirmou que o alinhamento é o centro, não uma referência lateral, e que falta o levantamento esquerdo. Não refletir o fundo para o lado esquerdo nem afirmar espessura mínima nesse lado sem levantamento. Trabalhar nesta etapa com a conferência da metade direita.

## Dados confirmados pelo usuário

- Entrada: alinhamento e levantamento em pontos no desenho, incluindo eixo, bordo esquerdo e bordo direito.
- Pista nominal: 3,50 m de cada lado do eixo.
- Caimento nas tangentes: 2% para fora em cada lado.
- Velocidade de projeto confirmada: 60 km/h.
- Atualização do formato de entrada: o usuário informou que os levantamentos sempre chegarão como número,E,N,Z,descrição. A amostra contém apenas a descrição CT. Portanto, substituir a identificação por EIXO/BORDO por associação geométrica ao alinhamento, com progressiva e afastamento; descrições não identificam a função dos pontos nesta amostra.
- Curvas: a rotina deverá calcular a superelevação; não depender de tabela ou cálculo existente no alinhamento. Superelevação máxima confirmada pelo usuário: 6%; velocidade de projeto: 60 km/h. Método e transições ainda precisam de definição/verificação. O teto de 6% não é uma taxa a aplicar indistintamente em todas as curvas.
- Acostamento novo à direita: 2,00 m além do bordo direito, seguindo o caimento da pista direita.
- Rebaixo de 0,26 m em relação à pista levantada; fundo fixo.
- Espessura mínima de 0,23 m entre fundo fixo e topo de projeto, podendo haver espessuras maiores.
- Harmonizar longitudinalmente a pista, considerando seções anteriores e posteriores e reconhecendo possíveis patologias dos bordos.
- Saídas desejadas: planilha de nota de serviço e modelo de corredor no Civil 3D.
- Preferência de projeto já registrada: curvas verticais editadas manualmente. Teto de rampa longitudinal previamente definido: 7%; não equivale a aprovação normativa.

## Definição geométrica

Todas as cotas e espessuras nesta formulação são verticais, em metros. Uma exigência de espessura normal à placa mudaria o cálculo e deve ser definida antes da execução.

Para uma seção com cota existente E no eixo, BE no bordo esquerdo e BD no bordo direito:

- Fundo fixo do eixo = E − 0,26.
- Fundo fixo do bordo esquerdo = BE − 0,26.
- Fundo fixo do bordo direito = BD − 0,26.
- Topo do eixo = G, a cota de greide projetada.
- Topo do bordo esquerdo = G + 3,50 × qE.
- Topo do bordo direito = G + 3,50 × qD.
- Topo da extremidade do acostamento = G + 5,50 × qD.

qE e qD são inclinações com sinal definido **do eixo para fora**. Nas tangentes, ambos são −0,02. Em uma curva, podem ter sinais diferentes; converter os sinais da fonte de superelevação do Civil 3D antes de usá-los. Não usar automaticamente −2% em curva sem dados.

Para garantir 0,23 m nos três pontos da pista, o greide deve respeitar:

G ≥ máximo(E − 0,03; BE − 0,03 − 3,50 × qE; BD − 0,03 − 3,50 × qD).

Isso é um limite inferior de cota, não o greide suavizado final. A harmonização deve satisfazer esse limite juntamente com a rampa longitudinal e o critério de suavização que for confirmado. Não suavizar primeiro e depois simplesmente elevar cada seção isoladamente: isso pode reintroduzir ondas.

Exemplo apenas ilustrativo: E=100,00; BE=99,93; BD=99,93; qE=qD=−2%. O menor G permitido é 99,97. Os bordos projetados ficam em 99,90 e a espessura nos três pontos é 0,23 m.

## Leitura e rastreabilidade do levantamento

Associar cada ponto ao alinhamento por progressiva e afastamento, preservando número/handle, descrição, coordenadas e cota original. O usuário confirmou a identificação pelas descrições dos pontos; os três códigos exatos ainda precisam ser informados. Não misturar eixos diferentes nem pontos de uma curva próxima.

As larguras levantadas podem diferir das larguras nominais. Uma cota levantada fora do bordo de projeto não pode ser apresentada como cota existente exatamente a 3,50 m sem interpolação/modelagem declarada. A rotina deve manter o afastamento medido e o ponto de controle utilizado.

Pontos potencialmente anômalos devem ser sinalizados e comparados com vizinhos; não excluí-los silenciosamente da checagem de espessura. Se um ponto alto for removido no preparo, isso passa a ser uma revisão explícita do fundo fixo daquela região.

A garantia nos pontos levantados não prova a espessura entre eles. Para verificar uma faixa contínua, é necessário definir o modelo de interpolação do levantamento e conferir as regiões entre seções, inclusive nas transições de superelevação. Ausência de dados deve aparecer como não verificada.

## Acostamento

A geometria do topo segue a pista direita até o afastamento +5,50 m. O usuário confirmou que não existe levantamento da faixa nova entre +3,50 e +5,50 m. A rotina fornecerá a cota de locação do topo por continuidade do caimento da pista direita, mas deixará espessura, corte/aterro e fundo de preparo como não verificados. Não extrapolar o terreno para apresentar essas grandezas como medidas ou garantidas. Para dimensionar/quantificar a faixa nova, obter levantamento lateral e definir seu preparo em etapa posterior.

## Harmonização longitudinal

O módulo existente CorrectionCore confere diferenças entre correções manuais: até 2,5 mm entre seções separadas exatamente 5 m. Não é uma regra normativa ou garantia de IRI. Ao ser perguntado sobre a regra, o usuário respondeu sim, sem escolher outra interpretação. Foi comunicado que será mantida a interpretação já registrada: diferença entre correções consecutivas, não variação de rampa nem limite absoluto acumulado. O novo otimizador precisa respeitar esse controle e sinalizar intervalos de comprimento diferente, sem inventar proporcionalidade.

O novo cálculo deve separar referência longitudinal, tendência robusta para diagnóstico de patologias, limite inferior por espessura e solução final. Extremos, encontros e tolerâncias precisam ser definidos para não elevar acessos ou vínculos sem informar o usuário. Se as restrições forem incompatíveis, apresentar o conflito em vez de liberar resultado que viole espessura/rampa.

## Nota de serviço pretendida

Por progressiva e ponto de controle: alinhamento, estaca/progressiva, código, afastamento, coordenadas de projeto, cota levantada/interpolada, fundo fixo, cota de topo, espessura calculada, folga sobre 0,23 m, diferença topo−existente, escavação prevista e situação de verificação. Acostamento sem fundo conhecido: espessura e corte/aterro em branco, com justificativa.

Manter separados rebaixo de 0,26 m, diferença topo−existente e espessura da placa. Esses valores representam operações diferentes; uma diferença positiva no topo não identifica por si só o material de aterro. Guardar parâmetros, fontes e alertas em aba própria.

## Modelo Civil 3D pretendido

Prévia revisável antes de criar objetos; preservação dos dados originais; identificação REABILITAÇÃO DE PISTA — IFM. Confirmar estilos, seção tipo e códigos adequados às larguras, superelevação e espessura variável. Uma seção de concreto constante de 0,23 m não reproduz o fundo fixo irregular com espessura variável.

O topo projetado e o fundo fixo precisam ser representados separadamente. Quando forem criados perfil/corredor/superfícies, reler cotas e amostrar a geometria resultante contra os controles da nota. Informar explicitamente os intervalos verificados e os ainda não verificados. Compilar a rotina não equivale a validar sua execução no Civil 3D.

## Pendências enviadas ao usuário

1. Alinhamento de exemplo fornecido: arquivo `D:\Edmar Trypolonio\EIXO_BR-222.xml`, LandXML 1.2, eixo BR-222. Descrições informadas EIXO e BORDO; ainda obter levantamento com coordenadas e cotas.
2. Referência/método definidos para a obra; velocidade confirmada de 60 km/h e teto de superelevação confirmado em 6%.
3. Não há levantamento no acostamento novo: topo calculável, espessura e corte/aterro não verificáveis nesta etapa. Preparo ainda não definido.

Após essas respostas, definir dados do acostamento, vínculos de início/fim e parâmetros de harmonização necessários ao cálculo. Nenhum eixo/desenho específico foi escolhido: há sessões Civil 3D 2026 e 2027 abertas, e não se presume que o trevo consultado anteriormente seja o levantamento deste serviço.

## Diagnóstico da amostra recebida

Arquivo original de pontos: `C:\Users\User\.codex\attachments\b127faa1-9b5c-4be1-b2bb-c4913a3e25a0\Texto colado.txt`. Somente leitura; 707 números únicos, todos CT. CSV na ordem número,Este,Norte,cota,descrição; coordenadas LandXML na ordem Norte,Este foram convertidas antes da projeção.

Diagnóstico produzido pelo script `diagnosticar_levantamento.py`: progressivas acumuladas de aproximadamente 277320,012 a 278200,235 m; afastamentos de −0,142 a +5,612 m, positivos à direita do sentido crescente do eixo. Nenhum ponto tem afastamento menor que −0,50 m. Portanto, não existe evidência de pista levantada a ±3,50 m em relação ao alinhamento recebido.

O agrupamento exploratório por lacunas longitudinais maiores que 2 m resultou em 89 conjuntos, com separação mediana próxima de 10,004 m. Isso não define automaticamente as estacas executivas nem autoriza interpolar para 5 m. Três conjuntos têm apenas dois pontos; tratar lacunas explicitamente.

Há diferenças abruptas próximas ao eixo: pontos 05 e 06, cota 7,336 e 7,598 m, respectivamente, separados aproximadamente 0,125 m. Não classificar o ponto alto automaticamente como pavimento ou como erro. Perguntado se representa meio-fio/canaleta e qual é o papel do alinhamento de referência neste trecho.

Resultados preservados em `Diagnostico_BR222_01/pontos_projetados.csv` e `diagnostico.json`. Diagnóstico não importa pontos no Civil 3D nem altera os originais. A identificação de eixo físico e bordos ainda depende da interpretação dessa seção. Não deslocar o alinhamento recebido automaticamente para forçar as larguras nominais.

## Conferência preparada — 20/10/2026

`preparar_secoes.py` processa o CSV de projeção e o LandXML, sem alterar o levantamento ou DWG. A classificação exploratória usa pontos até 0,50 m do eixo, salto único entre 0,15 e 0,45 m e lacuna longitudinal de 2 m para agrupar. Esses limiares são parâmetros expostos para reconhecer candidatos desta amostra; não constituem regra normativa nem substituem revisão. Pontos além de +3,50 m são preservados e têm uso construtivo marcado para revisão.

Resultado: 707 pontos, 89 seções observadas, referências originais reconhecidas em 84, cinco seções sem par de referência/fundo próximo do eixo. Progressivas médias aproximadas dessas cinco: 277350,029; 277360,028; 277369,997; 277580,044; 277999,998 m. Não interpolados dados faltantes nem gerado fundo fictício.

Planilha `outputs/br222-20261020/CONFERENCIA_LEVANTAMENTO_BR222_IFM.xlsx`: abas Seções e Pontos; todos os dados recebidos preservados e relacionados. Limite parcial de cota calculado somente nas tangentes e nos pontos candidatos a fundo da faixa direita 0 a 3,50 m: máximo(fundo + 0,23 + 0,02 × afastamento). Não é greide final nem prova espessura entre pontos. Seções em curva ficam sem esse limite, aguardando superelevação/transições. Valores dos pontos externos não são reinterpretados como terreno do novo acostamento.

No trecho levantado, o XML tem 23 elementos, com menor raio de aproximadamente 600 m e aproximações da transição por pequenos arcos. O raio global de 17,81 m citado na leitura inicial do eixo completo fica fora do trecho levantado. Não aplicar taxas independentes por pequeno arco de transição sem compatibilizar a lei de rotação.

Verificações realizadas: projeção analítica em retas/arcos e sinais; classificação com par confirmado, dados ausentes e sem salto; 707 registros reconciliados; fórmulas da planilha recalculadas com alteração temporária da espessura e parâmetro restaurado; arquivo exportado reaberto para conferir IDs, contagens e fórmulas; prévias visuais de ambas as abas. Nenhum greide, corredor ou superfície desta reabilitação foi criado. Ainda faltam harmonização longitudinal, regra de superelevação/transições e integração CAD; a planilha entregue é conferência preliminar, não nota executiva.
