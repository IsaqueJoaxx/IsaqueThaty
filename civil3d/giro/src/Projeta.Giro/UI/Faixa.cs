using System.Windows.Input;
using Autodesk.Windows;
using Projeta.Giro.Infra;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Giro.UI;

/// <summary>Aba "PROJETA Giro" na faixa de opções.</summary>
internal static class Faixa
{
    private const string IdAba = "PROJETA_GIRO_ABA";
    private static bool _inscrito;

    public static void Iniciar()
    {
        if (ComponentManager.Ribbon is not null) Criar();
        else ComponentManager.ItemInitialized += AoInicializar;
        if (!_inscrito)
        {
            _inscrito = true;
            AcApp.SystemVariableChanged += (_, e) =>
            {
                if (string.Equals(e.Name, "WSCURRENT", StringComparison.OrdinalIgnoreCase)) try { Criar(); } catch { }
            };
        }
    }

    private static void AoInicializar(object? sender, RibbonItemEventArgs e)
    {
        if (ComponentManager.Ribbon is null) return;
        ComponentManager.ItemInitialized -= AoInicializar;
        try { Criar(); } catch { }
    }

    private static void Criar()
    {
        var faixa = ComponentManager.Ribbon;
        if (faixa is null || faixa.Tabs.Any(t => t.Id == IdAba)) return;
        var aba = new RibbonTab { Title = "PROJETA Giro", Id = IdAba };

        aba.Panels.Add(Painel("Veículo",
            Botao("Veículo de\nprojeto", "GIROVEICULO", Icones.Veiculo, "Escolhe o veículo de projeto (DNIT VP, CO, O, SR, RE ou do usuário)."),
            Botao("Gabarito\nde giro", "GIROGABARITO", Icones.Gabarito, "Gabarito de giro de 30° a 180° no raio mínimo, com transição de volante.")));

        aba.Panels.Add(Painel("Caminho de varredura",
            Botao("Arco\nautomático", "GIROARCO", Icones.Arco, "Condução por pontos: giro com transição de volante e reta até cada ponto. Ré para veículos rígidos."),
            Botao("Seguir\ntrajetória", "GIROSEGUIR", Icones.Seguir, "O eixo dianteiro segue uma polilinha, linha, arco, alinhamento ou feature line."),
            Botao("Atualizar", "GIROATUALIZAR", Icones.Atualizar, "Refaz a varredura: caminho editado, outro veículo, velocidade ou veículos a cada X m.")));

        aba.Panels.Add(Painel("Verificação e animação",
            Botao("Verificar\nconflitos", "GIROVERIFICAR", Icones.Verificar, "Confronta a envoltória com meios-fios e obstáculos; marca invasões e falta de folga."),
            Botao("Animar", "GIROANIMAR", Icones.Animar, "Anima o veículo ao longo da varredura (Esc para parar)."),
            Botao("Relatório", "GIRORELATORIO", Icones.Relatorio, "Largura varrida, esterçamento, articulação e avisos da manobra.")));

        aba.Panels.Add(Painel("Ajustes",
            Botao("Configurar", "GIROCONFIG", Icones.Config, "Velocidade de manobra, folga, intervalo dos veículos e rodas."),
            Botao("Exemplo", "GIRODEMO", Icones.Demo, "Desenha um cruzamento de exemplo com varreduras e verificação."),
            Botao("Ajuda", "GIROAJUDA", Icones.Ajuda, "Lista de comandos e premissas.")));

        faixa.Tabs.Add(aba);
    }

    private static RibbonPanel Painel(string titulo, params RibbonItem[] itens)
    {
        var src = new RibbonPanelSource { Title = titulo };
        foreach (var i in itens) src.Items.Add(i);
        return new RibbonPanel { Source = src };
    }

    private static RibbonButton Botao(string texto, string comando, Func<int, System.Windows.Media.ImageSource> icone, string dica) => new()
    {
        Text = texto,
        ShowText = true,
        ShowImage = true,
        LargeImage = icone(32),
        Image = icone(16),
        Size = RibbonItemSize.Large,
        Orientation = System.Windows.Controls.Orientation.Vertical,
        CommandParameter = "_" + comando + " ",
        CommandHandler = Comando.Instancia,
        ToolTip = new RibbonToolTip { Title = texto.Replace("\n", " "), Content = dica, Command = comando },
        Id = "PROJETA_GIRO_" + comando,
    };

    private sealed class Comando : ICommand
    {
        public static readonly Comando Instancia = new();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter)
        {
            if (parameter is not RibbonCommandItem b || b.CommandParameter is not string cmd) return;
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute("\u0003\u0003" + cmd, true, false, true);
        }
    }
}
