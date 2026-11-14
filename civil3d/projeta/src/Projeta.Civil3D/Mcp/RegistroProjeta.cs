using Projeta.Mcp;

namespace Projeta.Civil3D.Mcp;

/// <summary>
/// Todas as ferramentas que o Claude enxerga. Para expor uma rotina nova do PROJETA:
///   1. escreva um método estático (Argumentos → ResultadoFerramenta) — ele roda na thread principal, com o documento travado;
///   2. registre com new FerramentaDelegada(nome, título, descrição, esquema, método, somenteLeitura: ...);
///   3. lance ErroFerramenta/ErroArgumento para erros esperados (a mensagem volta para o Claude);
///   4. rode PRJMCPTESTE e depois peça ao Claude para usar.
/// A descrição é o "manual" que o Claude lê: diga quando usar, unidades e o que volta.
/// </summary>
internal static class RegistroProjeta
{
    public static RegistroFerramentas Criar()
    {
        var r = new RegistroFerramentas();
        FerramentasCivil.Registrar(r);
        FerramentasRedes.Registrar(r);
        FerramentasPavimentacao.Registrar(r);
        FerramentasSuperficies.Registrar(r);
        return r;
    }
}
