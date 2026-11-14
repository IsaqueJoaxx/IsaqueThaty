# GREIDE AUTOMÁTICO — IFM: integração inicial

## Entrega

Nova aba Greide — IFM e botão na faixa PROJETA. Base de desenvolvimento compilada em `bin\Previa09-GreideIFM\2026` e `bin\Previa09-GreideIFM\2027`, versão 0.9.0. Esta entrega ainda não foi instalada; a instalação anterior permanece na Previa08. Não usar o instalador atual para esta entrega: seus metadados ainda correspondem à versão 0.8.

Esta é uma integração parcial e revisável da rotina enviada, limitada à **prévia das tangentes e PIVs**. Não cria perfil, não grava arquivos externos, não altera desenhos e não afirma atendimento integral ao DER-MG. Não executa o Dynamo nem requer Python dentro do Civil 3D.

## Uso após carregar a nova DLL em uma sessão nova

Abra a aba **Greide — IFM**, ou digite `PROJETAGREIDE`. Escolha explicitamente o eixo e um perfil de terreno (EG) pertencente a ele. Informe passo de leitura, distância mínima entre PIVs e tolerância. A cota inicial vem do terreno; a cota final não é imposta. Clique em **Gerar prévia de tangentes**.

O painel apresenta progressivas em metros, cotas e rampas de saída. As rampas são limitadas ao máximo informado, que não pode exceder 7%. Isso é um requisito do usuário, não uma alegação de que todas as classes de rodovia do DER-MG permitem 7%.

Os valores iniciais 40 m, 0,20 m e 10 m vêm da rotina enviada e não foram certificados como valores normativos. Se a tolerância ou a distância mínima não forem atendidas, o painel informa a pendência. Não há botão para criar um perfil nesta fase. Não há otimização de volumes, concordância com greides de cruzamentos, ajuste da cota final, curvas verticais, rampa mínima ou validação de visibilidade/drenagem.

## Diferenças em relação ao Dynamo

Origem preservada: `D:\Claude ai\Dynamo rotinas\_versoes antigas\GreideAutoB.dyn`.

- Sem seleção implícita do primeiro alinhamento ou de um perfil arbitrário.
- Rampa máxima de 8% da rotina substituída pelo teto de 7% solicitado; o usuário pode reduzir o teto.
- Sem importação automática das tabelas K da rotina, cuja correspondência normativa não foi comprovada.
- Sem encurtar curvas abaixo de K mínimo; curvas não são geradas nesta etapa.
- Sem criação parcial de perfil após falhas de API.
- Sem gravação automática de CSV/TXT ou modificação do Dynamo original.
- Núcleo de tangentes adaptado: ajusta a inclinação por mínimos quadrados a partir de cada PIV, limita a rampa e verifica o desvio nas amostras. Não replica a relaxação de tolerância em 35% usada no Dynamo. Resultados podem diferir da rotina original.

## Referências e critérios pendentes

Referência indicada pelo usuário: DER-MG. Encontrado localmente `D:\PROJETA ENGENHARIA\DER-MG\VolVI\Vol_VI_GeomTerraplenagem2013.pdf`. Na página PDF 9, seção 4.2 (página impressa VI/12), o documento determina a definição das características limites com a fiscalização e a adoção das normas DNIT/DER-MG quando especificadas. Não fornece, nessa seção, uma regra universal de rampa de 7%.

O PDF adicional enviado, **Controle geométrico de terraplenagem — Orientação de Controle de Terraplenagem segundo as Normas do DNIT e DER/MG**, é um relatório acadêmico de pós-graduação, de Márcio Marques Moreira Rocha, FEAMIG, 2012, conforme capa e folha de rosto. Contém discussão de greide e curvas verticais; serve como referência complementar. Não é uma norma oficial publicada pelo DER-MG. As tabelas e fórmulas devem ser rastreadas às fontes primárias aplicáveis antes de codificar critérios normativos.

Para concluir o gerador: definir classe da via, relevo, velocidade diretriz, edição/documentos normativos, critérios de visibilidade, rampas mínima/máxima e restrições de início/fim/cruzamentos. Validar as curvas, bloquear sobreposição e insuficiência de K, apresentar geometria completa para revisão e só então habilitar criação transacional com desfazer e estilos INFRA apropriados.

Portal oficial consultado: https://der.mg.gov.br/institucional/legislacao/normas-tecnicas-deer

## Verificações

- Compilado para Civil 3D 2026 e 2027: zero avisos e zero erros; registro `compilacao-previa09-greideifm.log`.
- Núcleo matemático: 11 cenários automatizados, incluindo rampas 0%, ±2/7/20% conforme casos, intervalo de ponta a ponta, aviso de desvio em terreno íngreme, recusa de rampa de 8%, NaN, distância mínima nula e progressivas repetidas. Os cenários efetivos de inclinação são 0%, +2%, +7%, -7%, +20% e -20%.
- Não testado em sessão Civil 3D; leitura via API, aparência da nova aba e resultados sobre desenho real ainda precisam de conferência.
- Sem teste de conformidade normativa. Rampa limitada em tangentes não equivale a um greide final aprovado.

