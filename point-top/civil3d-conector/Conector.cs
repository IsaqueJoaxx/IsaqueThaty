// POINT TOP — conector 0.4 para Civil 3D 2027.
// Diferenças em relação à prévia 0.3 (civil3d\Link.cs, mantida para análise):
//  - nada roda ao carregar a DLL: sem IExtensionApplication e sem evento antes de PTVINCULAR2;
//  - comandos e eventos passam por proteção, porque exceção gerenciada não tratada encerra o Civil 3D;
//  - todo acesso a tipos Civil fica em método interno, chamado dentro da proteção;
//  - a reconstrução automática só existe se o usuário responder Sim, e o padrão é Nao;
//  - o greide é comparado com o do momento do vínculo. O Civil 3D mantém as cotas dos PVIs, mas desloca
//    as estacas quando o comprimento do eixo muda (observado em teste); o conector avisa em vez de esconder.
// Falha nativa (fora do .NET) continua possível; o log delimita a última etapa concluída.
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using CAlignment = Autodesk.Civil.DatabaseServices.Alignment;
using CBaseline = Autodesk.Civil.DatabaseServices.Baseline;
using CCorridor = Autodesk.Civil.DatabaseServices.Corridor;
using CProfile = Autodesk.Civil.DatabaseServices.Profile;
using CProfilePVI = Autodesk.Civil.DatabaseServices.ProfilePVI;
using CProfileType = Autodesk.Civil.DatabaseServices.ProfileType;
using CRegion = Autodesk.Civil.DatabaseServices.BaselineRegion;

[assembly: CommandClass(typeof(PointTop.Conector2027.Comandos))]

namespace PointTop.Conector2027;

internal sealed record GreidePerfil(string Nome, List<double[]> Pvis);

internal sealed record ResumoGreide(string Situacao, int Pvis, int Movidos, double MaxEstaca, double MaxCota, string Texto);

internal sealed class Vinculo
{
    public required Document Doc;
    public ObjectId Eixo, Corredor;
    public readonly HashSet<ObjectId> Observados = new();
    public bool Alterado, Pendente, Automatico, Atualizando, AguardandoReconstrucao;
    public DateTime UltimoEnvio = DateTime.MinValue;
    public object? Leitura;
    public string Mensagem = "";
    public List<GreidePerfil> GreideBase = new();
}

public sealed class Comandos
{
    private const string Versao = "0.4.0";
    private static Vinculo? vinculo;
    private static bool avisouPublicacao, ocioso;

    // ---------- Comandos: só proteção aqui; nenhum tipo Civil na assinatura ou no corpo. ----------
    [CommandMethod("PTVINCULAR2", CommandFlags.Modal)]
    public void Vincular() => Proteger("VINCULAR", VincularInterno);

    [CommandMethod("PTATUALIZAR2", CommandFlags.Modal)]
    public void Atualizar() => Proteger("ATUALIZAR", AtualizarInterno);

    [CommandMethod("PTESTADO", CommandFlags.Modal)]
    public void Estado() => Proteger("ESTADO", EstadoInterno);

    [CommandMethod("PTGREIDEOK", CommandFlags.Modal)]
    public void AceitarGreide() => Proteger("GREIDEOK", AceitarGreideInterno);

    [CommandMethod("PTDESLIGAR2", CommandFlags.Modal)]
    public void Desligar() => Proteger("DESLIGAR", () => { Parar(); Escrever("\nPOINT TOP: vínculo desligado.\n"); });

    private static void Proteger(string etapa, Action acao)
    {
        Registrar(etapa + "_INICIO", "");
        try { acao(); Registrar(etapa + "_FIM", ""); }
        catch (System.Exception ex)
        {
            Registrar(etapa + "_ERRO", ex.ToString());
            if (vinculo != null) { vinculo.Automatico = false; vinculo.Mensagem = "Falha em " + etapa + ": " + ex.Message + " Automático suspenso."; }
            Escrever("\nPOINT TOP: falha registrada em " + etapa + ": " + ex.Message + "\n");
        }
    }

    private static void EmEvento(string etapa, Action acao)
    {
        try { acao(); }
        catch (System.Exception ex)
        {
            Registrar("EVENTO_" + etapa + "_ERRO", ex.ToString());
            // Erro em evento não pode se repetir a cada ciclo ocioso: encerra o vínculo.
            try { Parar(); } catch (System.Exception ex2) { Registrar("PARAR_ERRO", ex2.ToString()); }
        }
    }

