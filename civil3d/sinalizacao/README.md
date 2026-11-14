# PROJETA • Sinalização Viária

Este é um plugin para **Civil 3D 2026 e 2027** de projeto de sinalização viária. Ele cobre:

- sinalização horizontal;
- sinalização vertical;
- semafórica, obras e defensas;
- quantitativos em Excel;
- modelo 3D (BIM).

Também abre no AutoCAD puro, mas sem os recursos de alinhamento e de superfície.

O código foi escrito do zero, inspirado no conjunto de funções do sinC (Seabra Soluções). Do sinC só foram usados os nomes do menu e os cabeçalhos das planilhas, como referência do que cada ferramenta faz. Nenhum código, bloco, imagem ou planilha dele foi aproveitado.

As placas são desenhadas pelo próprio plugin a partir dos manuais do CONTRAN, com forma, cores, orlas e pictograma.

---

## 1. Instalar

Feche o Civil 3D e rode o PowerShell na pasta do projeto:

```powershell
powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1
```

- O script compila para cada versão encontrada: 2026 em .NET 8 e 2027 em .NET 10.
- Depois cria o bundle `%APPDATA%\Autodesk\ApplicationPlugins\ProjetaSinalizacao.bundle`, que carrega sozinho ao abrir o Civil 3D.
- Para só compilar, sem instalar, use `-SemInstalar`. Para testar sem instalar, use `NETLOAD` na DLL de `src\Projeta.Sinalizacao\bin\Release\net8.0-windows` (2026) ou `net10.0-windows` (2027).

No Civil 3D, use estes comandos:

- **`SINPAINEL`** abre o painel com todos os comandos.
- **`SINAJUDA`** mostra a lista de comandos.
- **`SINDEMO`** monta, num desenho vazio, um projeto de exemplo completo e confere os resultados.

---

## 2. Comandos

### Projeto
| Comando | O que faz |
|---|---|
| `SINPADROES` / `-SINPADROES` | Define os padrões do projeto e os grava no DWG, com a opção de usar como padrão para novos desenhos. Inclui escala de plotagem, tamanho das placas e textos no papel, tipo de via (urbana, estrada, rodovia ou área protegida), velocidade, estaqueamento (20 m ou km), situação padrão e materiais (chapa, película, suporte e tinta). |
| `SINNORMAS` | Abre o `normas.json` com larguras, cadências, tamanhos, espaçamentos e alturas. Depois de editar, rode `SINCORRIGE`. |

### Sinalização horizontal (vol. IV)
| Comando | O que faz |
|---|---|
| `SINVIA` | Lança a via completa a partir do eixo: linha central (LFO-1 a LFO-4), divisão de faixas (LMS-2) e bordos (LBO). Aceita sentido duplo ou único e n faixas. |
| `SINFAIXA` | Lança uma marca longitudinal ao longo de uma polilinha, linha, arco ou alinhamento, com um ou vários deslocamentos. Tipos: LFO-1/2/3/4, LMS-1/2, LBO, LCO, MFE, MFE-CF, MFP, MFR, MCI, LCA e LPP. A largura e a cadência saem da velocidade e podem ser alteradas. |
| `SINTRANSV` | LRE, LDP, FTP-1 (zebrada), FTP-2 (paralela), LRV (espaçamento decrescente para v₁ → v₂) e MCC. |
| `SINZEBRADO` | ZPA: listras a 45° do fluxo recortadas pelo contorno, com linha de contorno opcional, em branco ou amarelo. |
| `SINSETA` | Setas PEM (siga em frente, vire, siga/vire, retorno, vire à esquerda e à direita) e MOF. O comprimento é de 5,00 m ou 7,50 m conforme a velocidade. |
| `SINLEGENDA` | Legendas no pavimento (PARE, ESCOLA, SÓ\|ÔNIBUS…). As letras são alongadas e a área pintada é calculada. |
| `SINSIMBOLO` | SIP (dê a preferência), bicicleta, deficiente e saúde. |
| `SINTACHAS` | Tachas e tachões mono ou bidirecionais ao longo de uma linha, com espaçamento menor em curva. |

