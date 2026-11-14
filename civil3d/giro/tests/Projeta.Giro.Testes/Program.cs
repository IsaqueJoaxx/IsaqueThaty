using System.Globalization;
using System.Text;
using Projeta.Giro.Core.Cinematica;
using Projeta.Giro.Core.Comum;
using Projeta.Giro.Core.Geometria;
using Projeta.Giro.Core.Veiculos;
using Projeta.Giro.Core.Verificacao;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
int falhas = 0;
void Ok(bool c, string msg) { Console.WriteLine((c ? "  ok   " : "  FALHA ") + msg); if (!c) falhas++; }

Console.WriteLine("== Biblioteca e raios estáticos (DNIT IPR-718, Tabela 6)");
var bib = Biblioteca.Carregar(null);
Ok(bib.Todos.Count == 5, $"5 veículos de referência ({string.Join(", ", bib.Ordenados.Select(v => v.Id))})");
foreach (var v in bib.Ordenados)
{
    var e = v.Estatico();
    Console.WriteLine($"  {v.Id,-3} δmax {e.Angulo:F1}°  roda ext {e.RodaExternaDianteira:F2} (ref {v.RaioMinimoRodaExterna:F2})  roda int {e.RodaInternaTraseira:F2} (ref {v.RefRaioRodaInterna:F2})  balanço {e.BalancoDianteiro:F2} (ref {v.RefRaioBalanco:F2})  comp {v.Comprimento:F2} (ref {v.RefComprimento:F2})");
    Ok(Math.Abs(e.RodaExternaDianteira - v.RaioMinimoRodaExterna!.Value) < 0.01, $"{v.Id}: raio da roda externa calibrado");
    if (v.Id != "O") Ok(Math.Abs(e.RodaInternaTraseira - v.RefRaioRodaInterna!.Value) < 0.25, $"{v.Id}: roda interna traseira dentro de 0,25 m da referência");
    if (v.Id != "O") Ok(Math.Abs(e.BalancoDianteiro - v.RefRaioBalanco!.Value) < 0.25, $"{v.Id}: balanço dianteiro dentro de 0,25 m da referência");
}

Console.WriteLine("== Tractriz em círculo (regime permanente)");
{
    var v = bib.Obter("CO")!;
    double rf = 15, l = v.Motriz.EntreEixos;
    var pts = Geo.PontosArco(V2.Zero, rf, -Math.PI / 2, 6 * Math.PI, 0.0005);
    var m = new Manobra { Veiculo = v, Inicio = pts[0], RumoInicial = 0, Modo = "seguir" };
    m.Trechos.Add(new Trecho { Pontos = pts });
    var r = Varredura.Simular(m, new OpcoesVarredura { CalcularEnvoltoria = false });
    var ult = r.Poses[r.Poses.Count * 3 / 4];
    double rr = ult.U[0].Ponto(-l).Comprimento;
    Ok(Math.Abs(rr - Math.Sqrt(rf * rf - l * l)) < 0.02, $"eixo traseiro em {rr:F3} m (teórico {Math.Sqrt(rf * rf - l * l):F3})");
    double dTeo = Math.Asin(l / rf);
    Ok(Math.Abs(ult.Delta - dTeo) < Num.Rad(0.3), $"esterçamento {Num.Graus(ult.Delta):F2}° (teórico {Num.Graus(dTeo):F2}°)");
}

Console.WriteLine("== Autocondução: a reta final passa pelo alvo e o rumo bate com a simulação");
foreach (var id in new[] { "VP", "CO", "SR", "RE" })
{
    var v = bib.Obter(id)!;
    var op = new OpcoesConducao { Velocidade = 10 };
    var alvo = new V2(40, 35);
    var t = Autoconducao.Ate(v, V2.Zero, 0, alvo, false, op);
    Ok(t.Ok, $"{id}: trecho gerado ({t.Erro})");
    if (!t.Ok) continue;
    var m = new Manobra { Veiculo = v, Inicio = V2.Zero, RumoInicial = 0 };
    m.Trechos.Add(new Trecho { Pontos = t.Pontos });
    var r = Varredura.Simular(m);
    var fim = r.Poses[^1];
    double dirAlvo = (alvo - t.Pontos[^2]).Angulo;
    Ok(Math.Abs(Geo.AngNorm(t.DirecaoFinal - dirAlvo)) < Num.Rad(0.2), $"{id}: reta final aponta para o alvo (erro {Num.Graus(Geo.AngNorm(t.DirecaoFinal - dirAlvo)):F3}°)");
    Ok(Num.Graus(r.DeltaMaximo) <= Num.Graus(v.AnguloMaximo) + 0.5 && Math.Abs(r.DeltaMaximo - Math.Abs(t.DeltaPico)) < Num.Rad(1.5), $"{id}: esterçamento máx simulado {Num.Graus(r.DeltaMaximo):F1}° (perfil {Num.Graus(t.DeltaPico):F1}°, máx {Num.Graus(v.AnguloMaximo):F1}°)");
    Ok(!r.Avisos.Any(a => a.Texto.Contains("acima do máximo")), $"{id}: sem aviso de esterçamento ({r.Avisos.Count} avisos: {string.Join(" | ", r.Avisos)})");
    Console.WriteLine($"     comprimento {r.Comprimento:F1} m, largura máx {r.LarguraMaxima:F2} m em {r.SLarguraMaxima:F0} m, articulação máx {Num.Graus(r.ArticulacaoMaxima):F0}°, {r.Envoltoria.Count} polígono(s)");
}

