# Manual de uso do PRJPAINEL

**PROJETA REDES — IFM · Versão 1.0.0 · Civil 3D 2026 e 2027**

Este manual ensina a abrir o painel, criar e editar redes, conferir o dimensionamento e produzir saídas. Também apresenta os dispositivos longitudinais, o modelador e a integração com SWMM e EPANET. Foi elaborado a partir dos comandos e das telas do pacote instalado. Os exemplos são didáticos; os parâmetros de cada obra precisam ser definidos pelo projetista.

## 1 Comece por aqui

1. Abra o Civil 3D e um desenho de trabalho em metros.
2. Digite **PRJPAINEL** e pressione Enter. Aparece o painel **PROJETA REDES — IFM**.
3. Clique no botão desejado e acompanhe as perguntas na linha de comando. Use F2 quando precisar consultar mensagens anteriores.
4. Se quiser conhecer o programa, abra um desenho vazio e use **Demonstração**, ou digite **PRJDEMO**. Esse comando cria objetos; use um arquivo de teste.
5. Para começar uma rede real de drenagem, siga a sequência abaixo.

**Criar rede → Opções da rede → Equações de chuva → Lançar PVs e tubos → Criar bacia → Calcular → Recalcular → Aplicar ao desenho → Relatórios → Salvar DWG.**

O painel é um lançador de comandos. Ao clicar em um botão, ele interrompe o comando que estava em execução para iniciar o escolhido. Termine uma operação antes de clicar em outra.

## Como escolher a rede

Quando existe apenas uma rede compatível, o programa a seleciona. Quando há mais de uma, clique em um elemento da rede desejada ou use a opção **Lista** e informe o número mostrado. Confira o nome da rede nas mensagens antes de editar.

## Como os dados são guardados

O projeto fica associado ao DWG. **Salvar o comando no projeto não substitui salvar o arquivo:** use QSAVE ou Salvar no Civil 3D. A biblioteca de dispositivos e os caminhos dos motores externos ficam no perfil do Windows, fora do DWG.

**Outros módulos instalados:** o assistente de IRI/greide abre por **PROJETAASSISTENTE**; a distribuição de terraplenagem abre por **PRJTERRA**. Eles não são abas do PRJPAINEL.

<!-- page -->

## 2 Crie sua primeira rede de drenagem

### Prepare o desenho

Confirme que coordenadas, distâncias e cotas estão em metros. Tenha uma superfície de terreno cobrindo os nós previstos. Se precisar de estacas e projeção longitudinal, prepare também o alinhamento e a vista de perfil correspondentes. Faça uma cópia do desenho para o primeiro ensaio.

### Passo a passo

1. Clique em **Criar rede** — PRJREDE. Escolha **Drenagem** e informe um nome que identifique o trecho.
2. Selecione a superfície de terreno e, quando necessário, o alinhamento de referência. Confira qual superfície está sendo usada; não escolha automaticamente um candidato só pelo prefixo do nome.
3. Na janela de opções, revise os parâmetros de cálculo e desenho. Confirme recobrimentos, limites de declividade, velocidades, lâmina admissível, seções comerciais e critérios de concordância.
4. Configure a chuva em **Equações de chuva** — PRJEQUACOES, conforme o capítulo seguinte.
5. Clique em **Lançar PVs e tubos** — PRJLANCAR. Clique os pontos na ordem **montante → jusante**. Os pontos consecutivos são ligados por trechos.
6. Durante o lançamento, use **Tipo** para escolher PV, BL, Caixa ou Exutório. Use **Novo** para começar outro ramal e **Desfazer** para retirar o último lançamento. Enter encerra a sequência.
7. Para ligar o ramal a um nó existente, clique sobre esse nó. O programa procura nós próximos; confirme a mensagem de conexão para evitar criar ligações na rede errada.
8. Cadastre as bacias e confira o lançamento de cada uma. Depois use **Calcular (planilha)**.

### Nós avulsos e ligações posteriores

