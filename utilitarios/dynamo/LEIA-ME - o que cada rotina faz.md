# Rotinas Dynamo — índice

Pasta organizada em 30/09/2026. Tudo roda no Dynamo do Civil 3D (nó *Python Script*).
Os arquivos que são só `.py` precisam ser colados num nó Python Script.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `Pontos COGO` | Point Groups por descrição e pontos de locação do corredor |
| `Greide automatico` | Greide gerado automaticamente a partir do terreno |
| `rotina corredores` | Loteamento: corredores em lote, esquinas, concordâncias, amarração de greides |
| `Recapeamento greide colado` | Greide colado na pista existente + corredor de recape |
| `Intersecao de vias` | Corredor de interseção entre duas vias |
| `Rotatoria` | Geometria de rotatória (6 nós Python para montar) |
| `Impressao de secoes PDF` | Plotagem de seções / notas de serviço em PDF |
| `_diagnostico API` | Sondas de leitura da API do Civil 3D (desenvolvimento) |
| `_versoes antigas` | Versões superadas, guardadas por segurança |

---

## Pontos COGO

### ClassificaPontos_PointGroups.dyn
Cria ou atualiza um Point Group para cada descrição (Raw Description) dos pontos COGO do desenho. Pontos sem descrição vão para o grupo `SEM_CODIGO`.
- Entrada: `Executar` (True para rodar).
- Não apaga nada; grupo com o mesmo nome é atualizado.

### Locacao_Corredor_Pontos.dyn (v3)
Gera pontos COGO de locação a partir dos códigos de ponto do corredor (eixo, bordo, drenagem, banqueta...), cria o grupo `LOCACAO - <corredor>` e exporta CSV PNEZD.
- Entradas: 1 corredores (`*` = todos) · 2 mapa `CODIGO=DESCRICAO` (ex.: `Crown=EIXO; ETW=BORDO`) · 3 intervalo (20 = estacas; 0 = todas as seções) · 4 pontos notáveis (PC, PT, TS, SC, início/fim de região) · 5 prefixo do grupo · 6 apagar pontos anteriores do grupo · 7 pasta do CSV (vazio = pasta do DWG; `-` = não exporta) · 8 EXECUTAR (False = só simula e lista os códigos encontrados).
- Saídas: `<desenho>_LOCACAO_PNEZD.csv` e `<desenho>_LOCACAO_relatorio.csv`; o último relatório também vai para `Documentos\Dynamo\locacao_ultimo_relatorio.txt`.
- A v3 aceita baselines por feature line (esquinas), que travavam a versão anterior.

---

## Greide automatico

### GreideAuto_v6.dyn  (+ `greide_dynamo.py`, mesmo código)
Lê o perfil do terreno do alinhamento, gera o greide acompanhando o terreno com a rampa limitada à máxima, dimensiona as curvas verticais parabólicas simétricas pelo K mínimo da velocidade diretriz (tabela AASHTO/DNIT) e cria o perfil `GREIDE-AUTO`.
- Sem entradas ligadas: os parâmetros ficam no topo do código (alinhamento, perfil do terreno, cota inicial, rampa máx. 8 % / mín. 0,5 %, velocidade 40 km/h, tangente mín. 40 m / máx. 300 m, tolerância 0,20 m, passo 10 m).
- Saídas na pasta do DWG: `<alinhamento>_greide_pvis.csv`, `_greide_pvis.txt` (para *Create Profile from File*) e `_terreno.csv`.
- É a versão mais nova (a v6 acrescentou a tangente máxima). O `greide_dynamo.py` é o mesmo código, para colar num nó novo.
- A versão LISP + Python fica na raiz: `GreideAuto_v6.lsp` e `greide_auto.py`.

---

## rotina corredores (loteamento)

