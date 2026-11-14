using Autodesk.AutoCAD.Runtime;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Horizontal;
using Projeta.Sinalizacao.Infra;

namespace Projeta.Sinalizacao.Comandos;

/// <summary>Sinalização horizontal: marcas longitudinais, via completa, transversais, zebrado, setas, legendas, símbolos e tachas.</summary>
public sealed class CmdHorizontal
{
    internal static string Kw(string codigo) => codigo.Replace("-", "");
    internal static string DeKw(string kw, IEnumerable<string> codigos) => codigos.FirstOrDefault(c => Kw(c).Equals(kw, StringComparison.OrdinalIgnoreCase)) ?? kw;

    private static List<double> LerDeslocamentos(string s)
    {
        var l = new List<double>();
        foreach (var p in s.Split(new[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            if (Num.TentarLer(p, out double v)) l.Add(v);
        return l.Count == 0 ? new List<double> { 0 } : l;
    }

    [CommandMethod("PROJETA_SIN", "SINFAIXA", CommandFlags.Modal)]
    public void Faixa()
    {
        try
        {
            using var s = Sessao.Abrir();
            var cods = CatalogoLongitudinal.Tipos.Select(t => t.Codigo).ToList();
            var kw = Selecao.Palavra(s.Ed, "Marca longitudinal", cods.Select(Kw).ToArray(), "LFO2");
            if (kw is null) return;
            string cod = DeKw(kw, cods);
            var tipo = CatalogoLongitudinal.Obter(cod)!;
            Ctx.Msg($"{tipo.Codigo} — {tipo.Nome}");
            var v = Selecao.Numero(s.Ed, "Velocidade regulamentada (km/h)", s.Cfg.Velocidade);
            if (v is null) return;
            var cam = Selecao.PedirCaminho(s.Ed, s.Tr, "Selecione o eixo/linha (polilinha, linha, arco ou alinhamento)");
            if (cam is null) return;
            var des = Selecao.Texto(s.Ed, "Deslocamento(s) lateral(is) em m (+ esquerda, − direita), separados por ;", "0");
            if (des is null) return;
            var (tPad, ePad) = GeradorLongitudinal.CadenciaPadrao(s.Normas, tipo, v.Value);
            double? traco = null, espaco = null;
            if (tipo.Linhas.Contains(Padrao.Seccionada))
            {
                var t = Selecao.Numero(s.Ed, "Traço (m)", tPad); if (t is null) return;
                var e = Selecao.Numero(s.Ed, "Espaço (m)", ePad); if (e is null) return;
                if (Math.Abs(t.Value - tPad) > 1e-9) traco = t; if (Math.Abs(e.Value - ePad) > 1e-9) espaco = e;
            }
            int n = 0;
            foreach (var d in LerDeslocamentos(des))
            {
                s.Criar(new ObjetoSin
                {
                    Categoria = Categoria.MarcaLongitudinal, Codigo = tipo.Codigo, Situacao = s.Cfg.SituacaoPadrao,
                    Caminho = cam.Vertices, CaminhoFechado = cam.Fechado,
                    Marca = new DadosMarca { Velocidade = v.Value, Deslocamento = d, Traco = traco, Espaco = espaco, Material = s.Cfg.MaterialPintura },
                });
                n++;
            }
            s.Confirmar();
            Ctx.Msg($"{n} marca(s) {tipo.Codigo} lançada(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINVIA", CommandFlags.Modal)]
    public void Via()
    {
        try
        {
            using var s = Sessao.Abrir();
            var cam = Selecao.PedirCaminho(s.Ed, s.Tr, "Selecione o eixo da via");
            if (cam is null) return;
            var sentido = Selecao.Palavra(s.Ed, "Sentido de circulação", new[] { "Duplo", "Unico" }, "Duplo");
            if (sentido is null) return;
            var nf = Selecao.Inteiro(s.Ed, "Faixas por sentido", 1, 1, 6); if (nf is null) return;
            var w = Selecao.Numero(s.Ed, "Largura da faixa (m)", 3.5); if (w is null) return;
            var v = Selecao.Numero(s.Ed, "Velocidade regulamentada (km/h)", s.Cfg.Velocidade); if (v is null) return;
            string? central = null;
            if (sentido == "Duplo")
            {
                var c = Selecao.Palavra(s.Ed, "Linha central", new[] { "LFO1", "LFO2", "LFO3", "LFO4" }, "LFO2");
                if (c is null) return;
                central = DeKw(c, CatalogoLongitudinal.Tipos.Select(t => t.Codigo));
            }
            var bordo = Selecao.Palavra(s.Ed, "Linhas de bordo", new[] { "Sim", "Nao" }, "Sim");
            if (bordo is null) return;
            var objs = Montar(cam.Vertices, sentido == "Duplo", nf.Value, w.Value, v.Value, central, bordo == "Sim", s.Normas);
            foreach (var o in objs) { o.Situacao = s.Cfg.SituacaoPadrao; o.Marca!.Material = s.Cfg.MaterialPintura; s.Criar(o); }
            s.Confirmar();
            Ctx.Msg($"Via lançada: {objs.Count} marcas longitudinais.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    /// <summary>Marcas de uma via completa (eixo, divisão de faixas e bordos).</summary>
    internal static List<ObjetoSin> Montar(List<Vertice> eixo, bool duplo, int faixas, double largura, double v, string? central, bool bordo, Core.Normas.Normas n)
    {
        var l = new List<ObjetoSin>();
        ObjetoSin M(string cod, double d) => new()
        {
            Categoria = Categoria.MarcaLongitudinal, Codigo = cod, Caminho = eixo,
            Marca = new DadosMarca { Velocidade = v, Deslocamento = d },
        };
        double wl = Core.Normas.Normas.PorVelocidade(n.LarguraLinha, v);
        if (duplo)
        {
            l.Add(M(central ?? "LFO-2", 0));
            for (int k = 1; k < faixas; k++) { l.Add(M("LMS-2", k * largura)); l.Add(M("LMS-2", -k * largura)); }
            if (bordo) { double b = faixas * largura - 0.15 - wl / 2; l.Add(M("LBO", b)); l.Add(M("LBO", -b)); }
        }
        else
        {
            double meia = faixas * largura / 2;
            for (int k = 1; k < faixas; k++) l.Add(M("LMS-2", -meia + k * largura));
            if (bordo) { double b = meia - 0.15 - wl / 2; l.Add(M("LBO", b)); l.Add(M("LBO", -b)); }
        }
        return l;
    }

    [CommandMethod("PROJETA_SIN", "SINTRANSV", CommandFlags.Modal)]
    public void Transversal()
    {
        try
        {
            using var s = Sessao.Abrir();
            var kw = Selecao.Palavra(s.Ed, "Marca transversal", new[] { "LRE", "LDP", "FTP1", "FTP2", "LRV", "MCC" }, "FTP1");
            if (kw is null) return;
            string cod = kw switch { "FTP1" => "FTP-1", "FTP2" => "FTP-2", _ => kw };
            var m = new DadosMarca { Material = s.Cfg.MaterialPintura, Velocidade = s.Cfg.Velocidade };
            string msg1 = cod is "LRV" ? "Primeira linha: ponto inicial (bordo)" : "Ponto inicial da travessia (bordo/meio-fio)";
            var p1 = Selecao.Ponto(s.Ed, msg1); if (p1 is null) return;
            var p2 = Selecao.Ponto(s.Ed, "Ponto final (outro bordo)", p1); if (p2 is null) return;
            m.X1 = p1.Value.X; m.Y1 = p1.Value.Y; m.X2 = p2.Value.X; m.Y2 = p2.Value.Y;
            var n = s.Normas;
            switch (cod)
            {
                case "LRE": { var x = Selecao.Numero(s.Ed, "Largura da linha (0,30 a 0,60 m)", n.LarguraLRE); if (x is null) return; m.Largura = x; break; }
                case "LDP": { var x = Selecao.Numero(s.Ed, "Largura da linha (0,20 a 0,40 m)", n.LarguraLDP); if (x is null) return; m.Largura = x; break; }
                case "FTP-1":
                {
                    var e = Selecao.Numero(s.Ed, "Extensão da faixa no sentido do fluxo (mín. 3,00; recomendada 4,00 m)", n.FTP1Extensao); if (e is null) return;
                    var l = Selecao.Numero(s.Ed, "Largura das listras (0,30 a 0,40 m)", n.FTP1Largura); if (l is null) return;
                    var es = Selecao.Numero(s.Ed, "Espaço entre listras (0,30 a 0,80 m)", n.FTP1Espaco); if (es is null) return;
                    m.Extensao = e.Value; m.Largura = l; m.Espaco = es;
                    break;
                }
                case "FTP-2":
                {
                    var e = Selecao.Numero(s.Ed, "Distância livre entre as linhas (mín. 3,00 m)", n.FTP2Extensao); if (e is null) return;
                    var l = Selecao.Numero(s.Ed, "Largura das linhas (0,40 a 0,60 m)", n.FTP2Largura); if (l is null) return;
                    m.Extensao = e.Value; m.Largura = l;
                    break;
                }
                case "LRV":
                {
                    var v1 = Selecao.Numero(s.Ed, "Velocidade de aproximação (km/h)", 60); if (v1 is null) return;
                    var v2 = Selecao.Numero(s.Ed, "Velocidade desejada no fim (km/h)", 30); if (v2 is null) return;
                    var nl = Selecao.Inteiro(s.Ed, "Número de linhas", 8, 2, 50); if (nl is null) return;
                    var ex = Selecao.Numero(s.Ed, "Extensão total (m)", 60); if (ex is null) return;
                    var lado = Selecao.Palavra(s.Ed, "As linhas seguintes ficam (no sentido do fluxo) à", new[] { "Esquerda", "Direita" }, "Esquerda");
                    if (lado is null) return;
                    m.V1 = v1.Value; m.V2 = v2.Value; m.NumLinhas = nl.Value; m.Extensao = ex.Value; m.FluxoAEsquerda = lado == "Esquerda";
                    break;
                }
                case "MCC":
                {
                    var e = Selecao.Numero(s.Ed, "Largura da ciclofaixa (m)", 2.5); if (e is null) return;
                    m.Extensao = e.Value;
                    break;
                }
            }
            s.Criar(new ObjetoSin { Categoria = Categoria.MarcaTransversal, Codigo = cod, Situacao = s.Cfg.SituacaoPadrao, Marca = m });
            s.Confirmar();
            Ctx.Msg($"{cod} lançada.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINZEBRADO", CommandFlags.Modal)]
    public void Zebrado()
    {
        try
        {
            using var s = Sessao.Abrir();
            var cam = Selecao.PedirCaminho(s.Ed, s.Tr, "Selecione a polilinha fechada da área (ilha/canteiro fictício)");
            if (cam is null) return;
            var c = new Caminho(cam.Vertices, true);
            var pts = c.Trecho(0, c.Comprimento, 0.02);
            var a = Selecao.Ponto(s.Ed, "Sentido do fluxo: primeiro ponto"); if (a is null) return;
            var b = Selecao.Ponto(s.Ed, "Sentido do fluxo: segundo ponto", a); if (b is null) return;
            var cor = Selecao.Palavra(s.Ed, "Cor (branca = mesmo sentido, amarela = sentidos opostos)", new[] { "Branca", "Amarela" }, "Branca");
            if (cor is null) return;
            var n = s.Normas;
            var la = Selecao.Numero(s.Ed, "Largura das listras (0,30 a 0,50 m)", n.ZPALinha); if (la is null) return;
            var es = Selecao.Numero(s.Ed, "Espaço entre listras (1,10 a 3,50 m)", n.ZPAEspaco); if (es is null) return;
            var co = Selecao.Numero(s.Ed, "Largura da linha de contorno (0 = sem contorno)", n.ZPAContorno); if (co is null) return;
            s.Criar(new ObjetoSin
            {
                Categoria = Categoria.MarcaTransversal, Codigo = "ZPA", Situacao = s.Cfg.SituacaoPadrao,
                Caminho = pts.Select(p => new Vertice(p.X, p.Y)).ToList(), CaminhoFechado = true,
                Marca = new DadosMarca
                {
                    AnguloFluxo = (b.Value - a.Value).Angulo, LarguraListra = la.Value, EspacoListra = es.Value, LarguraContorno = co.Value,
                    Cor = cor == "Amarela" ? Cor.Amarelo : Cor.Branco, Material = s.Cfg.MaterialPintura,
                },
            });
            s.Confirmar();
            Ctx.Msg("Zebrado lançado.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    private static void InserirPontual(Sessao s, string codigo, DadosMarca m, string desc)
    {
        int n = 0;
        while (true)
        {
            var p = Selecao.Ponto(s.Ed, $"Ponto de inserção de {desc} (base, no início do sentido do fluxo) <terminar>");
            if (p is null) break;
            var q = Selecao.Ponto(s.Ed, "Ponto na direção do fluxo", p);
            if (q is null) break;
            s.Criar(new ObjetoSin
            {
                Categoria = Categoria.Inscricao, Codigo = codigo, Situacao = s.Cfg.SituacaoPadrao,
                X = p.Value.X, Y = p.Value.Y, Rotacao = (q.Value - p.Value).Angulo, Marca = m,
            });
            n++;
        }
        s.Confirmar();
        Ctx.Msg($"{n} inscrição(ões) {codigo} lançada(s).");
    }

    [CommandMethod("PROJETA_SIN", "SINSETA", CommandFlags.Modal)]
    public void Seta()
    {
        try
        {
            using var s = Sessao.Abrir();
            var mapa = new Dictionary<string, string>
            {
                ["SF"] = "PEM-SF", ["VE"] = "PEM-VE", ["VD"] = "PEM-VD", ["SFVE"] = "PEM-SFVE", ["SFVD"] = "PEM-SFVD",
                ["RE"] = "PEM-RE", ["RD"] = "PEM-RD", ["VEVD"] = "PEM-VEVD", ["MOFE"] = "MOF-E", ["MOFD"] = "MOF-D",
            };
            var kw = Selecao.Palavra(s.Ed, "Seta", mapa.Keys.ToArray(), "SF");
            if (kw is null) return;
            var v = Selecao.Numero(s.Ed, "Velocidade (km/h) — define o comprimento (5,00 ou 7,50 m)", s.Cfg.Velocidade); if (v is null) return;
            double padrao = Core.Normas.Normas.PorVelocidade(s.Cfg.Via == Core.Normas.TipoVia.Urbana ? s.Normas.ComprimentoSetaUrbana : s.Normas.ComprimentoSetaRural, v.Value);
            var L = Selecao.Numero(s.Ed, "Comprimento da seta (m)", padrao); if (L is null) return;
            InserirPontual(s, mapa[kw], new DadosMarca { Velocidade = v.Value, Comprimento = L.Value, Material = s.Cfg.MaterialPintura }, "seta");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINLEGENDA", CommandFlags.Modal)]
    public void Legenda()
    {
        try
        {
            using var s = Sessao.Abrir();
            var t = Selecao.Texto(s.Ed, "Texto (use | para quebrar linhas; a primeira fica mais perto do condutor)", "PARE");
            if (t is null) return;
            var v = Selecao.Numero(s.Ed, "Velocidade (km/h) — define a altura das letras", s.Cfg.Velocidade); if (v is null) return;
            var h = Selecao.Numero(s.Ed, "Altura das letras (m)", Core.Normas.Normas.PorVelocidade(s.Normas.AlturaLegenda, v.Value)); if (h is null) return;
            InserirPontual(s, "LEG", new DadosMarca { Texto = t, Velocidade = v.Value, AlturaLetra = h.Value, FatorLargura = s.Normas.FatorLarguraLegenda, EspacoLinhas = h.Value, Material = s.Cfg.MaterialPintura }, "legenda");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINSIMBOLO", CommandFlags.Modal)]
    public void Simbolo()
    {
        try
        {
            using var s = Sessao.Abrir();
            var kw = Selecao.Palavra(s.Ed, "Símbolo (SIP dê a preferência, SIB bicicleta, SID deficiente, SIS saúde)", new[] { "SIP", "SIB", "SID", "SIS" }, "SIP");
            if (kw is null) return;
            var e = Selecao.Numero(s.Ed, "Escala do símbolo", 1.0); if (e is null) return;
            InserirPontual(s, kw, new DadosMarca { Escala = e.Value, Material = s.Cfg.MaterialPintura }, "símbolo");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINTACHAS", CommandFlags.Modal)]
    public void TachasCmd()
    {
        try
        {
            using var s = Sessao.Abrir();
            var kw = Selecao.Palavra(s.Ed, "Tipo", new[] { "TachaMono", "TachaBi", "TachaoMono", "TachaoBi" }, "TachaBi");
            if (kw is null) return;
            string cod = kw.ToUpperInvariant() switch { "TACHAMONO" => "TACHA-MONO", "TACHAOMONO" => "TACHAO-MONO", "TACHAOBI" => "TACHAO-BI", _ => "TACHA-BI" };
            var cor = Selecao.Palavra(s.Ed, "Cor do elemento refletivo", new[] { "Branca", "Amarela", "Vermelha" }, "Amarela");
            if (cor is null) return;
            var cam = Selecao.PedirCaminho(s.Ed, s.Tr, "Selecione a linha onde vão as tachas");
            if (cam is null) return;
            var d = Selecao.Numero(s.Ed, "Deslocamento lateral (m)", 0, true); if (d is null) return;
            var er = Selecao.Numero(s.Ed, "Espaçamento em tangente (m)", s.Normas.EspacamentoTachasReta); if (er is null) return;
            var ec = Selecao.Numero(s.Ed, $"Espaçamento em curva com R < {Num.F0(s.Normas.RaioCurvaTachas)} m", s.Normas.EspacamentoTachasCurva); if (ec is null) return;
            s.Criar(new ObjetoSin
            {
                Categoria = Categoria.Tachas, Codigo = cod, Situacao = s.Cfg.SituacaoPadrao, Caminho = cam.Vertices, CaminhoFechado = cam.Fechado,
                Marca = new DadosMarca { Deslocamento = d.Value, EspacamentoReta = er.Value, EspacamentoCurva = ec.Value, Cor = cor switch { "Branca" => Cor.Branco, "Vermelha" => Cor.Vermelho, _ => Cor.Amarelo } },
            });
            s.Confirmar();
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }
}
