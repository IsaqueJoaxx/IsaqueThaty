# INTEGRAÇÃO CIVIL 3D — IFM

Pesquisa realizada em 19/10/2026. Objetivo: permitir que o assistente consulte objetos e execute operações no Civil 3D, seguindo os padrões PROJETA. Nenhuma ponte de terceiros foi instalada nesta pesquisa.

## Opções encontradas

| Opção | O que a documentação declara | Adequação ao projeto |
| --- | --- | --- |
| Horizun Civil 3D MCP | Ferramentas para eixos, greides, superfícies e corredores; comunicação local, prévia de alterações e releitura após gravação. Declara alvos de compilação 2026/2027, com evidências principais em sessão 2025 e validações adicionais pendentes. | Primeiro candidato a avaliar para uso operacional; confirmar o pacote compatível e testar nas versões instaladas. |
| hjlrosales / Civil3D_MCP | Ponte C# e servidor Node.js, comunicação local, consulta/edição de objetos; declara Civil 3D 2025/2026. | Alternativa para 2026; compatibilidade com 2027 não estabelecida pela documentação consultada. |
| barbosaihan / civil3d-mcp | Ponte que recebe e executa C# com acesso à API do Civil 3D, acompanhada de modelos de código. | Alternativa de desenvolvimento para testar rotinas; exige verificar compatibilidade e revisar o mecanismo de execução. |

As descrições são declarações dos autores. Não foram instaladas ou testadas localmente e não equivalem a certificação da Autodesk.

Fontes primárias: [Horizun Civil 3D MCP](https://github.com/HorizunGroup/horizun-civil3d-mcp), [Civil3D_MCP](https://github.com/hjlrosales/Civil3D_MCP), [civil3d-mcp](https://github.com/barbosaihan/civil3d-mcp).

## Catálogo de plugins

A pesquisa no catálogo disponível neste chat por Autodesk, Civil 3D, AutoCAD e MCP não retornou uma integração específica para Civil 3D. O resultado não comprova ausência em todos os catálogos; outras opções podem aparecer no [diretório de plugins](https://chatgpt.com/plugins).

## Recomendação para esta máquina

Avaliar primeiro o Horizun por oferecer operações definidas para objetos de engenharia. A documentação declara integração com Codex, mas o funcionamento nesta instalação ainda precisa ser comprovado. A habilidade PROJETA VIÁRIO — IFM complementa a integração com as convenções já aprovadas pelo usuário.

Para concretizar uma instalação futura: conferir a versão/atualização exata do Civil 3D e o pacote publicado; revisar instalador e dependências; configurar a ponte e seu registro no cliente; confirmar uma chamada real com o desenho identificado. O primeiro ensaio deve listar alinhamentos, perfis, superfícies e estilos INFRA em um desenho de teste. Só depois testar uma alteração reversível e verificar os objetos resultantes. Não alterar a template para realizar esse ensaio.

A instalação já existente do Assistente PROJETA é independente de uma ponte MCP: seus comandos locais não fornecem, por si só, acesso remoto ao desenho para este chat.
