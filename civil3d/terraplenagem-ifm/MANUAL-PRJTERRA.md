# Manual de uso do PRJTERRA

**DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM · Versão 0.1.0 · Civil 3D 2026 e 2027**

Este manual explica como preparar a cubação, cadastrar cortes e aterros, definir restrições de transporte, calcular a distribuição e conferir os resultados. As instruções correspondem à versão instalada. Os valores dos exemplos são didáticos e precisam ser substituídos pelos dados geométricos, geotécnicos e econômicos da obra.

## 1 Abra o programa e experimente

1. Abra um desenho no Civil 3D e digite **PRJTERRA**.
2. Na janela **DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM**, clique em **Exemplo** e confirme a substituição do cenário atual.
3. Clique em **Calcular distribuição**.
4. Consulte as abas **Distribuição** e **Pendências / reservas**.
5. Gere um **Relatório HTML** para conhecer os quadros de saída.
6. Use **Salvar projeto** para guardar o cenário em JSON.

O comando **PRJTERRADEMO** também abre o exemplo fictício. Ele substitui o cenário em memória daquele documento, mas não desenha automaticamente. Salve um cenário real antes de usar a demonstração.

### Sequência para um projeto real

**Importar cubação → Gerar origens/aterros → Revisar materiais e fatores → Cadastrar jazidas e ADME → Definir rotas e custos → Calcular → Resolver pendências → Salvar → Exportar e desenhar.**

### O que o programa procura

Primeiro, o cálculo procura reduzir cortes sem destino e aterros não atendidos, em volume equivalente de banco. Depois, procura reduzir o momento ou o custo de transporte, conforme o objetivo escolhido. Jazidas e ADME são capacidades disponíveis: o programa não obriga o uso integral delas.

### O que fica salvo

O cenário é mantido em memória enquanto o documento está na sessão, mas **não é gravado automaticamente no DWG**. Salve o arquivo JSON pelo botão **Salvar projeto**. Salvar apenas o desenho não substitui essa operação.

<!-- page -->

## 2 Conheça os botões e as abas

| Botão | Função |
|---|---|
| Novo | Iniciar um cenário vazio após confirmação |
| Abrir projeto | Ler um cenário JSON salvo |
| Salvar projeto | Gravar as entradas do cenário em JSON |
| Exemplo | Substituir as entradas por dados fictícios |
| Importar áreas CSV | Ler progressivas e áreas de corte e aterro |
| Gerar origens/aterros | Agrupar a cubação e criar os itens de distribuição |
| Calcular distribuição | Calcular com as entradas atuais |
| Exportar CSV | Exportar movimentos e pendências |
| Relatório HTML | Gerar relatório com quadros e rastreabilidade |
| Desenhar diagrama | Fechar a janela e solicitar a inserção do diagrama |

### Abas de entrada

**Origens / cortes / jazidas:** volumes disponíveis e propriedades dos materiais. **Destinos / aterros / ADME:** demandas, capacidades e requisitos. **Faixas de DMT e custos:** limites das faixas e custos unitários. **Rotas e bloqueios:** regras de pares específicos. **Barreiras:** travessias longitudinais proibidas. **Cubação:** seções e volumes importados. **Compensação lateral:** valores retirados do transporte longitudinal quando a opção foi utilizada.

### Campos superiores

**Cenário** identifica a alternativa em estudo. **DMT máximo (m)** limita as ligações aceitas. **Franquia (m)** é a distância descontada apenas no momento extraordinário. A lista de objetivos permite minimizar momento total, momento extraordinário ou custo por faixa.

Use a linha nova da grade para cadastrar itens e confira a célula antes de sair. Nomes devem ser únicos dentro das origens e dentro dos destinos. Depois de editar entradas, calcule novamente: o resultado anterior é invalidado para exportação e desenho.

<!-- page -->

## 3 Importe a cubação do Civil 3D

Use **PRJTERRALINK** quando a cubação já estiver calculada em uma lista de materiais do Civil 3D.

1. Salve o cenário atual se precisar preservá-lo. O comando abre um cenário novo com a cubação selecionada.
2. Digite PRJTERRALINK e selecione o alinhamento.
3. Escolha explicitamente o grupo de linhas de amostragem.
4. Escolha explicitamente a lista de materiais.
5. Confira a aba **Cubação** e a identificação da fonte na janela.
6. Clique em **Gerar origens/aterros**, informe o fator banco/geométrico e o comprimento máximo de agrupamento.
7. Revise os itens gerados antes de calcular.

