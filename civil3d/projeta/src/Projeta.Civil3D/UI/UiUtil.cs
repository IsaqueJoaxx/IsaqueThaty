using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Projeta.Core.Comum;
using Projeta.Core.Modelador;
using Projeta.Core.Relatorios;

namespace Projeta.Civil3D.UI;

internal static class UiUtil
{
    public static readonly Font FonteMono = new("Consolas", 9.5f);
    public static readonly Font FonteNormal = new("Segoe UI", 9f);

    public static Button Botao(string texto, EventHandler click, int largura = 110)
    {
        var b = new Button { Text = texto, Width = largura, Height = 28, Margin = new Padding(4) };
        b.Click += click;
        return b;
    }

    public static FlowLayoutPanel BarraBotoes(params Control[] botoes)
    {
        var f = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(4) };
        f.Controls.AddRange(botoes);
        return f;
    }

    /// <summary>FILEDIA = 0 (scripts, console): os nomes de arquivo são pedidos na linha de comando.</summary>
    private static bool SemDialogo()
    {
        try { return Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("FILEDIA")) == 0; }
        catch { return false; }
    }

    private static string? PedirCaminho(string msg, string sugestao)
    {
        var o = new Autodesk.AutoCAD.EditorInput.PromptStringOptions($"\n{msg}{(sugestao.Length > 0 ? $" <{sugestao}>" : "")}: ") { AllowSpaces = true };
        var r = Infra.Ctx.Ed.GetString(o);
        if (r.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return null;
        var c = r.StringResult.Trim().Trim('"');
        if (c.Length == 0) c = sugestao;
        return c.Length == 0 ? null : c;
    }

    public static string? SalvarArquivo(string filtro, string nomeSugerido)
    {
        if (SemDialogo())
        {
            var p0 = Infra.Config.Atual.UltimaPasta;
            if (string.IsNullOrEmpty(p0) || !Directory.Exists(p0)) p0 = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return PedirCaminho("Arquivo para gravar", Path.Combine(p0, nomeSugerido));
        }
        using var d = new SaveFileDialog { Filter = filtro, FileName = nomeSugerido, OverwritePrompt = true };
        var pasta = Infra.Config.Atual.UltimaPasta;
        if (!string.IsNullOrEmpty(pasta) && Directory.Exists(pasta)) d.InitialDirectory = pasta;
        if (d.ShowDialog() != DialogResult.OK) return null;
        Infra.Config.Atual.UltimaPasta = Path.GetDirectoryName(d.FileName);
        try { Infra.Config.Atual.Salvar(); } catch { }
        return d.FileName;
    }

    public static string? AbrirArquivo(string filtro)
    {
        if (SemDialogo())
        {
            var c = PedirCaminho("Arquivo para abrir", "");
            if (c is not null && !File.Exists(c)) { Infra.Ctx.Msg("Arquivo não encontrado: " + c); return null; }
            return c;
        }
        using var d = new OpenFileDialog { Filter = filtro, CheckFileExists = true };
        var pasta = Infra.Config.Atual.UltimaPasta;
        if (!string.IsNullOrEmpty(pasta) && Directory.Exists(pasta)) d.InitialDirectory = pasta;
        if (d.ShowDialog() != DialogResult.OK) return null;
        Infra.Config.Atual.UltimaPasta = Path.GetDirectoryName(d.FileName);
        try { Infra.Config.Atual.Salvar(); } catch { }
        return d.FileName;
    }

    public static void AbrirNoSistema(string caminho)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(caminho) { UseShellExecute = true }); }
        catch { }
    }

    public static DataGridView Grade(bool somenteLeitura = true)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = somenteLeitura,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            BackgroundColor = SystemColors.Window,
            Font = FonteNormal,
        };
        g.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return g;
    }

    public static void PreencherGrade(DataGridView g, TabelaDados t)
    {
        g.Columns.Clear();
        g.Rows.Clear();
        foreach (var c in t.Colunas) g.Columns.Add(c, c);
        if (g.Columns.Count > 0) g.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        for (int i = 0; i < t.Linhas.Count; i++)
        {
            int r = g.Rows.Add(t.Linhas[i]);
            if (i < t.Situacao.Count) Colorir(g.Rows[r], t.Situacao[i]);
        }
    }

    public static void Colorir(DataGridViewRow row, string situacao)
    {
        row.DefaultCellStyle.BackColor = situacao switch
        {
            "ERRO" => Color.FromArgb(253, 228, 228),
            "AVISO" => Color.FromArgb(255, 247, 224),
            _ => SystemColors.Window,
        };
    }

    // ------------------------------------------------------------------ pré-visualizações

    /// <summary>Desenha elementos de planta (2D) centralizados no retângulo.</summary>
    public static void DesenharPlanta(Graphics g, Rectangle area, IReadOnlyList<Planta2D> elementos)
    {
        g.Clear(Color.FromArgb(32, 36, 44));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var pts = new List<P2>();
        foreach (var e in elementos)
            switch (e)
            {
                case PlCirculo c: pts.Add(new P2(c.Centro.X - c.Raio, c.Centro.Y - c.Raio)); pts.Add(new P2(c.Centro.X + c.Raio, c.Centro.Y + c.Raio)); break;
                case PlArco a: pts.Add(new P2(a.Centro.X - a.Raio, a.Centro.Y - a.Raio)); pts.Add(new P2(a.Centro.X + a.Raio, a.Centro.Y + a.Raio)); break;
                case PlPolilinha p: pts.AddRange(p.Pontos); break;
                case PlTexto t: pts.Add(t.Posicao); break;
            }
        if (pts.Count == 0) { g.DrawString("(sem elementos de planta)", FonteNormal, Brushes.Gray, 8, 8); return; }
        var (esc, ox, oy) = Enquadrar(area, pts);
        PointF T(P2 p) => new((float)(ox + p.X * esc), (float)(oy - p.Y * esc));
        foreach (var e in elementos)
        {
            using var pen = new Pen(e.Camada switch { "tampa" => Color.Gold, "eixo" => Color.Gray, _ => Color.Cyan }, 1.5f);
            switch (e)
            {
                case PlCirculo c:
                    {
                        var p = T(new P2(c.Centro.X - c.Raio, c.Centro.Y + c.Raio));
                        float d = (float)(2 * c.Raio * esc);
                        if (d > 0.5f) g.DrawEllipse(pen, p.X, p.Y, d, d);
                        break;
                    }
                case PlArco a:
                    {
                        var p = T(new P2(a.Centro.X - a.Raio, a.Centro.Y + a.Raio));
                        float d = (float)(2 * a.Raio * esc);
                        if (d > 0.5f) g.DrawArc(pen, p.X, p.Y, d, d, (float)-a.DeGraus, (float)-(a.AteGraus - a.DeGraus));
                        break;
                    }
                case PlPolilinha pl when pl.Pontos.Count >= 2:
                    {
                        var arr = pl.Pontos.Select(T).ToArray();
                        if (pl.Fechada && arr.Length >= 3) g.DrawPolygon(pen, arr); else g.DrawLines(pen, arr);
                        break;
                    }
                case PlTexto tx:
                    g.DrawString(tx.Texto, FonteNormal, Brushes.White, T(tx.Posicao));
                    break;
            }
        }
        DesenharEixos(g, T(new P2(0, 0)));
    }

    /// <summary>Desenha uma seção-tipo: concreto (cinza) e contorno hidráulico (azul).</summary>
    public static void DesenharSecao(Graphics g, Rectangle area, AvaliadorDispositivo.SecaoAvaliada s)
    {
        g.Clear(Color.FromArgb(32, 36, 44));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var pts = s.Concreto.Concat(s.Hidraulico).Concat(s.Escavacao).ToList();
        if (pts.Count == 0) { g.DrawString("(seção vazia)", FonteNormal, Brushes.Gray, 8, 8); return; }
        var (esc, ox, oy) = Enquadrar(area, pts);
        PointF T(P2 p) => new((float)(ox + p.X * esc), (float)(oy - p.Y * esc));
        if (s.Escavacao.Count >= 3) using (var pe = new Pen(Color.FromArgb(120, 160, 120, 80), 1f) { DashStyle = DashStyle.Dash }) g.DrawPolygon(pe, s.Escavacao.Select(T).ToArray());
        if (s.Concreto.Count >= 3)
        {
            using var br = new SolidBrush(Color.FromArgb(150, 170, 170, 170));
            g.FillPolygon(br, s.Concreto.Select(T).ToArray());
            g.DrawPolygon(Pens.Silver, s.Concreto.Select(T).ToArray());
        }
        if (s.Hidraulico.Count >= 2) using (var ph = new Pen(Color.DeepSkyBlue, 2f)) g.DrawLines(ph, s.Hidraulico.Select(T).ToArray());
        DesenharEixos(g, T(new P2(0, 0)));
        g.DrawString($"Concreto: {Num.F(s.AreaConcreto, 4)} m²/m   Perímetro revestido: {Num.F(s.PerimetroRevestido, 3)} m", FonteNormal, Brushes.White, 6, area.Bottom - 20);
    }

    private static (double esc, double ox, double oy) Enquadrar(Rectangle area, List<P2> pts)
    {
        double x0 = pts.Min(p => p.X), x1 = pts.Max(p => p.X), y0 = pts.Min(p => p.Y), y1 = pts.Max(p => p.Y);
        double w = Math.Max(x1 - x0, 1e-3), h = Math.Max(y1 - y0, 1e-3);
        double esc = Math.Min((area.Width - 40) / w, (area.Height - 50) / h);
        double ox = area.Left + area.Width / 2.0 - (x0 + x1) / 2 * esc;
        double oy = area.Top + (area.Height - 20) / 2.0 + (y0 + y1) / 2 * esc;
        return (esc, ox, oy);
    }

    private static void DesenharEixos(Graphics g, PointF o)
    {
        using var p = new Pen(Color.FromArgb(90, 255, 255, 255), 1f) { DashStyle = DashStyle.Dot };
        g.DrawLine(p, o.X - 12, o.Y, o.X + 12, o.Y);
        g.DrawLine(p, o.X, o.Y - 12, o.X, o.Y + 12);
    }
}
