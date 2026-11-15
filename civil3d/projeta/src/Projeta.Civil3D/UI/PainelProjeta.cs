using System.Drawing;
using System.Windows.Forms;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.UI;

/// <summary>Painel lateral (PaletteSet) com os comandos agrupados por bloco.</summary>
internal sealed class PainelProjeta : UserControl
{
    public static readonly (string Grupo, (string Rotulo, string Comando, string Dica)[] Itens)[] Grupos =
    {
        ("Rede", new[]
        {
            ("Criar rede", "PRJREDE", "Cria uma rede de drenagem, esgoto, água ou genérica"),
            ("Opções da rede", "PRJOPCOES", "Parâmetros de cálculo, prefixos e desenho"),
            ("Equações de chuva", "PRJEQUACOES", "Catálogo IDF e equações do projeto"),
            ("Lançar PVs e tubos", "PRJLANCAR", "Clique os pontos em sequência (montante → jusante)"),
            ("Inserir nó", "PRJNO", "PV, boca de lobo, caixa ou exutório avulso"),
            ("Ligar tubo", "PRJTUBO", "Liga dois nós existentes"),
            ("Criar bacia", "PRJBACIA", "Polilinha fechada → bacia de contribuição"),
            ("Propriedades", "PRJPROPRIEDADES", "Edita nó, tubo ou bacia"),
            ("Salvar rede (JSON)", "PRJEXPORTARREDE", "Salva uma rede em JSON para levar a outro desenho"),
            ("Carregar rede (JSON)", "PRJIMPORTARREDE", "Adiciona uma rede salva em JSON e a desenha"),
            ("Excluir rede", "PRJEXCLUIRREDE", "Remove uma rede inteira (modelo e desenho)"),
        }),
        ("Edição", new[]
        {
            ("Conectar", "PRJCONECTAR", "Muda o nó de jusante de um tubo"),
            ("Inverter sentido", "PRJINVERTER", "Inverte montante/jusante dos tubos"),
            ("Dividir tubo", "PRJDIVIDIR", "Insere um nó no meio de um tubo"),
            ("Trocar dispositivo", "PRJTROCAR", "Troca o modelo dos nós selecionados"),
            ("Remover", "PRJREMOVER", "Remove nós, tubos ou bacias da rede"),
            ("Numerar", "PRJNUMERAR", "Renumera nós e trechos por coletor"),
        }),
        ("Cálculo e saídas", new[]
        {
            ("Calcular (planilha)", "PRJCALCULAR", "Dimensiona e abre a planilha editável"),
            ("Atualizar", "PRJATUALIZAR", "Lê o desenho, recalcula e redesenha"),
            ("Tabela no desenho", "PRJTABELA", "Dimensionamento, nota de PVs, materiais, vala"),
            ("Relatório HTML", "PRJRELATORIO", "Relatório completo da rede"),
            ("Nota de serviço PVs", "PRJNOTACAIXAS", "CSV para o Excel"),
            ("Projetar no perfil", "PRJPERFIL", "Desenha tubos e PVs em um profile view"),
            ("Interferências", "PRJINTERFERENCIA", "Cruzamentos entre redes"),
            ("Escavação de valas", "PRJESCAVACAO", "Volumes por faixa de profundidade"),
        }),
        ("Integração", new[]
        {
            ("Importar Pipe Network", "PRJIMPORTARPIPE", "Converte uma rede do Civil 3D"),
            ("Devolver para a Pipe Network", "PRJDEVOLVERPIPE", "Grava diâmetros e cotas calculados na rede do Civil 3D"),
            ("Importar planilha", "PRJIMPORTARCSV", "Rede a partir de CSV (Excel)"),
            ("Exportar planilha", "PRJEXPORTARCSV", "Rede para CSV"),
            ("Modelo de planilha", "PRJMODELOCSV", "Gera um CSV de exemplo"),
        }),
        ("Longitudinais", new[]
        {
            ("Valeta / sarjeta", "PRJLONGITUDINAL", "Lança dispositivo ao longo de um alinhamento"),
            ("Editar longitudinal", "PRJLONGEDITAR", "Altera e regenera um dispositivo"),
            ("Quantitativos", "PRJLONGQUANT", "Concreto, escavação, capacidade"),
        }),
        ("Alinhamento", new[]
        {
            ("Apagar PI", "PRJAPAGARPI", "Apaga o PI clicado mesmo com restrições (refaz o eixo no mesmo alinhamento)"),
            ("Apagar PIV (greide)", "PRJAPAGARPIV", "Apaga o PIV clicado no greide mesmo com restrições (refaz o greide no mesmo perfil)"),
            ("Vias do loteamento em lote", "PRJVIAS", "Selecione as polilinhas dos eixos das ruas: alinhamentos com curva nos vértices, perfil do terreno, greide proposto passando na mesma cota nos encontros, vistas de perfil, corredor com superfície, linhas de amostragem e Excel"),
        }),
        ("Superfícies", new[]
        {
            ("Volumes entre superfícies", "PRJVOLUMES", "Corte e aterro (terreno × projeto ou levantamentos), por polígono, com mapa de cores e Excel"),
            ("Projeto × obra", "PRJCONFORMIDADE", "Compara o levantamento da obra com o projeto: o que está fora da tolerância, mapa, pontos críticos, controle por estaca e Excel"),
        }),
        ("Geo e terreno", new[]
        {
            ("Relevo de satélite", "PRJRELEVO", "Relevo gratuito de 30 m da área (polilinha ou janela): ANADEM (terreno, sem vegetação) ou Copernicus (superfície); cria a superfície no Civil 3D em SIRGAS 2000 / UTM"),
            ("Georreferenciar desenho", "PRJGEORREF", "Clique pontos e digite lat/long ou UTM: move, gira e escala o desenho e define o sistema (liga o mapa de satélite do Civil)"),
            ("Exportar para o Google Earth", "PRJKMZ", "Polilinhas, eixos, feature lines, pontos COGO, blocos, textos e superfícies (contorno ou triângulos 3D) para KMZ"),
            ("Importar do Google Earth", "PRJKML", "Marcadores, caminhos e polígonos de um KML/KMZ para o desenho (pontos COGO e polilinhas), com cotas do arquivo ou de uma superfície"),
            ("Volume rápido", "PRJVOLRAPIDO", "Corte e aterro numa região (polilinha ou pontos clicados), mapa de cores transparente e tabela-resumo no desenho"),
            ("Faixas de cota / declividade", "PRJFAIXAS", "Colore a superfície por faixas de cota ou de declividade (classes da EMBRAPA) e insere a tabela de áreas"),
        }),
        ("Interseções e rotatórias", new[]
        {
            ("Montar a interseção", "PRJINTERSECAO", "Clique perto do cruzamento e selecione os eixos: esquinas com raio e greide, bordos, visibilidade de quem para no PARE, verificações, montagens, corredor (existente ou novo) e Excel"),
            ("Montar a rotatória", "PRJROTATORIA", "Clique no centro e selecione os eixos dos ramos: anel e greide, conexões de entrada e saída, ilhas divisórias, bordos, retenção, trajetória mais rápida (R1–R5), verificações e Excel"),
            ("Sinalização (CONTRAN)", "PRJSINALIZA", "Sinalização horizontal e vertical da interseção ou rotatória montada pelo PROJETA: retenção e dê a preferência, faixas de pedestres, PARE, eixo, faixa de conversão, placas R-1, R-2, R-33 / R-24a, verificações e quantitativo em Excel (MBST 2022)"),
            ("Plano semafórico (Webster)", "PRJSEMAFORO", "Semáforo de tempo fixo da interseção montada pelo PROJETA: estágios, entreverdes, ciclo de Webster, verdes, pedestres, grau de saturação, atraso e fila; grupos focais, laços, diagramas de estágios e de tempos e Excel (MBST Vol. V)"),
            ("Conferir se os eixos casam", "PRJCASAMENTO", "Clique em dois alinhamentos (ou Varios): se a ponta encosta no eixo, se é tangente e se as cotas e rampas dos greides batem; marca verde/vermelho e acerta a cota da ponta"),
            ("Conectar dois eixos", "PRJCONECTAREIXO", "Clique num alinhamento e no outro, perto do canto: cria a conexão (arco tangente nos eixos ou nos bordos) com o greide casando com os dois"),
        }),
        ("Terraplenagem", new[]
        {
            ("Platôs", "PRJPLATO", "Platô em cada polígono: cota que compensa corte e aterro (ou fixa), caimento, taludes até o terreno, superfícies, volumes e Excel"),
            ("Volumes por seções (cubação)", "PRJCUBACAO", "Nota de cubação pelo grupo de seções do Civil: lista de materiais ou áreas entre terreno e projeto, compensação lateral, Brückner e Excel"),
        }),
        ("Pavimentação", new[]
        {
            ("Nota de serviço (corredor)", "PRJNOTASERVICO", "Planilha Excel no layout E-NS: eixo, bordos, sarjeta, meio-fio e passeio a cada 10 m"),
        }),
        ("SWMM / EPANET", new[]
        {
            ("SWMM: simular", "PRJSWMMCALCULAR", "Exporta, roda o SWMM e mostra resultados"),
            ("SWMM: exportar", "PRJSWMMEXPORTAR", "Gera o arquivo .inp"),
            ("SWMM: importar", "PRJSWMMIMPORTAR", "Cria rede a partir de .inp"),
            ("EPANET: simular", "PRJEPANETCALCULAR", "Roda o EPANET (e ajusta diâmetros)"),
            ("EPANET: exportar", "PRJEPANETEXPORTAR", "Gera o arquivo .inp"),
            ("EPANET: importar", "PRJEPANETIMPORTAR", "Cria rede a partir de .inp"),
            ("Projeção populacional", "PRJPOPULACAO", "Aritmético, geométrico, logístico..."),
            ("Configurar motores", "PRJMOTORES", "Caminhos do runswmm/runepanet"),
        }),
        ("Modelador", new[]
        {
            ("Modelador", "PRJMODELADOR", "Cria e edita dispositivos e seções"),
            ("Seções-tipo", "PRJSECOES", "Abre o modelador nas seções-tipo dos dispositivos longitudinais"),
            ("Inserir dispositivo", "PRJDISPOSITIVO", "Insere um dispositivo avulso"),
            ("Biblioteca", "PRJBIBLIOTECA", "Abre a pasta da sua biblioteca de dispositivos e recarrega as definições"),
            ("Demonstração", "PRJDEMO", "Monta uma rede de exemplo completa (autoteste)"),
        }),
        ("Geral", new[]
        {
            ("Painel lateral", "PRJPAINEL", "Painel com todos os comandos do PROJETA"),
            ("Ponte com o Claude", "PRJMCP", "Liga ou desliga a ponte com o Claude (MCP), mostra o status e configura o início automático"),
            ("Ajuda", "PRJAJUDA", "Lista de comandos"),
            ("Recriar a aba", "PRJABA", "Recria a aba PROJETA na faixa de opções (se ela sumir ao trocar o espaço de trabalho)"),
        }),
    };

