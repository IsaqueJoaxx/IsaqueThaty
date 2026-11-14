# JUNÇÃO DE CORREDORES — IFM

Comando: `JUNCAOIFM`. Também disponível na aba **Corredores**, botão **Curva de ligação — IFM…**.

## Uso

1. No espaço Modelo, selecione o primeiro alinhamento, clicando no trecho reto e no lado pelo qual a ligação chega.
2. Selecione o segundo alinhamento, no trecho reto e no lado pelo qual a ligação sai. Os cliques escolhem o canto da junção, independentemente do sentido das estacas dos eixos.
3. Informe o raio da curva, nas unidades do desenho.
4. Escolha pelo número o greide de projeto de cada eixo. A rotina lista somente perfis FG pertencentes ao respectivo alinhamento e exige escolha explícita mesmo quando existe apenas um.
5. Escolha a seção tipo existente no desenho e a superfície alvo, ou 0 para configurar os alvos manualmente.
6. Revise estacas de encontro, cotas, comprimento e rampa. Digite **Sim** para criar. Enter ou Esc cancela sem criar objetos.

Cria um alinhamento com arco circular tangente aos dois trechos, um perfil de projeto que une as cotas dos greides nos pontos de tangência, e um corredor independente com a seção tipo escolhida. Usa os primeiros estilos de alinhamento/perfil e conjuntos de rótulos do desenho; podem ser editados depois. Não modifica os alinhamentos nem greides originais. Um comando **U** desfaz a criação.

## Limites e revisão

- Esta versão atende uma curva entre **dois trechos retos**; rejeita curvas/espirais, eixos iguais, paralelas, trechos insuficientes para o raio e perfis que não cobrem os encontros.
- O raio é o raio do **eixo da ligação**, não o raio da borda do pavimento. Larguras e inclinações transversais vêm da seção tipo escolhida.
- O perfil criado é uma única tangente vertical entre as cotas de encontro, limitada a 7% em módulo. Não garante continuidade de rampa com os perfis originais. As curvas verticais são manuais; o limite de rampa não comprova atendimento normativo.
- A geometria e o perfil são **estáticos**: mudanças posteriores nos eixos/greides originais exigem revisar e refazer a ligação.
- Não cria o objeto nativo Intersection de T/cruzamento, não funde corredores existentes e não recorta suas regiões automaticamente. Ajuste os encontros e recortes dos corredores originais para eliminar sobreposições. Não cria superfície composta automaticamente.
- O corredor precisa de seção tipo/PKT válidos. Apenas alvos de superfície são atribuídos automaticamente; alvos de largura, cota e offset devem ser revisados. A orientação esquerda/direita da seção acompanha o sentido primeiro eixo → segundo eixo.
- Confira rampa, drenagem, seção transversal, encontros e superfície resultante no Civil 3D antes de usar em projeto.

## Carregamento e validação

Pacote preparado: **Previa21-JuncaoIFM, 0.21.0**. Para carregar manualmente em uma sessão sem outra versão do Assistente PROJETA carregada, execute **NETLOAD** e escolha `Projeta.Civil3D.dll` em `bin/Previa21-JuncaoIFM/2026` ou `2027`, conforme a versão do Civil 3D. Se outra versão já estiver carregada automaticamente, será necessário atualizar a instalação antes de usar esta DLL; o AutoCAD não descarrega a versão anterior na mesma sessão.

Esta entrega não altera a instalação existente. Compilação e testes matemáticos são verificáveis fora do Civil 3D; criação, reconstrução, cancelamento e desfazer no desenho ainda precisam ser testados em sessão Civil 3D.