**Inserir nó** — PRJNO cria um dispositivo sem precisar lançar uma sequência inteira. **Ligar tubo** — PRJTUBO pede primeiro o nó de montante e depois o nó de jusante; ambos devem pertencer à mesma rede.

### Antes do primeiro cálculo

Confira se os ramais estão conectados, se o exutório está correto e se existem cotas e contribuições coerentes. Uma representação em planta aparentemente correta não comprova que a topologia e os dados de cálculo estão completos.

<!-- page -->

## 3 Cadastre a chuva e as bacias

### Equações de chuva

Abra **Equações de chuva** — PRJEQUACOES. O catálogo inicial possui equações identificadas para Belo Horizonte, Brasília, Lavras e uma equação regional da RMBH. Escolha a formulação adequada ao local e confira seus parâmetros e sua fonte. Na regional da RMBH, revise a precipitação anual **PA** do local.

Ao confirmar a escolha, indique a rede de drenagem que usará a equação, quando solicitado. A alteração não substitui o cálculo: execute PRJCALCULAR novamente. Ajuste também o período de retorno e os parâmetros relacionados em **Opções da rede**.

### Criar uma bacia

1. Desenhe o limite da área de contribuição com uma polilinha fechada.
2. Clique em **Criar bacia** — PRJBACIA e escolha a rede de drenagem.
3. Selecione os contornos fechados. Contornos abertos são ignorados.
4. Confira a área apresentada. Com o desenho em metros, o programa converte m² para hectares.
5. Escolha o nó que recebe a contribuição. O programa sugere o nó mais próximo do centroide; essa proximidade não comprova o caminho do escoamento. Se necessário, selecione explicitamente o nó correto.
6. Informe o coeficiente de escoamento **C**.
7. Selecione o talvegue para o cálculo de tc por Kirpich, ou pressione Enter para usar tc informado. Na criação sem talvegue, o valor inicial usa o tc mínimo da rede; revise-o em **Propriedades**.

### Campos que precisam de atenção

| Campo | Significado | Unidade ou formato |
|---|---|---|
| Área da bacia | Área contribuinte | ha |
| C | Coeficiente de escoamento | adimensional |
| tc | Tempo de concentração | min |
| Intensidade de chuva | Resultado da equação IDF | mm/h |
| Período de retorno | Entrada da chuva de projeto | conforme o campo da equação |

Após editar o contorno, o talvegue ou o vínculo da bacia, use **Atualizar** e confira novamente as contribuições. Verifique especialmente sobreposição de bacias e lançamentos duplicados.

<!-- page -->

## 4 Use a planilha de dimensionamento

Clique em **Calcular (planilha)** — PRJCALCULAR. Escolha a rede. O programa atualiza os dados necessários, calcula e abre a planilha. Este comando atende às redes por gravidade; a rede de água é simulada pelo EPANET.

### Colunas editáveis

As quatro colunas com fundo azul-claro permitem impor valores ao cálculo. As outras apresentam resultados.

| Coluna | O que informar | Exemplo didático |
|---|---|---|
| D/b forçado ou DN forçado | Diâmetro ou largura, em mm | 600 significa 0,60 m |
| I forçada | Declividade, em % | 1 significa 1% |
| CF mont. forçada | Cota de fundo a montante, em m | 850,250 |
| CF jus. forçada | Cota de fundo a jusante, em m | 849,750 |

Não confunda a unidade desta planilha com a do CSV de entrada: a planilha usa **mm e %** nas colunas de diâmetro e declividade; o CSV usa **m e m/m**.

### Sequência correta para editar

1. Altere o valor na coluna azul do trecho desejado.
2. Saia da célula e clique em **Recalcular**. Confira os resultados e as mensagens; os trechos conectados podem ser afetados.
3. Repita os ajustes necessários e verifique erros, avisos, cotas, recobrimentos e velocidades.
4. Clique em **Aplicar ao desenho** para redesenhar e gravar o resultado no projeto do DWG.
5. Salve o DWG.

**Recalcular** atualiza a análise na janela. **Aplicar ao desenho** confirma o cenário no desenho. Não aplique ou exporte logo após digitar uma alteração sem antes recalcular: o botão Aplicar não executa esse recálculo por conta própria.

