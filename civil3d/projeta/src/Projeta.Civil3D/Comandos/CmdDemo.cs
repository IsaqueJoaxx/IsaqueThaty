using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Longitudinais;
using Projeta.Core.Modelador;
using Projeta.Core.Redes;

[assembly: CommandClass(typeof(Projeta.Civil3D.Comandos.CmdDemo))]

namespace Projeta.Civil3D.Comandos;

/// <summary>
/// PRJDEMO — monta no desenho uma demonstração completa (sem superfície e sem janelas): rede de drenagem com bacias,
/// rede de esgoto, todos os dispositivos da biblioteca e todas as seções-tipo como dispositivos longitudinais.
/// Também serve de autoteste: compara o volume dos sólidos gerados com a estimativa do núcleo.
/// </summary>
public sealed class CmdDemo
{
    [CommandMethod("PRJDEMO", CommandFlags.Modal)]
    public static void Demo() => Operacoes.Rodar("PRJDEMO", s =>
    {
        var pr = s.Ed.GetPoint(new PromptPointOptions("\nPonto base da demonstração <0,0>: ") { AllowNone = true });
        if (pr.Status != PromptStatus.OK && pr.Status != PromptStatus.None) return;
        var b = pr.Status == PromptStatus.OK ? pr.Value : Point3d.Origin;
        int falhas = 0;
        var ms = Ctx.EspacoModelo(s.Tr, s.Db);

        // ---------------- rede de drenagem
        var dren = Operacoes.NovaRede(s.Projeto, "Demo drenagem", TipoRede.Drenagem);
        var pts = new (string n, double x, double y, double z, TipoNo t)[]
        {
            ("BL1", 2, 6, 850.05, TipoNo.BocaDeLobo), ("A", 0, 0, 850.00, TipoNo.PV), ("B", 60, 0, 849.40, TipoNo.PV), ("C", 120, 0, 848.60, TipoNo.PV),
            ("D", 180, 0, 847.70, TipoNo.PV), ("E", 240, 0, 846.50, TipoNo.Exutorio), ("R1", 60, 70, 850.20, TipoNo.PV), ("R2", 120, 50, 849.30, TipoNo.PV),
        };
        foreach (var p in pts) { var n = dren.NovoNo(p.t, b.X + p.x, b.Y + p.y); n.Nome = p.n; n.CotaTampaForcada = p.z; }
        void T(Rede r, string a, string c) => r.NovoTrecho(r.NoPorNome(a)!, r.NoPorNome(c)!);
        T(dren, "BL1", "A"); T(dren, "A", "B"); T(dren, "B", "C"); T(dren, "C", "D"); T(dren, "D", "E"); T(dren, "R1", "R2"); T(dren, "R2", "C");
        dren.NoPorNome("D")!.DispositivoId = "PV_RETANGULAR";
        Ctx.GarantirLayer(s.Tr, s.Db, DesenhoRede.Layer(dren, "BACIA"), 3);
        void Bacia(string no, double x0, double y0, double w, double h, double c)
        {
            var pl = new Polyline();
            pl.SetDatabaseDefaults();
            pl.AddVertexAt(0, new Point2d(b.X + x0, b.Y + y0), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(b.X + x0 + w, b.Y + y0), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(b.X + x0 + w, b.Y + y0 + h), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(b.X + x0, b.Y + y0 + h), 0, 0, 0);
            pl.Closed = true;
            pl.Layer = DesenhoRede.Layer(dren, "BACIA");
            Ctx.Adicionar(s.Tr, ms, pl);
            var bac = new Bacia { Nome = "B-" + no, NoId = dren.NoPorNome(no)!.Id, C = c, HandleContorno = Ctx.HandleDe(pl.ObjectId), AreaHa = w * h / 10000.0 };
            bac.Contorno = DesenhoRede.PontosCurva(pl);
            dren.Bacias.Add(bac);
        }
        Bacia("BL1", -40, 10, 60, 100, 0.75); Bacia("A", -40, -110, 60, 100, 0.70); Bacia("B", 25, -110, 60, 100, 0.65);
        Bacia("C", 90, -110, 60, 100, 0.70); Bacia("D", 155, -110, 80, 250, 0.60); Bacia("R1", 25, 60, 60, 120, 0.70); Bacia("R2", 90, 10, 60, 110, 0.70);
        Operacoes.Atualizar(s, dren, sincronizar: false);
        var resD = dren.Resultado!;
        Ctx.Msg($"[DEMO] Drenagem: {resD.Trechos.Count} trechos, {resD.Trechos.Count(t => t.Erros.Count > 0)} com erro; seções: {string.Join(", ", resD.Trechos.Select(t => t.Secao).Distinct())}");
        if (resD.TemErros) falhas++;
        int nBlocos = dren.Nos.Count(n => !Ctx.IdDeHandle(s.Db, n.HandleBloco).IsNull);
        int nSolidosTubo = dren.Trechos.Count(t => !Ctx.IdDeHandle(s.Db, t.HandleSolido).IsNull);
        Ctx.Msg($"[DEMO] Drenagem desenhada: {nBlocos}/{dren.Nos.Count} blocos de nós, {nSolidosTubo}/{dren.Trechos.Count} sólidos de tubos.");
        if (nBlocos < dren.Nos.Count || nSolidosTubo < dren.Trechos.Count) falhas++;
        TabelasDesenho.Inserir(s, Core.Relatorios.GeradorTabelas.Dimensionamento(dren, resD), new Point3d(b.X, b.Y - 140, 0), 0.8, DesenhoRede.Layer(dren, "TABELA"));

        // ---------------- rede de esgoto
        var esg = Operacoes.NovaRede(s.Projeto, "Demo esgoto", TipoRede.Esgoto);
        esg.Esgoto.PopulacaoInicial = 800; esg.Esgoto.PopulacaoFinal = 1300;
        for (int i = 0; i < 5; i++) { var n = esg.NovoNo(i == 4 ? TipoNo.Exutorio : TipoNo.PV, b.X + i * 60, b.Y - 30); n.CotaTampaForcada = 849.0 - i * 0.4; }
        for (int i = 0; i < 4; i++) esg.NovoTrecho(esg.Nos[i], esg.Nos[i + 1]);
        Operacoes.Atualizar(s, esg, sincronizar: false);
        Ctx.Msg($"[DEMO] Esgoto: {esg.Resultado!.Trechos.Count} trechos, DN {string.Join("/", esg.Resultado.Trechos.Select(t => (t.Diametro * 1000).ToString("0")).Distinct())}; erros: {esg.Resultado.Trechos.Count(t => t.Erros.Count > 0)}");
        var interf = Core.Dimensionamento.VerificadorInterferencias.Verificar(new[] { dren, esg }, 0.2);
        Ctx.Msg($"[DEMO] Interferências drenagem x esgoto: {interf.Count}");

        // ---------------- dispositivos da biblioteca (volume real x estimado)
        int k = 0;
        foreach (var def in Sessao.Bib.Dispositivos.Values.OrderBy(d => d.Id))
        {
            var mod = AvaliadorDispositivo.Avaliar(def, new Dictionary<string, double> { ["Profundidade"] = 2.5, ["DiametroMaximo"] = 0.8 });
            if (!mod.Ok) { Ctx.Msg($"[DEMO] {def.Id}: erro de avaliação — {string.Join("; ", mod.Mensagens)}"); falhas++; continue; }
            var ins = CmdModelador.InserirDispositivo(s, def, mod, new Point3d(b.X + k * 8, b.Y + 220, 845), 0);
            double est = mod.Saida.Sum(x => x.VolumeEstimado());
            double dif = est > 0 ? Math.Abs(ins.VolumeReal - est) / est : 0;
            bool ok = ins.VolumeReal > 0 && ins.Erros.Count == 0;
            Ctx.Msg($"[DEMO] {def.Id,-20} volume do sólido {Num.F(ins.VolumeReal, 3)} m³; estimado {Num.F(est, 3)} m³ ({Num.F(dif * 100, 1)}%){(ok ? "" : " — FALHA " + string.Join("; ", ins.Erros))}");
            if (!ok) falhas++;
            k++;
        }

        // ---------------- seções-tipo como longitudinais ao longo de um eixo em curva
        int j = 0;
        foreach (var sec in Sessao.Bib.Secoes.Values.OrderBy(x => x.Id))
        {
            var d = new DispositivoLongitudinal { Nome = $"DEMO-{sec.Id}", SecaoId = sec.Id, NomeAlinhamento = "(eixo sintético)", EstacaInicial = 0, EstacaFinal = 40 };
            var am = new List<AmostraLongitudinal>();
            double y0 = b.Y + 260 + j * 12, r = 80;
            for (int i = 0; i <= 20; i++)
            {
                double ang = i * 2.0 / r; // arco de 40 m em raio 80 m
                double x = b.X + r * Math.Sin(ang), y = y0 + r * (1 - Math.Cos(ang));
                am.Add(new AmostraLongitudinal(i * 2.0, x, y, 846 - i * 2.0 * 0.01, Math.Cos(ang), Math.Sin(ang)));
            }
            var erros = DesenhoLongitudinal.GerarComAmostras(s, d, am);
            s.Projeto.Longitudinais.Add(d);
            double volReal = 0;
            foreach (var h in d.HandlesDesenho)
                if (Ctx.Abrir<Solid3d>(s.Tr, s.Db, h) is Solid3d so) volReal += so.MassProperties.Volume;
            double volCalc = d.Resultado?.VolumeConcreto ?? 0;
            bool ok = volReal > 0 && erros.Count == 0;
            Ctx.Msg($"[DEMO] {sec.Id,-20} sólido {Num.F(volReal, 3)} m³; seção×comprimento {Num.F(volCalc, 3)} m³{(ok ? "" : " — FALHA " + string.Join("; ", erros))}");
            if (!ok) falhas++;
            j++;
        }

        s.SalvarEConfirmar();
        // releitura do projeto gravado no DWG
        using (var s2 = Sessao.Abrir())
        {
            bool ok = s2.Projeto.Redes.Count >= 2 && s2.Projeto.Redes.Any(r => r.Nome == dren.Nome && r.Resultado is not null);
            Ctx.Msg($"[DEMO] Projeto relido do DWG: {s2.Projeto.Redes.Count} redes, {s2.Projeto.Longitudinais.Count} longitudinais — {(ok ? "OK" : "FALHA")}");
            if (!ok) falhas++;
        }
        Ctx.Msg(falhas == 0 ? "[DEMO] CONCLUÍDO SEM FALHAS." : $"[DEMO] CONCLUÍDO COM {falhas} FALHA(S).");
    });
}
