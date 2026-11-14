# PROJETA • Assistente Civil — Drenagem, Dispositivos, SWMM/EPANET e Modelador

Plugin próprio para **Civil 3D 2026 (.NET 8) e 2027 (.NET 10)** que reimplementa, com código original, o conjunto de
ferramentas do tipo SOLIDOS: redes de drenagem/esgoto/água, dispositivos longitudinais, integração com
SWMM e EPANET e um modelador paramétrico de dispositivos 3D.

```
Projeta.Civil3D/
├─ src/Projeta.Core/        núcleo de cálculo (sem AutoCAD) — hidrologia, hidráulica, dimensionamento,
│                            modelador, SWMM/EPANET, relatórios. Testável em qualquer máquina.
├─ src/Projeta.Civil3D/     plugin do Civil 3D — comandos PRJ*, desenho, janelas, painel
├─ tests/Projeta.Core.Testes/  36 testes (inclui simulações reais no SWMM e no EPANET)
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
No Civil 3D: **PRJPAINEL** abre o painel lateral com todos os comandos; **PRJAJUDA** lista tudo na linha de comando;
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
| SIMPORTEXCEL | `PRJIMPORTARCSV`, `PRJMODELOCSV`, `PRJEXPORTARCSV` | CSV com `;` (abre direto no Excel) |
| SWMMEXPORT / SWMMIMPORT / SWMMCALC / SWMMPLOT | `PRJSWMMEXPORTAR`, `PRJSWMMIMPORTAR`, `PRJSWMMCALCULAR` | chuva de projeto por blocos alternados; comparação Q racional × Q SWMM |
| EPANETEXPORT / EPANETIMPORT / EPANETCALC | `PRJEPANETEXPORTAR`, `PRJEPANETIMPORTAR`, `PRJEPANETCALCULAR` | demanda em marcha; ajuste automático de diâmetros por velocidade |
| (setores / projeção populacional) | `PRJPOPULACAO` | aritmético, geométrico, taxa decrescente, logístico, taxa arbitrada |
| SBUILDER | `PRJMODELADOR` | definição JSON com expressões, prévia de planta/seção, salva na biblioteca |
| Dispositivos longitudinais (valetas, sarjetas) | `PRJLONGITUDINAL`, `PRJLONGEDITAR`, `PRJLONGQUANT` | sólido por loft ao longo do alinhamento + capacidade e comprimento crítico |
| SOPTIONS / estilos | `PRJOPCOES`, `PRJEQUACOES` | |

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

- 37 testes do núcleo (hidráulica, IDF, dimensionamento de drenagem e esgoto, numeração, interferências, valas, CSV, JSON,
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
