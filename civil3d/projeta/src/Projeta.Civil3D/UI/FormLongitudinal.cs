using System.Drawing;
using System.Windows.Forms;
using Projeta.Core.Comum;
using Projeta.Core.Longitudinais;
using Projeta.Core.Modelador;

namespace Projeta.Civil3D.UI;

/// <summary>Configuração de um dispositivo longitudinal: seção-tipo, parâmetros, locação e hidráulica.</summary>
internal sealed class FormLongitudinal : Form
{
    private readonly DispositivoLongitudinal _d;
    private readonly Biblioteca _bib;
    private readonly ComboBox _secao = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
    private readonly ComboBox _perfil = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly ComboBox _superficie = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly ComboBox _equacao = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly DataGridView _params = UiUtil.Grade(false);
    private readonly PropertyGrid _pg = new() { Dock = DockStyle.Fill, ToolbarVisible = false, PropertySort = PropertySort.Categorized };
    private readonly PictureBox _preview = new() { Dock = DockStyle.Fill };
    private AvaliadorDispositivo.SecaoAvaliada? _sec;
    private readonly List<(string Handle, string Nome)> _perfis, _superficies;
    private readonly List<(string Id, string Nome)> _equacoes;

    public FormLongitudinal(DispositivoLongitudinal d, Biblioteca bib, List<(string Handle, string Nome)> perfis,
        List<(string Handle, string Nome)> superficies, List<(string Id, string Nome)> equacoes)
    {
        _d = d; _bib = bib; _perfis = perfis; _superficies = superficies; _equacoes = equacoes;
        Text = $"PROJETA — Dispositivo longitudinal ({d.NomeAlinhamento})";
        Size = new Size(1250, 760);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiUtil.FonteNormal;

        foreach (var s in bib.Secoes.Values.OrderBy(x => x.Categoria).ThenBy(x => x.Nome)) _secao.Items.Add(s);
        foreach (var p in perfis) _perfil.Items.Add(p.Nome);
        foreach (var s in superficies) _superficie.Items.Add(s.Nome);
        foreach (var e in equacoes) _equacao.Items.Add(e.Nome);

        var topo = new TableLayoutPanel { Dock = DockStyle.Top, Height = 110, ColumnCount = 4, Padding = new Padding(6) };
        topo.Controls.Add(new Label { Text = "Seção-tipo", AutoSize = true }, 0, 0); topo.Controls.Add(_secao, 1, 0);
        topo.Controls.Add(new Label { Text = "Equação de chuva", AutoSize = true }, 2, 0); topo.Controls.Add(_equacao, 3, 0);
        topo.Controls.Add(new Label { Text = "Perfil (greide)", AutoSize = true }, 0, 1); topo.Controls.Add(_perfil, 1, 1);
        topo.Controls.Add(new Label { Text = "Superfície", AutoSize = true }, 2, 1); topo.Controls.Add(_superficie, 3, 1);
        topo.Controls.Add(new Label { Text = "A cota vem do perfil ou da superfície conforme \"Cota a partir de\" (abaixo). X da seção cresce para a direita do alinhamento; use \"Espelhar\" no lado esquerdo.", AutoSize = true, MaximumSize = new Size(1100, 0) }, 0, 2);
        topo.SetColumnSpan(topo.GetControlFromPosition(0, 2)!, 4);

        _params.Columns.Add("Nome", "Parâmetro");
        _params.Columns.Add("Valor", "Valor");
        _params.Columns.Add("Desc", "Descrição");
        _params.Columns[0].ReadOnly = true; _params.Columns[2].ReadOnly = true;
        _params.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _params.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _params.CellEndEdit += (_, _) => { LerParametros(); Visualizar(); };
        _preview.Paint += (_, e) => { if (_sec is not null) UiUtil.DesenharSecao(e.Graphics, _preview.ClientRectangle, _sec); };
        _preview.Resize += (_, _) => _preview.Invalidate();

        _pg.SelectedObject = d;
        var div = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 520 };
        var esq = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };
        esq.Panel1.Controls.Add(_params);
        esq.Panel2.Controls.Add(_preview);
        div.Panel1.Controls.Add(esq);
        div.Panel2.Controls.Add(_pg);

        var ok = new Button { Text = "Gerar", Width = 110, Height = 28 };
        ok.Click += (_, _) => { if (Aplicar()) DialogResult = DialogResult.OK; };
        Controls.Add(div);
        Controls.Add(topo);
        Controls.Add(UiUtil.BarraBotoes(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 }, ok));

        _secao.SelectedIndexChanged += (_, _) => { if (_secao.SelectedItem is SecaoTipo s) { if (_d.SecaoId != s.Id) _d.Parametros.Clear(); _d.SecaoId = s.Id; PreencherParametros(); Visualizar(); } };
        for (int i = 0; i < _secao.Items.Count; i++) if (((SecaoTipo)_secao.Items[i]!).Id == d.SecaoId) _secao.SelectedIndex = i;
        if (_secao.SelectedIndex < 0 && _secao.Items.Count > 0) _secao.SelectedIndex = 0;
        _perfil.SelectedIndex = Math.Max(perfis.FindIndex(p => p.Handle == d.HandlePerfil), perfis.Count > 0 ? 0 : -1);
        _superficie.SelectedIndex = Math.Max(superficies.FindIndex(p => p.Handle == d.HandleSuperficie), superficies.Count > 0 ? 0 : -1);
        _equacao.SelectedIndex = Math.Max(equacoes.FindIndex(p => p.Id == d.EquacaoId), equacoes.Count > 0 ? 0 : -1);
    }

    private void PreencherParametros()
    {
        _params.Rows.Clear();
        if (_secao.SelectedItem is not SecaoTipo s) return;
        foreach (var p in s.Parametros)
            _params.Rows.Add(p.Nome, _d.Parametros.TryGetValue(p.Nome, out var v) ? v.ToString("0.####", Num.Br) : p.Padrao.Texto, $"{p.Descricao} ({p.Unidade})");
    }

    private void LerParametros()
    {
        if (_secao.SelectedItem is not SecaoTipo s) return;
        foreach (DataGridViewRow r in _params.Rows)
        {
            var nome = r.Cells[0].Value?.ToString() ?? "";
            var def = s.Parametros.FirstOrDefault(p => p.Nome == nome);
            var txt = r.Cells[1].Value?.ToString();
            if (Num.TentarLer(txt, out var v) && (def is null || txt != def.Padrao.Texto)) _d.Parametros[nome] = v;
            else _d.Parametros.Remove(nome);
        }
    }

    private void Visualizar()
    {
        if (_secao.SelectedItem is not SecaoTipo s) return;
        _sec = AvaliadorDispositivo.AvaliarSecao(s, _d.Parametros);
        _preview.Invalidate();
    }

    private bool Aplicar()
    {
        LerParametros();
        if (_d.OrigemCota == OrigemCota.Perfil)
        {
            if (_perfil.SelectedIndex < 0) { MessageBox.Show("O alinhamento não tem perfil. Escolha \"Superfície\" em \"Cota a partir de\".", "PROJETA"); return false; }
            _d.HandlePerfil = _perfis[_perfil.SelectedIndex].Handle; _d.NomePerfil = _perfis[_perfil.SelectedIndex].Nome;
        }
        else
        {
            if (_superficie.SelectedIndex < 0) { MessageBox.Show("Não há superfícies no desenho.", "PROJETA"); return false; }
            _d.HandleSuperficie = _superficies[_superficie.SelectedIndex].Handle; _d.NomeSuperficie = _superficies[_superficie.SelectedIndex].Nome;
        }
        if (_equacao.SelectedIndex >= 0) _d.EquacaoId = _equacoes[_equacao.SelectedIndex].Id;
        if (_d.EstacaFinal <= _d.EstacaInicial) { MessageBox.Show("A estaca final deve ser maior que a inicial.", "PROJETA"); return false; }
        return true;
    }
}

