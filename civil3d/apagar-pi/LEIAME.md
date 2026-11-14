# PROJETA — Apagar PI (`PRJAPAGARPI`)

Apaga o PI que você clicar num alinhamento do Civil 3D, mesmo quando o "Excluir PI" nativo
recusa por causa de restrições (curva fixa, linha flutuante, eixo convertido de polilinha).

## Como funciona

1. O eixo é lido como PIs + curvas. Os PIs são as interseções das tangentes, e cada PI pode
   ter: nenhuma curva, um arco simples ou uma espiral-curva-espiral.
2. O PI clicado sai da lista.
3. As curvas dos dois PIs vizinhos mantêm o raio e as espirais quando cabem na nova
   tangente. Se não couberem, são reduzidas até caber, e a mudança aparece na linha de comando.
4. O alinhamento é refeito **no mesmo objeto**: tangentes fixas entre os PIs e curvas livres.
   Estilo, rótulos, perfis e corredores continuam ligados a ele, e a estaca inicial é mantida.
   Como o eixo passa a ter tangentes fixas e curvas livres, o "Excluir PI" do próprio Civil 3D
   também passa a funcionar nele.

Se der algum erro ao refazer o eixo, nada é alterado. `U` desfaz o comando inteiro.

## Uso

```
NETLOAD  ->  bin\Release\net10.0-windows\Projeta.ApagarPI.dll
PRJAPAGARPI  ->  selecione o alinhamento  ->  clique perto do PI  ->  Enter (Sim)
```

O comando continua pedindo PIs até você teclar Enter ou Esc.

## Limites desta versão

- O eixo precisa começar e terminar em tangente.
- Grupos de curva aceitos: arco simples ou espiral-curva-espiral. Com curva composta ou
  reversa sem tangente entre os arcos, o comando avisa e não altera o eixo.
- As estacas depois do PI mudam. Revise os perfis de projeto e recalcule a superelevação,
  se houver; o comando avisa quando o eixo tem essas coisas.

## Compilar

```
dotnet build -c Release                      # Civil 3D 2027 (.NET 10)
dotnet build -c Release -p:CivilVersao=2026  # Civil 3D 2026 (.NET 8)
cd testes  e  dotnet run -c Release          # 16 testes da geometria, sem Civil 3D
```
