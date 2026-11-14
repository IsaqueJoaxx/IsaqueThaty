using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Integracao;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.Comandos;

public sealed class CmdImportar
{
    /// <summary>PRJIMPORTARPIPE — converte uma Pipe Network do Civil 3D em rede do PROJETA.</summary>
    [CommandMethod("PRJIMPORTARPIPE", CommandFlags.Modal)]
    public static void ImportarPipe() => Operacoes.Rodar("PRJIMPORTARPIPE", s =>
    {
        var ids = CivilApplication.ActiveDocument.GetPipeNetworkIds();
        var redesC3d = new List<Network>();
        foreach (ObjectId id in ids) if (s.Tr.GetObject(id, OpenMode.ForRead) is Network n) redesC3d.Add(n);
        if (redesC3d.Count == 0) { Ctx.Msg("Não há Pipe Networks no desenho."); return; }
        int i = s.EscolherDaLista("Pipe Networks", redesC3d.Select(n => n.Name).ToList());
        if (i < 0) return;
        var net = redesC3d[i];
        var tipo = Operacoes.LerTipo(s.PedirPalavra("Tipo da rede", new[] { "Drenagem", "Esgoto", "Generica" }, "Drenagem"));
        if (tipo is null) return;
        bool manter = s.PedirPalavra("Manter diâmetros e cotas de fundo da Pipe Network", new[] { "Sim", "Nao" }, "Sim") == "Sim";
        var rede = Operacoes.NovaRede(s.Projeto, net.Name, tipo.Value);
        rede.HandleRedeC3d = net.Handle.ToString();

        var porEstrutura = new Dictionary<ObjectId, No>();
        foreach (ObjectId sid in net.GetStructureIds())
        {
            var st = (Structure)s.Tr.GetObject(sid, OpenMode.ForRead);
            var no = rede.NovoNo(TipoNo.PV, st.Position.X, st.Position.Y);
            no.Nome = st.Name;
            no.HandleEstruturaC3d = st.Handle.ToString();
            no.CotaTampaForcada = st.RimElevation;
            porEstrutura[sid] = no;
        }
        No NoNaPonta(Point3d p)
        {
            var existente = rede.Nos.FirstOrDefault(n => Math.Abs(n.X - p.X) < 0.01 && Math.Abs(n.Y - p.Y) < 0.01);
            if (existente is not null) return existente;
            var n = rede.NovoNo(TipoNo.Exutorio, p.X, p.Y);
            n.CotaTampaForcada = p.Z + 1.0;
            return n;
        }
        int nTubos = 0;
        foreach (ObjectId pid in net.GetPipeIds())
        {
            var pipe = (Pipe)s.Tr.GetObject(pid, OpenMode.ForRead);
            double alt = pipe.InnerHeight > 0 ? pipe.InnerHeight : pipe.InnerDiameterOrWidth;
            double zi = pipe.StartPoint.Z - alt / 2, zf = pipe.EndPoint.Z - alt / 2;
            var a = !pipe.StartStructureId.IsNull && porEstrutura.TryGetValue(pipe.StartStructureId, out var na) ? na : NoNaPonta(pipe.StartPoint);
            var b = !pipe.EndStructureId.IsNull && porEstrutura.TryGetValue(pipe.EndStructureId, out var nb) ? nb : NoNaPonta(pipe.EndPoint);
            if (zf > zi) { (a, b) = (b, a); (zi, zf) = (zf, zi); } // montante = maior cota de fundo
            var t = rede.NovoTrecho(a, b);
            t.Nome = pipe.Name;
            t.HandlePipeC3d = pipe.Handle.ToString();
            if (manter)
            {
                bool ret = Math.Abs(pipe.InnerHeight - pipe.InnerDiameterOrWidth) > 0.005 && pipe.InnerHeight > 0;
                t.Forma = ret ? FormaSecao.Retangular : FormaSecao.Circular;
                t.DiametroForcado = pipe.InnerDiameterOrWidth;
                if (ret) t.AlturaForcada = pipe.InnerHeight;
                t.CotaMontanteForcada = zi;
                t.CotaJusanteForcada = zf;
            }
            nTubos++;
        }
        var top = new Topologia(rede);
        foreach (var n in top.Exutorios) n.Tipo = TipoNo.Exutorio;
        var surf = CivilUtil.EscolherSuperficie(s);
        if (surf is not null) { rede.HandleSuperficie = surf.Handle.ToString(); rede.NomeSuperficie = surf.Name; foreach (var n in rede.Nos) n.CotaTampaForcada = null; }
        Ctx.Msg($"Importados {rede.Nos.Count} nós e {nTubos} tubos de '{net.Name}'.");
        Ctx.Msg(manter
            ? "Diâmetros e cotas vieram como forçados: o cálculo só verifica. Para redimensionar, importe com 'Nao' ou limpe os forçados na planilha."
            : "Depois de conferir o cálculo (PRJCALCULAR), PRJDEVOLVERPIPE grava diâmetros e cotas nesta Pipe Network.");
        Operacoes.Atualizar(s, rede, sincronizar: false);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJIMPORTARCSV — importa nós, trechos e bacias de uma planilha CSV.</summary>
    [CommandMethod("PRJIMPORTARCSV", CommandFlags.Modal)]
    public static void ImportarCsv() => Operacoes.Rodar("PRJIMPORTARCSV", s =>
    {
        var arq = UiUtil.AbrirArquivo("Planilha CSV|*.csv;*.txt");
        if (arq is null) return;
        Rede? rede = null;
        if (s.Projeto.Redes.Count > 0 && s.PedirPalavra("Importar em", new[] { "Nova", "Existente" }, "Nova") == "Existente")
            rede = s.EscolherRede();
        if (rede is null)
        {
            var tipo = Operacoes.LerTipo(s.PedirPalavra("Tipo da nova rede", new[] { "Drenagem", "Esgoto", "Generica" }, "Drenagem"));
            if (tipo is null) return;
            rede = Operacoes.NovaRede(s.Projeto, Path.GetFileNameWithoutExtension(arq), tipo.Value);
            var surf = CivilUtil.EscolherSuperficie(s);
            if (surf is not null) { rede.HandleSuperficie = surf.Handle.ToString(); rede.NomeSuperficie = surf.Name; }
        }
        var texto = File.ReadAllText(arq, System.Text.Encoding.UTF8);
        var msgs = RedeCsv.Importar(texto, rede);
        Ctx.Msgs(msgs);
        if (msgs.Any(m => m.Gravidade == Gravidade.Erro) && s.PedirPalavra("Houve erros. Continuar mesmo assim", new[] { "Sim", "Nao" }, "Nao") != "Sim") return;
        Operacoes.Atualizar(s, rede);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJEXPORTARCSV — exporta a rede para CSV (reimportável).</summary>
    [CommandMethod("PRJEXPORTARCSV", CommandFlags.Modal)]
    public static void ExportarCsv() => Operacoes.Rodar("PRJEXPORTARCSV", s =>
    {
        var rede = s.EscolherRede();
        if (rede is null) return;
        var arq = UiUtil.SalvarArquivo("Planilha CSV|*.csv", Texto.IdSeguro(rede.Nome, 60) + ".csv");
        if (arq is null) return;
        File.WriteAllText(arq, RedeCsv.Exportar(rede), new System.Text.UTF8Encoding(true));
        Ctx.Msg("Rede exportada para " + arq);
    });

    /// <summary>PRJMODELOCSV — salva um CSV de exemplo com o formato de importação.</summary>
    [CommandMethod("PRJMODELOCSV", CommandFlags.Modal)]
    public static void ModeloCsv() => Operacoes.Rodar("PRJMODELOCSV", s =>
    {
        var arq = UiUtil.SalvarArquivo("Planilha CSV|*.csv", "modelo_rede_projeta.csv");
        if (arq is null) return;
        File.WriteAllText(arq, RedeCsv.Modelo(), new System.Text.UTF8Encoding(true));
        UiUtil.AbrirNoSistema(arq);
    });
}
