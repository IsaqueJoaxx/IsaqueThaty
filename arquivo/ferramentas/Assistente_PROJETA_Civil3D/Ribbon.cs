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

// Aba "Point top" no ribbon (antes "PROJETA": o nome ficou para a aba do PROJETA Assistente Civil). Criada por código a cada sessão (não grava nada no CUI).
// Os botões abrem o painel na aba certa, ou rodam um comando: IR AO PERFIL só muda a vista; VIAS DO LOTEAMENTO roda o
// PRJVIAS do PROJETA Assistente Civil (outro complemento), que cria alinhamentos, perfis e corredor (um Ctrl+Z por etapa).
// O painel INTERSEÇÕES roda PRJSINALIZA, PRJSEMAFORO, PRJSEGURANCA e PRJCAPACIDADE, também do PROJETA Assistente Civil.
internal static class ProjetaRibbon
{
    private const string TabId = "PROJETA_ASSISTENTE_TAB";

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
        var source = new RibbonPanelSource { Title = "ASSISTENTE CIVIL — IFM · " + Commands.Target };
        source.Items.Add(Button("Superfícies", "superficies", Icons.Surface, "Lista as superfícies e sugere terreno natural e projeto."));
        source.Items.Add(Button("Volumes", "volumes", Icons.Volume, "Corte e aterro entre terreno natural e projeto, com limite e fatores."));
        source.Items.Add(Button("Seções", "secoes", Icons.Section, "Linhas de amostragem e vistas de seção ao longo de um eixo."));
        source.Items.Add(Button("Corredores", "corredores", Icons.Corridor, "Corredores em lote: eixo, greide de projeto e seção tipo."));
        source.Items.Add(Button("GREIDE — IFM", "greides", Icons.Volume, "Prévia e criação de tangentes; rampa máxima até 7%. Curvas inseridas manualmente."));
        source.Items.Add(Button("POLIGONAL — IFM", "poligonal", Icons.Section, "Importação de caderneta, ajuste e relatório de fechamento."));
        source.Items.Add(Button("CORREÇÃO DO GREIDE — IFM", "correcao", Icons.Profile, "Cotas do eixo e bordos; concreto de 23 cm; variação de correção até 2,5 mm a cada 5 m. Comando PROJETACORRECAO."));
        source.Items.Add(Button("IRI RODOVIÁRIO — IFM", "iri", Icons.Profile, "Importa IRI medido por trecho; médias ponderadas e comparação com limite informado. Comando: PROJETAIRI."));
        source.Items.Add(Button("REDES — IFM", "redes", Icons.Network, "Esgoto (NBR 9649), escavação e quantitativos, renomear e associar redes de tubulação."));
        source.Items.Add(new RibbonButton
        {
            Text = "IR AO PERFIL", ShowText = true, ShowImage = true, Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            LargeImage = Icons.Draw(Icons.Profile, 32), Image = Icons.Draw(Icons.Profile, 16),
            ToolTip = "Clique no eixo e a tela vai para a vista de perfil dele; clique na vista de perfil para voltar ao eixo. Enter sai. Comando: PROJETAPERFIL.",
            Id = "PROJETA_irperfil", CommandParameter = "PROJETAPERFIL", CommandHandler = RunCommand.Instance
        });
        source.Items.Add(Command("VIAS DO LOTEAMENTO", "vias", Icons.Streets, "PRJVIAS",
            "Selecione as polilinhas dos eixos das ruas: alinhamentos com curva nos vértices, perfil do terreno, greide proposto passando na mesma cota nos encontros, vistas de perfil, corredor com superfície, linhas de amostragem e Excel. Comando PRJVIAS (do PROJETA Assistente Civil, que precisa estar instalado)."));