**Limpar forçados** remove os valores impostos de diâmetro, declividade e cotas de fundo de todos os trechos da rede e recalcula. Para retirar uma imposição individual, deixe sua célula vazia e recalcule.

**Cancelar** fecha a planilha sem aplicar as alterações da sessão. Os botões **Exportar CSV** e **Relatório HTML** utilizam o resultado calculado na janela; recalcule antes de exportar.

<!-- page -->

## 5 Edite e atualize a rede

Use **Propriedades** para editar os dados de nó, tubo ou bacia. Selecione objetos pertencentes ao PROJETA, como bloco, linha, sólido ou rótulo reconhecido pelo programa. Uma linha comum do AutoCAD não se torna um tubo do PROJETA apenas por ter a mesma aparência.

| Ferramenta | Uso principal |
|---|---|
| Conectar | Mudar o nó de jusante de um tubo |
| Inverter sentido | Trocar montante e jusante do trecho selecionado |
| Dividir tubo | Inserir um nó e dividir o trecho |
| Trocar dispositivo | Alterar o modelo dos nós selecionados |
| Remover | Retirar elementos do modelo da rede |
| Numerar | Renumerar nós e trechos por coletor |

**Remover um nó pode remover seus tubos associados.** Confira a seleção antes de concluir. Depois de conectar, inverter ou dividir trechos, revise a topologia e o cálculo.

### Quando usar Atualizar

Execute **Atualizar** — PRJATUALIZAR após mover nós, alterar contornos, editar parâmetros ou fazer mudanças gráficas que precisem voltar ao modelo. O comando lê alterações reconhecidas, amostra o terreno, recalcula e redesenha. Quando há várias redes, pode oferecer atualização de uma ou de todas.

A atualização redesenha elementos gerados pelo programa. Evite depender de edições manuais da geometria dos sólidos ou dos tubos para representar parâmetros que deveriam estar nas propriedades da rede.

### Organização dos layers

As redes usam prefixos como **PRJ-DRE**, **PRJ-ESG**, **PRJ-AGU** e **PRJ-GEN**, conforme o tipo. Os sufixos identificam tubos, dispositivos, textos e objetos 3D. Para examinar somente a planta, congele os layers de sólidos com sufixo **-3D**.

### Salve antes de renumerar uma entrega

A renumeração altera as referências apresentadas nas saídas. Após numerar, gere novamente notas e relatórios que serão usados na entrega. Use UNDO quando precisar desfazer uma operação do desenho e confira o modelo após desfazer.

<!-- page -->

## 6 Gere tabelas e documentos

Faça o cálculo e aplique o resultado antes de produzir os documentos da rede. Confira se a rede escolhida e a data do cálculo correspondem ao cenário que será entregue.

### Tabela no desenho

Use **Tabela no desenho** — PRJTABELA. Escolha entre **Dimensionamento**, **Nos**, **Materiais**, **Vala** ou **Bacias**. Informe a altura do texto e o ponto de inserção do canto superior esquerdo. Confira a legibilidade na escala de impressão.

### Relatório HTML

Use **Relatório HTML** — PRJRELATORIO. Escolha a rede calculada e o local do arquivo. O relatório reúne parâmetros, resultados, mensagens e quadros associados. Abra o HTML no navegador para consultar ou imprimir. Depois de alterar o projeto, gere uma nova versão do arquivo.

### Nota de serviço dos PVs

Use **Nota de serviço PVs** — PRJNOTACAIXAS. Salve o CSV e abra no Excel. Confira nomes, coordenadas, cotas e profundidades antes de usar a nota em campo.

### Projetar no perfil

Use **Projetar no perfil** — PRJPERFIL e acompanhe as solicitações para a vista de perfil. Confirme que alinhamento, estacas e vista pertencem ao trecho da rede. A representação projetada é uma saída do PROJETA; não a trate automaticamente como uma Pipe Network nativa.

### Interferências

