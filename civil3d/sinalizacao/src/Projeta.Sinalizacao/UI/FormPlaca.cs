using System.Drawing;
using System.Windows.Forms;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Vertical;
using Projeta.Sinalizacao.Infra;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.UI;

/// <summary>Escolha de placas do catálogo (com prévia), montagem do conjunto, suporte e situação.</summary>
internal sealed class FormPlaca : Form
{
    private readonly N _n;
    private readonly Config _cfg;
    private readonly ComboBox _familia = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
    private readonly TextBox _busca = new() { Width = 250, PlaceholderText = "Buscar código ou nome…" };
    private readonly ListBox _lista = new() { Width = 250, Height = 360, IntegralHeight = false };
    private readonly Previa _previa = new() { Width = 220, Height = 220 };
    private readonly ComboBox _dim = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 100 };
    private readonly TextBox _valor = new() { Width = 100 };
    private readonly CheckBox _obra = new() { Text = "Obra (fundo laranja)", AutoSize = true };
    private readonly ListBox _conj = new() { Width = 220, Height = 90, IntegralHeight = false };
    private readonly Previa _previaConj = new() { Width = 130, Height = 170 };
    private readonly ComboBox _suporte = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _vao = new() { Width = 60, Text = "6,00" };
    private readonly ComboBox _sit = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _chapa = new() { Width = 200 };
    private readonly TextBox _pelicula = new() { Width = 200 };
    private readonly Label _info = new() { AutoSize = false, Width = 470, Height = 34, ForeColor = Color.DimGray };
    private readonly List<PlacaItem> _itens = new();

    public DadosPlaca? Resultado { get; private set; }
    public Situacao Situacao { get; private set; }

    public FormPlaca(Config cfg, N n)
    {
        _cfg = cfg; _n = n;
        Text = "PROJETA • Inserir placa (catálogo CONTRAN)";
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 540);

        _familia.Items.AddRange(new object[] { "Todas", "Regulamentação", "Advertência", "Obras (advertência laranja)" });
        _familia.SelectedIndex = 0;
        foreach (TipoSuporte t in Enum.GetValues<TipoSuporte>()) _suporte.Items.Add(Suportes.Nome(t));
        _suporte.SelectedIndex = 0;
        foreach (Situacao s in Enum.GetValues<Situacao>()) _sit.Items.Add(ObjetoSin.NomeSituacao(s));
        _sit.SelectedIndex = (int)cfg.SituacaoPadrao;
        _chapa.Text = cfg.Chapa; _pelicula.Text = cfg.Pelicula;

        var esq = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Location = new Point(10, 10), Size = new Size(260, 470), WrapContents = false };
        esq.Controls.AddRange(new Control[] { _familia, _busca, _lista });
        Controls.Add(esq);

        var meio = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Location = new Point(280, 10), Size = new Size(230, 470), WrapContents = false };
        meio.Controls.Add(_previa);
        meio.Controls.Add(Linha("Dimensão (m):", _dim));
        meio.Controls.Add(Linha("Valor:", _valor));
        meio.Controls.Add(_obra);
        var add = new Button { Text = "Adicionar ao conjunto ↓", Width = 220, Height = 28 };
        add.Click += (_, _) => Adicionar();
        meio.Controls.Add(add);
        meio.Controls.Add(new Label { Text = "Conjunto no mesmo suporte (de cima para baixo):", AutoSize = false, Width = 220, Height = 30 });
        meio.Controls.Add(_conj);
        var bts = new FlowLayoutPanel { Width = 220, Height = 30 };
        var rem = new Button { Text = "Remover", Width = 70 }; rem.Click += (_, _) => { if (_conj.SelectedIndex >= 0) { _itens.RemoveAt(_conj.SelectedIndex); AtualizarConjunto(); } };
        var sub = new Button { Text = "Subir", Width = 60 }; sub.Click += (_, _) => { int i = _conj.SelectedIndex; if (i > 0) { (_itens[i - 1], _itens[i]) = (_itens[i], _itens[i - 1]); AtualizarConjunto(); _conj.SelectedIndex = i - 1; } };
        bts.Controls.AddRange(new Control[] { rem, sub });
        meio.Controls.Add(bts);
        Controls.Add(meio);

        var dir = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Location = new Point(520, 10), Size = new Size(230, 470), WrapContents = false };
        dir.Controls.Add(_previaConj);
        dir.Controls.Add(new Label { Text = "Suporte:", AutoSize = true });
        dir.Controls.Add(_suporte);
        dir.Controls.Add(Linha("Vão/braço (m):", _vao));
        dir.Controls.Add(new Label { Text = "Situação:", AutoSize = true });
        dir.Controls.Add(_sit);
        dir.Controls.Add(new Label { Text = "Chapa:", AutoSize = true });
        dir.Controls.Add(_chapa);
        dir.Controls.Add(new Label { Text = "Película:", AutoSize = true });
        dir.Controls.Add(_pelicula);
        Controls.Add(dir);

        _info.Location = new Point(10, 488);
        Controls.Add(_info);
        var ok = new Button { Text = "Inserir no desenho", Width = 140, Height = 30, Location = new Point(490, 500), DialogResult = DialogResult.OK };
        var canc = new Button { Text = "Cancelar", Width = 100, Height = 30, Location = new Point(640, 500), DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => Concluir();
        Controls.Add(ok); Controls.Add(canc);
        AcceptButton = ok; CancelButton = canc;

        _familia.SelectedIndexChanged += (_, _) => Filtrar();
        _busca.TextChanged += (_, _) => Filtrar();
        _lista.SelectedIndexChanged += (_, _) => Selecionou();
        _lista.DoubleClick += (_, _) => Adicionar();
        _dim.TextChanged += (_, _) => AtualizarPrevia();
        _valor.TextChanged += (_, _) => AtualizarPrevia();
        _obra.CheckedChanged += (_, _) => AtualizarPrevia();
        _suporte.SelectedIndexChanged += (_, _) => _vao.Enabled = Suportes.Suspenso((TipoSuporte)_suporte.SelectedIndex);
        _vao.Enabled = false;
        Filtrar();
    }

    private static Control Linha(string rotulo, Control c)
    {
        var p = new FlowLayoutPanel { Width = 225, Height = 30, Margin = new Padding(0) };
        p.Controls.Add(new Label { Text = rotulo, Width = 95, TextAlign = ContentAlignment.MiddleLeft, Height = 26 });
        p.Controls.Add(c);
        return p;
    }

    private TipoPlaca? Atual => _lista.SelectedItem is ItemLista il ? il.Tipo : null;

    private sealed record ItemLista(TipoPlaca Tipo) { public override string ToString() => $"{Tipo.Codigo} — {Tipo.Nome}"; }

    private void Filtrar()
    {
        string b = _busca.Text.Trim();
        var l = Catalogo.Tipos.Where(t => _familia.SelectedIndex switch
        {
            1 => t.Familia == Familia.Regulamentacao,
            2 or 3 => t.Familia == Familia.Advertencia,
            _ => true,
        }).Where(t => b.Length == 0 || t.Codigo.Contains(b, StringComparison.OrdinalIgnoreCase) || t.Nome.Contains(b, StringComparison.OrdinalIgnoreCase));
        _lista.BeginUpdate();
        _lista.Items.Clear();
        foreach (var t in l) _lista.Items.Add(new ItemLista(t));
        _lista.EndUpdate();
        if (_lista.Items.Count > 0) _lista.SelectedIndex = 0;
        _obra.Checked = _familia.SelectedIndex == 3;
    }

    private void Selecionou()
    {
        var t = Atual;
        if (t is null) return;
        _dim.Items.Clear();
        foreach (var o in GeradorPlaca.Opcoes(t, _n)) _dim.Items.Add(Num.M(o));
        _dim.Text = Num.M(GeradorPlaca.DimensaoPadrao(t, _n, _cfg.Via));
        _valor.Enabled = t.ValorPadrao is not null;
        _valor.Text = t.ValorPadrao ?? "";
        _obra.Enabled = t.Familia == Familia.Advertencia;
        AtualizarPrevia();
    }

    private PlacaItem? ItemAtual()
    {
        var t = Atual;
        if (t is null) return null;
        return new PlacaItem
        {
            Codigo = t.Codigo, Dimensao = Num.Ler(_dim.Text, GeradorPlaca.DimensaoPadrao(t, _n, _cfg.Via)),
            Valor = t.ValorPadrao is null ? null : _valor.Text.Trim(), Obra = _obra.Enabled && _obra.Checked,
        };
    }

    private void AtualizarPrevia()
    {
        var it = ItemAtual();
        if (it is null) return;
        var f = Motor.Face(it, _n, _cfg.Via);
        _previa.Desenho = f.d;
        _previa.Legenda = $"{it.Codigo}  {GeradorPlaca.TextoDimensao(f.forma, it.Dimensao)}  {Num.F(f.area, 3)} m²";
        _previa.Invalidate();
        _info.Text = Atual!.Rotulo;
    }

    private void Adicionar()
    {
        var it = ItemAtual();
        if (it is null) return;
        _itens.Add(it);
        AtualizarConjunto();
    }

    private void AtualizarConjunto()
    {
        _conj.Items.Clear();
        foreach (var i in _itens) _conj.Items.Add($"{i.Codigo}{(string.IsNullOrEmpty(i.Valor) ? "" : $" ({i.Valor})")}  {Num.M(i.Dimensao)}{(i.Obra ? "  obra" : "")}");
        var o = new ObjetoSin { Categoria = Categoria.Placa, Placa = new DadosPlaca { Placas = _itens.ToList() } };
        _previaConj.Desenho = _itens.Count == 0 ? null : Motor.Conjunto(o, _n, _cfg.Via).d;
    }

    private void Concluir()
    {
        if (_itens.Count == 0) { var it = ItemAtual(); if (it is not null) _itens.Add(it); }
        if (_itens.Count == 0) { DialogResult = DialogResult.None; return; }
        Resultado = new DadosPlaca
        {
            Placas = _itens.ToList(), Suporte = (TipoSuporte)_suporte.SelectedIndex, Vao = Num.Ler(_vao.Text, 0),
            Chapa = _chapa.Text.Trim(), Pelicula = _pelicula.Text.Trim(), MaterialSuporte = _cfg.MaterialSuporte,
        };
        Situacao = (Situacao)_sit.SelectedIndex;
    }
}
