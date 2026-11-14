# PROJETA • Assistente Civil — Drenagem, Dispositivos, SWMM/EPANET e Modelador

Plugin próprio para **Civil 3D 2026 (.NET 8) e 2027 (.NET 10)** que reimplementa, com código original, o conjunto de
ferramentas do tipo SOLIDOS: redes de drenagem/esgoto/água, dispositivos longitudinais, integração com
SWMM e EPANET e um modelador paramétrico de dispositivos 3D.

```
Projeta.Civil3D/
├─ src/Projeta.Core/        núcleo de cálculo (sem AutoCAD) — hidrologia, hidráulica, dimensionamento,
│                            modelador, SWMM/EPANET, relatórios. Testável em qualquer máquina.
├─ src/Projeta.Civil3D/     plugin do Civil 3D — comandos PRJ*, desenho, janelas, painel
├─ tests/Projeta.Core.Testes/  51 testes (inclui simulações reais no SWMM e no EPANET)
├─ biblioteca/              exemplos de dispositivos e seções do usuário (JSON)
├─ exemplos/                CSV de importação, relatório HTML, INPs gerados
└─ instalador/              PackageContents.xml + instalar.ps1 (bundle de autocarga)
```

---

## 1. Compilar e instalar

Pré-requisitos: Civil 3D 2026 e/ou 2027, **.NET 8 SDK** (para o 2026) e **.NET 10 SDK** (para o 2027 — o Civil 3D 2027 roda em .NET 10).

```powershell
# compila para as versões instaladas (2026 e/ou 2027) e instala o bundle de autocarga
powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1

# só uma versão / só compilar (teste com NETLOAD)
powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1 -Versoes 2026
powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1 -SemInstalar
```

Sem o script: `dotnet build src\Projeta.Civil3D -c Release` (2026 → `bin\Release\net8.0-windows`) ou
`dotnet build src\Projeta.Civil3D -c Release -p:CivilVersao=2027` (2027 → `bin\Release\net10.0-windows`);
no Civil 3D, `NETLOAD` na DLL gerada. Se o Civil 3D estiver em outro caminho, acrescente `-p:AcadDir="..."`.

O bundle fica em `%APPDATA%\Autodesk\ApplicationPlugins\Projeta.bundle` (com `Contents\2026` e `Contents\2027`).
No Civil 3D: a aba **PROJETA** da faixa de opções tem um botão para cada comando, num painel por grupo (rede, superfícies, geo e terreno, interseções e rotatórias, terraplenagem, pavimentação...), com um desenho do que o comando faz no ícone (rede, perfil, corte e aterro, rotatória, globo...) e um selo quando cabe (+ criar, × excluir, ↓ importar, ↑ exportar, ✓ conferir, ▶ rodar), e a descrição na dica (a folha de conferência dos ícones sai de `civil3d/testes-sem-tela/icones`, sem abrir o Civil); ela é montada pela mesma lista do painel lateral e volta sozinha quando o espaço de trabalho muda (**PRJABA** recria). Quando o **CTGeo (CivilTools)**, de Marcus Philippe A. P. Pinto, está instalado, a aba e o painel ganham um grupo com os comandos dele (CTGEO, CTSTREET, CTBROWSER, CTEARTH, CTGEOEXPORT, CTKMLABRIR, CTKMLIMPORTAR, CSTKML, CTGEOCOORD, CTGEOSTATUS); os botões só chamam esses comandos, o CTGeo continua sendo instalado e mantido pelo autor. **PRJPAINEL** abre o painel lateral com todos os comandos; **PRJAJUDA** lista tudo na linha de comando;
**PRJDEMO** monta num desenho vazio uma demonstração completa (drenagem com bacias, esgoto, todos os dispositivos e seções) e
funciona como autoteste — compara o volume dos sólidos gerados com o cálculo e relê o projeto gravado no DWG.

Testes do núcleo (qualquer SO): `dotnet run --project tests/Projeta.Core.Testes`.
Para incluir as simulações, defina `PROJETA_RUNSWMM` e `PROJETA_RUNEPANET` com os executáveis dos motores.

---

## 2. Fluxo de trabalho (drenagem)

1. `PRJREDE` → tipo, nome, superfície do terreno e (opcional) alinhamento para estacas. Abre as opções da rede.
2. `PRJLANCAR` → clique os PVs de **montante para jusante**. Clicar sobre um nó existente conecta a ele.
   Opções: `Tipo` (PV, BL, Caixa, Exutório), `Novo` (novo ramal), `Desfazer`.
3. `PRJBACIA` → selecione polilinhas fechadas; escolha o nó de lançamento, o C e (opcional) o talvegue (tc por Kirpich).
4. `PRJCALCULAR` → dimensiona e abre a **planilha dinâmica**. Force diâmetro, declividade ou cotas de fundo
   nas colunas azuis e clique **Recalcular**; os trechos de jusante se ajustam. **Aplicar ao desenho** redesenha.
5. Saídas: `PRJTABELA`, `PRJRELATORIO` (HTML completo), `PRJNOTACAIXAS` (CSV), `PRJPERFIL` (projeção em profile view),
   `PRJESCAVACAO` (valas por faixa de profundidade), `PRJINTERFERENCIA` (cruzamentos entre redes).
6. Mexeu no desenho (moveu PV com MOVE/grip, apagou um tubo, editou a polilinha da bacia)? `PRJATUALIZAR`.
   Rótulos de nós movidos à mão mantêm a posição (acompanham o nó se ele for deslocado).

O projeto inteiro fica **dentro do DWG** (Xrecord no dicionário de objetos nomeados), então acompanha o arquivo e respeita UNDO.

