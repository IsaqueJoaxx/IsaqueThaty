using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Desenho;

namespace Projeta.Sinalizacao.Infra;

/// <summary>
/// Materializa um <see cref="ObjetoSin"/> no DWG: cada objeto é uma referência a um bloco anônimo
/// (um clique seleciona o conjunto todo) e carrega os dados no dicionário de extensão.
/// </summary>
internal static class Gerador
{
    public const string LayerTexto = "SIN-TEXTO";

    public static (string Nome, short Cor) Layer(ObjetoSin o) => o.Categoria switch
    {
        Categoria.Placa => ("SIN-V-PLACAS", 7),
        Categoria.MarcaLongitudinal => ("SIN-H-" + o.Codigo.ToUpperInvariant(), o.Codigo.StartsWith("LFO", StringComparison.OrdinalIgnoreCase) || o.Codigo is "MFR" or "MFE-CF" or "LPP" ? (short)2 : (short)7),
        Categoria.MarcaTransversal => ("SIN-H-TRANSVERSAIS", 7),
        Categoria.Inscricao => ("SIN-H-INSCRICOES", 7),
        Categoria.Tachas => ("SIN-H-TACHAS", 2),
        Categoria.Semaforica => ("SIN-S-SEMAFORICA", 3),
        Categoria.RedeSemaforica => ("SIN-S-REDE", 30),
        Categoria.Obra or Categoria.ZonaObra => ("SIN-O-OBRAS", 30),
        Categoria.Defensa => ("SIN-D-DEFENSAS", 8),
        _ => ("SIN-OUTROS", 7),
    };

    private static string Sufixo(Situacao s) => s switch
    {
        Situacao.Manter => " (EXIST.)",
        Situacao.Remover => " (REMOVER)",
        Situacao.Recuperar => " (RECUP.)",
        _ => "",
    };

    /// <summary>Gera (ou regenera) o objeto. Se <paramref name="refExistente"/> for nulo, cria uma nova referência no espaço do modelo.</summary>
    public static ObjectId Materializar(Transaction tr, Database db, Config cfg, ObjetoSin o, ObjectId refExistente, List<string>? avisos = null)
    {
        Comandos.CmdDemo.Passo($"    materializar {o.Categoria} {o.Codigo}");
        var n = cfg.Normas();
        var g = Motor.Gerar(o, n, cfg.Exibicao());
        Comandos.CmdDemo.Passo($"    motor ok ({g.Desenho.Itens.Count} primitivas)");
        foreach (var a in g.Avisos) avisos?.Add($"{o.Id}: {a}");
        var (layer, corLayer) = Layer(o);
        Ctx.GarantirLayer(tr, db, layer, corLayer);
        Ctx.GarantirLayer(tr, db, LayerTexto, 7);

        var (btrId, btr) = Conversor.NovoBlocoAnonimo(tr, db);
        Comandos.CmdDemo.Passo("    bloco anônimo criado");
        double t = cfg.TextoModelo;
        double mm = cfg.MmModelo;
        V2 baseP;

        switch (o.Categoria)
        {
            case Categoria.Placa:
            {
                baseP = new V2(o.X, o.Y);
                double F = cfg.FatorPlaca;
                double gap = 4 * mm;
                var faces = g.Desenho.Transformado(F, 0, new V2(0, gap));
                Conversor.Desenhar(tr, btr, faces, ModoCor.Placa, o.Situacao, V2.Zero);
                // haste, marcador do suporte e orientação da face
                AddLinha(tr, btr, new[] { V2.Zero, new V2(0, gap) }, 0, ColorByLayer);
                AddDisco(tr, btr, V2.Zero, 0.9 * mm, ColorByLayer);
                double az = o.Placa?.Azimute ?? Math.PI / 2;
                var u = V2.Polar(az);
                AddLinha(tr, btr, new[] { u.Esquerda * (2.0 * mm), -u.Esquerda * (2.0 * mm) }, 0.5 * mm, ColorByLayer);
                AddLinha(tr, btr, new[] { V2.Zero, u * (3.0 * mm) }, 0, ColorByLayer);
                if (o.Situacao == Situacao.Remover)
                {
                    var cx = faces.Limites();
                    var vermelho = Color.FromColorIndex(ColorMethod.ByAci, 1);
                    AddLinha(tr, btr, new[] { new V2(cx.XMin, cx.YMin), new V2(cx.XMax, cx.YMax) }, 0.5 * mm, vermelho);
                    AddLinha(tr, btr, new[] { new V2(cx.XMin, cx.YMax), new V2(cx.XMax, cx.YMin) }, 0.5 * mm, vermelho);
                }
                string rot = $"{o.Id}{Sufixo(o.Situacao)}\n{o.Codigo}";
                AddTexto(tr, btr, rot, new V2(0, -1.6 * t), t, 0, AttachmentPoint.TopCenter);
                break;
            }
            case Categoria.Semaforica:
            case Categoria.Obra:
            {
                baseP = new V2(o.X, o.Y);
                double rotSim = o.Categoria == Categoria.Semaforica ? o.Rotacao - Math.PI / 2 : o.Rotacao;
                var sim = g.Desenho.Transformado(cfg.FatorDispositivos, rotSim);
                Conversor.Desenhar(tr, btr, sim, ModoCor.Placa, o.Situacao, V2.Zero);
                var cx = sim.Limites();
                AddTexto(tr, btr, $"{o.Id} {o.Codigo}{Sufixo(o.Situacao)}", new V2(0, cx.YMin - 0.6 * t), 0.8 * t, 0, AttachmentPoint.TopCenter);
                break;
            }
            default:
            {
                baseP = PontoBase(o);
                bool marca = o.Categoria is Categoria.MarcaLongitudinal or Categoria.MarcaTransversal or Categoria.Inscricao or Categoria.Tachas;
                Conversor.Desenhar(tr, btr, g.Desenho, marca ? ModoCor.Marca : ModoCor.Placa, o.Situacao, baseP, "0", agrupar: marca);
                Comandos.CmdDemo.Passo("    desenho ok");
                Rotulo(tr, btr, cfg, o, baseP, t);
                Comandos.CmdDemo.Passo("    rótulo ok");
                break;
            }
        }

        BlockReference br;
        if (!refExistente.IsNull)
        {
            br = (BlockReference)tr.GetObject(refExistente, OpenMode.ForWrite);
            br.BlockTableRecord = btrId;
            br.Position = Ctx.P(baseP);
            br.Rotation = 0;
            br.ScaleFactors = new Scale3d(1);
            br.Layer = layer;
        }
        else
        {
            br = new BlockReference(Ctx.P(baseP), btrId) { Layer = layer };
            Ctx.Adicionar(tr, Ctx.EspacoModelo(tr, db), br);
        }
        Comandos.CmdDemo.Passo("    referência ok");
        o.BaseX = baseP.X; o.BaseY = baseP.Y; o.BaseRot = 0;
        Armazenamento.Gravar(tr, br, o);
        Comandos.CmdDemo.Passo("    dados gravados");
        return br.ObjectId;
    }

