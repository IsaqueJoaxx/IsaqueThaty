# Rotina Dynamo – Criação de Rotatória (Civil 3D 2024+)

## Estrutura dos Nós Python

| Arquivo | Nó no Dynamo | Função |
|---|---|---|
| `01_calc_raios.py` | Python Script | Calcula os 4 raios a partir dos sliders |
| `02_pontos_acessos.py` | Python Script | Geometria de cada ramal (ângulos, bordas) |
| `03_criar_polilinhas.py` | Python Script | Desenha círculos e polilinhas no DWG |
| `04_criar_alinhamento_civil3d.py` | Python Script | Alinhamento circular do eixo da pista |
| `05_criar_alinhamentos_ramais.py` | Python Script | Alinhamentos retos por acesso |
| `06_ilhas_divisorias.py` | Python Script | Polilinhas triangulares (splitter islands) |

## Inputs (Number/Integer Sliders no Dynamo)

| Slider | Descrição | Valor típico |
|---|---|---|
| `raio_ilha` | Raio da ilha central (borda) | 8 – 15 m |
| `largura_faixa` | Largura de cada faixa da pista | 3.5 – 4.5 m |
| `num_faixas` | Número de faixas na pista anular | 1 – 2 |
| `num_acessos` | Número de ramais (braços) | 3 – 6 |
| `larg_acesso` | Largura total do acesso | 6 – 9 m |
| `centro` | Ponto central [X, Y] | via nó Point |
| `angulo_inicial` | Orientação do 1º ramal (graus) | 0 – 359 |
| `nome_alinhamento` | Nome base dos alinhamentos | "ROT-..." |

## Como montar no Dynamo

1. Abra o Civil 3D com o DWG do projeto.
2. Abra o Dynamo Player ou Editor.
3. Crie os **Number Slider** e **Integer Slider** para cada parâmetro.
4. Insira um nó **Python Script** para cada arquivo acima.
5. Cole o código do arquivo no nó correspondente.
6. Conecte as saídas na ordem:

```
Sliders → Nó 01 → raios[1,2,3] → Nó 03 (polilinhas)
                                 → Nó 04 (alinhamento eixo)
Sliders → Nó 02 → acessos      → Nó 03
                                 → Nó 05 (alinhamentos ramais)
                                 → Nó 06 (ilhas divisórias)
```

7. Conecte nós **Watch** nas saídas para verificar os ObjectIds criados.
8. Clique em **Run** (ou ative o modo automático).

## Layers criados automaticamente

| Layer | Conteúdo | Cor ACI |
|---|---|---|
| `ROT_ILHA_CENTRAL` | Círculo da ilha central | 1 – vermelho |
| `ROT_PISTA_ANULAR` | Borda interna da pista | 3 – verde |
| `ROT_BORDA_EXTERNA` | Borda externa total | 5 – azul |
| `ROT_ACESSO` | Polilinhas de cada ramal | 4 – ciano |
| `ROT_ILHA_DIV` | Ilhas divisórias | 6 – magenta |

## Requisitos

- Civil 3D 2024 ou superior
- Dynamo for Civil 3D (já incluído na instalação padrão)
- IronPython 2.7 (padrão Dynamo ≤ 2.x) **ou** CPython 3.x (Dynamo 2.13+)
- Referências: `acmgd.dll`, `acdbmgd.dll`, `AeccDbMgd.dll`, `AeccProjMgd.dll`
