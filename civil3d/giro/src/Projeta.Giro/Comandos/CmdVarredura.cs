using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Projeta.Giro.Core.Cinematica;
using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Veiculos;
using Projeta.Giro.Infra;

namespace Projeta.Giro.Comandos;

/// <summary>Caminho de varredura: escolher veículo, seguir trajetória, arco automático (condução), gabarito e atualização.</summary>
public sealed class CmdVarredura
{
    [CommandMethod("PROJETA_GIRO", "GIROVEICULO", CommandFlags.Modal)]
    public void Veiculo() => Operacoes.Executar("GIROVEICULO", () => Operacoes.EscolherVeiculo());

    // ------------------------------------------------------------------ seguir trajetória

    [CommandMethod("PROJETA_GIRO", "GIROSEGUIR", CommandFlags.Modal)]
    public void Seguir() => Operacoes.Executar("GIROSEGUIR", () =>
    {
        var cfg = Config.Atual;
        var v = cfg.Veiculo();
        Ctx.Msg("Veículo: " + Operacoes.Resumo(v));
        var op = new PromptEntityOptions("\nCaminho do eixo dianteiro (polilinha, linha, arco, alinhamento, feature line) — clique perto da ponta de saída [Veiculo/Velocidade]: ");
        op.Keywords.Add("Veiculo");
        op.Keywords.Add("Velocidade");
        op.AppendKeywordsToMessage = false;
        PromptEntityResult r;
        while (true)
        {
            r = Ctx.Ed.GetEntity(op);
            if (r.Status == PromptStatus.Keyword)
            {
                if (r.StringResult == "Veiculo") v = Operacoes.EscolherVeiculo() ?? v;
                else Operacoes.PerguntarVelocidade();
                continue;
            }
            if (r.Status != PromptStatus.OK) return;
            break;
        }
        Manobra? m;
        using (var tr = Ctx.Db.TransactionManager.StartTransaction())
        {
            var e = (Entity)tr.GetObject(r.ObjectId, OpenMode.ForRead);
            m = MontarSeguir(e, Ctx.V(Ctx.Wcs(r.PickedPoint)), v, null);
            tr.Commit();
        }
        if (m is null) return;
        Operacoes.Gerar(m);
    });

    /// <summary>Monta a manobra "seguir" a partir de uma curva. inverter = null decide pela ponta mais próxima do ponto clicado.</summary>
    internal static Manobra? MontarSeguir(Entity e, V2? clique, Veiculo v, bool? inverter, Manobra? base0 = null)
    {
        var pts = Curvas.Pontos(e, 0.0005, 0.10, out bool fechada, out string tipo);
        if (pts is null || pts.Count < 2) { Ctx.Msg($"não foi possível ler o caminho ({tipo})."); return null; }
        if (fechada) pts.Add(pts[0]);
        bool inv = inverter ?? (clique is V2 c && c.Dist(pts[^1]) < c.Dist(pts[0]));
        if (inv) pts.Reverse();
        double comp = Geo.Comprimento(pts);
        if (comp < 1) { Ctx.Msg("caminho curto demais."); return null; }
        int k = 1;
        while (k < pts.Count - 1 && pts[k].Dist(pts[0]) < 0.2) k++;
        var cfg = Config.Atual;
        var m = base0 ?? new Manobra { Modo = "seguir", Velocidade = cfg.Velocidade, IntervaloVeiculos = cfg.IntervaloVeiculos };
        m.Veiculo = v;
        m.Inicio = pts[0];
        m.RumoInicial = (pts[k] - pts[0]).Angulo;
        m.Trechos = new() { new Trecho { Pontos = Geo.Arredondar(pts) } };
        m.HandleCaminho = e.Handle.ToString();
        m.CaminhoInvertido = inv;
        m.Nome = tipo;
        Ctx.Msg($"Seguindo {tipo} de {Num.F(comp, 1)} m{(inv ? " (no sentido inverso ao do desenho)" : "")}.");
        return m;
    }

