# Medicao revisada GO-220

Arquivos desta revisao:
- GO-220_Medicao_0a300_350a400.dwg
- Medicao_GO220_0a300_350a400.xlsx
- medicao_fundo_plataforma_resumo.csv
- medicao_fundo_plataforma_secoes.csv

## Limites confirmados

Estacas de 20 m. Trechos 0 a 300 (0 a 6.000 m) e 350 a 400 (7.000 a 8.000 m). O intervalo entre as estacas 300 e 350 foi excluido, sem integrar volumes atraves dele. Foram utilizadas 358 secoes existentes, incluindo secoes intermediarias.

Largura horizontal constante de 13,40 m: offsets -6,70 a +6,70 m. Os taludes laterais nao fazem parte da medicao, conforme solicitado. Nenhum talude foi redesenhado.

O fundo de terraplanagem foi definido geometricamente por TOP -1,00 m, com espessura vertical. O pacote informado pelo usuario e composto de 0,20 m de base, 0,20 m de sub-base, 0,40 m de reforco e 0,20 m de subleito. Nao foi acrescentada espessura de asfalto. Se houver revestimento acima desse pacote e a TOP representar o topo desse revestimento, a referencia de fundo deve ser revisada antes do uso contratual.

A faixa inferior do pacote, de TOP -1,00 a TOP -0,80 m, foi considerada subleito. Sua area transversal e 13,40 x 0,20 = 2,68 m2. Os volumes geometricos do subleito sao 16.080 m3 no primeiro trecho e 2.680 m3 no segundo, totalizando 18.760 m3. A area em planta total e 93.800 m2. Estas sao grandezas geometricas; a unidade de pagamento depende do item de medicao aplicavel.

## Terraplanagem

Volume entre terreno natural e o fundo definido, apenas dentro da plataforma. As areas sao calculadas com os segmentos das secoes existentes e seus cruzamentos. Volumes por media das areas das extremidades multiplicada pela distancia entre progressivas. Categorias 100% no primeiro metro abaixo da envoltoria superior e 95% no excedente, conforme a regra solicitada. Os percentuais nao sao fatores de volume.

Nao somar o volume de subleito com o movimento de terra como se fossem um mesmo servico. As camadas do pacote sao itens separados.

## Representacao no DWG

Preenchimentos: 100% azul ACI 5; 95% ACI 30; subleito ciano ACI 4. Linhas vermelhas delimitam as camadas da plataforma. As novas tabelas das 358 secoes mostram as quatro categorias de terraplanagem e a area de subleito separada. As superficies e as vistas nativas permanecem no desenho. As tabelas QTO e as exibicoes antigas de materiais foram ocultadas na copia para evitar sobreposicao. Fora dos trechos medidos nao foram gerados os novos quantitativos.

Os preenchimentos, linhas e tabelas adicionados sao objetos AutoCAD estaticos, sem vinculo automatico com as superficies. Nao houve alteracao da superficie TOP original nem criacao de uma superficie nativa de fundo. A copia foi gerada no mecanismo Civil 3D/AutoCAD 2026 instalado e deve ser conferida visualmente no Civil 3D 2027 antes de emissao.

Esta revisao substitui a referencia de calculo dos arquivos GO-220_Compactacao_95_100.dwg e quantitativos.csv, que comparavam o terreno com TOP sem descontar o pacote. A rotina Dynamo da primeira entrega tambem usa a referencia antiga e nao deve ser executada nesta revisao sem adaptacao.
