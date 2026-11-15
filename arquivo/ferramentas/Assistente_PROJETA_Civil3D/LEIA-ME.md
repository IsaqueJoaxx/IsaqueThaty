# Assistente PROJETA - complemento Civil 3D 2026 e 2027

## Estado da entrega - Prévia 08 (12/10/2026)

Painel acoplável com quatro abas (Superfícies, Volumes, Seções e Corredores) e uma aba Point top no ribbon. O painel lê o desenho ativo sozinho. Calcular volumes não grava nada. Criar superfície de volume, seções ou corredores só acontece depois de uma prévia e de uma confirmação, e cada criação é um único comando: um `U` (Ctrl+Z) desfaz tudo o que ele criou.

Fontes anteriores: Prévia 04 em `_versoes antigas\Previa04-IFM`; a Prévia 06 (feita pelo Codex, antes destas mudanças) em `_versoes antigas\Estado-12-10-1320`.

### O que mudou da Prévia 07 para a 08: PKT ausentes

As seções tipo do template que usam submontagens do Subassembly Composer guardam o caminho do PKT do computador onde foram montadas: `C:\Users\Admin\Desktop\Engenharia e bases para projetos\Assemblys funcionais\`. Nesta máquina esse caminho não existe, então o Civil 3D acusa "submontagens ausentes" e o corredor sai vazio. No template há 18 submontagens assim, em 10 seções tipo (ADUELA, GABIÃO, CONTENÇÃO LE/LD, TALUDE E CONTENÇÃO LE/LD, CONTENÇÃO E TALUDE LE/LD, RUA PARA REVIT, PASSEIO + MF), e os 7 PKT estão em `D:\PROJETA ENGENHARIA\_FERRAMENTAS\pkt`.

- **Aviso na aba Corredores.** Quando a seção tipo escolhida (ou outra do desenho) tem PKT não encontrado, a aba diz qual arquivo falta e se ele está na pasta dos PKT. A prévia do corredor também avisa.
- **"Corrigir PKT ausentes…"** aponta cada submontagem sem arquivo para o PKT de mesmo nome na pasta (ou, se não houver, para o PKT que contém a mesma classe). Só muda o caminho gravado no desenho; os arquivos da pasta não são copiados nem alterados. É um comando só (`PROJETAPKT`): um `U` volta tudo.
- **"Trocar pasta dos PKT…"** escolhe outra pasta. A escolha fica guardada para o seu usuário do Windows (registro, `HKCU\Software\PROJETA\AssistenteCivil`).
- **Desatualizadas.** Se o PKT da pasta for mais novo que o usado quando a seção tipo foi montada, a mensagem avisa; para usar a versão nova, atualize as submontagens no Prospector. No template, isso acontece com `Contenção.pkt`.
- **Para não repetir em todo desenho novo:** abra o próprio `TEMPLATE TER & INFRA - PROJETA.dwt`, use "Corrigir PKT ausentes" e salve o template. Isso eu não fiz: o template é seu e não mexi nele.

### O que cada aba faz

- **Superfícies.** Lista as superfícies e sugere terreno natural e projeto pelo nome. A ficha (pontos, triângulos, áreas, cotas, extensão) é lida só quando você seleciona a superfície. "Ir até a superfície no desenho" (ou duplo clique) dá zoom e a deixa selecionada.
- **Volumes.** Terreno natural (base) x projeto (comparação), na área comum ou dentro de uma polilinha fechada que você clica no desenho, com fatores de corte e aterro. "Calcular" não grava nada. "Criar superfície de volume" cria `VOL - <terreno> x <projeto>` com o mesmo limite e fatores. "Exportar para Excel (CSV)" salva o resultado com ponto e vírgula e vírgula decimal, que o Excel em português abre direto.
- **Seções.** Eixo, estacas inicial e final, intervalo, larguras à esquerda e à direita e superfícies amostradas. Cria o grupo de linhas de amostragem e, se marcado, desenha as vistas de seção a partir de um ponto que você clica.
- **Corredores.** Marque os eixos (cada um usa o próprio greide de projeto), escolha a seção tipo, a superfície onde os taludes encontram o terreno, um corredor para todos ou um por eixo, e a frequência. O corredor é criado, os alvos de superfície são ligados e ele é reconstruído.

### O que mudou da Prévia 06 para a 07

- **Leitura automática.** Depois de cada comando, ao trocar de desenho e ao reabrir a paleta. As abas só são atualizadas se algo mudou, então escolhas e resultados não se perdem. O botão "Atualizar" força a releitura.
- **Desfazer reconhecido.** Depois de `U` ou Refazer o painel relê o desenho e tira da tela o "criado" que foi desfeito. Na aba Volumes, o botão de criar volta a ficar disponível, porque o cálculo continua valendo.
- **Aba Point top no ribbon**, com os botões Superfícies, Volumes, Seções e Corredores. Ela volta sozinha se o espaço de trabalho for trocado. O botão **VIAS DO LOTEAMENTO** roda o `PRJVIAS` do PROJETA Assistente Civil, e o painel **INTERSEÇÕES E ROTATÓRIAS** roda o `PRJSINALIZA` (sinalização), o `PRJSEMAFORO` (plano semafórico), o `PRJSEGURANCA` (auditor de segurança da rotatória) e o `PRJCAPACIDADE` (capacidade e fila), também do PROJETA Assistente Civil (precisa estar instalado; sem ele, o botão só avisa). A interseção ou a rotatória é montada antes, com o `PRJINTERSECAO` ou o `PRJROTATORIA` da aba PROJETA.
- **Leitura leve.** A lista não lê triângulos; a ficha completa só é lida ao selecionar a superfície.
- **Clique do limite só enxerga polilinhas.** Antes, clicar na borda da polilinha pegava a superfície que estava por baixo.
- **Exportar volumes para CSV.**
- **Corredor reconstruído logo após criar.** Se a reconstrução falhar, a mensagem diz qual corredor reconstruir à mão. Quando a seção tipo não tem subassembly que peça superfície como alvo, a mensagem explica o "0 alvos".
- **Resultado no topo da aba.** A aba rola até a mensagem do comando, mesmo que você estivesse lá embaixo.
- **Comandos digitados abrem a aba certa**, inclusive na primeira vez que a paleta é aberta (antes caíam em Superfícies).
- **Correções:** o aviso "Os campos mudaram" aparecia sozinho depois de criar a superfície de volume; e a mensagem final depois de criar era sobrescrita pela releitura.
- **Instalação carrega ao abrir o Civil 3D**, para a aba do ribbon aparecer sem digitar nada.

### Comandos

| Comando | O que faz |
|---|---|
| `PROJETAASSISTENTE` | Abre o painel na aba Superfícies |
| `PROJETASUPERFICIES` | Relê o desenho e escreve um resumo na linha de comando. Só leitura |
| `PROJETAVOLUMES`, `PROJETASECOES`, `PROJETACORREDORES` | Digitados, abrem a aba correspondente. Os botões do painel usam os mesmos comandos para calcular ou criar |
| `PROJETALIMITE` | Pede o clique na polilinha do limite (usado pelo botão "Selecionar polilinha") |
| `PROJETAPKT` | Aponta as submontagens PKT não encontradas para a pasta dos PKT (usado pelo botão "Corrigir PKT ausentes") |
| `PROJETAZOOM` | Vai até a superfície selecionada (usado pelo painel) |

## Regra de sugestão pelo nome

| Nome da superfície | Função sugerida |
|---|---|
| `1 TN`, `01-TN`, `1_terreno`, `1` | Terreno natural |
| `2 Projeto`, `02 - FG`, `2.final` | Projeto |
| Duas ou mais com o mesmo número | Terreno natural · escolher / Projeto · escolher |
| Superfície de volume, com qualquer nome | Superfície de volume |
| `10 Base`, `TN`, `Projeto`, sem número | Não identificado |

Depois do número é obrigatório um espaço, um separador (`- – — _ . :`) ou o fim do nome. A sugestão só preenche a escolha inicial; você pode trocar nas listas.

## Compilação

Requer o SDK .NET 10 e os pacotes de referência do .NET 8, que vêm com o SDK .NET 8 já instalado nesta máquina. No PowerShell, nesta pasta:

```powershell
.\Compilar.ps1
```

O script confere os SDKs e as bibliotecas do Civil 3D 2026 e 2027, compila as duas versões e copia o resultado para:

- `bin\Previa08\2026\Projeta.Civil3D.dll` - Civil 3D 2026 (.NET 8)
- `bin\Previa08\2027\Projeta.Civil3D.dll` - Civil 3D 2027 (.NET 10)

O registro fica em `compilacao-previa08.log`. Para outra prévia: `.\Compilar.ps1 -Previa Previa09`. O NuGet.Config continua sem fontes externas: o projeto usa a versão do .NET 8 já instalada (informada pelo Compilar.ps1 em `Net8TargetingPackVersion`). Nada é baixado.

## Instalação

Feche o Civil 3D e rode:

```powershell
.\Instalar.ps1
```

O script pede permissão de administrador e copia as duas versões para `C:\Program Files\Autodesk\ApplicationPlugins\Projeta.Civil3D.bundle`. Essa pasta é confiável para o Civil 3D por padrão, então não é preciso alterar "Locais confiáveis" nem o SECURELOAD. O Civil 3D escolhe sozinho a versão certa (2026 ou 2027) e carrega o complemento ao abrir; a aba Point top aparece no ribbon.

Cada instalação usa um nome de arquivo com data e hora, então dá para reinstalar com o Civil 3D aberto; a versão nova entra na próxima vez que ele abrir.

Depois de instalar, não carregue prévias antigas com NETLOAD: o Civil 3D mantém a primeira DLL com este nome que entrar na sessão, e uma prévia antiga carregada antes impede a nova de funcionar até o Civil 3D ser reaberto.

Para desinstalar, apague a pasta `Projeta.Civil3D.bundle` com o Civil 3D fechado.

## Teste realizado em 12/10/2026

Cada versão foi carregada com NETLOAD ("Carregar uma vez") numa janela separada do Civil 3D, aberta só para o teste, sem tocar nos seus desenhos. Um desenho novo, criado a partir do `TEMPLATE TER & INFRA - PROJETA.dwt` e fechado sem salvar, recebeu superfícies de grade 200 x 200 m com resultado conhecido: `01 TN` plana na cota 100, `2 - Projeto` inclinada de 99 a 101, `02_Projeto alternativo` plana na cota 102, `10 Base` e a superfície de volume `1 Volume TN x Projeto`; o eixo `Eixo 01` (0 a 200 m) com greide, o `Eixo 02` sem greide, uma seção tipo e uma polilinha de limite de 60 x 100 m.

| Verificação | 2027 | 2026 |
|---|---|---|
| DLL carrega; aba Point top no ribbon com os 4 botões | sim | sim |
| Leitura automática ao abrir o painel, após comandos e ao trocar/fechar desenho | sim | sim |
| Ficha da superfície lida ao selecionar (121 pontos, 200 triângulos, 4 ha) | sim | sim |
| Volumes na área comum: corte 10.000 m³ e aterro 10.000 m³ (esperado: 10.000 e 10.000) | sim | sim |
| Volumes na polilinha: corte 0, aterro 3.000 m³; com aterro × 1,1 = 3.300 m³ (esperado) | sim | sim |
| Clique na borda da polilinha pega a polilinha, não a superfície por baixo | mesmo código, não repetido | sim |
| Superfície de volume criada com os mesmos valores | sim | criada; valores não conferidos |
| Um `U` remove a superfície de volume; painel atualiza a lista | sim | sim |
| Depois do `U`, aba Volumes avisa e libera "Criar" de novo | sim | compilado depois do teste |
| Exportação CSV com os valores do cálculo | sim | não conferido |
| Seções: 11 linhas de amostragem, superfícies marcadas amostradas, 11 vistas de seção | sim | sim |
| Um `U` remove o grupo de linhas e as vistas de seção | sim (testado sem vistas) | sim |
| Corredor criado de 0 a 200 m com o greide do eixo | sim | sim |
| Corredor reconstruído, 21 estacas a cada 10 m | não conferido | sim |
| Um `U` remove o corredor | não conferido | sim |
| Depois do `U`, a aba relê o desenho e tira a mensagem de "criado" | sim | compilado depois do teste |
| Comando digitado com a paleta nunca aberta abre a aba certa | sim | compilado depois do teste |

As janelas de teste abriram com licença educacional; por isso nada foi salvo nelas. As últimas correções (desfazer reconhecido e aba certa na primeira abertura) foram vistas no 2027; o 2026 usa o mesmo código. A instalação por bundle não foi executada: depende da permissão de administrador e fica a seu critério.

### Observações do teste (do template, não do complemento)

- No 2026, as vistas de seção criadas com o estilo padrão do template mostraram estacas e cotas em pés (a estaca de 20 m apareceu como `0+65.62`). O complemento usa o estilo padrão do desenho; vale conferir o estilo de vista de seção no template do 2026.
- A seção tipo `ADUELA` do template avisa "submontagens ausentes" ao criar o corredor, e o complemento não encontrou nela nenhum alvo de superfície para ligar (0 alvos). Com uma seção tipo completa, os taludes devem ser ligados à superfície escolhida; isso ainda não foi testado.

## Arquitetura

- `Commands.cs` registra os comandos, mantém uma única paleta e faz a leitura automática: no fim de cada comando agenda uma releitura para o próximo momento ocioso, com o documento travado, e só repassa às abas se a assinatura do desenho mudou (ou depois de `U`/Refazer).
- O painel nunca escreve no desenho diretamente. Cada botão guarda o pedido (`Requests.cs`) e chama um comando fixo pelo nome; a escrita acontece dentro desse comando (`Operations.cs`). O Civil 3D agrupa tudo o que um comando faz numa única etapa de desfazer, por isso um `U` desfaz a operação inteira.
- `DrawingModel.cs` faz a leitura leve (nomes, tipos, eixos, greides, seções tipo) e a ficha detalhada sob demanda.
- `Ribbon.cs` cria a aba Point top; `MainPanel.cs`, `ToolPage.cs` e as quatro páginas formam o painel; `Theme.cs` acompanha o tema claro ou escuro (COLORTHEME).
- Cuidado para quem for mexer: no teste, `SampleLineGroup.GetSectionSources()` derrubou o Civil 3D quando chamado num `OpenCloseTransaction` com o grupo aberto só para leitura. O complemento usa transação normal com o grupo aberto para escrita.

## Próximo marco

Drenagem continua aguardando a planilha, fórmulas, critérios, unidades e casos de conferência. Para o que já existe: escolha de estilos de vista de seção e de rótulos no painel.

Feito na Prévia 18 (0.18.0): volume por estaca a partir das seções do próprio Civil 3D, na aba Volumes. Veja `VOLUME-SECOES.md`.

## Fontes oficiais consultadas

- [AutoCAD 2027 SDK: o que todo desenvolvedor de plugin precisa saber](https://blog.autodesk.io/autocad-2027-sdk-what-every-plugin-developer-needs-to-know/) - 2027 exige .NET 10; 2025 e 2026 usam .NET 8.
- [Portal Civil 3D 2027](https://help.autodesk.com/view/CIV3D/2027/ENU/).
- [GetSurfaceIds - referência Autodesk 2026](https://help.autodesk.com/cloudhelp/2026/ENU/Civil3D-API/files/html/59b32dbe-7928-7fc4-cf12-2d16b1ce214f.htm).
- [PaletteSet - métodos Autodesk](https://help.autodesk.com/cloudhelp/2022/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-__MEMBERTYPE_Methods_Autodesk_AutoCAD_Windows_PaletteSet.html) e [propriedades](https://help.autodesk.com/cloudhelp/2019/ENU/OARX-ManagedRefGuide/files/OREFNET-__MEMBERTYPE_Properties_Autodesk_AutoCAD_Windows_PaletteSet.html).
