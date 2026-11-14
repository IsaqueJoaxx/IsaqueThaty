# Plugin "Interseção" para Civil 3D 2025+

Comando **`CRIARINTERSECAO`**. Ele pergunta os raios das curvas de
concordância, a cota da interseção, a meia-largura da pista, o assembly e os
nomes; então cria as 4 curvas de concordância, os alinhamentos de offset das
bordas, o **corredor** e a **superfície** do corredor.

Novidades desta versão:

- **Cria o perfil automaticamente**: se um alinhamento não tiver greide de
  projeto (FG), o plugin gera um **perfil plano na cota** informada. Se já
  houver perfil FG, ele é mantido.
- **Gera os alinhamentos de offset** das bordas (±meia-largura), quando você
  responde "Sim" ao prompt correspondente.

## Limitação importante (leia antes)

A API .NET do Civil 3D **não expõe** um método para criar o objeto nativo
*Interseção* — isso foi confirmado pela Autodesk e continua sem solução até a
versão 2025. Por isso o plugin **reproduz o resultado na prática** (geometria +
corredor + superfície) em vez de criar o objeto Interseção propriamente dito.
Funcionalmente o resultado é equivalente para modelagem/volumes.

## Pré-requisitos no desenho

Antes de rodar o comando, tenha no desenho:

1. Os **dois alinhamentos** que se cruzam.
2. Um **assembly** compatível (com um link de superfície chamado `Top`).

O perfil de projeto **não é mais obrigatório**: se faltar, o plugin cria um
greide plano na cota informada. Se você já tiver o greide definitivo, ele é
respeitado.

## Como compilar

1. Instale o .NET 8 SDK e o Visual Studio 2022 (ou `dotnet build` pela linha de comando).
2. Confirme o caminho do Civil 3D no `Intersecao.csproj` (propriedade `C3DPath`).
   O padrão é `C:\Program Files\Autodesk\AutoCAD 2025`. As DLLs do Civil ficam
   na subpasta `C3D`.
3. Compile:

   ```
   dotnet build Intersecao.csproj -c Release
   ```

   A saída será `bin\Release\net8.0-windows\Intersecao.dll`.

> Para Civil 3D **2024 ou anterior**: mude `TargetFramework` para `net4.8` no
> `.csproj` e ajuste o `C3DPath` para a pasta da sua versão.

## Como usar no Civil 3D

1. No Civil 3D, digite `NETLOAD` e selecione a `Intersecao.dll`.
2. Digite `CRIARINTERSECAO`.
3. Responda aos prompts:
   - Selecione o alinhamento **principal** e o **secundário**.
   - Informe os **raios** (mesmo raio para as 4 esquinas ou um por esquina).
   - Informe a **cota** da interseção.
   - Informe a **meia-largura da pista** (offset da borda).
   - Selecione o **assembly**.
   - Confirme os **nomes** do corredor e da superfície.

## Pontos marcados `>>> AJUSTE <<<` no código

- **Perfil usado no corredor** (`PrimeiroPerfil`): por padrão pega o perfil de
  projeto `FG`. Troque se você usa outra convenção.
- **Código de link da superfície** (`CriarSuperficieCorredor`): usa `Top`.
  Se o seu assembly usa outro código (ex.: `Sup`, `Pav`), altere ali.
- **Caminho das DLLs** no `.csproj`.

## Observações de engenharia

- As curvas de concordância são geradas tangentes às **bordas de pista**
  (eixo deslocado da meia-largura informada), usando a tangente de cada
  alinhamento no ponto de cruzamento. É uma aproximação por retas tangentes no
  cruzamento — ótima para cruzamentos aproximadamente retos; em alinhamentos
  muito curvos junto ao cruzamento, revise as curvas manualmente.
- O corredor usa uma única região por baseline com o mesmo assembly. Para
  modelar aproximações/afunilamentos diferentes, adicione regiões extras.
