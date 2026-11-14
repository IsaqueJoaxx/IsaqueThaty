# Assistente PROJETA — complemento Civil 3D 2026 e 2027

## Estado da entrega — Prévia 05 (12/10/2026)

Painel acoplável nativo que lista as superfícies do desenho ativo e sugere qual é terreno natural e qual é projeto. Somente leitura: não calcula volumes, não lê triângulos, não cria objetos e não altera o desenho. A conversa e o cadastro de bacias continuam na prévia HTML, separados deste complemento.

O que mudou em relação à Prévia 04 (o fonte da Prévia 04 está em `_versoes antigas\Previa04-IFM`):

- **Duas versões.** Uma compilação para o Civil 3D 2026 (.NET 8) e outra para o 2027 (.NET 10), a partir do mesmo fonte.
- **Status mostra só o nome do arquivo.** O caminho completo aparece ao parar o mouse sobre o status.
- **Superfícies de volume ficam fora da sugestão.** Aparecem como "Superfície de volume" e não contam como candidatas a terreno natural ou projeto.
- **Tipos em português.** TIN, Grade, Volume TIN e Volume grade.
- **Zeros à esquerda reconhecidos.** "01 TN" e "02_Projeto" funcionam. "10", "20" e "012" continuam sem função.
- **Tabela cabe na paleta.** Três colunas (Superfície, Função sugerida, Tipo) que se ajustam à largura, sem rolagem lateral. O nome completo e o handle aparecem ao parar o mouse sobre a linha.
- **Cabeçalho da tabela no tema do painel**, e o painel acompanha o tema claro ou escuro do Civil 3D (COLORTHEME), inclusive quando ele muda com o painel aberto.
- **Mais espaço para a tabela.** As linhas de texto do painel ocupam só a altura do texto, de acordo com a largura da paleta e a escala do monitor.
- **Instalação por bundle** (`Instalar.ps1`), com carregamento sob demanda.

## Regra de sugestão pelo nome

| Nome da superfície | Função sugerida |
|---|---|
| `1 TN`, `01-TN`, `1_terreno`, `1` | Terreno natural |
| `2 Projeto`, `02 - FG`, `2.final` | Projeto |
| Duas ou mais com o mesmo número | Terreno natural · escolher / Projeto · escolher |
| Superfície de volume, com qualquer nome | Superfície de volume |
| `10 Base`, `TN`, `Projeto`, sem número | Não identificado |

Depois do número é obrigatório um espaço, um separador (`- – — _ . :`) ou o fim do nome.

## Compilação

Requer o SDK .NET 10 e os pacotes de referência do .NET 8, que vêm com o SDK .NET 8 já instalado nesta máquina (8.0.423). No PowerShell, nesta pasta:

```powershell
.\Compilar.ps1
```

O script confere os SDKs e as bibliotecas do Civil 3D 2026 e 2027, compila as duas versões e copia o resultado para:

- `bin\Previa05\2026\Projeta.Civil3D.dll` — Civil 3D 2026
- `bin\Previa05\2027\Projeta.Civil3D.dll` — Civil 3D 2027

O registro fica em `compilacao-previa05.log`. Para outra prévia: `.\Compilar.ps1 -Previa Previa06`. O NuGet.Config continua sem fontes externas: o SDK 10 pediria pacotes .NET 8 mais novos pela internet, e o projeto usa a versão já instalada (informada pelo Compilar.ps1 em `Net8TargetingPackVersion`). Nada é baixado.

## Instalação

```powershell
.\Instalar.ps1
```

O script pede permissão de administrador e copia as duas versões para `C:\Program Files\Autodesk\ApplicationPlugins\Projeta.Civil3D.bundle`. Essa pasta é confiável para o Civil 3D por padrão, então não é preciso alterar "Locais confiáveis" nem o SECURELOAD. O Civil 3D escolhe sozinho a versão certa (2026 ou 2027).

A DLL carrega quando você digita `PROJETAASSISTENTE` ou `PROJETASUPERFICIES`. Cada instalação usa um nome de arquivo com data e hora, então dá para atualizar com o Civil 3D aberto; a versão nova entra na próxima vez que ele abrir.