    /// <summary>
    /// Aplicativos de terceiros que ganham um grupo na aba e no painel quando estão instalados (pasta do bundle em
    /// ApplicationPlugins). Os botões só chamam os comandos deles; o aplicativo continua sendo do autor.
    /// </summary>
    public static readonly (string Grupo, string Bundle, (string Rotulo, string Comando, string Dica)[] Itens)[] Externos =
    {
        ("CTGeo (CivilTools)", "CivilTools.CTGeo.bundle", new[]
        {
            ("CTGeo", "CTGEO", "Paleta lateral com Street View, navegador e Google Earth Pro a partir de um ponto do desenho. CivilTools CTGeo, de Marcus Philippe A. P. Pinto"),
            ("Street View", "CTSTREET", "Street View a partir de um ponto do desenho (CTGeo)"),
            ("Navegador", "CTBROWSER", "Mapa no navegador a partir de um ponto do desenho (CTGeo)"),
            ("Google Earth Pro", "CTEARTH", "Google Earth Pro a partir de um ponto do desenho; precisa do Earth Pro instalado (CTGeo)"),
            ("Exportar KML", "CTGEOEXPORT", "Exporta KML (CTGeo)"),
            ("Abrir KML", "CTKMLABRIR", "Abre um KML (CTGeo)"),
            ("Importar KML", "CTKMLIMPORTAR", "Importa um KML para o desenho (CTGeo)"),
            ("CSTKML", "CSTKML", "Rotina LISP CSTKML V1.5, que vem com o CTGeo"),
            ("Coordenadas", "CTGEOCOORD", "Coordenadas de um ponto do desenho (CTGeo)"),
            ("Status do CTGeo", "CTGEOSTATUS", "Versão e diagnóstico do CTGeo (mande a mensagem ao autor se algo falhar)"),
        }),
    };

