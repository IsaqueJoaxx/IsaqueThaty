# Greide Automático — LISP + Python

Duas peças que resolvem a mesma coisa por caminhos diferentes:

| Arquivo | Onde roda | O que faz |
|---|---|---|
| `GreideAuto.lsp` | Dentro do Civil 3D | Pergunta a cota inicial, a rampa máxima e a velocidade; lê o perfil do terreno do alinhamento; gera os PIVs e as curvas verticais; cria o perfil de projeto no desenho. |
| `greide_auto.py` | Fora do CAD (Python 3.8+) | Mesmo cálculo, com relatório completo, gráfico e **LandXML** com tangentes + curvas prontas para importar. |

A LISP resolve o dia a dia. O Python é o caminho garantido quando você precisa que as **curvas verticais** entrem no desenho já dimensionadas — a API COM do Civil 3D é irregular na criação de curvas por script, então a LISP tenta duas rotas e, se nenhuma passar, ela cria o greide só com as tangentes e avisa. O LandXML nunca falha nisso.

---

## Como o greide é gerado

1. O perfil do terreno é amostrado de metro em metro (passo configurável).
2. A linha parte da **cota inicial** informada.
3. Cada tangente recebe a rampa que melhor se ajusta ao terreno (mínimos quadrados ancorados no PIV anterior), **truncada na rampa máxima** e, se você quiser, com uma **rampa mínima** de drenagem.
4. A tangente cresce enquanto o desvio vertical em relação ao terreno ficar dentro da tolerância. Quando estoura, nasce um PIV.
5. Em cada PIV entra uma **parábola simétrica** com `L = K × A`, sendo K o mínimo da velocidade diretriz e A a diferença algébrica de rampas em %. Aplica-se também `L ≥ 0,6·V` (conforto/estética do DNIT).
6. Curvas que se sobrepõem na mesma tangente são reduzidas proporcionalmente e o relatório avisa sempre que o K resultante cair abaixo do mínimo.

Os três parâmetros que mais mudam o resultado:

- **Rampa máxima** — quanto menor, mais o greide se descola do terreno (mais corte/aterro).
- **Tolerância** — desvio vertical aceito entre a tangente e o terreno. Valores pequenos (0,10–0,20 m) geram muitos PIVs e colam no terreno; valores maiores (0,40–0,60 m) geram um greide mais limpo.
- **Tangente mínima** — o menor trecho reto entre dois PIVs. Segure em 40–60 m para via urbana e mais em rodovia; é o que impede o greide de virar serrote.

---

## Caminho 1 — dentro do Civil 3D

**Instalar:** `APPLOAD` → carregue `GreideAuto.lsp` (marque em *Startup Suite* para carregar sempre).

**Comandos:**

- `GREIDEAUTO` — o comando principal
- `GREIDEEXP` — só exporta o perfil do terreno para CSV (entrada do Python)
- `GREIDECFG` — mostra e zera os parâmetros memorizados na sessão

**Fluxo do `GREIDEAUTO`:**

1. Clique no alinhamento.
2. Escolha, na lista, qual perfil é o do terreno (ele já sugere o que tem "EG", "TERR" ou "SUPERF" no nome).
3. Responda os prompts — todos têm valor padrão entre `< >`, é só dar Enter para aceitar:
   - corda da estaca (20 m)
   - **cota inicial** (sugere a cota do terreno na estaca 0)
   - **rampa máxima (%)**
   - rampa mínima para drenagem (0 = sem mínima)
   - velocidade diretriz
   - tangente mínima, tolerância, passo de amostragem
   - **tipo de curva**: 1 = L por K mínimo · 2 = K informado por você · 3 = L fixo em todos os PIVs
4. A rotina imprime a tabela de PIVs, as verificações e o corte/aterro máximo na linha de comando.
5. Grava, na pasta do desenho:
   - `<alinhamento>_terreno.csv` — entrada do `greide_auto.py`
   - `<alinhamento>_greide_pvis.csv` — tabela de PIVs para a memória de cálculo
   - `<alinhamento>_greide_pvis.txt` — formato do **Create Profile from File** do Civil 3D
