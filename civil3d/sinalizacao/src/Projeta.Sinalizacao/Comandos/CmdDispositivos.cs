using Autodesk.AutoCAD.Runtime;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Defensas;
using Projeta.Sinalizacao.Core.Obras;
using Projeta.Sinalizacao.Core.Semaforica;
using Projeta.Sinalizacao.Infra;

namespace Projeta.Sinalizacao.Comandos;

/// <summary>Semafórica, obras (dispositivos e zona de obra) e defensas.</summary>
public sealed class CmdDispositivos
{
    private static int InserirDispositivos(Sessao s, Categoria cat, string codigo, DadosDispositivo dd, string msgDir)
    {
        int n = 0;
        while (true)
        {
            var p = Selecao.Ponto(s.Ed, $"Posição de {codigo} <terminar>");
            if (p is null) break;
            var q = Selecao.Ponto(s.Ed, msgDir, p);
            if (q is null) break;
            s.Criar(new ObjetoSin { Categoria = cat, Codigo = codigo, Situacao = s.Cfg.SituacaoPadrao, X = p.Value.X, Y = p.Value.Y, Rotacao = (q.Value - p.Value).Angulo, Dispositivo = dd });
            n++;
            s.Tr.TransactionManager.QueueForGraphicsFlush();
        }
        return n;
    }

