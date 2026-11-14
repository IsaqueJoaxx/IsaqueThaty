# VOLUME POR SEÇÕES — aba Volumes

Prévia 18 (0.18.0), para Civil 3D 2026 e 2027.

## Como usar

1. Na aba **Volumes**, em **Calcular por**, escolha **Seções do Civil 3D (áreas médias)**.
2. **Grupo de seções**: a lista traz todos os grupos de linhas de amostragem do desenho, com o eixo e o número de linhas.
3. **Seção do terreno** e **Seção do projeto**: só aparecem as superfícies já amostradas no grupo (TIN, grade e superfícies de corredor). O painel sugere o terreno pelo nome (começando por 1, ou TN/terreno) e, para o projeto, o nome começando por 2 ou a superfície de terraplenagem do corredor (Datum, terraplenagem, subleito).
4. **Da estaca / Até a estaca** (em metros): já vêm com o grupo inteiro. Mude para calcular um trecho.
5. **Calcular volumes**. Nada é gravado no desenho.

O resultado mostra os totais de corte, aterro e saldo (com os fatores, se diferentes de 1), a tabela por estaca (áreas e volumes) e os avisos. **Exportar para Excel (.xlsx)** salva o Resumo, a tabela por estaca e, se houver, a tabela da lista de materiais do Civil 3D.

Comando: `PROJETAVOLSECOES` (é o que o botão usa).

## Como calcula

- Em cada linha de amostragem, lê a seção do terreno e a do projeto que o Civil 3D já calculou.
- **Corte**: terreno acima do projeto. **Aterro**: projeto acima do terreno. A área só conta na largura em que as duas seções existem, como na lista de materiais do Civil 3D.
- **Volume**: método das áreas médias, V = (A1 + A2) / 2 × distância entre estacas vizinhas.
- Estaca de 20 m na tabela (12+5,600) e a estaca em metros na planilha.

## Conferência com o Civil 3D

Se o grupo tiver lista de materiais calculada no Civil 3D, o painel lê os volumes dela no mesmo trecho e mostra a diferença. Os volumes do Civil 3D já vêm com os fatores da lista; a diferença compara com o PROJETA nos mesmos fatores. Diferenças vêm de superfícies diferentes na lista, do método (prismoidal) ou da correção de curva.

## Avisos

- Linha sem a seção do terreno ou do projeto, ou com a seção vazia (a superfície não cobre a linha): área zero nela.
- Seção do projeto vazia em todas as linhas: a superfície não tem triângulos (numa superfície de corredor, confira se ela é construída e reconstrua o corredor).
- Intervalo bem maior que o comum no grupo (ponte, linhas apagadas): o volume desse trecho sai só das duas seções das pontas.
- Duas linhas na mesma estaca.
- Seções estáticas: não acompanham mudanças nas superfícies.

## Teste (13/10/2026, Civil 3D 2027, janela de teste, desenho novo nunca salvo)

| Caso | PROJETA | Esperado / Civil 3D |
|---|---|---|
| TN inclinado × plataforma plana de 20 m, 11 seções, 0 a 200 m | corte 2.120,000 m³, aterro 2.120,000 m³ | conta à mão 2.120 e 2.120; lista de materiais do Civil 3D 2.120 e 2.120 |
| Mesmo grupo, trecho de 40 a 160 m | 840,000 e 840,000 | conta à mão e Civil 3D: 840 e 840 |
| Lista com fatores 1,25 (corte) e 1,1 (aterro) | — | Civil 3D: 2.650 e 2.332 (= 2.120 × fatores): os volumes do Civil 3D vêm com fator |
| TN ondulado × superfície de corredor (Top), 21 seções a cada 10 m | corte 227,564, aterro 135,157 | Civil 3D: 227,564 e 135,157; maior diferença de área por estaca 3 × 10⁻¹⁴ m² |
| Duas superfícies do mesmo corredor (Top e Datum) | aparecem separadas na lista | — |

O cálculo foi testado também fora do Civil 3D contra integrais exatas (projeto inclinado cruzando o terreno, terreno ondulado, buraco na superfície, seções sem sobreposição, pontos em ordem invertida).

Achados do teste que mudaram o código:

- O Civil 3D lista, numa superfície mais estreita que a linha de amostragem, pontos soltos nas pontas da linha (sem ligação, `SegmentTo = -1`). O cálculo usa só os trechos ligados; antes disso a área saía errada.
- As seções de material da lista de materiais aparecem junto com as de superfície e apontam para a mesma superfície. Ficam de fora.
- Com atualização pendente (corredor reconstruído no mesmo comando), as seções vieram vazias na primeira leitura. O cálculo agora lê as seções uma vez antes de calcular.

Não testado: a versão do Civil 3D 2026 (só compilada) e o botão do painel instalado (a aba foi testada numa janela à parte, com o mesmo código).