    // ------------------------------------------------------------------ arco automático / condução

    [CommandMethod("PROJETA_GIRO", "GIROARCO", CommandFlags.Modal)]
    public void Arco() => Operacoes.Executar("GIROARCO", () =>
    {
        var cfg = Config.Atual;
        var v = cfg.Veiculo();
        Ctx.Msg("Veículo: " + Operacoes.Resumo(v));
        Point3d p0u;
        while (true)
        {
            var o = new PromptPointOptions("\nPosição inicial do eixo dianteiro [Veiculo/Velocidade]: ");
            o.Keywords.Add("Veiculo");
            o.Keywords.Add("Velocidade");
            o.AppendKeywordsToMessage = false;
            var r = Ctx.Ed.GetPoint(o);
            if (r.Status == PromptStatus.Keyword)
            {
                if (r.StringResult == "Veiculo") v = Operacoes.EscolherVeiculo() ?? v;
                else Operacoes.PerguntarVelocidade();
                continue;
            }
            if (r.Status != PromptStatus.OK) return;
            p0u = r.Value;
            break;
        }
        var p1u = Operacoes.Ponto("Direção do veículo (ponto à frente): ", p0u);
        if (p1u is null) return;
        var p0 = Ctx.V(Ctx.Wcs(p0u));
        double dir = Operacoes.Direcao(p0u, p1u.Value);
        var m = new Manobra { Modo = "arco", Veiculo = v, Velocidade = cfg.Velocidade, Inicio = p0, RumoInicial = dir, IntervaloVeiculos = cfg.IntervaloVeiculos };
        var op = new OpcoesConducao { Velocidade = cfg.Velocidade };
        if (!Conduzir(m, op)) return;
        m.Raio = op.Raio;
        Operacoes.Gerar(m);
    });

    /// <summary>Laço interativo de condução: cada ponto clicado vira um trecho (giro com transição + reta até o ponto).</summary>
    internal static bool Conduzir(Manobra m, OpcoesConducao op)
    {
        var v = m.Veiculo;
        double l = v.Motriz.EntreEixos;
        var U = Seguidor.Alinhado(v, m.Inicio, m.RumoInicial);
        var pilha = new Stack<PoseUnidade[]>();
        bool re = false;
        var feitos = new List<List<V2>>();
        Ctx.Msg($"Raio mínimo do eixo dianteiro: {Num.F(Autoconducao.RaioMinimo(v, false))} m. Clique os pontos por onde o {(re ? "eixo traseiro" : "eixo dianteiro")} deve passar; Enter termina.");
        while (true)
        {
            V2 pRef = re ? U[0].Ponto(-l) : U[0].Guia;
            double dRef = re ? U[0].Rumo + Math.PI : U[0].Rumo;
            string msg = $"\nPróximo ponto do {(re ? "eixo traseiro (ré)" : "eixo dianteiro")} [{(re ? "Frente" : "Re")}/Raio/Desfazer/Fim] <Fim>: ";
            var kws = new[] { re ? "Frente" : "Re", "Raio", "Desfazer", "Fim" };
            PromptResult res;
            Point3d alvoUcs;
            if (Ctx.ComInterface)
            {
                var jig = new JigConducao(v, U, pRef, dRef, re, op, feitos, msg, kws);
                res = Ctx.Ed.Drag(jig);
                alvoUcs = jig.Atual;
            }
            else
            {
                var o = new PromptPointOptions(msg) { AllowNone = true, AppendKeywordsToMessage = false };
                foreach (var k in kws) o.Keywords.Add(k);
                var pr = Ctx.Ed.GetPoint(o);
                res = pr;
                alvoUcs = pr.Status == PromptStatus.OK ? pr.Value : Point3d.Origin;
            }
            if (res.Status == PromptStatus.Cancel || res.Status == PromptStatus.Error) return m.Trechos.Count > 0 && Confirmar("Gerar a varredura com os trechos já definidos?");
            if (res.Status == PromptStatus.None) break;
            if (res.Status == PromptStatus.Keyword)
            {
                switch (res.StringResult)
                {
                    case "Fim": goto fim;
                    case "Re":
                        if (v.Articulado) Ctx.Msg("marcha à ré só para veículos rígidos nesta versão.");
                        else re = true;
                        break;
                    case "Frente": re = false; break;
                    case "Raio":
                    {
                        double rmin = Autoconducao.RaioMinimo(v, re);
                        var o = new PromptDistanceOptions($"\nRaio do {(re ? "eixo traseiro" : "eixo dianteiro")} (mínimo {Num.F(rmin)} m; Enter = mínimo): ")
                        { AllowNone = true, AllowNegative = false, AllowZero = false };
                        var rr = Ctx.Ed.GetDistance(o);
                        if (rr.Status == PromptStatus.None) op.Raio = null;
                        else if (rr.Status == PromptStatus.OK) op.Raio = rr.Value < rmin ? null : rr.Value;
                        Ctx.Msg(op.Raio is double rv ? $"Raio de giro: {Num.F(rv)} m." : "Raio de giro: mínimo do veículo.");
                        break;
                    }
                    case "Desfazer":
                        if (m.Trechos.Count == 0) { Ctx.Msg("nada para desfazer."); break; }
                        m.Trechos.RemoveAt(m.Trechos.Count - 1);
                        feitos.RemoveAt(feitos.Count - 1);
                        U = pilha.Pop();
                        break;
                }
                continue;
            }
            var alvo = Ctx.V(Ctx.Wcs(alvoUcs));
            var t = Autoconducao.Ate(v, pRef, dRef, alvo, re, op);
            if (!t.Ok) { Ctx.Msg(t.Erro!); continue; }
            var trecho = new Trecho { Re = re, Pontos = Geo.Arredondar(t.Pontos) };
            var poses = Varredura.Percorrer(v, U, trecho);
            pilha.Push(U);
            U = poses[^1].U;
            m.Trechos.Add(trecho);
            feitos.Add(t.Pontos);
        }
        fim:
        if (m.Trechos.Count == 0) { Ctx.Msg("nenhum trecho definido."); return false; }
        return true;
    }