### Sinalização vertical (vol. I, II e III)
| Comando | O que faz |
|---|---|
| `SINPLACA` | Abre o catálogo com busca e prévia: 51 placas de regulamentação (R-1 a R-40) e 69 de advertência (A-1a a A-48). Você define o tamanho pelas opções do CONTRAN e o valor (velocidade, altura, peso…). A advertência pode ir com fundo laranja (obra). Várias placas podem ir no mesmo suporte. Escolha o suporte e a situação e clique a posição e de onde vem o tráfego. |
| `-SINPLACA` | Faz o mesmo pela linha de comando, por exemplo `R-19+R-6a`. |
| `SINPLACAPRO` | Modelador de placas retangulares (indicação, educativas, serviços e obras). Você define fundo, orla, linhas de texto e setas. O tamanho sai da altura de letra e é arredondado a 0,05 m. Mostra prévia e área, e salva os modelos na biblioteca (`%APPDATA%\Projeta\Sinalizacao\placas`). |
| `SINPLACAADD` | Acrescenta uma placa a um conjunto já inserido. |
| `SINSUPORTE` | Troca o suporte: coluna simples, dupla ou tripla, braço projetado simples ou duplo, semipórtico simples ou duplo, pórtico ou fixação em estrutura existente. |
| `SINIMPORTAR` / `SINMODELOCADASTRO` | Importa um cadastro de campo em CSV ou XLSX. As colunas são `COD; SUP; X; Y` e, opcionalmente, `ROT; SIT; DIM; VALOR; ID`. |

### Semafórica, obras e defensas (vol. V e VII)
| Comando | O que faz |
|---|---|
| `SINSEMAFORO` | Grupos focais veiculares (200/300 mm), de pedestres e de ciclistas, botoeira, coluna simples, coluna com braço projetado, controlador e caixa de passagem. |
| `SINREDE` | Rede semafórica subterrânea ou aérea, em metros. |
| `SINOBRA` / `SINOBRALINHA` | Cone, tambor, cavalete, barreira plástica, cilindro, balizador, tela ou pisca, um a um ou a cada N metros. |
| `SINOBRAZONA` | Interdição de faixa. Você clica o início e o fim da área de trabalho e o plugin calcula e desenha: as 3 placas de pré-sinalização (A-24, R-19 reduzida, A-21b/c), o taper, a zona de proteção, os dispositivos e o taper de saída. Avisa se o caminho for curto. |
| `SINDEFENSA` | Defensa semimaleável simples ou dupla, rígida ou barreira de concreto. Terminais abatidos ou ancorados, com postes, lâminas e terminais contados. |

### Projeto pronto
| Comando | O que faz |
|---|---|
| `SINCONSULTA` | Mostra os dados e as quantidades de um objeto e abre a edição. Clicar no sólido 3D também funciona. |
| `SINSITUACAO` | Marca os objetos como Implantar, Manter (existente), Remover ou Recuperar. Em planta, "remover" sai em vermelho ou com X, e "existente" sai em cinza. |
| `SINCORRIGE` | Regenera tudo com os padrões e normas atuais e acompanha o que foi movido ou girado com MOVE/ROTATE. Também corrige IDs repetidos por COPY. |
| `SINLOCALIZAR` | Localiza um objeto por ID ou código e dá zoom nele. |
| `SINNUMERAR` | Renumera os IDs (PL-001, ML-001…) pela ordem das estacas. |
| `SINQUANT` | Gera os quantitativos em Excel. Veja o item 4. |
| `SIN3D` / `SIN3DAPAGAR` | Gera o modelo 3D com placas e suportes, semafórica, dispositivos de obra, defensas e barreiras, na cota da superfície TIN que você escolher. O comando `SIN3DAPAGAR` apaga esse modelo. |
| `SINDEMO` | Monta a demonstração e confere os resultados (autoteste). |

---

## 3. Como os objetos funcionam