    // ---------- Vínculo ----------
    private static void VincularInterno()
    {
        var alvo = App.DocumentManager.MdiActiveDocument;
        if (alvo == null) return;
        var ed = alvo.Editor;
        var eixoSel = ed.GetEntity(new PromptEntityOptions("\nPOINT TOP: selecione o alinhamento: "));
        Registrar("SELECAO_EIXO", eixoSel.Status.ToString());
        if (eixoSel.Status != PromptStatus.OK) return;

        using (var tr = alvo.Database.TransactionManager.StartOpenCloseTransaction())
        {
            if (tr.GetObject(eixoSel.ObjectId, OpenMode.ForRead) is not CAlignment a) { ed.WriteMessage("\nO objeto selecionado não é um alinhamento. Nada foi vinculado.\n"); return; }
            if (a.IsReferenceObject) { ed.WriteMessage("\nEixo referenciado: edite no desenho de origem. Nada foi vinculado.\n"); return; }
        }

        var corrOp = new PromptEntityOptions("\nSelecione o corredor dependente ou Enter para apenas acompanhar: ") { AllowNone = true };
        var corrSel = ed.GetEntity(corrOp);
        Registrar("SELECAO_CORREDOR", corrSel.Status.ToString());
        if (corrSel.Status != PromptStatus.OK && corrSel.Status != PromptStatus.None) return;
        var corredor = corrSel.Status == PromptStatus.OK ? corrSel.ObjectId : ObjectId.Null;

        if (!corredor.IsNull)
        {
            using var tr = alvo.Database.TransactionManager.StartOpenCloseTransaction();
            if (tr.GetObject(corredor, OpenMode.ForRead) is not CCorridor c) { ed.WriteMessage("\nO objeto selecionado não é um corredor. Nada foi vinculado.\n"); return; }
            if (c.IsReferenceObject || !Depende(c, eixoSel.ObjectId)) { ed.WriteMessage("\nCorredor referenciado ou sem linha de base neste eixo. Nada foi vinculado.\n"); return; }
        }

        var automatico = false;
        if (!corredor.IsNull)
        {
            var op = new PromptKeywordOptions("\nReconstruir o corredor sozinho ao terminar cada edição do eixo?") { AllowNone = true };
            op.Keywords.Add("Sim");
            op.Keywords.Add("Nao");
            op.Keywords.Default = "Nao";
            var r = ed.GetKeywords(op);
            if (r.Status == PromptStatus.Cancel) return;
            automatico = r.StringResult == "Sim";
        }

        Parar();
        var v = new Vinculo { Doc = alvo, Eixo = eixoSel.ObjectId, Corredor = corredor, Automatico = automatico };
        v.Mensagem = automatico ? "Vinculado; reconstrução automática ligada para este corredor." : "Vinculado; reconstrução só com PTATUALIZAR2.";
        v.GreideBase = LerGreides(v);
        vinculo = v;
        alvo.Database.ObjectModified += AoModificar;
        alvo.CommandEnded += AoTerminar;
        alvo.CommandCancelled += AoCancelar;
        alvo.CommandFailed += AoCancelar;
        App.DocumentManager.DocumentToBeDestroyed += AoFecharDesenho;
        App.Idle += AoOcioso;
        Ler(v);
        ed.WriteMessage("\nPOINT TOP: vinculado. Edite o eixo normalmente. PTATUALIZAR2 reconstrói; PTESTADO mostra a situação; PTDESLIGAR2 encerra.\n");
    }

    private static bool Depende(CCorridor c, ObjectId eixo)
    {
        foreach (CBaseline b in c.Baselines)
        {
            try { if (b.AlignmentId == eixo) return true; }
            catch (System.Exception) { /* Linha de base por feature line não usa AlignmentId. */ }
        }
        return false;
    }

    private static void Parar()
    {
        var v = vinculo;
        App.Idle -= AoOcioso;
        App.DocumentManager.DocumentToBeDestroyed -= AoFecharDesenho;
        if (v == null) return;
        vinculo = null;
        try
        {
            v.Doc.Database.ObjectModified -= AoModificar;
            v.Doc.CommandEnded -= AoTerminar;
            v.Doc.CommandCancelled -= AoCancelar;
            v.Doc.CommandFailed -= AoCancelar;
        }
        catch (System.Exception ex) { Registrar("DESASSINAR_ERRO", ex.ToString()); }
        Publicar(v, false);
    }

