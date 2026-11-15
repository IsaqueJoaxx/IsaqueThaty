using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Windows;
// O projeto tem a própria classe Geometry (contorno de polilinha); aqui é a do WPF.
using MediaGeometry = System.Windows.Media.Geometry;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;

namespace Projeta.Civil3D;

// Aba "Point top" no ribbon (antes "PROJETA": o nome ficou para a aba do PROJETA Assistente Civil). Criada por código a cada sessão
// (não grava nada no CUI). Painéis por tema: Terreno e volumes, Greide e perfil, Topografia, Vias e redes, Interseções e rotatórias.
// Os botões abrem o painel na aba certa ou rodam um comando: IR AO PERFIL só muda a vista; os comandos PRJ... são do PROJETA
// Assistente Civil (outro complemento): PRJVIAS, PRJLOCACAO, PRJSINALIZA, PRJSEMAFORO, PRJCAPACIDADE, PRJSEGURANCA,
// PRJALTERNATIVAS e PRJINTERFERENCIAS. Cada botão tem a dica com título, descrição e o comando.
internal static class ProjetaRibbon
{
    private const string TabId = "PROJETA_ASSISTENTE_TAB";
    private const string DoAssistente = "Comando do PROJETA Assistente Civil, que precisa estar instalado.";

    public static void Install()
    {
        if (ComponentManager.Ribbon is null)
        {
            ComponentManager.ItemInitialized -= OnItemInitialized;
            ComponentManager.ItemInitialized += OnItemInitialized;
            return;
        }
        Create();
    }

    private static void OnItemInitialized(object? sender, RibbonItemEventArgs e)
    {
        if (ComponentManager.Ribbon is null) return;
        ComponentManager.ItemInitialized -= OnItemInitialized;
        Create();
    }

