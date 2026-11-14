# DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM

Versão inicial 0.1.0 para Civil 3D 2026 (.NET 8) e 2027 (.NET 10). Implementação própria baseada nas funções publicamente descritas do DDM; o instalador original não foi executado e suas DLLs não foram descompiladas. Este módulo tem arquivos, identidade e comandos próprios.

## Comandos

- `PRJTERRA`: abre o cenário do documento atual; salva/carrega projetos JSON independentes do DWG.
- `PRJTERRADEMO`: abre um cenário fictício; substitui o cenário em memória desse documento, sem desenhar automaticamente.
- `PRJTERRALINK`: seleciona alinhamento, grupo de linhas e lista de materiais explicitamente; lê corte/aterro incremental calculado pelo Civil 3D e abre a cubação.
- `PRJTERRAPLANTA`: abre um cenário JSON salvo, recalcula e desenha setas esquemáticas entre os centros no alinhamento escolhido. Linhas não são rotas de transporte.

No painel: **Desenhar diagrama** insere a distribuição linear na camada `IFM_TERRAPLENAGEM`, após escolha do ponto e da escala horizontal. A criação é feita em uma única transação e pode ser desfeita com UNDO. A escala é em unidades do desenho por metro; confirme as unidades do DWG.

## Fluxo de trabalho

1. Comece com `PRJTERRADEMO` e **Calcular distribuição**.
2. Para o projeto real, use `PRJTERRALINK` ou importe um CSV de áreas, UTF-8, separado por ponto e vírgula ou tabulação, com cabeçalho `progressiva_m;corte_m2;aterro_m2`. Não use estaca+fração nem separadores de milhar. Aceita vírgula decimal.
3. Em **Gerar origens/aterros**, informe fator banco/geométrico e comprimento máximo dos grupos. Essa ação substitui as origens e destinos anteriores. Revise cada grupo: categoria, CBR, expansão, permeabilidade, acessos e exigências dos aterros. Os valores iniciais são exemplos e não representam ensaios do local.
4. Cadastre jazidas com `Jazida=true`, volume disponível em banco, progressiva de acesso e distância adicional em `Acesso_m`. Para solo impróprio, marque `SoDescarte`.
5. Cadastre ADME com `ADME=true`, capacidade em banco e fator 1. Não há criação automática de uma localização fictícia para empréstimo/ADME: as pendências apontam os itens que precisam de solução.
6. Em **Rotas e bloqueios**, indique nomes de origem/destino exatamente como cadastrados; `DMT_override_m=-1` usa progressivas + acessos. DMT explícita é a distância total da rota alternativa, incluindo acessos. Bloqueado impede o par. Barreiras impedem travessias de progressiva, exceto quando existe rota alternativa explícita. Categoria exigida 0 aceita qualquer categoria compatível.
7. Revise faixas: o limite superior é inclusivo, e a faixa seguinte começa acima dele. O custo informado é por m³ de banco da faixa inteira. Custo não é tabela SICRO automática. Franquia afeta apenas o momento extraordinário.
8. Calcule, confira pendências, exporte CSV e relatório HTML. Salve o cenário antes de fechar. O DWG não armazena o projeto JSON automaticamente.

## Cálculos e unidades

CSV de áreas: volume = distância × média das áreas nas extremidades. Para áreas lineares, centroide longitudinal calculado pela integral do primeiro momento. Grupos são limitados pelo comprimento escolhido; não se inventam seções intermediárias. A classificação geotécnica é manual por grupo.

Lista Civil 3D: usa volumes incrementais nativos, sem recomputá-los a partir das áreas; centro do intervalo aproxima o centroide, pois a distribuição interna do volume não é fornecida. Só aceita fatores de corte/aterro iguais a 1 para evitar dupla homogeneização. A leitura soma a lista escolhida; categorias e qualidade de materiais não são inferidas dessa soma. Para classificar materiais diferentes, forneça origens separadas. Corrija/recalcule a lista no Civil 3D após alterações do corredor; o vínculo é uma fotografia, não atualização automática.

Demanda em banco = volume geométrico de aterro × fator banco/geométrico. O fator não é o percentual de expansão do ensaio. A expansão cadastrada é usada como critério de compatibilidade; não como conversão volumétrica. Volumes soltos não são calculados nesta versão. Energia de compactação é rótulo informado para agrupamento, sem certificação de atendimento normativo.

DMT automática = valor absoluto da diferença de progressivas + acessos. Momento = banco × DMT / 1000 (m³·km). Extraordinário = banco × máximo(0, DMT − franquia) / 1000.

O solver usa fluxo de custo mínimo com arcos reversos. Primeiro minimiza a soma de corte sem destino e aterro não atendido, em m³ de banco; depois minimiza o objetivo escolhido. Jazidas não utilizadas e capacidade ociosa de ADME não são penalizadas. Não utiliza um algoritmo guloso de proximidade. Se a oferta/demanda não fecha, a otimização é apenas sobre o volume viável; as pendências precisam ser resolvidas antes de aprovar o cenário. Jazida é capacidade disponível, não obrigação de escavar; ADME é capacidade disponível, não obrigação de preencher. Limite operacional: 600 itens entre origens e destinos.

## Escopo desta versão

Disponível: cubação por CSV/lista de materiais, agrupamento longitudinal, propriedades e restrições geotécnicas, jazidas e ADME, DMT e desvios, barreiras, objetivos de transporte/custo, quadros por categoria/faixa e energia informada, relatórios, projetos JSON, diagrama linear, Bruckner no relatório HTML e movimentos esquemáticos em planta. Compensação lateral opcional por intervalo, somente quando o usuário confirma que todo corte pode alimentar o aterro local. Ela é registrada à parte e retira apenas esses volumes do transporte longitudinal; não calcula o transporte transversal.

Ainda não equivale a todas as funções do DDM: sem leitura de projetos `.Ddm`, editor de expressões para hachuras/materiais, segmentação automática por camadas/categorias, otimização geométrica de localização de jazidas, estilos avançados e atualização dinâmica dos desenhos. As linhas em planta são esquemáticas e não incluem o traçado dos acessos externos.

## Instalação e verificação

Feche o Civil 3D e execute `Instalar.ps1`. Ele compila e instala apenas `Terraplenagem.IFM.bundle` em `%APPDATA%\Autodesk\ApplicationPlugins`, preservando os demais plugins. `-SemCompilar` usa os binários existentes. Não altera SECURELOAD nem locais confiáveis. Se uma política da empresa bloquear a carga, use o procedimento de confiança aprovado por ela.

Compilação: `dotnet build Civil/Terraplenagem.Civil.csproj -c Release -m:1 -p:UseSharedCompilation=false`.
Testes: `dotnet run --project Tests -c Release`.

A compilação e os testes do núcleo não comprovam o funcionamento dentro de uma sessão Civil 3D; leitura, painel e desenho precisam de teste no aplicativo com um DWG de validação.

Referências funcionais públicas: https://tbn2net.com/ddm/ e https://www.tbn2net.com/help/DDM/estudocaso.html.
