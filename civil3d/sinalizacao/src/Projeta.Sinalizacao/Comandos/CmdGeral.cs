using Autodesk.AutoCAD.Runtime;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Normas;
using Projeta.Sinalizacao.Infra;

namespace Projeta.Sinalizacao.Comandos;

/// <summary>Painel, ajuda, padrões do projeto e normas.</summary>
public sealed class CmdGeral
{
    public static readonly (string Grupo, (string Rotulo, string Comando, string Dica)[] Itens)[] Grupos =
    {
        ("Projeto", new[]
        {
            ("Padrões do projeto", "SINPADROES", "Escala, tipo de via, velocidade, materiais e arquivo de normas"),
            ("Normas (larguras, cadências…)", "SINNORMAS", "Abre o normas.json com os parâmetros do CONTRAN"),
        }),
        ("Sinalização horizontal", new[]
        {
            ("Via completa", "SINVIA", "Eixo + divisão de faixas + bordos de uma vez"),
            ("Marca longitudinal", "SINFAIXA", "LFO, LMS, LBO, LCO, MFE, MFR, MCI, LCA, LPP"),
            ("Transversal", "SINTRANSV", "LRE, LDP, faixa de pedestres FTP-1/FTP-2, LRV, MCC"),
            ("Zebrado (ZPA)", "SINZEBRADO", "Preenche área não utilizável"),
            ("Seta", "SINSETA", "PEM (siga, vire, retorno) e MOF"),
            ("Legenda", "SINLEGENDA", "PARE, ESCOLA, ÔNIBUS…"),
            ("Símbolo", "SINSIMBOLO", "Dê a preferência, bicicleta, deficiente, saúde"),
            ("Tachas / tachões", "SINTACHAS", "Ao longo de uma linha"),
        }),
        ("Sinalização vertical", new[]
        {
            ("Inserir placa", "SINPLACA", "Catálogo CONTRAN com prévia; várias placas no mesmo suporte"),
            ("Modelador de placas", "SINPLACAPRO", "Indicação, educativas e serviços: tamanho pelo texto"),
            ("Acrescentar placa ao suporte", "SINPLACAADD", "Empilha mais uma placa num conjunto"),
            ("Trocar suporte", "SINSUPORTE", "Coluna, braço, semipórtico, pórtico"),
            ("Importar cadastro", "SINIMPORTAR", "CSV/XLSX com COD, SUP, X, Y"),
            ("Modelo de cadastro", "SINMODELOCADASTRO", "Gera um CSV de exemplo"),
        }),
        ("Semafórica, obras e defensas", new[]
        {
            ("Semafórica", "SINSEMAFORO", "Grupos focais, colunas, botoeira, controlador"),
            ("Rede semafórica", "SINREDE", "Subterrânea ou aérea"),
            ("Dispositivo de obra", "SINOBRA", "Cone, tambor, cavalete, barreira…"),
            ("Dispositivos ao longo", "SINOBRALINHA", "Cones a cada N metros"),
            ("Zona de obra", "SINOBRAZONA", "Interdição de faixa: placas, taper, proteção"),
            ("Defensa / barreira", "SINDEFENSA", "Semimaleável, rígida ou concreto"),
        }),
        ("Projeto pronto", new[]
        {
            ("Consultar / editar", "SINCONSULTA", "Dados e quantidades de um objeto"),
            ("Situação", "SINSITUACAO", "Implantar, manter, remover, recuperar"),
            ("Corrigir projeto", "SINCORRIGE", "Regenera tudo com os padrões atuais"),
            ("Localizar", "SINLOCALIZAR", "Por ID ou código"),
            ("Numerar", "SINNUMERAR", "Renumera pela ordem das estacas"),
            ("Quantitativos (Excel)", "SINQUANT", "Resumo e detalhamento com estaca e lado"),
            ("Modelo 3D (BIM)", "SIN3D", "Placas, suportes, semafórica, obras, defensas"),
            ("Apagar 3D", "SIN3DAPAGAR", "Remove os sólidos gerados"),
            ("Demonstração", "SINDEMO", "Monta um projeto de exemplo (autoteste)"),
        }),
    };