Use **Interferências** — PRJINTERFERENCIA para verificar cruzamentos entre redes modeladas. Revise a folga vertical configurada e os itens apontados. O resultado depende de quais redes e geometrias estão efetivamente representadas; elementos externos não cadastrados não entram automaticamente na análise.

### Escavação de valas

Use **Escavação de valas** — PRJESCAVACAO. Revise os parâmetros da vala e examine os volumes por faixa de profundidade, berço, reaterro e demais quantitativos apresentados. Uma tabela de volume não substitui a definição do método executivo e da seção de escavação da obra.

<!-- page -->

## 7 Lance valetas sarjetas e canaletas

O grupo **Longitudinais** utiliza alinhamento e uma referência de cota para gerar os dispositivos ao longo do trecho. As seções vêm da biblioteca; seus parâmetros podem ser editados.

### Criar o dispositivo

1. Clique em **Valeta / sarjeta** — PRJLONGITUDINAL.
2. Selecione o alinhamento correspondente.
3. Escolha a **Seção-tipo**: valeta, sarjeta, canaleta, meia-cana, meio-fio ou outra seção disponível.
4. Edite as dimensões na grade de parâmetros e confira a prévia da seção.
5. Nas propriedades, defina início, fim, afastamento, desnível e demais dados de locação.
6. Em **Cota a partir de**, confirme se a origem é **Perfil** ou **Superfície**. Selecione explicitamente o perfil ou a superfície desejada nas listas superiores.
7. Confirme a orientação da seção: o eixo X cresce para a direita do alinhamento. Use **Espelhar** quando necessário no lado esquerdo.
8. Escolha a equação de chuva e os parâmetros hidráulicos aplicáveis. Clique em **Gerar** e leia as mensagens.

As listas podem apresentar uma opção inicial automaticamente. Antes de gerar, confira o nome da referência vertical; o primeiro perfil da lista não é necessariamente o greide correto.

### Editar e conferir

**Editar longitudinal** — PRJLONGEDITAR permite selecionar um dispositivo, mudar as entradas e regenerá-lo. Se o alinhamento original não existir mais, o comando informa o problema.

**Quantitativos** — PRJLONGQUANT apresenta comprimento, concreto, escavação, área revestida, declividades, capacidade mínima e comprimento crítico, quando calculáveis. Confira as observações e a compatibilidade da seção com o escoamento real.

**Exemplo de conferência:** gere um trecho curto, examine planta, seção e vista 3D, confira o lado do alinhamento e compare o comprimento com o esperado. Só depois amplie o lançamento para todo o trecho.

<!-- page -->

## 8 Trabalhe com redes de esgoto

O fluxo de criação e edição é semelhante ao da drenagem, mas as contribuições e os critérios são os de esgoto configurados no programa. A janela de opções identifica esses critérios como **Esgoto (NBR 9649)**; essa identificação não comprova, sozinha, atendimento a todas as exigências do projeto.

1. Use **Criar rede** — PRJREDE e escolha **Esgoto**.
2. Selecione superfície e alinhamento, quando necessários.
3. Abra **Opções da rede** e revise população inicial e final, consumo/contribuição, coeficientes, infiltração, limites hidráulicos e condições dos dispositivos.
4. Se precisar estimar a população, abra **Projeção populacional** — PRJPOPULACAO. Confira os dados históricos, o horizonte e o método escolhido antes de utilizar os resultados como entrada da rede.
5. Lance os nós e tubos de montante para jusante.
6. Use **Calcular (planilha)**, examine as mensagens e ajuste os valores forçados somente quando necessário.
7. Recalcule, aplique ao desenho e gere as saídas.

### O que conferir

Revise vazões inicial e final, infiltração, contribuição por extensão, lâmina relativa, velocidade, tensão trativa, declividades, recobrimentos e degraus. Confira também o sentido dos ramais e o destino final da rede.

Uma projeção populacional plausível não garante que as vazões de entrada estejam corretas. Confira como os valores foram levados aos parâmetros da rede e se as unidades coincidem.

### SWMM para esgoto

