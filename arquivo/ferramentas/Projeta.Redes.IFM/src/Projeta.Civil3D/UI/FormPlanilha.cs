using System.Drawing;
using System.Windows.Forms;
using Projeta.Core.Comum;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Redes;
using Projeta.Core.Relatorios;

namespace Projeta.Civil3D.UI;

/// <summary>
/// Planilha dinâmica de dimensionamento: mostra o cálculo trecho a trecho e permite forçar
/// diâmetro, declividade e cotas de fundo; "Recalcular" refaz o dimensionamento na hora.
/// </summary>
internal sealed class FormPlanilha : Form
{
    private readonly Rede _rede;
    private readonly Projeto _projeto;
    private readonly DataGridView _g = UiUtil.Grade(false);
    private readonly TextBox _msgs = new() { Dock = DockStyle.Bottom, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
    private readonly Label _resumo = new() { Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) };
    private const int NEdit = 4;

    public FormPlanilha(Rede rede, Projeto projeto)
    {
        _rede = rede; _projeto = projeto;
        Text = $"PROJETA — IFM — Planilha de dimensionamento: {rede.Nome} ({rede.Tipo})";
        Size = new Size(1400, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiUtil.FonteNormal;
        _g.CellEndEdit += (_, _) => _resumo.Text = "Valores alterados — clique em Recalcular.";

        var recalcular = UiUtil.Botao("Recalcular", (_, _) => Recalcular(), 110);
        var limpar = UiUtil.Botao("Limpar forçados", (_, _) =>
        {
            foreach (var t in _rede.Trechos) { t.DiametroForcado = null; t.DeclividadeForcada = null; t.CotaMontanteForcada = null; t.CotaJusanteForcada = null; }
            Recalcular(false);
        }, 120);
        var csv = UiUtil.Botao("Exportar CSV", (_, _) => Exportar(false), 110);
        var html = UiUtil.Botao("Relatório HTML", (_, _) => Exportar(true), 120);
        var aplicar = new Button { Text = "Aplicar ao desenho", DialogResult = DialogResult.OK, Width = 150, Height = 28 };
        var cancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 };
        Controls.Add(_g);
        Controls.Add(_resumo);
        Controls.Add(_msgs);
        Controls.Add(UiUtil.BarraBotoes(cancelar, aplicar, html, csv, limpar, recalcular));
        CancelButton = cancelar;
        Preencher();
    }

    private void Preencher()
    {
        var res = _rede.Resultado;
        _g.Columns.Clear(); _g.Rows.Clear();
        if (res is null) { _resumo.Text = "Rede não calculada."; return; }
        var tab = GeradorTabelas.Dimensionamento(_rede, res);
        _g.Columns.Add("Trecho", "Trecho");
        _g.Columns.Add("DForc", _rede.Tipo == TipoRede.Esgoto ? "DN forçado (mm)" : "D/b forçado (mm)");
        _g.Columns.Add("IForc", "I forçada (%)");
        _g.Columns.Add("CFMForc", "CF mont. forçada");
        _g.Columns.Add("CFJForc", "CF jus. forçada");
        foreach (var c in tab.Colunas.Skip(1)) _g.Columns.Add(c, c);
        for (int i = 0; i < _g.Columns.Count; i++)
        {
            bool editavel = i >= 1 && i <= NEdit;
            _g.Columns[i].ReadOnly = !editavel;
            if (editavel) _g.Columns[i].DefaultCellStyle.BackColor = Color.FromArgb(232, 244, 255);
            _g.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
        _g.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _g.Columns[^1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _g.Columns[0].Frozen = true;
        for (int i = 0; i < res.Trechos.Count; i++)
        {
            var rt = res.Trechos[i];
            var t = _rede.TrechoPorId(rt.TrechoId);
            var linha = new List<object>
            {
                rt.Nome,
                t?.DiametroForcado is double d ? (d * 1000).ToString("0") : "",
                t?.DeclividadeForcada is double s ? Num.F(s * 100, 3) : "",
                t?.CotaMontanteForcada is double zm ? Num.F(zm, 3) : "",
                t?.CotaJusanteForcada is double zj ? Num.F(zj, 3) : "",
            };
            linha.AddRange(tab.Linhas[i].Skip(1));
            int r = _g.Rows.Add(linha.ToArray());
            _g.Rows[r].Tag = rt.TrechoId;
            UiUtil.Colorir(_g.Rows[r], rt.Situacao);
        }
        int erros = res.Trechos.Count(t => t.Erros.Count > 0), avisos = res.Trechos.Count(t => t.Erros.Count == 0 && t.Avisos.Count > 0);
        var ext = res.Trechos.Sum(t => t.Comprimento);
        _resumo.Text = $"{res.Metodo}   |   {res.Trechos.Count} trechos, {Num.F(ext, 1)} m   |   {erros} com erro, {avisos} com aviso   |   calculado em {res.Data:dd/MM/yyyy HH:mm}";
        _msgs.Text = string.Join(Environment.NewLine, res.Mensagens.Select(m => m.ToString()));
    }

    private void Recalcular(bool lerGrade = true)
    {
        if (lerGrade)
        {
            foreach (DataGridViewRow row in _g.Rows)
            {
                var t = _rede.TrechoPorId(row.Tag as string);
                if (t is null) continue;
                double? Ler(int c) => Num.TentarLer(row.Cells[c].Value?.ToString(), out var v) ? v : null;
                var d = Ler(1); t.DiametroForcado = d is > 0 ? d / 1000.0 : null;
                var s = Ler(2); t.DeclividadeForcada = s is > 0 ? s / 100.0 : null;
                t.CotaMontanteForcada = Ler(3);
                t.CotaJusanteForcada = Ler(4);
            }
        }
        _rede.Resultado = DimensionadorGravidade.Calcular(_rede, _projeto);
        Preencher();
    }

    private void Exportar(bool html)
    {
        var res = _rede.Resultado;
        if (res is null) return;
        if (!html)
        {
            var arq = UiUtil.SalvarArquivo("CSV (Excel)|*.csv", Texto.IdSeguro(_rede.Nome, 60) + "_dimensionamento.csv");
            if (arq is null) return;
            File.WriteAllText(arq, GeradorTabelas.Dimensionamento(_rede, res).ParaCsv(), new System.Text.UTF8Encoding(true));
            UiUtil.AbrirNoSistema(arq);
        }
        else
        {
            var arq = UiUtil.SalvarArquivo("HTML|*.html", Texto.IdSeguro(_rede.Nome, 60) + "_relatorio.html");
            if (arq is null) return;
            File.WriteAllText(arq, Comandos.CmdSaidas.RelatorioCompleto(_rede, _projeto));
            UiUtil.AbrirNoSistema(arq);
        }
    }
}