    [CommandMethod("PROJETA_SIN", "SINSEMAFORO", CommandFlags.Modal)]
    public void Semaforo()
    {
        try
        {
            using var s = Sessao.Abrir();
            var itens = CatalogoSemaforica.Itens.Select(i => i.Codigo).ToList();
            foreach (var i in CatalogoSemaforica.Itens) Ctx.Msg($"  {i.Codigo.Replace("-", "")}: {i.Nome}");
            var kw = Selecao.Palavra(s.Ed, "Item", itens.Select(CmdHorizontal.Kw).ToArray(), "GFV200");
            if (kw is null) return;
            string cod = CmdHorizontal.DeKw(kw, itens);
            var dd = new DadosDispositivo();
            if (cod == "COL-BP") { var b = Selecao.Numero(s.Ed, "Comprimento do braço projetado (m)", s.Normas.ComprimentoBracoProjetado); if (b is null) return; dd.Braco = b.Value; }
            string msg = cod.StartsWith("COL") ? "Direção do braço/pista" : "Ponto de onde vem o tráfego (a face fica voltada para ele)";
            int n = InserirDispositivos(s, Categoria.Semaforica, cod, dd, msg);
            s.Confirmar();
            Ctx.Msg($"{n} item(ns) de semafórica inserido(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINREDE", CommandFlags.Modal)]
    public void Rede()
    {
        try
        {
            using var s = Sessao.Abrir();
            var kw = Selecao.Palavra(s.Ed, "Rede", new[] { "Subterranea", "Aerea" }, "Subterranea");
            if (kw is null) return;
            var cam = Selecao.PedirCaminho(s.Ed, s.Tr, "Selecione o traçado da rede");
            if (cam is null) return;
            s.Criar(new ObjetoSin { Categoria = Categoria.RedeSemaforica, Codigo = kw == "Aerea" ? "REDE-AER" : "REDE-SUB", Situacao = s.Cfg.SituacaoPadrao, Caminho = cam.Vertices });
            s.Confirmar();
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    private static readonly string[] KwObra = { "CONE", "TAMBOR", "CAVALETE", "BARREIRA", "CILINDRO", "BALIZADOR", "TELA", "LUZ" };

    [CommandMethod("PROJETA_SIN", "SINOBRA", CommandFlags.Modal)]
    public void Obra()
    {
        try
        {
            using var s = Sessao.Abrir();
            var kw = Selecao.Palavra(s.Ed, "Dispositivo de obra", KwObra, "CONE");
            if (kw is null) return;
            int n = InserirDispositivos(s, Categoria.Obra, kw, new DadosDispositivo(), "Direção do fluxo");
            s.Confirmar();
            Ctx.Msg($"{n} dispositivo(s) inserido(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINOBRALINHA", CommandFlags.Modal)]
    public void ObraLinha()
    {
        try
        {
            using var s = Sessao.Abrir();
            var kw = Selecao.Palavra(s.Ed, "Dispositivo", KwObra, "CONE");
            if (kw is null) return;
            var cam = Selecao.PedirCaminho(s.Ed, s.Tr, "Selecione a linha de canalização");
            if (cam is null) return;
            var e = Selecao.Numero(s.Ed, "Espaçamento (m)", Math.Max(2, s.Cfg.Velocidade / s.Normas.DivisorEspacamentoCones)); if (e is null) return;
            var c = new Caminho(cam.Vertices, cam.Fechado);
            int n = 0;
            for (double d = 0; d <= c.Comprimento + 1e-6; d += Math.Max(0.5, e.Value))
            {
                var p = c.Ponto(d);
                s.Criar(new ObjetoSin { Categoria = Categoria.Obra, Codigo = kw, Situacao = s.Cfg.SituacaoPadrao, X = p.X, Y = p.Y, Rotacao = c.Tangente(d).Angulo, Dispositivo = new DadosDispositivo() });
                n++;
            }
            s.Confirmar();
            Ctx.Msg($"{n} {kw} lançados a cada {Num.F(e.Value, 2)} m.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINOBRAZONA", CommandFlags.Modal)]
    public void Zona()
    {
        try
        {
            using var s = Sessao.Abrir();
            var cam = Selecao.PedirCaminho(s.Ed, s.Tr, "Selecione o eixo no SENTIDO DO TRÁFEGO que passa pela obra");
            if (cam is null) return;
            var c = new Caminho(cam.Vertices, cam.Fechado);
            var p0 = Selecao.Ponto(s.Ed, "Início da área de trabalho (clique próximo ao eixo)"); if (p0 is null) return;
            var p1 = Selecao.Ponto(s.Ed, "Fim da área de trabalho", p0); if (p1 is null) return;
            var (s0, off0, _) = c.Projetar(p0.Value);
            var (s1, _, _) = c.Projetar(p1.Value);
            if (s1 < s0) (s0, s1) = (s1, s0);
            var lado = Selecao.Palavra(s.Ed, "A faixa interditada fica à", new[] { "Direita", "Esquerda" }, off0 < 0 ? "Direita" : "Esquerda");
            if (lado is null) return;
            var w = Selecao.Numero(s.Ed, "Largura da faixa interditada (m)", 3.5); if (w is null) return;
            var d = Selecao.Numero(s.Ed, "Deslocamento do eixo da faixa interditada em relação ao caminho (+ esq / − dir)", lado == "Direita" ? -w.Value / 2 : w.Value / 2, true); if (d is null) return;
            var v = Selecao.Numero(s.Ed, "Velocidade regulamentada (km/h)", s.Cfg.Velocidade); if (v is null) return;
            var disp = Selecao.Palavra(s.Ed, "Dispositivo de canalização", new[] { "CONE", "TAMBOR", "CILINDRO", "BALIZADOR" }, "CONE"); if (disp is null) return;
            var o = new ObjetoSin
            {
                Categoria = Categoria.ZonaObra, Codigo = "ZONA", Situacao = Situacao.Implantar, Caminho = cam.Vertices,
                Zona = new ParamZonaObra { InicioTrabalho = s0, ComprimentoTrabalho = Math.Max(1, s1 - s0), Velocidade = v.Value, LarguraFaixa = w.Value, DeslocamentoFaixa = d.Value, FechaDireita = lado == "Direita", Dispositivo = disp },
            };
            s.Criar(o);
            s.Confirmar();
            Ctx.Msg($"Zona de obra {o.Id}: taper {Num.F(o.Q("taper"), 1)} m, proteção {Num.F(o.Q("protecao"), 1)} m, {o.Q("dispositivos")} {disp.ToLowerInvariant()}s e {o.Q("placas")} placas.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINDEFENSA", CommandFlags.Modal)]
    public void Defensa()
    {
        try
        {
            using var s = Sessao.Abrir();
            var kw = Selecao.Palavra(s.Ed, "Tipo", new[] { "Simples", "Dupla", "Rigida", "Concreto" }, "Simples");
            if (kw is null) return;
            var tipo = kw switch { "Dupla" => TipoDefensa.SemimaleavelDupla, "Rigida" => TipoDefensa.Rigida, "Concreto" => TipoDefensa.BarreiraConcreto, _ => TipoDefensa.SemimaleavelSimples };
            var cam = Selecao.PedirCaminho(s.Ed, s.Tr, "Selecione o alinhamento da defensa");
            if (cam is null) return;
            var p = new ParamDefensa { Tipo = tipo };
            if (tipo != TipoDefensa.BarreiraConcreto)
            {
                var ti = Selecao.Palavra(s.Ed, "Terminal no início", new[] { "Abatido", "Ancorado", "Nenhum" }, "Abatido"); if (ti is null) return;
                var tf = Selecao.Palavra(s.Ed, "Terminal no fim", new[] { "Abatido", "Ancorado", "Nenhum" }, "Abatido"); if (tf is null) return;
                static TipoTerminal T(string x) => x switch { "Abatido" => TipoTerminal.Abatido, "Ancorado" => TipoTerminal.AncoradoTalude, _ => TipoTerminal.Nenhum };
                p.TerminalInicio = T(ti); p.TerminalFim = T(tf);
                var lado = Selecao.Palavra(s.Ed, "O tráfego fica à (no sentido da linha)", new[] { "Esquerda", "Direita" }, "Esquerda"); if (lado is null) return;
                p.LadoTrafego = lado == "Esquerda" ? 1 : -1;
            }
            var o = new ObjetoSin { Categoria = Categoria.Defensa, Situacao = s.Cfg.SituacaoPadrao, Caminho = cam.Vertices, Defensa = p };
            s.Criar(o);
            s.Confirmar();
            Ctx.Msg($"{o.Id}: {o.Descricao}, {Num.F(o.Q("comp"), 2)} m, {o.Q("postes")} postes, {o.Q("terminais")} terminais.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }
}