    private static bool Confirmar(string pergunta)
    {
        var o = new PromptKeywordOptions("\n" + pergunta);
        o.Keywords.Add("Sim");
        o.Keywords.Add("Nao");
        o.Keywords.Default = "Sim";
        o.AllowNone = true;
        var r = Ctx.Ed.GetKeywords(o);
        return r.Status == PromptStatus.None || (r.Status == PromptStatus.OK && r.StringResult == "Sim");
    }

    /// <summary>Prévia da condução: caminho até o cursor, envoltória aproximada e o veículo na posição final.</summary>
    private sealed class JigConducao : DrawJig
    {
        private readonly Veiculo _v;
        private readonly PoseUnidade[] _u;
        private readonly V2 _ref;
        private readonly double _dir;
        private readonly bool _re;
        private readonly OpcoesConducao _op;
        private readonly List<List<V2>> _feitos;
        private readonly string _msg;
        private readonly string[] _kws;
        public Point3d Atual { get; private set; }
        private (List<V2>? Caminho, List<List<V2>> Env, List<Entity> Veic, string? Erro) _previa = (null, new(), new(), null);

        public JigConducao(Veiculo v, PoseUnidade[] u, V2 pRef, double dir, bool re, OpcoesConducao op, List<List<V2>> feitos, string msg, string[] kws)
        {
            _v = v; _u = u; _ref = pRef; _dir = dir; _re = re; _op = op; _feitos = feitos; _msg = msg; _kws = kws;
            Atual = Point3d.Origin;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var o = new JigPromptPointOptions(_msg)
            {
                UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.NullResponseAccepted | UserInputControls.NoNegativeResponseAccepted,
                AppendKeywordsToMessage = false,
            };
            foreach (var k in _kws) o.Keywords.Add(k);
            var r = prompts.AcquirePoint(o);
            if (r.Status == PromptStatus.Keyword) return SamplerStatus.OK;
            if (r.Status != PromptStatus.OK) return SamplerStatus.Cancel;
            if (r.Value.DistanceTo(Atual) < 1e-4) return SamplerStatus.NoChange;
            Atual = r.Value;
            Calcular();
            return SamplerStatus.OK;
        }

