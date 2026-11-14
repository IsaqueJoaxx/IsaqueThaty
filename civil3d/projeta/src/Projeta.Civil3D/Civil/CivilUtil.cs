using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Surface = Autodesk.Civil.DatabaseServices.Surface;
using Projeta.Civil3D.Infra;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.Civil;

/// <summary>Acesso a superfícies, alinhamentos, perfis e profile views do Civil 3D.</summary>
internal static class CivilUtil
{
    public static List<(ObjectId Id, string Nome)> Superficies(Transaction tr)
    {
        var l = new List<(ObjectId, string)>();
        foreach (ObjectId id in CivilApplication.ActiveDocument.GetSurfaceIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Surface s) l.Add((id, s.Name));
        return l;
    }

    public static List<(ObjectId Id, string Nome)> Alinhamentos(Transaction tr)
    {
        var l = new List<(ObjectId, string)>();
        foreach (ObjectId id in CivilApplication.ActiveDocument.GetAlignmentIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Alignment a) l.Add((id, a.Name));
        return l;
    }

    public static List<(ObjectId Id, string Nome)> Perfis(Transaction tr, Alignment al)
    {
        var l = new List<(ObjectId, string)>();
        foreach (ObjectId id in al.GetProfileIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Profile p) l.Add((id, p.Name));
        return l;
    }

    /// <summary>Cota da superfície no ponto (NaN se fora da superfície).</summary>
    public static double Cota(Surface? s, double x, double y)
    {
        if (s is null) return double.NaN;
        try { return s.FindElevationAtXY(x, y); }
        catch { return double.NaN; }
    }

    public static bool EstacaAfastamento(Alignment? al, double x, double y, out double estaca, out double afastamento)
    {
        estaca = afastamento = double.NaN;
        if (al is null) return false;
        try
        {
            double st = 0, off = 0;
            al.StationOffset(x, y, ref st, ref off);
            estaca = st; afastamento = off;
            return true;
        }
        catch { return false; }
    }

    public static bool PontoNaEstaca(Alignment al, double estaca, double afastamento, out double x, out double y)
    {
        x = y = double.NaN;
        try
        {
            double e = 0, n = 0;
            al.PointLocation(estaca, afastamento, ref e, ref n);
            x = e; y = n;
            return true;
        }
        catch { return false; }
    }

    /// <summary>Atualiza cota do terreno e estaca/afastamento dos nós a partir da superfície e do alinhamento da rede.</summary>
    public static int AmostrarTerreno(Sessao s, Rede rede)
    {
        var surf = Ctx.Abrir<Surface>(s.Tr, s.Db, rede.HandleSuperficie);
        var al = Ctx.Abrir<Alignment>(s.Tr, s.Db, rede.HandleAlinhamento);
        int fora = 0;
        foreach (var n in rede.Nos)
        {
            if (surf is not null)
            {
                n.CotaTerreno = Cota(surf, n.X, n.Y);
                if (double.IsNaN(n.CotaTerreno) && !n.CotaTampaForcada.HasValue) fora++;
            }
            if (al is not null && EstacaAfastamento(al, n.X, n.Y, out var st, out var off)) { n.Estaca = st; n.Afastamento = off; }
            else { n.Estaca = double.NaN; n.Afastamento = double.NaN; }
        }
        // bacias: desnível do talvegue pela superfície
        foreach (var b in rede.Bacias)
        {
            var tal = Ctx.Abrir<Curve>(s.Tr, s.Db, b.HandleTalvegue);
            if (tal is null) continue;
            b.ComprimentoTalvegue = tal.GetDistanceAtParameter(tal.EndParam);
            double z1 = Cota(surf, tal.StartPoint.X, tal.StartPoint.Y), z2 = Cota(surf, tal.EndPoint.X, tal.EndPoint.Y);
            if (!double.IsNaN(z1) && !double.IsNaN(z2)) b.DesnivelTalvegue = Math.Abs(z1 - z2);
        }
        return fora;
    }

    public static Surface? EscolherSuperficie(Sessao s, bool permitirNenhuma = true)
    {
        var lista = Superficies(s.Tr);
        if (lista.Count == 0) { Ctx.Msg("Nenhuma superfície no desenho — as cotas de tampa deverão ser informadas."); return null; }
        var opcoes = lista.Select(x => x.Nome).ToList();
        if (permitirNenhuma) opcoes.Add("(nenhuma)");
        int i = s.EscolherDaLista("Superfície do terreno", opcoes);
        if (i < 0 || i >= lista.Count) return null;
        return (Surface)s.Tr.GetObject(lista[i].Id, OpenMode.ForRead);
    }

    public static Alignment? EscolherAlinhamento(Sessao s, string msg = "Selecione o alinhamento", bool permitirNenhum = true)
    {
        var peo = new PromptEntityOptions($"\n{msg}{(permitirNenhum ? " <nenhum>" : "")}: ");
        peo.SetRejectMessage("\nSelecione um alinhamento do Civil 3D.");
        peo.AddAllowedClass(typeof(Alignment), false);
        peo.AllowNone = permitirNenhum;
        var r = s.Ed.GetEntity(peo);
        if (r.Status != PromptStatus.OK) return null;
        return (Alignment)s.Tr.GetObject(r.ObjectId, OpenMode.ForRead);
    }

    public static ProfileView? PedirProfileView(Sessao s)
    {
        var peo = new PromptEntityOptions("\nSelecione o profile view (grade do perfil): ");
        peo.SetRejectMessage("\nSelecione um profile view.");
        peo.AddAllowedClass(typeof(ProfileView), false);
        var r = s.Ed.GetEntity(peo);
        if (r.Status != PromptStatus.OK) return null;
        return (ProfileView)s.Tr.GetObject(r.ObjectId, OpenMode.ForRead);
    }
}