### Requisitos da lista

A lista precisa ter materiais calculados, ao menos duas seções e fatores de corte e aterro iguais a **1**. Fatores diferentes são rejeitados para evitar aplicar homogeneização duas vezes. O programa também rejeita volume incremental não nulo na primeira seção.

Não altere os fatores de uma lista usada em outra entrega sem avaliar as consequências. Se necessário, prepare uma lista geométrica própria ou importe um CSV de áreas geométricas confirmadas.

### Como o volume é aproveitado

O programa utiliza os volumes incrementais que o Civil 3D fornece; não os substitui por um novo cálculo de áreas médias. Para a posição do centro de cada intervalo, utiliza o ponto médio como aproximação.

A leitura considera os totais de corte e aterro da lista escolhida. Ela **não classifica automaticamente as categorias nem a qualidade dos materiais**. Se houver materiais diferentes, prepare origens separadas e atribua seus volumes e propriedades corretamente.

### Depois de alterar o corredor

Recalcule os materiais no Civil 3D e importe novamente. A leitura é uma fotografia dos dados naquele momento, não um vínculo dinâmico. Salve o cenário anterior antes de substituir a cubação.

<!-- page -->

## 4 Importe áreas por CSV

O CSV é uma alternativa para dados provenientes de planilhas ou relatórios. Ele deve conter exatamente estas três colunas, nesta ordem:

**progressiva_m;corte_m2;aterro_m2**

| progressiva_m | corte_m2 | aterro_m2 |
|---|---|---|
| 0 | 10 | 0 |
| 20 | 15 | 0 |
| 40 | 5 | 2 |
| 60 | 0 | 10 |
| 80 | 0 | 15 |
| 100 | 0 | 5 |

### Prepare o arquivo

Use UTF-8 e separador ponto e vírgula ou tabulação. Aceita vírgula ou ponto decimal, mas não misture ambos no mesmo número. Não utilize separadores de milhar. Informe progressivas acumuladas em metros, como **120**, e não a notação **6+0**. Áreas são em m² e devem ser não negativas; as progressivas devem estar em ordem estritamente crescente.

1. Clique em **Importar áreas CSV** e escolha o arquivo.
2. Confira as linhas da aba **Cubação**.
3. Clique em **Gerar origens/aterros** para transformar as áreas em volumes e grupos.
4. Revise todos os itens gerados, principalmente suas propriedades geotécnicas.

### Cálculo por áreas médias

O volume entre duas seções é a distância entre elas multiplicada pela média das áreas das extremidades. Exemplo: áreas de corte de 10 m² e 15 m², separadas por 20 m, produzem **250 m³** de corte.

Para áreas variando linearmente, o centroide longitudinal considera essa variação. O centroide não é sempre o ponto médio. O CSV de entrada de áreas não é o mesmo formato do CSV exportado com resultados.

<!-- page -->

## 5 Gere grupos e configure a compensação lateral

**Gerar origens/aterros** utiliza a cubação atual para criar itens como C1, C2, A1 e A2. Essa operação **substitui as origens e os destinos anteriores**. Salve o cenário antes de regenerar se já cadastrou materiais, jazidas, ADME ou ajustes manuais.

### Fator banco por geométrico

Informe quanto volume em banco é necessário para cada unidade de volume geométrico de aterro. Exemplo didático: fator **1,13** e aterro de **1.000 m³** correspondem a uma demanda de **1.130 m³ de banco**.

O valor inicial sugerido não é uma recomendação para a obra. Defina o fator com os dados do projeto. O campo **Expansao_pct** do material é um requisito de compatibilidade geotécnica e não substitui esse fator volumétrico.

### Comprimento máximo de agrupamento

O comprimento limita a extensão longitudinal de cada grupo. Grupos menores detalham melhor a distribuição; grupos maiores reduzem a quantidade de itens. O programa não inventa seções intermediárias. Se o espaçamento entre duas seções superar o limite escolhido, aumente esse limite ou forneça mais seções.

O agrupamento não separa automaticamente camadas e categorias. Revise e separe os itens conforme a orientação de terraplenagem. O limite desta versão é **600 itens no total entre origens e destinos**.

### Compensação lateral opcional

