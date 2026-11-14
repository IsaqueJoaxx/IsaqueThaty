using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Projeta.Giro.Core.Cinematica;
using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Infra;

namespace Projeta.Giro.Comandos;

/// <summary>Ajuda, configuração, demonstração e limpeza.</summary>
public sealed class CmdGeral
{
    [CommandMethod("PROJETA_GIRO", "GIROAJUDA", CommandFlags.Modal)]
    public void Ajuda()
    {
        var l = new[]
        {
            "──────── PROJETA • Giro de Veículos ────────",
            "GIROVEICULO     escolhe o veículo de projeto (DNIT: VP, CO, O, SR, RE + os seus).",
            "GIROARCO        condução por pontos: o veículo gira com transição de volante e segue em reta até cada ponto (Ré para veículos rígidos).",
            "GIROSEGUIR      o eixo dianteiro segue uma polilinha, linha, arco, alinhamento ou feature line.",
            "GIROGABARITO    gabarito de giro (30°…180°) no raio mínimo, como as figuras do DNIT.",
            "GIROATUALIZAR   refaz uma varredura (caminho editado, outro veículo, velocidade, veículos a cada X m).",
            "GIROVERIFICAR   confronta a envoltória com meios-fios/obstáculos e marca as invasões (vermelho) e a falta de folga (amarelo).",
            "GIROLIMPARVERIF apaga as marcas de verificação de uma varredura.",
            "GIROANIMAR      anima o veículo ao longo da varredura (Esc para parar).",
            "GIRORELATORIO   resumo: largura varrida, esterçamento, articulação, avisos.",
            "GIROCONFIG      velocidade, folga, intervalo dos veículos, rodas.",
            "GIRODEMO        desenha um cruzamento de exemplo com varreduras e verificação.",
            "Layers: GIRO-ENVOLTORIA, GIRO-RODAS, GIRO-TRAJETORIA, GIRO-VEICULO, GIRO-TEXTO, GIRO-CONFLITO, GIRO-FOLGA.",
            "Premissas: velocidade de manobra 10 km/h, tempo batente a batente 4 s (VP) / 6 s (demais), folga 0,50 m — ajuste ao caso.",
            "Rascunho técnico: confira com os gabaritos do DNIT (IPR-718) e valide no projeto executivo.",
        };
        foreach (var s in l) Ctx.Msg(s);
    }

    [CommandMethod("PROJETA_GIRO", "GIROCONFIG", CommandFlags.Modal)]
    public void Configurar() => Operacoes.Executar("GIROCONFIG", () =>
    {
        var cfg = Config.Atual;
        while (true)
        {
            var o = new PromptKeywordOptions(
                $"\nVelocidade {Num.F(cfg.Velocidade, 0)} km/h · Folga {Num.F(cfg.Folga)} m · Veículos a cada {(cfg.IntervaloVeiculos > 0 ? Num.F(cfg.IntervaloVeiculos, 0) + " m" : "início/fim")} · Rodas {(cfg.DesenharRodas ? "sim" : "não")} [Velocidade/Folga/Intervalo/Rodas] <sair>: ");
            o.Keywords.Add("Velocidade");
            o.Keywords.Add("Folga");
            o.Keywords.Add("Intervalo");
            o.Keywords.Add("Rodas");
            o.AllowNone = true;
            o.AppendKeywordsToMessage = false;
            var r = Ctx.Ed.GetKeywords(o);
            if (r.Status != PromptStatus.OK) break;
            switch (r.StringResult)
            {
                case "Velocidade": Operacoes.PerguntarVelocidade(); break;
                case "Folga":
                {
                    var d = Ctx.Ed.GetDistance(new PromptDistanceOptions($"\nFolga (m) <{Num.F(cfg.Folga)}>: ") { AllowNone = true, AllowNegative = false, AllowZero = true });
                    if (d.Status == PromptStatus.OK) cfg.Folga = d.Value;
                    break;
                }
                case "Intervalo":
                {
                    var d = Ctx.Ed.GetDouble(new PromptDoubleOptions($"\nDesenhar o veículo a cada quantos metros (0 = só início e fim) <{Num.F(cfg.IntervaloVeiculos, 0)}>: ") { AllowNone = true, AllowNegative = false, AllowZero = true });
                    if (d.Status == PromptStatus.OK) cfg.IntervaloVeiculos = d.Value;
                    break;
                }
                case "Rodas": cfg.DesenharRodas = !cfg.DesenharRodas; break;
            }
            cfg.Salvar();
        }
    });

