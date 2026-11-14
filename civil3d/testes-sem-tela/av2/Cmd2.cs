using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Core.Comum;
using Projeta.Core.Pavimentacao;
using AcCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

[assembly: CommandClass(typeof(Projeta.NotaServicoAvulso2.CmdNotaServicoAvulso))]

namespace Projeta.NotaServicoAvulso2;

/// <summary>
/// PRJNOTASERVICO avulso (NETLOAD): mesma montagem do plugin (NotaServicoCivil + NotaServico), com escolhas por lista
/// e a planilha gravada em 'relatorios' ao lado do DWG. Usa a configuração salva pelo plugin, se houver.
/// </summary>
public sealed class CmdNotaServicoAvulso
{
    [CommandMethod("PRJNOTASERVICO2", CommandFlags.Modal)]
    public static void Gerar()
    {
        var doc = AcCore.DocumentManager.MdiActiveDocument;
        var ed = doc.Editor;
        void Msg(string s) => ed.WriteMessage("\n" + s);
        try
        {
            using var tr = doc.Database.TransactionManager.StartTransaction();
            var cors = NotaServicoCivil.Corredores(tr);
            if (cors.Count == 0) { Msg("O desenho não tem corredores."); return; }
            int ic = Lista(ed, "Corredores", cors.Select(c => c.Nome).ToList(), 0);
            if (ic < 0) return;
            var cor = (Corridor)tr.GetObject(cors[ic].Id, OpenMode.ForRead);
            var bls = NotaServicoCivil.Baselines(cor);
            if (bls.Count == 0) { Msg($"O corredor {cor.Name} não tem baseline por alinhamento."); return; }
            int ib = Lista(ed, "Baselines", bls.Select(b => NotaServicoCivil.NomeBaseline(tr, b)).ToList(), 0);
            if (ib < 0) return;
            var bl = bls[ib];

            var sups = new List<(ObjectId Id, string Nome)>();
            foreach (ObjectId id in Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument.GetSurfaceIds())
                if (tr.GetObject(id, OpenMode.ForRead) is Surface s) sups.Add((id, s.Name));
            var nomes = sups.Select(x => x.Nome).ToList();
            int padrao = NotaServicoCivil.SuperficiePadrao(nomes, NotaServicoCivil.SuperficiesDoCorredor(cor));
            nomes.Add("(nenhuma)");
            int isup = Lista(ed, "Superfície do terreno", nomes, Math.Max(padrao, 0));
            if (isup < 0) return;
            Surface? terreno = isup < sups.Count ? (Surface)tr.GetObject(sups[isup].Id, OpenMode.ForRead) : null;

            var cfg = CarregarConfig(Msg);
            var cods = NotaServicoCivil.CodigosDaBaseline(bl);
            Msg("Códigos no corredor: " + (cods.Count == 0 ? "(nenhum)" : string.Join(", ", cods.Keys)));
            foreach (var e in cfg.Elementos)
            {
                var achado = e.Codigos.FirstOrDefault(c => cods.ContainsKey(c));
                Msg($"  {e.Nome,-14} ← {string.Join(" | ", e.Codigos)}  {(achado is null ? "[NÃO ENCONTRADO]" : $"[usa {achado}]")}");
            }

            var lida = NotaServicoCivil.Montar(tr, cor, bl, terreno, cfg, null, null, doc.Name);
            var ns = lida.Nota;
            var pasta = doc.IsNamedDrawing && !string.IsNullOrEmpty(doc.Database.Filename)
                ? Path.Combine(Path.GetDirectoryName(doc.Database.Filename)!, "relatorios")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PROJETA");
            var arq = Path.Combine(pasta, Texto.IdSeguro(ns.Titulo, 60) + "_nota_servico.xlsx");
            ns.Salvar(arq);
            Msg($"Nota de serviço gravada: {arq}");
            Msg($"  {ns.Linhas.Count} linhas de {ns.Estaca(ns.EstacaInicial)} a {ns.Estaca(ns.EstacaFinal)}; {lida.SecoesLidas} seções lidas; {lida.Interpoladas} interpoladas.");
            foreach (var a in ns.Avisos) Msg("  [AVISO] " + a);
        }
        catch (System.Exception ex) { Msg("[ERRO] PRJNOTASERVICO: " + ex.Message); }
    }

    private static int Lista(Editor ed, string titulo, IList<string> itens, int padrao)
    {
        if (itens.Count == 1) { ed.WriteMessage($"\n{titulo}: {itens[0]}"); return 0; }
        ed.WriteMessage($"\n--- {titulo} ---");
        for (int i = 0; i < itens.Count; i++) ed.WriteMessage($"\n  {i + 1}) {itens[i]}");
        var o = new PromptIntegerOptions($"\nNúmero [1-{itens.Count}] <{padrao + 1}>: ")
        {
            AllowNone = true, LowerLimit = 1, UpperLimit = itens.Count, DefaultValue = padrao + 1, UseDefaultValue = true,
        };
        var r = ed.GetInteger(o);
        if (r.Status == PromptStatus.None) return padrao;
        return r.Status == PromptStatus.OK ? r.Value - 1 : -1;
    }

    private static ConfigNotaServico CarregarConfig(Action<string> msg)
    {
        var arq = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "nota_servico.json");
        try
        {
            if (File.Exists(arq) && JsonProjeta.Desserializar<ConfigNotaServico>(File.ReadAllText(arq)) is { } c) { c.Migrar(); c.Validar(); return c; }
        }
        catch (System.Exception ex) { msg($"[AVISO] Configuração ignorada ({ex.Message})."); }
        return new ConfigNotaServico();
    }
}

