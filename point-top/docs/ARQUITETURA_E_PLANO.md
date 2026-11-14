# POINT TOP — IFM · Arquitetura e implementação

## Objetivo e decisão inicial

Plataforma para reunir revisão de rodovias, loteamentos e terraplenagem com dados rastreáveis. Primeiro ciclo: importar, inspecionar, calcular e emitir resultados conferíveis. Não criar automaticamente obras, lotes ou greides no primeiro MVP.

A entrega 0.1 usa HTML/CSS/JavaScript local para reduzir dependências e permitir abrir imediatamente no Windows. Volumes usam um motor Python separado, copiado com seus testes. O contrato integrado de processamento e a distribuição desktop são etapas seguintes.

## Arquitetura entregue

    ABRIR_POINT_TOP.cmd -> app/index.html -> app/ui.js -> app/core.js
    arquivo LandXML/JSON -> memória do navegador -> downloads JSON/CSV/DXF
    ABRIR_VOLUMES.cmd -> python/volume_ifm.py -> Rasterio/NumPy -> pasta de medição

app/core.js: análise de segmentos e exportação DXF de tangentes, sem dependência da interface.
app/ui.js: importação XML pelo navegador, estado do projeto, desenho 2D e exportações.
python/: motor raster autônomo e testes existentes, execução local pendente.
exemplos/: LandXML sintético de 200 m.
tests/: verificações do núcleo.

Sem serviços externos, telemetria ou chamadas de rede. Importação LandXML limitada a 20 MB para evitar travamento em arquivos muito grandes. Nome e tabelas são renderizados como texto. Nenhum caminho de arquivo é executado pela interface.

## Arquitetura alvo

Interface desktop Windows -> serviço local de trabalhos -> processos Python -> catálogo SQLite e arquivos por projeto.
Conector Civil 3D .NET separado -> intercâmbio versionado -> serviço local.
Visualização GIS/3D -> mosaicos e malhas simplificadas gerados pelo motor.

Escolha proposta: invólucro desktop com WebView e motor Python empacotado. Comparar PySide6 e um host .NET/WebView2 em um experimento antes de fixar a distribuição. Não é necessário migrar o núcleo de domínio para mudar a interface.

Projeto futuro: identificador, versão do esquema, CRS horizontal, unidade/datum vertical declarados, superfícies com papéis escolhidos, eixos, limites, cenários, arquivos de entrada e histórico. Arquivos grandes ficam no disco; o catálogo guarda referências, tamanho, data e hash calculado em segundo plano. Fontes nunca são sobrescritas. Cada cálculo produz pasta exclusiva com parâmetros, versão do motor e estado concluído/falhou/cancelado.

Contratos propostos:
- Trabalho de volume: IDs das superfícies 1/2, limite opcional, tolerância em metros e confirmações verticais.
- Evento: ID do trabalho, etapa, progresso, mensagem e caminho de saída somente na conclusão.
- Resultado: corte, aterro, saldo, áreas válidas/NoData, grade, CRS, método, parâmetros e avisos.
- Trabalho LandXML: arquivo, eixos selecionados e opções; resultado preserva tipos geométricos e unidades originais.

## Módulos e interface

Barra lateral: Projeto, Terrenos, Eixos, Terraplenagem, Loteamentos, GIS/3D, Relatórios e Integrações. Apenas Eixos, informação de Volumes e evolução estão presentes na 0.1.

Centro: planta GIS ou terreno 3D; perfil e seções em painéis vinculados na etapa futura. Lado direito: propriedades, unidade, CRS, origem e avisos. Rodapé futuro: fila de trabalhos, memória, cancelamento e histórico. Cada função terá estado Disponível, Prévia ou Planejada; os resultados mostram quando foram calculados e com quais fontes.

Rodovias: alinhar geometria, perfis, seções e quantitativos. Diferenciar PIs horizontais de PIVs verticais. Curvas verticais editadas manualmente. Teto de rampa de 7% pode ser configurado para menos, mas não certifica conformidade; parâmetros normativos precisam de referência/edição e revisão técnica.

Loteamentos: importar polígonos, calcular áreas e confrontações, revisar quadras/vias e cenários. Parcelamento automático e validação urbanística exigem regras locais e aprovação técnica; estão fora do primeiro MVP.

## Grandes arquivos