    [CommandMethod("PROJETA_GIRO", "GIROLIMPAR", CommandFlags.Modal)]
    public void Limpar() => Operacoes.Executar("GIROLIMPAR", () =>
    {
        using var tr = Ctx.Db.TransactionManager.StartTransaction();
        var usados = new HashSet<string>();
        var ms = Ctx.EspacoModelo(tr, Ctx.Db, OpenMode.ForRead);
        foreach (ObjectId id in ms)
            if (tr.GetObject(id, OpenMode.ForRead) is Entity e && Armazenamento.Marca(e) is string mk && mk.Length > 2) usados.Add(mk[2..]);
        int n = 0;
        foreach (var id in Armazenamento.Ids(tr, Ctx.Db))
            if (!usados.Contains(id)) { Armazenamento.Remover(tr, Ctx.Db, id); n++; }
        tr.Commit();
        Ctx.Msg($"{n} manobra(s) sem desenho removida(s) do arquivo; {usados.Count} em uso.");
    });

    // ------------------------------------------------------------------ demonstração

    [CommandMethod("PROJETA_GIRO", "GIRODEMO", CommandFlags.Modal)]
    public void Demo() => Operacoes.Executar("GIRODEMO", () =>
    {
        var o = Ctx.ComInterface ? Operacoes.Ponto("Ponto de inserção do exemplo: ") : Point3d.Origin;
        if (o is null) return;
        var b = Ctx.V(Ctx.Wcs(o.Value));
        V2 P(double x, double y) => b + new V2(x, y);
        var bib = Config.Biblioteca;
        var cfg = Config.Atual;

        // cruzamento em T: via principal leste-oeste (7 m) e via lateral para o norte (7 m), raios de meio-fio de 12 m
        var meiosFios = new List<ObjectId>();
        using (Ctx.Doc.LockDocument())
        using (var tr = Ctx.Db.TransactionManager.StartTransaction())
        {
            Ctx.GarantirLayer(tr, Ctx.Db, "GIRO-DEMO-MEIOFIO", 7, LineWeight.LineWeight035);
            var ms = Ctx.EspacoModelo(tr, Ctx.Db);
            Polyline Pl(params (V2 P, double B)[] v)
            {
                var pl = new Polyline();
                pl.SetDatabaseDefaults();
                for (int i = 0; i < v.Length; i++) pl.AddVertexAt(i, Ctx.P2(v[i].P), v[i].B, 0, 0);
                pl.Layer = "GIRO-DEMO-MEIOFIO";
                return pl;
            }
            double k = Math.Tan(Math.PI / 8);
            meiosFios.Add(Ctx.Adicionar(tr, ms, Pl((P(-70, -3.5), 0), (P(70, -3.5), 0))));
            meiosFios.Add(Ctx.Adicionar(tr, ms, Pl((P(-70, 3.5), 0), (P(-15.5, 3.5), k), (P(-3.5, 15.5), 0), (P(-3.5, 60), 0))));
            meiosFios.Add(Ctx.Adicionar(tr, ms, Pl((P(70, 3.5), 0), (P(15.5, 3.5), -k), (P(3.5, 15.5), 0), (P(3.5, 60), 0))));
            Desenho.GarantirLayers(tr, Ctx.Db);
            var cam = Pl((P(-55, -1.75), 0), (P(-12, -1.75), k), (P(-1.75, 8.5), 0), (P(-1.75, 55), 0));
            cam.Layer = Desenho.LTrajetoria;
            Ctx.Adicionar(tr, ms, cam);
            tr.Commit();
        }

        // 1) SR conversão à direita (oeste → norte): reta, giro de 90° no batente com transição e reta, alinhado à faixa da via lateral
        var sr = bib.Obter("SR")!;
        var op = new OpcoesConducao { Velocidade = cfg.Velocidade };
        double taxa = Autoconducao.TaxaPorMetro(sr, cfg.Velocidade);
        var giro = Autoconducao.Curva(sr, false, V2.Zero, Math.PI, -Math.PI / 2, sr.AnguloMaximo, taxa, 0.05);
        double xIni = 1.75 - giro.Fim.X;          // onde começar o giro para terminar no eixo da faixa (x = 1,75)
        var pts1 = new List<V2> { P(55, 1.75) };
        pts1.AddRange(giro.Pontos.Select(q => P(xIni + q.X, 1.75 + q.Y)));
        pts1.Add(P(xIni + giro.Fim.X, 50));
        var m1 = new Manobra
        {
            Modo = "arco", Nome = "demo: SR à direita", Veiculo = sr, Velocidade = cfg.Velocidade, Inicio = P(55, 1.75), RumoInicial = Math.PI, IntervaloVeiculos = 8,
            Trechos = { new Trecho { Pontos = Geo.Arredondar(pts1) } },
        };
        Operacoes.Gerar(m1);

        // 2) VP conversão à esquerda (leste → norte) seguindo a polilinha do eixo
        var vp = bib.Obter("VP")!;
        var m2 = new Manobra { Modo = "seguir", Nome = "demo: VP à esquerda", Veiculo = vp, Velocidade = cfg.Velocidade };
        var pl2 = new Pl();
        pl2.Add(P(-55, -1.75)); pl2.Add(P(-12, -1.75), Math.Tan(Math.PI / 8)); pl2.Add(P(-1.75, 8.5)); pl2.Add(P(-1.75, 55));
        var pts2 = pl2.Pontos(0.0005);
        m2.Inicio = pts2[0]; m2.RumoInicial = 0;
        m2.Trechos.Add(new Trecho { Pontos = Geo.Arredondar(pts2) });
        Operacoes.Gerar(m2);

        // 3) gabaritos de 90° de todos os veículos DNIT, ao lado
        double x0 = 110;
        foreach (var v in bib.Ordenados.Where(x => bib.Padrao.Contains(x.Id)))
        {
            var pts = Autoconducao.Gabarito(v, P(x0, -30), Math.PI / 2, 90, false, op, v.Comprimento);
            var mg = new Manobra { Modo = "gabarito", Nome = "demo: 90°", Veiculo = v, Velocidade = cfg.Velocidade, Inicio = P(x0, -30), RumoInicial = Math.PI / 2, Trechos = { new Trecho { Pontos = Geo.Arredondar(pts) } } };
            Operacoes.Gerar(mg);
            x0 += 45;
        }

        // 4) verificação das duas conversões contra os meios-fios
        List<Core.Verificacao.Obstaculo> obs;
        using (var tr = Ctx.Db.TransactionManager.StartTransaction())
        {
            obs = Curvas.Obstaculos(tr, meiosFios, out _);
            tr.Commit();
        }
        Operacoes.Verificar(m1, obs, cfg.Folga);
        Operacoes.Verificar(m2, obs, cfg.Folga);
        if (Ctx.ComInterface)
        {
            try { Ctx.Doc.SendStringToExecute("_.ZOOM _E ", true, false, false); } catch { }
        }
        Ctx.Msg("GIRODEMO concluído: SR à direita (vermelho = invasão do meio-fio com raio de 12 m), VP à esquerda e gabaritos de 90° dos veículos DNIT.");
    });
}
