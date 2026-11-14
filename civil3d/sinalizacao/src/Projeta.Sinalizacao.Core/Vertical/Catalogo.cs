using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;

namespace Projeta.Sinalizacao.Core.Vertical;

/// <summary>
/// Catálogo de placas de regulamentação (R-1 a R-40) e advertência (A-1a a A-48) com nomes do CONTRAN
/// e pictogramas desenhados por este plugin (simplificados para projeto em planta).
/// </summary>
public static class Catalogo
{
    private static readonly List<TipoPlaca> _tipos = new();
    private static readonly Dictionary<string, TipoPlaca> _porCodigo = new(StringComparer.OrdinalIgnoreCase);
    public static IReadOnlyList<TipoPlaca> Tipos => _tipos;

    public static TipoPlaca? Obter(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var c = codigo.Trim().ToUpperInvariant().Replace(" ", "");
        // aceita "R1", "r-19", "A-1A"
        if (_porCodigo.TryGetValue(c, out var t)) return t;
        if (c.Length > 1 && char.IsLetter(c[0]) && c[1] != '-' && _porCodigo.TryGetValue(c[0] + "-" + c[1..], out t)) return t;
        var semSufixo = c.Length > 0 && char.IsLetter(c[^1]) ? c[..^1] + char.ToLowerInvariant(c[^1]) : c;
        return _porCodigo.TryGetValue(semSufixo, out t) ? t : null;
    }

    private static void R(string cod, string nome, Action<Pic>? picto = null, bool proib = false, string? valor = null, Forma forma = Forma.Circular, string? espelho = null)
        => Add(new TipoPlaca { Codigo = cod, Nome = nome, Familia = Familia.Regulamentacao, Forma = forma, Picto = picto, Proibicao = proib, ValorPadrao = valor, EspelhoDe = espelho });

    private static void A(string cod, string nome, Action<Pic>? picto = null, string? valor = null, string? espelho = null, Forma forma = Forma.Losango)
        => Add(new TipoPlaca { Codigo = cod, Nome = nome, Familia = Familia.Advertencia, Forma = forma, Picto = picto, ValorPadrao = valor, EspelhoDe = espelho });

    private static void Add(TipoPlaca t) { _tipos.Add(t); _porCodigo[t.Codigo.ToUpperInvariant()] = t; _porCodigo[t.Codigo] = t; }