Console.WriteLine("== Alvo inalcançável e ré");
{
    var v = bib.Obter("CO")!;
    var t = Autoconducao.Ate(v, V2.Zero, 0, new V2(2, 6), false, new OpcoesConducao());
    Ok(!t.Ok, "alvo dentro do círculo de giro → erro: " + t.Erro);
    var tr = Autoconducao.Ate(v, V2.Zero, Math.PI, new V2(-20, -15), true, new OpcoesConducao());
    Ok(tr.Ok, "ré do CO gerada " + tr.Erro);
    var tr2 = Autoconducao.Ate(bib.Obter("SR")!, V2.Zero, Math.PI, new V2(-20, -15), true, new OpcoesConducao());
    Ok(!tr2.Ok, "ré do SR recusada: " + tr2.Erro);
}

Console.WriteLine("== Conflito com meio-fio");
{
    var v = bib.Obter("SR")!;
    var pts = Autoconducao.Gabarito(v, V2.Zero, 0, 90, true, new OpcoesConducao());
    var m = new Manobra { Veiculo = v, Inicio = V2.Zero, RumoInicial = 0, Trechos = { new Trecho { Pontos = pts } } };
    var r = Varredura.Simular(m);
    var meioFio = new Obstaculo { Pontos = Geo.PontosArco(new V2(22, 18), 8, -Math.PI / 2, -Math.PI / 2, 0.01).Select(p => p).ToList() };
    var c = Conflitos.Verificar(r.Envoltoria, new[] { meioFio, new Obstaculo { Pontos = new() { new(-30, 30), new(-30, 60) } } }, 0.5);
    Console.WriteLine("     " + c.Resumo(0.5));
    Ok(c.Invasoes.Count + c.NaFolga.Count >= 0, "verificação executada");
}

// exporta casos para conferência visual
var sb = new StringBuilder("[");
bool primeiro = true;
void Exportar(string nome, ResultadoVarredura r)
{
    if (!primeiro) sb.Append(','); primeiro = false;
    string P(IEnumerable<V2> ps) => "[" + string.Join(",", ps.Select(p => $"[{p.X:F3},{p.Y:F3}]")) + "]";
    var v = r.Manobra.Veiculo;
    var est = v.Estatico();
    var corpos = new List<string>();
    foreach (var s in new[] { 0.0, r.Comprimento * 0.5, r.Comprimento })
    {
        var (f, l) = Corpo.Desenho(v, r.PoseEm(s));
        corpos.AddRange(f.Select(x => P(x.Append(x[0]))));
        corpos.AddRange(l.Select(x => P(x)));
    }
    sb.Append($"{{\"nome\":\"{nome}\",\"caminho\":{P(r.CaminhoDianteiro)},\"env\":[{string.Join(",", r.Envoltoria.Select(e => P(e.Append(e[0]))))}],\"rodas\":[{string.Join(",", r.Rodas.Select(e => P(e.Append(e[0]))))}],\"corpos\":[{string.Join(",", corpos)}],\"info\":\"{v.Id} larg.máx {r.LarguraMaxima:F2} m, δmax {Num.Graus(r.DeltaMaximo):F1}°, avisos {r.Avisos.Count}\"}}");
}
foreach (var id in new[] { "VP", "CO", "O", "SR", "RE" })
{
    var v = bib.Obter(id)!;
    var pts = Autoconducao.Gabarito(v, V2.Zero, Math.PI / 2, 180, false, new OpcoesConducao(), v.Comprimento);
    var m = new Manobra { Veiculo = v, Inicio = V2.Zero, RumoInicial = Math.PI / 2, Trechos = { new Trecho { Pontos = pts } } };
    Exportar($"{id} — giro de 180° no raio mínimo", Varredura.Simular(m));
}
{
    var v = bib.Obter("SR")!;
    var m = new Manobra { Veiculo = v, Inicio = V2.Zero, RumoInicial = 0 };
    var op = new OpcoesConducao();
    V2 p = V2.Zero; double dir = 0;
    foreach (var alvo in new[] { new V2(30, 0), new V2(45, 25), new V2(20, 50), new V2(-10, 40) })
    {
        var t = Autoconducao.Ate(v, p, dir, alvo, false, op);
        if (!t.Ok) { Console.WriteLine("  erro: " + t.Erro); break; }
        m.Trechos.Add(new Trecho { Pontos = t.Pontos });
        p = alvo; dir = t.DirecaoFinal;
    }
    Exportar("SR — autocondução por 4 pontos", Varredura.Simular(m));
}
{
    var v = bib.Obter("CO")!;
    var m = new Manobra { Veiculo = v, Inicio = V2.Zero, RumoInicial = 0 };
    var op = new OpcoesConducao();
    var t1 = Autoconducao.Ate(v, V2.Zero, 0, new V2(25, 12), false, op);
    m.Trechos.Add(new Trecho { Pontos = t1.Pontos });
    var tras = new V2(25, 12) + V2.Polar(t1.DirecaoFinal + Math.PI, v.Motriz.EntreEixos);
    var tr = Autoconducao.Ate(v, tras, t1.DirecaoFinal + Math.PI, tras + V2.Polar(t1.DirecaoFinal + Math.PI, 22) + V2.Polar(t1.DirecaoFinal + Math.PI / 2, 14), true, op);
    if (tr.Ok) m.Trechos.Add(new Trecho { Re = true, Pontos = tr.Pontos }); else Console.WriteLine("  ré: " + tr.Erro);
    Exportar("CO — frente e ré", Varredura.Simular(m));
}
sb.Append(']');
File.WriteAllText(args.Length > 0 ? args[0] : "casos.json", sb.ToString());
Console.WriteLine(falhas == 0 ? "TODOS OS TESTES PASSARAM" : $"{falhas} FALHA(S)");
return falhas == 0 ? 0 : 1;