Os comandos de SWMM também aceitam redes de esgoto por gravidade. Confira as condições de contorno e as entradas exportadas antes de interpretar a simulação. Não substitua automaticamente o dimensionamento por gravidade por um resultado de simulação sem comparar as hipóteses.

<!-- page -->

## 9 Trabalhe com redes de água e EPANET

Para água, o caminho de cálculo é **EPANET: simular**. O botão **Calcular (planilha)** não seleciona redes de água.

### Preparação e simulação

1. Crie uma rede do tipo **Agua** com PRJREDE.
2. Revise em **Opções da rede** os consumos, população, coeficientes, diâmetros, rugosidade e limites de pressão e velocidade.
3. Lance as junções e os tubos. Use **Inserir nó** para cadastrar pelo menos um **Reservatorio** ou **Tanque**, conforme a modelagem. Ao inserir reservatório, o comando pede seu nível d'água em metros.
4. Edite os demais dados do reservatório/tanque e as demandas nos campos disponíveis. Confira os níveis e a referência altimétrica.
5. Configure o executável de linha de comando do EPANET em **Configurar motores** — PRJMOTORES.
6. Clique em **EPANET: simular** — PRJEPANETCALCULAR. Escolha a rede de água.
7. Examine os quadros de nós e tubos: demanda, carga, pressão, vazão, velocidade, perda e situação.

### Ajuste de diâmetros

Com **Dimensionar por velocidade** ativado nas opções de água, o programa pode aumentar diâmetros e repetir a simulação. O ajuste é guiado por velocidade; não é uma otimização completa de custo nem garante que todas as pressões fiquem adequadas.

Confira as mensagens e os avisos de pressão baixa, pressão alta e velocidade elevada. Verifique o efeito de mudanças de diâmetro na rede inteira.

### Arquivos EPANET

**EPANET: exportar** — PRJEPANETEXPORTAR gera um arquivo .inp. **EPANET: importar** — PRJEPANETIMPORTAR cria uma rede a partir de .inp. Depois da importação, confira as mensagens e a correspondência dos elementos; não presuma que qualquer recurso de um modelo externo será reproduzido integralmente.

<!-- page -->

## 10 Simule a drenagem com SWMM

Os comandos de exportar e importar gerenciam arquivos .inp. A execução da simulação depende de um motor externo de linha de comando, como **runswmm.exe**. Abrir o painel não instala esse motor automaticamente.

### Configure os motores

Use **Configurar motores** — PRJMOTORES para informar os executáveis SWMM e EPANET. Se o caminho ainda não estiver configurado, o comando de simulação pode solicitar o arquivo. Selecione o motor de linha de comando correspondente, não apenas o atalho da interface gráfica.

### Execute e interprete

1. Dimensione a rede de drenagem, confira as bacias e aplique ao desenho.
2. Clique em **SWMM: simular** — PRJSWMMCALCULAR.
3. Escolha a rede e revise a janela de opções do SWMM, especialmente os dados da chuva e do período de simulação disponíveis.
4. Confirme o motor, quando solicitado, e aguarde a execução.
5. No quadro de trechos, compare a vazão de dimensionamento com a vazão máxima simulada. Confira velocidade, Q/Qplena, lâmina relativa e sobrecarga.
6. No quadro de nós, confira profundidades, carga hidráulica máxima e alagamentos.
7. Leia erros, avisos e erros de continuidade. A mensagem informa a pasta onde ficaram os arquivos da execução.

### Por que as vazões podem ser diferentes

O dimensionamento racional e a simulação dinâmica podem usar representações diferentes de chuva, armazenamento e propagação. Uma vazão maior no SWMM não demonstra, por si só, erro do outro cálculo. Compare área, coeficientes, duração, chuva de projeto, contribuições e condições de contorno.

### Exportação para análise externa

Use **SWMM: exportar** — PRJSWMMEXPORTAR para gerar o .inp e abrir no SWMM. Use **SWMM: importar** — PRJSWMMIMPORTAR para trazer uma rede e revise os avisos. Recursos como controles, LID e qualidade da água não estão integralmente implementados nesta versão do plugin.

