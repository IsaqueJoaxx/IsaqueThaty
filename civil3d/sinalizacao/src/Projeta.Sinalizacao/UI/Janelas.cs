using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.Windows;
using Projeta.Sinalizacao.Comandos;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Vertical;
using Projeta.Sinalizacao.Infra;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.UI;

/// <summary>Janelas e painel (somente com interface; não são usados no accoreconsole).</summary>
internal static class Janelas
{
    public static (DadosPlaca Dados, Situacao Situacao)? EscolherPlaca(Config cfg, N n)
    {
        using var f = new FormPlaca(cfg, n);
        if (AcApp.ShowModalDialog(f) != DialogResult.OK || f.Resultado is null) return null;
        return (f.Resultado, f.Situacao);
    }

    public static ModeloPlaca? Modelador(N n, double velocidade)
    {
        using var f = new FormModelador(n, velocidade);
        return AcApp.ShowModalDialog(f) == DialogResult.OK ? f.Resultado : null;
    }

    private static Form FormGrade(string titulo, object alvo, out PropertyGrid grade, string? extra = null)
    {
        var f = new Form
        {
            Text = titulo, Font = new Font("Segoe UI", 9f), StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(520, 600), MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.SizableToolWindow,
        };
        grade = new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = alvo, PropertySort = PropertySort.Categorized, ToolbarVisible = false };
        var baixo = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(6) };
        var ok = new Button { Text = "OK", Width = 90, DialogResult = DialogResult.OK };
        var canc = new Button { Text = "Cancelar", Width = 90, DialogResult = DialogResult.Cancel };
        baixo.Controls.Add(canc); baixo.Controls.Add(ok);
        if (extra is not null) baixo.Controls.Add(new CheckBox { Text = extra, AutoSize = true, Name = "extra", Padding = new Padding(0, 6, 20, 0) });
        f.Controls.Add(grade); f.Controls.Add(baixo);
        f.AcceptButton = ok; f.CancelButton = canc;
        return f;
    }

    public static bool Editar(ObjetoSin o, N n)
    {
        var ed = Edicao.Para(o);
        using var f = FormGrade($"PROJETA • {o.Id} — {o.Descricao}", ed, out _);
        if (AcApp.ShowModalDialog(f) != DialogResult.OK) return false;
        ed.Aplicar();
        return true;
    }

    public static (bool ok, bool padrao) EditarPadroes(Config cfg)
    {
        var copia = Config.DeJson(cfg.ParaJson())!;
        using var f = FormGrade("PROJETA • Padrões do projeto de sinalização", copia, out _, "Salvar também como padrão para novos desenhos");
        if (AcApp.ShowModalDialog(f) != DialogResult.OK) return (false, false);
        foreach (var p in typeof(Config).GetProperties().Where(p => p.CanWrite && p.CanRead)) p.SetValue(cfg, p.GetValue(copia));
        bool padrao = f.Controls.Find("extra", true).FirstOrDefault() is CheckBox cb && cb.Checked;
        return (true, padrao);
    }

    public static string? SalvarComo(string titulo, string filtro, string sugestao)
    {
        using var d = new System.Windows.Forms.SaveFileDialog { Title = titulo, Filter = filtro, FileName = Path.GetFileName(sugestao), InitialDirectory = Path.GetDirectoryName(sugestao), OverwritePrompt = true };
        return d.ShowDialog() == DialogResult.OK ? d.FileName : null;
    }

    public static string? Abrir(string titulo, string filtro)
    {
        using var d = new System.Windows.Forms.OpenFileDialog { Title = titulo, Filter = filtro };
        return d.ShowDialog() == DialogResult.OK ? d.FileName : null;
    }

    private static PaletteSet? _paleta;

    public static void MostrarPainel()
    {
        if (_paleta is null)
        {
            _paleta = new PaletteSet("PROJETA • Sinalização", new Guid("6C1E4F2A-8B3D-4E59-9A71-2D5C8F0B3E47"))
            {
                Style = PaletteSetStyles.ShowCloseButton | PaletteSetStyles.ShowAutoHideButton | PaletteSetStyles.ShowPropertiesMenu,
                MinimumSize = new Size(260, 400),
            };
            _paleta.Add("Sinalização", new Painel());
            _paleta.Size = new Size(280, 760);
            _paleta.Dock = DockSides.Left;
        }
        _paleta.Visible = true;
    }
}

/// <summary>Painel lateral com os comandos agrupados.</summary>
internal sealed class Painel : UserControl
{
    public Painel()
    {
        Font = new Font("Segoe UI", 9f);
        var fluxo = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(4) };
        var dicas = new ToolTip();
        fluxo.Controls.Add(new Label { Text = "PROJETA • Sinalização Viária", AutoSize = false, Width = 240, Height = 26, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(47, 93, 80) });
        foreach (var (grupo, itens) in CmdGeral.Grupos)
        {
            fluxo.Controls.Add(new Label { Text = grupo.ToUpperInvariant(), AutoSize = false, Width = 240, Height = 22, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.SteelBlue, Padding = new Padding(0, 6, 0, 0) });
            foreach (var (rotulo, cmd, dica) in itens)
            {
                var b = new Button { Text = rotulo, Width = 240, Height = 26, TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.System };
                string c = cmd;
                b.Click += (_, _) => AcApp.DocumentManager.MdiActiveDocument?.SendStringToExecute("\x03\x03" + c + " ", true, false, true);
                dicas.SetToolTip(b, $"{cmd} — {dica}");
                fluxo.Controls.Add(b);
            }
        }
        Controls.Add(fluxo);
    }
}
