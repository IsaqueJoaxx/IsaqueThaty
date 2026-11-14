# IQ C3D — ferramentas próprias para Civil 3D 2026 e 2027

Plugin em C# (.NET 8) escrito do zero. Uma DLL serve para as duas versões
(o 2027 roda em .NET 10 e carrega a DLL .NET 8 normalmente — testado).

## Pastas
- `src\` — código-fonte (um arquivo por grupo: Pontos, Superficies, Alinhamentos; App = ribbon; Util = apoio + gravador de .xlsx)
- `build.ps1` — compila (`powershell -ExecutionPolicy Bypass -File build.ps1`)
- `install.ps1` — instala/atualiza em `C:\ProgramData\Autodesk\ApplicationPlugins\IQC3D.bundle`
  (cada build ganha nome próprio, então dá para atualizar com o Civil 3D aberto; vale na próxima abertura)
- `testes\` — saídas de teste

## Uma vez só: marcar a pasta como confiável
Sem isso o Civil 3D não carrega o plugin sozinho ao abrir. Em **cada** versão (2026 e 2027):
OPÇÕES → aba **Arquivos** → **Locais confiáveis** → Adicionar:
`C:\ProgramData\Autodesk\ApplicationPlugins\IQC3D.bundle\...`
(os três pontinhos no final incluem as subpastas). Reinicie o Civil 3D.

## Comandos (aba "IQ C3D" no ribbon)
| Grupo | Comando | O que faz |
|---|---|---|
| Pontos | IQ_CONVERTERPONTOS | POINT/blocos do AutoCAD → pontos COGO (ou o inverso) |
| | IQ_PONTOS3DPOLY | liga pontos COGO numa polilinha 3D (por número ou seleção) |
| | IQ_APAGARPROXIMOS | apaga pontos a menos de X m de outro (fica o de menor número) |
| | IQ_CRIARPOINTGROUPS | um point group por descrição (completa ou 1ª palavra) |
| | IQ_MOVERLABELS | desloca, gira ou reseta rótulos de pontos |
| Superfícies | IQ_SOLIDOSUPERFICIE | sólido 3D por profundidade, cota fixa ou até outra superfície |
| | IQ_PERFISSUPERFICIE | perfil de superfície em vários alinhamentos (+ offsets) |
| | IQ_BREAKLINELAYER | tudo que está num layer vira breakline |
| Alinhamentos | IQ_MARCAINTERSECOES | marca cruzamentos entre eixos com a estaca de cada um |
| | IQ_EXTRAIRPOLILINHA | alinhamento → polilinha |
| | IQ_ESTENDERTANGENTES | poligonal de PIs (prolonga as tangentes) com PIs numerados |
| | IQ_NOTASERVICO | nota de serviço em Excel: estaca, E/N, terreno × projeto no eixo e nos afastamentos |
| Apoio | IQ_DADOSTESTE | monta um desenho de teste (pontos, superfície, 2 eixos, greide) |
| | IQ_AJUDA / IQ_MENU | lista os comandos / recria a aba do ribbon |

Estaca = 20 m (ex.: 12+5,30). Offsets: negativo = lado esquerdo (LE), positivo = direito (LD).

## Próximas etapas
Perfis e Corredores → Sondagens (janela no Civil 3D) → Platôs (cota do platô segue o greide) e Outros.