Fontes anteriores à integração preservadas em `_versoes antigas\Antes-Greides-IFM`. As prévias compiladas anteriores foram preservadas.

## Ajuste solicitado: menos PIVs

Simplificador remove PIVs intermediários quando a nova tangente respeita a rampa, a tolerância em relação à proposta original e não aumenta o pior desvio do terreno além do já existente ou da tolerância. Os extremos são preservados. A comparação com a proposta original evita acumulação de erro por remoções sucessivas. Não garante número global mínimo de PIVs. O usuário definiu que colocará as curvas verticais manualmente. A prévia segue sem criação de perfil.

Verificação atualizada: 12 cenários passaram, incluindo retirada dos PIVs colineares em terrenos que excedem o teto de rampa e preservação de uma quebra necessária. Compilação final de ambas as versões: zero avisos e zero erros. Ainda sem teste dentro do Civil 3D.


Atualização de instalação: prévia 0.9.0 instalada com autorização do usuário. Manifesto e DLLs de 2026/2027 verificados por comparação com a compilação, carregamento na abertura habilitado. Ainda sem teste em sessão Civil 3D.


## Prévia 10 — criação de tangentes autorizada

Substitui a limitação anterior de somente prévia. Botão Criar greide no desenho cria um perfil FG por layout, com tangentes fixas contíguas nos PIVs revisados; sem curvas automáticas. Nome padrão 2 — GREIDE IFM, nomes repetidos recebem sufixo sem substituir perfis existentes. Estilo de perfil e conjunto de rótulos são escolhidos no painel; se houver exatamente um INFRA na categoria, ele é sugerido. Isso não constitui o levantamento completo dos estilos INFRA da template.

A criação relê as amostras do perfil e compara com a prévia revisada. Mudança de dados exige nova prévia. Verifica novamente rampas e quantidade de tangentes; qualquer falha antes do Commit aborta a criação. O comando é uma operação única para desfazer. Avisos de tolerância e demais critérios ainda não validados são apresentados na confirmação. Não altera o perfil do terreno nem cria curvas ou vista de perfil.

Compilação 0.10.0 para Civil 3D 2026 e 2027: zero avisos e zero erros. Núcleo de tangentes mantém os 12 testes; criação por API ainda não testada em uma sessão Civil 3D. Teste em cópia do desenho: revisar prévia, selecionar estilos, confirmar criação, conferir PIVs/rampas no editor de geometria e usar U para verificar desfazer.



## Prévia 11 — greide que acompanha o terreno

Cálculo refeito (`GradePreview.cs`). A versão anterior encadeava tangentes a partir do PIV anterior; com o terreno acima de 7% o afastamento passava de uma tangente para a seguinte e o greide ficava atrasado em relação ao relevo.

1. Linha que acompanha o terreno sem passar da rampa máxima: entre todas as linhas com rampas dentro do limite, a de menor soma de cortes e aterros. Onde o terreno é mais suave que o limite, ela é o próprio terreno; onde é mais íngreme, corta e aterra o necessário e volta ao terreno logo depois. Resolvida de uma vez no trecho inteiro (programação dinâmica exata), então não há acúmulo.
2. Menor número de PIVs dentro da tolerância de simplificação dessa linha. Tangente mais curta que a distância desejada custa mais; só fica quando o relevo pede.
3. Pares de PIVs mais próximos que a distância desejada viram um só, no cruzamento das tangentes vizinhas, quando continua na tolerância.

Rampas nunca passam do máximo (no máximo 7%), por construção e por verificação final. Cortes e aterros acima do limite de aviso aparecem por trecho, com a indicação de terreno acima da rampa máxima; não bloqueiam prévia nem criação. Sem curvas verticais.

Painel: intervalo de leitura (até 5.000 leituras), rampa máxima, tolerância de simplificação (padrão 0,50 m), distância desejada entre PIVs (40 m, preferência), avisar corte/aterro acima de (1,00 m), começar na cota do terreno (marcado). A lista mostra o afastamento de cada PIV (+ aterro, − corte).

Verificações: compilado 0.11.0 para 2026 e 2027, zero avisos e zero erros (`bin\Previa11-GreideIFM`). Núcleo testado fora do Civil 3D: 14 terrenos (plano, ±7%, 20% preso e livre, ondulado, acidentado, trecho de 12%, serra, vala, 3 relevos realistas, passo irregular) iguais ao protótipo de referência; 400 terrenos aleatórios sem rampa acima do limite e sempre dentro da tolerância; 5.000 leituras em 6 ms. O método do passo 1 (no protótipo) conferido contra programação linear em 300 casos. No Civil 3D 2027, em janela de teste com desenho novo não salvo: leitura do perfil do terreno, prévia e criação do perfil (18 PIVs, 17 tangentes, maior rampa 6,94%). Não instalado: a instalação atual continua na 0.10.0. Para instalar: `Instalar.ps1` (já aponta para a Previa11-GreideIFM e 0.11.0).
