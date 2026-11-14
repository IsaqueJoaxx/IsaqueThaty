# INTEGRAÇÃO CIVIL 3D — IFM

Estado em 19/10/2026: instalado e registrado no Codex; aguardando abertura do Civil 3D para validar conexão com desenho.

## Conclusão após autorização

O usuário autorizou explicitamente as duas pastas confiáveis e confirmou que fechou o Civil 3D. Instalação concluída para 2026 e 2027: 49 arquivos do bundle e 14 arquivos do servidor conferidos por SHA-256. Os dois diretórios foram acrescentados aos perfis Civil existentes com backup e releitura; SECURELOAD preservado em cada perfil.

Servidor `horizun-civil3d` registrado pelo CLI oficial no Codex, apontando para `C:\Users\User\AppData\Local\Programs\Horizun\Civil3D-MCP\server\horizun-civil3d-mcp.exe`. Backup do config em `codex-config-antes-horizun.toml`. Comparação semântica confirma os demais ajustes preservados; a CLI apenas omitiu `args=[]` do conector node_repl, equivalente a nenhum argumento.

Teste de protocolo com o executável instalado: initialize aprovado e catálogo com 28 ferramentas. `horizun_c3d_health` respondeu `no_civil3d_instance`; nenhum desenho foi lido ou alterado. Usuário recebeu solicitação para abrir o Civil 3D e um desenho de teste. Evidência: `verificacao-instalada.json`.

Próximo passo: conferir health depois da abertura; identificar o documento e fazer somente consultas de alinhamentos, superfícies e perfis. O cliente Codex poderá precisar ser reiniciado para disponibilizar o conector neste chat. O helper `scripts/mcp_call.py` permite testar o servidor instalado sem reiniciar o chat.

## Histórico da preparação

- Usuário solicitou prosseguir com integração Horizun.
- Release obtida da origem: HorizunGroup/horizun-civil3d-mcp, v0.9.2.
- ZIP original conferido com SHA256SUMS publicado: FC89B1E0ECB0FA5B3D19EC0B5C9AA2F6E564AD704F663BF0943B9AC4274974A7.
- Civil 3D 2026: runtime net8, AutoCAD assembly 25.1.0.0, Civil assembly 13.8.0.1516; corresponde ao pacote publicado.
- Civil 3D 2027: runtime net10, AutoCAD assembly 26.0.0.0, Civil assembly 13.9.0.628; plugin compilado localmente a partir da tag v0.9.2 com referências instaladas. Zero erros/avisos de compilação.
- Core/Server: 552 testes aprovados, zero falhas; dois avisos preexistentes xUnit1031.
- Pacote combinado: `pacote-local-2026-2027`; original mantido em `pacote-0.9.2`. O código funcional não foi modificado. Acrescentados somente o build 2027 e suas entradas de metadados ao pacote local.
- Ensaio `install.ps1 -Years 2026,2027 -DryRun` aprovado, conferindo correspondência de referências.
- Revisão automática rejeitou instalação com `-RegisterTrustedPath`: a mudança persistente dos Locais confiáveis requer autorização específica. Pergunta enviada ao usuário, ainda pendente. Não contornar esse bloqueio.
- Tentativas de instalar sem alterar confiança foram impedidas pelo instalador: `acad.exe` PID 41732 em execução. Nada instalado. A enumeração de processos no sandbox não mostrou esse processo; confiar na verificação fora do sandbox/instalador.
- Não encerrar o AutoCAD; usuário deve salvar e fechar. Pergunta enviada ao usuário.
- Codex ainda não registrado para este servidor, pois o executável de destino não foi instalado. Nenhuma configuração de segurança alterada.

## Continuação

Após o usuário fechar o Civil 3D, executar o instalador do pacote combinado sem registro em Claude. Incluir `-RegisterTrustedPath` somente se o usuário responder autorizando explicitamente as duas pastas. O instalador faz backup e compara hashes. Registrar no Codex usando `codex mcp add horizun-civil3d -- <executável instalado>`, preservando backup e configurações existentes. Validar initialize/tools/list e health. Solicitar abertura do Civil 3D e desenho de teste para comprovar leitura real. Não declarar conexão operacional antes dessa verificação.