### O que é desenhado
| Elemento | Representação | Layer |
|---|---|---|
| PV / caixa / BL / exutório | bloco próprio por nó com símbolo em planta (na cota da tampa) + sólido 3D do modelador, com furos dos tubos | `PRJ-DRE-PV`, `-BL`, `-CX`, `-EXU`, sólidos em `-3D` |
| Tubo | polilinha em planta + seta de fluxo + sólido 3D (cilindro oco ou galeria, várias linhas) entre as faces internas dos PVs | `-TUBO`, `-SETA`, `-3D` |
| Rótulos | nó: nome, CT, CF, h; tubo: nome, seção, L, i | `-TEXTO` |
| Bacia | rótulo com área, C e tc no centroide da sua polilinha | `-BACIA` |

Prefixo `PRJ-DRE` (drenagem), `PRJ-ESG` (esgoto), `PRJ-AGU` (água), `PRJ-GEN` (genérica).
Congele o layer `-3D` para ficar só com a planta.

---

## 3. Correspondência com o SOLIDOS

| SOLIDOS | PROJETA | Observação |
|---|---|---|
| SNETWORK | `PRJREDE` | drenagem, esgoto, água, genérica |
| (lançamento) | `PRJLANCAR`, `PRJNO`, `PRJTUBO` | lança "sem se preocupar com cotas"; o cálculo define tudo |
| SCATCHMENT | `PRJBACIA` | talvegue opcional (Kirpich) |
| SCALC | `PRJCALCULAR` | planilha editável com recálculo imediato |
| SNUM | `PRJNUMERAR` | por coletor (principal primeiro), prefixos separados para BL |
| SPROPS / SMATCHPROPS | `PRJPROPRIEDADES` | inclui parâmetros do dispositivo |
| SCONNECT / SDISCONNECT / SRECON | `PRJCONECTAR`, `PRJATUALIZAR` | |
| SREVERSE | `PRJINVERTER` | |
| SSPLIT | `PRJDIVIDIR` | |
| SSWAP / SRESET | `PRJTROCAR` | |
| STABLE / SREPORT / SEXPORTTABLE | `PRJTABELA`, `PRJRELATORIO`, exportar CSV nas janelas | |
| SPROJECTION / SPATHPROF | `PRJPERFIL` | |
| SINTERF | `PRJINTERFERENCIA` | folga vertical configurável |
| SCREATESURFACES (valas) | `PRJESCAVACAO` | volumes, faixas de profundidade, berço, reaterro, bota-fora, escoramento |
| SIMPORTGRAVITY | `PRJIMPORTARPIPE` | mantém ou recalcula diâmetros/cotas |
| (volta para o Civil 3D) | `PRJDEVOLVERPIPE` | grava na Pipe Network de origem: diâmetro (tamanho da lista de peças igual ou maior), cotas de fundo dos tubos, tampa e fundo das estruturas |
| SIMPORTEXCEL | `PRJIMPORTARCSV`, `PRJMODELOCSV`, `PRJEXPORTARCSV` | CSV com `;` (abre direto no Excel) |
| SWMMEXPORT / SWMMIMPORT / SWMMCALC / SWMMPLOT | `PRJSWMMEXPORTAR`, `PRJSWMMIMPORTAR`, `PRJSWMMCALCULAR` | chuva de projeto por blocos alternados; comparação Q racional × Q SWMM |
| EPANETEXPORT / EPANETIMPORT / EPANETCALC | `PRJEPANETEXPORTAR`, `PRJEPANETIMPORTAR`, `PRJEPANETCALCULAR` | demanda em marcha; ajuste automático de diâmetros por velocidade |
| (setores / projeção populacional) | `PRJPOPULACAO` | aritmético, geométrico, taxa decrescente, logístico, taxa arbitrada |
| SBUILDER | `PRJMODELADOR` | definição JSON com expressões, prévia de planta/seção, salva na biblioteca |
| Dispositivos longitudinais (valetas, sarjetas) | `PRJLONGITUDINAL`, `PRJLONGEDITAR`, `PRJLONGQUANT` | sólido por loft ao longo do alinhamento + capacidade e comprimento crítico |
| SOPTIONS / estilos | `PRJOPCOES`, `PRJEQUACOES` | |

Ferramenta de eixo: `PRJAPAGARPI` apaga o PI clicado de um alinhamento mesmo quando o Excluir PI do Civil 3D recusa por restrições (curva fixa, linha flutuante, eixo vindo de polilinha). Refaz o eixo no mesmo objeto com tangentes fixas e curvas livres (arco ou espiral-curva-espiral) com os raios de antes; curva vizinha que não cabe na nova tangente é reduzida e relatada. Estilo, rótulos, perfis e corredores continuam ligados ao alinhamento (revise os perfis de projeto, porque as estacas depois do PI mudam).

`PRJAPAGARPIV` faz o mesmo no greide: clique na linha do greide dentro do perfil e perto do PIV; o greide é refeito no mesmo perfil com rampas fixas e curvas verticais livres (parábola, assimétrica ou circular) com o comprimento de antes. Não mexe em perfil de terreno, perfil sobreposto nem em greide que vem de atalho de dados (só leitura). Se um corredor usa o greide, reconstrua o corredor.

Nota de serviço de pavimentação: `PRJNOTASERVICO` lê um corredor e grava um .xlsx no layout E-NS (aba "Nota de Serviço" + aba "Origem"). Uma linha a cada 10 m (configurável), nos pontos notáveis do eixo (PC/PT, TS/SC/CS/ST, PCC/PRC) e do greide (BVC/EVC, PIV sem curva) e no início/fim de cada região. Eixo: estaca (20 m), cota de projeto (greide da baseline), cota do terreno (superfície escolhida) e cota vermelha. Cada lado: afastamento, cota e inclinação dos elementos da seção, achados pelos códigos de ponto do corredor — padrão Bordo Pista=`ETW`, Drenagem=`Flowline_Gutter`, Topo Meio Fio=`Back_Curb`|`Top_Curb`, Passeio=`Sidewalk_Out`; a opção Codigos troca (a configuração fica em `%APPDATA%\Projeta\nota_servico.json`). Terraplenagem (opção Terraplenagem, ligada por padrão): de cada lado, as banquetas (pontos com código `*BERMA*`, `*Ban*` ou `*Bench*`; do eixo para fora, cada par = início e fim, colunas Banqueta 1..n) e o offset (código `*OFFSET*`, `Daylight*`, `Corte*33-*` ou `Aterro*33-*`, o mais externo; sem código, o ponto mais externo sobre o terreno), com C/A (corte/aterro). Os códigos aceitam `*` e `?`. Nota larga sai em A3, duas páginas de largura. Grupos sem nenhum ponto saem da planilha. Estacas que não são seções calculadas do corredor são interpoladas (afastamento e altura sobre o greide) e listadas na aba Origem. Não altera o desenho. Avulso para NETLOAD num Civil 3D já aberto: `avulso\NotaServico`.

