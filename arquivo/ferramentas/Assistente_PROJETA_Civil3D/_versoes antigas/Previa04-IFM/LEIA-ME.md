# Assistente PROJETA — base do complemento Civil 3D 2027

## Estado da entrega

Fonte de um painel acoplável nativo e de uma operação de leitura das superfícies do desenho ativo. **Compilado com sucesso em 12/10/2026 com SDK .NET 10.0.401: zero avisos e zero erros. DLL gerada; ainda não carregada nem testada dentro do Civil 3D.** A prévia HTML e seu cadastro de bacias continuam separados e preservados.

O marco implementado no fonte é restrito a nome, tipo e handle das superfícies. Não calcula volumes, não lê triângulos, não seleciona terreno/projeto, não cria objetos e não altera o desenho. A conversa e o cadastro de bacias da prévia ainda não estão incorporados ao painel nativo. Não há IA, serviço externo, credenciais, hospedagem ou carregamento automático.

## Instalação observada em 11/10/2026

| Item | Evidência local |
|---|---|
| AutoCAD 2027 | `C:\Program Files\Autodesk\AutoCAD 2027\acad.exe`, versão R26.0.60.0.0 |
| Bibliotecas base | acmgd.dll, accoremgd.dll e acdbmgd.dll, versão 26.0.60.0.0 |
| Civil 3D | `C3D\AeccDbMgd.dll`, versão 13.9.628.0 |
| Base AEC | `ACA\AecBaseMgd.dll`, versão 8.9.51.0 |
| Runtime da instalação | acdbmgd.runtimeconfig.json e C3D\AeccUiMgd.runtimeconfig.json declaram net10.0 |
| Runtime .NET disponível | 10.0.0, incluindo WindowsDesktop |
| SDK inicialmente disponível | somente 8.0.423; SDK 10.0.401 instalado em 12/10/2026 |

A presença das bibliotecas confirma referências disponíveis; não comprova licença, sessão ativa ou execução do complemento. O projeto usa `net10.0-windows`, x64, WinForms e referências locais com `Private=false`: não distribui bibliotecas Autodesk junto do complemento.

## Compilação

É necessário disponibilizar um SDK .NET 10 compatível em um ambiente de desenvolvimento. O SDK .NET 10.0.401 foi instalado nesta etapa, com autorização do usuário, pelo pacote oficial Microsoft.DotNet.SDK.10 via WinGet. A instalação terminou com sucesso. Ter o runtime 10 instalado não substitui o SDK.

No PowerShell, execute o script a partir desta pasta:

```powershell
.\Compilar.ps1
```

O script verifica SDK e referências antes de compilar. Não modifica a instalação Autodesk. O arquivo NuGet.Config desativa fontes externas, pois este projeto usa apenas referências locais e os packs do SDK; um ambiente sem os packs WindowsDesktop precisará ser preparado separadamente.

Saída esperada **somente após compilação bem-sucedida**:

`D:\PROJETA ENGENHARIA\_FERRAMENTAS\Assistente_PROJETA_Civil3D\bin\Release\net10.0-windows\Projeta.Civil3D.dll`

## Validação futura dentro do Civil 3D

1. Compilar, revisar os avisos e usar uma cópia de um desenho de referência em Civil 3D 2027.
2. Usar NETLOAD para carregar a DLL compilada. Respeitar as verificações de segurança do produto; não desabilitar SECURELOAD nem ampliar pastas confiáveis de forma indiscriminada. Nenhuma configuração de segurança foi alterada nesta tarefa.
3. Executar `PROJETAASSISTENTE` para abrir a paleta e acoplá-la na lateral.
4. Clicar em **Consultar superfícies do desenho ativo**, ou executar `PROJETASUPERFICIES`. Comparar nomes e tipos com o Prospector.
5. Verificar um desenho sem superfícies: a lista deve ficar vazia e informar zero superfícies.
6. Alternar entre dois desenhos: a consulta anterior deve ser limpa, exigindo nova leitura. Fechar um desenho também invalida a lista.
7. Editar uma superfície e consultar novamente: a lista é uma fotografia da última consulta, não atualização contínua. Conferir que nenhum objeto foi criado/modificado pelo complemento e que o estado de alterações do desenho permaneceu igual ao anterior à consulta.
8. Testar com comando em andamento: o botão deve solicitar que ele seja concluído. Não cancela comandos.

