# Greide inteligente

Situação em 06/11/2026: funcionando no painel, testado só com dados sintéticos. **Falta validar com levantamento e perfil reais** antes de usar em projeto. O resultado é rascunho técnico; a validação é do responsável.

## O que faz

1. **Analisa o levantamento:** cobertura, falhas, caimento medido, ondulação entre lances e pontos que fogem da vizinhança.
2. **Propõe o greide:** por programação linear, com o menor volume que respeita a espessura mínima em todos os pontos da seção e o limite de variação de cota entre lances.
3. **Propõe a conformação:** o greide que mais cola na pista, abaixo do final menos a espessura mínima.
4. **Confere o resultado:** recalcula a partir das cotas da nota, sem usar as restrições do otimizador.
5. **Calcula o IRI:** somente quando há perfil longitudinal adequado.

## Entradas

- **Eixo:** LandXML só com linhas e arcos. Eixo com espiral é recusado; exporte sem espirais.
- **Levantamento:** pontos em CSV/TXT, superfície LandXML ou tabela `estaca; le55; le35; eixo; ld35; ld55`.
- **Perfil para IRI, opcional:** `distancia; cota` ou `distancia; LE; LD`.

## Saídas

Ficam em `Resultados\Greide_data_hora_...`, sem sobrescrever execuções anteriores:

`Notas_de_servico.xlsx`, `Greides_e_corredor.xml`, `Corredor_3D.dxf`, `Pontos_PNEZD.csv`, `Perfil.png`, `iri.csv` (quando há IRI), `relatorio.html` e `resultado.json`.

## Conferências

| Item | Quando reprova |
|---|---|
| Espessura sobre a pista e sobre a conformação | Abaixo do mínimo em qualquer ponto |
| Rampa máxima | Acima do limite |
| Rampa mínima, se informada | Atenção quando há trecho abaixo |
| Variação de cota entre lances, no greide calculado | Acima do limite |
| Variação de cota nas cotas da nota, em mm | Atenção quando o arredondamento ultrapassa o limite |
| K mínimo convexo e côncavo | Abaixo do limite |
| Caimento transversal | Desvio maior que 0,05 %, ou acima da superelevação máxima |
| Raio mínimo do eixo | Abaixo de V² / 127 (e + f) |
| Superelevação desligada com curvas no eixo | Atenção |
| Fresagem máxima, se informada | Acima do limite |
| Cotas sem levantamento ou interpoladas | Atenção |

Os limites iniciais de rampa (6 %) e de K (30 m/%) são **premissas**, não valores de norma. Ajuste conforme a norma e o contrato da obra.

## Pontos suspeitos

O programa compara cada linha da seção com a mediana móvel de 5 estacas e aponta o que foge mais que 15 mm ou 5 vezes a dispersão robusta. Quando o ponto está acima da pista e a seção está na espessura mínima, ele aparece como "governa o greide": um erro de levantamento ali pode estar levantando o greide e custando material. O ponto é só apontado, nunca removido.

## IRI

Modelo Golden Car a 80 km/h, por transição de estados, com média móvel de 250 mm (Sayers, TRR 1501, 1995).

| Pontos do perfil | O programa faz |
|---|---|
| Até 0,30 m | Calcula o IRI |
| De 0,30 a 0,60 m | Calcula e marca como Classe 2, com viés que pode passar de 10 % |
| Acima de 0,60 m | Não calcula e explica o motivo; o resto do trabalho continua |

Também avisa sobre resolução de cotas pior que 0,5 mm, falhas no perfil (o segmento afetado fica sem IRI) e primeiro segmento sem os 20 m de aquecimento.

### Levantamento por estacas não dá IRI

Com cotas a cada 5 m, não existem ondas menores que 10 m nos dados, e o IRI é mais sensível perto de 2,4 m e 15 m. Por isso o programa nunca chama esse resultado de IRI. O que ele apresenta é a **resposta às ondas longas**: o mesmo modelo, aplicado ao perfil das estacas, para comparar a pista existente com a conformação e o greide final no eixo.

Esse número serve como piso do que a geometria impõe. A pista pronta terá também as ondas curtas da execução, então o IRI medido depois será maior.

## O que foi testado

- **IRI:** a simulação confere com a resposta analítica do modelo em ondas de 1,5 a 30 m, com pontos a cada 0,05 a 0,25 m, com margem de 2 % a 4 %. Os picos de sensibilidade aparecem perto de 2,4 m e 15 m. A referência consultada não traz um conjunto de dados de saída para comparação, então ainda não houve conferência contra outro programa de IRI nem contra perfilômetro.
- **Greide:** a espessura foi recalculada a partir da planilha entregue, sem usar o otimizador.
- **Conferências:** erros inseridos de propósito (espessura, rampa, K, caimento e raio) foram detectados.
- **Pontos suspeitos:** um erro de 8 cm inserido no exemplo foi encontrado e marcado como governando o greide.
- **Painel:** o exemplo sintético foi executado pelo botão da tela, do clique aos arquivos.

## Limites conhecidos

- **Arredondamento:** as cotas da nota são arredondadas ao milímetro, e isso pode levar a variação de 2,5 mm para cerca de 4 mm entre lances. A conferência mostra as duas medidas.
- **Curvas verticais:** o greide é uma sequência de cotas por estaca, com curvatura limitada; não há PVIs com curvas parabólicas ajustadas.
- **Espirais:** não são aceitas no eixo.
- **Dependências:** usa `scipy`, `matplotlib` e `openpyxl` do QGIS e o `ezdxf` do Python do usuário. Sem o `ezdxf`, apenas o DXF deixa de ser gerado, com aviso.