    private static void Create()
    {
        var ribbon = ComponentManager.Ribbon;
        if (ribbon is null || ribbon.Tabs.Any(t => t.Id == TabId)) return;

        var terreno = Panel($"Terreno e volumes · IFM · {Commands.Target}",
            Page("Superfícies", "superficies", Icons.Surface, "Superfícies",
                 "Lista as superfícies do desenho e sugere qual é o terreno natural e qual é o projeto."),
            Page("Volumes", "volumes", Icons.Volume, "Volumes",
                 "Corte e aterro entre o terreno natural e o projeto, com limite e fatores de empolamento e contração."),
            Page("Seções", "secoes", Icons.Section, "Seções transversais",
                 "Linhas de amostragem e vistas de seção ao longo de um eixo."));

        var greide = Panel("Greide e perfil",
            Page("Greide", "greides", Icons.Grade, "Greide — IFM",
                 "Prévia e criação das tangentes do greide, com rampa máxima de até 7 %. As curvas verticais entram à mão."),
            Page("Correção\ndo greide", "correcao", Icons.Correction, "Correção do greide — IFM",
                 "Cotas do eixo e dos bordos; concreto de 23 cm; variação da correção de até 2,5 mm a cada 5 m.", "PROJETACORRECAO"),
            Page("IRI", "iri", Icons.Iri, "IRI rodoviário — IFM",
                 "Importa o IRI medido por trecho e compara as médias ponderadas com o limite informado.", "PROJETAIRI"),
            Command("Ir ao\nperfil", "irperfil", Icons.GoToProfile, "PROJETAPERFIL", "Ir ao perfil",
                    "Clique no eixo e a tela vai para a vista de perfil dele; clique na vista de perfil para voltar ao eixo. Enter sai.", proprio: true));

        var topografia = Panel("Topografia",
            Page("Poligonal", "poligonal", Icons.Traverse, "Poligonal — IFM",
                 "Importa a caderneta, ajusta a poligonal e gera o relatório de fechamento."),
            Command("Locação\nde campo", "locacao", Icons.Stakeout, "PRJLOCACAO", "Locação de campo",
                    "Pontos numerados dos meios-fios, ilhas, bordos e eixos (início, fim, vértices, PC, PT, centros e intermediários a cada X m), pontos COGO, quadros de coordenadas e de curvas na prancha, CSV para a estação total ou o GNSS e a nota de locação em Excel."));

        var vias = Panel("Vias e redes",
            Page("Corredores", "corredores", Icons.Corridor, "Corredores",
                 "Corredores em lote: eixo, greide de projeto e seção tipo."),
            Command("Vias do\nloteamento", "vias", Icons.Streets, "PRJVIAS", "Vias do loteamento",
                    "Selecione as polilinhas dos eixos das ruas: alinhamentos com curva nos vértices, perfil do terreno, greide passando na mesma cota nos encontros, vistas de perfil, corredor com superfície, linhas de amostragem e Excel."),
            Page("Redes", "redes", Icons.Network, "Redes — IFM",
                 "Esgoto (NBR 9649), escavação e quantitativos; renomear e associar redes de tubulação."));

        // Interseções e rotatórias montadas pelo PRJINTERSECAO / PRJROTATORIA (aba PROJETA).
        var intersecoes = Panel("Interseções e rotatórias",
            Command("Sinalização", "sinalizacao", Icons.Signal, "PRJSINALIZA", "Sinalização",
                    "Sinalização horizontal e vertical (CONTRAN / MBST) da interseção ou rotatória montada pelo PROJETA: linhas de retenção, faixas de pedestres, setas, legenda PARE, placas R-1, R-2, R-33 e R-24a e o quantitativo em Excel."),
            Command("Plano\nsemafórico", "semaforo", Icons.TrafficLight, "PRJSEMAFORO", "Plano semafórico",
                    "Semáforo de tempo fixo da interseção (MBST vol. V e Webster): estágios, amarelo, vermelho geral, ciclo e verdes, grupos focais, laços, diagramas e Excel."),
            Command("Capacidade\ne fila", "capacidade", Icons.Queue, "PRJCAPACIDADE", "Capacidade e fila",
                    "Capacidade, grau de saturação, atraso, nível de serviço e fila de 95 % de cada entrada da rotatória ou movimento com PARE (HCM 7ª ed.); faixas recomendadas, armazenamento da conversão, fila desenhada e Excel."),
            Command("Auditor de\nsegurança", "seguranca", Icons.Shield, "PRJSEGURANCA", "Auditor de segurança",
                    "Trajetória mais rápida (velocidade e deflexão de cada entrada), giro do veículo de projeto em todos os movimentos com a folga das rodas e visibilidade em 3D sobre as superfícies; envoltórias, visadas, problemas e REPROVADO no Excel."),
            Command("Comparar\nalternativas", "alternativas", Icons.Alternatives, "PRJALTERNATIVAS", "Comparador de alternativas",
                    "No mesmo cruzamento: PARE simples, canalizada, mini-rotatória e rotatórias de 1 e 2 faixas lado a lado, com área ocupada, pavimento, terraplenagem, custo pela sua tabela (SICRO/SINAPI), capacidade e pontos de conflito; pontuação e relatório de decisão em Excel."),
            Command("Interferências", "interferencias", Icons.Interference, "PRJINTERFERENCIAS", "Interferências e desapropriação",
                    "Cruza o limite da obra com os lotes (desenho, KML/KMZ ou shapefile) e com postes, árvores, redes, muros e edificações: área atingida por lote, o que é invadido, planta hachurada, quadros e Excel."));

        var tab = new RibbonTab { Title = "Point top", Id = TabId };
        foreach (var p in new[] { terreno, greide, topografia, vias, intersecoes }) tab.Panels.Add(new RibbonPanel { Source = p });
        ribbon.Tabs.Add(tab);
    }

    private static RibbonPanelSource Panel(string title, params RibbonItem[] items)
    {
        var source = new RibbonPanelSource { Title = title };
        foreach (var i in items) source.Items.Add(i);
        return source;
    }

    private static RibbonToolTip Tip(string title, string content, string? command) => new()
    {
        Title = title, Content = content, Command = command ?? "", IsHelpEnabled = false,
    };

    private static RibbonButton Large(string text, string id, string glyph, object tip) => new()
    {
        Text = text, ShowText = true, ShowImage = true, Size = RibbonItemSize.Large,
        Orientation = System.Windows.Controls.Orientation.Vertical,
        LargeImage = Icons.Draw(glyph, 32), Image = Icons.Draw(glyph, 16),
        ToolTip = tip, Id = "PROJETA_" + id,
    };

    // Botão que roda um comando (ver RunCommand); proprio = comando deste complemento.
    private static RibbonButton Command(string text, string id, string glyph, string command, string title, string tip, bool proprio = false)
    {
        var b = Large(text, id, glyph, Tip(title, proprio ? tip : tip + " " + DoAssistente, command));
        b.CommandParameter = command;
        b.CommandHandler = RunCommand.Instance;
        return b;
    }

    // Botão que abre o painel do Assistente na aba indicada.
    private static RibbonButton Page(string text, string page, string glyph, string title, string tip, string? command = null)
    {
        var b = Large(text, page, glyph, Tip(title, tip + " Abre a aba no painel do Assistente.", command));
        b.CommandParameter = page;
        b.CommandHandler = OpenPage.Instance;
        return b;
    }