<!-- page -->

## 11 Importe e exporte dados

### Pipe Network do Civil 3D

Clique em **Importar Pipe Network** — PRJIMPORTARPIPE. Escolha a rede nativa na lista, o tipo de rede e se deseja manter diâmetros e cotas de fundo. Depois escolha a superfície, quando aplicável, e confira a rede convertida.

A conversão cria uma representação própria do PROJETA. Não é um vínculo dinâmico bidirecional com a Pipe Network original. Se escolher uma superfície na importação, o programa libera as cotas de tampa forçadas para amostrar o terreno; confira essas cotas após converter.

### CSV para Excel

1. Clique em **Modelo de planilha** — PRJMODELOCSV e salve o exemplo gerado pelo próprio programa.
2. Abra no Excel e siga a estrutura das linhas de nós, trechos e bacias. Preserve os identificadores que ligam os trechos aos nós.
3. Salve como CSV compatível, em UTF-8 e separado por ponto e vírgula. Use vírgula decimal conforme o modelo; evite separadores de milhar.
4. Use **Importar planilha** — PRJIMPORTARCSV. Escolha uma rede nova ou existente, quando oferecido. Leia as mensagens antes de continuar após qualquer erro.
5. Confira a geometria, os vínculos e os dados hidráulicos e calcule novamente.

| No CSV de entrada | Unidade |
|---|---|
| Coordenadas, cotas e diâmetro | m |
| Declividade de TRECHO | m/m |
| Área de BACIA | ha |
| tc de BACIA | min |

**Exemplo de unidade:** para um tubo de 600 mm e declividade de 1%, o CSV usa diâmetro **0,60** e declividade **0,01**. Na planilha dinâmica, as mesmas grandezas aparecem como **600** e **1**.

**Exportar planilha** — PRJEXPORTARCSV gera um CSV da rede reimportável. Ele é diferente do CSV de dimensionamento e da nota de PVs.

### Transferência em JSON

Os comandos adicionais **PRJEXPORTARREDE** e **PRJIMPORTARREDE** transferem redes entre desenhos. Após importar, revise a superfície e o alinhamento de referência. O JSON da rede não substitui o backup do DWG nem leva automaticamente a biblioteca personalizada do Windows.

<!-- page -->

## 12 Use o modelador e a biblioteca

**Modelador** — PRJMODELADOR abre o editor de dispositivos e seções paramétricas. A biblioteca inclui modelos como PV circular, PV retangular, caixa, boca de lobo e ala de lançamento. A definição é em JSON e pode conter expressões.

### Trabalhe a partir de um modelo existente

1. Abra o modelador e escolha um dispositivo ou seção parecido com o necessário.
2. Examine a definição, os parâmetros e a prévia antes de editar.
3. Para criar uma alternativa, use um identificador próprio. Um modelo personalizado com o mesmo ID de um modelo padrão o substitui na biblioteca carregada.
4. Altere as dimensões e as expressões, avalie o modelo e corrija as mensagens de validação.
5. Confira planta, seção e quantitativos. Salve na biblioteca do usuário.
6. Faça uma inserção de teste em um desenho vazio e confira o sólido 3D.

### Inserir fora de uma rede

**Inserir dispositivo** — PRJDISPOSITIVO permite escolher o modelo, informar parâmetros, indicar o ponto e a rotação. Um dispositivo avulso não passa a integrar uma rede apenas por estar próximo de um tubo.

### Expressões e identificadores

Os parâmetros aceitam operações e funções previstas pelo editor. Use nomes distintos também quanto à grafia: o avaliador não diferencia maiúsculas de minúsculas. Por exemplo, **hg** e **Hg** colidem e não devem ser usados como dois parâmetros diferentes.

O modelador aponta nomes repetidos e problemas geométricos, como polígonos que se cruzam. Revise também as hipóteses das operações booleanas: os volumes mostrados na avaliação do modelo podem ser estimativas; compare com as propriedades do sólido efetivamente criado.

### Onde a biblioteca fica

