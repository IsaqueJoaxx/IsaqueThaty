# Integração de volumes 0.2 · POINT TOP — IFM

Esta atualização substitui as limitações da arquitetura inicial sobre ausência de Python e janela separada de volumes. A arquitetura inicial permanece como registro do plano; o estado atual está neste documento e no LEIA-ME.

## Fluxo entregue

Iniciador Windows -> Node.js (server.cjs, 127.0.0.1:48731) -> painel local -> seleção Windows -> IDs de arquivos da sessão -> pedido de trabalho -> processo Python independente -> volume_ifm.calculate -> eventos JSON por linha -> progresso/resultados no painel.

O seletor usa OpenFileDialog em um processo PowerShell com janela de console oculta. Somente os arquivos escolhidos entram no registro de IDs; o cálculo não aceita caminhos arbitrários enviados pelo painel. Não há shell no lançamento do motor Python. O usuário pode cancelar a seleção; o cálculo ainda não pode ser cancelado.

API de saúde verifica importação real de Rasterio/NumPy. Trabalho usa dois arquivos selecionados ou o exemplo sintético explícito, tolerância finita não negativa e confirmação vertical. Apenas um trabalho por vez. O estado concluído só é publicado quando o processo termina com sucesso e envia resultado. Falhas não liberam downloads de resultados parciais.

Autorização por chave aleatória de sessão, verificação Host/Origin e servidor restrito a loopback. Downloads usam chave própria do trabalho e lista fixa de nomes; são transmitidos por fluxo, sem Blob do raster no navegador. Cabeçalhos impedem incorporação em iframe e detecção automática de tipo. Não disponibiliza diretórios ou arquivos arbitrários.

## Arquivos

- server.cjs: serviço, seletores, fila única, processos e saídas.
- app/volumes.js: formulário, progresso, cartões e links.
- python/worker.py: ponte JSON e geração do exemplo GeoTIFF.
- python-runtime.json: configuração do Python existente.
- runtime/packages: dependências exclusivas do POINT TOP.
- runtime/jobs: parâmetros e resumos dos trabalhos; não são restaurados na interface nesta versão.
- Resultados/Medicao_*: raster de diferenças, relatório HTML, JSON e CSV.

O estado do formulário de volumes e suas seleções ainda não integram o JSON do projeto de eixos. Cada resultado grava caminhos de origem, CRS, resolução, tolerância, data e método; ainda não há hashes dos arquivos. Não se afirma reprodutibilidade se os arquivos de origem forem substituídos depois.

## Ambiente e ensaios

Python do QGIS 3.44.15 em apps/Python312; PYTHONHOME definido somente no processo filho. PYTHONPATH prioriza runtime/packages. Não foram alterados arquivos do QGIS. A tentativa inicial de venv não foi usada; o ambiente ativo usa dependências locais por pasta. Para distribuir a outra máquina, empacotamento e licenças/dependências precisam ser tratados em etapa própria.

Testes: node tests/core.test.cjs (8 condições), node tests/integration.test.cjs (24 condições). A integração usa a porta 48731: execute sem uma instância já aberta. Testes de volume usam o Python configurado com PYTHONHOME e PYTHONPATH definidos para essas pastas.

Resultado do teste real do motor, com dados sintéticos: 48 m³ de corte, 88 m³ de aterro e 40 m³ de saldo. Os quatro arquivos de saída foram retornados com sucesso. Seis testes Python passaram, inclusive NoData e múltiplas janelas.

Limites restantes: sem cancelamento, sem restauração do histórico em tela, sem medições reais validadas, sem benchmark de grande volume e sem teste de integração Civil 3D. A leitura por blocos limita arrays de trabalho, mas não garante consumo total de memória fixo para todo tipo de GeoTIFF.

Referência da implementação de processos assíncronos: https://nodejs.org/api/child_process.html#child_processspawncommand-args-options
