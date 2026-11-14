# Diagnóstico isolado 0.3.1 — PTVINCULAR

## Informação nova confirmada pelo usuário

O Civil 3D fechou ao executar PTVINCULAR, antes de surgir qualquer pedido de seleção. Isso delimita a falha à entrada do comando ou preparação do primeiro prompt. Não confirma a instrução exata, uma falha de filtro de seleção, tipo de objeto, inicialização, ambiente ou conflito entre extensões. Não há dump/stack trace disponível. Não é correto atribuir o fechamento ao rebuild do corredor, que só poderia ser agendado após seleção e ativação.

## Preparação concluída

Foi criada uma DLL de diagnóstico com identidade e comandos diferentes, sem sobrescrever a DLL anterior. A versão 0.3 continua suspensa.

DLL: civil3d-diagnostico/bin/Release/net10.0-windows/PointTop.Diagnostics2027.dll.
Comandos:
- PTDSTATUS: registra entrada do comando e exibe uma mensagem. Não seleciona nem abre entidades.
- PTDSELECIONAR: seleção genérica de entidade, sem filtro Alignment/Corridor, sem abrir a entidade em transação.

O diagnóstico não referencia AeccDbMgd, não mantém ObjectIds estáticos, não assina eventos, não cria vínculos, não reconstrói corredor e não escreve no DWG. A única escrita intencional é no log local runtime/civil3d-diagnostics/diagnostico-PID.log. As etapas são gravadas em disco antes e depois das operações, com flush. Exceções gerenciadas são registradas; isso não captura nem impede toda falha nativa do host.

## Validação

Compilação contra o AutoCAD/Civil 3D 2027 instalado. O êxito de compilação não comprova que o carregamento e os comandos funcionarão no host. A nova DLL não foi carregada automaticamente. Não foi modificado o DWG do usuário. Não é uma correção confirmada da falha nem restabelece a função de alinhamento inteligente.

## Próxima etapa controlada

Em uma sessão sem a DLL antiga e com um desenho vazio, carregar somente PointTop.Diagnostics2027.dll e executar PTDSTATUS. Primeiro conferir o log; não avançar automaticamente para seleção. Se entrada/saída do comando forem confirmadas, testar PTDSELECIONAR em uma linha comum de um desenho descartável. Só depois introduzir os tipos Civil 3D e as leituras, em etapas separadas.

Não instruir a repetir PTVINCULAR na versão antiga. Preservar quaisquer detalhes do relatório Autodesk para estabelecer a causa do fechamento.
