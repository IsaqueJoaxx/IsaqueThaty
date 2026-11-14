using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Projeta.Sinalizacao.Civil;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Defensas;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Semaforica;
using Projeta.Sinalizacao.Core.Vertical;

namespace Projeta.Sinalizacao.Infra;

/// <summary>
/// Modelo 3D (BIM) da sinalização: placas e suportes, semafórica, dispositivos de obra e defensas, em sólidos
/// com a cota da superfície (TIN) quando informada. Cada sólido guarda o handle do objeto de origem.
/// </summary>
internal static class Bim
{
    private sealed class Ctx3D
    {
        public required Transaction Tr { get; init; }
        public required BlockTableRecord Ms { get; init; }
        public required string Origem { get; init; }
        public int Solidos { get; set; }
    }

    private static Color Rgb(Cor c) { var (r, g, b) = Cores.Rgb(c); return Color.FromRgb(r, g, b); }
    private static readonly Color CorAco = Color.FromRgb(160, 165, 170);
    private static readonly Color CorConcreto = Color.FromRgb(190, 190, 185);

    private static void Add(Ctx3D c, Solid3d s, string layer, Color cor)
    {
        s.Layer = layer;
        s.Color = cor;
        Ctx.Adicionar(c.Tr, c.Ms, s);
        Armazenamento.GravarTexto(c.Tr, s, Armazenamento.Chave3D, c.Origem);
        c.Solidos++;
    }

    private static Solid3d Cilindro(Point3d baseP, double h, double r, double rTopo = -1)
    {
        var s = new Solid3d();
        s.CreateFrustum(h, r, r, rTopo < 0 ? r : rTopo);
        s.TransformBy(Matrix3d.Displacement(new Vector3d(baseP.X, baseP.Y, baseP.Z + h / 2)));
        return s;
    }

    /// <summary>Caixa com base centrada em <paramref name="baseP"/>, girada de <paramref name="rot"/> em torno de Z.</summary>
    private static Solid3d Caixa(Point3d baseP, double dx, double dy, double dz, double rot)
    {
        var s = new Solid3d();
        s.CreateBox(dx, dy, dz);
        s.TransformBy(Matrix3d.Rotation(rot, Vector3d.ZAxis, Point3d.Origin));
        s.TransformBy(Matrix3d.Displacement(new Vector3d(baseP.X, baseP.Y, baseP.Z + dz / 2)));
        return s;
    }

