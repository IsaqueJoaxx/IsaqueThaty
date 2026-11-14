using System.Drawing;
using System.Windows.Forms;
using Projeta.Core.Comum;
using Projeta.Core.Hidrologia;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.UI;

/// <summary>Gerenciador de equações de chuva (IDF): catálogo padrão + equações do projeto.</summary>
internal sealed class FormEquacoes : Form
{
    private readonly Projeto _projeto;
    private readonly ListBox _lista = new() { Dock = DockStyle.Left, Width = 300 };
    private readonly PropertyGrid _pg = new() { Dock = DockStyle.Fill, ToolbarVisible = false, PropertySort = PropertySort.Categorized };
    private readonly TextBox _T = new() { Width = 60, Text = "10" };
    private readonly TextBox _t = new() { Width = 60, Text = "10" };
    private readonly Label _res = new() { AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
    private readonly HashSet<string> _padrao;
    public string? EquacaoEscolhida { get; private set; }

    public FormEquacoes(Projeto projeto, string? selecionar = null)
    {
        _projeto = projeto;
        _padrao = CatalogoEquacoes.Padrao().Select(e => e.Id).ToHashSet();
        Text = "PROJETA — IFM — Equações de chuvas intensas (IDF)";
        Size = new Size(980, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiUtil.FonteNormal;

        var teste = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(4) };
        teste.Controls.AddRange(new Control[] { new Label { Text = "Teste: T (anos)", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, _T, new Label { Text = "t (min)", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, _t,
            UiUtil.Botao("Calcular i", (_, _) => Testar(), 90), _res });
        var direita = new Panel { Dock = DockStyle.Fill };
        direita.Controls.Add(_pg);
        direita.Controls.Add(teste);

        _lista.SelectedIndexChanged += (_, _) => Mostrar();
        _pg.PropertyValueChanged += (_, _) => { Testar(); RecarregarLista(((EquacaoChuva?)_pg.SelectedObject)?.Id); };

        var usar = new Button { Text = "Usar na rede", DialogResult = DialogResult.OK, Width = 120, Height = 28 };
        usar.Click += (_, _) => EquacaoEscolhida = (_lista.SelectedItem as EquacaoChuva)?.Id;
        Controls.Add(direita);
        Controls.Add(_lista);
        Controls.Add(UiUtil.BarraBotoes(
            new Button { Text = "Fechar", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 }, usar,
            UiUtil.Botao("Excluir", (_, _) => Excluir(), 90),
            UiUtil.Botao("Duplicar", (_, _) => Duplicar(), 90),
            UiUtil.Botao("Nova", (_, _) => Nova(), 90)));
        RecarregarLista(selecionar);
    }

    private void RecarregarLista(string? selecionar)
    {
        _lista.BeginUpdate();
        _lista.Items.Clear();
        foreach (var e in _projeto.TodasEquacoes().OrderBy(e => e.Nome)) _lista.Items.Add(e);
        _lista.EndUpdate();
        for (int i = 0; i < _lista.Items.Count; i++)
            if (((EquacaoChuva)_lista.Items[i]!).Id == selecionar) { _lista.SelectedIndex = i; return; }
        if (_lista.Items.Count > 0 && _lista.SelectedIndex < 0) _lista.SelectedIndex = 0;
    }

    private void Mostrar()
    {
        if (_lista.SelectedItem is not EquacaoChuva e) return;
        // equações do catálogo são exibidas como cópia (alterações só via "Duplicar")
        bool doProjeto = _projeto.Equacoes.Any(x => x.Id == e.Id);
        _pg.SelectedObject = doProjeto ? _projeto.Equacoes.First(x => x.Id == e.Id) : JsonProjeta.Clonar(e);
        _pg.Enabled = true;
        _pg.HelpVisible = true;
        Text = $"PROJETA — IFM — Equações IDF — {(doProjeto ? "equação do projeto (editável)" : "catálogo padrão (use Duplicar para alterar)")}";
        Testar();
    }

    private void Testar()
    {
        if (_pg.SelectedObject is not EquacaoChuva e) return;
        try
        {
            double T = Num.Ler(_T.Text, 10), t = Num.Ler(_t.Text, 10);
            _res.Text = $"i = {Num.F(e.Intensidade(T, t), 2)} mm/h   ({e.Descricao()})";
        }
        catch (Exception ex) { _res.Text = "Erro: " + ex.Message; }
    }

    private void Nova()
    {
        var e = new EquacaoChuva { Local = "Nova equação", K = 1000, A = 0.15, B = 10, C = 0.75, Fonte = "informe a referência" };
        _projeto.Equacoes.Add(e);
        RecarregarLista(e.Id);
    }

    private void Duplicar()
    {
        if (_lista.SelectedItem is not EquacaoChuva e) return;
        var c = JsonProjeta.Clonar(e);
        c.Id = Guid.NewGuid().ToString("N")[..8];
        c.Local += " (cópia)";
        _projeto.Equacoes.Add(c);
        RecarregarLista(c.Id);
    }

    private void Excluir()
    {
        if (_lista.SelectedItem is not EquacaoChuva e) return;
        if (_padrao.Contains(e.Id) && !_projeto.Equacoes.Any(x => x.Id == e.Id)) { MessageBox.Show("Equações do catálogo padrão não podem ser excluídas.", "PROJETA — IFM"); return; }
        _projeto.Equacoes.RemoveAll(x => x.Id == e.Id);
        RecarregarLista(null);
    }
}
