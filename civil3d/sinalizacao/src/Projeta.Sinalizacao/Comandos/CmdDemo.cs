using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Projeta.Sinalizacao.Civil;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Defensas;
using Projeta.Sinalizacao.Core.Desenho;
using Projeta.Sinalizacao.Core.Obras;
using Projeta.Sinalizacao.Core.Quantitativos;
using Projeta.Sinalizacao.Core.Vertical;
using Projeta.Sinalizacao.Infra;

namespace Projeta.Sinalizacao.Comandos;

/// <summary>
/// Demonstração e autoteste: monta um cruzamento urbano, uma travessia semaforizada, uma zona de obra e defensas,
/// gera quantitativos (XLSX) e o modelo 3D, sem janelas (roda também no accoreconsole).
/// </summary>
public sealed class CmdDemo
{
    private static List<Vertice> L(params double[] xy)
    {
        var l = new List<Vertice>();
        for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new Vertice(xy[i], xy[i + 1]));
        return l;
    }

    private static readonly string Log = Path.Combine(Path.GetTempPath(), "projeta_sin_demo.log");
    private static bool _registrando;
    /// <summary>Registro de passos da demonstração (só grava enquanto o SINDEMO roda; erros sempre).</summary>
    internal static void Passo(string s)
    {
        if (!_registrando && !s.StartsWith("ERRO")) return;
        try { File.AppendAllText(Log, $"{DateTime.Now:HH:mm:ss.fff} {s}{Environment.NewLine}"); } catch { }
    }

    [CommandMethod("PROJETA_SIN", "SINDEMO", CommandFlags.Modal)]
    public void Demo()
    {
        try
        {
            try { File.Delete(Log); } catch { }
        _registrando = Environment.GetEnvironmentVariable("SINDEMO_DETALHADO") is not null;
            var falhas = new List<string>();
            void Checar(bool ok, string msg)
        {
            Ctx.Msg((ok ? "DEMO ok   " : "DEMO FALHA ") + msg);
            bool r = _registrando; _registrando = true; Passo((ok ? "ok    " : "FALHA ") + msg); _registrando = r;
            if (!ok) falhas.Add(msg);
        }
            Passo("início");

            ObjectId eixoA = ObjectId.Null, eixoC = ObjectId.Null;
            using (var s = Sessao.Abrir(true))
            {
                var c = s.Cfg;
                c.Escala = 500; c.Via = Core.Normas.TipoVia.Urbana; c.Velocidade = 50;
                var ms = Ctx.EspacoModelo(s.Tr, s.Db);
                string lp = Ctx.GarantirLayer(s.Tr, s.Db, "SIN-DEMO-PISTA", 8);
                void Pista(params double[] xy)
                {
                    var pl = Conversor.Poli(L(xy).Select(v => v.P).ToList(), V2.Zero, false);
                    pl.Layer = lp; Ctx.Adicionar(s.Tr, ms, pl);
                }
                // bordos das pistas (contexto)
                Pista(0, -3.5, 146.5, -3.5, 146.5, -120); Pista(0, 3.5, 146.5, 3.5, 146.5, 120);
                Pista(300, -3.5, 153.5, -3.5, 153.5, -120); Pista(300, 3.5, 153.5, 3.5, 153.5, 120);
                Pista(0, -203.5, 600, -203.5); Pista(0, -196.5, 600, -196.5);

                // eixos (polilinhas) e alinhamentos do Civil 3D
                var plA = Conversor.Poli(new List<V2> { new(0, 0), new(300, 0) }, V2.Zero, false); plA.Layer = lp;
                var plC = Conversor.Poli(new List<V2> { new(0, -200), new(600, -200) }, V2.Zero, false); plC.Layer = lp;
                var idA = Ctx.Adicionar(s.Tr, ms, plA); var idC = Ctx.Adicionar(s.Tr, ms, plC);
                Passo("pistas desenhadas");
                if (Eixos.CivilDisponivel && Environment.GetEnvironmentVariable("SINDEMO_SEM_EIXOS") is null)
                {
                    Passo("criando alinhamentos");
                    try { eixoA = Eixos.CriarDePolilinha(s.Tr, s.Db, idA, "Eixo A - Av. Demonstração"); eixoC = Eixos.CriarDePolilinha(s.Tr, s.Db, idC, "Eixo C - Rodovia em obras"); }
                    catch (System.Exception ex) { Ctx.Msg("DEMO aviso: alinhamentos não criados (" + ex.Message + ")."); }
                }
                Checar(!Eixos.CivilDisponivel || !eixoA.IsNull, "alinhamentos criados a partir das polilinhas");

                var n = s.Normas;
                // ---------------------------------------------------- horizontal
                var marcas = new List<ObjetoSin>();
                marcas.AddRange(CmdHorizontal.Montar(L(35, 0, 140, 0), true, 1, 3.5, 50, "LFO-2", false, n));
                marcas.AddRange(CmdHorizontal.Montar(L(0, 0, 140, 0), true, 1, 3.5, 50, null, true, n).Where(o => o.Codigo == "LBO"));
                marcas.AddRange(CmdHorizontal.Montar(L(160, 0, 300, 0), true, 1, 3.5, 50, "LFO-2", true, n));
                marcas.AddRange(CmdHorizontal.Montar(L(150, -120, 150, -16), true, 1, 3.5, 50, "LFO-2", true, n));
                marcas.AddRange(CmdHorizontal.Montar(L(150, 16, 150, 120), true, 1, 3.5, 50, "LFO-2", true, n));
                marcas.AddRange(CmdHorizontal.Montar(L(0, -200, 600, -200), true, 1, 3.5, 60, "LFO-3", true, n));
                Passo("criando marcas");
                foreach (var o in marcas) { s.Criar(o); Passo("  " + o.Id + " " + o.Codigo); }
                Checar(marcas.Count == 15, $"15 marcas longitudinais lançadas ({marcas.Count})");

                ObjetoSin T(string cod, double x1, double y1, double x2, double y2, Action<DadosMarca>? cfg = null)
                {
                    var m = new DadosMarca { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Velocidade = 50 };
                    cfg?.Invoke(m);
                    var o = new ObjetoSin { Categoria = Categoria.MarcaTransversal, Codigo = cod, Marca = m };
                    s.Criar(o);
                    return o;
                }
                Passo("transversais");
                var ftp = T("FTP-1", 146.5, -12, 153.5, -12);
                T("FTP-1", 146.5, 12, 153.5, 12);
                T("FTP-1", 138, -3.5, 138, 3.5);
                T("FTP-1", 162, -3.5, 162, 3.5);
                T("LRE", 150, -15.6, 153.5, -15.6);
                T("LRE", 146.5, 15.6, 150, 15.6);
                T("FTP-2", 230, -3.5, 230, 3.5);
                T("LRE", 224, -3.5, 224, 0);
                T("LRE", 236, 0, 236, 3.5);
                T("LRV", 146.5, 70, 150, 70, m => { m.V1 = 60; m.V2 = 30; m.NumLinhas = 6; m.Extensao = 40; m.FluxoAEsquerda = false; });
                Checar(Math.Abs(ftp.Q("area") - 11.2) < 1e-6, $"FTP-1 de 7,00 m com 11,20 m² ({Num.F(ftp.Q("area"), 3)})");

                var zpa = new ObjetoSin
                {
                    Categoria = Categoria.MarcaTransversal, Codigo = "ZPA", CaminhoFechado = true, Caminho = L(5, 0, 35, -1.2, 35, 1.2),
                    Marca = new DadosMarca { AnguloFluxo = 0, LarguraListra = 0.4, EspacoListra = 1.1, LarguraContorno = 0.10, Cor = Cor.Amarelo },
                };
                s.Criar(zpa);
                Checar(zpa.Q("area") > 0.5, $"zebrado com área pintada ({Num.F(zpa.Q("area"), 2)} m²)");

                ObjetoSin I(string cod, double x, double y, double rot, Action<DadosMarca>? cfg = null)
                {
                    var m = new DadosMarca { Velocidade = 50 };
                    cfg?.Invoke(m);
                    var o = new ObjetoSin { Categoria = Categoria.Inscricao, Codigo = cod, X = x, Y = y, Rotacao = rot, Marca = m };
                    s.Criar(o);
                    return o;
                }
                I("PEM-SF", 95, -1.75, 0); I("PEM-SFVD", 120, -1.75, 0);
                I("PEM-SF", 205, 1.75, Math.PI); I("PEM-SFVE", 180, 1.75, Math.PI);
                var leg = I("LEG", 151.75, -26, Math.PI / 2, m => { m.Texto = "PARE"; m.AlturaLetra = 1.6; m.FatorLargura = n.FatorLarguraLegenda; });
                I("LEG", 148.25, 26, -Math.PI / 2, m => { m.Texto = "PARE"; m.AlturaLetra = 1.6; m.FatorLargura = n.FatorLarguraLegenda; });
                I("SIP", 151.75, -34, Math.PI / 2);
                Checar(leg.Q("area") > 0.5, $"legenda PARE ({Num.F(leg.Q("area"), 2)} m²)");

                var tachas = new ObjetoSin { Categoria = Categoria.Tachas, Codigo = "TACHA-BI", Caminho = L(160, 0, 300, 0), Marca = new DadosMarca { Cor = Cor.Amarelo } };
                s.Criar(tachas);
                Checar(tachas.Q("un") == 9, $"tachas a cada 16 m em 140 m ({tachas.Q("un")})");

                Passo("vertical");
                // ---------------------------------------------------- vertical
                ObjetoSin P(double x, double y, double az, TipoSuporte sup, params PlacaItem[] placas)
                {
                    var o = new ObjetoSin { Categoria = Categoria.Placa, X = x, Y = y, Placa = new DadosPlaca { Suporte = sup, Azimute = az } };
                    o.Placa.Placas.AddRange(placas);
                    s.Criar(o);
                    return o;
                }
                P(154.5, -17, -Math.PI / 2, TipoSuporte.ColunaSimples, new PlacaItem { Codigo = "R-1" });
                P(145.5, 17, Math.PI / 2, TipoSuporte.ColunaSimples, new PlacaItem { Codigo = "R-1" });
                var conj = P(100, -5.5, Math.PI, TipoSuporte.ColunaSimples, new PlacaItem { Codigo = "R-19", Valor = "40" }, new PlacaItem { Codigo = "R-6a" });
                P(125, -5.5, Math.PI, TipoSuporte.ColunaSimples, new PlacaItem { Codigo = "A-32b" });
                P(200, 5.5, 0, TipoSuporte.ColunaSimples, new PlacaItem { Codigo = "A-14" });
                P(260, 5.5, 0, TipoSuporte.ColunaSimples, new PlacaItem { Codigo = "R-7" });
                var ind = ModeloPlaca.Exemplo();
                P(60, -6.5, Math.PI, TipoSuporte.ColunaDupla, new PlacaItem { Codigo = ind.Codigo, Modelo = ind.ParaJson() });
                Checar(Math.Abs(conj.Q("area") - 2 * Math.PI * 0.0625) < 1e-9, $"conjunto R-19 + R-6a com 0,393 m² ({Num.F(conj.Q("area"), 3)})");

                // cadastro de campo (placas existentes e a remover)
                var cad = Cadastro.Interpretar(new List<List<string>>
                {
                    new() { "COD", "SUP", "X", "Y", "ROT", "SIT" },
                    new() { "R-3", "Coluna Simples", "290,0", "5,5", "0", "Manter" },
                    new() { "A-18", "Coluna Simples", "40,0", "-5,5", "180", "Remover" },
                }, out var avCad);
                int imp = Operacoes.ImportarCadastro(s, cad);
                Checar(imp == 2 && avCad.Count == 0, $"cadastro importado ({imp} placas)");

                Passo("semafórica");
                // ---------------------------------------------------- semafórica
                ObjetoSin D(Categoria cat, string cod, double x, double y, double rot)
                {
                    var o = new ObjetoSin { Categoria = cat, Codigo = cod, X = x, Y = y, Rotacao = rot, Dispositivo = new DadosDispositivo() };
                    s.Criar(o);
                    return o;
                }
                D(Categoria.Semaforica, "COL-BP", 220, -5.5, Math.PI);
                D(Categoria.Semaforica, "GFV-200", 220, -1.75, Math.PI);
                D(Categoria.Semaforica, "COL-BP", 240, 5.5, 0);
                D(Categoria.Semaforica, "GFV-200", 240, 1.75, 0);
                D(Categoria.Semaforica, "GFP-200", 227, -6, Math.PI / 2);
                D(Categoria.Semaforica, "GFP-200", 233, 6, -Math.PI / 2);
                D(Categoria.Semaforica, "BOT", 228.5, -4.6, Math.PI / 2);
                D(Categoria.Semaforica, "CTR", 214, -9, Math.PI / 2);
                var rede = new ObjetoSin { Categoria = Categoria.RedeSemaforica, Codigo = "REDE-SUB", Caminho = L(214, -9, 220, -9, 220, -7, 240, -7, 240, 5.5) };
                s.Criar(rede);
                Checar(Math.Abs(rede.Q("comp") - (6 + 2 + 20 + 12.5)) < 1e-6, $"rede semafórica com 40,50 m ({Num.F(rede.Q("comp"), 2)})");

                Passo("obras e defensas");
                // ---------------------------------------------------- obras e defensas
                var zona = new ObjetoSin
                {
                    Categoria = Categoria.ZonaObra, Codigo = "ZONA", Caminho = L(0, -200, 600, -200),
                    Zona = new ParamZonaObra { InicioTrabalho = 450, ComprimentoTrabalho = 50, Velocidade = 60, LarguraFaixa = 3.5, DeslocamentoFaixa = -1.75, FechaDireita = true },
                };
                s.Criar(zona);
                Checar(zona.Q("dispositivos") == 32 && zona.Q("placas") == 3, $"zona de obra: 32 cones e 3 placas ({zona.Q("dispositivos")}/{zona.Q("placas")})");
                D(Categoria.Obra, "CAVALETE", 470, -203, 0);
                D(Categoria.Obra, "CAVALETE", 480, -203, 0);
                var df = new ObjetoSin { Categoria = Categoria.Defensa, Caminho = L(100, -195.5, 250, -195.5), Defensa = new ParamDefensa { LadoTrafego = -1 } };
                s.Criar(df);
                Checar(df.Q("postes") == 39 && df.Q("terminais") == 2, $"defensa 150 m: 39 postes, 2 terminais ({df.Q("postes")}/{df.Q("terminais")})");
                s.Criar(new ObjetoSin { Categoria = Categoria.Defensa, Caminho = L(300, -195.5, 360, -195.5), Defensa = new ParamDefensa { Tipo = TipoDefensa.BarreiraConcreto } });
                Passo("confirmando 1");
                s.Confirmar();
                Passo("confirmado 1");
                Ctx.Msg("DEMO objetos criados: " + string.Join(", ", s.Cfg.Contadores.Select(kv => $"{kv.Key}={kv.Value}")));
            }

            // ---------------------------------------------------- leitura, MOVE e correção
            using (var s = Sessao.Abrir(true))
            {
                var todos = Armazenamento.Todos(s.Tr, s.Db);
                Checar(todos.Count == 57, $"{todos.Count} objetos relidos do DWG");
                var conj = todos.First(x => x.Obj.Codigo.StartsWith("R-19"));
                var br = (BlockReference)s.Tr.GetObject(conj.Id, OpenMode.ForWrite);
                br.TransformBy(Matrix3d.Displacement(new Vector3d(5, 0, 0)));
                s.Regenerar(conj.Id);
                var depois = s.Ler(conj.Id)!;
                Checar(Math.Abs(depois.X - 105) < 1e-6, $"placa movida com MOVE acompanha os dados (X = {Num.F(depois.X, 2)})");
                var faixa = todos.First(x => x.Obj.Codigo == "FTP-1");
                var brf = (BlockReference)s.Tr.GetObject(faixa.Id, OpenMode.ForWrite);
                brf.TransformBy(Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, brf.Position));
                s.Regenerar(faixa.Id);
                var fr = s.Ler(faixa.Id)!;
                Checar(Math.Abs(fr.Q("area") - 11.2) < 1e-6 && Math.Abs(fr.Marca!.X2 - fr.Marca.X1) < 1e-6, "faixa girada com ROTATE mantém a área e gira os pontos");
                int rep = Operacoes.CorrigirIdsRepetidos(s, Armazenamento.Todos(s.Tr, s.Db));
                Checar(rep == 0, "sem IDs repetidos");
                int regen = 0;
                foreach (var (id, _) in Armazenamento.Todos(s.Tr, s.Db)) if (s.Regenerar(id)) regen++;
                Checar(regen == todos.Count, $"SINCORRIGE regenerou {regen} objetos");
                s.Confirmar();
            }

            // ---------------------------------------------------- quantitativos e 3D
            using (var s = Sessao.Abrir(true))
            {
                var todos = Armazenamento.Todos(s.Tr, s.Db);
                var eixos = new List<(ObjectId, string)>();
                if (!eixoA.IsNull) eixos.Add((eixoA, "Eixo A - Av. Demonstração"));
                if (!eixoC.IsNull) eixos.Add((eixoC, "Eixo C - Rodovia em obras"));
                string dwg = s.Db.Filename;
                string pasta = !string.IsNullOrWhiteSpace(dwg) && Path.IsPathRooted(dwg) ? Path.GetDirectoryName(dwg)! : Path.GetTempPath();
                string arq = Path.Combine(pasta, "SINDEMO_quantitativos.xlsx");
                Operacoes.ExportarQuantitativos(s, todos, eixos, arq, "Quantitativos — demonstração PROJETA Sinalização");
                Checar(File.Exists(arq), $"planilha gravada: {arq}");
                var lido = Cadastro.LerXlsx(arq);
                Checar(lido.Count > 20, $"planilha relida ({lido.Count} linhas no resumo)");
                if (eixos.Count > 0)
                {
                    var loc = Operacoes.Localizar(s.Tr, todos.First(x => x.Obj.Codigo.StartsWith("R-19")).Obj, eixos);
                    Checar(loc.Eixo.StartsWith("Eixo A") && loc.Estaca is > 104 and < 106 && loc.Lado == "LD", $"placa R-19 na estaca {Estaca.Formatar(loc.Estaca ?? 0)} {loc.Lado} do {loc.Eixo}");
                }
                Passo("3D");
                int sol = Bim.Gerar(s, todos, ObjectId.Null);
                Checar(sol > 100, $"{sol} sólidos 3D gerados");
                s.Confirmar();
                foreach (var a in s.Avisos) Ctx.Msg("DEMO aviso: " + a);
            }
            string fim = falhas.Count == 0 ? "DEMO CONCLUIDA SEM FALHAS" : $"DEMO CONCLUIDA COM {falhas.Count} FALHA(S)";
        Ctx.Msg(fim);
        _registrando = true; Passo(fim); _registrando = false;
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }
}