A biblioteca personalizada fica em **%APPDATA%\Projeta\biblioteca**, nas subpastas **dispositivos** e **secoes**. Use o comando adicional **PRJBIBLIOTECA** para abrir a pasta e recarregar definições. Faça backup dessa pasta ao transferir modelos para outro computador.

<!-- page -->

## 13 Resolva problemas comuns

### PRJPAINEL não é reconhecido

Feche e reabra o Civil 3D após a instalação. Confira que está abrindo o Civil 3D 2026 ou 2027. A instalação deste plugin é **Projeta.bundle**, na pasta ApplicationPlugins do perfil do usuário. Não confunda com o assistente de IRI, cujo comando é PROJETAASSISTENTE. Se persistir, registre a mensagem de carregamento; não desative a segurança do Civil 3D como primeira tentativa.

### O painel está aberto mas nada acontece

Confira a linha de comando: o programa pode estar aguardando seleção, número, ponto ou confirmação. Ative o desenho correto e pressione Esc apenas se quiser cancelar a operação atual. Depois clique no comando novamente.

### Nenhuma rede compatível

Verifique se há uma rede desse tipo no DWG. O cálculo por planilha exclui água; EPANET exige água; a criação de bacias exige drenagem. Elementos de outro plugin não são automaticamente reconhecidos como objetos do PROJETA.

### Cotas ou dimensões inesperadas

Confira unidades, superfície, cotas forçadas, fonte vertical, sentido dos tubos e valores impostos na planilha. A superfície precisa cobrir o local. Para longitudinais, confira perfil, afastamento, desnível e espelhamento.

### Resultados não mudaram depois de editar

Na planilha, clique em Recalcular antes de Aplicar ao desenho ou exportar. Depois de alterações no desenho, use Atualizar. Refaça relatórios e quadros exportados para que representem o cenário atual; arquivos já exportados não são atualizados automaticamente.

### SWMM ou EPANET não executa

Confira o caminho do motor em PRJMOTORES e as mensagens da execução. Verifique se o relatório foi criado e se o arquivo .inp é aceito pelo motor escolhido. Na água, confira se há reservatório ou tanque e condições de contorno válidas.

### Confira antes de entregar

Confirme rede, superfícies, cotas, contribuições, parâmetros, valores forçados e mensagens. Examine planta, perfil e sólidos. Recalcule, aplique, gere novamente as saídas e salve o DWG. As verificações automáticas do programa apoiam a revisão, mas não certificam integralmente o projeto.

<!-- page -->

## 14 Referência dos botões de rede e edição

Os nomes abaixo correspondem aos botões do PRJPAINEL. O mesmo comando pode ser digitado diretamente na linha de comando.

| Grupo e botão | Comando | Finalidade |
|---|---|---|
| Rede · Criar rede | PRJREDE | Criar drenagem, esgoto, água ou rede genérica |
| Rede · Opções da rede | PRJOPCOES | Alterar referências, parâmetros e desenho |
| Rede · Equações de chuva | PRJEQUACOES | Escolher ou cadastrar equações IDF |
| Rede · Lançar PVs e tubos | PRJLANCAR | Lançar sequência de nós e trechos |
| Rede · Inserir nó | PRJNO | Inserir um nó avulso na rede |
| Rede · Ligar tubo | PRJTUBO | Ligar dois nós da mesma rede |
| Rede · Criar bacia | PRJBACIA | Associar contorno e contribuição a um nó |
| Rede · Propriedades | PRJPROPRIEDADES | Editar dados de nó, tubo ou bacia |
| Edição · Conectar | PRJCONECTAR | Mudar o nó de jusante |
| Edição · Inverter sentido | PRJINVERTER | Trocar montante e jusante |
| Edição · Dividir tubo | PRJDIVIDIR | Dividir o trecho com um nó |
| Edição · Trocar dispositivo | PRJTROCAR | Alterar o modelo dos nós |
| Edição · Remover | PRJREMOVER | Remover elementos do projeto |
| Edição · Numerar | PRJNUMERAR | Renumerar nós e trechos |
| Cálculo · Calcular (planilha) | PRJCALCULAR | Calcular rede por gravidade e editar forçados |
| Cálculo · Atualizar | PRJATUALIZAR | Sincronizar, recalcular e redesenhar |

