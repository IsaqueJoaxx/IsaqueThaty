using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Projeta.Giro.Core.Cinematica;
using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Verificacao;

namespace Projeta.Giro.Infra;

/// <summary>Desenha varreduras, veículos e conflitos no espaço do modelo.</summary>
internal static class Desenho
{
    public const string LTrajetoria = "GIRO-TRAJETORIA";
    public const string LEnvoltoria = "GIRO-ENVOLTORIA";
    public const string LRodas = "GIRO-RODAS";
    public const string LVeiculo = "GIRO-VEICULO";
    public const string LTexto = "GIRO-TEXTO";
    public const string LConflito = "GIRO-CONFLITO";
    public const string LFolga = "GIRO-FOLGA";

    public static void GarantirLayers(Transaction tr, Database db)
    {
        Ctx.GarantirLayer(tr, db, LTrajetoria, 6);
        Ctx.GarantirLayer(tr, db, LEnvoltoria, 4, LineWeight.LineWeight035);
        Ctx.GarantirLayer(tr, db, LRodas, 3);
        Ctx.GarantirLayer(tr, db, LVeiculo, 5);
        Ctx.GarantirLayer(tr, db, LTexto, 7);
        Ctx.GarantirLayer(tr, db, LConflito, 1, LineWeight.LineWeight050);
        Ctx.GarantirLayer(tr, db, LFolga, 2, LineWeight.LineWeight035);
    }

    private static ObjectId Add(Transaction tr, BlockTableRecord ms, Entity e, string layer, string marca, List<ObjectId> ids)
    {
        e.Layer = layer;
        Armazenamento.Marcar(e, marca);
        var id = Ctx.Adicionar(tr, ms, e);
        ids.Add(id);
        return id;
    }

    /// <summary>Contornos do veículo numa pose (polilinhas fechadas + linhas), sem adicionar ao desenho.</summary>
    public static List<Entity> EntidadesVeiculo(Core.Veiculos.Veiculo v, Pose p)
    {
        var (f, l) = Corpo.Desenho(v, p);
        var r = new List<Entity>();
        foreach (var x in f) r.Add(Ctx.Pl(x, true));
        foreach (var x in l) r.Add(Ctx.Pl(x, false));
        return r;
    }

    /// <summary>Desenha a varredura completa, grava a manobra e agrupa tudo. Retorna o número de entidades.</summary>
    public static int Varredura(Transaction tr, Database db, ResultadoVarredura r, bool desenharRodas)
    {
        var m = r.Manobra;
        var v = m.Veiculo;
        GarantirLayers(tr, db);
        Armazenamento.RegistrarApp(tr, db);
        var ms = Ctx.EspacoModelo(tr, db);
        var ids = new List<ObjectId>();
        string marca = "M:" + m.Id;

        foreach (var e in r.Envoltoria) Add(tr, ms, Ctx.Pl(e, true), LEnvoltoria, marca, ids);
        if (desenharRodas) foreach (var e in r.Rodas) Add(tr, ms, Ctx.Pl(e, true), LRodas, marca, ids);
        Add(tr, ms, Ctx.Pl(Geo.Simplificar(r.CaminhoDianteiro, 0.002), false), LTrajetoria, marca, ids);

        // trechos com esterçamento acima do máximo
        double lim = v.AnguloMaximo + Num.Rad(0.5);
        var trecho = new List<V2>();
        void Fecha() { if (trecho.Count >= 2) Add(tr, ms, Ctx.Pl(trecho.ToList(), false), LConflito, marca, ids); trecho.Clear(); }
        foreach (var p in r.Poses) { if (Math.Abs(p.Delta) > lim) trecho.Add(p.EixoDianteiro); else Fecha(); }
        Fecha();

        // veículos: início, intervalos e fim
        var posicoes = new List<double> { 0 };
        if (m.IntervaloVeiculos > 0.5) for (double s = m.IntervaloVeiculos; s < r.Comprimento - m.IntervaloVeiculos * 0.3; s += m.IntervaloVeiculos) posicoes.Add(s);
        if (r.Comprimento > 0.01) posicoes.Add(r.Comprimento);
        foreach (var s in posicoes)
            foreach (var e in EntidadesVeiculo(v, r.PoseEm(s))) Add(tr, ms, e, LVeiculo, marca, ids);

        // rótulo junto ao início
        var p0 = r.Poses[0];
        double h = Math.Max(0.25, Math.Min(0.5, v.Largura * 0.18));
        var posTexto = p0.U[0].Ponto(v.Motriz.XFrente + 0.6, 0);
        double rot = p0.Rumo;
        if (Math.Cos(rot) < 0) rot += Math.PI;
        var txt = Ctx.Texto($"{v.Id} — {v.Nome}\n{Num.F(m.Velocidade, 0)} km/h · {Num.F(r.Comprimento, 1)} m", Ctx.P(posTexto), h, rot,
            Math.Cos(p0.Rumo) >= 0 ? AttachmentPoint.MiddleLeft : AttachmentPoint.MiddleRight);
        Add(tr, ms, txt, LTexto, marca, ids);

        Armazenamento.Gravar(tr, db, m);
        Armazenamento.Agrupar(tr, db, Armazenamento.NomeGrupo("GIRO", m.Id), $"PROJETA Giro {v.Id} {m.Modo}", ids);
        return ids.Count;
    }

    /// <summary>Apaga a varredura (entidades e grupo) — a manobra gravada continua, para ser redesenhada.</summary>
    public static int ApagarVarredura(Transaction tr, Database db, string id) =>
        Armazenamento.ApagarMarcadas(tr, db, "M:" + id, Armazenamento.NomeGrupo("GIRO", id));

    public static int ApagarVerificacao(Transaction tr, Database db, string id) =>
        Armazenamento.ApagarMarcadas(tr, db, "V:" + id, Armazenamento.NomeGrupo("GIROV", id));

    public static int Conflitos(Transaction tr, Database db, string id, ResultadoConflito c, double folga)
    {
        GarantirLayers(tr, db);
        Armazenamento.RegistrarApp(tr, db);
        var ms = Ctx.EspacoModelo(tr, db);
        var ids = new List<ObjectId>();
        string marca = "V:" + id;
        foreach (var p in c.Invasoes)
        {
            Add(tr, ms, Ctx.Pl(p, false), LConflito, marca, ids);
            var meio = Geo.PontoNaDistancia(p, Geo.Acumulado(p), Geo.Comprimento(p) / 2).P;
            Add(tr, ms, new Circle(Ctx.P(meio), Vector3d.ZAxis, 1.0), LConflito, marca, ids);
        }
        foreach (var a in c.AreasInvadidas)
        {
            var plId = Add(tr, ms, Ctx.Pl(a, true), LConflito, marca, ids);
            try
            {
                var hc = new Hatch();
                hc.SetDatabaseDefaults();
                hc.Layer = LConflito;
                Armazenamento.Marcar(hc, marca);
                var hid = Ctx.Adicionar(tr, ms, hc);
                ids.Add(hid);
                hc.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                hc.Transparency = new Transparency((byte)128);
                hc.AppendLoop(HatchLoopTypes.External, new ObjectIdCollection { plId });
                hc.EvaluateHatch(true);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception) { }
        }
        foreach (var p in c.NaFolga) Add(tr, ms, Ctx.Pl(p, false), LFolga, marca, ids);
        if (ids.Count > 0) Armazenamento.Agrupar(tr, db, Armazenamento.NomeGrupo("GIROV", id), "PROJETA Giro verificação", ids);
        return ids.Count;
    }
}