/// <summary>Projeção populacional pelos métodos clássicos.</summary>
internal sealed class FormPopulacao : Form
{
    private readonly TextBox[] _anos = { new() { Text = "2000", Width = 70 }, new() { Text = "2010", Width = 70 }, new() { Text = "2022", Width = 70 } };
    private readonly TextBox[] _pops = { new() { Width = 90 }, new() { Width = 90 }, new() { Width = 90 } };
    private readonly TextBox _alvo = new() { Text = "2046", Width = 70 };
    private readonly TextBox _taxa = new() { Text = "", Width = 70 };
    private readonly DataGridView _g = UiUtil.Grade();
    public double? PopulacaoEscolhida { get; private set; }

    public FormPopulacao()
    {
        Text = "PROJETA — Projeção populacional";
        Size = new Size(820, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiUtil.FonteNormal;
        var topo = new TableLayoutPanel { Dock = DockStyle.Top, Height = 120, ColumnCount = 5, Padding = new Padding(6) };
        topo.Controls.Add(new Label { Text = "Censo", AutoSize = true }, 0, 0);
        topo.Controls.Add(new Label { Text = "Ano", AutoSize = true }, 0, 1);
        topo.Controls.Add(new Label { Text = "População", AutoSize = true }, 0, 2);
        for (int i = 0; i < 3; i++)
        {
            topo.Controls.Add(new Label { Text = $"{i + 1}º", AutoSize = true }, i + 1, 0);
            topo.Controls.Add(_anos[i], i + 1, 1);
            topo.Controls.Add(_pops[i], i + 1, 2);
        }
        var linha = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36 };
        linha.Controls.AddRange(new Control[] { new Label { Text = "Ano de projeto", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, _alvo,
            new Label { Text = "Taxa arbitrada (% a.a., opcional)", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, _taxa,
            UiUtil.Botao("Calcular", (_, _) => Calcular(), 90) });
        _g.Columns.Add("Metodo", "Método"); _g.Columns.Add("Pop", "População projetada"); _g.Columns.Add("Det", "Parâmetros");
        _g.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _g.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        var usar = new Button { Text = "Usar selecionada na rede", Width = 190, Height = 28 };
        usar.Click += (_, _) =>
        {
            if (_g.SelectedRows.Count == 0) return;
            if (Num.TentarLer(_g.SelectedRows[0].Cells[1].Value?.ToString(), out var p)) { PopulacaoEscolhida = p; DialogResult = DialogResult.OK; }
        };
        Controls.Add(_g);
        Controls.Add(linha);
        Controls.Add(topo);
        Controls.Add(UiUtil.BarraBotoes(new Button { Text = "Fechar", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 }, usar));
    }

    private void Calcular()
    {
        _g.Rows.Clear();
        double[] t = _anos.Select(x => Num.Ler(x.Text)).ToArray(), p = _pops.Select(x => Num.Ler(x.Text)).ToArray();
        if (t.Any(double.IsNaN) || p.Any(double.IsNaN)) { MessageBox.Show("Preencha os três censos.", "PROJETA"); return; }
        var lista = Core.Esgoto.ProjecaoPopulacional.Todos(t[0], p[0], t[1], p[1], t[2], p[2], Num.Ler(_alvo.Text, 2046), Num.Ler(_taxa.Text));
        foreach (var r in lista)
            _g.Rows.Add(r.Metodo.ToString(), r.Valido ? r.Populacao.ToString("0", Num.Br) : "-", r.Detalhe);
    }
}