> **Não renomeie nem mova esta pasta.** As rotinas daqui gravam relatórios com caminho fixo `D:\Claude ai\Dynamo rotinas\rotina corredores\`, e o AmarraGreides lê o `backup_greides.txt` daqui para desfazer.
>
> Os rótulos das entradas nesses gráficos ("MODO (auto | codigo)", "CORREDOR UNICO"...) foram copiados do CriarCorredoresEmLote. O significado real de cada entrada está abaixo.

### CriarCorredoresEmLote.dyn  (+ `.py`, mesmo código)
Cria os corredores de todas as ruas de uma vez: cada alinhamento com seu greide de projeto (FG) vira uma baseline, usando uma única seção tipo (assembly).
- IN[0] RODAR: False simula e reporta / True cria.
- IN[1] (rotulado "MODO"): na versão atual é um trecho do nome da assembly; vazio = usa a única assembly do desenho.
- IN[2] CORREDOR UNICO: True = 1 corredor com N baselines; False = 1 corredor por rua.
- IN[3] NOME BASE: nome do corredor (ou prefixo, se um por rua).
- Ignora alinhamentos offset / curb return. Ajustes de interseção ficam para depois, manualmente.

### DetectaEsquinas.dyn — somente leitura
Acha os cruzamentos entre alinhamentos, calcula as estacas e lista as concordâncias que seriam criadas (raio 2,0 m; eixo → face do meio-fio 4,2407 m). Grava `esquinas.txt`. Não altera o desenho.

### CriaConcordancias.dyn
Cria as concordâncias de esquina (alinhamentos `CONC...`) com raio 2,0 m no offset 4,2407 m, pelo mecanismo nativo de *Connected Alignment*. Não cria o objeto Interseção (a API não permite).
- IN[0] RODAR: False simula / True cria.
- IN[1] (rotulado "MODO"): quantas criar — `1` só a primeira (teste); `0` ou vazio = todas.
- Relatório: `concordancias.txt`.

### VerificaConcordancia.dyn — somente leitura
Mede as concordâncias `CONC` criadas (raio, offset, comprimento) e confere com o esperado (R = 2,0 m; 4,2407 m). Grava `verifica.txt`.

### AmarraGreides.dyn
Tira o degrau vertical nas esquinas: move o PVI da ponta da rua secundária para a cota do greide da rua principal. Principal é o eixo que continua para os dois lados; secundária é a que termina ali. Cruzamento de verdade (os dois continuam) não é mexido.
- IN[0] RODAR: False simula / True aplica.
- IN[1] (rotulado "MODO"): `amarrar` (padrão) ou `restaurar` (desfaz usando o backup).
- Avisa quando a rampa resultante passa de 8 %.
- `backup_greides.txt` = cotas originais (**não apague**); `amarracao.txt` = relatório; `backup_greides_ORIGINAL.txt` = cópia do primeiro backup.

Os demais `.txt` da pasta (esquinas, concordancias, verifica, vinculo, sonda_api, sonda_api2) são relatórios das rotinas e das sondas; são sobrescritos a cada execução.

---

## Recapeamento greide colado

### GreideColado_CorredorRecap.dyn  (+ `.py`, mesmo código)
Para recapeamento de rodovia (ex.: BR-222), em quatro etapas:
1. Amostra a superfície da pista existente no eixo e gera o **greide colado** (desvio máximo = tolerância).
2. Gera o **greide de projeto** paralelo, +0,22 m acima (mesmos PVIs e curvas).
3. Cria o **corredor** com a montagem de 11,00 m (3,50 faixa + 2,00 acostamento por lado, −2 %), frequência 1 m, e a superfície do corredor (links Top).
4. Troca o perfil da faixa de cotas do gráfico do perfil para o greide de projeto.
- Entradas: RODAR (False = só diagnóstico) · MODO (`tudo` | `greide` | `corredor`) · ALINHAMENTO · SUPERFICIE · MONTAGEM · ESTACA INICIAL/FINAL (aceita `14078`, `14078+10` ou `281+560`) · TOLERANCIA (0,01) · PASSO (2) · ALTURA (0,22).
- Não apaga nada; se já existir greide/corredor com o mesmo nome, reutiliza. Cada etapa roda numa transação própria.
- Nas curvas usa a superelevação do alinhamento; sem ela, fica −2 %.
- CSV dos PVIs em `Documentos\greide_colado_<alinhamento>.csv`.

---

## Intersecao de vias

### IntersecaoCorredor.py  (colar num nó Python Script)
Para a interseção entre dois alinhamentos: acha o ponto de junção, cria um greide para cada via com PVI na junção na cota informada e rampa constante a partir dela, cria o corredor com duas baselines usando uma assembly existente e a superfície do corredor (links Top).
- 7 entradas: alinhamento da via 1 · via 2 · cota da junção · rampa via 1 (%) · rampa via 2 (%) · nome da assembly · prefixo dos objetos.
- Complementa a LISP `INTERSECAO.lsp` (breaklines / 3DFACES das concordâncias), que fica na raiz.

---

## Rotatoria  (6 nós Python — montar o gráfico)
Não há `.dyn` pronto: são 6 códigos para colar em nós Python e ligar entre si (passo a passo no `LEIA-ME.md` da pasta).
Gera a geometria da rotatória: raios (ilha central, pista anular, borda externa), pontos de cada acesso, círculos e polilinhas das bordas, alinhamento circular do eixo da pista, alinhamentos retos dos ramais e ilhas divisórias triangulares. Layers `ROT_*`.
- Entradas: raio da ilha, largura e nº de faixas, nº de acessos, largura do acesso, centro, ângulo inicial, nome base.

---

## Impressao de secoes PDF  (3 nós Python)
Plota cada moldura A4 (bloco no Model Space) num PDF separado, nomeado e ordenado pela estaca do atributo. Feito para seções transversais / notas de serviço DER-SP.
- `00_INSPECIONAR_desenho.py` — rode primeiro: lista blocos, TAGs de atributo e nomes de papel do *DWG To PDF*.
- `01_GERAR_PDF_secoes.py` — rotina principal (6 entradas: bloco, pasta, papel, TAG da estaca, prefixo, EXECUTAR).
- `02_FALLBACK_gerar_script_scr.py` — plano B: gera um `.scr` com `-PLOT` para rodar no AutoCAD se o PlotEngine travar dentro do Dynamo.
- Passo a passo em `LEIA-ME_montagem_do_grafico.md`. Versão LISP: `IMPSECOES.lsp` / `PDFSECOES.lsp`, na raiz.

---

## _diagnostico API  (ferramentas de desenvolvimento)
Somente leitura: listam classes e métodos da API do Civil 3D para desenvolver as rotinas. Não são de uso no dia a dia.
- `GreideDiag.dyn` — assinaturas da API de perfis (PVIs, curvas verticais, CreateByLayout); resultado no Watch.
- `SondaAPI.dyn` — API de Connected Alignment / curb return → `sonda_api2.txt`.
- `SondaEntidades.dyn` — entidades de perfil + amostra de um greide real (alinhamento "ALINHAMENTO -1") → `entidades.txt`.
- `SondaVinculo.dyn` — procura um mecanismo associativo para amarrar greides → `vinculo.txt`.
- Os `.txt` são gravados na pasta `rotina corredores`.

---

## _versoes antigas

| Arquivo | Substituído por |
|---|---|
| `GreideAuto.dyn` | `Greide automatico\GreideAuto_v6.dyn` — código mais antigo (sem tratamento de estilos e sem tangente máxima), apesar de ter sido salvo por último em 26/09 |
| `GreideAutoB.dyn` | idem — versão intermediária (sem tangente máxima) |
| `Locacao_Corredor_Pontos_1.dyn` | `Pontos COGO\Locacao_Corredor_Pontos.dyn` (v3) |
| `CriarCorredoresEmLote_BACKUP_original.dyn` | `rotina corredores\CriarCorredoresEmLote.dyn` — a original casava alinhamento/greide pelo código no nome ("01 Rua São Miguel greide") |
| `AmarraGreides_rascunho.py` | `rotina corredores\AmarraGreides.dyn` |
| `CriaConcordancias_rascunho.py` | `rotina corredores\CriaConcordancias.dyn` |
| `CriarCorredoresEmLote_pacote.zip` | cópia compactada do CriarCorredoresEmLote (.dyn + .py) |

---

## O que mudou de lugar (30/09/2026)

Nenhum código foi alterado; só mudaram pasta e, em alguns casos, o nome do arquivo.

| Antes (em `D:\Claude ai\`) | Agora (em `Dynamo rotinas\`) |
|---|---|
| `ClassificaPontos_PointGroups_1.dyn` | `Pontos COGO\ClassificaPontos_PointGroups.dyn` |
| `Locacao_Corredor_Pontos.dyn` | `Pontos COGO\Locacao_Corredor_Pontos.dyn` |
| `Claude outputs\GreideAuto_v6.dyn` | `Greide automatico\GreideAuto_v6.dyn` |
| `Claude outputs\greide_dynamo.py` | `Greide automatico\greide_dynamo.py` |
| `Dynamo rotinas\rotina corredores\GreideColado_CorredorRecap.dyn` / `.py` | `Recapeamento greide colado\` |
| `IntersecaoCorredor.py` | `Intersecao de vias\IntersecaoCorredor.py` |
| `Dynamo_Rotatoria\` (pasta inteira) | `Rotatoria\` |
| `Impressão de seções\` (pasta inteira) | `Impressao de secoes PDF\` |
| `GreideDiag.dyn` | `_diagnostico API\GreideDiag.dyn` |
| `Dynamo rotinas\rotina corredores\SondaAPI.dyn`, `SondaEntidades.dyn`, `SondaVinculo.dyn` | `_diagnostico API\` |
| `GreideAuto.dyn`, `GreideAutoB.dyn`, `Locacao_Corredor_Pontos_1.dyn` | `_versoes antigas\` |
| `Dynamo rotinas\rotina corredores\CriarCorredoresEmLote_BACKUP_original.dyn` | `_versoes antigas\` |
| `Dynamo rotinas\rotina corredores\AmarraGreides.py` | `_versoes antigas\AmarraGreides_rascunho.py` |
| `Dynamo rotinas\rotina corredores\CriaConcordancias.py` | `_versoes antigas\CriaConcordancias_rascunho.py` |
| `Dynamo rotinas\rotina corredores\files.zip` | `_versoes antigas\CriarCorredoresEmLote_pacote.zip` |

Se usar o **Dynamo Player**, aponte a pasta dele para a subpasta da rotina (ex.: `Dynamo rotinas\Pontos COGO`).
