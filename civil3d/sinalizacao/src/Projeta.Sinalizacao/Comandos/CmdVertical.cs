using Autodesk.AutoCAD.Runtime;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Vertical;
using Projeta.Sinalizacao.Infra;

namespace Projeta.Sinalizacao.Comandos;

/// <summary>Sinalização vertical: placas do catálogo, placas montadas no modelador, conjuntos e suportes.</summary>
public sealed class CmdVertical
{
    internal static readonly string[] KwSuportes = { "Simples", "Dupla", "Tripla", "Braco", "BracoDuplo", "SemiSimples", "SemiDuplo", "Portico", "Existente" };
    internal static TipoSuporte SuporteDeKw(string kw) => kw switch
    {
        "Dupla" => TipoSuporte.ColunaDupla, "Tripla" => TipoSuporte.ColunaTripla, "Braco" => TipoSuporte.BracoProjetado,
        "BracoDuplo" => TipoSuporte.BracoProjetadoDuplo, "SemiSimples" => TipoSuporte.SemiPorticoSimples, "SemiDuplo" => TipoSuporte.SemiPorticoDuplo,
        "Portico" => TipoSuporte.Portico, "Existente" => TipoSuporte.Existente, _ => TipoSuporte.ColunaSimples,
    };

    /// <summary>Pede posição e orientação e insere o conjunto (repete até Enter).</summary>
    internal static int InserirRepetindo(Sessao s, DadosPlaca dp, Situacao sit)
    {
        int n = 0;
        while (true)
        {
            var p = Selecao.Ponto(s.Ed, "Posição do suporte <terminar>");
            if (p is null) break;
            var q = Selecao.Ponto(s.Ed, "Ponto de onde vem o tráfego que lê a placa (a face fica voltada para ele)", p);
            if (q is null) break;
            var o = new ObjetoSin
            {
                Categoria = Categoria.Placa, Situacao = sit, X = p.Value.X, Y = p.Value.Y,
                Placa = Clonar(dp),
            };
            o.Placa.Azimute = (q.Value - p.Value).Angulo;
            o.Codigo = string.Join(" + ", dp.Placas.Select(x => x.Codigo));
            s.Criar(o);
            n++;
            s.Tr.TransactionManager.QueueForGraphicsFlush();
        }
        return n;
    }

    private static DadosPlaca Clonar(DadosPlaca d)
    {
        var o = new ObjetoSin { Placa = d };
        return o.Clonar().Placa!;
    }