    /// <summary>Extrusão de um perfil (u = lateral, v = vertical) ao longo do segmento p0→p1.</summary>
    private static Solid3d? Extrusao(IList<V2> perfil, Point3d p0, Point3d p1)
    {
        var d = new Vector3d(p1.X - p0.X, p1.Y - p0.Y, 0);
        double L = d.Length;
        if (L < 1e-3) return null;
        d = d.GetNormal();
        var lateral = new Vector3d(-d.Y, d.X, 0);
        using var pl = Conversor.Poli(perfil, V2.Zero, true);
        var col = new DBObjectCollection { pl };
        using var regs = Region.CreateFromCurves(col);
        if (regs.Count == 0) return null;
        var reg = (Region)regs[0];
        var s = new Solid3d();
        s.Extrude(reg, L, 0);
        foreach (DBObject o in regs) o.Dispose();
        s.TransformBy(Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, p0, lateral, Vector3d.ZAxis, d));
        return s;
    }

    /// <summary>Chapa vertical: contorno local (x = largura, y = altura) com centro em <paramref name="centro"/>, face voltada para <paramref name="az"/>.</summary>
    private static Solid3d? Chapa(IList<V2> contorno, Point3d centro, double az, double esp)
    {
        var n = new Vector3d(Math.Cos(az), Math.Sin(az), 0);
        var xw = new Vector3d(-Math.Sin(az), Math.Cos(az), 0);
        using var pl = Conversor.Poli(contorno, V2.Zero, true);
        var col = new DBObjectCollection { pl };
        using var regs = Region.CreateFromCurves(col);
        if (regs.Count == 0) return null;
        var reg = (Region)regs[0];
        var s = new Solid3d();
        s.Extrude(reg, esp, 0);
        foreach (DBObject o in regs) o.Dispose();
        s.TransformBy(Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, centro, xw, Vector3d.ZAxis, n));
        return s;
    }

    private static (Cor fundo, Cor borda) CoresFace(Desenho2D d)
    {
        var cores = d.Itens.Where(p => p is PArea or PDisco).Select(p => p.Cor).Take(2).ToList();
        if (cores.Count == 0) return (Cor.Branco, Cor.Branco);
        return cores.Count == 1 ? (cores[0], cores[0]) : (cores[1], cores[0]);
    }

    public static int Gerar(Sessao s, List<(ObjectId Id, ObjetoSin Obj)> objs, ObjectId superficie)
    {
        var tr = s.Tr; var db = s.Db;
        var ms = Ctx.EspacoModelo(tr, db);
        foreach (var (nome, cor) in new (string, short)[] { ("SIN-3D-PLACAS", 7), ("SIN-3D-SUPORTES", 8), ("SIN-3D-SEMAFORICA", 3), ("SIN-3D-OBRAS", 30), ("SIN-3D-DEFENSAS", 8) })
            Ctx.GarantirLayer(tr, db, nome, cor);

        // remove sólidos anteriores dos mesmos objetos
        var origens = new HashSet<string>(objs.Select(o => o.Id.Handle.ToString()));
        foreach (ObjectId id in ms)
        {
            if (!id.ObjectClass.IsDerivedFrom(Autodesk.AutoCAD.Runtime.RXObject.GetClass(typeof(Solid3d)))) continue;
            var e = (Entity)tr.GetObject(id, OpenMode.ForRead);
            var org = Armazenamento.LerTexto(tr, e, Armazenamento.Chave3D);
            if (org is not null && origens.Contains(org)) { e.UpgradeOpen(); e.Erase(); }
        }

        var n = s.Normas;
        int total = 0;
        foreach (var (id, o) in objs)
        {
            var c = new Ctx3D { Tr = tr, Ms = ms, Origem = id.Handle.ToString() };
            double Z(double x, double y) => Eixos.Cota(tr, superficie, x, y) ?? 0;
            try
            {
                switch (o.Categoria)
                {
                    case Categoria.Placa: Placa(c, s, o, Z(o.X, o.Y)); break;
                    case Categoria.Semaforica: Semaforo(c, o, Z(o.X, o.Y), n); break;
                    case Categoria.Obra: Dispositivo(c, o.Codigo, new V2(o.X, o.Y), o.Rotacao, Z(o.X, o.Y)); break;
                    case Categoria.ZonaObra: Zona(c, s, o, Z); break;
                    case Categoria.Defensa: Defensa(c, s, o, Z); break;
                }
            }
            catch (System.Exception ex) { s.Avisos.Add($"{o.Id}: 3D não gerado ({ex.Message})."); }
            total += c.Solidos;
        }
        return total;
    }

    private static void Placa(Ctx3D c, Sessao s, ObjetoSin o, double z0)
    {
        var n = s.Normas;
        var dp = o.Placa ?? new DadosPlaca();
        var faces = dp.Placas.Select(p => Motor.Face(p, n, s.Cfg.Via)).ToList();
        if (faces.Count == 0) return;
        double H = faces.Sum(f => f.altura) + 0.05 * (faces.Count - 1);
        double W = faces.Max(f => f.largura);
        bool susp = Suportes.Suspenso(dp.Suporte);
        bool urb = s.Cfg.Via == Core.Normas.TipoVia.Urbana;
        double livre = susp ? (urb ? n.AlturaLivreSuspensaUrbana : n.AlturaLivreSuspensaRural) : n.AlturaLivre(s.Cfg.Via);
        double az = dp.Azimute;
        var nrm = V2.Polar(az);
        var xw = nrm.Esquerda; // direita de quem olha a placa
        var p0 = new V2(o.X, o.Y);
        double rTubo = 0.0365;

        // suportes
        double hCol = livre + H + 0.10;
        var centroPlacas = p0 + nrm * 0.06;
        switch (dp.Suporte)
        {
            case TipoSuporte.ColunaSimples:
                Add(c, Cilindro(new Point3d(p0.X, p0.Y, z0), hCol, rTubo), "SIN-3D-SUPORTES", CorAco); break;
            case TipoSuporte.ColunaDupla:
            case TipoSuporte.ColunaTripla:
            {
                int k = dp.Suporte == TipoSuporte.ColunaDupla ? 2 : 3;
                for (int i = 0; i < k; i++)
                {
                    double f = k == 1 ? 0 : -0.5 + i / (double)(k - 1);
                    var q = p0 + xw * (f * Math.Max(0.2, W - 0.2));
                    Add(c, Cilindro(new Point3d(q.X, q.Y, z0), hCol, rTubo), "SIN-3D-SUPORTES", CorAco);
                }
                break;
            }
            case TipoSuporte.Existente: break;
            default:
            {
                // braço projetado, semipórtico ou pórtico: coluna(s) + viga sobre a pista (para a esquerda de quem olha a placa)
                double vao = dp.Vao > 0 ? dp.Vao : dp.Suporte == TipoSuporte.Portico ? 12 : dp.Suporte is TipoSuporte.BracoProjetado or TipoSuporte.BracoProjetadoDuplo ? n.ComprimentoBracoProjetado : 6;
                double hc = livre + H + 0.4;
                Add(c, Cilindro(new Point3d(p0.X, p0.Y, z0), hc, 0.10), "SIN-3D-SUPORTES", CorAco);
                if (dp.Suporte is TipoSuporte.Portico)
                {
                    var q = p0 - xw * vao;
                    Add(c, Cilindro(new Point3d(q.X, q.Y, z0), hc, 0.10), "SIN-3D-SUPORTES", CorAco);
                }
                var meio = p0 - xw * (vao / 2);
                Add(c, Caixa(new Point3d(meio.X, meio.Y, z0 + hc - 0.3), vao + 0.2, 0.2, 0.3, xw.Angulo), "SIN-3D-SUPORTES", CorAco);
                centroPlacas = p0 - xw * (dp.Suporte == TipoSuporte.Portico ? vao / 2 : Math.Max(W / 2 + 0.3, vao * 0.6)) + nrm * 0.15;
                break;
            }
        }

        // placas empilhadas (a primeira em cima)
        double zTopo = z0 + livre + H;
        foreach (var f in faces)
        {
            double zc = zTopo - f.altura / 2;
            var forma = f.forma;
            var p = dp.Placas[faces.IndexOf(f)];
            var cont = GeradorPlaca.Contorno(forma, forma == Forma.Retangular ? f.largura : p.Dimensao, f.largura, f.altura);
            var (fundo, borda) = CoresFace(f.d);
            var centro = new Point3d(centroPlacas.X, centroPlacas.Y, zc);
            var ch = Chapa(cont, centro, az, 0.003);
            if (ch is not null) Add(c, ch, "SIN-3D-PLACAS", Rgb(borda));
            var interno = cont.Select(v => v * 0.86).ToList();
            var ch2 = Chapa(interno, centro + new Vector3d(nrm.X * 0.003, nrm.Y * 0.003, 0), az, 0.001);
            if (ch2 is not null) Add(c, ch2, "SIN-3D-PLACAS", Rgb(fundo));
            zTopo -= f.altura + 0.05;
        }
    }

    private static void Semaforo(Ctx3D c, ObjetoSin o, double z0, Core.Normas.Normas n)
    {
        var p = new Point3d(o.X, o.Y, z0);
        var tipo = CatalogoSemaforica.Obter(o.Codigo);
        double az = o.Rotacao;
        switch (o.Codigo)
        {
            case "COL-S": Add(c, Cilindro(p, tipo?.Altura ?? 4, 0.057), "SIN-3D-SEMAFORICA", CorAco); break;
            case "COL-BP":
            {
                double h = n.AlturaColunaSemaforo, braco = o.Dispositivo?.Braco > 0 ? o.Dispositivo.Braco : n.ComprimentoBracoProjetado;
                Add(c, Cilindro(p, h, 0.07), "SIN-3D-SEMAFORICA", CorAco);
                // braço na direção da rotação do objeto (perpendicular à face dos grupos focais)
                var dir = V2.Polar(az - Math.PI / 2);
                var m = new V2(o.X, o.Y) + dir * (braco / 2);
                Add(c, Caixa(new Point3d(m.X, m.Y, z0 + h - 0.15), braco, 0.10, 0.10, dir.Angulo), "SIN-3D-SEMAFORICA", CorAco);
                break;
            }
            case "CTR": Add(c, Caixa(p, 0.6, 0.4, 1.2, az), "SIN-3D-SEMAFORICA", Color.FromRgb(120, 120, 120)); break;
            case "CXP": Add(c, Caixa(new Point3d(p.X, p.Y, z0 - 0.5), 0.5, 0.5, 0.5, az), "SIN-3D-SEMAFORICA", CorConcreto); break;
            case "BOT": Add(c, Caixa(new Point3d(p.X, p.Y, z0 + 1.0), 0.15, 0.12, 0.25, az), "SIN-3D-SEMAFORICA", Color.FromRgb(240, 200, 0)); break;
            default:
            {
                // grupos focais: caixa na altura livre, face voltada para a rotação
                double w = tipo?.Largura ?? 0.3, d = tipo?.Profundidade ?? 0.25, h = tipo?.Altura ?? 0.8;
                Add(c, Caixa(new Point3d(p.X, p.Y, z0 + n.AlturaLivreGrupoFocal), w, d, h, az - Math.PI / 2), "SIN-3D-SEMAFORICA", Color.FromRgb(30, 30, 30));
                break;
            }
        }
    }

    private static void Dispositivo(Ctx3D c, string codigo, V2 pt, double rot, double z0)
    {
        var p = new Point3d(pt.X, pt.Y, z0);
        var laranja = Rgb(Cor.Laranja);
        switch (codigo.ToUpperInvariant())
        {
            case "CONE": Add(c, Cilindro(p, 0.75, 0.18, 0.03), "SIN-3D-OBRAS", laranja); break;
            case "TAMBOR": Add(c, Cilindro(p, 1.0, 0.30), "SIN-3D-OBRAS", laranja); break;
            case "CILINDRO": Add(c, Cilindro(p, 0.75, 0.10), "SIN-3D-OBRAS", Rgb(Cor.Amarelo)); break;
            case "BALIZADOR": Add(c, Caixa(p, 0.15, 0.15, 1.0, rot), "SIN-3D-OBRAS", laranja); break;
            case "CAVALETE": Add(c, Caixa(new Point3d(p.X, p.Y, z0 + 0.7), 1.2, 0.05, 0.3, rot), "SIN-3D-OBRAS", laranja); break;
            case "BARREIRA": Add(c, Caixa(p, 2.0, 0.5, 0.8, rot), "SIN-3D-OBRAS", laranja); break;
            case "LUZ": Add(c, Cilindro(new Point3d(p.X, p.Y, z0 + 1.0), 0.2, 0.08), "SIN-3D-OBRAS", Rgb(Cor.Amarelo)); break;
            default: Add(c, Caixa(p, 0.4, 0.4, 0.4, rot), "SIN-3D-OBRAS", laranja); break;
        }
    }

    private static void Zona(Ctx3D c, Sessao s, ObjetoSin o, Func<double, double, double> Z)
    {
        var g = Motor.Gerar(o.Clonar(), s.Normas, s.Cfg.Exibicao());
        foreach (var (p, ang, cod) in g.Pontos)
        {
            if (cod == (o.Zona?.Dispositivo ?? "CONE")) Dispositivo(c, cod, p, ang, Z(p.X, p.Y));
            else
            {
                // placa de pré-sinalização em coluna simples, voltada para o tráfego que chega
                var po = new ObjetoSin
                {
                    Categoria = Categoria.Placa, X = p.X, Y = p.Y,
                    Placa = new DadosPlaca { Placas = { new PlacaItem { Codigo = cod, Obra = cod.StartsWith("A") } }, Azimute = ang + Math.PI },
                };
                Placa(c, s, po, Z(p.X, p.Y));
            }
        }
    }

    private static void Defensa(Ctx3D c, Sessao s, ObjetoSin o, Func<double, double, double> Z)
    {
        var cam = o.CaminhoObj;
        if (cam is null) return;
        var p = o.Defensa ?? new ParamDefensa();
        var n = s.Normas;
        if (p.Tipo == TipoDefensa.BarreiraConcreto)
        {
            var perfil = new List<V2> { new(-0.30, 0), new(0.30, 0), new(0.30, 0.075), new(0.19, 0.33), new(0.075, 0.81), new(-0.075, 0.81), new(-0.19, 0.33), new(-0.30, 0.075) };
            for (double a = 0; a < cam.Comprimento - 1e-6; a += 2.0)
            {
                double b = Math.Min(cam.Comprimento, a + 2.0);
                var q0 = cam.Ponto(a); var q1 = cam.Ponto(b);
                var sol = Extrusao(perfil, new Point3d(q0.X, q0.Y, Z(q0.X, q0.Y)), new Point3d(q1.X, q1.Y, Z(q1.X, q1.Y)));
                if (sol is not null) Add(c, sol, "SIN-3D-DEFENSAS", CorConcreto);
            }
            return;
        }
        var r = GeradorDefensa.Gerar(cam, p, n);
        int lado = p.LadoTrafego >= 0 ? 1 : -1;
        double hTopo = n.AlturaDefensa;
        // perfil simplificado da lâmina (W) em u = lateral (lado do tráfego), v = vertical
        var lamina = new List<V2> { new(0, hTopo - 0.31), new(0.06, hTopo - 0.26), new(0.03, hTopo - 0.155), new(0.06, hTopo - 0.05), new(0, hTopo), new(-0.02, hTopo), new(0.01, hTopo - 0.155), new(-0.02, hTopo - 0.31) }
            .Select(v => new V2(v.X * lado + lado * 0.10, v.Y)).ToList();
        var faces = p.Tipo == TipoDefensa.SemimaleavelDupla ? new[] { lamina, lamina.Select(v => new V2(-v.X, v.Y)).ToList() } : new[] { lamina };
        for (int i = 0; i < r.Postes.Count; i++)
        {
            var q = cam.Ponto(r.Postes[i]);
            double z = Z(q.X, q.Y);
            Add(c, Caixa(new Point3d(q.X, q.Y, z), 0.15, 0.15, hTopo + 0.05, cam.Tangente(r.Postes[i]).Angulo), "SIN-3D-DEFENSAS", CorAco);
            if (i == 0) continue;
            var q0 = cam.Ponto(r.Postes[i - 1]);
            foreach (var f in faces)
            {
                var sol = Extrusao(f, new Point3d(q0.X, q0.Y, Z(q0.X, q0.Y)), new Point3d(q.X, q.Y, z));
                if (sol is not null) Add(c, sol, "SIN-3D-DEFENSAS", CorAco);
            }
        }
    }
}