Volumes entre superfícies: `PRJVOLUMES` calcula corte e aterro entre a superfície base (terreno ou levantamento anterior) e a de comparação (projeto ou levantamento novo), na sobreposição inteira ou dentro de polígonos fechados (lotes, platôs, trechos). Os volumes são os exatos da superfície de volume TIN do Civil 3D, criada só para o cálculo e desfeita (ou mantida, se pedir). Fatores de corte (empolamento) e de aterro; saldo = corte × fator − aterro × fator (positivo = sobra/bota-fora, negativo = falta/empréstimo). Mapa de cores de corte/aterro por faixa de altura (0,5 / 1 / 2 / 3 m) com legenda na camada `PRJ-VOLUMES-MAPA`, mandado para trás (U desfaz). Excel com abas Volumes, Faixas de altura e Origem. Ferramenta MCP: `civil_volumes_superficies`.

Projeto × obra: `PRJCONFORMIDADE` compara a superfície do levantamento da obra com a de projeto e marca o que está fora da tolerância. Desvio = cota da obra - cota do projeto (positivo = obra acima, falta cortar; negativo = abaixo, falta aterrar), pela superfície de volume TIN do Civil 3D criada só para o cálculo. Tolerância separada para cima e para baixo (padrão ±0,05 m) e faixas além dela (0,10 / 0,20 m, em `%APPDATA%\Projeta\conformidade.json`). Por área (sobreposição inteira ou polígonos): % dentro/acima/abaixo, desvio médio, desvio-padrão, EMQ, máximo e mínimo, volumes acima/abaixo do projeto e situação (conforme com pelo menos 95 % da área dentro). Mapa de cores (vermelho = acima, azul = abaixo, verde = dentro) com legenda e pontos críticos (maior excesso de cada região, com círculo e valor) na camada `PRJ-CONFORMIDADE-MAPA` (U desfaz). Com um alinhamento, controle por estaca: desvio em afastamentos fixos (automáticos pela faixa coberta ou fixados no .json), com as células fora da tolerância coloridas. Excel com abas Resumo, Faixas de desvio, Por estaca, Pontos críticos e Origem. Ferramenta MCP: `civil_projeto_obra`.

Platôs: `PRJPLATO` projeta um platô em cada polígono fechado selecionado. Cota do piso: a que compensa corte e aterro (corte = aterro × fator de homogeneização, padrão 1,25), uma cota fixa ou a elevação da polilinha. O piso cai (padrão 0,5 %) no sentido em que o terreno desce, pelo plano médio do terreno sob o polígono. Fora do polígono, o talude sai do ponto mais próximo da borda (corte 1,5H:1V e aterro 2H:1V por padrão; cone nos cantos convexos) até encontrar o terreno. Cria a superfície de cada platô (pontos do talude, borda do piso e encontro com o terreno como linhas de quebra) e a superfície final `PRJ-PLATOS FINAL` (terreno + platôs); desenha o piso com as cotas dos vértices e a seta do caimento, a crista do corte (vermelho), o pé do aterro (verde) e os tracinhos de talude, nas camadas `PRJ-PLATO-*`. Volumes exatos pela superfície de volume TIN dentro de cada área de terraplenagem, mapa de corte e aterro opcional e Excel com a aba Platôs (cotas, caimento, áreas, alturas dos taludes, crista e pé, volumes, saldo) seguida das abas de volumes. U desfaz o comando inteiro; últimas escolhas em `%APPDATA%\Projeta\platos.json`. Ferramenta MCP: `civil_platos`.

Volumes por seções (cubação): `PRJCUBACAO` (ou `PRJVOLUMES` → opção Seções) monta a nota de cubação a partir de um grupo de linhas de amostragem (sample lines) do Civil 3D. Duas origens: a **lista de materiais do Civil** (áreas e volumes dele, com os fatores e a correção de curva da lista) ou as **superfícies amostradas** (terreno × projeto, por exemplo a superfície do corredor). Na segunda, as áreas de corte e aterro de cada seção saem da integração exata entre as duas linhas, só onde as duas existem: os trechos que o Civil liga entre os pontos da seção definem a linha, então vãos e regiões fora da superfície ficam de fora; superfície não amostrada no grupo pode ser amostrada pelo comando (U desfaz). Volumes pelas áreas médias, com fator do corte e fator de homogeneização do aterro; quando o grupo tem lista de materiais, as áreas são conferidas com as do Civil estaca por estaca. Excel com a aba Cubação (estaca, distância, áreas, volumes, aterro × fator, compensação lateral, corte excedente, aterro a receber, ordenada de Brückner e observação), Conferência Civil e Origem; trecho de estacas opcional. Ferramenta MCP: `civil_volumes_secoes`.