    /// <summary>Grupos do PROJETA mais os dos aplicativos de terceiros instalados.</summary>
    public static IReadOnlyList<(string Grupo, (string Rotulo, string Comando, string Dica)[] Itens)> Todos()
    {
        var r = Grupos.ToList();
        foreach (var (grupo, bundle, itens) in Externos)
            if (Instalado(bundle)) r.Add((grupo, itens));
        return r;
    }

    private static bool Instalado(string bundle)
    {
        foreach (var raiz in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                 })
            try { if (Directory.Exists(Path.Combine(raiz, "Autodesk", "ApplicationPlugins", bundle))) return true; }
            catch { }
        return false;
    }

    public PainelProjeta()
    {
        Font = UiUtil.FonteNormal;
        var fluxo = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(4) };
        var dicas = new ToolTip();
        foreach (var (grupo, itens) in Todos())
        {
            fluxo.Controls.Add(new Label { Text = grupo.ToUpperInvariant(), AutoSize = false, Width = 230, Height = 22, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.SteelBlue, Padding = new Padding(0, 6, 0, 0) });
            foreach (var (rotulo, cmd, dica) in itens)
            {
                var b = new Button { Text = rotulo, Width = 230, Height = 26, TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.System };
                string c = cmd;
                b.Click += (_, _) =>
                {
                    var doc = AcApp.DocumentManager.MdiActiveDocument;
                    doc?.SendStringToExecute("\x03\x03" + c + " ", true, false, true);
                };
                dicas.SetToolTip(b, $"{cmd} — {dica}");
                fluxo.Controls.Add(b);
            }
        }
        Controls.Add(fluxo);
    }
}
