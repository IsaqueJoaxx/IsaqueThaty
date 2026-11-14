# Compactacao GO-220

O arquivo GO-220_Compactacao_95_100.dwg e uma copia configurada do DWG recebido. O original nao foi alterado.

## Regra aplicada

Na regiao entre as superficies natural e de projeto, o primeiro metro abaixo da envoltoria superior pertence a categoria 100%; o excedente pertence a categoria 95%. No corte, a envoltoria superior e o terreno natural. No aterro, e o projeto. Os percentuais sao categorias informadas pelo usuario, sem fatores de empolamento ou multiplicacao por 0,95.

O calculo usa a geometria amostrada das secoes existentes, inclusive os pontos de cruzamento entre superficies e os pontos em que a diferenca atinge um metro. Nao interpola atraves de buracos nas secoes. As areas estao em m2; os volumes entre secoes usam a media das areas das extremidades multiplicada pela distancia em progressiva, em metros. Este volume pode diferir de um calculo TIN tridimensional ou de criterios QTO diferentes.

Nao foi calculado um volume adicional abaixo da superficie de projeto: o limite solicitado foi a regiao entre primitivo e projeto. Nao foi criada uma camada nova de subleito, pois a espessura e os limites dessa camada nao foram definidos. O subleito de uma outra imagem nao constitui um dado geometrico deste DWG.

## Desenho entregue

- Mantem as duas superficies e as vistas nativas existentes.
- Adiciona preenchimentos nas camadas COMP_100 (azul, ACI 5) e COMP_95 (ACI 30), conforme o modelo visual enviado.
- Adiciona tabelas de duas colunas com Estaca, Area e as quatro categorias de corte/aterro. A estaca usa comprimento de 20 m.
- Oculta a exibicao dos MaterialSection e das tabelas QTO antigas na copia, para evitar sobreposicao dos valores anteriores. Os objetos e as listas nativas permanecem no arquivo.
- Os preenchimentos e as novas tabelas sao objetos AutoCAD estaticos. Nao sao novos materiais nativos de QTO e nao atualizam sozinhos quando as superficies mudam.
- O DWG foi gerado e reaberto no mecanismo Civil 3D/AutoCAD 2026 instalado. O formato DWG e compativel com 2027. A verificacao confirmou que as superficies, secoes e vistas nativas continuam presentes. Nao houve revisao visual manual de cada prancha.

## Repetir no Dynamo do Civil 3D 2027

1. Abra uma copia do DWG de origem ou o DWG entregue. Atualize a amostragem das secoes no Civil 3D se tiver alterado as superficies.
2. Abra Compactacao_95_100_GO220.dyn no Dynamo, em modo Manual. O codigo Python esta incorporado; nao exige os outros arquivos .py.
3. Use o mecanismo PythonNet3. Os nomes das duas superficies e do grupo ja estao preenchidos.
4. Com Aplicar preenchimentos e tabelas = False, execute para consultar areas e volumes.
5. Para atualizar os preenchimentos e tabelas, altere Aplicar para True e execute. Salve o desenho no Civil 3D depois de conferir.

As camadas COMP_95, COMP_100 e COMP_TABELA sao exclusivas da rotina: seus objetos sao substituidos a cada aplicacao. A rotina nao salva automaticamente. No desenho de origem, ela nao oculta as tabelas QTO antigas; essa ocultacao ja foi feita na copia entregue. Nao confunda as tabelas antigas de materiais com as novas categorias.

O motor geometrico Python foi verificado com os dados das 1.086 secoes do DWG e com exemplos de corte, aterro de 1,20 m e secao mista. O no Python do Dynamo nao foi executado interativamente nesta sessao; pode precisar de ajuste de interoperabilidade conforme a atualizacao do Dynamo instalada. O DWG entregue ja contem o resultado calculado, independentemente da execucao do grafo.

## Quantitativos

Consulte quantitativos.csv para areas por secao e volumes do intervalo anterior. Os totais nao devem ser usados como volume de tratamento de solo remanescente abaixo do projeto.
