using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Defensas;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Horizontal;
using Projeta.Sinalizacao.Core.Normas;
using Projeta.Sinalizacao.Core.Obras;
using Projeta.Sinalizacao.Core.Semaforica;
using Projeta.Sinalizacao.Core.Vertical;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.Core.Dados;

/// <summary>Resultado da geração de um objeto: desenho e quantidades.</summary>
public sealed class Geracao
{
    /// <summary>Desenho em coordenadas do mundo (marcas, defensas, zonas) ou locais (placas, dispositivos).</summary>
    public Desenho2D Desenho { get; set; } = new();
    public bool Local { get; set; }
    public List<string> Avisos { get; } = new();
    /// <summary>Itens auxiliares para 3D (postes de defensa, posições de dispositivos da zona).</summary>
    public List<(V2 P, double Ang, string Codigo)> Pontos { get; } = new();
}

/// <summary>Configurações de exibição que não são normativas (escala do desenho etc.).</summary>
public sealed class Exibicao
{
    /// <summary>Fator de ampliação dos símbolos de dispositivos (cones, semáforos) em planta.</summary>
    public double FatorDispositivos { get; set; } = 2.5;
    /// <summary>Fator de ampliação das faces de placas na zona de obra.</summary>
    public double FatorPlacasZona { get; set; } = 6.0;
    public double FatorTachas { get; set; } = 3.0;
    public TipoVia Via { get; set; } = TipoVia.Urbana;
}

/// <summary>
/// Gera desenho e quantitativos a partir dos dados gravados (<see cref="ObjetoSin"/>).
/// É o mesmo código usado ao lançar, ao editar e na correção do projeto.
/// </summary>
public static class Motor
{
    public static Geracao Gerar(ObjetoSin o, N n, Exibicao ex)
    {
        o.Quant.Clear();
        return o.Categoria switch
        {
            Categoria.Placa => Placas(o, n, ex),
            Categoria.MarcaLongitudinal => Longitudinal(o, n),
            Categoria.MarcaTransversal => Transversal(o, n),
            Categoria.Inscricao => Inscricao(o, n),
            Categoria.Tachas => TachasG(o, n, ex),
            Categoria.Semaforica or Categoria.Obra => Dispositivo(o, n),
            Categoria.RedeSemaforica => Rede(o),
            Categoria.Defensa => DefensaG(o, n),
            Categoria.ZonaObra => Zona(o, n, ex),
            _ => new Geracao(),
        };
    }

    // ------------------------------------------------------------------ placas
    /// <summary>Face de uma placa do conjunto (m, centrada) e sua área.</summary>
    public static (Desenho2D d, double largura, double altura, double area, string nome, Forma forma) Face(PlacaItem p, N n, TipoVia via)
    {
        if (!string.IsNullOrWhiteSpace(p.Modelo))
        {
            var m = ModeloPlaca.DeJson(p.Modelo!) ?? ModeloPlaca.Exemplo();
            var lay = GeradorIndicacao.Montar(m);
            p.Largura = lay.Largura; p.Altura = lay.Altura;
            return (lay.Desenho, lay.Largura, lay.Altura, lay.Area, m.Nome, Forma.Retangular);
        }
        var t = Catalogo.Obter(p.Codigo);
        if (t is null)
        {
            // código desconhecido: placa retangular genérica com o código escrito
            double w = p.Largura > 0 ? p.Largura : 0.60, h = p.Altura > 0 ? p.Altura : 0.40;
            var d = new Desenho2D();
            Pincel.Retangulo(d, -w / 2, -h / 2, w / 2, h / 2, Cor.Branco);
            Pincel.Tracos(d, new[] { new V2(-w / 2, -h / 2), new V2(w / 2, -h / 2), new V2(w / 2, h / 2), new V2(-w / 2, h / 2) }, 0.02, Cor.Preto, fechado: true);
            Fonte.Desenhar(d, p.Codigo, new V2(0, -h * 0.15), h * 0.3, Cor.Preto, Alinhamento.Centro);
            return (d, w, h, w * h, "Placa " + p.Codigo, Forma.Retangular);
        }
        double dim = p.Dimensao > 0 ? p.Dimensao : GeradorPlaca.DimensaoPadrao(t, n, via);
        p.Dimensao = dim;
        var face = GeradorPlaca.Gerar(t, dim, p.Valor, n, p.Obra);
        return (face, GeradorPlaca.LarguraFace(t.Forma, dim), GeradorPlaca.AlturaFace(t.Forma, dim), GeradorPlaca.Area(t.Forma, dim), t.Nome, t.Forma);
    }