Geo e terreno (grupo do painel com o mesmo nome; tudo em SIRGAS 2000 / UTM, com a projeção calculada pelo próprio PROJETA e conferida contra o PROJ/GDAL a menos de 2 mm):
- `PRJGEORREF` — **Pontos**: clique 2 ou mais pontos do desenho e digite a coordenada real de cada um (lat, long como o Google Maps copia, graus/minutos/segundos ou E N em UTM); mostra o ajuste com escala livre e com escala fixa (resíduos de cada um), move/gira/escala o desenho inteiro ou uma seleção e define o sistema do desenho. **Sistema**: só define o sistema de um desenho que já está em UTM. Nos dois casos grava o código nas configurações do Civil 3D e a localização geográfica (coordenadas de grade), o que liga o mapa de satélite do próprio Civil (GEOMAP → Aérea; GEOMAPIMAGE grava a imagem).
- `PRJRELEVO` — relevo de satélite gratuito (~30 m) na polilinha ou janela escolhida, em dois modelos: **ANADEM v1.0** (padrão na América do Sul; modelo de TERRENO da ANA/UFRGS, o Copernicus com a vegetação removida, ladrilhos MGRS no servidor da ANA) ou **Copernicus DEM GLO-30** (mundial; modelo de SUPERFÍCIE, com árvores e prédios, ladrilhos de 1° na AWS). Lê pela internet só os blocos internos da área (Cloud Optimized GeoTIFF, por faixas HTTP; cache em `%LOCALAPPDATA%\Projeta\relevo`), converte para o UTM do desenho e cria a superfície TIN recortada no contorno. Altitudes derivadas do geoide EGM2008, erro vertical de alguns metros: serve para estudo, traçado e anteprojeto, não substitui levantamento. O Google não permite baixar o relevo nem as imagens dele para o CAD, por isso as fontes são abertas (uso livre com atribuição).
- `PRJKMZ` — exporta para o Google Earth (KMZ/KML): linhas, polilinhas, arcos, splines, alinhamentos, feature lines, pontos COGO, blocos, textos e superfícies (contorno, ou triângulos 3D coloridos por cota). Pastas = camadas, cores = cores do desenho; altitudes no chão do Google Earth ou absolutas (cotas do desenho).
- `PRJKML` — importa marcadores, caminhos, polígonos (com furos), MultiGeometry e trilhas gx:Track de KML/KMZ: pontos viram pontos COGO e o resto polilinhas, em camadas `KML-<pasta>`, com cotas zero, do arquivo ou projetadas numa superfície.
- `PRJVOLRAPIDO` — corte e aterro numa região (polilinhas fechadas ou vértices clicados), com o mapa de cores transparente e a tabela-resumo no desenho (para fatores e Excel use `PRJVOLUMES`).
- `PRJFAIXAS` — colore a superfície por faixas de cota (passo redondo automático ou fixo) ou de declividade (classes da EMBRAPA 0-3-8-20-45-75 % ou limites próprios) usando a análise do próprio Civil, e insere a tabela com a cor, a área (m² e ha) e a % de cada faixa — áreas exatas, com cada triângulo cortado nas cotas-limite.
Ferramentas MCP: `civil_coordenadas` e `civil_relevo_satelite`.

