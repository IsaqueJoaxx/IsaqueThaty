using System.Drawing;
using System.Windows.Forms;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Vertical;
using Projeta.Sinalizacao.Infra;
using Alin = Projeta.Sinalizacao.Core.Desenho.Alinhamento;
using Cor = Projeta.Sinalizacao.Core.Desenho.Cor;

namespace Projeta.Sinalizacao.UI;

/// <summary>Modelador de placas retangulares (indicação, educativas, serviços, obras): o tamanho sai do texto.</summary>
internal sealed class FormModelador : Form
{
    private static readonly (string Nome, Cor Cor)[] Fundos =
    {
        ("Verde (indicação/orientação)", Cor.Verde), ("Azul (serviços auxiliares)", Cor.Azul), ("Marrom (atrativos turísticos)", Cor.Marrom),
        ("Branco (educativas/regulamentação)", Cor.Branco), ("Amarelo (advertência)", Cor.Amarelo), ("Laranja (obras)", Cor.Laranja), ("Preto", Cor.Preto),
    };

    private readonly TextBox _codigo = new() { Width = 120 };
    private readonly TextBox _nome = new() { Width = 220 };
    private readonly ComboBox _fundo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly ComboBox _familia = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly TextBox _altura = new() { Width = 70 };
    private readonly ComboBox _alin = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly CheckBox _orla = new() { Text = "Orla", Checked = true, AutoSize = true };
    private readonly DataGridView _grade = new() { Width = 420, Height = 210, AllowUserToAddRows = true, RowHeadersVisible = false };
    private readonly Previa _previa = new() { Width = 360, Height = 300 };
    private readonly Label _medidas = new() { AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

    public ModeloPlaca? Resultado { get; private set; }
    public static string PastaBiblioteca => Path.Combine(Config.PastaUsuario, "placas");

    public FormModelador(Core.Normas.Normas n, double velocidade)
    {
        Text = "PROJETA • Modelador de placas";
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 480);

        foreach (var f in Fundos) _fundo.Items.Add(f.Nome);
        foreach (var f in new[] { "Indicação", "Educativa", "Serviços auxiliares", "Obras" }) _familia.Items.Add(f);
        _alin.Items.AddRange(new object[] { "Esquerda", "Centro", "Direita" });
        _grade.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Texto", Width = 230 });
        var cs = new DataGridViewComboBoxColumn { HeaderText = "Seta", Width = 110 };
        cs.Items.AddRange(Enum.GetNames<SetaIndicacao>());
        _grade.Columns.Add(cs);
        _grade.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Escala", Width = 60 });

        var m = ModeloPlaca.Exemplo();
        m.AlturaLetra = Core.Normas.Normas.PorVelocidade(n.AlturaLetraIndicacao, velocidade);
        Carregar(m);

        var esq = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Location = new Point(10, 10), Size = new Size(440, 420), WrapContents = false };
        esq.Controls.Add(Linha("Código:", _codigo));
        esq.Controls.Add(Linha("Nome:", _nome));
        esq.Controls.Add(Linha("Fundo:", _fundo));
        esq.Controls.Add(Linha("Família:", _familia));
        var lh = Linha("Altura da letra (m):", _altura);
        lh.Controls.Add(_orla);
        esq.Controls.Add(lh);
        esq.Controls.Add(Linha("Alinhamento:", _alin));
        esq.Controls.Add(_grade);
        Controls.Add(esq);

        _previa.Location = new Point(450, 10);
        Controls.Add(_previa);
        _medidas.Location = new Point(450, 320);
        Controls.Add(_medidas);
        Controls.Add(new Label { Text = "Altura de letra padrão pela velocidade (normas.json → AlturaLetraIndicacao). Medidas arredondadas para 0,05 m.", Location = new Point(450, 350), Size = new Size(360, 40), ForeColor = Color.DimGray });

        var salvar = new Button { Text = "Salvar na biblioteca", Width = 140, Location = new Point(10, 440) };
        var abrir = new Button { Text = "Abrir da biblioteca", Width = 140, Location = new Point(160, 440) };
        var ok = new Button { Text = "Inserir no desenho", Width = 140, Height = 30, Location = new Point(560, 438), DialogResult = DialogResult.OK };
        var canc = new Button { Text = "Cancelar", Width = 100, Height = 30, Location = new Point(710, 438), DialogResult = DialogResult.Cancel };
        salvar.Click += (_, _) => Salvar();
        abrir.Click += (_, _) => Abrir();
        ok.Click += (_, _) => { Resultado = Ler(); };
        Controls.AddRange(new Control[] { salvar, abrir, ok, canc });
        AcceptButton = ok; CancelButton = canc;