    // ------------------------------------------------------------------ peças reutilizáveis
    private static List<V2> V(params double[] xy) { var l = new List<V2>(); for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new V2(xy[i], xy[i + 1])); return l; }

    private static List<V2> Concat(params IEnumerable<V2>[] partes)
    {
        var l = new List<V2>();
        foreach (var p in partes) foreach (var v in p) if (l.Count == 0 || V2.Dist(l[^1], v) > 1e-9) l.Add(v);
        return l;
    }

    private static void VireEsquerda(Pic p, double x = 0.32, double yIni = -0.85, double yCurva = 0.0, double r = 0.38, double xFim = -0.78)
        => p.SetaL(Concat(V(x, yIni, x, yCurva), Pic.Arco(x - r, yCurva, r, 0, 90, 10), V(xFim, yCurva + r)));

    private static void Retorno(Pic p)
        => p.SetaL(Concat(V(0.45, -0.85, 0.45, 0.20), Pic.Arco(-0.05, 0.20, 0.50, 0, 180, 16), V(-0.55, -0.50)));

    private static void Rotatoria(Pic p, double r = 0.55)
    {
        for (int i = 0; i < 3; i++)
        {
            double a0 = 100 + 120 * i;
            p.SetaL(Pic.Arco(0, 0, r, a0, a0 + 85, 10));
        }
    }

    private static void DuasSetasOpostas(Pic p, double x = 0.35)
    {
        p.Seta(-x, 0.85, -x, -0.85);
        p.Seta(x, -0.85, x, 0.85);
    }

    private static void Zebra(Pic p, double y0 = -0.95, double y1 = -0.62)
    {
        for (int i = -2; i <= 2; i++)
        {
            double x = i * 0.30;
            p.Ar(x - 0.09, y0, x + 0.09, y0, x + 0.09, y1, x - 0.09, y1);
        }
    }

    private static void SetasDimensaoVertical(Pic p)
    {
        p.Ar(-0.28, 0.86, 0.28, 0.86, 0, 0.50);
        p.Ar(-0.28, -0.86, 0.28, -0.86, 0, -0.50);
    }

    private static void SetasDimensaoHorizontal(Pic p)
    {
        p.Ar(-0.95, 0.30, -0.95, -0.30, -0.62, 0);
        p.Ar(0.95, 0.30, 0.95, -0.30, 0.62, 0);
    }

    private static void Eixo(Pic p, double y)
    {
        p.Ar(-0.55, y - 0.04, 0.55, y - 0.04, 0.55, y + 0.04, -0.55, y + 0.04);
        p.Ar(-0.85, y - 0.24, -0.55, y - 0.24, -0.55, y + 0.24, -0.85, y + 0.24);
        p.Ar(0.55, y - 0.24, 0.85, y - 0.24, 0.85, y + 0.24, 0.55, y + 0.24);
    }

    private static List<V2> Senoide(double amp, double ciclos, double y0, double y1, int n = 40, double fase = 0)
    {
        var l = new List<V2>();
        for (int i = 0; i <= n; i++)
        {
            double t = i / (double)n;
            l.Add(new V2(-amp * Math.Sin(2 * Math.PI * ciclos * t + fase), y0 + (y1 - y0) * t));
        }
        return l;
    }

    private static List<V2> Bezier(V2 a, V2 b, V2 c, V2 d, int n = 24)
    {
        var l = new List<V2>();
        for (int i = 0; i <= n; i++)
        {
            double t = i / (double)n, u = 1 - t;
            l.Add(a * (u * u * u) + b * (3 * u * u * t) + c * (3 * u * t * t) + d * (t * t * t));
        }
        return l;
    }

    static Catalogo()
    {
        // ============================================================ REGULAMENTAÇÃO
        R("R-1", "Parada obrigatória", forma: Forma.Octogonal);
        R("R-2", "Dê a preferência", forma: Forma.Triangular);
        R("R-3", "Sentido proibido");
        R("R-4a", "Proibido virar à esquerda", p => VireEsquerda(p), proib: true);
        R("R-4b", "Proibido virar à direita", proib: true, espelho: "R-4a");
        R("R-5a", "Proibido retornar à esquerda", Retorno, proib: true);
        R("R-5b", "Proibido retornar à direita", proib: true, espelho: "R-5a");
        R("R-6a", "Proibido estacionar", p => p.Texto("E", 0, 0, 1.25), proib: true);
        R("R-6b", "Estacionamento regulamentado", p => p.Texto("E", 0, 0, 1.25));
        R("R-6c", "Proibido parar e estacionar", p => p.Texto("E", 0, 0, 1.25), proib: true);
        R("R-7", "Proibido ultrapassar", p =>
        {
            Figuras.CarroTraseira(p.D, p.P(-0.46, -0.05), 0.85 * p.K, Cor.Vermelho);
            Figuras.CarroTraseira(p.D, p.P(0.46, -0.05), 0.85 * p.K, Cor.Preto);
        }, proib: true);
        R("R-8a", "Proibido mudar de faixa ou pista de trânsito da esquerda para direita", p =>
        {
            p.TrE(0.08, 0, -0.95, 0, -0.55); p.TrE(0.08, 0, -0.25, 0, 0.15); p.TrE(0.08, 0, 0.45, 0, 0.85);
            p.SetaL(Concat(V(-0.45, -0.85, -0.45, -0.35), Bezier(new V2(-0.45, -0.35), new V2(-0.45, 0.05), new V2(0.45, -0.05), new V2(0.45, 0.35)), V(0.45, 0.85)));
        }, proib: true);
        R("R-8b", "Proibido mudar de faixa ou pista de trânsito da direita para esquerda", proib: true, espelho: "R-8a");
        R("R-9", "Proibido trânsito de caminhões", p => p.Fig(Figuras.Caminhao, 0, -0.1, 0.92), proib: true);
        R("R-10", "Proibido trânsito de veículos automotores", p => p.Fig(Figuras.CarroLateral, 0, -0.05, 0.92), proib: true);
        R("R-11", "Proibido trânsito de veículos de tração animal", p => p.Fig(Figuras.Carroca, 0, 0, 0.88), proib: true);
        R("R-12", "Proibido trânsito de bicicletas", p => p.Fig(Figuras.Bicicleta, 0, 0, 1.0), proib: true);
        R("R-13", "Proibido trânsito de tratores e máquinas de obras", p => p.Fig(Figuras.Trator, 0, 0.05, 0.9), proib: true);
        R("R-14", "Peso bruto total máximo permitido", p => p.Texto(p.Valor, 0, 0, 0.8), valor: "10 t");
        R("R-15", "Altura máxima permitida", p => { SetasDimensaoVertical(p); p.Texto(p.Valor, 0, 0, 0.55, 1.8); }, valor: "3,0 m");
        R("R-16", "Largura máxima permitida", p => { SetasDimensaoHorizontal(p); p.Texto(p.Valor, 0, 0, 0.55, 1.15); }, valor: "2,5 m");
        R("R-17", "Peso máximo permitido por eixo", p => { Eixo(p, 0.42); p.Texto(p.Valor, 0, -0.35, 0.6); }, valor: "5 t");
        R("R-18", "Comprimento máximo permitido", p =>
        {
            p.E = 0.10; p.Seta(0, 0.45, 0.85, 0.45); p.Seta(0, 0.45, -0.85, 0.45);
            p.Texto(p.Valor, 0, -0.25, 0.6);
        }, valor: "15 m");
        R("R-19", "Velocidade máxima permitida", p =>
        {
            p.Texto(p.Valor, 0, 0.12, 0.95, 1.8);
            p.Texto("km/h", 0, -0.62, 0.26);
        }, valor: "60");
        R("R-20", "Proibido acionar buzina ou sinal sonoro", p => p.Fig((d, c, s, cor, e) => Figuras.Buzina(d, c, s, cor), 0, 0, 0.95), proib: true);
        R("R-21", "Alfândega", p => p.Texto("ALFÂNDEGA", 0, 0, 0.30, 1.9));
        R("R-22", "Uso obrigatório de corrente", p => p.Fig((d, c, s, cor, e) => Figuras.PneuCorrente(d, c, s, cor), 0, 0, 1.05));
        R("R-23", "Conserve-se à direita", p => { p.Seta(-0.55, 0.55, 0.55, -0.55); });
        R("R-24a", "Sentido de circulação da via/pista", p => p.Seta(-0.85, 0, 0.85, 0));
        R("R-24b", "Passagem obrigatória", p => { p.TrE(0.14, -0.9, 0.55, -0.3, 0.55); p.Seta(-0.3, 0.55, 0.45, -0.25, 0.45, -0.85); });
        R("R-25a", "Vire à esquerda", p => VireEsquerda(p));
        R("R-25b", "Vire à direita", espelho: "R-25a");
        R("R-25c", "Siga em frente ou à esquerda", p =>
        {
            p.Seta(0.32, -0.85, 0.32, 0.85);
            p.SetaL(Concat(V(0.32, -0.30), Pic.Arco(-0.06, -0.30, 0.38, 0, 90, 10), V(-0.80, 0.08)));
        });
        R("R-25d", "Siga em frente ou à direita", espelho: "R-25c");
        R("R-26", "Siga em frente", p => p.Seta(0, -0.85, 0, 0.85));
        R("R-27", "Ônibus, caminhões e veículos de grande porte mantenham-se à direita", p =>
        {
            p.Fig(Figuras.Caminhao, 0, 0.25, 0.8);
            p.E = 0.14; p.Seta(-0.55, -0.55, 0.65, -0.55);
        });
        R("R-28", "Duplo sentido de circulação", p => DuasSetasOpostas(p));
        R("R-29", "Proibido trânsito de pedestres", p => p.Fig(Figuras.Pessoa, 0, 0, 0.95), proib: true);
        R("R-30", "Pedestre, ande pela esquerda", p => { p.Fig(Figuras.Pessoa, 0.30, 0.15, 0.78); p.E = 0.14; p.Seta(0.2, -0.70, -0.85, -0.70); });
        R("R-31", "Pedestre, ande pela direita", p => { p.Fig(Figuras.Pessoa, -0.30, 0.15, 0.78, true); p.E = 0.14; p.Seta(-0.2, -0.70, 0.85, -0.70); });
        R("R-32", "Circulação exclusiva de ônibus", p => p.Fig(Figuras.Onibus, 0, -0.1, 0.95));
        R("R-33", "Sentido de circulação na rotatória", p => Rotatoria(p));
        R("R-34", "Circulação exclusiva de bicicletas", p => p.Fig(Figuras.Bicicleta, 0, 0, 1.0));
        R("R-35a", "Ciclista, transite à esquerda", p => { p.Fig(Figuras.Bicicleta, 0.10, 0.25, 0.78); p.E = 0.14; p.Seta(0.4, -0.65, -0.85, -0.65); });
        R("R-35b", "Ciclista, transite à direita", p => { p.Fig(Figuras.Bicicleta, -0.10, 0.25, 0.78); p.E = 0.14; p.Seta(-0.4, -0.65, 0.85, -0.65); });
        R("R-36a", "Ciclistas à esquerda, pedestres à direita", p =>
        {
            p.TrE(0.08, 0, -0.95, 0, 0.95);
            p.Fig(Figuras.Bicicleta, -0.50, 0, 0.42);
            p.Fig(Figuras.Pessoa, 0.48, 0, 0.62);
        });
        R("R-36b", "Pedestres à esquerda, ciclistas à direita", p =>
        {
            p.TrE(0.08, 0, -0.95, 0, 0.95);
            p.Fig(Figuras.Pessoa, -0.48, 0, 0.62);
            p.Fig(Figuras.Bicicleta, 0.50, 0, 0.42);
        });
        R("R-37", "Proibido trânsito de motocicletas, motonetas e ciclomotores", p => p.Fig(Figuras.Moto, 0, -0.05, 0.95), proib: true);
        R("R-38", "Proibido trânsito de ônibus", p => p.Fig(Figuras.Onibus, 0, -0.1, 0.95), proib: true);
        R("R-39", "Circulação exclusiva de caminhão", p => p.Fig(Figuras.Caminhao, 0, -0.1, 0.92));
        R("R-40", "Trânsito proibido a carros de mão", p => p.Fig(Figuras.CarroDeMao, 0, 0.05, 0.85), proib: true);

        // ============================================================ ADVERTÊNCIA
        A("A-1a", "Curva acentuada à esquerda", p => { p.E = 0.22; VireEsquerda(p, 0.30, -0.85, 0.02, 0.36, -0.72); });
        A("A-1b", "Curva acentuada à direita", espelho: "A-1a");
        A("A-2a", "Curva à esquerda", p => { p.E = 0.22; p.SetaL(Concat(V(0.28, -0.85, 0.28, -0.25), Pic.Arco(-0.52, -0.25, 0.80, 0, 62, 12))); });
        A("A-2b", "Curva à direita", espelho: "A-2a");
        A("A-3a", "Pista sinuosa à esquerda", p => { p.E = 0.20; p.SetaL(Senoide(0.28, 1.25, -0.85, 0.82)); });
        A("A-3b", "Pista sinuosa à direita", espelho: "A-3a");
        A("A-4a", "Curva acentuada em \"S\" à esquerda", p =>
        {
            p.E = 0.20;
            p.SetaL(Concat(V(0.32, -0.88, 0.32, -0.45), Pic.Arco(0.02, -0.45, 0.30, 0, 90, 8), V(-0.02, -0.15), Pic.Arco(-0.02, 0.15, 0.30, 270, 180, 8), V(-0.32, 0.85)));
        });
        A("A-4b", "Curva acentuada em \"S\" à direita", espelho: "A-4a");
        A("A-5a", "Curva em \"S\" à esquerda", p => { p.E = 0.20; p.SetaL(Concat(Bezier(new V2(0.30, -0.88), new V2(0.30, 0.0), new V2(-0.30, 0.0), new V2(-0.30, 0.55)), V(-0.30, 0.85))); });
        A("A-5b", "Curva em \"S\" à direita", espelho: "A-5a");
        A("A-6", "Cruzamento de vias", p => { p.E = 0.22; p.Tr(0, -0.95, 0, 0.95); p.Tr(-0.95, 0, 0.95, 0); });
        A("A-7a", "Via lateral à esquerda", p => { p.E = 0.22; p.Tr(0.1, -0.95, 0.1, 0.95); p.E = 0.18; p.Tr(0.1, 0.12, -0.80, 0.12); });
        A("A-7b", "Via lateral à direita", espelho: "A-7a");
        A("A-8", "Interseção em \"T\"", p => { p.E = 0.22; p.Tr(-0.80, 0.40, 0.80, 0.40); p.Tr(0, -0.92, 0, 0.40); });
        A("A-9", "Bifurcação em \"Y\"", p => { p.E = 0.22; p.Tr(0, -0.92, 0, -0.05); p.Tr(0, -0.05, -0.50, 0.65); p.Tr(0, -0.05, 0.50, 0.65); });
        A("A-10a", "Entroncamento oblíquo à esquerda", p => { p.E = 0.22; p.Tr(0.1, -0.95, 0.1, 0.95); p.E = 0.18; p.Tr(0.1, -0.05, -0.55, 0.60); });
        A("A-10b", "Entroncamento oblíquo à direita", espelho: "A-10a");
        A("A-11a", "Junções sucessivas contrárias — primeira à esquerda", p => { p.E = 0.22; p.Tr(0, -0.95, 0, 0.95); p.E = 0.17; p.Tr(0, -0.35, -0.70, -0.35); p.Tr(0, 0.35, 0.70, 0.35); });
        A("A-11b", "Junções sucessivas contrárias — primeira à direita", espelho: "A-11a");
        A("A-12", "Interseção em círculo", p => { p.E = 0.16; Rotatoria(p, 0.50); });
        A("A-13a", "Confluência à esquerda", p => { p.E = 0.22; p.Tr(0.15, -0.95, 0.15, 0.95); p.E = 0.17; p.TrL(Concat(V(-0.62, -0.75), Bezier(new V2(-0.62, -0.75), new V2(-0.55, -0.25), new V2(0.05, -0.10), new V2(0.15, 0.30)))); });
        A("A-13b", "Confluência à direita", espelho: "A-13a");
        A("A-14", "Semáforo à frente", p => Figuras.Semaforo(p.D, V2.Zero, 0.80 * p.K));
        A("A-15", "Parada obrigatória à frente", p =>
        {
            var oct = Poligonos.PoligonoRegular(p.P(0, 0.30), 0.50 * p.K, 8, Math.PI / 8);
            p.D.Area(oct, Cor.Vermelho);
            p.D.Area(Poligonos.PoligonoRegular(p.P(0, 0.30), 0.43 * p.K, 8, Math.PI / 8), Cor.Branco);
            p.D.Area(Poligonos.PoligonoRegular(p.P(0, 0.30), 0.38 * p.K, 8, Math.PI / 8), Cor.Vermelho);
            p.E = 0.16; p.Tr(0, -0.95, 0, -0.25);
        });
        A("A-16", "Bonde", p => p.Fig((d, c, s, cor, e) => Figuras.Bonde(d, c, s, cor), 0, 0, 0.8));
        A("A-17", "Pista irregular", p => p.Fig((d, c, s, cor, e) => Figuras.PistaIrregular(d, c, s, cor), 0, 0, 0.85));
        A("A-18", "Saliência ou lombada", p => p.Fig((d, c, s, cor, e) => Figuras.Lombada(d, c, s, cor), 0, 0.05, 0.9));
        A("A-19", "Depressão", p => p.Fig((d, c, s, cor, e) => Figuras.Depressao(d, c, s, cor), 0, 0.05, 0.9));
        A("A-20a", "Declive acentuado", p => { p.Ar(-0.78, -0.55, 0.78, -0.55, -0.78, 0.25); p.Texto(p.Valor, 0.28, 0.12, 0.34, 0.8); }, valor: "10%");
        A("A-20b", "Aclive acentuado", p => { p.Ar(-0.78, -0.55, 0.78, -0.55, 0.78, 0.25); p.Texto(p.Valor, -0.28, 0.12, 0.34, 0.8); }, valor: "10%");
        A("A-21a", "Estreitamento de pista ao centro", p => { p.E = 0.18; p.Tr(-0.52, -0.92, -0.52, -0.25, -0.22, 0.20, -0.22, 0.92); p.Tr(0.52, -0.92, 0.52, -0.25, 0.22, 0.20, 0.22, 0.92); });
        A("A-21b", "Estreitamento de pista à esquerda", p => { p.E = 0.18; p.Tr(-0.52, -0.92, -0.52, -0.25, -0.08, 0.20, -0.08, 0.92); p.Tr(0.42, -0.92, 0.42, 0.92); });
        A("A-21c", "Estreitamento de pista à direita", espelho: "A-21b");
        A("A-21d", "Alargamento de pista à esquerda", p => { p.E = 0.18; p.Tr(-0.08, -0.92, -0.08, -0.25, -0.52, 0.20, -0.52, 0.92); p.Tr(0.42, -0.92, 0.42, 0.92); });
        A("A-21e", "Alargamento de pista à direita", espelho: "A-21d");
        A("A-22", "Ponte estreita", p =>
        {
            p.E = 0.16; p.Tr(-0.52, -0.92, -0.52, -0.35, -0.24, 0.0, -0.24, 0.92); p.Tr(0.52, -0.92, 0.52, -0.35, 0.24, 0.0, 0.24, 0.92);
            for (double y = 0.15; y < 0.9; y += 0.25) { p.TrE(0.08, -0.24, y, -0.42, y); p.TrE(0.08, 0.24, y, 0.42, y); }
        });
        A("A-23", "Ponte móvel", p =>
        {
            p.Ar(-0.85, -0.25, -0.12, 0.30, -0.12, 0.48, -0.85, -0.07);
            p.Ar(0.85, -0.25, 0.12, 0.30, 0.12, 0.48, 0.85, -0.07);
            p.E = 0.07; p.TrL(Senoide(0.06, 3, -0.85, 0.85).Select(v => new V2(v.Y, -0.62 + v.X)).ToList());
        });
        A("A-24", "Obras", p => p.Fig(Figuras.Trabalhador, 0, 0, 0.88));
        A("A-25", "Mão dupla adiante", p => { p.E = 0.18; DuasSetasOpostas(p, 0.32); });
        A("A-26a", "Sentido único", p => { p.E = 0.22; p.Seta(-0.85, 0, 0.85, 0); });
        A("A-26b", "Sentido duplo", p => { p.E = 0.16; p.Seta(-0.70, 0.28, 0.70, 0.28); p.Seta(0.70, -0.28, -0.70, -0.28); });
        A("A-27", "Área com desmoronamento", p =>
        {
            p.Ar(-0.80, 0.70, -0.80, -0.70, 0.55, -0.70);
            p.Disco(0.20, -0.20, 0.12); p.Disco(0.45, -0.48, 0.10); p.Disco(0.02, 0.18, 0.09);
        });
        A("A-28", "Pista escorregadia", p =>
        {
            p.Fig(Figuras.CarroLateral, 0, 0.38, 0.58);
            p.E = 0.09;
            p.TrL(Senoide(0.14, 1.2, -0.85, 0.0).Select(v => v + new V2(-0.28, 0)).ToList());
            p.TrL(Senoide(0.14, 1.2, -0.85, 0.0, fase: Math.PI).Select(v => v + new V2(0.28, 0)).ToList());
        });
        A("A-29", "Projeção de cascalho", p =>
        {
            p.Fig(Figuras.CarroLateral, 0.15, -0.25, 0.60);
            foreach (var (x, y, r) in new[] { (-0.55, 0.25, 0.08), (-0.30, 0.45, 0.07), (-0.62, 0.55, 0.06), (-0.10, 0.62, 0.06), (-0.45, 0.02, 0.06) }) p.Disco(x, y, r);
        });
        A("A-30a", "Trânsito de ciclistas", p => p.Fig(Figuras.Ciclista, 0, -0.05, 0.85));
        A("A-30b", "Passagem sinalizada de ciclistas", p => { p.Fig(Figuras.Ciclista, 0, 0.12, 0.70); Zebra(p); });
        A("A-30c", "Trânsito compartilhado por ciclistas e pedestres", p => { p.Fig(Figuras.Bicicleta, -0.32, -0.25, 0.48); p.Fig(Figuras.Pessoa, 0.38, 0.12, 0.62); });
        A("A-31", "Trânsito de tratores ou maquinária agrícola", p => p.Fig(Figuras.Trator, 0, 0.05, 0.82));
        A("A-32a", "Trânsito de pedestres", p => p.Fig(Figuras.Pessoa, 0, 0, 0.9));
        A("A-32b", "Passagem sinalizada de pedestres", p => { p.Fig(Figuras.Pessoa, 0, 0.18, 0.70); Zebra(p); });
        A("A-33a", "Área escolar", p => { p.Fig(Figuras.Crianca, -0.30, 0.02, 0.80); p.Fig(Figuras.Crianca, 0.32, -0.05, 0.68); });
        A("A-33b", "Passagem sinalizada de escolares", p => { p.Fig(Figuras.Crianca, -0.26, 0.20, 0.66); p.Fig(Figuras.Crianca, 0.28, 0.14, 0.56); Zebra(p); });
        A("A-34", "Crianças", p => { p.Fig(Figuras.Crianca, -0.10, 0.05, 0.85); p.Disco(0.50, -0.62, 0.16); });
        A("A-35", "Animais", p => p.Fig(Figuras.Vaca, 0, 0.05, 0.85));
        A("A-36", "Animais selvagens", p => p.Fig(Figuras.Cervo, 0, -0.05, 0.82));
        A("A-37", "Altura limitada", p => { SetasDimensaoVertical(p); p.Texto(p.Valor, 0, 0, 0.42, 1.2); }, valor: "3,0 m");
        A("A-38", "Largura limitada", p => { SetasDimensaoHorizontal(p); p.Texto(p.Valor, 0, 0, 0.42, 1.1); }, valor: "2,5 m");
        A("A-39", "Passagem de nível sem barreira", p => p.Fig(Figuras.Trem, 0, 0, 0.82));
        A("A-40", "Passagem de nível com barreira", p =>
        {
            p.Ar(-0.85, -0.05, 0.85, -0.05, 0.85, 0.22, -0.85, 0.22);
            for (int i = 0; i < 4; i++) { double x = -0.70 + i * 0.42; p.ArC(Cor.Branco, x, -0.02, x + 0.18, -0.02, x + 0.18, 0.19, x, 0.19); }
            p.E = 0.12; p.Tr(-0.70, -0.85, -0.70, -0.05); p.Tr(0.70, -0.85, 0.70, -0.05);
        });
        Add(new TipoPlaca { Codigo = "A-41", Nome = "Cruz de Santo André", Familia = Familia.Advertencia, Forma = Forma.CruzSantoAndre });
        A("A-42a", "Início de pista dupla", p =>
        {
            p.E = 0.16; p.Seta(-0.42, -0.85, -0.42, 0.75); p.Seta(0.42, 0.75, 0.42, -0.85);
            p.Ar(0, -0.25, 0.12, 0.10, 0.12, 0.92, -0.12, 0.92, -0.12, 0.10);
        });
        A("A-42b", "Fim de pista dupla", p =>
        {
            p.E = 0.16; p.Seta(-0.42, -0.85, -0.42, 0.75); p.Seta(0.42, 0.75, 0.42, -0.85);
            p.Ar(-0.12, -0.92, 0.12, -0.92, 0.12, -0.10, 0, 0.25, -0.12, -0.10);
        });
        A("A-42c", "Pista dividida", p =>
        {
            p.E = 0.15;
            p.SetaL(Concat(V(0, -0.92, 0, -0.55), Bezier(new V2(0, -0.55), new V2(0, -0.30), new V2(-0.45, -0.30), new V2(-0.45, -0.02)), V(-0.45, 0.85)));
            p.SetaL(Concat(V(0, -0.55), Bezier(new V2(0, -0.55), new V2(0, -0.30), new V2(0.45, -0.30), new V2(0.45, -0.02)), V(0.45, 0.85)));
            p.Ar(0, -0.15, 0.12, 0.10, 0.12, 0.92, -0.12, 0.92, -0.12, 0.10);
        });
        A("A-43", "Aeroporto", p => p.Fig((d, c, s, cor, e) => Figuras.Aviao(d, c, s, cor), 0, 0, 0.88));
        A("A-44", "Vento lateral", p => p.Fig(Figuras.Biruta, 0, 0, 0.85));
        A("A-45", "Rua sem saída", p =>
        {
            p.E = 0.24; p.Tr(0, -0.92, 0, 0.35);
            p.ArC(Cor.Vermelho, -0.48, 0.35, 0.48, 0.35, 0.48, 0.62, -0.48, 0.62);
        });
        A("A-46", "Peso bruto total limitado", p => p.Texto(p.Valor, 0, 0, 0.55, 1.2), valor: "10 t");
        A("A-47", "Peso limitado por eixo", p => { Eixo(p, 0.35); p.Texto(p.Valor, 0, -0.35, 0.45, 1.0); }, valor: "5 t");
        A("A-48", "Comprimento limitado", p =>
        {
            p.E = 0.09; p.Seta(0, 0.38, 0.80, 0.38); p.Seta(0, 0.38, -0.80, 0.38);
            p.Texto(p.Valor, 0, -0.20, 0.42, 1.1);
        }, valor: "15 m");
    }
}
