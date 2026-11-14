# PROJETA • Giro de Veículos

Plugin para Civil 3D 2026/2027 com caminho de varredura de veículos (swept path), gabaritos de giro
dos veículos de projeto do DNIT (IPR-718: VP, CO, O, SR, RE), verificação de conflitos com meios-fios
e animação. Código próprio — não usa bibliotecas nem dados de terceiros além do Clipper2 (licença Boost).

## Instalar
Feche o Civil 3D e, na pasta do projeto:

    powershell -ExecutionPolicy Bypass -File .\instalador\instalar.ps1

Ao abrir o Civil 3D aparece a aba **PROJETA Giro**. `GIROAJUDA` lista os comandos.

## Comandos (entrega 1)
| Comando | O que faz |
|---|---|
| GIROVEICULO | escolhe o veículo de projeto |
| GIROARCO | condução por pontos (giro com transição de volante + reta até o ponto; ré para rígidos) |
| GIROSEGUIR | eixo dianteiro segue polilinha/linha/arco/alinhamento/feature line |
| GIROGABARITO | gabarito de 30°–180° no raio mínimo |
| GIROATUALIZAR | refaz a varredura (caminho editado, outro veículo, velocidade, veículos a cada X m) |
| GIROVERIFICAR | invasões da envoltória e das rodas sobre meios-fios/obstáculos, com folga |
| GIROANIMAR | animação do veículo (Esc para parar) |
| GIRORELATORIO | largura varrida, esterçamento, articulação, avisos |
| GIROCONFIG / GIRODEMO / GIROLIMPAR | ajustes, exemplo e limpeza de dados órfãos |

## Modelo
- Cinemática de baixa velocidade sem deriva: cada eixo efetivo segue seu ponto guia como tractriz
  (solução exata por trecho reto: tan(φ/2) = tan(φ₀/2)·e^(−s/L)).
- Esterçamento máximo calibrado pelo raio mínimo da roda externa dianteira da Tabela 6 do DNIT.
- Transição de volante: taxa = 2·δmáx / (tempo batente a batente) / velocidade.
- Envoltória: união (Clipper2) dos fechos convexos de posições consecutivas de carrocerias e rodas.

## Premissas [PREMISSA] — ajuste ao caso
- Velocidade de manobra 10 km/h; tempo batente a batente 4 s (VP) e 6 s (demais).
- Folga lateral 0,50 m; articulação máxima 80°.
- Balanços dos reboques do SR/RE e barra do dolly estimados das Figuras 14/15 do DNIT.
- Veículo O: com a geometria da Figura 13, o raio interno sai 7,70 m (tabela: 7,10 m).

Rascunho técnico — confira com os gabaritos oficiais e valide no projeto executivo.
