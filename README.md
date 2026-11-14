# PROJETA — tudo num lugar só

Código de todas as ferramentas do Isaque / PROJETA ENGENHARIA: plugins do Civil 3D, POINT TOP, IRI Rodoviário, LISPs e utilitários.
Um comando compila tudo, roda os testes e gera **um instalador** (`Instalar-PROJETA.exe`) que leva tudo para outro PC.

## Instalar em outro PC

1. Baixe o `Instalar-PROJETA.exe` (na página **Releases** do repositório no GitHub) e abra.
2. Marque o que quer e clique em **Instalar**. Não precisa de administrador.
3. Abra o Civil 3D: os plugins carregam sozinhos (`PRJPAINEL` abre o painel do PROJETA).

| Componente | O que é | Precisa no outro PC |
|---|---|---|
| Plugins do Civil 3D | PROJETA, Giro, Sinalização, Terraplenagem IFM, IQ C3D | Civil 3D 2026 e/ou 2027 |
| Servidor MCP | o Claude conversa com o Civil 3D aberto (`PRJMCP` → Iniciar) | Claude Desktop (opcional) |
| POINT TOP | volumes por GeoTIFF, eixos e greide no navegador | nada: vem com Node e Python próprios |
| IRI Rodoviário | cálculo do IRI | nada |
| LISPs | rotinas AutoLISP, carregadas com `APPLOAD` | AutoCAD ou Civil 3D |

O instalador usa o .NET Desktop 8 ou 10, que o Civil 3D 2026/2027 já instala. Num PC sem Civil 3D, o Windows oferece baixar o .NET Desktop Runtime na primeira vez.
Para desinstalar: Configurações → Aplicativos → **PROJETA**, ou `Desinstalar PROJETA.cmd` na pasta dos programas (`%LOCALAPPDATA%\Programs\PROJETA`).

## Depois de formatar o PC (recuperar o código)

```powershell
git clone https://github.com/<seu-usuario>/projeta-suite.git C:\Users\User\Projects\PROJETA
cd C:\Users\User\Projects\PROJETA
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Para compilar, a máquina precisa de: .NET SDK 8 e 10, Civil 3D 2026 e 2027 (as DLLs de referência vêm dele), Node.js, QGIS 3.44
(o Python 3.12 portátil do POINT TOP sai dele) e os pacotes Python do POINT TOP
(`pip install -r point-top\python\requirements.txt --target D:\POINT TOP\runtime\packages`, ou indique outra pasta em `-PacotesPython`).
O IRI Rodoviário entra pelo executável já gerado (`-IriExe`); o código dele está em `iri-rodoviario\src`.

## Compilar e gerar o instalador

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1                 # tudo: plugins 2026/2027, testes, POINT TOP, IRI, LISP, instalador
powershell -ExecutionPolicy Bypass -File .\build.ps1 -SemTestes
powershell -ExecutionPolicy Bypass -File .\build.ps1 -SemInstalador   # só monta saida\pacote
```

Sai em `saida\Instalar-PROJETA.exe`. Para conferir sem mexer no Windows:
`saida\Instalar-PROJETA.exe --silencioso --componentes civil,claude,pointtop,iri,lisp --raiz-teste C:\Temp\teste-instalacao`
(instala tudo dentro da pasta de teste, inclusive uma configuração do Claude Desktop de mentira; `--conteudo` lista o pacote).

## Onde está cada coisa

```
civil3d\
  projeta\            PROJETA • Assistente Civil (Projeta.Redes.dll): redes de drenagem/esgoto/água, SWMM/EPANET, dispositivos,
                      nota de serviço, volumes, projeto × obra, platôs, cubação, apagar PI/PIV, servidor MCP. README próprio.
  giro\               PROJETA Giro: giro de veículos, gabaritos DNIT, conflitos, animação
  sinalizacao\        PROJETA Sinalização: horizontal, vertical, semafórica, obras, defensas, quantitativos
  terraplenagem-ifm\  Distribuição de terraplenagem (PRJTERRA)
  iqc3d\              IQ C3D: alinhamentos, pontos, superfícies
  apagar-pi\          versão avulsa do PRJAPAGARPI (hoje dentro do PROJETA)
  testes-sem-tela\    testes no Civil 3D sem abrir a tela (accoreconsole) e scripts de instalação rápida
point-top\            POINT TOP - IFM (Node + Python): volumes GeoTIFF, eixos, greide, conectores do Civil 3D
iri-rodoviario\       IRI Rodoviário (Python; o .exe é gerado com PyInstaller)
lisp\                 LISPs feitos por nós (vão no instalador)
  acervo\             coleções de terceiros e rotinas antigas, guardadas como acervo (não vão no instalador)
utilitarios\          scripts Python, apps HTML, rotinas Dynamo, Interseções, TerrainCalc (iOS)
arquivo\              protótipos e versões anteriores (Assistente PROJETA, Redes IFM, Reabilitação de Pista, SubAssembly...), só o código
instalador\           o instalador (C#, WinForms)
build.ps1             compila tudo e gera o instalador
```

## De onde veio cada pasta (montagem em 13/11/2026)

As pastas originais não foram apagadas; esta passa a ser a oficial.

| Aqui | Origem |
|---|---|
| civil3d\projeta | C:\Users\User\Projects\Projeta.Civil3D |
| civil3d\giro, sinalizacao, iqc3d, apagar-pi | C:\Users\User\Projects\Projeta.Giro, Projeta.Sinalizacao, IsaqueC3D, Projeta.ApagarPI |
| civil3d\terraplenagem-ifm | D:\PROJETA ENGENHARIA\_FERRAMENTAS\Terraplenagem.IFM |
| civil3d\testes-sem-tela | C:\Temp\nsteste (só código e scripts) |
| point-top | D:\POINT TOP (sem runtime, resultados e o .mms de 348 MB) |
| iri-rodoviario | C:\Users\User\Projects\IRI_RODOVIARIO (sem build_env, build, dist) |
| lisp | D:\Claude ai (*.lsp), D:\pilhas.lsp, Projects\teste-fcc |
| lisp\acervo | D:\PROJETA ENGENHARIA\_FERRAMENTAS\LISP, D:\03 - AutoCAD e Ferramentas\Lisp, D:\Claude ai\talude |
| utilitarios | D:\Claude ai (scripts, HTML, Dynamo, Interseções), Projects\TerrainCalc |
| arquivo | D:\PROJETA ENGENHARIA\_FERRAMENTAS (demais projetos) e rotinas de D:\PROJETA ENGENHARIA |

Não entram: DWGs de projetos de clientes, documentos (propostas, orçamentos), instaladores de terceiros, arquivos acima de 20 MB.