Depois de instalar, não carregue as prévias antigas com NETLOAD: o Civil 3D mantém a primeira DLL com este nome que entrar na sessão, e uma prévia antiga carregada antes impede a nova de funcionar até o Civil 3D ser reaberto.

Para desinstalar, apague a pasta `Projeta.Civil3D.bundle` com o Civil 3D fechado.

## Teste realizado em 12/10/2026

Cada versão foi carregada com NETLOAD ("Carregar uma vez") numa janela separada do Civil 3D, aberta só para o teste. Um desenho novo, criado a partir do `TEMPLATE TER & INFRA - PROJETA.dwt` e fechado sem salvar, recebeu seis superfícies: `01 TN`, `2 - Projeto`, `02_Projeto alternativo`, `10 Base`, um nome longo sem número e a superfície de volume `1 Volume TN x Projeto`.

| Verificação | 2027 | 2026 |
|---|---|---|
| DLL carrega e o painel indica a versão | sim | sim |
| Botão do painel consulta as 6 superfícies | sim | sim |
| `01 TN` = Terreno natural; o volume não conta como candidato | sim | sim |
| `2 - Projeto` e `02_Projeto alternativo` = Projeto · escolher | sim | sim |
| Volume = Superfície de volume, tipo Volume TIN | sim | sim |
| `10 Base` e nome sem número = Não identificado | sim | sim |
| Status com nome do arquivo; tabela sem rolagem lateral | sim | sim |
| Dica com nome completo e handle | sim | não conferido |
| Troca para tema claro e volta ao escuro com o painel aberto | sim | não conferido |
| Ajuste da altura das linhas de texto | compilado, não visto | sim |

O ajuste da altura das linhas de texto entrou depois do teste no 2027; ele foi visto funcionando no 2026, com o mesmo código. O tema foi devolvido ao escuro ao final. A instalação por bundle não foi executada: depende da permissão de administrador e fica a seu critério.

## Arquitetura e limite de leitura

`Commands.cs` mantém uma única PaletteSet durante a sessão e registra os dois comandos. A troca ou o fechamento de desenhos limpa a fotografia exibida, sem consultar a base por eventos. A mudança de COLORTHEME só troca as cores do painel. O botão envia apenas o nome fixo do comando de leitura, sem interpolar entradas do usuário.

`SurfaceReader.cs` resolve o CivilDocument a partir da base do documento recebido. A leitura acontece no contexto do comando, numa transação curta, com `OpenMode.ForRead`, sem Commit. Apenas textos são enviados ao painel; nenhum objeto da base fica aberto ou guardado.

`SurfaceNaming.cs` contém a regra de sugestão pelo nome. `AssistantPanel.cs` exibe desenho, horário da consulta e a lista. O handle serve só como referência dentro daquele desenho.

## Próximo marco

Usar a sugestão para escolher terreno natural e projeto, acrescentar a seleção e validação do limite e, depois, os volumes. Drenagem continua aguardando a planilha, fórmulas, critérios, unidades e casos de conferência.

## Fontes oficiais consultadas

- [AutoCAD 2027 SDK: o que todo desenvolvedor de plugin precisa saber](https://blog.autodesk.io/autocad-2027-sdk-what-every-plugin-developer-needs-to-know/) — 2027 exige .NET 10; 2025 e 2026 usam .NET 8.
- [Portal Civil 3D 2027](https://help.autodesk.com/view/CIV3D/2027/ENU/).
- [GetSurfaceIds — referência Autodesk 2026](https://help.autodesk.com/cloudhelp/2026/ENU/Civil3D-API/files/html/59b32dbe-7928-7fc4-cf12-2d16b1ce214f.htm).
- [PaletteSet — métodos Autodesk](https://help.autodesk.com/cloudhelp/2022/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-__MEMBERTYPE_Methods_Autodesk_AutoCAD_Windows_PaletteSet.html) e [propriedades](https://help.autodesk.com/cloudhelp/2019/ENU/OARX-ManagedRefGuide/files/OREFNET-__MEMBERTYPE_Properties_Autodesk_AutoCAD_Windows_PaletteSet.html).