Interseções e rotatórias (grupo do painel com o mesmo nome):
- `PRJINTERSECAO` — interseção em nível (T, cruzamento ou mais braços): clique perto do cruzamento e selecione os eixos. O centro vai para o encontro dos eixos (cruzamento ou ponta) mais perto do clique; os braços saem onde cada eixo cruza o círculo de busca (confirma quantos são e oferece prolongar o eixo que para antes, como na rotatória). Pergunta a meia largura de cada eixo (lembrada pelo nome) e a via principal (as outras param no PARE). Cria: as esquinas (curva de raio R tangente aos bordos dos dois braços vizinhos, com greide casando com os dois; **Parametros**: raio 12 m [PREMISSA], meia largura padrão, caimento, velocidade da via principal 40 km/h [PREMISSA] e as montagens); os bordos das vias (`PRJ-INTERSECAO-BORDO`); o triângulo de visibilidade de cada aproximação com PARE (motorista a 4,4 m do bordo da principal, d = 0,278·V·tg com tg = 7,5 s, AASHTO caso B1 — referência complementar; `PRJ-INTERSECAO-VISIBILIDADE`); as verificações (ângulo entre a principal e cada secundária ≥ 60°, cota dos greides no cruzamento, rampa de cada aproximação ≤ 3 % — AASHTO, referência complementar —, esquinas e visibilidade); as montagens (pista completa e pista só faixas de cada eixo, esquina com passeio à direita e à esquerda); o corredor (**Existente/Novo/Nao**, como na rotatória: a via principal passa só com as faixas entre as esquinas, as outras começam no bordo da principal, pista completa depois das esquinas, esquinas com a faixa até o bordo mais perto) e o relatório em Excel (Resumo, Parâmetros, Braços, Esquinas, Visibilidade, Verificações, Corredor e Pendências). Duas opções no meio do comando: **faixa de conversão à esquerda** na via principal (nas aproximações que têm via secundária à esquerda de quem chega): alargamento simétrico da largura da faixa (3,60 m, DNIT 2024 — Manual de Projeto de Acessos, p. 47), pleno da linha de retenção até o fim do armazenamento (15 m [PREMISSA], 2 veículos sem estudo de tráfego) e da desaceleração, depois o teiper (desaceleração até a parada e teiper pela Tabela 6 do mesmo manual e a V da principal, por analogia [A CONFIRMAR]); o braço do outro lado do T só tem a transição de saída; contorno da faixa em `PRJ-INTERSECAO-CONVERSAO`, bordos alargados como alvo das faixas no corredor, aba Conversão no Excel; e **ilha canalizadora** nas esquinas (**Nao/Todas/Escolher**): pista de conversão à direita com bordo interno de raio 25 m [PREMISSA] e 5,4 m de largura (Tabela 10, 90°, classe B), ilha afastada 0,60 m dos bordos das vias (p. 41), contorno em `PRJ-INTERSECAO-ILHA`, verificação de área (≥ 4,6 m²) e de lados (≥ 3,6 m) pela AASHTO (referência complementar), região "Esquina canalizada" no corredor com a pista de conversão até a ilha e o meio-fio da ilha (o topo da ilha fica fora do corredor), aba Ilhas no Excel. Valores na opção **Parametros** › ajustar a faixa de conversão e a ilha. Em T, o lado da principal sem via transversal ganha meio-fio e passeio contínuos.
- `PRJROTATORIA` — monta a rotatória de uma vez: clique no centro e selecione os eixos dos ramos (não precisam se cruzar; um eixo que atravessa a rotatória vira dois braços) e confirme quantos braços a rotatória tem. Se achar menos, ele diz que eixo ficou de fora e por quê, e oferece prolongar em reta, até o centro, o eixo que para antes do anel apontando para a rotatória (no fim ou no início, sem mudar as estacas do trecho existente; o greide segue a rampa da ponta). Parâmetros na opção **Parametros** (guardados em `%APPDATA%\Projeta\rotatoria.json`; os de partida saem marcados [PREMISSA] no relatório, com a referência de cada um): DCI (40 m), pista anelar (6 m), faixa transponível (0), largura de entrada e de saída (5 m, da ilha ao meio-fio), ilha divisória (3,0 m junto do anel, 30,5 m de comprimento, 0,6 m no nariz, recuo de 1 m do anel; 0 = sem ilha), meia largura normal dos ramos (3,5 m; o alargamento até os bordos da rotatória sai em taper 1:10), raio de entrada (15 m) e de saída (20 m), caimento (-2 %) e a cota do anel (**Greides** dos ramos, **Terreno** ou **Fixa**). Cria, num comando só (U desfaz tudo, os eixos dos ramos não são alterados):
  - o eixo do anel (círculo anti-horário, mão direita, no meio da pista, começando no meio do maior vão entre os braços) e o greide dele, com um PIV em cada braço e parábola, a cota do PIV corrigida para a curva passar exatamente na cota do ramo;
  - as conexões de entrada (lado anti-horário do braço) e de saída (lado horário), meios-fios tangentes ao bordo externo do anel e ao bordo do ramo (ilha + faixa), cada uma com o greide casando com os dois;
  - o desenho: bordo externo, ilha central e faixa transponível (`PRJ-ROTATORIA`), ilhas divisórias (`PRJ-ROTATORIA-ILHA`), bordos dos ramos com o alargamento (`PRJ-ROTATORIA-BORDO`) e linhas de retenção no bordo do anel (`PRJ-ROTATORIA-RETENCAO`);
  - a **trajetória mais rápida** de cada braço (NCHRP 672): direto (R1 na entrada, R2 em volta da ilha, R3 na saída), à esquerda (R4) e à direita (R5), desenhadas em `PRJ-ROTATORIA-TRAJETORIA` (vermelho, azul, verde) com o raio e a velocidade rotulados. A trajetória é a curva mais suave que passa a 1,5 m de meios-fios, ilhas e do eixo da via, começando e terminando a 50 m das linhas de retenção (menor energia de flexão, do grosso para o fino); raio pelo arco de 3 pontos de 10 m; V = 8,7602·R^0,3861 (e = +0,02: R1, R3, R5) e V = 8,6164·R^0,3673 (e = −0,02: R2, R4). Trajetória que não cabe (conexões sobrepostas, meio-fio invadindo o ramo vizinho) é apontada como reprovada em vez de dar um raio sem sentido;
  - a tabela de **verificações**, cada uma APROVADO / REPROVADO / ATENÇÃO com a fonte: V1 ≤ 40 km/h, |V1 − V2| e |V1 − V4| ≤ 20 km/h, R3 ≥ R1, folga entre conexões vizinhas, largura de entrada (4,9 a 6,1 m), refúgio na ilha (≥ 1,8 m), comprimento da ilha (≥ 30,5 m), eixo do ramo radial ou à esquerda do centro, pista anelar ≥ entrada. Os critérios são referências complementares (NCHRP 672, TxDOT, FDOT, MaineDOT): a norma do órgão prevalece;
  - a **visibilidade à esquerda** em cada entrada (triângulo de visibilidade, NCHRP 672 6.7.3): observador a 15 m da linha de retenção, d = 0,278·tc·V com tc = 5 s; d1 pela aproximação do braço de montante (veículo entrante, média de V1 e V2 dele) e d2 pela pista anelar (veículo circulante, velocidade de R4 do braço de montante). As visadas e as pernas saem em `PRJ-ROTATORIA-VISIBILIDADE` (magenta = circulante, ciano = entrante) e a verificação dá ATENÇÃO quando a visada atravessa a ilha central, uma ilha divisória ou passa por fora da pista: o que houver ali (paisagismo, poste, placa, muro) tem de ficar abaixo da linha de visão;
  - as **montagens do corredor** (opção em Parametros, ligada por padrão), com subassemblies padrão do Civil, lado a lado abaixo da rotatória (camada `PRJ-ROTATORIA-MONTAGENS` com os nomes): anel com meio-fio e passeio, anel só pista, pista completa, ramo junto do anel (só pista), ramo com ilha divisória, meia pista, meia pista com meio-fio e passeio e conexão (meio-fio no eixo) — as três últimas à direita e à esquerda. Faixa com revestimento, base e sub-base (LaneSuperelevationAOR com caimento fixo), meio-fio com sarjeta (BasicCurbAndGutter), passeio (UrbanSidewalk), talude até o terreno (DaylightGeneral), meio-fio da ilha central (BasicCurb) e ilha divisória elevada. Valores em Parametros (revestimento 0,05, base 0,15, sub-base 0,20, meio-fio 0,15, sarjeta 0,30, passeio 2,0 m, talude 1:1 no corte e 1,5:1 no aterro) [PREMISSA]; dá para trocar depois nas propriedades de cada peça. O bordo externo e a ilha central saem como polilinhas para servir de alvo. A aba **Corredor** do relatório (e a linha de comando) diz que montagem vai em cada trecho de cada eixo: no anel, só pista nas aberturas dos braços e com meio-fio entre elas; nos ramos, só pista até as tangências das conexões, depois ilha divisória e pista completa; nas conexões, de que lado fica o passeio. No fim pergunta **Colocar no corredor [Existente/Novo/Nao]**: em um corredor existente (ou novo) cria as baselines do anel, das conexões e dos ramos (com o greide de projeto de cada um), as regiões com as montagens em cada trecho, os alvos de largura (bordos dos ramos, bordo externo do anel, eixos das conexões), o talude na superfície do terreno e a frequência de 1 m nas curvas; onde uma baseline que já existia passa pela rotatória, a região dela é encurtada, dividida ou retirada só nesse trecho (a linha de comando diz o que mudou) e o corredor é reconstruído;
  - o **relatório em Excel** (abas Resumo, Parâmetros, Braços, Trajetórias, Visibilidade, Corredor, Verificações e Pendências — giro do veículo de projeto, visibilidade de parada na aproximação, sinalização, corredor e drenagem ficam como pendências).