    // ---------- Eventos ----------
    private static void AoModificar(object sender, ObjectEventArgs e)
    {
        // Precisa ser leve: dispara para qualquer objeto do desenho.
        try
        {
            var v = vinculo;
            if (v != null && !v.Atualizando && v.Observados.Contains(e.DBObject.ObjectId)) { v.Alterado = true; v.Pendente = true; }
        }
        catch (System.Exception ex) { Registrar("MODIFICAR_ERRO", ex.Message); }
    }

    private static void AoTerminar(object sender, CommandEventArgs e) => EmEvento("TERMINAR", () =>
    {
        var v = vinculo;
        if (v == null) return;
        var cmd = e.GlobalCommandName.ToUpperInvariant();
        if (cmd is "U" or "UNDO" or "REDO" or "MREDO")
        {
            if (v.Automatico) { v.Automatico = false; v.Mensagem = "Desfazer/refazer detectado: automático suspenso. Use PTVINCULAR2 para ligar de novo."; }
        }
        v.Pendente = true;
    });

    private static void AoCancelar(object sender, CommandEventArgs e) => EmEvento("CANCELAR", () =>
    {
        var v = vinculo;
        if (v == null) return;
        if (v.Automatico) { v.Automatico = false; v.Mensagem = "Comando cancelado ou com falha: automático suspenso. Confira o desenho e use PTVINCULAR2."; }
        v.Pendente = true;
    });

    private static void AoFecharDesenho(object sender, DocumentCollectionEventArgs e) => EmEvento("FECHAR", () =>
    {
        if (vinculo != null && e.Document == vinculo.Doc) Parar();
    });

    private static void AoOcioso(object? sender, EventArgs e) => EmEvento("OCIOSO", () =>
    {
        var v = vinculo;
        if (v == null || v.Atualizando) return;
        if (!ocioso) { ocioso = true; Registrar("OCIOSO_PRIMEIRO", "Evento de ociosidade ativo."); }
        var ativo = App.DocumentManager.MdiActiveDocument == v.Doc;
        if (ativo && Convert.ToInt32(App.GetSystemVariable("CMDACTIVE")) == 0) Processar(v);
        if ((DateTime.UtcNow - v.UltimoEnvio).TotalSeconds >= 3) Publicar(v, true);
    });

    private static void Processar(Vinculo v)
    {
        if (!v.Pendente) return;
        v.Pendente = false;
        if (v.Alterado)
        {
            v.Alterado = false;
            if (!v.Corredor.IsNull)
            {
                v.AguardandoReconstrucao = true;
                if (v.Automatico)
                {
                    // Comando próprio: a reconstrução entra no desfazer do Civil 3D.
                    Registrar("AUTOMATICO_AGENDADO", "");
                    v.Doc.SendStringToExecute("PTATUALIZAR2 ", false, false, false);
                    return;
                }
                v.Mensagem = "Eixo ou perfil alterado. Corredor ainda não reconstruído: use PTATUALIZAR2.";
            }
            else v.Mensagem = "Eixo ou perfil alterado. Nenhum corredor vinculado.";
        }
        Ler(v);
    }