Marque a opção somente se puder assumir que **todo corte do intervalo é compatível com seu aterro local**. O programa compensa o menor valor entre o corte em banco e o aterro convertido em banco, antes da distribuição longitudinal.

Exemplo: corte de **120 m³**, aterro de **100 m³** e fator **1,20** podem ser inteiramente compensados no intervalo. Os valores ficam registrados na aba **Compensação lateral** e não aparecem como transporte longitudinal.

Distância longitudinal zero não significa movimentação transversal sem custo. O programa não calcula o transporte transversal dessa compensação. Se a hipótese de compatibilidade não for válida, deixe a opção desmarcada.

<!-- page -->

## 6 Cadastre cortes e jazidas

Na aba **Origens / cortes / jazidas**, cada linha representa uma oferta de material. Os valores iniciais de novas linhas são exemplos; revise todos antes de calcular.

| Campo | Uso e unidade |
|---|---|
| Nome | Identificador único da origem, como C1 ou J1 |
| Progressiva_m | Centro do corte ou progressiva de acesso à jazida, em m |
| Volume_banco_m3 | Volume disponível, em m³ de banco |
| Categoria | Categoria informada: 1, 2 ou 3 |
| CBR_pct | CBR informado do material, em % |
| Expansao_pct | Expansão geotécnica informada, em % |
| Permeabilidade_m_s | Permeabilidade do material, em m/s |
| Acesso_m | Distância adicional de acesso, em m |
| Jazida | Marcar quando a oferta for uma jazida ou empréstimo disponível |
| SoDescarte | Marcar quando o material só puder ser destinado a ADME |

### Corte e jazida têm tratamentos diferentes

O volume de corte sem destino é apontado como pendência. Uma jazida não utilizada é apresentada como reserva; não é obrigatório escavá-la integralmente. Jazidas não podem alimentar ADME neste modelo.

### Cadastre uma jazida externa

Crie uma linha com nome próprio, marque **Jazida**, informe capacidade em banco e propriedades. Em Progressiva_m, informe onde o acesso se liga ao eixo; em Acesso_m, informe a distância adicional até a jazida. Não utilize a coordenada X da jazida como se fosse uma progressiva.

### Não misture materiais incompatíveis

Não atribua um CBR médio a um grupo que precisa ser tratado como materiais distintos sem critério do projeto. Separe volumes e propriedades em origens diferentes. A classificação é uma entrada do usuário, não um resultado de ensaio produzido pelo programa.

<!-- page -->

## 7 Cadastre aterros e ADME

Na aba **Destinos / aterros / ADME**, cada linha representa demanda de aterro ou capacidade de disposição de material.

| Campo | Uso e unidade |
|---|---|
| Nome | Identificador único, como A1 ou ADME1 |
| Progressiva_m | Centro do aterro ou progressiva de acesso, em m |
| Volume_geometrico_m3 | Volume geométrico do aterro; para ADME, capacidade em banco |
| Banco_por_geometrico | Fator de conversão do aterro para volume de banco |
| CBR_min_pct | CBR mínimo aceito pelo aterro, em % |
| Expansao_max_pct | Expansão máxima aceita, em % |
| Permeabilidade_max_m_s | Permeabilidade máxima aceita, em m/s |
| Categoria_exigida | 0 para qualquer categoria; 1, 2 ou 3 para categoria específica |
| Acesso_m | Distância adicional ao destino, em m |
| ADME | Marcar para área de disposição de material excedente |
| Energia | Identificação da energia de compactação para o relatório |

### Quando uma origem pode atender um aterro

O material precisa atender simultaneamente ao CBR mínimo, à expansão máxima, à permeabilidade máxima e à categoria exigida. Também precisa existir uma rota permitida dentro do limite de DMT. Material marcado como SoDescarte não atende aterro.

### Regra específica da ADME

Marque **ADME**, informe sua capacidade em **m³ de banco** no campo Volume_geometrico_m3 e use **Banco_por_geometrico = 1**. Essa exceção de unidade é importante: a capacidade não representa automaticamente volume solto ou a geometria final do depósito.

ADME aceita materiais independentemente dos requisitos geotécnicos de aterro, mas continua sujeita às rotas e ao limite de DMT. Sua capacidade ociosa é uma reserva, não uma demanda obrigatória.

O campo Energia apenas agrupa os aterros no relatório. Ele não altera o fator de conversão nem certifica atendimento à compactação exigida.

<!-- page -->

