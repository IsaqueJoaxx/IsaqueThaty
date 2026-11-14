# Rotina Dynamo — PDF de Seções / Notas de Serviço DER-SP

Civil 3D 2026 · Dynamo · nó **Python Script** em **CPython3**

---

## Arquivos

| Arquivo | Função |
|---|---|
| `00_INSPECIONAR_desenho.py` | Descobre o nome real do bloco da moldura, os TAGs de atributo e os nomes canônicos de papel A4. **Rode primeiro.** |
| `01_GERAR_PDF_secoes.py` | Rotina principal. Um PDF por seção, janela + ajustar à folha. |
| `02_FALLBACK_gerar_script_scr.py` | Plano B: gera um `.scr` com comandos `-PLOT` para rodar no AutoCAD. Use se o PlotEngine falhar. |

---

## Passo 1 — Inspecionar

1. Abra o DWG com as seções no Civil 3D.
2. `Gerenciar` → `Dynamo`.
3. Novo gráfico → arraste um nó **Python Script** → duplo clique.
4. No canto do editor, troque o engine para **CPython3**.
5. Cole o conteúdo de `00_INSPECIONAR_desenho.py` → `Save Changes`.
6. Ligue a saída num nó **Watch** → `Run`.

Anote três coisas do resultado:

- **Nome do bloco** da moldura (o que tiver ~350 inserções)
- **TAG do atributo** com a estaca (ex.: `ESTACA`, `EST`, `SECAO`)
- **Nome canônico do papel**, ex.: `ISO_full_bleed_A4_(297.00_x_210.00_MM)`

> Se a moldura **não** for um bloco (só linhas soltas), o script não vai listar nada útil — me avise que adapto para polilinha/layer.

---

## Passo 2 — Montar o gráfico principal

Novo nó **Python Script**, com **6 entradas** (botão `+` no nó, nesta ordem):

| # | Nó de entrada | Exemplo |
|---|---|---|
| IN[0] | `String` | `MOLDURA_A4` |
| IN[1] | `String` | `C:\OBRA\PDF_SECOES` |
| IN[2] | `String` | `ISO_full_bleed_A4_(297.00_x_210.00_MM)` |
| IN[3] | `String` | `ESTACA` (ou vazio) |
| IN[4] | `String` | `NS_` (ou vazio) |
| IN[5] | `Boolean` | **False** no começo |

Cole `01_GERAR_PDF_secoes.py`, engine **CPython3**, saída → **Watch**.

---

## Passo 3 — Simular antes de plotar

Com `IN[5] = False` o script **não plota nada**. Ele só lista:

```
Molduras encontradas: 347
Papel 297 x 210 mm | moldura 0.30 x 0.21 un | rotacao 0
------------------------------------------------------
   1 | 0+00                     -> NS_0+00.pdf
   2 | 0+20                     -> NS_0+20.pdf
   3 | 1+00                     -> NS_1+00.pdf
```

Confira a **contagem**, a **ordem** e os **nomes**. Só depois vire `IN[5]` para **True**.

---

## O que a rotina faz

- Varre o Model Space e pega toda `BlockReference` com o nome informado (resolve blocos dinâmicos).
- Usa `GeometricExtents` de cada bloco como área de janela da plotagem.
- Ordena por estaca quando o atributo é numérico (`120+10` → 2410), senão em grade: esquerda→direita, cima→baixo.
- Detecta rotação sozinha: compara a proporção da moldura com a do papel e aplica 90° se necessário.
- Trata nomes duplicados (`NS_0+00.pdf`, `NS_0+00_(2).pdf`).
- Força `BACKGROUNDPLOT = 0` e restaura o valor original no final.
- Erra por folha, não por lote: se uma seção falhar, as demais continuam e a falha aparece no relatório.

---

## Ajustes finos (topo do arquivo `01_`)

```python
DISPOSITIVO = "DWG To PDF.pc3"
ESTILO_PLOT = ""      # ex: "monochrome.ctb" para PB
MARGEM      = 0.0     # folga ao redor da moldura, em unidades de desenho
TOL_LINHA   = 0.35    # tolerância p/ agrupar molduras na mesma linha da grade
```

---

## Se der problema

| Sintoma | Causa provável | Solução |
|---|---|---|
| `NENHUMA moldura encontrada` | Nome do bloco errado, ou a moldura não é bloco | Rode o `00_` e confira |
| PDF sai cortado / em branco | Nome de mídia errado | Copie o canônico exato do `00_` |
| Seção sai deitada | Rotação automática errou | Force `rotacao = PlotRotation.Degrees090` |
| `eInvalidInput` no `Validate` | Mídia inexistente no dispositivo | Troque para outro nome de papel A4 |
| `ja esta em processo de plotagem` | Plotagem em 2º plano pendente | Feche a fila de plotagem e rode de novo |
| Dynamo trava / erro no PlotEngine | Conflito de contexto do Dynamo | Use o `02_FALLBACK` |

### Sobre o Plano B (`.scr`)

O Dynamo apenas **escreve** o script; quem plota é o AutoCAD via comando `SCRIPT`. É a rota mais estável.

Ressalva: a sequência de perguntas do `-PLOT` varia por versão e idioma. Se travar no meio, rode `-PLOT` manualmente uma vez, anote a ordem real das respostas e ajuste a lista `linhas` no script.

---

## Escala

Ficou em **Scale to Fit** (ajustar à janela), conforme escolhido. Se as molduras já estiverem dimensionadas em mm de papel e você quiser fidelidade métrica exata, troque no `01_`:

```python
psv.SetUseStandardScale(ps, True)
psv.SetStdScaleType(ps, StdScaleType.StdScale1To1)
```