    /// <summary>Conjunto: faces empilhadas (a primeira em cima), base da última em y = 0, centradas em x = 0.</summary>
    public static (Desenho2D d, double alturaTotal, double larguraMax) Conjunto(ObjetoSin o, N n, TipoVia via)
    {
        var dp = o.Placa ?? new DadosPlaca();
        var faces = dp.Placas.Select(p => Face(p, n, via)).ToList();
        double gap = 0.05;
        double H = faces.Sum(f => f.altura) + gap * Math.Max(0, faces.Count - 1);
        var d = new Desenho2D();
        double y = H;
        foreach (var f in faces)
        {
            var c = f.d.Limites();
            double cy = y - f.altura / 2;
            d.Juntar(f.d.Transformado(1, 0, new V2(0, cy - (c.EhVazia ? 0 : (c.YMin + c.YMax) / 2))));
            y -= f.altura + gap;
        }
        return (d, H, faces.Count == 0 ? 0 : faces.Max(f => f.largura));
    }

    private static Geracao Placas(ObjetoSin o, N n, Exibicao ex)
    {
        var g = new Geracao { Local = true };
        var dp = o.Placa ??= new DadosPlaca();
        if (dp.Placas.Count == 0) dp.Placas.Add(new PlacaItem { Codigo = o.Codigo });
        var (d, H, W) = Conjunto(o, n, ex.Via);
        g.Desenho = d;
        double area = 0;
        var nomes = new List<string>();
        foreach (var p in dp.Placas)
        {
            var f = Face(p, n, ex.Via);
            area += f.area;
            nomes.Add(f.nome);
        }
        o.Codigo = string.Join(" + ", dp.Placas.Select(p => string.IsNullOrWhiteSpace(p.Modelo) ? p.Codigo : (ModeloPlaca.DeJson(p.Modelo!)?.Codigo ?? p.Codigo)));
        o.Descricao = string.Join(" + ", nomes);
        o.Quant["area"] = area;
        o.Quant["nPlacas"] = dp.Placas.Count;
        o.Quant["alturaFaces"] = H;
        o.Quant["larguraFaces"] = W;
        var qs = Suportes.Calcular(dp.Suporte, H, n, ex.Via, dp.Vao, dp.MaterialSuporte);
        o.Quant["suporteColunas"] = qs.NumColunas;
        o.Quant["suporteComprColuna"] = qs.ComprimentoColuna;
        o.Quant["suporteViga"] = qs.ComprimentoViga;
        o.Quant["suporteTotal"] = qs.ComprimentoTotal;
        if (string.IsNullOrWhiteSpace(dp.Chapa)) dp.Chapa = n.ChapaPadrao;
        if (string.IsNullOrWhiteSpace(dp.Pelicula)) dp.Pelicula = n.PeliculaPadrao;
        return g;
    }