    // ---------- Reconstrução ----------
    private static void AtualizarInterno()
    {
        var v = vinculo;
        if (v == null) { Escrever("\nPOINT TOP: nenhum vínculo ativo. Use PTVINCULAR2.\n"); return; }
        if (v.Doc != App.DocumentManager.MdiActiveDocument) { Escrever("\nPOINT TOP: ative o desenho vinculado.\n"); return; }
        v.Atualizando = true;
        try
        {
            if (v.Corredor.IsNull) { v.Mensagem = "Traçado atualizado no painel; nenhum corredor vinculado."; return; }
            var greideAntes = LerGreides(v);
            var inicio = DateTime.UtcNow;
            var reconstruiu = false;
            using (var tr = v.Doc.Database.TransactionManager.StartTransaction())
            {
                var c = (CCorridor)tr.GetObject(v.Corredor, OpenMode.ForRead);
                var camada = (LayerTableRecord)tr.GetObject(c.LayerId, OpenMode.ForRead);
                if (camada.IsLocked) throw new InvalidOperationException("A camada do corredor está bloqueada.");
                if (c.IsReferenceObject || !Depende(c, v.Eixo)) throw new InvalidOperationException("O corredor é referência ou perdeu o vínculo com o eixo.");
                if (c.IsOutOfDate)
                {
                    c.UpgradeOpen();
                    Registrar("REBUILD_INICIO", c.Name);
                    c.Rebuild();
                    reconstruiu = true;
                    Registrar("REBUILD_FIM", (DateTime.UtcNow - inicio).TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s");
                }
                tr.Commit();
            }
            bool desatualizado;
            using (var tr = v.Doc.Database.TransactionManager.StartOpenCloseTransaction())
                desatualizado = ((CCorridor)tr.GetObject(v.Corredor, OpenMode.ForRead)).IsOutOfDate;
            if (desatualizado) throw new InvalidOperationException("O Civil 3D ainda informa corredor desatualizado. Confira alvos, greide e regiões.");

            var greideDepois = LerGreides(v);
            var pelaReconstrucao = Comparar(greideAntes, greideDepois);
            var resumo = Comparar(v.GreideBase, greideDepois);
            var texto = reconstruiu ? "Corredor reconstruído em " + (DateTime.UtcNow - inicio).TotalSeconds.ToString("F0", CultureInfo.GetCultureInfo("pt-BR")) + " s." : "Corredor já estava atualizado.";
            if (pelaReconstrucao.Situacao != "igual") texto += " ATENÇÃO: a própria reconstrução alterou PVIs do greide. Confira o perfil.";
            v.Mensagem = texto + " " + resumo.Texto;
            Registrar("GREIDE", resumo.Situacao + " | reconstrução: " + pelaReconstrucao.Situacao + " | " + resumo.Texto);
            v.AguardandoReconstrucao = false;
            v.Alterado = false;
        }
        catch (System.Exception ex)
        {
            v.Automatico = false;
            v.Mensagem = "Falha ao reconstruir: " + ex.Message + " Automático suspenso.";
            Registrar("ATUALIZAR_FALHA", ex.ToString());
        }
        finally
        {
            v.Atualizando = false;
            Ler(v);
            Escrever("\nPOINT TOP: " + v.Mensagem + "\n");
        }
    }

    private static List<GreidePerfil> LerGreides(Vinculo v)
    {
        var lista = new List<GreidePerfil>();
        using var tr = v.Doc.Database.TransactionManager.StartOpenCloseTransaction();
        var a = (CAlignment)tr.GetObject(v.Eixo, OpenMode.ForRead);
        foreach (ObjectId id in a.GetProfileIds())
        {
            var p = (CProfile)tr.GetObject(id, OpenMode.ForRead);
            if (p.ProfileType != CProfileType.FG) continue;
            var pvis = new List<double[]>();
            foreach (CProfilePVI pvi in p.PVIs) pvis.Add(new[] { pvi.RawStation, pvi.Elevation });
            lista.Add(new GreidePerfil(p.Name, pvis));
        }
        return lista;
    }

    private static ResumoGreide Comparar(List<GreidePerfil> referencia, List<GreidePerfil> atual)
    {
        const double tolerancia = 0.0005;
        var br = CultureInfo.GetCultureInfo("pt-BR");
        var total = atual.Sum(g => g.Pvis.Count);
        var nomesIguais = referencia.Select(g => g.Nome).SequenceEqual(atual.Select(g => g.Nome));
        if (!nomesIguais || total != referencia.Sum(g => g.Pvis.Count))
            return new ResumoGreide("estrutura", total, 0, 0, 0, "ATENÇÃO: perfis ou número de PVIs do greide mudaram desde o vínculo. Confira o perfil; PTGREIDEOK aceita o greide atual.");
        int movidos = 0, cotas = 0;
        double maxE = 0, maxC = 0;
        for (var g = 0; g < atual.Count; g++)
            for (var i = 0; i < atual[g].Pvis.Count; i++)
            {
                var dE = Math.Abs(atual[g].Pvis[i][0] - referencia[g].Pvis[i][0]);
                var dC = Math.Abs(atual[g].Pvis[i][1] - referencia[g].Pvis[i][1]);
                if (dE > tolerancia) { movidos++; maxE = Math.Max(maxE, dE); }
                if (dC > tolerancia) { cotas++; maxC = Math.Max(maxC, dC); }
            }
        if (cotas > 0)
            return new ResumoGreide("cotas", total, cotas, maxE, maxC, "ATENÇÃO: " + cotas + " PVIs com cota diferente do vínculo (até " + maxC.ToString("N3", br) + " m). Se foi edição sua, PTGREIDEOK aceita o greide atual.");
        if (movidos > 0)
            return new ResumoGreide("estacas", total, movidos, maxE, 0, "Greide: cotas dos " + total + " PVIs iguais, mas " + movidos + " mudaram de estaca (até " + maxE.ToString("N3", br) + " m), o que acontece quando o eixo muda de comprimento. Confira o perfil; PTGREIDEOK aceita o greide atual.");
        return new ResumoGreide("igual", total, 0, 0, 0, "Greide conferido: " + total + " PVIs iguais ao vínculo.");
    }

    private static void AceitarGreideInterno()
    {
        var v = vinculo;
        if (v == null) { Escrever("\nPOINT TOP: nenhum vínculo ativo.\n"); return; }
        v.GreideBase = LerGreides(v);
        v.Mensagem = "Greide atual aceito como referência.";
        Ler(v);
        Escrever("\nPOINT TOP: " + v.Mensagem + "\n");
    }

    // ---------- Leitura para o painel ----------
    private static void Ler(Vinculo v)
    {
        try
        {
            using var tr = v.Doc.Database.TransactionManager.StartOpenCloseTransaction();
            var a = (CAlignment)tr.GetObject(v.Eixo, OpenMode.ForRead);
            v.Observados.Clear();
            v.Observados.Add(v.Eixo);
            var perfis = new List<object>();
            double? fimGreide = null;
            foreach (ObjectId id in a.GetProfileIds())
            {
                v.Observados.Add(id);
                var p = (CProfile)tr.GetObject(id, OpenMode.ForRead);
                var fg = p.ProfileType == CProfileType.FG;
                if (fg) fimGreide = Math.Max(fimGreide ?? double.MinValue, p.EndingStation);
                perfis.Add(new { name = p.Name, handle = p.Handle.ToString(), kind = p.ProfileType.ToString(), start = p.StartingStation, end = p.EndingStation, manual = fg });
            }
            var pontos = new List<object>();
            var falhas = 0;
            var n = Math.Clamp((int)Math.Ceiling(a.Length / 10), 2, 1000);
            for (var i = 0; i <= n; i++)
            {
                var estaca = a.StartingStation + (a.EndingStation - a.StartingStation) * i / n;
                double x = 0, y = 0;
                try { a.PointLocation(estaca, 0, ref x, ref y); pontos.Add(new { station = estaca, x, y }); }
                catch (System.Exception) { falhas++; }
            }
            object? corredor = null;
            var avisos = new List<string>();
            if (fimGreide != null && fimGreide < a.EndingStation - 0.001) avisos.Add("O greide termina antes do fim do eixo.");
            var resumoGreide = Comparar(v.GreideBase, LerGreides(v));
            if (resumoGreide.Situacao != "igual") avisos.Add(resumoGreide.Texto);
            if (!v.Corredor.IsNull)
            {
                var c = (CCorridor)tr.GetObject(v.Corredor, OpenMode.ForRead);
                var regioes = new List<object>();
                foreach (CBaseline b in c.Baselines)
                {
                    if (b.AlignmentId != v.Eixo) continue;
                    foreach (CRegion g in b.BaselineRegions)
                    {
                        regioes.Add(new { name = g.Name, start = g.StartStation, end = g.EndStation });
                        if (g.EndStation > a.EndingStation + 0.001) avisos.Add("A região " + g.Name + " passa do fim do eixo.");
                        if (fimGreide != null && g.EndStation > fimGreide + 0.001) avisos.Add("A região " + g.Name + " passa do fim do greide.");
                    }
                }
                if (!c.IsOutOfDate && v.AguardandoReconstrucao)
                {
                    v.AguardandoReconstrucao = false;
                    v.Mensagem = "Corredor atualizado.";
                }
                corredor = new { name = c.Name, handle = c.Handle.ToString(), outOfDate = c.IsOutOfDate, pending = v.AguardandoReconstrucao, regions = regioes };
            }
            v.Leitura = new
            {
                schema = 2, connector = Versao, assistant = "POINT TOP — conector Civil 3D",
                observedAt = DateTime.UtcNow, document = v.Doc.Name, units = v.Doc.Database.Insunits.ToString(),
                axis = new { name = a.Name, handle = a.Handle.ToString(), length = a.Length, startStation = a.StartingStation, endStation = a.EndingStation, points = pontos, samplingFailures = falhas },
                profiles = perfis, corridor = corredor, warnings = avisos,
                greide = new { status = resumoGreide.Situacao, pvis = resumoGreide.Pvis, changed = resumoGreide.Movidos, maxStation = resumoGreide.MaxEstaca, maxElevation = resumoGreide.MaxCota, text = resumoGreide.Texto },
                automatic = v.Automatico, message = v.Mensagem,
            };
        }
        catch (System.Exception ex)
        {
            v.Automatico = false;
            v.Mensagem = "Leitura indisponível: " + ex.Message;
            v.Leitura = new { schema = 2, connector = Versao, observedAt = DateTime.UtcNow, error = v.Mensagem };
            Registrar("LER_ERRO", ex.ToString());
        }
        Publicar(v, true);
    }

    private static void EstadoInterno()
    {
        var v = vinculo;
        if (v == null) { Escrever("\nPOINT TOP " + Versao + ": sem vínculo.\n"); return; }
        // Com automático ligado, quem agenda a reconstrução é o evento de ociosidade, nunca este comando de consulta.
        if (!(v.Alterado && v.Automatico)) Processar(v);
        Ler(v);
        var json = JsonSerializer.SerializeToElement(v.Leitura);
        var sb = new StringBuilder("\nPOINT TOP " + Versao + ": " + v.Mensagem);
        if (json.TryGetProperty("axis", out var eixo))
            sb.Append("\n  Eixo: " + eixo.GetProperty("name").GetString() + " | comprimento " + eixo.GetProperty("length").GetDouble().ToString("N3", CultureInfo.GetCultureInfo("pt-BR")));
        if (json.TryGetProperty("corridor", out var c) && c.ValueKind == JsonValueKind.Object)
            sb.Append("\n  Corredor: " + c.GetProperty("name").GetString() + " | desatualizado: " + c.GetProperty("outOfDate").GetBoolean());
        if (json.TryGetProperty("warnings", out var av))
            foreach (var x in av.EnumerateArray()) sb.Append("\n  Aviso: " + x.GetString());
        Escrever(sb + "\n");
    }

    // ---------- Publicação e registro ----------
    private static string? Raiz()
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(typeof(Comandos).Assembly.Location)!);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "server.cjs"))) d = d.Parent;
        return d?.FullName;
    }

    private static void Publicar(Vinculo v, bool conectado)
    {
        try
        {
            var raiz = Raiz();
            if (raiz == null || v.Leitura == null) return;
            var pasta = Path.Combine(raiz, "runtime", "civil3d");
            Directory.CreateDirectory(pasta);
            var arquivo = Path.Combine(pasta, "session-" + Environment.ProcessId + ".json");
            var texto = JsonSerializer.Serialize(new { heartbeat = DateTime.UtcNow, connected = conectado, active = conectado && v.Doc == App.DocumentManager.MdiActiveDocument, data = v.Leitura });
            File.WriteAllText(arquivo + ".tmp", texto);
            File.Move(arquivo + ".tmp", arquivo, true);
            v.UltimoEnvio = DateTime.UtcNow;
        }
        catch (System.Exception ex)
        {
            v.UltimoEnvio = DateTime.UtcNow;
            Registrar("PUBLICAR_ERRO", ex.Message);
            if (!avisouPublicacao) { avisouPublicacao = true; Escrever("\nPOINT TOP: falha ao comunicar com o painel: " + ex.Message + "\n"); }
        }
    }

    private static void Escrever(string texto)
    {
        try { App.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(texto); }
        catch (System.Exception ex) { Registrar("ESCREVER_ERRO", ex.Message); }
    }

    private static void Registrar(string etapa, string detalhe)
    {
        try
        {
            var raiz = Raiz();
            if (raiz == null) return;
            var pasta = Path.Combine(raiz, "runtime", "civil3d-diagnostics");
            Directory.CreateDirectory(pasta);
            var bytes = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O") + " | " + etapa + " | " + detalhe + Environment.NewLine);
            using var s = new FileStream(Path.Combine(pasta, "conector-" + Environment.ProcessId + ".log"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.WriteThrough);
            s.Write(bytes);
            s.Flush(true);
        }
        catch (System.Exception) { /* Registro nunca pode derrubar o comando. */ }
    }
}