        // Interseções e rotatórias montadas pelo PRJINTERSECAO / PRJROTATORIA (aba PROJETA).
        var intersecoes = new RibbonPanelSource { Title = "INTERSEÇÕES E ROTATÓRIAS" };
        intersecoes.Items.Add(Command("SINALIZAÇÃO", "sinalizacao", Icons.Signal, "PRJSINALIZA",
            "Sinalização horizontal e vertical (CONTRAN / MBST) da interseção ou rotatória montada pelo PROJETA: linhas de retenção, faixas de pedestres, setas, legenda PARE, placas R-1, R-2, R-33 e R-24a, e o quantitativo em Excel. Comando PRJSINALIZA (do PROJETA Assistente Civil, que precisa estar instalado)."));
        intersecoes.Items.Add(Command("PLANO SEMAFÓRICO", "semaforo", Icons.TrafficLight, "PRJSEMAFORO",
            "Plano semafórico da interseção (MBST vol. V e Webster): estágios, amarelo, vermelho geral, ciclo e verdes, grupos focais desenhados, laços e diagramas, e o Excel. Comando PRJSEMAFORO (do PROJETA Assistente Civil, que precisa estar instalado)."));
        intersecoes.Items.Add(Command("AUDITOR DE SEGURANÇA", "seguranca", Icons.Shield, "PRJSEGURANCA",
            "Auditor de segurança da rotatória montada pelo PROJETA: trajetória mais rápida (velocidade e deflexão de cada entrada), giro do veículo de projeto em todos os movimentos com a folga das rodas aos meios-fios e à ilha central, e visibilidade em 3D sobre as superfícies; desenha envoltórias, visadas e problemas, com REPROVADO no Excel. Comando PRJSEGURANCA (do PROJETA Assistente Civil, que precisa estar instalado)."));
        intersecoes.Items.Add(Command("CAPACIDADE E FILA", "capacidade", Icons.Queue, "PRJCAPACIDADE",
            "Capacidade, grau de saturação, atraso, nível de serviço e fila de 95 % de cada entrada da rotatória ou movimento da interseção com PARE (HCM 7ª ed.); faixas recomendadas no anel, armazenamento da faixa de conversão, fila desenhada e Excel. Comando PRJCAPACIDADE (do PROJETA Assistente Civil, que precisa estar instalado)."));

        var tab = new RibbonTab { Title = "Point top", Id = TabId };
        tab.Panels.Add(new RibbonPanel { Source = source });
        tab.Panels.Add(new RibbonPanel { Source = intersecoes });
        ribbon.Tabs.Add(tab);
    }

    // Botão que roda um comando do PROJETA Assistente Civil (ver RunCommand).
    private static RibbonButton Command(string text, string id, string glyph, string command, string tip) => new()
    {
        Text = text, ShowText = true, ShowImage = true, Size = RibbonItemSize.Large,
        Orientation = System.Windows.Controls.Orientation.Vertical,
        LargeImage = Icons.Draw(glyph, 32), Image = Icons.Draw(glyph, 16),
        ToolTip = tip, Id = "PROJETA_" + id, CommandParameter = command, CommandHandler = RunCommand.Instance
    };

    private static RibbonButton Button(string text, string page, string glyph, string tip) => new()
    {
        Text = text, ShowText = true, ShowImage = true, Size = RibbonItemSize.Large,
        Orientation = System.Windows.Controls.Orientation.Vertical,
        LargeImage = Icons.Draw(glyph, 32), Image = Icons.Draw(glyph, 16),
        ToolTip = tip, Id = "PROJETA_" + page, CommandParameter = page, CommandHandler = OpenPage.Instance
    };

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

// Ícones desenhados em código (traço verde-água, legível no tema claro e no escuro).
internal static class Icons
{
    // Desenhos numa grade de 32 × 32.
    public const string Surface = "M3,25 L12,8 L20,19 L29,6 M3,25 L20,19 M12,8 L20,19 M3,25 L29,25 L29,6 M20,19 L29,25";
    public const string Volume = "M3,12 C9,6 15,18 21,10 C24,7 27,8 29,9 M3,20 L29,20 M3,27 L29,27";
    public const string Section = "M3,22 L9,22 L13,14 L19,14 L23,22 L29,22 M16,4 L16,28";
    public const string Network = "M3,9 L13,9 M19,9 L29,9 M16,12 L16,20 M3,23 L13,23 M19,23 L29,23 M13,6 L19,6 L19,12 L13,12 Z M13,20 L19,20 L19,26 L13,26 Z";
    public const string Profile = "M4,4 L4,28 L29,28 M4,22 C9,22 11,12 16,13 C21,14 23,9 29,8 M8,11 L13,11 M8,11 L10,9 M8,11 L10,13";
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

    // Áreas preenchidas (verde-água translúcido) por baixo do traço.
    private static readonly Dictionary<string, string> Fills = new()
    {
        [Volume] = "M3,12 C9,6 15,18 21,10 C24,7 27,8 29,9 L29,20 L3,20 Z",
        [Signal] = "M12.6,3.7 L19.4,3.7 L24.3,8.6 L24.3,15.4 L19.4,20.3 L12.6,20.3 L7.7,15.4 L7.7,8.6 Z",
        [TrafficLight] = "M11,2 L21,2 L21,23 L11,23 Z",
        [Queue] = "M3,11 L8,11 L8,19 L3,19 Z M11,11 L16,11 L16,19 L11,19 Z M19,11 L24,11 L24,19 L19,19 Z",
        [Shield] = "M16,3 L27,7 L27,15 C27,22 22,27 16,29 C10,27 5,22 5,15 L5,7 Z",
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


