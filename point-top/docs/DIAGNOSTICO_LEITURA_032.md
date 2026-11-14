# Etapa de leitura 0.3.2 — POINT TOP

Confirmado em sessão do Civil 3D: PTDSTATUS terminou com STATUS_DONE; PTDSELECIONAR retornou OK e terminou com SELECT_DONE. Log diagnostico-36040.log. Esses testes não identificam a causa do fechamento em PTVINCULAR; confirmam apenas o carregamento do diagnóstico mínimo e a seleção genérica.

Próxima etapa preparada, ainda sem execução no host: carregar TESTE_LEITURA/PointTop.Leitura2027.dll pelo NETLOAD e executar PTLEREIXO em uma cópia de teste que contenha um alinhamento nativo.

O comando usa seleção genérica sem AddAllowedClass. O acesso aos tipos Civil foi separado em um método sem inline, chamado somente depois da seleção, com registros antes/depois de abrir objeto ForRead, converter para Alignment, ler Name, Length e unidades. Não lê perfis, não coleta pontos, não registra eventos nem reconstrói. Não escreve no DWG. Publica somente log local leitura-PID.log com flush. Transação de leitura descartada sem Commit.

A DLL possui identidade e nome de comando novos; não substitui os componentes já carregados. Compilação bem-sucedida não significa teste em sessão. Não executar PTVINCULAR antigo. Próximo critério de aceite: READ_RESULT e COMMAND_DONE com nome e comprimento do eixo. Falha nativa continua possível; o registro delimita a última etapa concluída, mas não substitui o dump de erro da Autodesk.

## Resultado em 06/11/2026

PTLEREIXO executado em `accoreconsole`, em cópia descartável (`copia_infraworks.dwg`): READ_RESULT `ALINHAMENTO - 29 | Comprimento: 4.300,901 | Unidade do DWG: Meters` e COMMAND_DONE. Log `leitura-45432.log`. O critério de aceite foi atendido no console; a execução na janela do Civil 3D não foi feita.
