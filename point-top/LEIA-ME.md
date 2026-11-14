# POINT TOP — IFM · versão 0.4

Abra **ABRIR_POINT_TOP.cmd**. A interface agora usa um serviço somente local em http://127.0.0.1:48731 e o módulo de volumes está integrado ao painel.

## Calcular volumes

1. Abra Volumes GeoTIFF na barra lateral.
2. Para conhecer a função, clique em Testar GeoTIFF de exemplo. Esse teste gera dois rasters sintéticos de 4 × 4 células; não usa arquivos de obra.
3. Para uma medição, selecione explicitamente 1 — Primitivo e 2 — Projeto nas janelas do Windows. Opcionalmente escolha um limite GeoJSON.
4. Informe a tolerância e confirme cotas em metros e mesmo datum vertical. As duas superfícies precisam usar o mesmo CRS projetado em metros.
5. Clique em Calcular volumes e aguarde a conclusão. Corte, aterro e saldo aparecem na mesma tela.
6. Baixe os arquivos gerados ou encontre a pasta exclusiva da medição em Resultados. O relatório HTML pode ser aberto e impresso em PDF.

A leitura dos GeoTIFF é direta do disco, por blocos; os arquivos de entrada não são enviados ao navegador ou à internet. Downloads das saídas são transmitidos por fluxo ao navegador. O progresso acompanha blocos processados, sem estimativa de tempo restante. Não há cancelamento nesta versão. A interface não restaura o histórico ao recarregar, mas os resultados gravados permanecem no disco. Não feche o serviço durante o cálculo.

Convenção: 2 − 1; negativo = corte, positivo = aterro. Saldo = aterro − corte. Volumes geométricos, sem empolamento/compactação. Tolerância descarta |ΔZ| ≤ valor somente dos totais de corte/aterro. Limite pelo centro de célula; grades diferentes usam reamostragem bilinear. Sem limite, calcula a interseção válida.

## Ambiente desta máquina

Node.js executa o serviço local. O motor usa o Python 3.12 já distribuído com QGIS 3.44.15. NumPy 2.5.4 e Rasterio 1.5.2 foram instalados exclusivamente em runtime/packages; o QGIS não foi alterado. python-runtime.json define o executável e a pasta-base Python. Esta configuração depende do QGIS permanecer instalado nesse caminho; não é um instalador portátil.

O serviço escuta apenas 127.0.0.1 e usa uma chave por sessão nas chamadas do painel. Os resultados têm links próprios. Não compartilhe esses links durante a sessão. Não há acesso remoto, upload externo ou sincronização.

A aba de eixos mantém as funções anteriores: LandXML métrico até 20 MB, inventário, planta simplificada, projeto JSON, CSV, DXF apenas de tangentes e impressão do inventário. A planta não entra no PDF. Curvas/espirais aparecem como cordas tracejadas e não são exportadas como geometria exata.

## Validação realizada

- Oito verificações JavaScript de geometria/exportação.
- Oito testes Python de volume: sinal, NoData, limite, tolerância, múltiplas janelas, incompatibilidade, resolução, cobertura/reamostragem, superfície inclinada em grade diferente, escala e deslocamento.
- 14 verificações do leitor de sessões do Civil 3D.
- 28 testes Python do greide e do IRI, com a simulação conferida contra a resposta analítica do modelo.
- 48 verificações da integração HTTP e do processo Python: autenticação, origem, validação de entrada, uma tarefa por vez, resultado numérico e download das saídas de volumes e de greide.
- Exemplo sintético integrado: corte 48 m³, aterro 88 m³, saldo +40 m³, área válida 60 m² e área sem dados 4 m².

Ainda não foi feita comparação de medição real com Civil 3D/levantamento, nem ensaio de desempenho com arquivos grandes. Essa validação permanece necessária antes de uso em medição contratual. Validação visual/manual da tela e do seletor está separada dos testes automáticos.

## Próximas etapas

Validar o conector 0.4 na janela do Civil 3D. Depois, cancelamento cooperativo, histórico de medições e persistência dos arquivos de entrada no projeto; depois GIS/3D e perfis/seções. KML, XLSX, DWG, loteamentos automáticos e verificação normativa continuam planejados.

Arquitetura inicial: docs/ARQUITETURA_E_PLANO.md. Atualização técnica: docs/INTEGRACAO_VOLUMES_02.md.


## Greide inteligente

Analisa o levantamento, propõe conformação e greide final, gera notas de serviço e arquivos para o Civil 3D, confere o resultado e calcula IRI quando há perfil longitudinal adequado. Levantamento por estacas não gera IRI. Testado só com dados sintéticos. Detalhes em docs/GREIDE_INTELIGENTE.md.

## Conector Civil 3D 0.4

Acompanha o eixo no painel, reconstrói o corredor sob comando e confere os PVIs do greide. Foi testado por comando em cópias do desenho de teste; falta validar na janela do Civil 3D. Roteiro e resultados em docs/CONECTOR_04.md. O conector não edita o greide nem recalcula medições GeoTIFF. A DLL 0.3 da pasta civil3d não deve ser usada; histórico em docs/INCIDENTE_CIVIL3D_20261106.md.
