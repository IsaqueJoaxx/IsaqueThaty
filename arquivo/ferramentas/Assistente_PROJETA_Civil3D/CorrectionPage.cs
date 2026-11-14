using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Projeta.Civil3D;

internal sealed class CorrectionPage : ToolPage
{
    private readonly TextBox input = new() { Multiline=true, ScrollBars=ScrollBars.Both, WordWrap=false, Dock=DockStyle.Fill, Height=140, MaxLength=5_000_000 };
    private readonly NumericUpDown thickness=Ui.Number(23, .1m, 200, 2, .5m);
    private readonly ThemedListView list;
    private readonly Label summary;
    private readonly Button export;
    private List<CorrectionResult>? calculated;
    private decimal calculatedThickness;
    public CorrectionPage() : base("CORREÇÃO DO GREIDE — IFM", "Greide = base do concreto. Topo = base corrigida + espessura vertical (23 cm por padrão). Controle manual de correções: variação máxima de 2,5 mm entre seções a cada 5 m.")
    {
        var load=Ui.Button("Importar cotas CSV"); load.Click+=(_,_)=>LoadCsv();
        var example=Ui.Button("Carregar exemplo"); example.Click+=(_,_)=>input.Text=CorrectionCore.Example;
        AddWide(Ui.Row(load,example));
        AddWide(Wrap("Cole uma série por eixo, com distâncias crescentes. Cotas e larguras em metros; correção em milímetros; caimentos em % (-2 nas tangentes). Informe o caimento de cada bordo em cada seção, inclusive nas curvas.","muted"));
        AddWide(input,minHeight:140); AddWide(Wrap(CorrectionCore.Header,"muted"));
        AddRow("Concreto (cm)",thickness);
        var calc=Ui.Button("Conferir cotas e correções",primary:true); calc.Click+=(_,_)=>Calculate(); AddWide(calc);
        summary=Wrap("Nenhuma seção calculada.","muted"); AddWide(summary);
        list=new ThemedListView(new[]{"Dist. (m)","Corr. (mm)","Topo eixo","Saldo eixo","Saldo esq.","Saldo dir.","Controle"},new[]{12,12,15,13,13,13,22});
        AddWide(list,fill:true,minHeight:170);
        AddWide(Wrap("Saldo = base corrigida − pista existente, em metros: positivo indica espaço abaixo da base; negativo indica interferência com a pista existente. Não é aprovação de camada ou indicação automática de tratamento. A mesma correção vertical é aplicada ao eixo e aos bordos.","muted"));
        AddWide(Wrap("Somente pares espaçados exatamente 5 m são conferidos. Outros intervalos são sinalizados sem interpolação. A regra é informada pelo usuário e não calcula nem garante o IRI. Os resultados completos dos bordos são exportados para Excel.","muted"));
        export=Ui.Button("Exportar cotas para Excel (CSV)"); export.Enabled=false; export.Click+=(_,_)=>Export(); AddWide(export);
        input.TextChanged+=(_,_)=>Clear(); thickness.ValueChanged+=(_,_)=>Clear();
    }
    private void Clear() { calculated=null; export.Enabled=false; list.Items.Clear(); summary.Text="Dados alterados. Refaça a conferência."; }
    private void LoadCsv()
    {
        using var d=new OpenFileDialog{Filter="CSV UTF-8|*.csv;*.txt",Title="CORREÇÃO DO GREIDE — IFM"};
        if(d.ShowDialog(this)!=DialogResult.OK)return;
        try { if(new FileInfo(d.FileName).Length>10_000_000)throw new ArgumentException("Limite: 10 MB."); var text=File.ReadAllText(d.FileName,new UTF8Encoding(false,true)); CorrectionCore.Parse(text); input.Text=text; ShowOutcome("Cotas externas importadas. Confira os dados antes de calcular.",true); }
        catch(System.Exception ex){ShowOutcome("Importação não concluída: "+ex.Message,false);}
    }
    private void Calculate()
    {
        Clear();
        try
        {
            var rows=CorrectionCore.Calculate(CorrectionCore.Parse(input.Text),thickness.Value/100);
            static string N(decimal n)=>n.ToString("0.0000",Fmt.Br);
            list.BeginUpdate();
            try { foreach(var r in rows)list.Items.Add(new ListViewItem(new[]{N(r.Input.Station),r.Input.CorrectionMm.ToString("0.###",Fmt.Br),N(r.TopAxis),N(r.GapAxis),N(r.GapLeft),N(r.GapRight),r.Check})
                {ToolTipText=$"{r.Check}\nBase eixo: {N(r.BaseAxis)}; topo esq.: {N(r.TopLeft)}; topo dir.: {N(r.TopRight)}\nΔ correção: {r.DeltaMm?.ToString("0.###",Fmt.Br) ?? "—"} mm"}); }
            finally {list.EndUpdate();}
            calculated=rows; calculatedThickness=thickness.Value/100; export.Enabled=true;
            summary.Text=$"{rows.Count} seções · {rows.Count(r=>r.Check.StartsWith("Excede"))} excedências · {rows.Count(r=>r.Check.StartsWith("Intervalo"))} intervalos não verificados · {rows.Count(r=>r.GapAxis<0 || r.GapLeft<0 || r.GapRight<0)} seções com interferência na base.";
            ShowOutcome("Conferência concluída. Nenhuma cota do DWG foi alterada.",true);
        }
        catch(System.Exception ex){ShowOutcome("Conferência não concluída: "+ex.Message,false);}
    }
    private void Export()
    {
        if(calculated is null)return;
        using var d=new SaveFileDialog{Filter="CSV|*.csv",FileName="CORRECAO_GREIDE_IFM.csv",OverwritePrompt=true};
        if(d.ShowDialog(this)!=DialogResult.OK)return;
        try {File.WriteAllText(d.FileName,CorrectionCore.Export(calculated,calculatedThickness),new UTF8Encoding(true)); ShowOutcome("Cotas exportadas: "+d.FileName,true);}
        catch(System.Exception ex){ShowOutcome("Exportação não concluída: "+ex.Message,false);}
    }
    protected override void OnSnapshot(DrawingSnapshot? snapshot) { }
}
