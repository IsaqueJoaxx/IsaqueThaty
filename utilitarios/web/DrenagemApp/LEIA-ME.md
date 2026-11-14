# App de Dimensionamento de Drenagem

Aplicativo de página única (um arquivo HTML) baseado nas planilhas
`DimensionamentoDrenagem.xls`, `Dim_REDE_COLETORA.xls` e `BUEIROSGREIDE.xls`.

## Como usar
Dê **duplo-clique em `index.html`** — abre em qualquer navegador (Chrome, Edge, Firefox).
Não precisa instalar nada e funciona offline. Os dados ficam salvos no próprio navegador.

## Abas
1. **Projeto / Chuva (IDF)** — identificação da obra e a curva IDF `i = a/(tc+b)^c`.
   Presets já calibrados a partir das suas planilhas:
   - Bueiros de Greide (Tr=10): a=19,1849 · b=10,5 · c=0,660
   - Valeta Trapezoidal: a=30,6458 · b=15,0 · c=0,790
   - Valeta de Banqueta: a=27,4144 · b=15,0 · c=0,755
   - Rede Coletora: a=6,5659 · b=4,0 · c=0,375
2. **Vazão (Racional)** — `Q = Σ(A·C)·i/6` (A em ha, i em mm/min → Q em m³/s).
3. **Sarjeta / Valeta (Trapezoidal/Triangular)** — profundidade normal por Manning,
   velocidade, Froude, regime e capacidade da seção cheia. Séries DR.
4. **Canal Retangular** — capacidade com 15% de borda livre, yn, yc, Froude.
5. **Bueiro de Greide (Circular)** — capacidade a seção plena, lâmina h, y/D, velocidade, Froude.

## Fórmulas
- **Racional:** Q = Σ(A·C)·i/6 (equivale a C·i·A/360 com i em mm/h)
- **Manning:** Q = (1/n)·A·Rh^(2/3)·S^(1/2)
- **Froude:** F = V/√(g·y) → F<1 subcrítico, F>1 supercrítico

## Validação
Os cálculos foram conferidos contra valores das próprias planilhas (ex.: seção retangular
B=2,5 H=2,5 → Qcap=15,9118 m³/s; valeta trapezoidal Froude=1,3955). Resultados batem
dentro do arredondamento.

> Ferramenta de apoio. Sempre confira os resultados conforme as normas DNIT/DER aplicáveis.

## Botões úteis
- **Salvar projeto** — guarda no navegador.
- **Exportar/Importar .json** — leva o projeto para outro PC.
- **CSV** (em cada aba) — abre no Excel.
- **Imprimir / PDF** — gera relatório.
