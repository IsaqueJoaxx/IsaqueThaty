# TABELA DE VOLUMES — IFM

Prévia 12 (0.12.0), para Civil 3D 2026 e 2027.

Na aba Volumes, clique em **Tabela de volumes por estaca — IFM…** e continue na linha de comando. Também pode usar PROJETATABVOL.

O comando carrega a cópia TABVOL.lsp fornecida junto à DLL e inicia TABVOL. O arquivo original em `_FERRAMENTAS\LISP` foi preservado. O instalador copia a rotina para a pasta confiável do bundle, sem alterar SECURELOAD ou locais confiáveis.

A rotina mantém as opções originais: comparar superfícies ao longo do eixo, usar intervalos e estacas extras ou linhas de amostragem selecionadas, ou importar áreas de CSV/TXT. Usa estacas de 20 m, integra áreas na seção e calcula volumes pelas áreas médias. Os valores são arredondados a três casas, conforme a rotina original. A tabela e seu resumo são inseridos no desenho; o CSV é exportado ao lado do desenho ou em Documentos. A cópia integrada usa sufixo numérico se o nome do CSV já existe, preservando exportações anteriores.

Essa tabela não usa automaticamente as seleções, a polilinha ou os fatores da tela de volumes totais. As entradas são solicitadas pela própria TABVOL. Não equivale à integração de uma superfície de volume TIN.

Limitações herdadas: a rotina integra somente onde há cobertura comum e pode registrar área zero onde faltam dados. Linhas não reconhecidas na importação são ignoradas. A tabela não possui atualização automática quando as superfícies mudam; execute novamente após alterações. Um U desfaz a inserção no DWG, mas não remove o CSV. Confira os avisos e as áreas antes de usar os totais.

Verificação: compilação para os dois anos e conferência do empacotamento. A execução interativa da TABVOL dentro do Civil 3D ainda precisa de validação pelo usuário.