    // ------------------------------------------------------------------ marcas
    private static Geracao Longitudinal(ObjetoSin o, N n)
    {
        var g = new Geracao();
        var c = o.CaminhoObj;
        if (c is null) { g.Avisos.Add("Marca sem caminho."); return g; }
        var m = o.Marca ??= new DadosMarca();
        var r = GeradorLongitudinal.Gerar(c, new ParamLongitudinal
        {
            Codigo = o.Codigo, Velocidade = m.Velocidade, Deslocamento = m.Deslocamento, Largura = m.Largura,
            Traco = m.Traco, Espaco = m.Espaco, Fase = m.Fase, InverterLados = m.InverterLados, Cor = m.Cor,
        }, n);
        g.Desenho = r.Desenho;
        o.Descricao = CatalogoLongitudinal.Obter(o.Codigo)?.Nome ?? o.Codigo;
        o.Quant["comp"] = r.ComprimentoEixo;
        o.Quant["compPintado"] = r.ComprimentoPintado;
        o.Quant["area"] = r.Area;
        o.Quant["largura"] = r.Largura;
        o.Quant["traco"] = r.Traco;
        o.Quant["espaco"] = r.Espaco;
        o.Quant["nTracos"] = r.NumTracos;
        o.Quant["cor"] = (int)r.Cor;
        if (string.IsNullOrWhiteSpace(m.Material)) m.Material = n.MaterialPinturaPadrao;
        return g;
    }

    private static Geracao Transversal(ObjetoSin o, N n)
    {
        var g = new Geracao();
        var m = o.Marca ??= new DadosMarca();
        var p1 = new V2(m.X1, m.Y1);
        var p2 = new V2(m.X2, m.Y2);
        ResultadoPeca r;
        switch (o.Codigo.ToUpperInvariant())
        {
            case "LRE": r = GeradorTransversal.LRE(p1, p2, m.Largura ?? n.LarguraLRE); break;
            case "LDP": r = GeradorTransversal.LDP(p1, p2, m.Largura ?? n.LarguraLDP, m.Traco ?? n.TracoLDP, m.Espaco ?? n.EspacoLDP); break;
            case "FTP-1": r = GeradorTransversal.FTP1(p1, p2, m.Extensao > 0 ? m.Extensao : n.FTP1Extensao, m.Largura ?? n.FTP1Largura, m.Espaco ?? n.FTP1Espaco); break;
            case "FTP-2": r = GeradorTransversal.FTP2(p1, p2, m.Extensao > 0 ? m.Extensao : n.FTP2Extensao, m.Largura ?? n.FTP2Largura); break;
            case "LRV":
                r = GeradorTransversal.LRV(p1, p2, m.V1 > 0 ? m.V1 : 60, m.V2 > 0 ? m.V2 : 30, m.NumLinhas > 1 ? m.NumLinhas : 8,
                    m.Extensao > 0 ? m.Extensao : 60, m.Largura ?? N.PorVelocidade(n.LarguraLRV, m.V1 > 0 ? m.V1 : 60), m.FluxoAEsquerda);
                break;
            case "MCC": r = GeradorTransversal.MCC(p1, p2, m.Extensao > 0 ? m.Extensao : 2.5, m.Largura ?? n.MCCLado); break;
            case "ZPA":
            {
                var cont = o.Caminho?.Select(v => v.P).ToList() ?? new List<V2>();
                r = GeradorTransversal.ZPA(cont, m.AnguloFluxo, m.LarguraListra > 0 ? m.LarguraListra : n.ZPALinha,
                    m.EspacoListra > 0 ? m.EspacoListra : n.ZPAEspaco, m.LarguraContorno, m.Cor ?? Cor.Branco);
                break;
            }
            default:
                g.Avisos.Add($"Marca transversal desconhecida: {o.Codigo}");
                return g;
        }
        g.Desenho = r.Desenho;
        o.Descricao = r.Descricao;
        o.Quant["area"] = r.Area;
        o.Quant["comp"] = r.Comprimento;
        o.Quant["pecas"] = r.Pecas;
        o.Quant["cor"] = (int)(m.Cor ?? r.Cor);
        if (string.IsNullOrWhiteSpace(m.Material)) m.Material = n.MaterialPinturaPadrao;
        return g;
    }

