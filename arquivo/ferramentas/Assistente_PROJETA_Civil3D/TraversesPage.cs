using System.Text;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Net;

namespace Projeta.Civil3D;

internal sealed class TraversesPage : ToolPage
{
    private readonly TextBox ida = Field(), volta = Field(), apoio = Field(true), percurso = Field(true), retorno = Field(true), aliases = Field(true), system = Field();
    private readonly NumericUpDown factor = Ui.Number(1m,0.000001m,10m,8,0.000001m);
    private readonly NumericUpDown angular = Ui.Number(0m,0m,3600m,3,1m), precision = Ui.Number(0m,0m,10000000m,0,1000m);
    private readonly TextBox summary = Field(true);
    private readonly Button export = Ui.Button("Salvar relatório e memória…");
    private TraverseResult? resultIda, resultVolta;
    private SurveyBook? bookIda, bookVolta;
    private string? report;
    private sealed record Session(string Ida, string Volta, string Apoio, string Percurso, string Retorno, string Aliases, string Sistema, decimal Fator, decimal Angular, decimal Precisao);

    public TraversesPage() : base("POLIGONAL — IFM", "Importe a caderneta e os apoios, confira o percurso e recalcule o ajuste. Os arquivos originais são preservados. Nenhum arquivo .bat é executado.")
    {
        AddRow("Levantamento ida",ida);AddWide(Pick("Selecionar .m21 ou .bat…",ida,false));
        AddWide(Wrap("O cálculo usa partida e chegada conhecidas, com ré inicial e orientação final. Esses pontos podem reutilizar M001/M002: não exige ocupar novamente M001. Uma segunda campanha de volta é opcional.","muted"));
        AddRow("Levantamento volta (opcional)",volta);AddWide(Pick("Selecionar volta…",volta,false));
        AddWide(Wrap(".bat aceita somente texto com a mesma codificação M21 Topcon reconhecida. Outros formatos precisam de um exemplo para adaptação.","muted"));
        AddWide(Pick("Importar pontos conhecidos (.txt)…",apoio,true));
        AddWide(Wrap("Apoios: Ponto;N;E;Cota ou Nome Nome N E Cota. Inclua partida, ré inicial, chegada e vante final.","muted"));AddWide(apoio);
        AddWide(Wrap("Percurso de ida: ré inicial > partida > estações intermediárias > chegada > vante final. Edite a ordem antes de calcular.","muted"));AddWide(percurso);
        AddWide(Wrap("Percurso de volta (se houver arquivo independente). Mesmo formato, com ré e vante próprias.","muted"));AddWide(retorno);
        AddWide(Wrap("Associações de nomes: original=confirmado, uma por linha. Nenhum nome é corrigido silenciosamente.","muted"));AddWide(aliases);
        var prepare=Ui.Button("Conferir apoios e preparar nomes…");prepare.Click+=(_,_)=>PrepareSurvey();AddWide(prepare);
        AddRow("Sistema / datum / fuso declarado",system);AddRow("Fator horizontal informado",factor);
        AddWide(Wrap("Fator 1 = sem redução adicional. Se os apoios estão em UTM, confirme a redução das distâncias. O programa calcula o fechamento, mas não estima esse fator para fazer o levantamento passar.","warning"));
        AddWide(Wrap("Critérios de conferência do usuário. Zero = não avaliar. Informe os valores aplicáveis ao contrato/classe; não são preenchidos como limites universais DNIT.","warning"));
        AddRow("Tolerância angular total (segundos)",angular);AddRow("Precisão mínima 1 para",precision);
        var reference=Ui.Button("Referência linear NBR 13133:2021 — 1:12.000");
        reference.Click+=(_,_)=>
        {
            if(!Ui.Confirm(this,"CRITÉRIO LINEAR — IFM","Usar 1:12.000 como referência linear do item 5.6.6 da NBR 13133:2021?\n\nConfirme que essa edição e esse critério se aplicam ao contrato. Não define a tolerância angular, o fator UTM nem certifica os demais requisitos."))return;
            precision.Value=12000;SetStatus("Referência linear 1:12.000 aplicada. Tolerância angular e redução ao sistema ainda precisam de conferência.");
        };AddWide(reference);
        var calc=Ui.Button("Calcular / recalcular e ajustar",true);calc.Click+=(_,_)=>Calculate();AddWide(calc);
        summary.ReadOnly=true;summary.Height=200;AddWide(summary);
        export.Enabled=false;export.Click+=(_,_)=>Export();AddWide(export);
        var save=Ui.Button("Salvar entradas para recálculo…");save.Click+=(_,_)=>Save();
        var load=Ui.Button("Abrir entradas salvas…");load.Click+=(_,_)=>LoadSession();AddWide(Ui.Row(save,load));
        foreach(var t in new[]{ida,volta,apoio,percurso,retorno,aliases,system})t.TextChanged+=(_,_)=>InvalidateResult();
        foreach(var n in new[]{factor,angular,precision})n.ValueChanged+=(_,_)=>InvalidateResult();
    }
    private static TextBox Field(bool multi=false)=>new(){Dock=DockStyle.Fill,Multiline=multi,Height=multi?90:26,ScrollBars=multi?ScrollBars.Vertical:ScrollBars.None};
    private Button Pick(string title,TextBox target,bool text)
    {
        var button=Ui.Button(title);button.Click+=(_,_)=>
        {
            using var dialog=new OpenFileDialog{Title=title,Filter=text?"Pontos de apoio (*.txt;*.csv)|*.txt;*.csv":"Caderneta (*.m21;*.bat)|*.m21;*.bat",CheckFileExists=true};
            if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            try
            {
                if(text){target.Text=Traverse.ReadText(dialog.FileName);return;}
                var book=Traverse.Read(dialog.FileName);target.Text=dialog.FileName;
                (target==ida?percurso:retorno).Text=Traverse.SuggestedRoute(book);
                SetStatus($"Importadas {book.Shots.Count} observações e {book.Stations.Length} estações. Confira o percurso e os nomes compostos.");
            }
            catch(Exception ex){SetStatus("Importação não concluída: "+ex.Message,"warning");}
        };return button;
    }
    protected override void OnSnapshot(DrawingSnapshot? snapshot) { /* Independent of the DWG; keep user inputs and reviewed result. */ }
    private void PrepareSurvey()
    {
        try
        {
            var book=Traverse.Read(ida.Text);
            if(string.IsNullOrWhiteSpace(percurso.Text))percurso.Text=Traverse.SuggestedRoute(book);
            var route=Traverse.Route(percurso.Text);if(route.Length<4)throw new ArgumentException("Informe o percurso com ré inicial e vante final.");
            var known=Traverse.Support(apoio.Text);
            foreach(var name in new[]{route[0],route[1],route[^2],route[^1]})if(!known.ContainsKey(name))throw new ArgumentException("Falta coordenada do apoio: "+name);
            var proposed=Traverse.SuggestedAliases(book,percurso.Text);
            if(!string.IsNullOrWhiteSpace(volta.Text))
            {
                var other=Traverse.Read(volta.Text);if(string.IsNullOrWhiteSpace(retorno.Text))retorno.Text=Traverse.SuggestedRoute(other);
                foreach(var pair in Traverse.SuggestedAliases(other,retorno.Text))
                {
                    if(proposed.TryGetValue(pair.Key,out var old)&&old!=pair.Value)throw new ArgumentException("Nomes conflitantes entre campanhas: "+pair.Key);
                    proposed[pair.Key]=pair.Value;
                }
            }
            var mapping=string.Join(Environment.NewLine,proposed.Select(p=>p.Key+"="+p.Value));
            if(proposed.Count>0)
            {
                if(!string.IsNullOrWhiteSpace(aliases.Text)){SetStatus("Apoios conferidos. Associações já preenchidas: revise-as antes de calcular.");return;}
                if(!Ui.Confirm(this,"CONFERIR NOMES — IFM","Propostas pela sequência de visadas (confirme na caderneta):\n\n"+mapping+"\n\nAplicar essas associações? Os arquivos originais serão preservados."))return;
                aliases.Text=mapping;
            }
            SetStatus($"Apoios conferidos: {route[1]} → {route[^2]}; orientação final {route[^2]} → {route[^1]}. Clique em Calcular / recalcular.");
        }
        catch(Exception ex){SetStatus("Preparação não concluída: "+ex.Message,"warning");}
    }
    private void InvalidateResult(){report=null;export.Enabled=false;resultIda=null;resultVolta=null;summary.Clear();}
    private Session Inputs()=>new(ida.Text,volta.Text,apoio.Text,percurso.Text,retorno.Text,aliases.Text,system.Text,factor.Value,angular.Value,precision.Value);
    private void Calculate()
    {
        InvalidateResult();
        try
        {
            var input=Inputs();bookIda=Traverse.Read(input.Ida);
            resultIda=Traverse.Calculate(bookIda,input.Apoio,input.Percurso,input.Aliases,(double)input.Fator,(double)input.Angular,(double)input.Precisao,input.Sistema);
            var text=new StringBuilder(resultIda.Report);
            if(!string.IsNullOrWhiteSpace(input.Volta))
            {
                bookVolta=Traverse.Read(input.Volta);
                resultVolta=Traverse.Calculate(bookVolta,input.Apoio,input.Retorno,input.Aliases,(double)input.Fator,(double)input.Angular,(double)input.Precisao,input.Sistema);
                text.AppendLine("\nVOLTA INDEPENDENTE\n"+resultVolta.Report+"\nCOMPARAÇÃO IDA / VOLTA AJUSTADAS\nPonto;dN;dE;dZ;distância horizontal m");
                var common=0;
                foreach(var a in resultIda.Points)
                {
                    var b=resultVolta.Points.FirstOrDefault(p=>p.Name.Equals(a.Name,StringComparison.OrdinalIgnoreCase));if(b is null)continue;
                    common++;text.AppendLine(string.Join(";",a.Name,Traverse.F(a.N-b.N),Traverse.F(a.E-b.E),Traverse.F(a.Z-b.Z),Traverse.F(Math.Sqrt(Math.Pow(a.N-b.N,2)+Math.Pow(a.E-b.E,2)))));
                }
                text.AppendLine($"Pontos comuns: {common}. Apoios fixos coincidem por injunção; isso não comprova precisão. Resultados das campanhas não são fundidos automaticamente.");
            }
            else text.AppendLine("\nNão foi importada campanha independente de volta. Conferência disponível: distâncias e desníveis recíprocos presentes no M21.");
            report=text.ToString();summary.Text=report.Replace("\n",Environment.NewLine);export.Enabled=true;
            SetStatus($"Calculado: {resultIda.Points.Count} pontos. Erro angular {Traverse.F(resultIda.AngularError)} segundos; linear {Traverse.F(resultIda.LinearError)} m. Confira relatório e critérios antes de usar.","good");
        }
        catch(Exception ex){InvalidateResult();SetStatus("Poligonal não calculada: "+ex.Message,"warning");}
    }
    private static JsonSerializerOptions JsonOptions=>new(){WriteIndented=true};
    private void Save()
    {
        using var dialog=new SaveFileDialog{Filter="Entradas de poligonal (*.json)|*.json",FileName="Poligonal IFM.json",OverwritePrompt=true};
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try{File.WriteAllText(dialog.FileName,JsonSerializer.Serialize(Inputs(),JsonOptions),new UTF8Encoding(true));SetStatus("Entradas salvas. O recálculo relê os arquivos de observações.");}
        catch(Exception ex){SetStatus(ex.Message,"warning");}
    }
    private void LoadSession()
    {
        using var dialog=new OpenFileDialog{Filter="Entradas de poligonal (*.json)|*.json"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            var x=JsonSerializer.Deserialize<Session>(Traverse.ReadText(dialog.FileName))??throw new ArgumentException("Entradas inválidas.");
            if(x.Fator<factor.Minimum||x.Fator>factor.Maximum||x.Angular<angular.Minimum||x.Angular>angular.Maximum||x.Precisao<precision.Minimum||x.Precisao>precision.Maximum)throw new ArgumentException("Parâmetros fora do intervalo.");
            ida.Text=x.Ida;volta.Text=x.Volta;apoio.Text=x.Apoio;percurso.Text=x.Percurso;retorno.Text=x.Retorno;aliases.Text=x.Aliases;system.Text=x.Sistema;factor.Value=x.Fator;angular.Value=x.Angular;precision.Value=x.Precisao;
            SetStatus("Entradas reabertas. Confira e clique em Calcular / recalcular.");
        }
        catch(Exception ex){SetStatus("Não foi possível abrir: "+ex.Message,"warning");}
    }
    private void Export()
    {
        if(report is null||resultIda is null)return;
        using var dialog=new FolderBrowserDialog{Description="Pasta para salvar uma nova revisão do relatório"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            var folder=Path.Combine(dialog.SelectedPath,"Poligonal_IFM_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+"_"+Guid.NewGuid().ToString("N")[..6]);Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"Relatorio.html"),"<!doctype html><html lang='pt-BR'><meta charset='utf-8'><title>POLIGONAL — IFM</title><style>body{font:14px Arial;margin:35px}pre{white-space:pre-wrap}table{border-collapse:collapse}td,th{border:1px solid #bbb;padding:6px}@media print{body{font-size:10pt}}</style><h1>POLIGONAL — IFM</h1><pre>"+WebUtility.HtmlEncode(report)+"</pre>"+Table(resultIda,"Ida")+(resultVolta is null?"":Table(resultVolta,"Volta"))+"</html>",new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(folder,"Entradas.json"),JsonSerializer.Serialize(Inputs(),JsonOptions),new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(folder,"Auditoria.json"),JsonSerializer.Serialize(new{Ida=bookIda,Volta=bookVolta,ResultadoIda=resultIda,ResultadoVolta=resultVolta},JsonOptions),new UTF8Encoding(true));
            WriteResult(folder,"Ida",resultIda);if(resultVolta is not null)WriteResult(folder,"Volta",resultVolta);
            SetStatus("Relatório e nova revisão salvos em "+folder,"good");
        }
        catch(Exception ex){SetStatus("Exportação não concluída: "+ex.Message,"warning");}
    }
    private static string Table(TraverseResult r,string name)
    {
        var s=new StringBuilder("<h2>"+name+" — Coordenadas após ajuste angular e após Bowditch</h2><table><tr><th>Ponto</th><th>N antes</th><th>E antes</th><th>Cota antes</th><th>N ajustado</th><th>E ajustado</th><th>Cota ajustada</th></tr>");
        for(var i=0;i<r.Points.Count;i++){var a=r.RawPoints[i];var b=r.Points[i];s.Append("<tr>"+string.Concat(new[]{b.Name,Traverse.F(a.N),Traverse.F(a.E),Traverse.F(a.Z),Traverse.F(b.N),Traverse.F(b.E),Traverse.F(b.Z)}.Select(x=>"<td>"+WebUtility.HtmlEncode(x)+"</td>"))+"</tr>");}return s+"</table>";
    }
    private static void WriteResult(string folder,string name,TraverseResult r)
    {
        File.WriteAllText(Path.Combine(folder,name+"_Memoria.csv"),r.Memory,new UTF8Encoding(true));
        File.WriteAllText(Path.Combine(folder,name+"_Coordenadas.csv"),"Ponto;N;E;Cota\n"+string.Join("\n",r.Points.Select(p=>string.Join(";",p.Name,Traverse.F(p.N),Traverse.F(p.E),Traverse.F(p.Z)))),new UTF8Encoding(true));
    }
}