**Dica de navegação:** passe o mouse sobre o botão para consultar a dica com o nome do comando. Se houver mais de uma rede, confira a seleção antes de executar a alteração.

<!-- page -->

## 15 Referência das saídas e integrações

| Grupo e botão | Comando | Finalidade |
|---|---|---|
| Saídas · Tabela no desenho | PRJTABELA | Inserir quadros no DWG |
| Saídas · Relatório HTML | PRJRELATORIO | Gerar relatório da rede |
| Saídas · Nota de serviço PVs | PRJNOTACAIXAS | Exportar a nota em CSV |
| Saídas · Projetar no perfil | PRJPERFIL | Representar a rede na vista de perfil |
| Saídas · Interferências | PRJINTERFERENCIA | Verificar cruzamentos entre redes |
| Saídas · Escavação de valas | PRJESCAVACAO | Calcular quantitativos da vala |
| Integração · Importar Pipe Network | PRJIMPORTARPIPE | Converter uma rede nativa do Civil 3D |
| Integração · Importar planilha | PRJIMPORTARCSV | Importar rede em CSV |
| Integração · Exportar planilha | PRJEXPORTARCSV | Exportar CSV reimportável |
| Integração · Modelo de planilha | PRJMODELOCSV | Gerar exemplo do formato CSV |
| Longitudinais · Valeta / sarjeta | PRJLONGITUDINAL | Criar dispositivo ao longo do alinhamento |
| Longitudinais · Editar longitudinal | PRJLONGEDITAR | Editar e regenerar o dispositivo |
| Longitudinais · Quantitativos | PRJLONGQUANT | Consultar volumes e capacidade |

**Dica para a entrega:** diferencie o CSV reimportável da rede, o CSV de dimensionamento e a nota de PVs. Eles têm finalidades e estruturas diferentes.

<!-- page -->

## 16 Referência de simulação modelador e ajuda

| Grupo e botão | Comando | Finalidade |
|---|---|---|
| SWMM · simular | PRJSWMMCALCULAR | Executar o motor e consultar resultados |
| SWMM · exportar | PRJSWMMEXPORTAR | Gerar arquivo .inp |
| SWMM · importar | PRJSWMMIMPORTAR | Criar rede a partir de .inp |
| EPANET · simular | PRJEPANETCALCULAR | Simular água e ajustar DN por velocidade, se ativado |
| EPANET · exportar | PRJEPANETEXPORTAR | Gerar arquivo .inp |
| EPANET · importar | PRJEPANETIMPORTAR | Criar rede a partir de .inp |
| Simulação · Projeção populacional | PRJPOPULACAO | Estimar população por métodos disponíveis |
| Simulação · Configurar motores | PRJMOTORES | Informar executáveis externos |
| Modelador · Modelador | PRJMODELADOR | Editar dispositivos e seções |
| Modelador · Inserir dispositivo | PRJDISPOSITIVO | Inserir modelo avulso |
| Modelador · Demonstração | PRJDEMO | Criar cenário didático e executar verificações |
| Modelador · Ajuda | PRJAJUDA | Listar comandos |

### Comandos adicionais sem botão próprio no painel

| Comando | Uso |
|---|---|
| PRJEXPORTARREDE | Exportar uma rede em JSON |
| PRJIMPORTARREDE | Importar uma rede em JSON |
| PRJEXCLUIRREDE | Excluir uma rede inteira após confirmação |
| PRJSECOES | Abrir o editor de seções |
| PRJBIBLIOTECA | Abrir a pasta e recarregar a biblioteca |

**Escopo:** manual da versão 1.0.0, baseado no código e nas telas do pacote PROJETA REDES — IFM instalado. Não registra um novo teste de todos os comandos em sessão Civil 3D e não constitui verificação normativa dos projetos. Revisão de 15 de outubro de 2026.
