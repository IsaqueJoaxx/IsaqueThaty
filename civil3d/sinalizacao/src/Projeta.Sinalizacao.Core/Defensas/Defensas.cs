using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Desenho;
using N = Projeta.Sinalizacao.Core.Normas.Normas;

namespace Projeta.Sinalizacao.Core.Defensas;

public enum TipoDefensa { SemimaleavelSimples, SemimaleavelDupla, Rigida, BarreiraConcreto }
public enum TipoTerminal { Nenhum, Abatido, AncoradoTalude }

public sealed class ParamDefensa
{
    public TipoDefensa Tipo { get; set; } = TipoDefensa.SemimaleavelSimples;
    public TipoTerminal TerminalInicio { get; set; } = TipoTerminal.Abatido;
    public TipoTerminal TerminalFim { get; set; } = TipoTerminal.Abatido;
    /// <summary>Lado da via (para desenhar a lâmina voltada ao tráfego): +1 tráfego à esquerda do caminho, −1 à direita.</summary>
    public int LadoTrafego { get; set; } = 1;
}

public sealed class ResultadoDefensa
{
    public Desenho2D Desenho { get; } = new();
    public double Comprimento { get; set; }
    public double ComprimentoTerminais { get; set; }
    public double ComprimentoCorrente => Math.Max(0, Comprimento - ComprimentoTerminais);
    public int NumPostes { get; set; }
    public int NumLaminas { get; set; }
    public int NumTerminais { get; set; }
    public List<double> Postes { get; } = new();
    public List<string> Avisos { get; } = new();
}

public static class GeradorDefensa
{
    public static string Nome(TipoDefensa t) => t switch
    {
        TipoDefensa.SemimaleavelSimples => "Defensa metálica semimaleável simples",
        TipoDefensa.SemimaleavelDupla => "Defensa metálica semimaleável dupla",
        TipoDefensa.Rigida => "Defensa metálica rígida",
        TipoDefensa.BarreiraConcreto => "Barreira de concreto (New Jersey)",
        _ => t.ToString(),
    };

    public static string Nome(TipoTerminal t) => t switch
    {
        TipoTerminal.Abatido => "Terminal abatido",
        TipoTerminal.AncoradoTalude => "Terminal ancorado em talude",
        _ => "Sem terminal",
    };

    public static double ComprimentoTerminal(TipoTerminal t, N n) => t switch
    {
        TipoTerminal.Abatido => n.ComprimentoTerminalAbatido,
        TipoTerminal.AncoradoTalude => n.ComprimentoTerminalAncorado,
        _ => 0,
    };

    public static ResultadoDefensa Gerar(Caminho c, ParamDefensa p, N n)
    {
        var r = new ResultadoDefensa { Comprimento = c.Comprimento };
        double L = c.Comprimento;
        if (L < 0.5) { r.Avisos.Add("Caminho muito curto."); return r; }
        int lado = p.LadoTrafego >= 0 ? 1 : -1;

        if (p.Tipo == TipoDefensa.BarreiraConcreto)
        {
            r.Desenho.Area(c.Faixa(0, L, 0, 0.60), Cor.Cinza);
            r.Desenho.Area(c.Faixa(0, L, 0, 0.18), Cor.Preto);
            r.NumLaminas = (int)Math.Ceiling(L / 2.0); // módulos pré-moldados de 2 m (referência)
            return r;
        }

        double ti = ComprimentoTerminal(p.TerminalInicio, n), tf = ComprimentoTerminal(p.TerminalFim, n);
        if (ti + tf > L) { r.Avisos.Add("Os terminais são maiores que o comprimento da defensa; terminais desconsiderados."); ti = tf = 0; }
        r.ComprimentoTerminais = ti + tf;
        r.NumTerminais = (ti > 0 ? 1 : 0) + (tf > 0 ? 1 : 0);
        double esp = p.Tipo == TipoDefensa.Rigida ? n.EspacamentoPostesRigida : n.EspacamentoPostesSemimaleavel;
        // postes: em todo o comprimento, inclusive terminais, com espaçamento uniforme
        int nPostes = (int)Math.Ceiling(L / esp - 1e-9) + 1;
        double passo = L / (nPostes - 1);
        for (int i = 0; i < nPostes; i++) r.Postes.Add(i * passo);
        r.NumPostes = nPostes * (p.Tipo == TipoDefensa.SemimaleavelDupla ? 1 : 1);
        int faces = p.Tipo == TipoDefensa.SemimaleavelDupla ? 2 : 1;
        r.NumLaminas = (int)Math.Ceiling(L / n.ComprimentoLamina - 1e-9) * faces;

        // desenho: lâmina (faixa estreita) do lado do tráfego, postes como quadrados, terminais com afastamento
        double offLamina = lado * 0.12;
        V2 Pt(double s, double off) => c.PontoDeslocado(s, off);
        // terminais abatidos: a lâmina se afasta do tráfego nas pontas
        double Afast(double s)
        {
            double a = 0;
            if (ti > 0 && s < ti) a = (ti - s) / ti * 0.5;
            if (tf > 0 && s > L - tf) a = Math.Max(a, (s - (L - tf)) / tf * 0.5);
            return -lado * a;
        }
        var faceA = new List<V2>();
        var faceB = new List<V2>();
        var amostras = c.Trecho(0, L, 0.02);
        // reamostra com passo de 1 m para incluir o afastamento dos terminais
        var ss = new List<double>();
        for (double s = 0; s < L; s += 1.0) ss.Add(s);
        ss.Add(L);
        foreach (var s in ss)
        {
            double off = offLamina + Afast(s);
            faceA.Add(Pt(s, off + lado * 0.04));
            faceB.Add(Pt(s, off - lado * 0.04));
        }
        faceB.Reverse();
        r.Desenho.Area(faceA.Concat(faceB).ToList(), Cor.Preto);
        if (faces == 2)
        {
            var fa = ss.Select(s => Pt(s, -offLamina - Afast(s) + lado * 0.04)).ToList();
            var fb = ss.Select(s => Pt(s, -offLamina - Afast(s) - lado * 0.04)).ToList();
            fb.Reverse();
            r.Desenho.Area(fa.Concat(fb).ToList(), Cor.Preto);
        }
        foreach (var s in r.Postes)
        {
            var q = Pt(s, Afast(s));
            var u = c.Tangente(s);
            r.Desenho.Area(Poligonos.Retangulo(q, u, 0.15, 0.15), Cor.Preto);
        }
        if (amostras.Count == 0) r.Avisos.Add("Caminho sem pontos.");
        return r;
    }
}