        private void Calcular()
        {
            foreach (var e in _previa.Veic) e.Dispose();
            var alvo = Ctx.V(Ctx.Wcs(Atual));
            var t = Autoconducao.Ate(_v, _ref, _dir, alvo, _re, _op);
            if (!t.Ok) { _previa = (null, new(), new(), t.Erro); return; }
            var poses = Varredura.Percorrer(_v, _u, new Trecho { Re = _re, Pontos = t.Pontos }, 0.2);
            var env = poses.Count > 1 ? Varredura.Envoltorias(_v, poses, 0.6, comRodas: false).Corpo : new List<List<V2>>();
            _previa = (t.Pontos, env, Desenho.EntidadesVeiculo(_v, poses[^1]), null);
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            var g = draw.Geometry;
            var tr = draw.SubEntityTraits;
            tr.Color = 8;
            foreach (var f in _feitos) Linha(g, f);
            if (_previa.Caminho is null)
            {
                tr.Color = 1;
                Linha(g, new List<V2> { _ref, Ctx.V(Ctx.Wcs(Atual)) });
                return true;
            }
            tr.Color = 4;
            foreach (var e in _previa.Env) Linha(g, e.Append(e[0]).ToList());
            tr.Color = 6;
            Linha(g, _previa.Caminho);
            foreach (var e in _previa.Veic) g.Draw(e);
            return true;
        }

        private static void Linha(Geometry g, IReadOnlyList<V2> pts)
        {
            if (pts.Count < 2) return;
            var col = new Point3dCollection();
            foreach (var p in pts) col.Add(new Point3d(p.X, p.Y, 0));
            g.Polyline(col, Vector3d.ZAxis, IntPtr.Zero);
        }
    }

    // ------------------------------------------------------------------ gabarito de giro

    [CommandMethod("PROJETA_GIRO", "GIROGABARITO", CommandFlags.Modal)]
    public void Gabarito() => Operacoes.Executar("GIROGABARITO", () =>
    {
        var cfg = Config.Atual;
        var v = cfg.Veiculo();
        Ctx.Msg("Veículo: " + Operacoes.Resumo(v));
        var p0u = Operacoes.Ponto("Posição inicial do eixo dianteiro: ");
        if (p0u is null) return;
        var p1u = Operacoes.Ponto("Direção do veículo (ponto à frente): ", p0u);
        if (p1u is null) return;
        var ra = Ctx.Ed.GetDouble(new PromptDoubleOptions("\nÂngulo de giro em graus (30, 60, 90, 120, 150, 180...) <90>: ")
        { AllowNone = true, AllowNegative = false, AllowZero = false, DefaultValue = 90, UseDefaultValue = true });
        if (ra.Status != PromptStatus.OK) return;
        double ang = Math.Min(ra.Value, 359);
        var ko = new PromptKeywordOptions("\nLado do giro");
        ko.Keywords.Add("Esquerda");
        ko.Keywords.Add("Direita");
        ko.Keywords.Default = "Direita";
        ko.AllowNone = true;
        var rk = Ctx.Ed.GetKeywords(ko);
        if (rk.Status != PromptStatus.OK && rk.Status != PromptStatus.None) return;
        bool esq = rk.StringResult == "Esquerda";
        var p0 = Ctx.V(Ctx.Wcs(p0u.Value));
        double dir = Operacoes.Direcao(p0u.Value, p1u.Value);
        var op = new OpcoesConducao { Velocidade = cfg.Velocidade };
        var pts = Autoconducao.Gabarito(v, p0, dir, ang, esq, op);
        var m = new Manobra
        {
            Modo = "gabarito", Nome = $"giro de {Num.F(ang, 0)}° à {(esq ? "esquerda" : "direita")}", Veiculo = v, Velocidade = cfg.Velocidade,
            Inicio = p0, RumoInicial = dir, Trechos = { new Trecho { Pontos = Geo.Arredondar(pts) } }, IntervaloVeiculos = cfg.IntervaloVeiculos,
        };
        Operacoes.Gerar(m);
        var e = v.Estatico();
        Ctx.Msg($"Giro estático no batente: roda externa dianteira {Num.F(e.RodaExternaDianteira)} m, balanço dianteiro {Num.F(e.BalancoDianteiro)} m, roda interna traseira {Num.F(e.RodaInternaTraseira)} m" +
                (v.RefRaioRodaInterna is double ri ? $" (referência: {Num.F(ri)} m)." : "."));
    });

