using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Core.Comum;
using Projeta.Core.Dimensionamento;
using Projeta.Core.Redes;

namespace Projeta.Civil3D.Comandos;

/// <summary>Execução padronizada dos comandos e rotina de atualização das redes.</summary>
internal static class Operacoes
{
    /// <summary>Abre a sessão, executa o corpo e trata erros. O corpo decide se confirma (SalvarEConfirmar).</summary>
    public static void Rodar(string nome, Action<Sessao> corpo)
    {
        try
        {
            using var s = Sessao.Abrir();
            corpo(s);
        }
        catch (System.Exception ex)
        {
            Ctx.Msg($"[ERRO] {nome}: {ex.Message}");
#if DEBUG
            Ctx.Msg(ex.StackTrace ?? "");
#endif
        }
    }

    /// <summary>Sincroniza com o desenho, amostra o terreno, dimensiona (gravidade) e redesenha.</summary>
    public static void Atualizar(Sessao s, Rede rede, bool sincronizar = true, bool redesenhar = true, bool calcular = true)
    {
        if (sincronizar) Ctx.Msgs(DesenhoRede.Sincronizar(s, rede), false);
        int fora = CivilUtil.AmostrarTerreno(s, rede);
        if (fora > 0) Ctx.Msg($"[AVISO] {fora} nó(s) fora da superfície e sem cota de tampa forçada.");
        if (calcular && rede.Tipo != TipoRede.Agua && rede.Trechos.Count > 0)
        {
            rede.Resultado = DimensionadorGravidade.Calcular(rede, s.Projeto);
            Resumo(rede.Resultado);
        }
        if (redesenhar)
        {
            var erros = DesenhoRede.Redesenhar(s, rede);
            foreach (var e in erros.Take(12)) Ctx.Msg("[DESENHO] " + e);
            if (erros.Count > 12) Ctx.Msg($"[DESENHO] ... e mais {erros.Count - 12} mensagens.");
        }
    }

    public static void Resumo(ResultadoRede res)
    {
        Ctx.Msgs(res.Mensagens.Where(m => m.Gravidade != Gravidade.Info));
        int erros = res.Trechos.Count(t => t.Erros.Count > 0), avisos = res.Trechos.Count(t => t.Erros.Count == 0 && t.Avisos.Count > 0);
        Ctx.Msg($"Dimensionamento: {res.Trechos.Count} trechos — {erros} com erro, {avisos} com aviso. {res.Metodo}");
        foreach (var t in res.Trechos.Where(t => t.Erros.Count > 0).Take(8)) Ctx.Msg($"  [ERRO] {t.Nome}: {string.Join("; ", t.Erros)}");
    }

    public static string PrefixoLayer(TipoRede t) => t switch
    {
        TipoRede.Esgoto => "PRJ-ESG",
        TipoRede.Agua => "PRJ-AGU",
        TipoRede.Generica => "PRJ-GEN",
        _ => "PRJ-DRE",
    };

    public static TipoRede? LerTipo(string? s) => s switch
    {
        "Drenagem" => TipoRede.Drenagem,
        "Esgoto" => TipoRede.Esgoto,
        "Agua" => TipoRede.Agua,
        "Generica" => TipoRede.Generica,
        _ => null,
    };

    public static Rede NovaRede(Projeto p, string nome, TipoRede tipo)
    {
        var r = new Rede { Nome = nome, Tipo = tipo, PrefixoLayer = PrefixoLayer(tipo) };
        switch (tipo)
        {
            case TipoRede.Esgoto: r.PrefixoNo = "PV-"; r.DispositivoPadraoPV = "PV_CIRCULAR"; break;
            case TipoRede.Agua: r.PrefixoNo = "N-"; r.PrefixoTrecho = "TB-"; r.DispositivoPadraoPV = "MARCADOR"; r.AbrirFurosNosPVs = false; break;
        }
        // nomes únicos de rede
        var nomes = p.Redes.Select(x => x.Nome).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int k = 2; string baseNome = nome;
        while (nomes.Contains(r.Nome)) r.Nome = $"{baseNome} ({k++})";
        p.Redes.Add(r);
        return r;
    }
}