    private static readonly Color ColorByLayer = Color.FromColorIndex(ColorMethod.ByLayer, 256);

    private static V2 PontoBase(ObjetoSin o)
    {
        if (o.Caminho is { Count: > 0 }) return o.Caminho[0].P;
        if (o.Marca is not null && (o.Categoria == Categoria.MarcaTransversal)) return new V2(o.Marca.X1, o.Marca.Y1);
        return new V2(o.X, o.Y);
    }

    private static void Rotulo(Transaction tr, BlockTableRecord btr, Config cfg, ObjetoSin o, V2 baseP, double t)
    {
        if (!cfg.RotularMarcas && o.Categoria is Categoria.MarcaLongitudinal or Categoria.MarcaTransversal) return;
        V2 p; double ang = 0; string txt;
        switch (o.Categoria)
        {
            case Categoria.MarcaLongitudinal:
            case Categoria.Defensa:
            case Categoria.RedeSemaforica:
            case Categoria.ZonaObra:
            {
                var c = o.CaminhoObj;
                if (c is null) return;
                double s = o.Categoria == Categoria.ZonaObra && o.Zona is not null ? Math.Clamp(o.Zona.InicioTrabalho, 0, c.Comprimento) : c.Comprimento / 2;
                double desl = (o.Marca?.Deslocamento ?? 0) + (o.Categoria == Categoria.ZonaObra ? (o.Zona?.DeslocamentoFaixa ?? 0) : 0);
                p = c.PontoDeslocado(s, desl + 1.6 * t + (o.Q("largura") / 2));
                ang = c.Tangente(s).Angulo;
                txt = o.Categoria switch
                {
                    Categoria.MarcaLongitudinal => $"{o.Codigo}{Sufixo(o.Situacao)}",
                    Categoria.Defensa => $"{o.Id} {o.Descricao}{Sufixo(o.Situacao)}",
                    Categoria.ZonaObra => $"{o.Id} — área de trabalho{Sufixo(o.Situacao)}",
                    _ => $"{o.Codigo}{Sufixo(o.Situacao)}",
                };
                break;
            }
            case Categoria.MarcaTransversal:
            {
                var m = o.Marca!;
                if (o.Codigo == "ZPA" && o.Caminho is { Count: > 2 })
                {
                    double sx = 0, sy = 0; foreach (var v in o.Caminho) { sx += v.X; sy += v.Y; }
                    p = new V2(sx / o.Caminho.Count, sy / o.Caminho.Count);
                }
                else
                {
                    var a = new V2(m.X1, m.Y1); var b = new V2(m.X2, m.Y2);
                    p = (a + b) / 2 - (b - a).Unitario().Esquerda * (2.5 * t);
                    ang = (b - a).Angulo;
                }
                txt = $"{o.Codigo}{Sufixo(o.Situacao)}";
                break;
            }
            default: return;
        }
        // mantém o texto legível (não de cabeça para baixo)
        ang = Num.NormAng(ang);
        if (ang > Math.PI / 2 + 1e-6 || ang < -Math.PI / 2 - 1e-6) ang += Math.PI;
        AddTexto(tr, btr, txt, p - baseP, t, ang, AttachmentPoint.MiddleCenter);
    }

    private static void AddTexto(Transaction tr, BlockTableRecord btr, string s, V2 p, double h, double rot, AttachmentPoint anc)
    {
        var m = Ctx.Texto(s, Ctx.P(p), h, rot, anc);
        m.Layer = LayerTexto;
        m.Color = ColorByLayer;
        Ctx.Adicionar(tr, btr, m);
    }

    private static void AddLinha(Transaction tr, BlockTableRecord btr, IList<V2> pts, double largura, Color cor)
    {
        var pl = Conversor.Poli(pts, V2.Zero, false, largura);
        pl.Color = cor;
        pl.Layer = "0";
        Ctx.Adicionar(tr, btr, pl);
    }

    private static void AddDisco(Transaction tr, BlockTableRecord btr, V2 c, double r, Color cor) => Conversor.Disco(tr, btr, c, r, cor, "0");
}