## 8 Defina DMT rotas e barreiras

### Distância automática

Sem uma rota específica, a DMT é a diferença absoluta entre as progressivas da origem e do destino, somada aos acessos dos dois itens.

Exemplo: origem na progressiva **200 m**, destino na **800 m**, acesso de origem de **100 m** e acesso de destino de **50 m** resultam em **750 m de DMT**.

Essa distância segue a representação por progressivas. Ela não é obtida por navegação em estradas, análise de vias ou rastreamento do trajeto dos caminhões.

### Aba Rotas e bloqueios

| Campo | Como preencher |
|---|---|
| Origem | Nome exatamente como cadastrado na aba de origens |
| Destino | Nome exatamente como cadastrado na aba de destinos |
| Bloqueado | Marcar para proibir esse par |
| DMT_override_m | -1 usa a distância automática; valor não negativo informa a distância total da rota |

Uma distância explícita deve incluir os acessos: o programa não soma novamente os acessos a esse valor. O bloqueio prevalece mesmo quando há uma DMT explícita. Não cadastre o mesmo par mais de uma vez.

### Barreiras longitudinais

Na aba **Barreiras**, cadastre a progressiva da travessia proibida e o motivo. Uma barreira situada entre a origem e o destino impede a ligação automática. Quando existe uma rota alternativa com distância explicitamente informada, ela pode representar um desvio da barreira.

Exemplo: origem em 100 m, destino em 900 m e barreira em 500 m ficam bloqueados pela rota automática. Se houver um desvio real, cadastre a DMT total desse desvio no par correspondente. O programa não procura o desvio por conta própria.

O **DMT máximo** também se aplica às rotas explícitas. Confira se todos os nomes continuam válidos após regenerar ou renomear os grupos.

<!-- page -->

## 9 Escolha o objetivo e os custos

Os três objetivos utilizam as mesmas restrições de material, capacidade e rota. A escolha muda a medida de transporte a minimizar depois de reduzir as pendências de corte e aterro.

| Objetivo | Medida utilizada |
|---|---|
| Minimizar momento total | Volume em banco multiplicado pela DMT |
| Minimizar momento extraordinário | Volume em banco multiplicado pela distância que excede a franquia |
| Minimizar custo por faixa | Volume em banco multiplicado pelo custo unitário da faixa |

### Momento total e extraordinário

Para **1.000 m³ de banco**, DMT de **800 m** e franquia de **200 m**, o momento total é **800 m³·km**. O momento extraordinário é **600 m³·km**.

A franquia afeta o momento extraordinário, mas não encurta a DMT nem aumenta o limite máximo de transporte. O relatório pode apresentar ambos os momentos independentemente do objetivo escolhido.

### Faixas de DMT e custos

Cada faixa tem **Nome**, **Ate_m** e **Custo_por_m3_banco**. O limite superior é inclusivo: uma distância de 200 m pertence à faixa cujo limite é 200 m. A próxima começa acima desse limite. Não repita limites e cubra todas as distâncias permitidas.

O custo unitário vale para o volume inteiro transportado naquela faixa, não apenas para o trecho de distância adicional. Exemplo: **1.000 m³** e custo **3,50 por m³ de banco** produzem custo de **3.500,00**, na unidade monetária adotada pelo usuário.

Os custos iniciais são valores didáticos. O programa não consulta automaticamente tabelas SICRO, preços atuais ou contratos. Informe custos compatíveis entre as faixas. O campo pode representar uma composição definida pelo projetista; não há cálculo automático separado do preço de aquisição da jazida.

Para comparar alternativas, salve cada cenário com nome próprio e mantenha os mesmos critérios e unidades na comparação.

<!-- page -->

## 10 Calcule e interprete as pendências

Clique em **Calcular distribuição** após revisar todas as entradas. O programa valida os dados e cria as abas **Distribuição** e **Pendências / reservas**.

### Distribuição

Cada linha mostra origem, destino, categoria, volumes em banco e geométrico, DMT, faixa, momentos e custo. O volume geométrico corresponde ao destino: ele é obtido dividindo o volume em banco pelo fator do aterro. Para ADME com fator 1, os valores coincidem numericamente, mas a capacidade foi informada em banco.

O total distribuído mostrado na janela é o transporte longitudinal calculado. A compensação lateral, quando utilizada, é registrada separadamente e deve ser somada à análise global de aproveitamento de materiais.