    private static Geracao Inscricao(ObjetoSin o, N n)
    {
        var g = new Geracao();
        var m = o.Marca ??= new DadosMarca();
        ResultadoPeca r;
        string cod = o.Codigo.ToUpperInvariant();
        if (cod == "LEG")
        {
            double h = m.AlturaLetra > 0 ? m.AlturaLetra : N.PorVelocidade(n.AlturaLegenda, m.Velocidade);
            r = Inscricoes.Legenda(m.Texto, h, m.FatorLargura > 0 ? m.FatorLargura : n.FatorLarguraLegenda, m.EspacoLinhas > 0 ? m.EspacoLinhas : h);
        }
        else if (cod.StartsWith("PEM") || cod.StartsWith("MOF"))
        {
            double L = m.Comprimento > 0 ? m.Comprimento : N.PorVelocidade(n.ComprimentoSetaUrbana, m.Velocidade);
            r = Inscricoes.Seta(cod, L);
        }
        else r = Inscricoes.Simbolo(cod, m.Escala > 0 ? m.Escala : 1);
        // local → mundo (o referencial local aponta para +Y; Rotacao é a direção do tráfego)
        g.Desenho = r.Desenho.Transformado(1, o.Rotacao - Math.PI / 2, new V2(o.X, o.Y));
        o.Descricao = r.Descricao;
        o.Quant["area"] = r.Area;
        o.Quant["comp"] = r.Comprimento;
        o.Quant["pecas"] = r.Pecas;
        o.Quant["cor"] = (int)Cor.Branco;
        if (string.IsNullOrWhiteSpace(m.Material)) m.Material = n.MaterialPinturaPadrao;
        return g;
    }

    private static Geracao TachasG(ObjetoSin o, N n, Exibicao ex)
    {
        var g = new Geracao();
        var c = o.CaminhoObj;
        if (c is null) { g.Avisos.Add("Tachas sem caminho."); return g; }
        var m = o.Marca ??= new DadosMarca();
        double er = m.EspacamentoReta > 0 ? m.EspacamentoReta : n.EspacamentoTachasReta;
        double ec = m.EspacamentoCurva > 0 ? m.EspacamentoCurva : n.EspacamentoTachasCurva;
        var pos = Tachas.Posicoes(c, er, ec, n.RaioCurvaTachas, m.Fase);
        var cor = m.Cor ?? Cor.Branco;
        var sim = Tachas.Planta(o.Codigo, cor, ex.FatorTachas);
        foreach (var s in pos)
        {
            var p = c.PontoDeslocado(s, m.Deslocamento);
            double ang = c.Tangente(s).Angulo;
            g.Desenho.Juntar(sim.Transformado(1, ang, p));
            g.Pontos.Add((p, ang, o.Codigo));
        }
        o.Descricao = (Tachas.Tipos.FirstOrDefault(t => t.Codigo == o.Codigo)?.Nome ?? o.Codigo) + " " + Cores.Nome(cor);
        o.Quant["un"] = pos.Count;
        o.Quant["comp"] = c.Comprimento;
        o.Quant["esp"] = er;
        o.Quant["cor"] = (int)cor;
        return g;
    }

    // ------------------------------------------------------------------ semafórica / obras
    private static Geracao Dispositivo(ObjetoSin o, N n)
    {
        var g = new Geracao { Local = true };
        var dd = o.Dispositivo ??= new DadosDispositivo();
        var tipo = CatalogoSemaforica.Obter(o.Codigo);
        double braco = dd.Braco > 0 ? dd.Braco : n.ComprimentoBracoProjetado;
        if (o.Codigo == "COL-BP") dd.Braco = braco;
        g.Desenho = CatalogoSemaforica.Planta(o.Codigo, braco);
        o.Descricao = tipo?.Nome ?? o.Codigo;
        if (dd.Altura <= 0) dd.Altura = tipo?.Altura ?? 1;
        o.Quant["un"] = 1;
        if (o.Codigo == "COL-BP") o.Quant["braco"] = braco;
        return g;
    }

