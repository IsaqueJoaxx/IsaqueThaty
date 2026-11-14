using Projeta.Core.Comum;
using Projeta.Core.Expressoes;

namespace Projeta.Core.Modelador;

/// <summary>Matriz de transformação 4x4 (linha-maior), independente do AutoCAD.</summary>
public sealed class Matriz
{
    public readonly double[] M = new double[16];
    public Matriz() { M[0] = M[5] = M[10] = M[15] = 1; }
    public static Matriz Identidade => new();

    public static Matriz Translacao(P3 d) { var m = new Matriz(); m.M[3] = d.X; m.M[7] = d.Y; m.M[11] = d.Z; return m; }

    /// <summary>Rotação de <paramref name="angRad"/> em torno do eixo (unitário) passando pela origem (Rodrigues).</summary>
    public static Matriz Rotacao(P3 eixo, double angRad)
    {
        var u = eixo.Unitario();
        double c = Math.Cos(angRad), s = Math.Sin(angRad), t = 1 - c;
        var m = new Matriz();
        m.M[0] = t * u.X * u.X + c; m.M[1] = t * u.X * u.Y - s * u.Z; m.M[2] = t * u.X * u.Z + s * u.Y;
        m.M[4] = t * u.X * u.Y + s * u.Z; m.M[5] = t * u.Y * u.Y + c; m.M[6] = t * u.Y * u.Z - s * u.X;
        m.M[8] = t * u.X * u.Z - s * u.Y; m.M[9] = t * u.Y * u.Z + s * u.X; m.M[10] = t * u.Z * u.Z + c;
        return m;
    }

    public static Matriz operator *(Matriz a, Matriz b)
    {
        var r = new Matriz();
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                double s = 0;
                for (int k = 0; k < 4; k++) s += a.M[i * 4 + k] * b.M[k * 4 + j];
                r.M[i * 4 + j] = s;
            }
        return r;
    }

    public P3 Aplicar(P3 p) => new(
        M[0] * p.X + M[1] * p.Y + M[2] * p.Z + M[3],
        M[4] * p.X + M[5] * p.Y + M[6] * p.Z + M[7],
        M[8] * p.X + M[9] * p.Y + M[10] * p.Z + M[11]);

    public P3 AplicarVetor(P3 v) => new(
        M[0] * v.X + M[1] * v.Y + M[2] * v.Z,
        M[4] * v.X + M[5] * v.Y + M[6] * v.Z,
        M[8] * v.X + M[9] * v.Y + M[10] * v.Z);
}

// ------------------------------------------------------------------ árvore de sólidos avaliada

public abstract record Solido
{
    /// <summary>Volume estimado (exato para primitivas; para booleanas supõe subtraendos contidos e uniões disjuntas).</summary>
    public abstract double VolumeEstimado();
}
/// <summary>Caixa com centro da face inferior em <see cref="Base"/>, dimensões X, Y, Z.</summary>
public sealed record SolCaixa(P3 Base, double X, double Y, double Z) : Solido
{ public override double VolumeEstimado() => X * Y * Z; }
/// <summary>Cilindro/tronco de cone com centro da base, eixo unitário, raios e altura.</summary>
public sealed record SolCilindro(P3 Base, P3 Eixo, double RaioBase, double RaioTopo, double Altura) : Solido
{ public override double VolumeEstimado() => Math.PI * Altura * (RaioBase * RaioBase + RaioBase * RaioTopo + RaioTopo * RaioTopo) / 3; }
public sealed record SolEsfera(P3 Centro, double Raio) : Solido
{ public override double VolumeEstimado() => 4.0 / 3.0 * Math.PI * Math.Pow(Raio, 3); }
/// <summary>Extrusão de um perfil 2D. Plano XY → extrude em +Z a partir de Desloc; XZ → em +Y; YZ → em +X.</summary>
public sealed record SolExtrusao(IReadOnlyList<P2> Perfil, string Plano, double Altura) : Solido
{ public override double VolumeEstimado() => Geo.Area(Perfil) * Altura; }
/// <summary>Revolução de um perfil (x = raio, y = z) em torno do eixo Z.</summary>
public sealed record SolRevolucao(IReadOnlyList<P2> Perfil, double AnguloGraus) : Solido
{
    public override double VolumeEstimado()
    {
        var c = Geo.Centroide(Perfil);
        return 2 * Math.PI * Math.Abs(c.X) * Geo.Area(Perfil) * AnguloGraus / 360.0;
    }
}
public sealed record SolBooleano(string Tipo, Solido Base, IReadOnlyList<Solido> Outros) : Solido
{
    public override double VolumeEstimado() => Tipo switch
    {
        "uniao" => Base.VolumeEstimado() + Outros.Sum(o => o.VolumeEstimado()),
        "subtrair" => Math.Max(0, Base.VolumeEstimado() - Outros.Sum(o => o.VolumeEstimado())),
        _ => Math.Min(Base.VolumeEstimado(), Outros.Select(o => o.VolumeEstimado()).DefaultIfEmpty(0).Min()),
    };
}
public sealed record SolTransformado(Solido Base, Matriz Matriz) : Solido
{ public override double VolumeEstimado() => Base.VolumeEstimado(); }

