using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Infra;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJDEVOLVERPIPE — a volta do PRJIMPORTARPIPE (como o reimport do InfoDrainage): grava o dimensionamento do
/// PROJETA na Pipe Network do Civil 3D de onde a rede veio. Diâmetro pela lista de peças (o tamanho igual ou o
/// próximo acima), cotas de fundo dos tubos (interpoladas na posição real das pontas, que ficam na parede da
/// estrutura) e cotas de tampa e de fundo das estruturas. A Pipe Network continua a mesma: perfis, rótulos e
/// tabelas do Civil 3D acompanham. Tudo numa transação: se algo falhar, nada muda; U desfaz o comando.
/// </summary>
public sealed class CmdDevolverPipe
{
    private const double TolPos = 0.01;    // m — estrutura × nó (mesma tolerância do PRJIMPORTARPIPE)
    private const double TolCota = 0.001;  // m
    private const double TolDiam = 0.0005; // m

    [CommandMethod("PRJDEVOLVERPIPE", CommandFlags.Modal)]
    public static void Devolver() => Operacoes.Rodar("PRJDEVOLVERPIPE", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede", r => r.Tipo != TipoRede.Agua);
        if (rede is null) return;
        var net = EscolherPipeNetwork(s, rede);
        if (net is null) return;

        Operacoes.Atualizar(s, rede, redesenhar: false);
        var res = rede.Resultado;
        if (res is null || res.Trechos.Count == 0) { Ctx.Msg("A rede não tem dimensionamento para devolver."); return; }
        if (res.TemErros && s.PedirPalavra("O dimensionamento tem erros. Devolver mesmo assim", new[] { "Sim", "Nao" }, "Nao") != "Sim") return;

        var estruturas = MapearEstruturas(s, net, rede);
        var linhas = new List<string>();
        int tubos = 0, tubosAlterados = 0, semVinculo = 0, forcados = 0, estr = 0, estrAlteradas = 0;

        foreach (ObjectId pid in net.GetPipeIds())
        {
            var pipe = (Pipe)s.Tr.GetObject(pid, OpenMode.ForRead);
            var t = AcharTrecho(rede, pipe, estruturas);
            var rt = t is null ? null : res.Trecho(t.Id);
            var a = t is null ? null : rede.NoPorId(t.NoMontanteId);
            var b = t is null ? null : rede.NoPorId(t.NoJusanteId);
            if (t is null || rt is null || a is null || b is null)
            {
                semVinculo++;
                linhas.Add($"{pipe.Name}: sem trecho correspondente no PROJETA — não alterado");
                continue;
            }
            tubos++;
            if (t.DiametroForcado is not null && t.CotaMontanteForcada is not null && t.CotaJusanteForcada is not null) forcados++;

            pipe.UpgradeOpen();
            double dAntes = pipe.InnerDiameterOrWidth, hAntes = Altura(pipe);
            double ziAntes = Fundo(pipe, pipe.StartPoint), zfAntes = Fundo(pipe, pipe.EndPoint);
            var avisos = new List<string>();

            if (Math.Abs(dAntes - rt.Diametro) > TolDiam)
            {
                var manter = pipe.HoldOnResizeType;
                pipe.HoldOnResizeType = HoldOnResizeType.Invert;
                double d = Redimensionar(pipe, rt.Diametro);
                pipe.HoldOnResizeType = manter;
                if (d < rt.Diametro - TolDiam)
                    avisos.Add($"a lista de peças não tem tamanho ≥ {rt.Diametro * 1000:0} mm; ficou {d * 1000:0} mm, MENOR que o calculado");
            }
            if (rt.Forma == FormaSecao.Retangular && Math.Abs(Altura(pipe) - rt.Altura) > TolDiam)
                avisos.Add($"galeria: altura {Altura(pipe):0.00} m na peça e {rt.Altura:0.00} m no cálculo — troque o tamanho à mão");
            if (rt.Celulas > 1)
                avisos.Add($"o cálculo pede {rt.Celulas} células ({rt.Secao}); a Pipe Network tem um tubo só");

            double meia = Altura(pipe) / 2;
            double zi = FundoNoTrecho(pipe.StartPoint, a, b, rt), zf = FundoNoTrecho(pipe.EndPoint, a, b, rt);
            if (Math.Abs(Fundo(pipe, pipe.StartPoint) - zi) > TolCota)
                pipe.StartPoint = new Point3d(pipe.StartPoint.X, pipe.StartPoint.Y, zi + meia);
            if (Math.Abs(Fundo(pipe, pipe.EndPoint) - zf) > TolCota)
                pipe.EndPoint = new Point3d(pipe.EndPoint.X, pipe.EndPoint.Y, zf + meia);

            double ziDepois = Fundo(pipe, pipe.StartPoint), zfDepois = Fundo(pipe, pipe.EndPoint);
            if (Math.Abs(pipe.InnerDiameterOrWidth - dAntes) > TolDiam || Math.Abs(ziDepois - ziAntes) > TolCota || Math.Abs(zfDepois - zfAntes) > TolCota)
            {
                tubosAlterados++;
                linhas.Add($"{pipe.Name}: {Secao(dAntes, hAntes)} → {Secao(pipe.InnerDiameterOrWidth, Altura(pipe))}; " +
                           $"fundo {ziAntes:0.000} / {zfAntes:0.000} → {ziDepois:0.000} / {zfDepois:0.000}");
            }
            foreach (var av in avisos) linhas.Add($"{pipe.Name}: [AVISO] {av}");
        }

        // estruturas depois dos tubos: o fundo do poço acompanha o tubo mais baixo já na cota nova
        foreach (var (sid, no) in estruturas)
        {
            var rn = res.No(no.Id);
            if (rn is null) continue;
            estr++;
            var st = (Structure)s.Tr.GetObject(sid, OpenMode.ForWrite);
            double tampaAntes = st.RimElevation, fundoAntes = st.SumpElevation;
            if (double.IsFinite(rn.CotaTampa) && Math.Abs(st.RimElevation - rn.CotaTampa) > TolCota)
                st.RimElevation = rn.CotaTampa;
            if (double.IsFinite(rn.CotaFundo) && Math.Abs(st.SumpElevation - rn.CotaFundo) > TolCota)
            {
                st.ControlSumpBy = StructureControlSumpType.ByElevation;
                st.SumpElevation = rn.CotaFundo;
            }
            if (Math.Abs(st.RimElevation - tampaAntes) > TolCota || Math.Abs(st.SumpElevation - fundoAntes) > TolCota)
            {
                estrAlteradas++;
                linhas.Add($"{st.Name}: tampa {tampaAntes:0.000} → {st.RimElevation:0.000}; fundo {fundoAntes:0.000} → {st.SumpElevation:0.000}");
            }
        }

        s.SalvarEConfirmar();

        Ctx.Msg($"Pipe Network '{net.Name}': {tubosAlterados} de {tubos} tubo(s) e {estrAlteradas} de {estr} estrutura(s) alterados.");
        foreach (var l in linhas.Take(40)) Ctx.Msg("  " + l);
        if (linhas.Count > 40) Ctx.Msg($"  ... e mais {linhas.Count - 40} linha(s).");
        if (semVinculo > 0) Ctx.Msg($"[AVISO] {semVinculo} tubo(s) da Pipe Network sem trecho correspondente no PROJETA — não alterados.");
        if (tubos > 0 && forcados == tubos && tubosAlterados == 0)
            Ctx.Msg("Todos os trechos estão com diâmetro e cotas forçados (vieram da Pipe Network), então o cálculo só verificou. " +
                    "Para redimensionar, importe com 'Nao' ou limpe os forçados na planilha (PRJCALCULAR).");
        if (tubosAlterados + estrAlteradas > 0) Ctx.Msg("U desfaz.");
    });

    private static Network? EscolherPipeNetwork(Sessao s, Rede rede)
    {
        var id = Ctx.IdDeHandle(s.Db, rede.HandleRedeC3d);
        if (!id.IsNull && !id.IsErased && s.Tr.GetObject(id, OpenMode.ForRead) is Network ligada)
        {
            Ctx.Msg($"Pipe Network: {ligada.Name}");
            return ligada;
        }
        var redes = new List<Network>();
        foreach (ObjectId nid in CivilApplication.ActiveDocument.GetPipeNetworkIds())
            if (s.Tr.GetObject(nid, OpenMode.ForRead) is Network n) redes.Add(n);
        if (redes.Count == 0) { Ctx.Msg("Não há Pipe Networks no desenho."); return null; }
        int padrao = Math.Max(0, redes.FindIndex(n => string.Equals(n.Name, rede.Nome, StringComparison.OrdinalIgnoreCase)));
        int i = s.EscolherDaLista($"Pipe Network que recebe o cálculo de '{rede.Nome}'", redes.Select(n => n.Name).ToList(), padrao);
        if (i < 0) return null;
        rede.HandleRedeC3d = redes[i].Handle.ToString();
        return redes[i];
    }

    /// <summary>Estrutura → nó: pelo vínculo gravado no PRJIMPORTARPIPE ou, em rede importada antes dele, pela posição.</summary>
    private static Dictionary<ObjectId, No> MapearEstruturas(Sessao s, Network net, Rede rede)
    {
        var mapa = new Dictionary<ObjectId, No>();
        foreach (ObjectId sid in net.GetStructureIds())
        {
            var st = (Structure)s.Tr.GetObject(sid, OpenMode.ForRead);
            var h = st.Handle.ToString();
            var no = rede.Nos.FirstOrDefault(n => n.HandleEstruturaC3d == h)
                  ?? rede.Nos.FirstOrDefault(n => n.HandleEstruturaC3d is null && Perto(n, st.Position));
            if (no is null) continue;
            no.HandleEstruturaC3d = h;
            mapa[sid] = no;
        }
        return mapa;
    }

    /// <summary>Tubo → trecho: pelo vínculo gravado ou pelos nós das duas pontas (como o PRJIMPORTARPIPE montou).</summary>
    private static Trecho? AcharTrecho(Rede rede, Pipe pipe, Dictionary<ObjectId, No> estruturas)
    {
        var h = pipe.Handle.ToString();
        var t = rede.Trechos.FirstOrDefault(x => x.HandlePipeC3d == h);
        if (t is not null) return t;
        var a = NoDaPonta(rede, estruturas, pipe.StartStructureId, pipe.StartPoint);
        var b = NoDaPonta(rede, estruturas, pipe.EndStructureId, pipe.EndPoint);
        if (a is null || b is null) return null;
        t = rede.Trechos.FirstOrDefault(x => x.HandlePipeC3d is null &&
            ((x.NoMontanteId == a.Id && x.NoJusanteId == b.Id) || (x.NoMontanteId == b.Id && x.NoJusanteId == a.Id)));
        if (t is not null) t.HandlePipeC3d = h;
        return t;
    }

    private static No? NoDaPonta(Rede rede, Dictionary<ObjectId, No> estruturas, ObjectId estrutura, Point3d p) =>
        !estrutura.IsNull && estruturas.TryGetValue(estrutura, out var n) ? n : rede.Nos.FirstOrDefault(k => Perto(k, p));

    private static bool Perto(No n, Point3d p) => Math.Abs(n.X - p.X) < TolPos && Math.Abs(n.Y - p.Y) < TolPos;

    /// <summary>
    /// Troca para o menor tamanho da família com diâmetro/largura ≥ d e devolve o diâmetro final.
    /// ResizeByInnerDiameterOrWidth(.., true) pega o tamanho MAIS PRÓXIMO, que pode ser o de baixo;
    /// subindo o alvo aos poucos, o primeiro tamanho ≥ d que aparece é o próximo acima.
    /// </summary>
    private static double Redimensionar(Pipe pipe, double d)
    {
        double alvo = d;
        for (int i = 0; i < 80; i++)
        {
            pipe.ResizeByInnerDiameterOrWidth(alvo, true);
            double atual = pipe.InnerDiameterOrWidth;
            if (atual >= d - TolDiam) return atual;
            alvo = Math.Max(alvo, atual) + 0.025;
        }
        return pipe.InnerDiameterOrWidth;
    }

    private static double Altura(Pipe p) => p.InnerHeight > 0 ? p.InnerHeight : p.InnerDiameterOrWidth;

    private static double Fundo(Pipe p, Point3d eixo) => eixo.Z - Altura(p) / 2;

    /// <summary>Cota de fundo do cálculo (de centro a centro de PV) na posição da ponta do tubo.</summary>
    private static double FundoNoTrecho(Point3d p, No a, No b, ResultadoTrecho rt)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
        double u = l2 > 0 ? Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2, 0, 1) : 0;
        return rt.CotaFundoMontante + (rt.CotaFundoJusante - rt.CotaFundoMontante) * u;
    }

    private static string Secao(double largura, double altura) =>
        Math.Abs(largura - altura) < TolDiam ? $"Ø{largura * 1000:0}" : $"{largura:0.00}x{altura:0.00}";
}