    // ------------------------------------------------------------------ atualizar / trocar veículo

    [CommandMethod("PROJETA_GIRO", "GIROATUALIZAR", CommandFlags.Modal)]
    public void Atualizar() => Operacoes.Executar("GIROATUALIZAR", () =>
    {
        var sel = Operacoes.SelecionarManobra("Selecione a varredura a atualizar: ");
        if (sel is null) return;
        var m = sel.Value.M;
        var cfg = Config.Atual;
        while (true)
        {
            var o = new PromptKeywordOptions($"\nVeículo {m.Veiculo.Id}, {Num.F(m.Velocidade, 0)} km/h, veículos a cada {(m.IntervaloVeiculos > 0 ? Num.F(m.IntervaloVeiculos, 0) + " m" : "início/fim")}. Alterar [Veiculo/Velocidade/Intervalo] <atualizar>: ");
            o.Keywords.Add("Veiculo");
            o.Keywords.Add("Velocidade");
            o.Keywords.Add("Intervalo");
            o.AllowNone = true;
            o.AppendKeywordsToMessage = false;
            var r = Ctx.Ed.GetKeywords(o);
            if (r.Status == PromptStatus.None) break;
            if (r.Status != PromptStatus.OK) return;
            if (r.StringResult == "Veiculo")
            {
                var nv = Operacoes.EscolherVeiculo();
                if (nv is not null)
                {
                    if (nv.Articulado && m.Trechos.Any(t => t.Re)) Ctx.Msg("aviso: esta manobra tem trechos em ré; com reboque eles serão ignorados.");
                    m.Veiculo = nv;
                }
            }
            else if (r.StringResult == "Velocidade")
            {
                var d = Ctx.Ed.GetDouble(new PromptDoubleOptions($"\nVelocidade (km/h) <{Num.F(m.Velocidade, 0)}>: ") { AllowNone = true, AllowNegative = false, AllowZero = false });
                if (d.Status == PromptStatus.OK) m.Velocidade = d.Value;
            }
            else
            {
                var d = Ctx.Ed.GetDouble(new PromptDoubleOptions($"\nDesenhar o veículo a cada quantos metros (0 = só início e fim) <{Num.F(m.IntervaloVeiculos, 0)}>: ") { AllowNone = true, AllowNegative = false, AllowZero = true });
                if (d.Status == PromptStatus.OK) m.IntervaloVeiculos = d.Value;
            }
        }
        if (m.Modo == "seguir" && m.HandleCaminho is not null)
        {
            using var tr = Ctx.Db.TransactionManager.StartTransaction();
            var id = Ctx.IdDeHandle(Ctx.Db, m.HandleCaminho);
            if (id.IsNull) Ctx.Msg("o caminho original foi apagado — mantida a trajetória gravada.");
            else
            {
                var e = (Entity)tr.GetObject(id, OpenMode.ForRead);
                var nova = MontarSeguir(e, null, m.Veiculo, m.CaminhoInvertido, m);
                if (nova is null) return;
            }
            tr.Commit();
        }
        Operacoes.Gerar(m, substituir: true);
    });
}
