# REDES — IFM

Aba do Assistente PROJETA para redes de tubulação do Civil 3D (desenho em metros). Quatro funções,
escolhidas no campo **Função**. A drenagem pluvial fica na aba de drenagem, feita à parte.

## Esgoto (NBR 9649)

Lê a rede escolhida, monta o fluxo de montante para jusante (sentido de escoamento dos tubos) e
verifica cada trecho. Só lê o desenho; nada é alterado.

- Vazões: Qi = C·K2·Pi·q/86400 e Qf = C·K1·K2·Pf·q/86400, distribuídas pela extensão da rede (taxa
  linear), mais infiltração (L/s·km) e vazões pontuais. Vazão mínima de cálculo por trecho (1,5 L/s).
- Vazão pontual: gravada na **Vazão conhecida** da estrutura (o Civil 3D guarda em m³/s; a aba
  converte de L/s). Botão *Vazão pontual…* com um trecho selecionado grava na estrutura de montante.
- Lâmina com Qf: y/D ≤ 0,75; se V > Vc = 6·√(g·Rh), o limite passa a 0,50. Velocidade máxima 5 m/s.
- Tensão trativa com Qi: σ = 10⁴·Rh·I ≥ 1,0 Pa. Declividade mínima informada: 0,0055·Qi^−0,47.
- DN mínimo (padrão 150 mm), recobrimento mínimo (padrão 0,90 m, pelo terreno da superfície de
  referência), tubo de queda a partir de 0,50 m de degrau, saída acima da chegada, e lâmina de saída
  acima de alguma chegada (pede verificar remanso).
- Avisa circuito fechado, tubo sem estrutura e mais de um tubo saindo da mesma estrutura.

## Drenagem pluvial (método racional)

Lê a rede pluvial, acumula de montante para jusante e verifica cada trecho. Calcular só lê o desenho.

- Q = C·i·A/360 (Q em m³/s, i em mm/h, A em ha); a aba mostra em L/s. Área e C·A acumulados.
- Chuva: i = K·TR^a / (t + b)^c (t em minutos). O usuário digita K, a, b, c e a fonte. O botão
  *Estudos de chuva do SGB…* procura o município no catálogo do Serviço Geológico do Brasil (mais de
  700 estudos, dentro da DLL), abre o estudo no navegador e preenche a fonte.
- tc: tempo de entrada (padrão 10 min) nas estruturas sem tc próprio; a jusante, o maior entre o tc da
  própria área e o tc de montante mais o percurso no tubo (L/V). Ramais sem área não aumentam o tc.
- Área, C e tc de cada estrutura ficam nas propriedades da estrutura do Civil 3D (guia Áreas de
  contribuição). *Áreas de contribuição…* abre uma tabela de todas as estruturas (aceita colar do Excel)
  e grava tudo de uma vez (um U desfaz). O Civil 3D guarda o tc em horas; a aba converte para minutos.
  Estruturas com bacias (catchments) ligadas usam as bacias e ficam bloqueadas na tabela.
- Verifica capacidade (Manning, seção parcialmente cheia), y/D máximo (padrão 0,85), velocidade
  mínima e máxima (0,60 e 5,00 m/s), DN mínimo (400 mm), recobrimento (1,00 m), diâmetro diminuindo
  para jusante e saída acima da chegada. Sugere o menor DN comercial (NBR 8890) que leva a vazão.
- Vazão conhecida das estruturas entra como vazão pontual. Área em estrutura sem tubo de saída é avisada.
- *Gravar vazões nos tubos…* grava a vazão de projeto na propriedade Vazão de cada tubo (m³/s), para
  rótulos, tabelas e perfis do Civil 3D. Um U desfaz.
- Os limites padrão são pontos de partida: confira o manual de drenagem da prefeitura ou do órgão.

## Escavação e quantitativos

Vala por trecho: largura = máx(largura mínima, DE + 2·folga); profundidade = terreno − (GI − parede)
+ berço; volume exato para profundidade variando linearmente e talude opcional; escoramento das duas
paredes acima da profundidade limite; reaterro = escavação − tubo − berço. Resumo por faixa de
profundidade média (até 1,50 / 1,50–3,00 / 3,00–4,50 / 4,50–6,00 / acima de 6,00 m). Quantitativos: tubos por tamanho e
material, estruturas por tipo, descrição e faixa de altura de 0,50 m.

## Renomear na ordem do fluxo

Coletor mais longo primeiro (montante → jusante), depois os ramais. PV e bocas de lobo têm numeração
própria (PV-01…, BL-01…); tubos (TR-01…) seguem a ordem das estruturas de montante. Mostra a prévia;
*Aplicar* só renomeia se a rede não mudou depois da prévia. Um U desfaz tudo.

## Associar aos eixos

Cada estrutura (e cada tubo, pelo ponto médio) fica com o eixo mais próximo entre os marcados, se o
afastamento não passar do máximo. Estaca e afastamento passam a valer nos rótulos do Civil 3D. U desfaz.

## Planilhas

*Exportar Excel…* grava .xlsx: esgoto (trechos e notas), drenagem (trechos, áreas por estrutura e
parâmetros) ou escavação (trechos, faixas, quantitativos).

## Testes feitos

- Verificações automáticas sem Civil 3D (`_testes_redes`): Manning, inversão da lâmina, σ ≈ 1 Pa na declividade
  mínima, vazões em série e com ramais, vazão pontual, tubo de queda, circuito, vala, talude,
  escoramento parcial, quantitativos, renomeação e planilha.
- Civil 3D 2027: rede gerada com 6 PV e 5 tubos; cálculo, vala, renomeação, associação, vazão pontual
  (conferida no diálogo da estrutura: 0,002 m³/s) e as duas planilhas.
- A roda do mouse sobre um campo numérico agora rola a aba e não muda o valor (vale para todas as abas).
- Drenagem, sem Civil 3D: vazão de um trecho conferida à mão (1 ha, C 0,8, 10 min → 254,66 L/s), tc com
  percurso em série, junção com ramal sem área e vazão pontual, DN diminuindo, DN sugerido, área no
  lançamento, recusa sem IDF e o catálogo do SGB.
- Drenagem, Civil 3D 2027: rede pluvial gerada (5 estruturas, 4 tubos), áreas gravadas e conferidas no
  diálogo da estrutura (m², C, tc em horas), cálculo, vazões gravadas e conferidas no diálogo do tubo
  (0,102 m³/s), planilha, tabela de áreas (edição, total, validação) e a busca de estudos do SGB.
