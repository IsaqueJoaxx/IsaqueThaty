using System.Drawing;
using System.Windows.Forms;

namespace Projeta.Civil3D.UI;

/// <summary>Janela genérica com uma ou mais abas de PropertyGrid. Edita os objetos no lugar.</summary>
internal sealed class FormPropriedades : Form
{
    public FormPropriedades(string titulo, params (string Aba, object Objeto)[] abas)
    {
        Text = titulo;
        Size = new Size(560, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiUtil.FonteNormal;
        MinimizeBox = false;

        Control conteudo;
        if (abas.Length == 1)
        {
            conteudo = new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = abas[0].Objeto, PropertySort = PropertySort.Categorized, ToolbarVisible = false };
        }
        else
        {
            var tc = new TabControl { Dock = DockStyle.Fill };
            foreach (var (aba, obj) in abas)
            {
                var tp = new TabPage(aba);
                tp.Controls.Add(new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = obj, PropertySort = PropertySort.Categorized, ToolbarVisible = false });
                tc.TabPages.Add(tp);
            }
            conteudo = tc;
        }
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 100, Height = 28 };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 };
        Controls.Add(conteudo);
        Controls.Add(UiUtil.BarraBotoes(cancel, ok));
        AcceptButton = ok; CancelButton = cancel;
    }
}

/// <summary>Edita valores numéricos de parâmetros (nome, valor, padrão, descrição).</summary>
internal sealed class FormParametros : Form
{
    private readonly DataGridView _g = UiUtil.Grade(false);
    public Dictionary<string, double> Valores { get; } = new(StringComparer.OrdinalIgnoreCase);

    public FormParametros(string titulo, IEnumerable<Projeta.Core.Modelador.ParametroDef> defs, IReadOnlyDictionary<string, double> atuais, IReadOnlyDictionary<string, double> resolvidos)
    {
        Text = titulo;
        Size = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiUtil.FonteNormal;
        _g.Columns.Add("Parametro", "Parâmetro");
        _g.Columns.Add("Valor", "Valor (vazio = padrão)");
        _g.Columns.Add("Usado", "Valor usado");
        _g.Columns.Add("Unid", "Unid.");
        _g.Columns.Add("Desc", "Descrição");
        _g.Columns[0].ReadOnly = true; _g.Columns[2].ReadOnly = true; _g.Columns[3].ReadOnly = true; _g.Columns[4].ReadOnly = true;
        _g.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _g.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        foreach (var d in defs)
        {
            string valor = atuais.TryGetValue(d.Nome, out var v) ? v.ToString("0.####", Core.Comum.Num.Br) : "";
            string usado = resolvidos.TryGetValue(d.Nome, out var u) ? u.ToString("0.####", Core.Comum.Num.Br) : "";
            int r = _g.Rows.Add(d.Nome, valor, usado, d.Unidade, (d.Origem == "rede" ? "[preenchido pela rede] " : "") + d.Descricao + $" (padrão: {d.Padrao})");
            if (d.Origem == "rede") _g.Rows[r].DefaultCellStyle.ForeColor = Color.Gray;
        }
        var ok = new Button { Text = "OK", Width = 100, Height = 28 };
        ok.Click += (_, _) =>
        {
            foreach (DataGridViewRow row in _g.Rows)
            {
                var nome = row.Cells[0].Value?.ToString() ?? "";
                var txt = row.Cells[1].Value?.ToString();
                if (Core.Comum.Num.TentarLer(txt, out var x)) Valores[nome] = x;
            }
            DialogResult = DialogResult.OK;
        };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 };
        Controls.Add(_g);
        Controls.Add(UiUtil.BarraBotoes(cancel, ok));
        CancelButton = cancel;
    }
}

/// <summary>Visualizador de uma ou mais tabelas, com exportação para CSV e inserção no desenho.</summary>
internal sealed class FormTabelas : Form
{
    private readonly TabControl _tc = new() { Dock = DockStyle.Fill };
    private readonly List<Core.Relatorios.TabelaDados> _tabelas;
    public int TabelaEscolhida => _tc.SelectedIndex;

    public FormTabelas(string titulo, IEnumerable<Core.Relatorios.TabelaDados> tabelas, IEnumerable<string>? mensagens = null, bool permitirInserir = true)
    {
        _tabelas = tabelas.ToList();
        Text = titulo;
        Size = new Size(1200, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiUtil.FonteNormal;
        foreach (var t in _tabelas)
        {
            var tp = new TabPage(t.Titulo.Length > 40 ? t.Titulo[..40] + "…" : t.Titulo);
            var g = UiUtil.Grade();
            tp.Controls.Add(g);
            _tc.TabPages.Add(tp);
            UiUtil.PreencherGrade(g, t);
        }
        Controls.Add(_tc);
        var msgs = mensagens?.ToList() ?? new();
        if (msgs.Count > 0)
        {
            var tb = new TextBox { Dock = DockStyle.Bottom, Height = 90, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Text = string.Join(Environment.NewLine, msgs) };
            Controls.Add(tb);
        }
        var botoes = new List<Control> { new Button { Text = "Fechar", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 } };
        if (permitirInserir) botoes.Add(new Button { Text = "Inserir no desenho", DialogResult = DialogResult.Yes, Width = 140, Height = 28 });
        botoes.Add(UiUtil.Botao("Exportar CSV", (_, _) =>
        {
            if (_tc.SelectedIndex < 0) return;
            var t = _tabelas[_tc.SelectedIndex];
            var arq = UiUtil.SalvarArquivo("CSV (Excel)|*.csv", Core.Comum.Texto.IdSeguro(t.Titulo, 60) + ".csv");
            if (arq is null) return;
            File.WriteAllText(arq, t.ParaCsv(), new System.Text.UTF8Encoding(true));
            UiUtil.AbrirNoSistema(arq);
        }, 120));
        Controls.Add(UiUtil.BarraBotoes(botoes.ToArray()));
    }
}