    private sealed class OpenPage : ICommand
    {
        public static readonly OpenPage Instance = new();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter)
        {
            if (parameter is RibbonButton b) parameter = b.CommandParameter;
            Commands.ShowPage(parameter as string ?? "superficies");
        }
    }

    // Botão que roda um comando direto no desenho ativo (cancela antes o comando em andamento, como ^C^C).
    private sealed class RunCommand : ICommand
    {
        public static readonly RunCommand Instance = new();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter)
        {
            if (parameter is RibbonButton b) parameter = b.CommandParameter;
            var document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            if (document is null || parameter is not string command) return;
            // Comando de outro complemento (PRJ... é do PROJETA Assistente Civil): se ele não estiver carregado, avisa em vez de dar "comando desconhecido".
            if (!Autodesk.AutoCAD.Internal.Utils.IsCommandDefined(command))
            {
                document.Editor.WriteMessage($"\nO comando {command} é do PROJETA Assistente Civil, que não está carregado nesta sessão. Instale o PROJETA (Instalar-PROJETA.exe) e reabra o Civil 3D.\n");
                return;
            }
            document.SendStringToExecute("\x03\x03_" + command + " ", true, false, false);
        }
    }
}

// Ícones desenhados em código (traço verde-água, legível no tema claro e no escuro), um para cada botão.
internal static class Icons
{
    // Desenhos numa grade de 32 × 32.
    public const string Surface = "M3,25 L12,8 L20,19 L29,6 M3,25 L20,19 M12,8 L20,19 M3,25 L29,25 L29,6 M20,19 L29,25";
    public const string Volume = "M3,12 C9,6 15,18 21,10 C24,7 27,8 29,9 M3,20 L29,20 M3,27 L29,27";
    public const string Section = "M3,22 L9,22 L13,14 L19,14 L23,22 L29,22 M16,4 L16,28";
    public const string Network = "M3,9 L13,9 M19,9 L29,9 M16,12 L16,20 M3,23 L13,23 M19,23 L29,23 M13,6 L19,6 L19,12 L13,12 Z M13,20 L19,20 L19,26 L13,26 Z";
    public const string Profile = "M4,4 L4,28 L29,28 M4,22 C9,22 11,12 16,13 C21,14 23,9 29,8 M8,11 L13,11 M8,11 L10,9 M8,11 L10,13";
    // Greide: tangentes com os PIVs marcados sobre o terreno.
    public const string Grade = "M3,27 L29,27 M3,22 L11,14 L20,18 L29,7 M9.4,14 A1.6,1.6 0 1 0 12.6,14 A1.6,1.6 0 1 0 9.4,14 M18.4,18 A1.6,1.6 0 1 0 21.6,18 A1.6,1.6 0 1 0 18.4,18";
    // Correção: greide de projeto, perfil medido e as setas da correção.
    public const string Correction = "M3,9 L29,9 M3,20 C8,16 12,24 17,19 C21,15 25,22 29,18 M9,9 L9,17 M7,11.5 L9,9 L11,11.5 M22,9 L22,16 M20,11.5 L22,9 L24,11.5";
    // IRI: irregularidade da pista e o mostrador.
    public const string Iri = "M3,25 C4.5,21 6.5,21 8,25 C9.5,29 11.5,29 13,25 C14.5,21 16.5,21 18,25 C19.5,29 21.5,29 23,25 C24.5,21 26.5,21 28,25 M7,15 A9,9 0 0 1 25,15 M16,15 L21,8";
    // Ir ao perfil: o perfil e a seta de ida.
    public const string GoToProfile = "M3,28 L29,28 M3,22 C8,22 11,14 16,15 C21,16 23,11 29,10 M18,4 L28,4 M25,1.5 L28,4 L25,6.5";
    // Poligonal fechada com as estações.
    public const string Traverse = "M5,24 L10,7 L24,5 L28,20 L14,27 Z M3.5,24 L6.5,24 M8.5,7 L11.5,7 M22.5,5 L25.5,5 M26.5,20 L29.5,20 M12.5,27 L15.5,27";
    // Locação: tripé com a estação total.
    public const string Stakeout = "M11,4 L21,4 L21,11 L11,11 Z M16,11 L16,14 M16,14 L8,29 M16,14 L24,29 M16,14 L16,29 M14,7.5 L18,7.5";
    public const string Streets = "M3,25 L13,25 Q22,25 25,17 L28,8 M13,3 L13,29 M3,12 L13,12 M22,3 L22,12.5 M11,25 A2,2 0 1 0 15,25 A2,2 0 1 0 11,25 M11,12 A2,2 0 1 0 15,12 A2,2 0 1 0 11,12";
    public const string Corridor = "M5,29 C9,18 14,12 27,4 M11,29 C14,21 18,16 29,10 M8,29 C11.5,19.5 16,14 28,7";
    // Placa octogonal (PARE) no poste.
    public const string Signal = "M12.6,3.7 L19.4,3.7 L24.3,8.6 L24.3,15.4 L19.4,20.3 L12.6,20.3 L7.7,15.4 L7.7,8.6 Z M11.5,12 L20.5,12 M16,20.3 L16,29 M10,29 L22,29";
    // Semáforo de três focos no poste.
    public const string TrafficLight = "M11,2 L21,2 L21,23 L11,23 Z M16,23 L16,30 M12,30 L20,30 M13.8,6.5 A2.2,2.2 0 1 0 18.2,6.5 A2.2,2.2 0 1 0 13.8,6.5 M13.8,12.5 A2.2,2.2 0 1 0 18.2,12.5 A2.2,2.2 0 1 0 13.8,12.5 M13.8,18.5 A2.2,2.2 0 1 0 18.2,18.5 A2.2,2.2 0 1 0 13.8,18.5";
    // Fila de três veículos até a linha de retenção, com a cota do comprimento.
    public const string Queue = "M28,5 L28,27 M3,11 L8,11 L8,19 L3,19 Z M11,11 L16,11 L16,19 L11,19 Z M19,11 L24,11 L24,19 L19,19 Z M3,25 L24,25 M3,23 L3,27 M24,23 L24,27";
    // Escudo com o visto (auditoria de segurança).
    public const string Shield = "M16,3 L27,7 L27,15 C27,22 22,27 16,29 C10,27 5,22 5,15 L5,7 Z M10.5,16 L14.5,20 L22,11";
    // Alternativas: cruzamento simples × rotatória.
    public const string Alternatives = "M2,16 L12,16 M7,11 L7,21 M19,16 A4.5,4.5 0 1 0 28,16 A4.5,4.5 0 1 0 19,16 M16,16 L19,16 M28,16 L31,16 M23.5,8.5 L23.5,11.5 M23.5,20.5 L23.5,23.5 M14,4 L14,28";
    // Interferências: lote atingido e o poste.
    public const string Interference = "M3,10 L20,10 L20,28 L3,28 Z M26,4 L26,29 M22,8 L30,8 M23,12 L29,12";

