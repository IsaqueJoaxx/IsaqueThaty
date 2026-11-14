# IRI RODOVIÁRIO — IFM

Prévia 19, versão 0.19.0, para Civil 3D 2026 e 2027.

O assistente analisa resultados de IRI já calculados por levantamento. Abra pela aba PROJETA, botão IRI RODOVIÁRIO — IFM, ou digite PROJETAIRI. Também está disponível como aba do painel existente.

## Entrada

Importe um CSV UTF-8 ou cole os dados com o cabeçalho:

```csv
rodovia;faixa;sentido;trilha;inicio_m;fim_m;iri_m_km
EXEMPLO;1;Crescente;Direita;0;100;2,1
EXEMPLO;1;Crescente;Direita;100;200;3,4
EXEMPLO;1;Crescente;Esquerda;0;100;2,3
```

Os dados acima são fictícios. Use ponto e vírgula ou tabulação; decimal com vírgula ou ponto, sem separador de milhares. Início e fim são distâncias acumuladas em metros, com fim maior que início, inclusive no sentido decrescente da rodovia. Converta quilômetros ou estacas antes de importar. Rodovia, faixa, sentido e trilha são obrigatórios e a grafia identifica cada série; padronize os nomes antes da análise.

## Resultados

Cada combinação rodovia/faixa/sentido/trilha possui sua própria extensão medida, média ponderada por extensão, máximo e número de lacunas. A média é soma(IRI × extensão) / soma(extensão). Lacunas não são preenchidas. Trechos sobrepostos e duplicados são rejeitados dentro da mesma série. Séries diferentes podem cobrir a mesma quilometragem.

A comparação é opcional. Informe o limite em m/km e sua origem (norma, contrato ou critério). Valores iguais ao limite entram em “até o limite”; valores maiores entram em “acima do limite”. A extensão acima do limite corresponde aos segmentos cujo IRI agregado supera o limite, não à localização de irregularidades dentro desses segmentos. Não há limite normativo predefinido nem declaração de aprovação.

Exportar análise CSV grava os trechos com o limite, a referência e a comparação. Os resumos ponderados são exibidos no painel. Alterar os dados ou o critério invalida os resultados anteriores e exige nova análise.

## Escopo e referências

Esta versão não calcula IRI por simulação de quarto de carro, não transforma greide de projeto ou superfície TIN em medição do pavimento, não cria entidades no DWG e não vincula automaticamente o levantamento ao eixo. Os dados são identificados como externos, mesmo ao trocar de desenho.

Referências técnicas consultadas:

- [DNIT 442/2023 PRO — levantamento do perfil longitudinal com perfilômetro inercial](https://www.gov.br/dnit/pt-br/assuntos/planejamento-e-pesquisa/ipr/coletanea-de-normas/coletanea-de-normas/procedimento-pro/dnit_442_2023_pro.pdf)
- [World Bank Technical Paper 46 — definição e cálculo do IRI](https://documents.worldbank.org/curated/en/851131468160775725/pdf/multi-page.pdf)

## Pacote e instalação

Compilação: bin/Previa19-IriIFM/2026 e bin/Previa19-IriIFM/2027. O pacote inclui o assistente PROJETA completo, com as abas anteriores e a nova aba IRI.

Para atualizar a instalação existente, feche o Civil 3D e execute, no PowerShell, a partir da pasta do projeto:

```powershell
.\Instalar.ps1 -Previa Previa19-IriIFM
```

O instalador existente atualiza o bundle em Program Files e solicita elevação do Windows. Não carregue simultaneamente duas cópias do assistente. Após instalar, abra o Civil 3D e use PROJETAIRI.

Verificação: compilação para ambos os anos sem avisos ou erros e 21 verificações automatizadas do núcleo. Abertura, aparência e interação dentro do Civil 3D ainda não foram testadas. Esta entrega não atualizou a instalação do usuário.