        foreach (var c in new Control[] { _codigo, _nome, _altura }) c.TextChanged += (_, _) => Atualizar();
        foreach (var c in new[] { _fundo, _familia, _alin }) c.SelectedIndexChanged += (_, _) => Atualizar();
        _orla.CheckedChanged += (_, _) => Atualizar();
        _grade.CellValueChanged += (_, _) => Atualizar();
        _grade.RowsRemoved += (_, _) => Atualizar();
        _grade.CurrentCellDirtyStateChanged += (_, _) => { if (_grade.IsCurrentCellDirty) _grade.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        Atualizar();
    }

    private static FlowLayoutPanel Linha(string rotulo, Control c)
    {
        var p = new FlowLayoutPanel { Width = 430, Height = 30, Margin = new Padding(0) };
        p.Controls.Add(new Label { Text = rotulo, Width = 130, TextAlign = ContentAlignment.MiddleLeft, Height = 26 });
        p.Controls.Add(c);
        return p;
    }

    private void Carregar(ModeloPlaca m)
    {
        _codigo.Text = m.Codigo; _nome.Text = m.Nome;
        int fi = Array.FindIndex(Fundos, f => f.Cor == m.Fundo); _fundo.SelectedIndex = fi < 0 ? 0 : fi;
        _familia.SelectedIndex = m.Familia switch { Familia.Educativa => 1, Familia.ServicosAuxiliares => 2, Familia.Obras => 3, _ => 0 };
        _altura.Text = m.AlturaLetra.ToString("0.000", Num.Br);
        _alin.SelectedIndex = (int)m.Alinhamento;
        _orla.Checked = m.Orla;
        _grade.Rows.Clear();
        foreach (var l in m.Linhas) _grade.Rows.Add(l.Texto, l.Seta.ToString(), Num.F0(l.Escala));
    }

    private ModeloPlaca Ler()
    {
        var m = new ModeloPlaca
        {
            Codigo = string.IsNullOrWhiteSpace(_codigo.Text) ? "I-01" : _codigo.Text.Trim(), Nome = _nome.Text.Trim(),
            Fundo = Fundos[Math.Max(0, _fundo.SelectedIndex)].Cor,
            Familia = _familia.SelectedIndex switch { 1 => Familia.Educativa, 2 => Familia.ServicosAuxiliares, 3 => Familia.Obras, _ => Familia.Indicacao },
            AlturaLetra = Math.Max(0.03, Num.Ler(_altura.Text, 0.15)), Alinhamento = (Alin)Math.Max(0, _alin.SelectedIndex), Orla = _orla.Checked,
        };
        foreach (DataGridViewRow r in _grade.Rows)
        {
            if (r.IsNewRow) continue;
            string t = r.Cells[0].Value?.ToString() ?? "";
            var seta = Enum.TryParse<SetaIndicacao>(r.Cells[1].Value?.ToString(), out var sv) ? sv : SetaIndicacao.Nenhuma;
            double esc = Num.Ler(r.Cells[2].Value?.ToString(), 1);
            if (t.Length > 0 || seta != SetaIndicacao.Nenhuma) m.Linhas.Add(new LinhaIndicacao(t, seta, esc <= 0 ? 1 : esc));
        }
        return m;
    }

    private void Atualizar()
    {
        try
        {
            var lay = GeradorIndicacao.Montar(Ler());
            _previa.Desenho = lay.Desenho;
            _medidas.Text = $"{Num.M(lay.Largura)} × {Num.M(lay.Altura)} m  =  {Num.F(lay.Area, 3)} m²";
        }
        catch { }
    }

    private void Salvar()
    {
        Directory.CreateDirectory(PastaBiblioteca);
        var m = Ler();
        string nome = string.Concat((m.Codigo + " " + m.Nome).Split(Path.GetInvalidFileNameChars())).Trim();
        string arq = Path.Combine(PastaBiblioteca, nome + ".json");
        File.WriteAllText(arq, m.ParaJson());
        MessageBox.Show(this, $"Salvo em {arq}", "Modelador", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Abrir()
    {
        Directory.CreateDirectory(PastaBiblioteca);
        using var d = new OpenFileDialog { InitialDirectory = PastaBiblioteca, Filter = "Modelo de placa (*.json)|*.json" };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var m = ModeloPlaca.DeJson(File.ReadAllText(d.FileName));
        if (m is null) { MessageBox.Show(this, "Arquivo inválido.", "Modelador"); return; }
        Carregar(m);
        Atualizar();
    }
}