    [CommandMethod("PROJETA_SIN", "SINPAINEL", CommandFlags.Modal)]
    public void Painel()
    {
        try
        {
            if (!Ctx.ComInterface) { Ajuda(); return; }
            UI.Janelas.MostrarPainel();
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINAJUDA", CommandFlags.Modal)]
    public void Ajuda()
    {
        try
        {
            Ctx.Msg("PROJETA • Sinalização Viária — comandos:");
            foreach (var (g, itens) in Grupos)
            {
                Ctx.Msg($"  {g.ToUpperInvariant()}");
                foreach (var (r, c, d) in itens) Ctx.Msg($"    {c,-18} {r} — {d}");
            }
            Ctx.Msg("  Versões de linha de comando: -SINPLACA, -SINPADROES.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINPADROES", CommandFlags.Modal)]
    public void Padroes()
    {
        try
        {
            if (!Ctx.ComInterface) { PadroesLinha(); return; }
            using var s = Sessao.Abrir();
            var (ok, padrao) = UI.Janelas.EditarPadroes(s.Cfg);
            if (!ok) return;
            if (padrao) s.Cfg.SalvarComoPadrao();
            s.Confirmar();
            Ctx.Msg("Padrões gravados no desenho" + (padrao ? " e como padrão para novos desenhos." : ".") + " Use SINCORRIGE para aplicar aos objetos existentes.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "-SINPADROES", CommandFlags.Modal)]
    public void PadroesLinha()
    {
        try
        {
            using var s = Sessao.Abrir();
            var c = s.Cfg;
            var e = Selecao.Numero(s.Ed, "Escala 1:N", c.Escala); if (e is null) return; c.Escala = e.Value;
            var v = Selecao.Palavra(s.Ed, "Tipo de via", Enum.GetNames<TipoVia>(), c.Via.ToString()); if (v is null) return; c.Via = Enum.Parse<TipoVia>(v);
            var vel = Selecao.Numero(s.Ed, "Velocidade padrão (km/h)", c.Velocidade); if (vel is null) return; c.Velocidade = vel.Value;
            var pl = Selecao.Numero(s.Ed, "Tamanho de uma placa de 0,50 m no papel (mm)", c.TamanhoPlacaPapel); if (pl is null) return; c.TamanhoPlacaPapel = pl.Value;
            var tx = Selecao.Numero(s.Ed, "Altura do texto no papel (mm)", c.AlturaTextoPapel); if (tx is null) return; c.AlturaTextoPapel = tx.Value;
            var es = Selecao.Numero(s.Ed, "Estaqueamento (20 = estacas de 20 m; 1000 = km)", c.PassoEstaca); if (es is null) return; c.PassoEstaca = es.Value;
            s.Confirmar();
            Ctx.Msg("Padrões gravados. Use SINCORRIGE para aplicar aos objetos existentes.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINNORMAS", CommandFlags.Modal)]
    public void NormasCmd()
    {
        try
        {
            using var s = Sessao.Abrir();
            string arq = string.IsNullOrWhiteSpace(s.Cfg.ArquivoNormas) ? Config.ArquivoNormasPadrao : s.Cfg.ArquivoNormas;
            if (!File.Exists(arq)) { new Normas().Salvar(arq); Ctx.Msg($"Criado {arq} com os valores padrão."); }
            Ctx.Msg($"Normas em uso: {arq}");
            Ctx.Msg("Valores que não foram conferidos diretamente no manual (prática usual — confira para o seu projeto):");
            foreach (var x in Normas.NaoConferidos) Ctx.Msg("  • " + x);
            Ctx.Msg("Depois de editar e salvar o arquivo, rode SINCORRIGE para regenerar os objetos.");
            if (Ctx.ComInterface) try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe", $"\"{arq}\"") { UseShellExecute = true }); } catch { }
            s.Confirmar();
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }
}
