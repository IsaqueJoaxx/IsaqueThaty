# CORREÇÃO DO GREIDE — IFM

Versão 0.20.0, Previa20-CorrecaoIFM. Comando PROJETACORRECAO ou botão CORREÇÃO DO GREIDE — IFM na aba Point top. Preserva o módulo de análise de IRI medido.

Importe um CSV UTF-8 ou cole uma série por eixo com este cabeçalho e dados fictícios:

```csv
distancia_m;greide_base_m;existente_eixo_m;existente_esq_m;existente_dir_m;largura_esq_m;largura_dir_m;caimento_esq_pct;caimento_dir_pct;correcao_mm
0;100;100;99,93;99,93;3,5;3,5;-2;-2;0
5;100;100;99,93;99,93;3,5;3,5;-2;-2;2,5
10;100;100;99,93;99,93;3,5;3,5;-2;-2;6
```

Use distâncias acumuladas crescentes em metros, não estaca+fração. Cotas e larguras também em metros; correções em milímetros; caimentos em porcentagem. As larguras são distâncias horizontais do eixo até cada bordo. Caimento negativo baixa o bordo em relação ao eixo em ambos os lados. Informe os caimentos reais nas curvas. Dados pertencentes a eixos diferentes devem ser analisados separadamente.

## Cálculo

- Base corrigida no eixo = greide + correção / 1000.
- Base no bordo = base corrigida no eixo + largura × caimento / 100.
- Topo no eixo e nos bordos = respectivas bases + espessura vertical de concreto, inicialmente 0,23 m.
- Saldo abaixo da base = base corrigida − cota existente no ponto correspondente.

Saldo positivo indica espaço geométrico abaixo da base; negativo indica interferência. O saldo não identifica sozinho quais camadas serão executadas ou removidas. As espessuras calculadas são verticais; não são espessuras medidas perpendicularmente à superfície. A mesma correção vertical é aplicada ao eixo e aos dois bordos, preservando os caimentos informados.

## Regra de correção informada pelo usuário

Para cada par consecutivo com distância exatamente 5 m, verificar valor absoluto da diferença entre as correções ≤ 2,5 mm. A primeira seção fica sem comparação. Intervalos diferentes de 5 m são sinalizados como não verificados; não se presume proporcionalidade ou interpolação. A correção acumulada não é limitada a 2,5 mm.

O controle não calcula nem garante IRI. Não otimiza automaticamente o greide, não altera o DWG e não aplica correções automaticamente. Cotas são importadas de fonte externa, sem extração automática de eixo ou bordos. Use o arquivo exportado no Excel para revisar e editar as correções, depois importe novamente.

Exportação CSV: cotas originais, larguras, caimentos, correção, espessura de concreto, bases e topos calculados, saldos no eixo e bordos, variação da correção e situação de cada intervalo. Decimais preservam precisão de milímetros; o painel mostra cotas com quatro casas decimais.

Compilado para Civil 3D 2026/2027. O funcionamento dentro da sessão Civil 3D precisa de validação.