- Cada placa, marca ou dispositivo é **uma referência de bloco anônimo**, então um clique seleciona o conjunto todo.
- Os dados ficam no **dicionário de extensão** da referência: código, situação, parâmetros e o caminho de origem. Por isso o desenho pode ser regenerado sem depender da polilinha original.
- MOVE e ROTATE são acompanhados: o próximo `SINCORRIGE` ou `SINCONSULTA` transforma as coordenadas guardadas.
- As placas são desenhadas no tamanho real e ampliadas para a escala de plotagem. A placa de 0,50 m aparece com o tamanho definido em *Placa no papel (mm)*.
- A face da placa fica sempre em pé. Um traço no marcador do suporte mostra para onde ela está voltada.
- Layers:
  - `SIN-V-PLACAS`;
  - `SIN-H-<código>` (`SIN-H-LFO-2`…), `SIN-H-TRANSVERSAIS`, `SIN-H-INSCRICOES` e `SIN-H-TACHAS`;
  - `SIN-S-SEMAFORICA` e `SIN-S-REDE`;
  - `SIN-O-OBRAS`;
  - `SIN-D-DEFENSAS`;
  - `SIN-TEXTO` (rótulos);
  - `SIN-3D-*`.

---

## 4. Quantitativos (`SINQUANT`)

A pasta `.xlsx` tem estas abas:

1. **Resumo**:
   - placas em m² por família, chapa, película e situação;
   - suportes em m por tipo e material;
   - pintura em m² por cor e material;
   - extensão por tipo de marca;
   - tachas;
   - semafórica (un e m de rede);
   - obras;
   - defensas (m, postes, terminais).
2. **Placas**: uma linha por placa, com dimensão, área, chapa, película, suporte, eixo, estaca, lado, afastamento e N/E.
3. **Suportes**: colunas e comprimento de cada coluna, viga ou braço, e total.
4. **Marcas longitudinais**: cor, largura, cadência, extensão, comprimento pintado, área, estaca inicial e final, e lado.
5. **Transversais e inscrições**, **Tachas**, **Semafórica**, **Obras** e **Defensas**.

O comprimento do suporte é a altura livre (2,10 m urbana; 1,20 m rural; 4,60/5,50 m suspensa) + a altura das placas + 0,10 m + o engastamento.

A **estaca e o lado** saem do alinhamento do Civil 3D. Você pode escolher um alinhamento ou deixar no automático, que usa o mais próximo de cada objeto. **LD** é à direita do sentido do estaqueamento e **LE** à esquerda.

---

## 5. Normas e fontes

Os valores ficam em `normas.json` e podem ser alterados. Os valores padrão:

**Conferidos no Manual Brasileiro de Sinalização de Trânsito (CONTRAN):**
- Vol. I (Regulamentação):
  - circular Ø 0,40 / 0,50 / 0,75 / 0,30 m (urbana / estrada / rodovia / área protegida), com orla e tarja de 10% do diâmetro;
  - R-1 com lado de 0,25 / 0,35 / 0,40 / 0,18 m, orla interna de 8% e externa de 4%;
  - R-2 com lado de 0,75 / 0,75 / 0,90 / 0,40 m;
  - altura livre de 2,0 a 2,5 m (urbana) e 1,2 m (rural);
  - placas suspensas a 4,6 e 5,5 m;
  - afastamento lateral de 0,30 m (urbana) e 1,20 m (rural).
  - O plugin adota Ø 0,50 m na via urbana, acima do mínimo de 0,40 m.
- Vol. IV (Horizontal):
  - LFO/LMS/LBO com 0,10 m (v < 80) e 0,15 m (v ≥ 80);
  - cadências seccionadas de 2/4 (v < 60), 3/6 (60 a 80) e 4/12 (≥ 80);
  - LCO de 1/1 (≤ 60) e 2/2;
  - linhas duplas a 0,10 m;
  - MFE, MFP e MCI de 0,20 a 0,30 m;
  - LRE de 0,30 a 0,60 m;
  - LDP de 0,20 a 0,40 m com traço e espaço de 0,50 m;
  - FTP-1 com listras de 0,30 a 0,40 m, espaço de 0,30 a 0,80 m e extensão de 3 a 4 m;
  - FTP-2 com 0,40 a 0,60 m e distância de 3 a 4 m;
  - LRV de 0,20 / 0,30 / 0,40 m;
  - ZPA com 0,30 a 0,50 m e espaço de 1,10 a 3,50 m, a 45°;
  - setas de 5,00 e 7,50 m;
  - transição de largura com l = 0,5·v·d.