    [CommandMethod("PROJETA_SIN", "SINPLACA", CommandFlags.Modal)]
    public void Placa()
    {
        try
        {
            if (!Ctx.ComInterface) { PlacaLinha(); return; }
            using var s = Sessao.Abrir();
            var r = UI.Janelas.EscolherPlaca(s.Cfg, s.Normas);
            if (r is null) return;
            int n = InserirRepetindo(s, r.Value.Dados, r.Value.Situacao);
            s.Confirmar();
            Ctx.Msg($"{n} conjunto(s) de placas inserido(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "-SINPLACA", CommandFlags.Modal)]
    public void PlacaLinha()
    {
        try
        {
            using var s = Sessao.Abrir();
            var cods = Selecao.Texto(s.Ed, "Código(s) da(s) placa(s) no mesmo suporte, de cima para baixo (ex.: R-19+R-6a)", "R-1", false);
            if (cods is null) return;
            var dp = new DadosPlaca { Chapa = s.Cfg.Chapa, Pelicula = s.Cfg.Pelicula, MaterialSuporte = s.Cfg.MaterialSuporte };
            foreach (var c in cods.Split('+', StringSplitOptions.RemoveEmptyEntries))
            {
                var t = Catalogo.Obter(c);
                if (t is null) { Ctx.Msg($"Código {c} não encontrado no catálogo — entra como placa genérica."); dp.Placas.Add(new PlacaItem { Codigo = c.Trim() }); continue; }
                var item = new PlacaItem { Codigo = t.Codigo };
                Ctx.Msg($"{t.Codigo} — {t.Nome}");
                if (t.ValorPadrao is not null)
                {
                    var v = Selecao.Texto(s.Ed, $"Valor exibido na {t.Codigo}", t.ValorPadrao); if (v is null) return;
                    item.Valor = v;
                }
                double padrao = GeradorPlaca.DimensaoPadrao(t, s.Normas, s.Cfg.Via);
                var d = Selecao.Numero(s.Ed, $"Dimensão ({(t.Forma == Forma.Circular ? "diâmetro" : "lado")}, opções: {string.Join(" / ", GeradorPlaca.Opcoes(t, s.Normas).Select(Num.M))})", padrao);
                if (d is null) return;
                item.Dimensao = d.Value;
                if (t.Familia == Familia.Advertencia)
                {
                    var ob = Selecao.Palavra(s.Ed, "Sinalização de obra (fundo laranja)?", new[] { "Sim", "Nao" }, "Nao");
                    if (ob is null) return;
                    item.Obra = ob == "Sim";
                }
                dp.Placas.Add(item);
            }
            if (dp.Placas.Count == 0) return;
            var su = Selecao.Palavra(s.Ed, "Suporte", KwSuportes, "Simples");
            if (su is null) return;
            dp.Suporte = SuporteDeKw(su);
            if (Suportes.Suspenso(dp.Suporte))
            {
                var vao = Selecao.Numero(s.Ed, "Vão/comprimento do braço ou viga (m)", dp.Suporte == TipoSuporte.Portico ? 12 : dp.Suporte is TipoSuporte.BracoProjetado or TipoSuporte.BracoProjetadoDuplo ? s.Normas.ComprimentoBracoProjetado : 6);
                if (vao is null) return;
                dp.Vao = vao.Value;
            }
            var sit = Selecao.Palavra(s.Ed, "Situação", new[] { "Implantar", "Manter", "Remover", "Recuperar" }, s.Cfg.SituacaoPadrao.ToString());
            if (sit is null) return;
            int n = InserirRepetindo(s, dp, Enum.Parse<Situacao>(sit));
            s.Confirmar();
            Ctx.Msg($"{n} conjunto(s) inserido(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINPLACAPRO", CommandFlags.Modal)]
    public void Modelador()
    {
        try
        {
            if (!Ctx.ComInterface) { Ctx.Msg("O modelador de placas precisa da interface do Civil 3D."); return; }
            using var s = Sessao.Abrir();
            var m = UI.Janelas.Modelador(s.Normas, s.Cfg.Velocidade);
            if (m is null) return;
            var dp = new DadosPlaca
            {
                Placas = { new PlacaItem { Codigo = m.Codigo, Modelo = m.ParaJson() } },
                Chapa = s.Cfg.Chapa, Pelicula = s.Cfg.Pelicula, MaterialSuporte = s.Cfg.MaterialSuporte,
            };
            var lay = GeradorIndicacao.Montar(m);
            Ctx.Msg($"Placa {m.Codigo}: {Num.M(lay.Largura)} × {Num.M(lay.Altura)} m = {Num.F(lay.Area, 3)} m².");
            var su = Selecao.Palavra(s.Ed, "Suporte", KwSuportes, lay.Largura > 1.5 ? "Dupla" : "Simples");
            if (su is null) return;
            dp.Suporte = SuporteDeKw(su);
            if (Suportes.Suspenso(dp.Suporte))
            {
                var vao = Selecao.Numero(s.Ed, "Vão/comprimento do braço ou viga (m)", 6); if (vao is null) return;
                dp.Vao = vao.Value;
            }
            int n = InserirRepetindo(s, dp, s.Cfg.SituacaoPadrao);
            s.Confirmar();
            Ctx.Msg($"{n} placa(s) do modelador inserida(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINPLACAADD", CommandFlags.Modal)]
    public void AdicionarAoConjunto()
    {
        try
        {
            using var s = Sessao.Abrir();
            var sel = s.PedirObjeto("Selecione o conjunto de placas");
            if (sel is null) return;
            var (id, o) = sel.Value;
            if (o.Categoria != Categoria.Placa) { Ctx.Msg("Selecione um conjunto de placas."); return; }
            var c = Selecao.Texto(s.Ed, "Código da placa a acrescentar (embaixo das existentes)", "R-6a", false);
            if (c is null) return;
            var t = Catalogo.Obter(c);
            var item = new PlacaItem { Codigo = t?.Codigo ?? c };
            if (t?.ValorPadrao is not null) { var v = Selecao.Texto(s.Ed, "Valor", t.ValorPadrao); if (v is null) return; item.Valor = v; }
            o.Placa!.Placas.Add(item);
            s.Atualizar(id, o);
            s.Confirmar();
            Ctx.Msg($"{o.Id}: agora com {o.Placa.Placas.Count} placas ({o.Codigo}).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINSUPORTE", CommandFlags.Modal)]
    public void TrocarSuporte()
    {
        try
        {
            using var s = Sessao.Abrir();
            var su = Selecao.Palavra(s.Ed, "Novo suporte", KwSuportes, "Simples");
            if (su is null) return;
            var objs = s.SelecionarObjetos("Selecione os conjuntos de placas", false).Where(x => x.Obj.Categoria == Categoria.Placa).ToList();
            foreach (var (id, o) in objs) { o.Placa!.Suporte = SuporteDeKw(su); s.Atualizar(id, o); }
            s.Confirmar();
            Ctx.Msg($"{objs.Count} suporte(s) alterado(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }
}
