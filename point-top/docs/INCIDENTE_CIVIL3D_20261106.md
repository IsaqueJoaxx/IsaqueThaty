# Teste do conector suspenso — 06/11/2026

O usuário apresentou a janela de relatório de fechamento inesperado do Civil 3D 2027 após a tentativa de carregar o conector e a informação de que PTVINCULAR não foi reconhecido.

A causa não foi determinada. A presença da DLL entre os módulos do processo foi confirmada antes do incidente, mas isso não comprova a conclusão de Initialize nem o registro dos comandos. Não há snapshot de vínculo em runtime/civil3d. O acad.err da pasta PROJETA é antigo (13/10) e não explica este incidente. As buscas iniciais nos eventos da aplicação, arquivos temporários e log Horizun não forneceram a exceção desta falha.

Não repetir NETLOAD, não ativar reconstrução automática e não descrever o conector como estável até obter os detalhes do relatório Autodesk. A DLL permanece no local para análise; nenhum componente foi instalado para carregamento automático. Nenhum processo foi encerrado e nenhum DWG foi salvo ou sobrescrito durante esta investigação.

Próxima evidência necessária: detalhes de erro do relatório Autodesk, especialmente módulo/arquivo com falha, código de exceção e caminho dos arquivos de diagnóstico. Não enviar relatório a terceiros sem solicitação explícita do usuário.

Após identificar a causa, validar primeiro em desenho descartável, somente leitura e com diagnóstico de carregamento. Só então testar vínculo e reconstrução de corredor com cópia de projeto. Os testes anteriores de volumes não validam este conector.

Atualização: o usuário localizou o fechamento ao executar PTVINCULAR, antes do primeiro pedido de seleção. Preparado diagnóstico isolado 0.3.1, sem eventos/objetos Civil/rebuild, ainda não carregado no host. A DLL 0.3 permanece suspensa; a causa ainda não foi identificada. Ver DIAGNOSTICO_PTVINCULAR_031.md.


## Revisão de 06/11/2026, 11h: a causa atribuída não se sustenta

**Conclusão:** a janela de erro vista provavelmente veio de outro trabalho, executado pelo Claude no mesmo computador, e não do PTVINCULAR. Não há registro de queda do Civil 3D nessa madrugada.

### Linha do tempo (horário local, 06/11)

| Hora | Fato | Origem |
|---|---|---|
| 02:04 | Civil 3D iniciado (PID 34996) | `AdlSdk-34996.log` |
| 02:05:54 | `PointTop.Civil3D.dll` selecionada no NETLOAD | atalho em `Recent\NetLoadDialog` |
| 02:09:50, 02:10:27, 02:11:02, 02:11:28 | 4 falhas de `accoreconsole.exe` com janela "Autodesk Civil 3D 2027" | eventos 1000/1026, `CER\dialog.log` |
| 02:11:17 a 02:11:26 | PID 34996 encerra gravando arquivos de sessão, compatível com fechamento normal | `AcCommandWeight.xml`, `agent-stdout-34996.log` |
| 02:11:27 | Novo Civil 3D (PID 36040) | processo |
| 02:11:57 | Conector marcado como suspenso | `TESTE_SUSPENSO.txt` |
| 02:13:48 | DLL 0.3 selecionada de novo no NETLOAD, já no PID 36040 | mesmo atalho |
| 11:04 | PID 36040 segue aberto, com a DLL 0.3 carregada | lista de módulos do processo |

### Evidências

- Os 4 relatórios da Autodesk trazem o comando `R3DINVENTARIO`, a cópia `copia.dwg` e o erro `DirectoryNotFoundException` em `Rodovias3D.Cmd`. Esse código é um leitor do projeto Rodovias 3D, sem relação com o POINT TOP.
- Em 06/11 não há evento 1000, 1001 ou 1026 de `acad.exe`, nem dump de `acad.exe` em `CrashDumps` (o último é de 03/11), nem relatório da Autodesk referente ao PTVINCULAR.
- O PTVINCULAR da DLL 0.3 foi executado em `accoreconsole`, em cópia descartável: pediu a seleção, vinculou e desligou sem falha. O mesmo filtro `AddAllowedClass` suspeito foi exercitado.

### Limites desta conclusão

- A janela vista pelo usuário não foi examinada. A atribuição vem da coincidência de horário e da ausência de falha do `acad.exe`.
- Console não é a janela do Civil 3D: o teste não exclui problema exclusivo da interface.
- O relato "PTVINCULAR não reconhecido" não foi reproduzido nem explicado.
- O código exit `0xC0000005` apareceu ao sair do console em parte das execuções, também com plugin não relacionado ao POINT TOP e nunca sem carregar os tipos Civil. Não gerou janela nem evento e não foi investigado além disso.

### Arquivos deixados pelas 4 falhas

- `%LOCALAPPDATA%\CrashDumps\accoreconsole.exe.{34044,35832,38092,38472}.dmp`, cerca de 27 MB cada.
- `C:\ProgramData\Autodesk\CER\acminidump_big.dmp`, cerca de 25 MB.
- Relatórios em `%LOCALAPPDATA%\Autodesk\CER\9d6767...`.

Podem ser apagados. O `dialog.log` registra envio automático de informação gráfica ativo, e que os arquivos extras do relatório não foram enviados, por falta de consentimento.

### Decisão

A DLL 0.3 continua fora de uso por não ter proteção contra exceções, e não por causa comprovada de fechamento. O trabalho segue no conector 0.4: ver `docs/CONECTOR_04.md`.
