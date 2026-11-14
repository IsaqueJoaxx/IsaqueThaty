namespace Projeta.Instalador;

/// <summary>Janela do instalador: escolhe os componentes e a pasta, instala e mostra o que foi feito.</summary>
internal sealed class Janela : Form
{
    private readonly CheckBox _civil, _mcp, _claude, _pointTop, _iri, _lisp;
    private readonly TextBox _pasta, _log;
    private readonly Button _instalar, _fechar, _escolher;
    private readonly ProgressBar _barra;

    public Janela()
    {
        Text = "Instalar PROJETA";
        Font = new Font("Segoe UI", 9.5f);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(640, 600);

        var civil = Instalador.CivilInstalado();
        var titulo = new Label { Text = "PROJETA", Font = new Font("Segoe UI", 18f, FontStyle.Bold), AutoSize = true, Location = new Point(18, 12) };
        var versao = new Label
        {
            Text = $"Versão {Instalador.Versao()} · Civil 3D nesta máquina: " + (civil.Count == 0 ? "não encontrado" : string.Join(", ", civil)),
            AutoSize = true, ForeColor = Color.DimGray, Location = new Point(20, 52),
        };
        int y = 86;
        CheckBox Caixa(string texto, string dica, bool marcado, int recuo = 0)
        {
            var c = new CheckBox { Text = texto, Checked = marcado, AutoSize = true, Location = new Point(22 + recuo, y) };
            var l = new Label { Text = dica, AutoSize = false, Size = new Size(580 - recuo, 34), ForeColor = Color.DimGray, Location = new Point(40 + recuo, y + 22) };
            Controls.Add(c); Controls.Add(l);
            y += 58;
            return c;
        }
        _civil = Caixa("Plugins do Civil 3D", "PROJETA (redes, volumes, platôs, cubação, nota de serviço...), Giro, Sinalização, Terraplenagem IFM e IQ C3D, para o Civil 3D 2026 e 2027.", true);
        _mcp = Caixa("Servidor MCP do PROJETA", "Deixa o Claude conversar com o Civil 3D aberto (no Civil: PRJMCP → Iniciar).", true);
        y -= 18;
        _claude = Caixa("Registrar no Claude Desktop", "Inclui o servidor na configuração do Claude Desktop (faz cópia de segurança antes).", false, 20);
        _pointTop = Caixa("POINT TOP", "Volumes por GeoTIFF, eixos e greide no navegador. Vem com Node e Python próprios: não precisa de QGIS.", true);
        _iri = Caixa("IRI Rodoviário", "Cálculo do IRI a partir do levantamento.", true);
        _lisp = Caixa("LISPs", "Rotinas AutoLISP (greide automático, rotatórias, quadros, KML...). Carregue com APPLOAD.", true);
        _mcp.CheckedChanged += (_, _) => { _claude.Enabled = _mcp.Checked; if (!_mcp.Checked) _claude.Checked = false; };

        var lp = new Label { Text = "Pasta dos programas (POINT TOP, IRI, LISPs):", AutoSize = true, Location = new Point(20, y + 4) };
        _pasta = new TextBox { Text = new Destinos().Programas, Location = new Point(22, y + 26), Width = 520 };
        _escolher = new Button { Text = "...", Location = new Point(550, y + 25), Width = 60 };
        _escolher.Click += (_, _) =>
        {
            using var f = new FolderBrowserDialog { SelectedPath = _pasta.Text, Description = "Pasta dos programas do PROJETA" };
            if (f.ShowDialog(this) == DialogResult.OK) _pasta.Text = Path.Combine(f.SelectedPath, "PROJETA");
        };
        y += 62;
        _barra = new ProgressBar { Location = new Point(22, y), Width = 588, Height = 14 };
        _log = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(22, y + 22), Size = new Size(588, 600 - y - 80), Font = new Font("Consolas", 8.5f) };
        _instalar = new Button { Text = "Instalar", Location = new Point(420, 556), Size = new Size(90, 30) };
        _fechar = new Button { Text = "Fechar", Location = new Point(520, 556), Size = new Size(90, 30) };
        _instalar.Click += async (_, _) => await Instalar();
        _fechar.Click += (_, _) => Close();
        AcceptButton = _instalar;
        Controls.AddRange(new Control[] { titulo, versao, lp, _pasta, _escolher, _barra, _log, _instalar, _fechar });
        if (Instalador.Pacote() is null) { _instalar.Enabled = false; Log("Este instalador foi gerado sem o pacote (rode o build.ps1)."); }
        else Log("Feche o Civil 3D antes de instalar (se ele estiver aberto, a versão nova vale quando ele abrir de novo).");
    }

    private void Log(string s)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(s)); return; }
        _log.AppendText(s + Environment.NewLine);
    }

    private async Task Instalar()
    {
        var c = Componentes.Nenhum;
        if (_civil.Checked) c |= Componentes.Civil;
        if (_mcp.Checked) c |= Componentes.Mcp;
        if (_claude.Checked) c |= Componentes.Claude;
        if (_pointTop.Checked) c |= Componentes.PointTop;
        if (_iri.Checked) c |= Componentes.Iri;
        if (_lisp.Checked) c |= Componentes.Lisp;
        if (c == Componentes.Nenhum) { MessageBox.Show(this, "Escolha pelo menos um componente.", Text); return; }
        var d = new Destinos { Programas = _pasta.Text.Trim() };
        foreach (Control x in Controls) if (x is CheckBox or Button or TextBox { ReadOnly: false }) x.Enabled = false;
        _fechar.Enabled = false;
        _log.Clear();
        var inst = new Instalador(Log, p => BeginInvoke(() => _barra.Value = Math.Clamp(p, 0, 100)));
        try
        {
            await Task.Run(() => inst.Instalar(c, d));
            MessageBox.Show(this, inst.Avisos.Count == 0 ? "PROJETA instalado." : "PROJETA instalado, com avisos (veja o registro).", Text, MessageBoxButtons.OK,
                            inst.Avisos.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            Log("ERRO: " + ex.Message);
            MessageBox.Show(this, "A instalação não terminou: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _fechar.Enabled = true; }
    }
}
