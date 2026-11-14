# REABILITAÇÃO DE PISTA — IFM · Nova concepção (22/10/2026)

O programa agora **projeta** o greide, em vez de só conferir correções informadas.
Os arquivos antigos (`diagnosticar_levantamento.py`, `preparar_secoes.py`, `gerar_conferencia.mjs`) foram mantidos sem alteração.

## Arquivos novos

| Arquivo | Função |
|---|---|
| `projetar_greide.py` | Linha de comando: lê os dados, projeta o greide e grava as saídas |
| `reabilitacao_core.py` | Leitura de levantamento/eixo, TIN em estaca × afastamento, IRI (quarto de carro), otimização |
| `saidas_ifm.py` | Nota de serviço em Excel e LandXML para o Civil 3D |
| `requirements.txt` | Dependências: `py -m pip install -r requirements.txt` |

## Jeito rápido: GERAR_NOTA_IFM.bat

Arraste para cima do `GERAR_NOTA_IFM.bat` o levantamento da pista, os de acostamento e, se tiver, o eixo `.xml`. Também dá para dar dois cliques e escolher os arquivos numa janela.

- **Como ele reconhece cada arquivo:**
  - nome com `LEV` ou `PISTA` → levantamento da pista;
  - nome com `ACOST` ou `FUNDO` → levantamento depois do rebaixo;
  - `.xml` → eixo;
  - qualquer outro nome → ele pergunta.
- **Perguntas:**
  - se a pista foi levantada **antes** ou **depois** do rebaixo;
  - sem XML, a estaca inicial (ele sugere o número do nome do arquivo, ex.: 268630).
- **Resultado:** grava em `outputs\<nome da pista>_<data-hora>` e abre a nota de serviço no Excel.

Os demais parâmetros usam os padrões abaixo. Para mudar, use a linha de comando.

## Como rodar pela linha de comando

```
py projetar_greide.py ^
   --pista  dados\BR222_268630_272055\LEV_268630_A_272055_pista.csv ^
   --fundo  dados\BR222_268630_272055\ACOST-271200_A_272390_ld.csv ^
   --fundo  dados\BR222_268630_272055\ACOST-271740_a_272510_le.csv ^
   --eixo   EIXO_BR-222.xml ^
   --pista-estado antes ^
   --saida  outputs\NOME_NOVO
```

Sem `--eixo`, o programa gera um **eixo provisório** com a linha média do levantamento. Nesse caso, informe `--estaca-inicial`, por exemplo `--estaca-inicial 268630`. Use esse modo só para prévia.

## Entradas

- **Levantamento:** `número;N;E;Z;descrição` ou `número;E;N;Z;descrição`, com `;` ou `,`. A ordem N/E é detectada pela grandeza. Também aceita o formato da coletora `9.608.922.993`.
- **`--pista`:** levantamento da pista.
  - Com `--pista-estado antes`, é considerado anterior ao rebaixo: fundo previsto = cota − `--rebaixo`.
  - Com `--pista-estado depois`, a pista já está cortada: fundo = cota levantada.
- **`--fundo`:** um ou mais levantamentos depois do rebaixo (a "planilha de acostamento"). O lado LD/LE é identificado sozinho. Ponto alto colado ao eixo, com salto de 0,15 a 0,45 m, vira **REFERÊNCIA ORIGINAL** e não entra no fundo.
- **Prioridade do fundo:** primeiro o fundo medido; depois o fundo vindo da pista.

## Critérios de projeto (todos como parâmetro)

- **Espessura mínima:** `--espessura 0.23` entre o fundo e o topo.
  - Por padrão, é exigida a cada 0,50 m da seção.
  - Com `--so-controles`, é exigida só no eixo, a 3,50 e a 5,50.
- **Seção:** 5,50 + 5,50 com `--caimento 0.02` para fora.
- **Curvas:** com R < `--raio-dispensa` (1.800 m), seção em plano único:
  - e = máx(2 %; emáx·(2·Rmin/R − (Rmin/R)²)), com `--velocidade 60` e `--emax 0.06`;
  - transição pela `--rampa-relativa 0.006`, com 2/3 na tangente.
- **Greide:** o de **menor volume** acima do limite de espessura cujo **IRI teórico**, em qualquer trecho de 100 m, fique ≤ `--iri-alvo` (padrão 1,0 m/km).
  - O IRI é calculado no eixo e nas trilhas ±0,9 e ±2,6 m, pelo quarto de carro da ASTM E1926 a 80 km/h.
  - Referência: o DNIT (IS 22/2019) recebe pavimento novo ou restaurado com IRI ≤ 2,5 m/km medido; a meta de 1,0 deixa margem para a execução.
  - `--suavidade-mm` fixa diretamente a variação de rampa entre estacas.
- **Amarrações:** `--fixar 271200=9.412` (pode repetir).
- **Folga construtiva:** `--folga 0.005`, por exemplo.

## Saídas (pasta nova; nunca sobrescreve)

- **`NOTA_SERVICO_GREIDE_IFM.xlsx`**
  - Aba NOTA DE SERVIÇO, a cada 5 m: LE 5,50 / LE 3,50 / EIXO / LD 3,50 / LD 5,50, com pista levantada, fundo, projeto e espessura.
  - Projeto e espessura são **fórmulas**: se você alterar o greide do eixo ou o caimento, tudo recalcula. Espessura abaixo do mínimo fica em vermelho.
  - Outras abas: RESUMO (parâmetros, volume, IRI, alternativas volume × IRI, curvas, conferências, alertas), IRI 100 m e PONTOS.
- **`GREIDE_IFM.xml`** (LandXML 1.2): eixo, perfil "GREIDE IFM" com PVIs a cada 5 m, linhas 3D dos bordos de projeto (PlanFeatures) e superfície "TOPO PROJETO IFM".
- **`controles.csv`, `resumo.json`:** dados completos, para conferência.

## Limites conhecidos

- **Eixo provisório:** estacas e afastamentos aproximados, e curvas estimadas pela variação de rumo. Com o LandXML oficial, raios e PC/PT são exatos.
- **Espirais do LandXML:** aproximadas por corda. Conferir quando houver.
- **Lacunas do levantamento:** onde falta fundo, a espessura sai "não verificada" e o greide é interpolado.
- **Acostamento (3,50 a 5,50):** sem levantamento após rebaixo, a cota de projeto sai pelo caimento e a espessura fica em branco.
- **IRI da pista existente:** com seções a ~5 m, é apenas indicativo (ondas curtas não são captadas).