### Diferencie problemas de reservas disponíveis

| Situação | Interpretação |
|---|---|
| Corte sem destino | Material ainda precisa de aterro compatível, ADME ou rota viável |
| Demanda não atendida | Aterro ainda precisa de oferta compatível ou acesso viável |
| Reserva de jazida não utilizada | Parte da oferta opcional permaneceu disponível |
| Capacidade ADME não utilizada | Parte da capacidade de disposição permaneceu ociosa |

O contador de pendências/reservas inclui as quatro situações. Um número maior que zero não significa que todas as linhas sejam erros. Leia o texto de cada item.

### Se o balanço não fechar

Confira volumes, fatores, qualidade, categorias, acessos, barreiras, bloqueios e DMT máximo. Depois avalie a necessidade real de cadastrar empréstimo ou ADME. O programa aponta os itens sem solução, mas não cria um local fictício nem escolhe automaticamente onde implantar uma jazida.

Depois de corrigir qualquer entrada, calcule novamente antes de exportar ou desenhar. Um resultado calculado com pendências continua sendo uma solução parcial; não o trate como distribuição completa da obra.

<!-- page -->

## 11 Salve exporte e leia o Bruckner

### Salvar projeto

Use **Salvar projeto** e escolha o arquivo JSON. Ele preserva as entradas do cenário, incluindo materiais, volumes, rotas, custos e cubação. Ao usar **Abrir projeto**, calcule novamente para obter os resultados. O JSON é independente do DWG e pode ser usado para estudar alternativas.

**Abrir projeto** substitui o cenário da janela. Salve antes se quiser preservar a alternativa atual. Não conte com uma solicitação automática de salvamento ao fechar a janela ou o Civil 3D.

### Exportar CSV

O CSV de saída contém o quadro de movimentos e, em seguida, o quadro de pendências. Abra no Excel como dados delimitados por ponto e vírgula. Os números são exportados com ponto decimal e precisão elevada; confira as opções de importação do Excel antes de fazer somas.

O CSV exportado não é um arquivo de projeto e não pode ser lido pelo botão **Importar áreas CSV**, que espera somente progressiva e áreas. A compensação lateral é detalhada no relatório HTML e no JSON, não no quadro de transporte longitudinal do CSV.

### Relatório HTML

O relatório apresenta distribuição, escavação/carga/transporte por categoria e faixa, DMT ponderada, momentos, custos, aterros por energia informada, pendências e entradas do cenário. Quando houver cubação, inclui o **Diagrama de Bruckner**. Abra o arquivo salvo no navegador para consultar ou imprimir.

### Como interpretar o Bruckner

O gráfico mostra o saldo acumulado de corte menos aterro convertido em banco, conforme o fator usado ao gerar a cubação. Trechos ascendentes indicam acréscimo líquido de corte; descendentes, maior demanda de aterro.

O gráfico não incorpora a compatibilidade geotécnica e não é o quadro otimizado de distribuição. A compensação lateral não altera o saldo líquido de corte menos aterro homogeneizado. Se editar fatores de aterros manualmente depois da geração, confira o fator indicado no gráfico: o Bruckner usa o fator de cubação registrado, não uma nova conversão por destino.

<!-- page -->

## 12 Desenhe a distribuição e os movimentos

### Diagrama linear

1. Calcule a distribuição e salve o cenário.
2. Clique em **Desenhar diagrama**. A janela fecha para permitir a seleção no desenho.
3. Indique o ponto de inserção.
4. Informe a escala horizontal em unidades de desenho por metro. Em um desenho em metros, **1** representa uma unidade por metro.
5. Confira as ligações e os textos na camada **IFM_TERRAPLENAGEM**.

O diagrama relaciona origem, destino, volume em banco e DMT de cada movimento. O espaçamento vertical organiza as linhas; não representa elevação ou perfil do terreno. Sem movimentos longitudinais, não há diagrama para inserir.

### Movimentos em planta

1. Salve o projeto JSON com as entradas corretas.
2. Digite **PRJTERRAPLANTA**.
3. Abra o JSON salvo. O programa recalcula esse cenário.
4. Selecione o alinhamento cujas progressivas correspondem aos dados.
5. Confira as setas na camada **IFM_TERRA_MOVIMENTOS**.