- `PRJCASAMENTO` — clique em dois alinhamentos (ou **Varios** para selecionar o anel, os ramos e as conexões de uma vez). Acha cada encontro: a ponta de um sobre o outro (até 5 m, configurável em **Tolerancias**), ponta com ponta e cruzamentos; eixo fechado (anel) é tratado na emenda. Em cada encontro diz: se a ponta encosta no eixo ou quanto falta (tolerância 5 mm; ponta que corre tangente ao outro eixo a mais de 0,30 m é ligação no bordo; ponta oblíqua a mais de 1,5 m é só um eixo passando perto e não conta), se é tangente (quebra entre 0,01° e 15° é apontada) e, com os greides de projeto dos dois, a diferença de cota (tolerância 1 cm; no bordo soma o caimento transversal, padrão -2 %) e a de rampa na direção do outro (0,5 %; só em encontros que não são quase perpendiculares). Marca verde (casa), vermelho (não casa) ou amarelo (sem greide) na camada `PRJ-CASAMENTO` e oferece acertar a cota do PIV da ponta que não casa. Encontros dentro do anel de uma rotatória (eixos de ramos que seguem até o centro, na ilha central) não contam. Alinhamento com mais de um greide: pergunta qual é o de projeto (lembra na sessão).
- `PRJCONECTAREIXO` — clique num alinhamento perto do canto e no outro: cria o alinhamento de conexão (arco de raio R tangente aos dois, nos eixos ou nos bordos), em laço para fazer todos os cantos seguidos. **Bordos**: distância do eixo ao bordo onde a conexão encosta em cada alinhamento (lembrada pelo nome do alinhamento, em `%APPDATA%\Projeta\rotatoria.json`) e o caimento transversal. Tangências por Newton sobre o próprio alinhamento do Civil (tangentes, curvas e espirais). Quando os dois têm greide, cria o greide da conexão: sai com a cota e a rampa do primeiro no ponto de tangência (no bordo, com o caimento) e chega com as do segundo - um PIV no cruzamento das tangentes com a parábola que cabe, ou dois PIVs quando elas não se cruzam no trecho.

Não reimplementado nesta versão: exportação IFC, referências externas entre desenhos (há `PRJEXPORTARREDE`/`PRJIMPORTARREDE` em JSON),
divisores de vazão, editor de regras em VB, controles/LID/qualidade da água do SWMM (o INP gerado pode ser completado no próprio SWMM).

---

## 4. Critérios de cálculo

