using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Defensas;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Horizontal;
using Projeta.Sinalizacao.Core.Obras;
using Projeta.Sinalizacao.Core.Quantitativos;
using Projeta.Sinalizacao.Core.Vertical;
using Nm = Projeta.Sinalizacao.Core.Normas;

namespace Projeta.Sinalizacao.Testes;

public static partial class Testes
{
    private static readonly Nm.Normas N = new();

    private static void T_01_Caminho(string _)
    {
        var reta = Caminho.DePontos(new V2(0, 0), new V2(100, 0));
        Perto(reta.Comprimento, 100, 1e-9, "reta 100 m");
        var arco = new Caminho(new[] { new Vertice(0, 0, 1), new Vertice(20, 0) });
        Perto(arco.Comprimento, Math.PI * 10, 1e-9, "semicírculo R10 = 31,416 m");
        var meio = arco.Ponto(arco.Comprimento / 2);
        Perto(meio.X, 10, 1e-9, "meio do arco X"); Perto(meio.Y, -10, 1e-9, "meio do arco Y (bulge + = anti-horário)");
        var (s, off, _) = arco.Projetar(new V2(10, -15));
        Perto(s, Math.PI * 5, 1e-6, "projeção no arco: estação");
        Perto(off, -5, 1e-6, "projeção no arco: afastamento à direita");
        var inv = arco.Invertido();
        Perto(inv.Ponto(0).X, 20, 1e-9, "caminho invertido começa no fim");
        Perto(inv.Ponto(inv.Comprimento / 2).Y, -10, 1e-9, "invertido mantém a geometria");
        Checar(Estaca.Formatar(245.6) == "12+5,600", "estaca 245,6 → 12+5,600", Estaca.Formatar(245.6));
        Checar(Estaca.TentarLer("12+5,600", 20, out var e) && Math.Abs(e - 245.6) < 1e-9, "lê estaca 12+5,600");
    }

    private static void T_02_Longitudinais(string _)
    {
        var c = Caminho.DePontos(new V2(0, 0), new V2(100, 0));
        var r = GeradorLongitudinal.Gerar(c, new ParamLongitudinal { Codigo = "LFO-2", Velocidade = 50 }, N);
        Checar(r.NumTracos == 17, "LFO-2 50 km/h: 17 traços 2/4 em 100 m", r.NumTracos.ToString());
        Perto(r.ComprimentoPintado, 34, 1e-6, "LFO-2: 34 m pintados");
        Perto(r.Area, 3.4, 1e-6, "LFO-2: 3,40 m² (largura 0,10)");
        Checar(r.Cor == Cor.Amarelo, "LFO-2 amarela");

        r = GeradorLongitudinal.Gerar(c, new ParamLongitudinal { Codigo = "LFO-2", Velocidade = 70 }, N);
        Checar(r.NumTracos == 12 && Math.Abs(r.ComprimentoPintado - 34) < 1e-6, "LFO-2 70 km/h: 3/6 → 12 traços, 34 m (último parcial de 1 m)", $"{r.NumTracos} {r.ComprimentoPintado}");

        r = GeradorLongitudinal.Gerar(c, new ParamLongitudinal { Codigo = "LMS-2", Velocidade = 90 }, N);
        Checar(r.NumTracos == 7, "LMS-2 90 km/h: 4/12 → 7 traços", r.NumTracos.ToString());
        Perto(r.Area, 7 * 4 * 0.15, 1e-6, "LMS-2: largura 0,15 m para v ≥ 80");

        r = GeradorLongitudinal.Gerar(c, new ParamLongitudinal { Codigo = "LFO-3", Velocidade = 50 }, N);
        Perto(r.ComprimentoPintado, 200, 1e-6, "LFO-3 dupla contínua: 200 m pintados");
        Perto(r.Area, 20, 1e-6, "LFO-3: 20 m²");

        r = GeradorLongitudinal.Gerar(c, new ParamLongitudinal { Codigo = "LCO", Velocidade = 50 }, N);
        Checar(r.NumTracos == 50, "LCO ≤ 60 km/h: 1/1 → 50 traços", r.NumTracos.ToString());

        r = GeradorLongitudinal.Gerar(c, new ParamLongitudinal { Codigo = "LFO-4", Velocidade = 50 }, N);
        Perto(r.ComprimentoPintado, 134, 1e-6, "LFO-4: contínua 100 + seccionada 34");

        // em curva: área = comprimento × largura (offset no eixo)
        var curva = new Caminho(new[] { new Vertice(0, 0, Math.Tan(Math.PI / 8)), new Vertice(50, 50) });
        r = GeradorLongitudinal.Gerar(curva, new ParamLongitudinal { Codigo = "LBO", Velocidade = 50 }, N);
        Perto(r.Area, curva.Comprimento * 0.10, 0.002, "LBO em curva: área ≈ comprimento × 0,10");
    }