    // Áreas preenchidas (verde-água translúcido) por baixo do traço.
    private static readonly Dictionary<string, string> Fills = new()
    {
        [Volume] = "M3,12 C9,6 15,18 21,10 C24,7 27,8 29,9 L29,20 L3,20 Z",
        [Grade] = "M3,22 L11,14 L20,18 L29,7 L29,27 L3,27 Z",
        [Correction] = "M3,9 L29,9 L29,18 C25,22 21,15 17,19 C12,24 8,16 3,20 Z",
        [Traverse] = "M5,24 L10,7 L24,5 L28,20 L14,27 Z",
        [Stakeout] = "M11,4 L21,4 L21,11 L11,11 Z",
        [Signal] = "M12.6,3.7 L19.4,3.7 L24.3,8.6 L24.3,15.4 L19.4,20.3 L12.6,20.3 L7.7,15.4 L7.7,8.6 Z",
        [TrafficLight] = "M11,2 L21,2 L21,23 L11,23 Z",
        [Queue] = "M3,11 L8,11 L8,19 L3,19 Z M11,11 L16,11 L16,19 L11,19 Z M19,11 L24,11 L24,19 L19,19 Z",
        [Shield] = "M16,3 L27,7 L27,15 C27,22 22,27 16,29 C10,27 5,22 5,15 L5,7 Z",
        [Alternatives] = "M19,16 A4.5,4.5 0 1 0 28,16 A4.5,4.5 0 1 0 19,16 Z",
        [Interference] = "M3,20 L20,15 L20,28 L3,28 Z",
    };

    private static readonly Brush Stroke = Frozen(new SolidColorBrush(Color.FromRgb(38, 166, 140)));
    private static readonly Brush Fill = Frozen(new SolidColorBrush(Color.FromArgb(70, 101, 212, 186)));

    private static T Frozen<T>(T f) where T : Freezable { f.Freeze(); return f; }

    public static ImageSource Draw(string path, int size)
    {
        var geometry = MediaGeometry.Parse(path);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(size / 32.0, size / 32.0));
            var pen = new Pen(Stroke, size >= 32 ? 2.4 : 3.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            if (Fills.TryGetValue(path, out var area)) dc.DrawGeometry(Fill, null, MediaGeometry.Parse(area));
            dc.DrawGeometry(null, pen, geometry);
            dc.Pop();
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