**Drenagem — método racional.** Q = C·i·A/360 (m³/s; i em mm/h; A em ha) com ΣC·A acumulado e tc = maior entre o tc das bacias
que chegam ao nó e o tc de montante + tempo de percurso. Para cada trecho testa as seções em ordem crescente (diâmetros comerciais, depois galerias
se permitido), nunca menores que as de montante, e escolhe a primeira que atende:
recobrimento mínimo, declividade mínima/máxima, lâmina máxima y/D, velocidade máxima; tenta corrigir a velocidade mínima aumentando a declividade.
A cota de montante respeita o critério de concordância (geratriz superior, inferior ou lâmina d'água) e o degrau mínimo;
quando a declividade é limitada pela máxima, o tubo desce no PV de montante (degrau) para manter o recobrimento a jusante.

**Esgoto — NBR 9649.** Contribuição em marcha: Qi = C·K2·q·Pi/86400 e Qf = C·K1·K2·q·Pf/86400 distribuídas pela extensão
contribuinte, mais infiltração; vazão mínima de 1,5 L/s; Imín = 0,0055·Qi^-0,47; Imáx = 4,65·Qf^-0,67; y/D ≤ 0,75
(≤ 0,50 se v > vc = 6·√(g·Rh)); tensão trativa σ = γ·Rh·I verificada com a vazão inicial; aviso de tubo de queda acima do degrau configurado.

**Água — EPANET.** Demanda total K1·K2·q·P/86400 distribuída pela metade para cada extremidade dos tubos (proporcional ao comprimento),
mais demandas pontuais. Com "Dimensionar por velocidade", o EPANET roda em ciclos aumentando o diâmetro dos tubos acima da velocidade máxima.

**Equações de chuva incluídas (com fonte).** Confira a validade para o seu local; outras podem ser cadastradas em `PRJEQUACOES`
(forma i = K·T^a/(t+b)^c ou qualquer fórmula com T e t).

| Id | Local | Equação | Fonte |
|---|---|---|---|
| BH-UFU2024 | Belo Horizonte/MG | i = 1076,16·T^0,114/(t+9,783)^0,724 | *Atualização de modelos de chuva intensa para municípios do Sudeste e Centro-Oeste do Brasil*, Caminhos de Geografia (UFU) |
| BSB-UFU2024 | Brasília/DF | i = 816,70·T^0,081/(t+8,990)^0,704 | idem |
| LAV-UFU2024 | Lavras/MG | i = 815,55·T^0,137/(t+9,782)^0,724 | idem |
| RMBH-PINHEIRO | RMBH (regional) | Pinheiro (1997), com α e β* interpolados por duração; **ajuste PA** (precipitação anual) | Relatório IDF de Sarzedo/MG — SGB/CPRM (2022) |

---

## 5. Motores SWMM e EPANET

Os comandos `PRJSWMMCALCULAR` e `PRJEPANETCALCULAR` executam os motores oficiais em linha de comando
(`runswmm.exe` e `runepanet.exe`). Na primeira vez o plugin pede o caminho; normalmente estão na pasta de instalação do
EPA SWMM 5.2 e do EPANET 2.2. Também podem ser compilados a partir do código aberto
(github.com/USEPA/Stormwater-Management-Model e github.com/OpenWaterAnalytics/EPANET). `PRJMOTORES` troca os caminhos.

Os INPs gerados foram validados rodando os motores reais (ver `exemplos/`).

## Verificação feita

- 51 testes do núcleo (eixo/apagar PI e PIV, hidráulica, IDF, dimensionamento de drenagem e esgoto, numeração, interferências, valas, CSV, JSON,
  modelador, seções) e simulações reais no SWMM 5.2 e no EPANET 2.3 com os INPs gerados.
- Compilação contra as DLLs reais do Civil 3D 2026 (.NET 8) e 2027 (.NET 10), sem erros nem avisos.
- `PRJDEMO` executado no console do Civil 3D 2026 e 2027 (accoreconsole /product C3D): rede dimensionada e desenhada,
  sólidos de todos os dispositivos e seções com volume igual ao calculado, projeto relido do DWG — sem falhas.
  O desenho resultante e a planta em PDF estão em `exemplos/`.

---

## 6. Modelador (dispositivos e seções)

Definições em JSON; **qualquer campo numérico aceita expressão** usando parâmetros e variáveis.
Biblioteca padrão embutida + pasta do usuário `%APPDATA%\Projeta\biblioteca\dispositivos` e `...\secoes`
(mesmo `id` substitui o padrão). `PRJMODELADOR` edita, avalia (volume e quantitativos), mostra a planta/seção e salva.

### Dispositivo (PV, caixa, boca de lobo, ala…)
```jsonc
{
  "id": "MEU_PV", "nome": "...", "categoria": "PV",   // PV | BocaDeLobo | Caixa | Exutorio | Generico
  "alinharComTubo": false,          // gira o bloco na direção do tubo de saída (ou de chegada no exutório)
  "raioFuros": "Di/2",              // distância do centro à face interna — onde começam os furos dos tubos
  "parametros": [
    { "nome": "Profundidade", "padrao": 1.5, "origem": "rede" },     // preenchidos pela rede:
    { "nome": "DiametroMaximo", "padrao": 0.6, "origem": "rede" },   // Profundidade, DiametroMaximo, CotaFundo, CotaTampa
    { "nome": "Di", "padrao": "se(DiametroMaximo<=0.6, 1.0, 1.2)" }  // padrão pode ser expressão
  ],
  "variaveis": [ { "nome": "De", "expr": "Di + 0.2" } ],
  "solidos": [   // origem no FUNDO do dispositivo, Z para cima
    { "id": "ext", "op": "cilindro", "raio": "De/2", "altura": "Profundidade" },
    { "id": "int", "op": "cilindro", "raio": "Di/2", "altura": "Profundidade" },
    { "id": "pv",  "op": "subtrair", "de": "ext", "com": ["int"] }
  ],
  "saida": ["pv"],
  "planta": [ { "tipo": "circulo", "raio": "De/2" } ],
  "quantitativos": [ { "nome": "Concreto", "unidade": "m³", "expr": "volume('pv')" } ]
}
```
Operações: `caixa` (comprimento X, largura Y, altura Z; base = centro da face inferior), `cilindro`, `tronco` (raio/raioTopo, eixo opcional),
`esfera`, `extrusao` (perfil 2D, plano XY/XZ/YZ), `revolucao` (perfil x=raio, y=z, em torno de Z), `uniao`, `subtrair`, `intersecao`,
`mover`, `rotacionar`, `copiar`. Cada operação pode ter `"condicao"` (só é criada se verdadeira).
Planta: `circulo`, `retangulo`, `linha`/`polilinha`, `arco`, `texto` (com `camada`: contorno, tampa, eixo).

### Seção-tipo (valeta, sarjeta, meio-fio, canaleta…)
`contornoHidraulico` (borda esquerda → fundo → borda direita), `concreto` (polígono fechado) e `escavacao` (opcional).
Pontos `{ "x": "...", "y": "..." }` ou arcos `{ "arco": true, "x": cx, "y": cy, "raio": r, "de": 180, "ate": 360, "segmentos": 16 }`.
Origem = ponto de referência no eixo do dispositivo (cota do perfil/superfície + desnível); X cresce para a direita do alinhamento.

### Expressões
`+ - * / ^ %`, comparações, `&& || !`, `cond ? a : b`, `se(c,a,b)`, `min`, `max`, `abs`, `sqrt`, `pow`, `exp`, `ln`, `log10`,
`sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `atan2` (radianos), `rad()`, `deg()`, `round(x,n)`, `floor`, `ceil`, `clamp(x,a,b)`,
`interp(x, x1,y1, x2,y2, ...)`, constantes `pi`, `e`, `g`. Nos quantitativos: `volume('id')`.

---

## 7. Importação por planilha (CSV)

Separador `;` e vírgula decimal (padrão do Excel brasileiro). `PRJMODELOCSV` gera o modelo:
```
PV;PV-01;612000,00;7801000,00;850,00;PV_CIRCULAR
BL;BL-01;612005,00;7800995,00;849,90
EXUTORIO;EX-01;612150,00;7801020,00;847,20;845,00      (último campo = cota de lançamento)
TRECHO;T-01;PV-01;PV-02;[diâmetro m];[declividade m/m];[CF mont];[CF jus]
BACIA;B-01;BL-01;0,80;0,70;[tc min]
```

---

## 8. Observações

- Unidades do desenho em **metros**.
- Os dados ficam no DWG; a biblioteca de dispositivos e as configurações (caminhos dos motores) ficam em `%APPDATA%\Projeta`.
- Os volumes do modelador mostrados na janela são estimativas (primitivas exatas; booleanas supõem subtraendos contidos);
  no desenho, os sólidos 3D são exatos (propriedades do sólido).
- Os critérios padrão (velocidades, lâminas, recobrimentos, diâmetros) são editáveis em `PRJOPCOES` — ajuste às normas do órgão contratante.

---

## 9. Claude (MCP) — operar o Civil 3D conversando

O PROJETA expõe suas rotinas ao **Claude Desktop** pelo Model Context Protocol. Ao contrário dos servidores MCP genéricos
(em que a IA escreve código e executa dentro do Civil 3D), aqui o Claude só chama **ferramentas registradas do próprio PROJETA** —
o mesmo código dos comandos PRJ*, já testado.

```
Claude Desktop ──stdio──► Projeta.Mcp.Servidor.exe ──TCP 127.0.0.1 + token──► plugin (PRJMCP) ──► thread principal do Civil 3D
```

```
src/Projeta.Mcp/            protocolo MCP, registro/esquema das ferramentas e a ponte local (sem AutoCAD, testável)
src/Projeta.Mcp.Servidor/   executável que o Claude Desktop inicia
src/Projeta.Civil3D/Mcp/    lado do Civil 3D: PRJMCP, PRJMCPTESTE, despacho na thread principal e as ferramentas
tests/Projeta.Mcp.Testes/   testes da ponte e do protocolo (dotnet run)
```

### Instalação
1. `instalador\instalar.ps1` — plugin com a ponte (com o Civil 3D fechado).
2. `instalador\instalar-mcp.ps1` — publica o servidor em `%LOCALAPPDATA%\Projeta\mcp\servidor` e inclui `projeta-civil3d`
   no `claude_desktop_config.json` (com backup). `-Remover` desfaz.
3. No Civil 3D: `PRJMCP` → **Iniciar** (ou **Automatico** para ligar sempre). Reinicie o Claude Desktop.

### Ferramentas
| Ferramenta | O que faz | Altera o DWG |
|---|---|---|
| `projeta_conexao` | Civil 3D conectado? desenho ativo, versão | — |
| `desenho_status` | DWG, unidades, coordenadas, contagem de objetos e redes | — |
| `civil_listar_objetos` | superfícies, alinhamentos (perfis, grupos de seções), corredores, redes de tubulação, grupos de pontos | — |
| `civil_cota_terreno` | cota da superfície por XY ou por estaca/afastamento | — |
| `civil_perfil_terreno` | amostra o terreno ao longo do alinhamento (cotas e rampas) | — |
| `civil_criar_perfil_terreno` | cria perfil de terreno no alinhamento | sim |
| `civil_reconstruir_corredores` | Rebuild dos corredores | sim |
| `civil_volumes_superficies` | Corte e aterro entre duas superfícies (exato, por polígono), saldo com fatores, faixas de altura, .xlsx; `mapa` desenha o mapa de cores | só com `mapa`/`manter_superficie` |
| `civil_projeto_obra` | Projeto × obra: desvios da obra em relação ao projeto, % fora da tolerância, estatísticas, pontos críticos, controle por estaca, .xlsx; `mapa` desenha o mapa de desvios | só com `mapa` |
| `civil_platos` | Platôs em polígonos fechados: cota que compensa corte e aterro (ou fixa / da polilinha), caimento, taludes até o terreno, superfícies, crista, pé e tracinhos, volumes exatos, .xlsx | sim |
| `civil_volumes_secoes` | Nota de cubação pelo grupo de seções: lista de materiais do Civil ou áreas entre terreno e projeto amostrados, compensação lateral, Brückner, conferência com o Civil, .xlsx | só com `amostrar` |
| `civil_coordenadas` | Sistema de coordenadas do desenho e conversão de pontos entre lat/long e UTM (SIRGAS 2000), com fator de escala e convergência | — |
| `civil_relevo_satelite` | Superfície TIN com o relevo ANADEM (terreno) ou Copernicus (superfície) de 30 m de uma polilinha ou retângulo, no UTM do desenho | sim |
| `civil_nota_servico` | Nota de serviço do corredor (.xlsx layout E-NS); `somente_codigos` lista os códigos de ponto | só arquivo |
| `projeta_listar_redes` | redes do PROJETA e estado do último cálculo | — |
| `projeta_detalhar_rede` | dimensionamento, nós, bacias, materiais, vala, forçados, parâmetros, mensagens | — |
| `projeta_calcular_rede` | = PRJATUALIZAR (sincroniza, amostra terreno, dimensiona, redesenha) | sim |
| `projeta_alterar_parametros` | muda critérios (TR, recobrimento, velocidades, diâmetros...) e recalcula | sim |
| `projeta_editar_elementos` | força/libera diâmetro, declividade, cotas em trechos e nós e recalcula | sim |
| `projeta_relatorio` | relatório HTML ou CSV em arquivo | só arquivo |

### Segurança
- Só escuta em `127.0.0.1`; porta e token novos a cada início (`%LOCALAPPDATA%\Projeta\mcp\ponte.json`, só o seu usuário lê).
- Recusa chamadas com comando em andamento ou caixa de diálogo aberta — nunca interfere no que você está fazendo.
- Cada chamada que altera o desenho trava o documento com o nome `PRJMCP_<FERRAMENTA>` → **um passo de UNDO** (desfaz com `U`).
- Toda chamada fica registrada na linha de comando: `[PROJETA MCP] ...`.

### Diagnóstico
- `%LOCALAPPDATA%\Projeta\mcp\servidor\Projeta.Mcp.Servidor.exe --status` (ou `--ferramentas`, `--chamar projeta_listar_redes {}`).
- `PRJMCPTESTE` no Civil 3D roda todas as ferramentas no desenho aberto (modo Completo altera o DWG — use uma cópia);
  o resultado vai para `%TEMP%\projeta-mcp-teste.json`.
- `dotnet run --project tests/Projeta.Mcp.Testes` — testes da ponte e do protocolo (qualquer máquina).

### Nova ferramenta
Veja `src/Projeta.Civil3D/Mcp/RegistroProjeta.cs`: um método `Argumentos → ResultadoFerramenta` registrado com
`FerramentaDelegada`. Ele já roda na thread principal com o documento travado; lance `ErroFerramenta` para erros esperados.
A descrição da ferramenta é o manual que o Claude lê — diga quando usar, as unidades e o que volta.