// ------------------------------------------------------------------ planta

public abstract record Planta2D(string Camada);
public sealed record PlCirculo(P2 Centro, double Raio, string Camada) : Planta2D(Camada);
public sealed record PlPolilinha(IReadOnlyList<P2> Pontos, bool Fechada, string Camada) : Planta2D(Camada);
public sealed record PlArco(P2 Centro, double Raio, double DeGraus, double AteGraus, string Camada) : Planta2D(Camada);
public sealed record PlTexto(P2 Posicao, string Texto, double Altura, string Camada) : Planta2D(Camada);

/// <summary>Resultado da avaliação de um dispositivo para um conjunto de parâmetros.</summary>
public sealed class ModeloAvaliado
{
    public DefinicaoDispositivo Definicao { get; init; } = new();
    public Dictionary<string, double> Valores { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Solido> Solidos { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Solido> Saida { get; } = new();
    public List<Planta2D> Planta { get; } = new();
    public List<(string Nome, string Unidade, double Valor)> Quantitativos { get; } = new();
    public double RaioFuros { get; set; } = double.NaN;
    public List<Mensagem> Mensagens { get; } = new();
    public bool Ok => Mensagens.All(m => m.Gravidade != Gravidade.Erro);
}

/// <summary>Avalia definições de dispositivos e seções-tipo (parâmetros → geometria).</summary>
public static class AvaliadorDispositivo
{
    /// <summary>
    /// Resolve os valores dos parâmetros: valor informado > valor da rede > padrão (que pode ser expressão).
    /// </summary>
    public static ContextoDicionario ResolverParametros(IEnumerable<ParametroDef> parametros, IEnumerable<VariavelDef> variaveis,
        IReadOnlyDictionary<string, double>? informados, List<Mensagem> msgs, string objeto)
    {
        var ctx = new ContextoDicionario();
        // nomes não diferenciam maiúsculas nas expressões: "hg" e "Hg" seriam o mesmo parâmetro
        foreach (var g in parametros.Select(p => p.Nome).Concat(variaveis.Select(v => v.Nome))
                     .GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            msgs.Add(new(Gravidade.Erro, $"Nomes repetidos (maiúsculas/minúsculas não diferenciam): {string.Join(", ", g)}", objeto));
        if (informados is not null)
            foreach (var kv in informados) ctx[kv.Key] = kv.Value; // inclui valores "da rede"
        var pendentes = parametros.Where(p => !ctx.Variaveis.ContainsKey(p.Nome)).ToList();
        // padrões podem depender uns dos outros: resolve em até N passadas
        for (int passada = 0; passada < 8 && pendentes.Count > 0; passada++)
        {
            foreach (var p in pendentes.ToList())
            {
                try
                {
                    var v = p.Padrao.AvaliarValor(ctx);
                    ctx[p.Nome] = v;
                    pendentes.Remove(p);
                }
                catch (ErroExpressao) { /* tenta de novo na próxima passada */ }
            }
        }
        foreach (var p in pendentes)
        {
            msgs.Add(new(Gravidade.Erro, $"Não foi possível avaliar o padrão de '{p.Nome}' = {p.Padrao}", objeto));
            ctx[p.Nome] = 0.0;
        }
        foreach (var p in parametros)
        {
            if (ctx.Variaveis.TryGetValue(p.Nome, out var v) && !v.EhTexto)
            {
                double x = v.Num;
                if (p.Min is double mn && x < mn - 1e-9) msgs.Add(new(Gravidade.Aviso, $"{p.Nome} = {x:0.###} abaixo do mínimo {mn:0.###}", objeto));
                if (p.Max is double mx && x > mx + 1e-9) msgs.Add(new(Gravidade.Aviso, $"{p.Nome} = {x:0.###} acima do máximo {mx:0.###}", objeto));
            }
        }
        foreach (var vdef in variaveis)
        {
            try { ctx[vdef.Nome] = vdef.Expr.AvaliarValor(ctx); }
            catch (ErroExpressao ex) { msgs.Add(new(Gravidade.Erro, $"Variável {vdef.Nome}: {ex.Message}", objeto)); ctx[vdef.Nome] = 0.0; }
        }
        return ctx;
    }

    public static ModeloAvaliado Avaliar(DefinicaoDispositivo def, IReadOnlyDictionary<string, double>? valores = null)
    {
        var mod = new ModeloAvaliado { Definicao = def };
        var ctx = ResolverParametros(def.Parametros, def.Variaveis, valores, mod.Mensagens, def.Id);
        foreach (var kv in ctx.Variaveis) if (!kv.Value.EhTexto) mod.Valores[kv.Key] = kv.Value.Num;

        // funções extras disponíveis nas expressões dos quantitativos
        ctx.Funcoes = (nome, args) =>
        {
            if (nome.Equals("volume", StringComparison.OrdinalIgnoreCase) && args.Length == 1 && args[0].EhTexto)
                return mod.Solidos.TryGetValue(args[0].Str!, out var s) ? s.VolumeEstimado() : 0.0;
            return null;
        };

        foreach (var op in def.Solidos)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(op.Condicao) && !Expressao.Compilar(op.Condicao!).Avaliar(ctx).ComoBool()) continue;
                var s = AvaliarOperacao(op, ctx, mod.Solidos);
                if (s is not null) mod.Solidos[op.Id] = s;
            }
            catch (Exception ex) when (ex is ErroExpressao or ArgumentException or KeyNotFoundException)
            {
                mod.Mensagens.Add(new(Gravidade.Erro, $"Sólido '{op.Id}' ({op.Op}): {ex.Message}", def.Id));
            }
        }
        var ids = def.Saida.Count > 0 ? def.Saida : def.Solidos.Count > 0 ? new List<string> { def.Solidos[^1].Id } : new();
        foreach (var id in ids)
            if (mod.Solidos.TryGetValue(id, out var s)) mod.Saida.Add(s);
            else if (def.Solidos.Any(o => o.Id == id && string.IsNullOrWhiteSpace(o.Condicao)))
                mod.Mensagens.Add(new(Gravidade.Erro, $"Sólido de saída '{id}' não foi gerado", def.Id));

        foreach (var el in def.Planta)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(el.Condicao) && !Expressao.Compilar(el.Condicao!).Avaliar(ctx).ComoBool()) continue;
                var p = AvaliarPlanta(el, ctx);
                if (p is not null) mod.Planta.Add(p);
            }
            catch (ErroExpressao ex) { mod.Mensagens.Add(new(Gravidade.Erro, $"Planta ({el.Tipo}): {ex.Message}", def.Id)); }
        }

        foreach (var q in def.Quantitativos)
        {
            try { mod.Quantitativos.Add((q.Nome, q.Unidade, q.Expr.Avaliar(ctx))); }
            catch (ErroExpressao ex) { mod.Mensagens.Add(new(Gravidade.Aviso, $"Quantitativo '{q.Nome}': {ex.Message}", def.Id)); }
        }
        if (def.RaioFuros is Expr rf && !rf.Vazio)
        {
            try { mod.RaioFuros = rf.Avaliar(ctx); } catch (ErroExpressao ex) { mod.Mensagens.Add(new(Gravidade.Aviso, "RaioFuros: " + ex.Message, def.Id)); }
        }
        return mod;
    }

    private static double N(Expr? e, IContextoExpressao ctx, double padrao = 0) => e is Expr x && !x.Vazio ? x.Avaliar(ctx) : padrao;

    public static P3 Ponto(PontoExpr? p, IContextoExpressao ctx, P3 padrao = default) =>
        p is null ? padrao : new P3(p.X.Vazio ? 0 : p.X.Avaliar(ctx), p.Y.Vazio ? 0 : p.Y.Avaliar(ctx), p.Z.Vazio ? 0 : p.Z.Avaliar(ctx));

    /// <summary>Expande uma lista de pontos/arcos em pontos 2D (X, Y).</summary>
    public static List<P2> Pontos2D(IEnumerable<PontoExpr> pts, IContextoExpressao ctx)
    {
        var res = new List<P2>();
        foreach (var p in pts)
        {
            if (!p.Arco) { res.Add(new P2(p.X.Avaliar(ctx), p.Y.Avaliar(ctx))); continue; }
            double cx = p.X.Avaliar(ctx), cy = p.Y.Avaliar(ctx), r = p.Raio.Avaliar(ctx);
            double a0 = p.De.Avaliar(ctx) * Math.PI / 180, a1 = p.Ate.Avaliar(ctx) * Math.PI / 180;
            int n = Math.Max(2, p.Segmentos);
            for (int k = 0; k <= n; k++)
            {
                double a = a0 + (a1 - a0) * k / n;
                res.Add(new P2(cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
            }
        }
        // remove duplicados consecutivos
        for (int i = res.Count - 1; i > 0; i--) if (res[i].Distancia(res[i - 1]) < 1e-9) res.RemoveAt(i);
        if (res.Count > 2 && res[0].Distancia(res[^1]) < 1e-9) res.RemoveAt(res.Count - 1);
        return res;
    }

    private static Solido? AvaliarOperacao(OperacaoSolida op, IContextoExpressao ctx, Dictionary<string, Solido> existentes)
    {
        Solido Ref(string? id)
        {
            if (id is null || !existentes.TryGetValue(id, out var s)) throw new KeyNotFoundException($"sólido '{id}' não existe (ou não foi criado por condição)");
            return s;
        }
        switch (op.Op.ToLowerInvariant())
        {
            case "caixa":
                {
                    double x = N(op.Comprimento, ctx), y = N(op.Largura, ctx), z = N(op.Altura, ctx);
                    if (x <= 0 || y <= 0 || z <= 0) throw new ArgumentException($"dimensões não positivas ({x:0.###} x {y:0.###} x {z:0.###})");
                    return new SolCaixa(Ponto(op.Base, ctx), x, y, z);
                }
            case "cilindro":
            case "tronco":
                {
                    double r = N(op.Raio, ctx), rt = op.Op.Equals("tronco", StringComparison.OrdinalIgnoreCase) ? N(op.RaioTopo, ctx, r) : r, h = N(op.Altura, ctx);
                    if (r <= 0 || h <= 0 || rt < 0) throw new ArgumentException($"raio/altura inválidos (r={r:0.###}, h={h:0.###})");
                    var eixo = Ponto(op.Eixo, ctx, new P3(0, 0, 1));
                    if (eixo.Comprimento < 1e-9) eixo = new P3(0, 0, 1);
                    return new SolCilindro(Ponto(op.Base, ctx), eixo.Unitario(), r, rt, h);
                }
            case "esfera":
                {
                    double r = N(op.Raio, ctx);
                    if (r <= 0) throw new ArgumentException("raio não positivo");
                    return new SolEsfera(Ponto(op.Base, ctx), r);
                }
            case "extrusao":
                {
                    var pts = Pontos2D(op.Perfil ?? new(), ctx);
                    if (pts.Count < 3) throw new ArgumentException("perfil com menos de 3 pontos");
                    double h = N(op.Altura, ctx);
                    if (Math.Abs(h) < 1e-9) throw new ArgumentException("altura de extrusão nula");
                    Solido s = new SolExtrusao(pts, op.Plano.ToUpperInvariant(), h);
                    var b = Ponto(op.Base, ctx);
                    if (b.Comprimento > 1e-12) s = new SolTransformado(s, Matriz.Translacao(b));
                    return s;
                }
            case "revolucao":
                {
                    var pts = Pontos2D(op.Perfil ?? new(), ctx);
                    if (pts.Count < 3) throw new ArgumentException("perfil com menos de 3 pontos");
                    if (pts.Any(p => p.X < -1e-9)) throw new ArgumentException("perfil de revolução deve ter x (raio) >= 0");
                    double ang = N(op.Angulo, ctx, 360);
                    Solido s = new SolRevolucao(pts, ang);
                    var b = Ponto(op.Base, ctx);
                    if (b.Comprimento > 1e-12) s = new SolTransformado(s, Matriz.Translacao(b));
                    return s;
                }
            case "uniao":
            case "subtrair":
            case "intersecao":
                {
                    var outros = (op.Com ?? new()).Where(existentes.ContainsKey).Select(id => existentes[id]).ToList();
                    var tipo = op.Op.ToLowerInvariant();
                    if (outros.Count == 0) return Ref(op.De); // nada a combinar (ex.: furos condicionais ausentes)
                    return new SolBooleano(tipo, Ref(op.De), outros);
                }
            case "mover":
                return new SolTransformado(Ref(op.Alvo), Matriz.Translacao(Ponto(op.Deslocamento, ctx)));
            case "rotacionar":
                {
                    var eixo = Ponto(op.Eixo, ctx, new P3(0, 0, 1));
                    var centro = Ponto(op.Base, ctx);
                    double ang = N(op.Angulo, ctx) * Math.PI / 180;
                    var m = Matriz.Translacao(centro) * Matriz.Rotacao(eixo, ang) * Matriz.Translacao(centro * -1);
                    return new SolTransformado(Ref(op.Alvo), m);
                }
            case "copiar":
                return Ref(op.Alvo);
        }
        throw new ArgumentException($"operação desconhecida '{op.Op}'");
    }

    private static Planta2D? AvaliarPlanta(ElementoPlanta el, IContextoExpressao ctx)
    {
        var c = Ponto(el.Centro, ctx);
        switch (el.Tipo.ToLowerInvariant())
        {
            case "circulo": return new PlCirculo(c.XY, N(el.Raio, ctx), el.Camada);
            case "retangulo":
                {
                    double w = N(el.Largura, ctx) / 2, h = N(el.Altura, ctx) / 2;
                    return new PlPolilinha(new[] { new P2(c.X - w, c.Y - h), new P2(c.X + w, c.Y - h), new P2(c.X + w, c.Y + h), new P2(c.X - w, c.Y + h) }, true, el.Camada);
                }
            case "linha":
            case "polilinha":
                return new PlPolilinha(Pontos2D(el.Pontos ?? new(), ctx), el.Fechada, el.Camada);
            case "arco":
                return new PlArco(c.XY, N(el.Raio, ctx), N(el.De, ctx), N(el.Ate, ctx), el.Camada);
            case "texto":
                return new PlTexto(c.XY, el.Texto ?? "", N(el.Altura, ctx, 0.2), el.Camada);
        }
        return null;
    }

    // ------------------------------------------------------------------ seções-tipo

    public sealed class SecaoAvaliada
    {
        public SecaoTipo Tipo { get; init; } = new();
        public Dictionary<string, double> Valores { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<P2> Hidraulico { get; init; } = new();
        public List<P2> Concreto { get; init; } = new();
        public List<P2> Escavacao { get; init; } = new();
        public List<Mensagem> Mensagens { get; } = new();
        public double AreaConcreto => Concreto.Count >= 3 ? Geo.Area(Concreto) : 0;
        public double AreaEscavacao => Escavacao.Count >= 3 ? Geo.Area(Escavacao) : 0;
        public double PerimetroRevestido => Hidraulico.Count >= 2 ? Geo.Perimetro(Hidraulico, false) : 0;
        public Hidraulica.SecaoPoligonalAberta? Hidraulica => Hidraulico.Count >= 3 ? new(Hidraulico) : null;
        public bool Ok => Mensagens.All(m => m.Gravidade != Gravidade.Erro);
    }

    public static SecaoAvaliada AvaliarSecao(SecaoTipo tipo, IReadOnlyDictionary<string, double>? valores = null)
    {
        var msgs = new List<Mensagem>();
        var ctx = ResolverParametros(tipo.Parametros, tipo.Variaveis, valores, msgs, tipo.Id);
        List<P2> Lista(List<PontoExpr> l, string nome)
        {
            try { return Pontos2D(l, ctx); }
            catch (ErroExpressao ex) { msgs.Add(new(Gravidade.Erro, $"{nome}: {ex.Message}", tipo.Id)); return new(); }
        }
        var hid = Lista(tipo.ContornoHidraulico, "Contorno hidráulico");
        var conc = Lista(tipo.Concreto, "Concreto");
        var esc = Lista(tipo.Escavacao, "Escavação");
        if (esc.Count < 3 && conc.Count >= 3)
        {
            double x0 = conc.Min(p => p.X), x1 = conc.Max(p => p.X), y0 = conc.Min(p => p.Y), y1 = conc.Max(p => p.Y);
            esc = new() { new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1) };
        }
        var s = new SecaoAvaliada { Tipo = tipo, Hidraulico = hid, Concreto = conc, Escavacao = esc };
        s.Mensagens.AddRange(msgs);
        foreach (var kv in ctx.Variaveis) if (!kv.Value.EhTexto) s.Valores[kv.Key] = kv.Value.Num;
        if (conc.Count >= 3 && Geo.AreaComSinal(conc) < 0) conc.Reverse(); // anti-horário
        if (conc.Count >= 3 && Geo.AutoIntersecta(conc)) s.Mensagens.Add(new(Gravidade.Erro, "O polígono de concreto se cruza (verifique a ordem dos pontos e os nomes dos parâmetros)", tipo.Id));
        return s;
    }
}