    private static void T_03_Transversais(string _)
    {
        var a = new V2(0, 0); var b = new V2(7, 0);
        var f1 = GeradorTransversal.FTP1(a, b, 4, 0.40, 0.60);
        Checar(f1.Pecas == 7, "FTP-1 em 7,00 m: 7 listras de 0,40", f1.Pecas.ToString());
        Perto(f1.Area, 7 * 0.4 * 4, 1e-6, "FTP-1: 11,20 m²");
        var f2 = GeradorTransversal.FTP2(a, b, 4, 0.50);
        Perto(f2.Area, 7.0, 1e-6, "FTP-2: 2 × 7,00 × 0,50 = 7,00 m²");
        var lre = GeradorTransversal.LRE(a, new V2(3.5, 0), 0.40);
        Perto(lre.Area, 1.4, 1e-9, "LRE 3,50 × 0,40 = 1,40 m²");
        var pos = GeradorTransversal.PosicoesLRV(60, 30, 5, 60);
        Perto(pos[1], 18.75, 1e-6, "LRV 60→30 km/h, 5 linhas em 60 m: 2ª linha a 18,75 m");
        Perto(pos[2], 35.0, 1e-6, "LRV: 3ª linha a 35,00 m");
        Perto(pos[4], 60.0, 1e-6, "LRV: última linha no fim da extensão");
        var quadrado = new List<V2> { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        var z = GeradorTransversal.ZPA(quadrado, 0, 0.40, 1.50, 0, Cor.Branco);
        double esperado = 100 * 0.40 / 1.90;
        Checar(Math.Abs(z.Area - esperado) / esperado < 0.12, "ZPA 10×10: área ≈ 0,40/(0,40+1,50) da área", $"({z.Area:0.00} × {esperado:0.00})");
        Checar(z.Desenho.Itens.OfType<PArea>().All(p => p.Pts.All(v => v.X >= -1e-6 && v.X <= 10 + 1e-6 && v.Y >= -1e-6 && v.Y <= 10 + 1e-6)), "ZPA: listras recortadas dentro do contorno");
        var lc = GeradorTransversal.ZPA(new List<V2> { new(0, 0), new(10, 0), new(0, 10) }, 0, 0.4, 1.5, 0.15, Cor.Amarelo);
        Checar(lc.Desenho.Itens.OfType<PArea>().All(p => p.Pts.All(v => v.X >= -1e-6 && v.Y >= -1e-6 && v.X + v.Y <= 10 + 1e-6)), "ZPA triangular: contorno para dentro");
    }

    private static void T_04_Inscricoes(string _)
    {
        var s = Inscricoes.Seta("PEM-SF", 5.0);
        var cx = s.Desenho.Limites();
        Perto(cx.Altura, 5.0, 0.01, "seta siga em frente com 5,00 m");
        Checar(s.Area > 0.8 && s.Area < 1.8, "seta: área entre 0,8 e 1,8 m²", s.Area.ToString("0.00"));
        var s2 = Inscricoes.Seta("PEM-VD", 5.0);
        Checar(s2.Desenho.Limites().XMax > 1.5, "seta vire à direita aponta para +X");
        var l = Inscricoes.Legenda("PARE", 1.6, 0.42, 1.6);
        Perto(l.Desenho.Limites().Altura, 1.6, 0.02, "legenda PARE com letras de 1,60 m");
        Checar(l.Area > 0.5 && l.Area < 3.0, "legenda PARE: área plausível", l.Area.ToString("0.00"));
        var dAc = new Desenho2D();
        Fonte.Desenhar(dAc, "Á", V2.Zero, 1.0, Cor.Branco);
        Checar(dAc.Limites().YMax > 1.05 && dAc.Limites().XMin > -0.1 && dAc.Limites().XMax < 0.9, "acento agudo acima do A (cultura pt-BR)", dAc.Limites().ToString());
        Perto(Fonte.Largura("PARE", 1.0), (4 + 4 + 4 + 3.6 + 3 * 1.35) / 6 + 0.15, 1e-9, "largura do texto PARE (fonte própria)");
    }

    private static void T_05_Placas(string saida)
    {
        Perto(GeradorPlaca.Area(Forma.Octogonal, 0.25), 2 * (1 + Math.Sqrt(2)) * 0.0625, 1e-12, "R-1 lado 0,25: 0,3018 m²");
        Perto(GeradorPlaca.Area(Forma.Circular, 0.50), Math.PI * 0.0625, 1e-12, "circular Ø 0,50: 0,1963 m²");
        Perto(GeradorPlaca.Area(Forma.Losango, 0.50), 0.25, 1e-12, "advertência 0,50: 0,25 m²");
        Perto(GeradorPlaca.AlturaFace(Forma.Octogonal, 0.25), 0.6036, 1e-4, "R-1 lado 0,25: 0,60 m entre faces");
        Checar(Catalogo.Tipos.Count(t => t.Familia == Familia.Regulamentacao) == 51, "51 placas de regulamentação (R-1 a R-40 com variantes)", Catalogo.Tipos.Count(t => t.Familia == Familia.Regulamentacao).ToString());
        Checar(Catalogo.Tipos.Count(t => t.Familia == Familia.Advertencia) >= 69, "≥ 69 placas de advertência", Catalogo.Tipos.Count(t => t.Familia == Familia.Advertencia).ToString());
        Checar(Catalogo.Obter("r19")?.Codigo == "R-19" && Catalogo.Obter("A-1A")?.Codigo == "A-1a", "busca tolerante de código (r19, A-1A)");
        foreach (var t in Catalogo.Tipos)
        {
            var d = GeradorPlaca.Gerar(t, GeradorPlaca.DimensaoPadrao(t, N, Nm.TipoVia.Urbana), null, N);
            var cx = d.Limites();
            double w = GeradorPlaca.LarguraFace(t.Forma, GeradorPlaca.DimensaoPadrao(t, N, Nm.TipoVia.Urbana)) / 2 + 1e-3;
            if (d.Itens.Count < 2 || cx.XMax > w || cx.XMin < -w) { Checar(false, $"placa {t.Codigo} desenhada dentro da face", $"{cx}"); }
        }
        Checar(true, "todas as placas desenhadas dentro da face");
        var q = Suportes.Calcular(TipoSuporte.ColunaSimples, 0.50, N, Nm.TipoVia.Urbana);
        Perto(q.ComprimentoTotal, 2.10 + 0.50 + 0.10 + 0.60, 1e-9, "coluna simples urbana p/ placa Ø 0,50: 3,30 m");
        Checar(Suportes.Ler("Semi Pórtico Simples") == TipoSuporte.SemiPorticoSimples && Suportes.Ler("Braço Projetado Duplo") == TipoSuporte.BracoProjetadoDuplo, "lê nomes de suporte do cadastro");
        var m = ModeloPlaca.Exemplo();
        var lay = GeradorIndicacao.Montar(m);
        Checar(Math.Abs(lay.Largura / 0.05 - Math.Round(lay.Largura / 0.05)) < 1e-9 && Math.Abs(lay.Altura / 0.05 - Math.Round(lay.Altura / 0.05)) < 1e-9, "modelador: medidas em múltiplos de 0,05 m", $"{lay.Largura} × {lay.Altura}");
        Checar(lay.Altura >= 3 * 0.15 + 2 * 0.6 * 0.15, "modelador: altura comporta 3 linhas");
        File.WriteAllText(Path.Combine(saida, "indicacao.svg"), lay.Desenho.Svg(500, 0.05, "#ffffff"));
    }

    private static void T_06_ObrasDefensas(string _)
    {
        var r = ZonaObra.Calcular(400, new ParamZonaObra { InicioTrabalho = 150, ComprimentoTrabalho = 50, Velocidade = 60, LarguraFaixa = 3.5 }, N);
        Perto(r.Taper, 105, 1e-9, "taper 0,5 × 60 × 3,5 = 105 m");
        Perto(r.ZonaProtecao, 40, 1e-9, "zona de proteção a 60 km/h: 40 m");
        Checar(r.Contar("DISP") == 19 + 7 + 6, "zona: 32 cones (19 taper + 7 trabalho + 6 saída)", r.Contar("DISP").ToString());
        Checar(r.Contar("PLACA") == 3, "zona: 3 placas de pré-sinalização");
        Checar(r.Avisos.Count == 1, "zona: avisa que a pré-sinalização começa antes do caminho");
        var c = Caminho.DePontos(new V2(0, 0), new V2(100, 0));
        var d = GeradorDefensa.Gerar(c, new ParamDefensa(), N);
        Checar(d.NumPostes == 26 && d.NumLaminas == 25 && d.NumTerminais == 2, "defensa semimaleável 100 m: 26 postes, 25 lâminas, 2 terminais", $"{d.NumPostes}/{d.NumLaminas}/{d.NumTerminais}");
        Perto(d.ComprimentoCorrente, 76, 1e-9, "defensa: 76 m fora dos terminais abatidos (2 × 12 m)");
        var d2 = GeradorDefensa.Gerar(c, new ParamDefensa { Tipo = TipoDefensa.Rigida, TerminalInicio = TipoTerminal.Nenhum, TerminalFim = TipoTerminal.Nenhum }, N);
        Checar(d2.NumPostes == 51, "defensa rígida 100 m: 51 postes", d2.NumPostes.ToString());
    }

    private static void T_07_MotorEPlanilha(string saida)
    {
        var ex = new Exibicao();
        var objs = new List<ObjetoSin>();
        var pl = new ObjetoSin { Id = "PL-001", Categoria = Categoria.Placa, Codigo = "R-19", X = 10, Y = 5, Placa = new DadosPlaca { Placas = { new PlacaItem { Codigo = "R-19", Valor = "40" }, new PlacaItem { Codigo = "R-6a" } } } };
        objs.Add(pl);
        var g = Motor.Gerar(pl, N, ex);
        Perto(pl.Q("area"), 2 * Math.PI * 0.0625, 1e-9, "conjunto R-19 + R-6a Ø 0,50: área 0,393 m²");
        Perto(pl.Q("suporteTotal"), 2.10 + (0.5 + 0.05 + 0.5) + 0.10 + 0.60, 1e-9, "suporte do conjunto considera as duas placas");
        Checar(g.Local && g.Desenho.Itens.Count > 10, "placa gera desenho local");
        var json = pl.ParaJson();
        var volta = ObjetoSin.DeJson(json);
        Checar(volta is not null && volta.Placa!.Placas.Count == 2 && volta.Placa.Placas[0].Valor == "40", "dados da placa voltam do JSON");

        var ml = new ObjetoSin { Id = "ML-001", Categoria = Categoria.MarcaLongitudinal, Codigo = "LFO-2", Caminho = new() { new(0, 0), new(100, 0) }, Marca = new DadosMarca { Velocidade = 50 } };
        Motor.Gerar(ml, N, ex); objs.Add(ml);
        Perto(ml.Q("area"), 3.4, 1e-6, "motor: LFO-2 3,40 m²");
        var ftp = new ObjetoSin { Id = "MT-001", Categoria = Categoria.MarcaTransversal, Codigo = "FTP-1", Marca = new DadosMarca { X1 = 0, Y1 = 0, X2 = 7, Y2 = 0 } };
        Motor.Gerar(ftp, N, ex); objs.Add(ftp);
        Perto(ftp.Q("area"), 11.2, 1e-6, "motor: FTP-1 11,20 m²");
        var seta = new ObjetoSin { Id = "IN-001", Categoria = Categoria.Inscricao, Codigo = "PEM-SF", X = 50, Y = -1.75, Rotacao = 0, Marca = new DadosMarca { Velocidade = 50 } };
        var gs = Motor.Gerar(seta, N, ex); objs.Add(seta);
        var cs = gs.Desenho.Limites();
        Checar(cs.XMax - cs.XMin > 4.9 && cs.Largura < 5.1, "seta girada para o sentido do tráfego (+X)", cs.ToString());
        var tach = new ObjetoSin { Id = "TA-001", Categoria = Categoria.Tachas, Codigo = "TACHA-BI", Caminho = new() { new(0, 0), new(100, 0) }, Marca = new DadosMarca { Cor = Cor.Amarelo } };
        Motor.Gerar(tach, N, ex); objs.Add(tach);
        Checar(tach.Q("un") == 7, "tachas a cada 16 m em 100 m: 7", tach.Q("un").ToString());
        var df = new ObjetoSin { Id = "DF-001", Categoria = Categoria.Defensa, Caminho = new() { new(0, -8), new(100, -8) }, Defensa = new ParamDefensa() };
        Motor.Gerar(df, N, ex); objs.Add(df);
        var zo = new ObjetoSin { Id = "ZO-001", Categoria = Categoria.ZonaObra, Caminho = new() { new(-400, 0), new(400, 0) }, Zona = new ParamZonaObra { InicioTrabalho = 500 } };
        Motor.Gerar(zo, N, ex); objs.Add(zo);
        Checar(zo.Q("dispositivos") == 32 && zo.Q("placas") == 3, "motor: zona de obra com 32 cones e 3 placas", $"{zo.Q("dispositivos")} {zo.Q("placas")}");
        var se = new ObjetoSin { Id = "SE-001", Categoria = Categoria.Semaforica, Codigo = "GFV-200", X = 20, Y = 4 };
        Motor.Gerar(se, N, ex); objs.Add(se);
        var rs = new ObjetoSin { Id = "RS-001", Categoria = Categoria.RedeSemaforica, Codigo = "REDE-SUB", Caminho = new() { new(0, 6), new(30, 6), new(30, 20) } };
        Motor.Gerar(rs, N, ex); objs.Add(rs);
        Perto(rs.Q("comp"), 44, 1e-9, "rede semafórica: 44 m");

        foreach (var o in objs)
        {
            var j = o.ParaJson();
            var v = ObjetoSin.DeJson(j);
            Checar(v is not null && v.Categoria == o.Categoria && (v.Caminho?.Count ?? 0) == (o.Caminho?.Count ?? 0) && j.Length < 20000, $"JSON de {o.Id} ida e volta ({j.Length} caracteres)");
        }
        var itens = objs.Select(o => new ItemQuant(o, new Localizacao("Eixo 1", 12.3, 50, -3.2, o.Y, o.X))).ToList();
        var abas = PlanilhaQuantitativos.Montar(itens, "Teste", 20, N, Nm.TipoVia.Urbana);
        Checar(abas.Count >= 8, "planilha com resumo e abas de detalhamento", abas.Count.ToString());
        var arq = Path.Combine(saida, "quantitativos_teste.xlsx");
        Xlsx.Salvar(arq, abas);
        Checar(File.Exists(arq) && new FileInfo(arq).Length > 2000, "XLSX gravado");
        var lido = Cadastro.LerXlsx(arq);
        Checar(lido.Count > 5 && lido[0][0] == "Teste", "XLSX relido pelo leitor interno");
    }

    private static void T_08_Cadastro(string saida)
    {
        var arq = Path.Combine(saida, "cadastro.csv");
        File.WriteAllText(arq, Cadastro.ModeloCsv());
        var l = Cadastro.Ler(arq, out var avisos);
        Checar(l.Count == 4, "cadastro CSV: 4 linhas", l.Count.ToString());
        Checar(l[0].Codigo == "R-1" && Math.Abs(l[0].X - 2805.72) < 1e-9 && l[0].Rotacao == 90, "cadastro: código, X com vírgula e rotação");
        Checar(l[2].Situacao == Situacao.Manter && l[3].Situacao == Situacao.Remover, "cadastro: situação");
        Checar(l[1].Dimensao == 0.5 && l[1].Valor == "40", "cadastro: dimensão e valor");
        Checar(avisos.Count == 0, "cadastro sem avisos", string.Join("; ", avisos));
    }
}