As setas ligam os centros dos itens no alinhamento. Para jazida ou ADME externa, a progressiva corresponde ao acesso no eixo. **As linhas não mostram a rota real**, não desenham acessos externos e seu comprimento em planta não substitui a DMT do cenário.

### Atualização e desfazer

Os objetos desenhados são uma saída estática. Alterar o JSON ou recalcular não atualiza automaticamente um diagrama já inserido. Para substituir uma versão antiga, remova a representação anterior e insira novamente, evitando duplicação.

As criações são feitas em transação e podem ser desfeitas com UNDO. Confira o arquivo ativo antes de inserir: a planta deve usar o alinhamento e o sistema de progressivas do cenário.

<!-- page -->

## 13 Resolva problemas e confira a entrega

### O comando não é reconhecido

Reabra o Civil 3D depois da instalação. Confirme que está no Civil 3D 2026 ou 2027. Este módulo usa **Terraplenagem.IFM.bundle** na pasta ApplicationPlugins do perfil do Windows. Ele é separado do PRJPAINEL e do assistente de IRI.

### Cabeçalho ou números inválidos

No CSV de áreas, preserve exatamente o cabeçalho **progressiva_m;corte_m2;aterro_m2** e as três colunas. Confira separador, decimais, áreas não negativas e ordem das progressivas. Não utilize notação de estaca+fração.

### Falha ao gerar grupos

Verifique se existem ao menos duas seções e se o comprimento máximo não é menor que o espaçamento entre seções. O fator deve ser positivo. Guarde as entradas manuais antes de regenerar origens e destinos.

### Rotas ou faixas inválidas

Confira nomes exatos de origem/destino, pares duplicados, limites repetidos e cobertura das faixas. Renomear um item não corrige automaticamente as linhas da aba Rotas e bloqueios. Revise-as após qualquer alteração dos nomes.

### Não há movimento apesar de haver material

Confira primeiro a compatibilidade geotécnica e a categoria. Depois examine SoDescarte, Jazida, ADME, bloqueios, barreiras, DMT máximo e capacidades. A qualidade do material e a existência de uma rota precisam ser atendidas simultaneamente.

### Lista Civil 3D rejeitada

Confira cálculo dos materiais, fatores iguais a 1 e a primeira seção. Refaça a leitura após atualizar o corredor e os materiais. Não suponha que o programa tenha atualizado os volumes em segundo plano.

### Antes de entregar

Confira unidades e referência de progressivas; volumes e fatores; classificação geotécnica; jazidas/ADME; acessos e restrições; objetivo e custos; pendências e reservas. Salve o JSON, gere novamente os documentos e confira os desenhos. Os valores iniciais do programa não são critérios aprovados para a obra.

<!-- page -->

## 14 Referência dos comandos e do escopo

| Comando | Uso |
|---|---|
| PRJTERRA | Abrir o cenário do documento atual |
| PRJTERRADEMO | Abrir exemplo fictício e substituir o cenário em memória |
| PRJTERRALINK | Ler uma lista de materiais do Civil 3D para novo cenário |
| PRJTERRAPLANTA | Recalcular JSON salvo e desenhar movimentos esquemáticos |

### Unidades de consulta rápida

| Grandeza | Unidade |
|---|---|
| Progressivas, acessos, DMT e franquia | m |
| Áreas de corte e aterro | m² |
| Oferta de corte e jazida | m³ de banco |
| Demanda de aterro | m³ geométricos, convertidos pelo fator |
| Capacidade ADME | m³ de banco, com fator 1 |
| CBR e expansão geotécnica | % |
| Permeabilidade | m/s |
| Momento total e extraordinário | m³·km |
| Custo unitário de faixa | Unidade monetária por m³ de banco |

### Limites da versão 0.1.0

O módulo não lê projetos .Ddm nem possui editor de expressões para hachuras e materiais. Não faz classificação automática por camada/categoria, não procura geometricamente locais de jazida, não calcula volumes soltos nem rotas reais de transporte e não mantém desenhos atualizados dinamicamente.

A compensação lateral depende da hipótese explícita de compatibilidade local. A energia de compactação é informada para agrupamento, sem certificação automática. A importação da lista de materiais utiliza os totais da lista; materiais distintos precisam ser separados conforme o projeto.

**Base do manual:** código e telas da versão 0.1.0 instalada. Este documento não representa um novo teste de todos os comandos em sessão Civil 3D nem uma verificação normativa dos projetos. Revisão de 15 de outubro de 2026.