Raster: processamento por janelas fixas, cache GDAL limitado e saída tiled/BigTIFF quando necessário. A documentação do Rasterio ressalta que uma janela pode exigir leitura dos blocos físicos inteiros: arquivos de entrada mal organizados ainda podem causar grande I/O. Preparar COG/overviews como trabalho explícito em fase posterior.

O motor copiado usa blocos de 512 × 512, cache de 128 MiB e WarpedVRT quando as grades diferem. Esses limites não representam o consumo total do processo; existem buffers e listas de somas por bloco. Medir memória e tempo antes de prometer capacidade para arquivos de dezenas de GB.

LandXML: a 0.1 usa DOM e limite de 20 MB. Substituir por leitura incremental no motor para arquivos maiores. TINs e nuvens de pontos não estão implementados. Futuro: índices espaciais, níveis de detalhe, malhas paginadas e orçamento explícito de triângulos na tela. Nunca carregar um terreno completo na interface apenas para visualizá-lo.

CPU: processo separado do painel, fila com concorrência limitada e cancelamento cooperativo entre blocos. A janela Python atual usa thread e não oferece cancelamento. Não encerrar durante uma medição.

## Civil 3D

Não instalar ou substituir componentes existentes. Criar conector separado, compilado e testado por versão suportada do Civil 3D. Começar por exportação somente leitura de eixos, perfis e superfícies escolhidos. Na segunda etapa, importação/criação com prévia, confirmação e transação reversível dentro da sessão do desenho.

A instalação anterior Previa10-GreideIFM 0.10.0 permanece independente e sem alteração. Criação de greide nessa instalação ainda precisa de teste em sessão Civil 3D. Não declarar integração funcional com POINT TOP nesta entrega.

## Etapas e critérios de aceite

Estimativas indicativas para um desenvolvedor, sujeitas às amostras reais e versões Autodesk.

1. Fundação e validação — 1 a 2 semanas. Validar a 0.1 em navegador/CAD, instalar ambiente Python aprovado, executar testes raster e comparar caso real com uma referência independente. Aceite: relatório reproduzível com parâmetros e diferenças justificadas.
2. MVP desktop integrado — 3 a 5 semanas. Projetos SQLite, motor em processo separado, progresso/cancelamento, importação incremental LandXML e geometrias exatas. Aceite: reabrir projeto, cancelar sem publicar resultado incompleto e manter a interface responsiva durante arquivo grande.
3. GIS e relatórios — 3 a 5 semanas. Camadas, CRS, sobreposição, máscaras, PDF automático, XLSX e KML via transformação para WGS84. Aceite: coordenadas verificadas em pontos de controle e exportações abertas nos aplicativos de destino.
4. Civil 3D — 3 a 6 semanas. Matriz de versões, conector somente leitura, depois criação confirmada. Aceite: comparação de estaqueamento, raios, perfis e unidades com o desenho fonte; teste de desfazer.
5. GIS/3D e terrenos extensos — 4 a 8 semanas. COG, pirâmides e terreno com níveis de detalhe. Aceite: orçamento de memória medido e navegação fluida em conjunto real acordado.
6. Rodovias/loteamentos — por pacotes de 4 a 8 semanas. Perfis/seções, balanço de massas, polígonos e cenários. Aceite: casos de referência revisados por responsável técnico, sem presumir conformidade automática.

O MVP integrado termina na etapa 2; GIS/3D completo, DWG e automação de loteamentos não devem ser prometidos como parte da 0.1.

## Testes e limites atuais

Verificação automática JavaScript: extensão, continuidade, DXF de duas tangentes, descontinuidade, coordenada inválida, comprimento inválido, bloqueio DXF de curva e inventário de espiral. Sintaxe de ui.js conferida pelo Node.js.

Pendente: teste da interface real, importação de XML variado, impressão, abertura do DXF no CAD, testes Python e comparação de volumes reais. Descontinuidades usam tolerância fixa de 1 cm; não abrangem continuidade angular. Extensões vêm dos atributos do XML; não são recalculadas da curva. CRS não é interpretado pelo importador. KML fica bloqueado por ausência de transformação validada. Nenhum algoritmo geométrico do protótipo substitui uma revisão de engenharia.

## Referência técnica consultada

Rasterio, Windowed reading and writing: https://rasterio.readthedocs.io/en/stable/topics/windowed-rw.html
Consulta via documentação do projeto Rasterio. APIs Autodesk e empacotamento desktop deverão ser verificados para a versão escolhida antes da implementação do conector.