    private static Geracao Rede(ObjetoSin o)
    {
        var g = new Geracao();
        var c = o.CaminhoObj;
        if (c is null) { g.Avisos.Add("Rede sem caminho."); return g; }
        bool sub = o.Codigo == "REDE-SUB";
        var pts = c.Trecho(0, c.Comprimento, 0.05);
        if (sub)
        {
            // tracejado largo
            for (double s = 0; s < c.Comprimento; s += 3.0)
                g.Desenho.Area(c.Faixa(s, Math.Min(c.Comprimento, s + 2.0), 0, 0.12), Cor.Laranja);
        }
        else g.Desenho.Linha(pts, Cor.Laranja);
        o.Descricao = CatalogoSemaforica.Redes.FirstOrDefault(r => r.Codigo == o.Codigo).Nome ?? o.Codigo;
        o.Quant["comp"] = c.Comprimento;
        return g;
    }

    private static Geracao DefensaG(ObjetoSin o, N n)
    {
        var g = new Geracao();
        var c = o.CaminhoObj;
        if (c is null) { g.Avisos.Add("Defensa sem caminho."); return g; }
        var p = o.Defensa ??= new ParamDefensa();
        var r = GeradorDefensa.Gerar(c, p, n);
        g.Desenho = r.Desenho;
        g.Avisos.AddRange(r.Avisos);
        foreach (var s in r.Postes) g.Pontos.Add((c.Ponto(s), c.Tangente(s).Angulo, "POSTE"));
        o.Codigo = p.Tipo.ToString();
        o.Descricao = GeradorDefensa.Nome(p.Tipo);
        o.Quant["comp"] = r.Comprimento;
        o.Quant["compCorrente"] = r.ComprimentoCorrente;
        o.Quant["compTerminais"] = r.ComprimentoTerminais;
        o.Quant["postes"] = r.NumPostes;
        o.Quant["laminas"] = r.NumLaminas;
        o.Quant["terminais"] = r.NumTerminais;
        return g;
    }

    private static Geracao Zona(ObjetoSin o, N n, Exibicao ex)
    {
        var g = new Geracao();
        var c = o.CaminhoObj;
        if (c is null) { g.Avisos.Add("Zona de obra sem caminho."); return g; }
        var p = o.Zona ??= new ParamZonaObra();
        var r = ZonaObra.Calcular(c.Comprimento, p, n);
        g.Avisos.AddRange(r.Avisos);
        var sim = CatalogoObras.Planta(p.Dispositivo).Transformado(ex.FatorDispositivos);
        int nDisp = 0, nPlacas = 0;
        double areaPlacas = 0;
        foreach (var it in r.Itens)
        {
            var pt = c.PontoDeslocado(it.S, it.Desloc);
            double ang = c.Tangente(it.S).Angulo;
            if (it.Tipo == "DISP")
            {
                g.Desenho.Juntar(sim.Transformado(1, ang, pt));
                nDisp++;
            }
            else
            {
                var pi = new PlacaItem { Codigo = it.Codigo, Valor = it.Valor, Obra = it.Codigo.StartsWith("A") };
                var f = Face(pi, n, ex.Via);
                areaPlacas += f.area;
                nPlacas++;
                // face em pé (sem rotação) ao lado do ponto, com marcador do suporte
                g.Desenho.Disco(pt, 0.25, Cor.Preto);
                var c0 = f.d.Limites();
                g.Desenho.Juntar(f.d.Transformado(ex.FatorPlacasZona, 0, pt + new V2(0, f.altura * ex.FatorPlacasZona / 2 + 0.6) - c0.Centro * ex.FatorPlacasZona));
            }
            g.Pontos.Add((pt, ang, it.Tipo == "DISP" ? p.Dispositivo : it.Codigo));
        }
        o.Descricao = $"Zona de obra — {CatalogoObras.Obter(p.Dispositivo)?.Nome ?? p.Dispositivo}";
        o.Quant["dispositivos"] = nDisp;
        o.Quant["placas"] = nPlacas;
        o.Quant["areaPlacas"] = areaPlacas;
        o.Quant["taper"] = r.Taper;
        o.Quant["protecao"] = r.ZonaProtecao;
        o.Quant["extensao"] = r.FimSinalizacao - r.InicioSinalizacao;
        o.Quant["espTaper"] = r.EspacamentoTaper;
        o.Quant["espLong"] = r.EspacamentoLongitudinal;
        return g;
    }
}
