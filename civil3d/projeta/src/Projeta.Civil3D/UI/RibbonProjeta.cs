using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.UI;

/// <summary>
/// Aba PROJETA na faixa de opções do Civil 3D: um painel por grupo de <see cref="PainelProjeta.Grupos"/> e um botão por
/// comando (rótulo, ícone gerado com a sigla do comando na cor do grupo, dica com o comando e a descrição). A lista é a
/// mesma do painel lateral: comando novo entra nos dois. Recriada quando o espaço de trabalho muda (PRJABA também recria).
/// </summary>
internal static class RibbonProjeta
{
    private const string IdAba = "PROJETA_ABA_COMANDOS";
    private static bool _ligado;

    private static readonly System.Windows.Media.Color[] Cores =
    {
        System.Windows.Media.Color.FromRgb(0x1F, 0x6F, 0xB2), System.Windows.Media.Color.FromRgb(0x2E, 0x8B, 0x57),
        System.Windows.Media.Color.FromRgb(0xC2, 0x6A, 0x1B), System.Windows.Media.Color.FromRgb(0x7B, 0x4F, 0xA6),
        System.Windows.Media.Color.FromRgb(0x0F, 0x8A, 0x8A), System.Windows.Media.Color.FromRgb(0xB0, 0x3A, 0x3A),
        System.Windows.Media.Color.FromRgb(0x5D, 0x6D, 0x7E), System.Windows.Media.Color.FromRgb(0x8D, 0x6E, 0x2F),
        System.Windows.Media.Color.FromRgb(0x2B, 0x5C, 0x8A), System.Windows.Media.Color.FromRgb(0x6A, 0x8E, 0x23),
        System.Windows.Media.Color.FromRgb(0xA0, 0x40, 0x7A), System.Windows.Media.Color.FromRgb(0x3F, 0x51, 0xB5),
        System.Windows.Media.Color.FromRgb(0x55, 0x55, 0x55), System.Windows.Media.Color.FromRgb(0x00, 0x79, 0x6B),
    };

    /// <summary>Cria a aba agora, ou assim que a faixa de opções existir (o plugin carrega antes dela).</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Garantir()
    {
        if (!_ligado)
        {
            _ligado = true;
            AcApp.SystemVariableChanged += (_, e) =>
            {
                if (string.Equals(e.Name, "WSCURRENT", StringComparison.OrdinalIgnoreCase)) { try { Criar(false); } catch { } }
            };
        }
        if (ComponentManager.Ribbon is null) { ComponentManager.ItemInitialized += QuandoExistir; return; }
        Criar(false);
    }

    private static void QuandoExistir(object? sender, RibbonItemEventArgs e)
    {
        if (ComponentManager.Ribbon is null) return;
        ComponentManager.ItemInitialized -= QuandoExistir;
        try { Criar(false); } catch { }
    }

    /// <summary>(Re)cria a aba PROJETA; <paramref name="ativar"/> deixa ela na frente.</summary>
    public static bool Criar(bool ativar)
    {
        var ribbon = ComponentManager.Ribbon;
        if (ribbon is null) return false;
        foreach (var velha in ribbon.Tabs.Where(t => t.Id == IdAba).ToList()) ribbon.Tabs.Remove(velha);

        var aba = new RibbonTab { Title = "PROJETA", Id = IdAba };
        ribbon.Tabs.Add(aba);
        for (int g = 0; g < PainelProjeta.Grupos.Length; g++)
        {
            var (grupo, itens) = PainelProjeta.Grupos[g];
            var cor = Cores[g % Cores.Length];
            var fonte = new RibbonPanelSource { Title = grupo, Id = "PROJETA_" + g };
            if (itens.Length <= 3)
                foreach (var (rotulo, cmd, dica) in itens) fonte.Items.Add(Botao(rotulo, cmd, dica, cor, grande: true));
            else
                for (int i = 0; i < itens.Length; i += 3)
                {
                    var coluna = new RibbonRowPanel();
                    for (int k = i; k < Math.Min(i + 3, itens.Length); k++)
                    {
                        if (k > i) coluna.Items.Add(new RibbonRowBreak());
                        coluna.Items.Add(Botao(itens[k].Rotulo, itens[k].Comando, itens[k].Dica, cor, grande: false));
                    }
                    fonte.Items.Add(coluna);
                }
            aba.Panels.Add(new RibbonPanel { Source = fonte });
        }
        if (ativar) aba.IsActive = true;
        return true;
    }

    private static RibbonButton Botao(string rotulo, string cmd, string dica, System.Windows.Media.Color cor, bool grande) => new()
    {
        Id = "PROJETA_" + cmd,
        Text = grande ? Quebrar(rotulo) : rotulo,
        ShowText = true,
        ShowImage = true,
        Size = grande ? RibbonItemSize.Large : RibbonItemSize.Standard,
        Orientation = grande ? System.Windows.Controls.Orientation.Vertical : System.Windows.Controls.Orientation.Horizontal,
        Image = Icone(cmd, cor, 16),
        LargeImage = Icone(cmd, cor, 32),
        CommandParameter = cmd,
        CommandHandler = Executor.Instancia,
        ToolTip = new RibbonToolTip { Title = rotulo, Command = cmd, Content = dica },
    };

    /// <summary>Rótulo de botão grande em duas linhas (quebra no espaço mais perto do meio).</summary>
    private static string Quebrar(string s)
    {
        if (s.Length <= 12) return s;
        int meio = s.Length / 2, melhor = -1;
        for (int i = 0; i < s.Length; i++)
            if (s[i] == ' ' && (melhor < 0 || Math.Abs(i - meio) < Math.Abs(melhor - meio))) melhor = i;
        return melhor < 0 ? s : s[..melhor] + "\n" + s[(melhor + 1)..];
    }

    /// <summary>Ícone quadrado com a sigla do comando (sem o PRJ) na cor do grupo.</summary>
    private static ImageSource Icone(string cmd, System.Windows.Media.Color cor, int px)
    {
        string nome = cmd.StartsWith("PRJ", StringComparison.Ordinal) ? cmd[3..] : cmd;
        int n = px >= 32 ? 3 : 2;
        string sigla = nome.Length > n ? nome[..n] : nome;
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(cor), null, new System.Windows.Rect(0.5, 0.5, px - 1, px - 1), px * 0.2, px * 0.2);
            var ft = new FormattedText(sigla, CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight,
                new Typeface(new System.Windows.Media.FontFamily("Segoe UI"), System.Windows.FontStyles.Normal, System.Windows.FontWeights.Bold, System.Windows.FontStretches.Normal),
                px * (sigla.Length >= 3 ? 0.34 : 0.5), Brushes.White, 1.0);
            dc.DrawText(ft, new System.Windows.Point((px - ft.Width) / 2, (px - ft.Height) / 2));
        }
        var bmp = new RenderTargetBitmap(px, px, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);
        bmp.Freeze();
        return bmp;
    }

    /// <summary>Roda o comando do botão no desenho ativo (cancela o que estiver em andamento antes).</summary>
    private sealed class Executor : System.Windows.Input.ICommand
    {
        public static readonly Executor Instancia = new();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            string? cmd = parameter is RibbonCommandItem item ? item.CommandParameter as string : parameter as string;
            if (string.IsNullOrWhiteSpace(cmd)) return;
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute("\x03\x03" + cmd + " ", true, false, true);
        }
    }
}
