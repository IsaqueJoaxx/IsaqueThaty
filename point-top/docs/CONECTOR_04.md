# Conector Civil 3D 0.4

Situação em 06/11/2026: testado por comando, em cópias do desenho de teste. **Falta a validação na janela do Civil 3D.** Não usar em desenho de trabalho antes disso.

## O que mudou em relação à 0.3

| Tema | 0.3 (`civil3d\`, não usar) | 0.4 (`civil3d-conector\`) |
|---|---|---|
| Ao carregar a DLL | Assina evento de ociosidade | Não executa nada até `PTVINCULAR2` |
| Exceção em comando ou evento | Sem proteção: pode encerrar o programa | Registrada no log; o comando termina com mensagem |
| Seleção | Filtro de tipo no prompt | Seleção comum, com conferência do tipo depois |
| Greide | Afirmava que era preservado, sem conferir | Compara cada PVI com o estado do vínculo |
| Sessão encerrada | Permanecia no painel | Removida quando o processo termina |

## Comandos

- `PTVINCULAR2`: seleciona eixo e corredor. Pergunta sobre reconstrução automática; o padrão é `Nao`.
- `PTESTADO`: mostra eixo, corredor e avisos na linha de comando.
- `PTATUALIZAR2`: reconstrói o corredor, se estiver desatualizado, e confere o greide.
- `PTGREIDEOK`: aceita o greide atual como nova referência, depois que você o conferir.
- `PTDESLIGAR2`: encerra o vínculo.

## Roteiro de teste na janela do Civil 3D

Use uma cópia do desenho, em um Civil 3D recém-aberto, sem outro trabalho aberto.

1. `NETLOAD` e selecione `D:\POINT TOP\civil3d-conector\bin\Release\net10.0-windows\PointTop.Conector2027.dll`.
2. Execute `PTESTADO`. Deve responder `sem vínculo`.
3. Abra o POINT TOP em Civil 3D · Alinhamento e deixe ao lado.
4. Execute `PTVINCULAR2`, selecione eixo e corredor e responda `Nao`.
5. Arraste um ponto do eixo com os grips. O painel deve mostrar a alteração em poucos segundos, e o corredor como reconstrução pendente.
6. Execute `PTATUALIZAR2`. Confira no perfil e nas seções o que a mensagem informar sobre o greide.
7. Execute `U` duas vezes e confira se o eixo e o corredor voltam.
8. Só depois disso, repita com `Sim` na reconstrução automática.
9. Execute `PTDESLIGAR2`.

Se algo fechar ou falhar, guarde a janela de erro da Autodesk e `runtime\civil3d-diagnostics\conector-PID.log`. A última linha indica a etapa alcançada.

## O que foi medido

Cópia de `Ponte Integração Interior Sede] - Traçado 2 - teste.dwg`, eixo com 5.430,765 m, corredor 02 e greide VERTICAL02 com 19 PVIs. Execução em `accoreconsole`, sem salvar.

**Eixo inteiro deslocado 5 m em X**
- O corredor foi marcado como desatualizado e reconstruído em cerca de 3 a 7 s.
- Os taludes só mudaram depois da reconstrução. Na estaca 2.285, o afastamento esquerdo foi de -6,53 m para -6,92 m.
- Os 19 PVIs ficaram iguais.

**Tangente em torno da estaca 350 alterada em (6; 4) m**
- O eixo passou de 5.430,765 m para 5.431,029 m.
- As cotas dos PVIs não mudaram, mas **15 dos 19 PVIs mudaram de estaca**: até 0,854 m perto da edição e 0,264 m nos seguintes.
- A cota do greide em uma mesma estaca mudou até 3,9 cm nas amostras.
- O fim da região do corredor também acompanhou: de 2.538,006 m para 2.538,270 m.

**Desfazer**
- Reconstrução e edição do eixo são dois passos separados. O primeiro `U` desfez a reconstrução, e um `U` posterior devolveu o eixo à posição original.

## Conclusão sobre o greide

O conector não edita o greide, e a reconstrução não alterou nenhum PVI. Mas o próprio Civil 3D desloca as estacas dos PVIs quando o eixo muda de comprimento, então "o greide fica igual" não é verdade em toda edição. Por isso o conector avisa quantos PVIs mudaram e em quanto. Decidir se o deslocamento é aceitável continua sendo do projetista.

A edição do teste foi feita pela API (`PassThroughPoint2`), e não com grips. O deslocamento deve ser conferido também no arraste real (passo 6).

## O que não foi validado

- Comportamento na janela do Civil 3D, incluindo arraste com grips.
- Evento de ociosidade: ele não dispara durante o script no console. Por isso a atualização contínua do painel e a reconstrução automática (`Sim`) só foram compiladas, não exercitadas.
- Corredores com várias linhas de base, referências de dados ou outros desenhos.
- Conformidade de alvos, seções e quantitativos depois da reconstrução.
- Falhas nativas: a proteção cobre exceções .NET, não uma falha dentro do Civil 3D.

## Observação sobre `NETLOAD` em script

Em script, `NETLOAD` com caminho entre aspas quebra no espaço de `D:\POINT TOP`. Use `(command "_.NETLOAD" "D:/POINT TOP/...dll")`. A janela de seleção de arquivo não tem esse problema.

## Teste automático

`civil3d-conector-teste\` contém comandos que alteram o eixo (`PTTESTEMOVER`, `PTTESTEPI`). É apenas para cópias descartáveis e não faz parte do produto.