6. Pergunta se cria o perfil no desenho. Se sim, cria o perfil de projeto com os PIVs e tenta aplicar as curvas verticais.

Se a mensagem final disser que nem todas as curvas foram aplicadas, use o caminho 2 — o resultado é idêntico, com as curvas certas.

---

## Caminho 2 — LandXML (garantido, com curvas)

Precisa de Python 3.8+. `matplotlib` é opcional, só para o gráfico:

```bash
pip install matplotlib
```

### 2a. A partir do CSV que a LISP exportou

```bash
python greide_auto.py --csv "Rua das Hortensias_terreno.csv" ^
       --cota-inicial 745.20 --rampa-max 8 --rampa-min 0.5 --vel 40 ^
       --tangente-min 40 --tolerancia 0.20
```

### 2b. A partir de um LandXML exportado do Civil 3D (recomendado)

No Civil 3D: **Output → Export to LandXML**, marcando o alinhamento e o perfil do terreno. Depois:

```bash
python greide_auto.py --landxml eixo.xml --alinhamento "Rua das Hortensias" ^
       --cota-inicial 745.20 --rampa-max 8 --vel 40 --injetar
```

O `--injetar` grava o perfil de projeto **dentro do mesmo alinhamento** do arquivo original. Volte ao Civil 3D em **Insert → LandXML**, aponte o `eixo_greide.xml` e o greide entra amarrado ao eixo existente, com tangentes e curvas.

### Opções úteis

| Opção | Para quê |
|---|---|
| `--cota-final 760.30` | amarra a cota do último PIV (encontro com projeto existente) |
| `--k-convexa / --k-concava` | força os K em vez de usar a tabela |
| `--l-min-curva 50` | comprimento mínimo absoluto de curva |
| `--corda 20` | corda da estaca para formatar `est. 12+10,000` |
| `--est-inicial / --est-final` | trabalha só num trecho |
| `--nome-greide "GREIDE RUA A"` | nome do perfil no LandXML |

### Arquivos que saem

```
<base>_pvis.csv        tabela de PIVs (estaca, cota, rampas, A, tipo, K, L, PCV, PTV)
<base>_pvis.txt        "estação cota" para o Create Profile from File
<base>_greide.xml      LandXML com tangentes + curvas
<base>_relatorio.txt   memória de cálculo, verificações e corte/aterro
<base>_greide.png      gráfico terreno x greide com o diagrama de corte/aterro
```

---

## Tabela de K mínimo (visibilidade de parada)

| V (km/h) | 30 | 40 | 50 | 60 | 70 | 80 | 90 | 100 | 110 | 120 |
|---|---|---|---|---|---|---|---|---|---|---|
| K convexa | 2 | 4 | 7 | 11 | 17 | 26 | 39 | 52 | 74 | 95 |
| K côncava | 6 | 9 | 13 | 18 | 23 | 30 | 38 | 45 | 55 | 63 |

São valores de projeto AASHTO (métrico), que coincidem com os do DNIT para as convexas. O relatório do Python imprime, ao lado, o K calculado pela fórmula (DVP com h₁ = 1,08 m e h₂ = 0,60 m; côncava pelo critério do farol) para você comparar. **Se a classe da via do seu projeto pedir outros valores, troque:** no Python use `--k-convexa` / `--k-concava`; na LISP edite as funções `gr:kconvexa` e `gr:kconcava` no topo do arquivo.

Os K são de visibilidade de **parada**. Ultrapassagem, cruzamentos e drenagem em corte pedem revisão à parte.

---

## Limites conhecidos

- O greide é gerado a partir do perfil do terreno no **eixo**. Ele não conhece a seção transversal, então o corte/aterro do relatório é a diferença vertical no eixo, não volume.
- A criação das **curvas verticais pela LISP** depende da API COM responder — em algumas instalações ela cria só as tangentes. O LandXML resolve.
- Não há compensação de corte/aterro (foi a estratégia escolhida: seguir o terreno). Dá para acrescentar depois como um segundo modo.
- Interseções, greides de esquina e amarração entre ruas ficam por conta do projeto — a rotina trata um eixo por vez.