**Não conferidos (prática usual — confira para o seu projeto)**, com a lista no `SINNORMAS`:
- dimensões e orlas da advertência (padrão de 0,50 / 0,60 / 0,75 m);
- altura de letras de indicação (125 / 150 / 200 mm);
- legendas no pavimento (1,60 / 2,40 m);
- espaçamento de tachas (16 m em tangente, 8 m em curva);
- obras:
  - taper pela fórmula l = 0,5·v·d do vol. IV;
  - cones a v/10;
  - zona de proteção por velocidade;
  - placas a 1,5·v;
- defensas: postes a 4 m (rígida a 2 m), lâmina de 4 m, terminal abatido de 12 m;
- semafórica: coluna de 6 m e braço de 4,6 m;
- engastamento de 0,60 m.

Fontes:
- [Manual Brasileiro de Sinalização de Trânsito, Vol. I — Regulamentação (DNIT)](https://www.gov.br/dnit/pt-br/rodovias/operacoes-rodoviarias/faixa-de-dominio/regulamentacao-atual/microsoft-word-01-mbst-vol-i-sin-vert-regulamenta_347_343o.pdf)
- [Manual Brasileiro de Sinalização de Trânsito, Vol. IV — Horizontal (SENATRAN)](https://www.gov.br/transportes/pt-br/pt-br/assuntos/transito/arquivos-senatran/educacao/publicacoes/manual_vol_iv_2.pdf)
- [Vol. IV — Horizontal (cópia DNIT)](https://www.gov.br/dnit/pt-br/rodovias/operacoes-rodoviarias/faixa-de-dominio/regulamentacao-atual/microsoft-word-04-mbst-vol-iv-sinaliza_347_343o-horizontal_f.pdf)

**Pictogramas:** são silhuetas simplificadas, desenhadas pelo plugin para o projeto em planta. Não substituem a arte-final de fabricação das placas.

---

## 6. Testes

| Teste | Como rodar | O que confere |
|---|---|---|
| Núcleo (86 verificações) | `dotnet run --project tests\Projeta.Sinalizacao.Testes -c Release` | Roda com a cultura pt-BR e confere: <ul><li>geometria de retas e arcos;</li><li>cadências e áreas das marcas;</li><li>FTP, LRE, LRV e ZPA;</li><li>setas e legendas;</li><li>áreas das placas;</li><li>suportes;</li><li>zona de obra e defensas;</li><li>JSON de todos os tipos;</li><li>XLSX e cadastro.</li></ul> |
| Demonstração no Civil 3D | `powershell -ExecutionPolicy Bypass -File .\ferramentas\testar_console.ps1 -Versao 2026` (ou 2027) | Roda o `SINDEMO` no console do Civil 3D, numa cópia do template, e confere: <ul><li>57 objetos;</li><li>alinhamentos;</li><li>MOVE e ROTATE;</li><li>correção;</li><li>quantitativos com estaca e lado;</li><li>190 sólidos 3D.</li></ul> |
| Comandos com respostas | `…\testar_console.ps1 -Versao 2026 -Roteiro .\ferramentas\teste_interativo.txt` | Responde aos prompts de praticamente todos os comandos. |
| Janelas | `dotnet build ferramentas\TesteUI -c Release` e executar | Abre as janelas fora do Civil 3D e salva capturas em `exemplos\telas`. |

A pasta `exemplos` traz:
- a planta do `SINDEMO` plotada (`demo_cruzamento.pdf`, `demo_obra.pdf`, `demo_geral.pdf` e `demo_3d.pdf`);
- o DWG da demonstração;
- a planilha de quantitativos;
- as capturas das janelas.

---

## 7. Fora desta versão
- Placas de serviços auxiliares e atrativos turísticos com pictograma. Hoje elas são feitas no modelador, só com texto e setas.
- Padrão de placas do Chile.
- Exportação para o Navisworks.
- Marcas horizontais em 3D (ficam em planta).
- Tabela de quantitativos dentro do DWG (a planilha sai em Excel).
- Programação semafórica (fases e tempos).