## Arquitetura e limite de leitura

`Commands.cs` mantém uma única PaletteSet durante a sessão e registra os dois comandos. A troca/fechamento de desenhos limpa a fotografia exibida, sem consultar a base por eventos. O botão envia apenas o nome fixo do comando de leitura, sem interpolar entradas do usuário.

`SurfaceReader.cs` resolve o CivilDocument a partir da base do documento recebido. A leitura acontece no contexto do comando, em uma transação curta, com `OpenMode.ForRead`, sem Commit. Apenas strings são enviadas à interface; objetos da base não permanecem abertos ou guardados no painel. Uma falha limpa os resultados anteriores e apresenta erro, sem apresentar resultado parcial como leitura concluída.

`AssistantPanel.cs` exibe o desenho, horário da consulta e lista de superfícies. O handle serve apenas como referência diagnóstica dentro daquele desenho; não é um identificador global entre arquivos. Este marco não define ainda o vínculo das superfícies com terreno natural e projeto.

## Verificação realizada

- Referências e runtime inspecionados somente por leitura.
- XML do projeto/configuração e contratos estáticos de leitura verificados.
- Tentativa inicial com SDK 8.0.423 falhou com NETSDK1045. Após instalação autorizada do SDK 10.0.401, a compilação C# contra as DLLs Autodesk locais passou. UseWPF foi habilitado para resolver as referências WindowsBase transitivas das APIs de interface. Compilação final: zero avisos e zero erros; registro em compilacao.log.
- O comando `dotnet --info` também apresentou restrição de acesso ao Service Control Manager ao consultar workloads; `--list-sdks` funcionou e confirmou a versão instalada. Não houve tentativa de mudar serviços ou permissões.
- Nenhum teste em sessão do Civil 3D, nenhum desenho aberto, nenhuma DLL carregada e nenhuma instalação Autodesk modificada.

## Próximo marco

Carregar manualmente a DLL compilada no Civil 3D 2027 e executar o roteiro acima. Depois disso, usar a lista real para escolher terreno natural e projeto, acrescentando a seleção e validação do limite em uma etapa própria. Drenagem continuará aguardando a planilha ou sua análise, fórmulas, critérios, unidades e casos de conferência.

## Fontes oficiais consultadas

- [Portal Civil 3D 2027](https://help.autodesk.com/view/CIV3D/2027/ENU/).
- [GetSurfaceIds — referência Autodesk 2026](https://help.autodesk.com/cloudhelp/2026/ENU/Civil3D-API/files/html/59b32dbe-7928-7fc4-cf12-2d16b1ce214f.htm). A página equivalente 2027 não retornou conteúdo na consulta; o padrão foi cruzado com a referência 2027 da Autodesk abaixo. Isso não substitui compilação/teste contra as DLLs locais.
- [Referência Civil 3D 2027 mantida pela Autodesk Platform Services](https://github.com/autodesk-platform-services/skills/blob/main/skills/acad-dotnet/references/civil3d-api.md), verificada por seus autores contra Civil3D.NET 13.9.628.
- [PaletteSet — métodos Autodesk](https://help.autodesk.com/cloudhelp/2022/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-__MEMBERTYPE_Methods_Autodesk_AutoCAD_Windows_PaletteSet.html) e [propriedades](https://help.autodesk.com/cloudhelp/2019/ENU/OARX-ManagedRefGuide/files/OREFNET-__MEMBERTYPE_Properties_Autodesk_AutoCAD_Windows_PaletteSet.html). Documentação de versões anteriores; compatibilidade final pendente de compilação.
- [Transição .NET 10 — Autodesk](https://help.autodesk.com/view/ACD/2025/ENU/?caas=caas%2Fsfdcarticles%2Fsfdcarticles%2FAutoCAD-and-Toolsets-Requirements-for-products-affected-by-the-Microsoft-NET-10-transition.html). A escolha de net10.0 nesta entrega também se baseia nos arquivos runtimeconfig da instalação local.

