using System.Drawing;
using System.Windows.Forms;
using Projeta.Core.Comum;
using Projeta.Core.Modelador;

namespace Projeta.Civil3D.UI;

/// <summary>
/// Modelador de dispositivos e seções-tipo: edita a definição (JSON com expressões),
/// testa parâmetros, mostra planta/seção e quantitativos, e salva na biblioteca do usuário.
/// </summary>
internal sealed class FormModelador : Form
{
    private readonly Biblioteca _bib;
    private readonly TabControl _abas = new() { Dock = DockStyle.Left, Width = 280 };
    private readonly ListBox _listaDisp = new() { Dock = DockStyle.Fill };
    private readonly ListBox _listaSec = new() { Dock = DockStyle.Fill };
    private readonly TextBox _json = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = UiUtil.FonteMono, AcceptsTab = true };
    private readonly DataGridView _params = UiUtil.Grade(false);
    private readonly PictureBox _preview = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 36, 44) };
    private readonly TextBox _saida = new() { Dock = DockStyle.Bottom, Height = 130, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = UiUtil.FonteMono };
    private ModeloAvaliado? _modelo;
    private AvaliadorDispositivo.SecaoAvaliada? _secao;

    public bool EhSecao => _abas.SelectedIndex == 1;
    /// <summary>Definição a inserir no desenho (quando o usuário clica "Inserir no desenho").</summary>
    public DefinicaoDispositivo? ParaInserir { get; private set; }
    public Dictionary<string, double> ValoresTeste { get; } = new(StringComparer.OrdinalIgnoreCase);

    public FormModelador(Biblioteca bib, bool abrirSecoes = false)
    {
        _bib = bib;
        Text = "PROJETA — Modelador de dispositivos e seções";
        Size = new Size(1400, 820);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiUtil.FonteNormal;

        var tpD = new TabPage("Dispositivos"); tpD.Controls.Add(_listaDisp);
        var tpS = new TabPage("Seções-tipo"); tpS.Controls.Add(_listaSec);
        _abas.TabPages.Add(tpD); _abas.TabPages.Add(tpS);
        _listaDisp.SelectedIndexChanged += (_, _) => { if (_listaDisp.SelectedItem is DefinicaoDispositivo d) Carregar(JsonProjeta.Serializar(d)); };
        _listaSec.SelectedIndexChanged += (_, _) => { if (_listaSec.SelectedItem is SecaoTipo s) Carregar(JsonProjeta.Serializar(s)); };
        _abas.SelectedIndexChanged += (_, _) => { var l = EhSecao ? _listaSec : _listaDisp; if (l.Items.Count > 0) { l.SelectedIndex = -1; l.SelectedIndex = 0; } };

        _params.Columns.Add("Nome", "Parâmetro");
        _params.Columns.Add("Valor", "Valor de teste");
        _params.Columns.Add("Usado", "Usado");
        _params.Columns[0].ReadOnly = true; _params.Columns[2].ReadOnly = true;
        _params.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _params.CellEndEdit += (_, _) => Avaliar(false);

        _preview.Paint += (_, e) =>
        {
            if (EhSecao && _secao is not null) UiUtil.DesenharSecao(e.Graphics, _preview.ClientRectangle, _secao);
            else if (!EhSecao && _modelo is not null) UiUtil.DesenharPlanta(e.Graphics, _preview.ClientRectangle, _modelo.Planta);
        };
        _preview.Resize += (_, _) => _preview.Invalidate();

        var divCentral = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 560 };
        divCentral.Panel1.Controls.Add(_json);
        divCentral.Panel1.Controls.Add(new Label { Text = "Definição (JSON). Campos numéricos aceitam expressões: \"DiametroInterno/2 + 0.1\", se(), min(), max(), sqrt(), interp()...", Dock = DockStyle.Top, Height = 34 });
        var divDireita = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 330 };
        divDireita.Panel1.Controls.Add(_params);
        divDireita.Panel2.Controls.Add(_preview);
        divCentral.Panel2.Controls.Add(divDireita);

        Controls.Add(divCentral);
        Controls.Add(_saida);
        Controls.Add(_abas);
        Controls.Add(UiUtil.BarraBotoes(
            new Button { Text = "Fechar", DialogResult = DialogResult.Cancel, Width = 90, Height = 28 },
            UiUtil.Botao("Inserir no desenho", (_, _) => Inserir(), 140),
            UiUtil.Botao("Salvar na biblioteca", (_, _) => Salvar(), 150),
            UiUtil.Botao("Avaliar / visualizar", (_, _) => Avaliar(true), 140),
            UiUtil.Botao("Duplicar", (_, _) => Duplicar(), 90),
            UiUtil.Botao("Novo", (_, _) => Novo(), 80),
            UiUtil.Botao("Abrir pasta", (_, _) => { Directory.CreateDirectory(_bib.PastaUsuario ?? Biblioteca.PastaPadrao); UiUtil.AbrirNoSistema(_bib.PastaUsuario ?? Biblioteca.PastaPadrao); }, 100)));

        RecarregarListas();
        if (abrirSecoes) _abas.SelectedIndex = 1;
        else if (_listaDisp.Items.Count > 0) _listaDisp.SelectedIndex = 0;
    }

    private void RecarregarListas(string? selecionar = null)
    {
        _listaDisp.Items.Clear(); _listaSec.Items.Clear();
        foreach (var d in _bib.Dispositivos.Values.OrderBy(x => x.Id)) _listaDisp.Items.Add(d);
        foreach (var s in _bib.Secoes.Values.OrderBy(x => x.Id)) _listaSec.Items.Add(s);
        if (selecionar is null) return;
        var l = EhSecao ? _listaSec : _listaDisp;
        for (int i = 0; i < l.Items.Count; i++)
            if ((l.Items[i] is DefinicaoDispositivo d && d.Id == selecionar) || (l.Items[i] is SecaoTipo s && s.Id == selecionar)) { l.SelectedIndex = i; break; }
    }

    private void Carregar(string json)
    {
        _json.Text = json.Replace("\n", Environment.NewLine).Replace("\r\r", "\r");
        ValoresTeste.Clear();
        Avaliar(true);
    }

    private void Avaliar(bool recriarGrade)
    {
        _saida.Clear();
        try
        {
            if (!recriarGrade)
            {
                ValoresTeste.Clear();
                foreach (DataGridViewRow r in _params.Rows)
                    if (Num.TentarLer(r.Cells[1].Value?.ToString(), out var v)) ValoresTeste[r.Cells[0].Value?.ToString() ?? ""] = v;
            }
            List<ParametroDef> defs;
            IReadOnlyDictionary<string, double> usados;
            var linhas = new List<string>();
            if (EhSecao)
            {
                var s = JsonProjeta.Desserializar<SecaoTipo>(_json.Text) ?? throw new Exception("JSON vazio");
                _secao = AvaliadorDispositivo.AvaliarSecao(s, ValoresTeste);
                _modelo = null;
                defs = s.Parametros; usados = _secao.Valores;
                linhas.AddRange(_secao.Mensagens.Select(m => m.ToString()));
                linhas.Add($"Área de concreto: {Num.F(_secao.AreaConcreto, 4)} m²/m;  escavação: {Num.F(_secao.AreaEscavacao, 4)} m²/m;  perímetro revestido: {Num.F(_secao.PerimetroRevestido, 3)} m");
                var hid = _secao.Hidraulica;
                if (hid is not null)
                {
                    double y = hid.Altura;
                    linhas.Add($"Seção hidráulica plena: área {Num.F(hid.Area(y), 4)} m², perímetro molhado {Num.F(hid.PerimetroMolhado(y), 3)} m, " +
                               $"Q (n=0,015; I=1%) = {Num.F(Projeta.Core.Hidraulica.Manning.Vazao(hid, y, 0.015, 0.01) * 1000, 1)} L/s");
                }
            }
            else
            {
                var d = JsonProjeta.Desserializar<DefinicaoDispositivo>(_json.Text) ?? throw new Exception("JSON vazio");
                _modelo = AvaliadorDispositivo.Avaliar(d, ValoresTeste);
                _secao = null;
                defs = d.Parametros; usados = _modelo.Valores;
                linhas.AddRange(_modelo.Mensagens.Select(m => m.ToString()));
                linhas.Add($"Sólidos gerados: {string.Join(", ", _modelo.Solidos.Keys)}   |   saída: {_modelo.Saida.Count}   |   volume estimado: {Num.F(_modelo.Saida.Sum(x => x.VolumeEstimado()), 3)} m³");
                foreach (var q in _modelo.Quantitativos) linhas.Add($"  {q.Nome}: {Num.F(q.Valor, 3)} {q.Unidade}");
            }
            if (recriarGrade)
            {
                _params.Rows.Clear();
                foreach (var p in defs)
                    _params.Rows.Add(p.Nome, ValoresTeste.TryGetValue(p.Nome, out var v) ? v.ToString("0.####", Num.Br) : "",
                        usados.TryGetValue(p.Nome, out var u) ? u.ToString("0.####", Num.Br) : "");
            }
            else
            {
                foreach (DataGridViewRow r in _params.Rows)
                    r.Cells[2].Value = usados.TryGetValue(r.Cells[0].Value?.ToString() ?? "", out var u) ? u.ToString("0.####", Num.Br) : "";
            }
            _saida.Text = string.Join(Environment.NewLine, linhas);
        }
        catch (Exception ex)
        {
            _saida.Text = "Erro: " + ex.Message;
            _modelo = null; _secao = null;
        }
        _preview.Invalidate();
    }

    private void Novo()
    {
        if (EhSecao)
        {
            var s = new SecaoTipo
            {
                Id = "MINHA_SECAO", Nome = "Nova seção", Categoria = "Valeta",
                Parametros = { new ParametroDef { Nome = "b", Padrao = 0.5, Descricao = "Largura" }, new ParametroDef { Nome = "h", Padrao = 0.3 }, new ParametroDef { Nome = "e", Padrao = 0.08 } },
                ContornoHidraulico = { P("-b/2", "h"), P("-b/2", "0"), P("b/2", "0"), P("b/2", "h") },
                Concreto = { P("-b/2", "h"), P("-b/2", "0"), P("b/2", "0"), P("b/2", "h"), P("b/2+e", "h"), P("b/2+e", "-e"), P("-b/2-e", "-e"), P("-b/2-e", "h") },
            };
            Carregar(JsonProjeta.Serializar(s));
        }
        else
        {
            var d = new DefinicaoDispositivo
            {
                Id = "MEU_DISPOSITIVO", Nome = "Novo dispositivo", Categoria = "Caixa", AlinharComTubo = true, RaioFuros = "L/2",
                Parametros =
                {
                    new ParametroDef { Nome = "Profundidade", Padrao = 1.2, Origem = "rede" },
                    new ParametroDef { Nome = "DiametroMaximo", Padrao = 0.4, Origem = "rede" },
                    new ParametroDef { Nome = "L", Padrao = "max(0.8, DiametroMaximo + 0.4)", Descricao = "Lado interno" },
                    new ParametroDef { Nome = "e", Padrao = 0.15, Descricao = "Espessura" },
                },
                Solidos =
                {
                    new OperacaoSolida { Id = "ext", Op = "caixa", Comprimento = "L+2*e", Largura = "L+2*e", Altura = "Profundidade+e", Base = new PontoExpr { Z = "-e" } },
                    new OperacaoSolida { Id = "int", Op = "caixa", Comprimento = "L", Largura = "L", Altura = "Profundidade" },
                    new OperacaoSolida { Id = "caixa", Op = "subtrair", De = "ext", Com = new() { "int" } },
                },
                Saida = { "caixa" },
                Planta = { new ElementoPlanta { Tipo = "retangulo", Largura = "L+2*e", Altura = "L+2*e" } },
                Quantitativos = { new QuantitativoDef { Nome = "Concreto", Unidade = "m³", Expr = "volume('caixa')" } },
            };
            Carregar(JsonProjeta.Serializar(d));
        }
    }

    private static PontoExpr P(string x, string y) => new() { X = x, Y = y };

    private void Duplicar()
    {
        try
        {
            if (EhSecao) { var s = JsonProjeta.Desserializar<SecaoTipo>(_json.Text)!; s.Id += "_COPIA"; s.Nome += " (cópia)"; Carregar(JsonProjeta.Serializar(s)); }
            else { var d = JsonProjeta.Desserializar<DefinicaoDispositivo>(_json.Text)!; d.Id += "_COPIA"; d.Nome += " (cópia)"; Carregar(JsonProjeta.Serializar(d)); }
        }
        catch (Exception ex) { _saida.Text = "Erro: " + ex.Message; }
    }

    private void Salvar()
    {
        try
        {
            string arq, id;
            if (EhSecao)
            {
                var s = JsonProjeta.Desserializar<SecaoTipo>(_json.Text)!;
                if (string.IsNullOrWhiteSpace(s.Id)) throw new Exception("Informe o campo id");
                var a = AvaliadorDispositivo.AvaliarSecao(s);
                if (!a.Ok) throw new Exception("Corrija os erros antes de salvar: " + string.Join("; ", a.Mensagens));
                arq = _bib.SalvarSecao(s); id = s.Id;
            }
            else
            {
                var d = JsonProjeta.Desserializar<DefinicaoDispositivo>(_json.Text)!;
                if (string.IsNullOrWhiteSpace(d.Id)) throw new Exception("Informe o campo id");
                var m = AvaliadorDispositivo.Avaliar(d);
                if (!m.Ok) throw new Exception("Corrija os erros antes de salvar: " + string.Join("; ", m.Mensagens));
                arq = _bib.SalvarDispositivo(d); id = d.Id;
            }
            RecarregarListas(id);
            _saida.Text = "Salvo em " + arq + Environment.NewLine + _saida.Text;
        }
        catch (Exception ex) { _saida.Text = "Erro ao salvar: " + ex.Message; }
    }

    private void Inserir()
    {
        if (EhSecao) { _saida.Text = "Seções-tipo são usadas pelo comando PRJLONGITUDINAL."; return; }
        try
        {
            Avaliar(false);
            ParaInserir = JsonProjeta.Desserializar<DefinicaoDispositivo>(_json.Text);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { _saida.Text = "Erro: " + ex.Message; }
    }
}
