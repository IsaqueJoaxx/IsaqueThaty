using System.Drawing;
using System.Windows.Forms;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.UI;

/// <summary>Painel lateral (PaletteSet) com os comandos agrupados por bloco.</summary>
internal sealed class PainelProjeta : UserControl
{
    public static readonly (string Grupo, (string Rotulo, string Comando, string Dica)[] Itens)[] Grupos =
    {
        ("Rede", new[]
        {
            ("Criar rede", "PRJREDE", "Cria uma rede de drenagem, esgoto, água ou genérica"),
            ("Opções da rede", "PRJOPCOES", "Parâmetros de cálculo, prefixos e desenho"),
            ("Equações de chuva", "PRJEQUACOES", "Catálogo IDF e equações do projeto"),
            ("Lançar PVs e tubos", "PRJLANCAR", "Clique os pontos em sequência (montante → jusante)"),
            ("Inserir nó", "PRJNO", "PV, boca de lobo, caixa ou exutório avulso"),
            ("Ligar tubo", "PRJTUBO", "Liga dois nós existentes"),
            ("Criar bacia", "PRJBACIA", "Polilinha fechada → bacia de contribuição"),
            ("Propriedades", "PRJPROPRIEDADES", "Edita nó, tubo ou bacia"),
        }),
        ("Edição", new[]
        {
            ("Conectar", "PRJCONECTAR", "Muda o nó de jusante de um tubo"),
            ("Inverter sentido", "PRJINVERTER", "Inverte montante/jusante dos tubos"),
            ("Dividir tubo", "PRJDIVIDIR", "Insere um nó no meio de um tubo"),
            ("Trocar dispositivo", "PRJTROCAR", "Troca o modelo dos nós selecionados"),
            ("Remover", "PRJREMOVER", "Remove nós, tubos ou bacias da rede"),
            ("Numerar", "PRJNUMERAR", "Renumera nós e trechos por coletor"),
        }),
        ("Cálculo e saídas", new[]
        {
            ("Calcular (planilha)", "PRJCALCULAR", "Dimensiona e abre a planilha editável"),
            ("Atualizar", "PRJATUALIZAR", "Lê o desenho, recalcula e redesenha"),
            ("Tabela no desenho", "PRJTABELA", "Dimensionamento, nota de PVs, materiais, vala"),
            ("Relatório HTML", "PRJRELATORIO", "Relatório completo da rede"),
            ("Nota de serviço PVs", "PRJNOTACAIXAS", "CSV para o Excel"),
            ("Projetar no perfil", "PRJPERFIL", "Desenha tubos e PVs em um profile view"),
            ("Interferências", "PRJINTERFERENCIA", "Cruzamentos entre redes"),
            ("Escavação de valas", "PRJESCAVACAO", "Volumes por faixa de profundidade"),
        }),
        ("Integração", new[]
        {
            ("Importar Pipe Network", "PRJIMPORTARPIPE", "Converte uma rede do Civil 3D"),
            ("Devolver para a Pipe Network", "PRJDEVOLVERPIPE", "Grava diâmetros e cotas calculados na rede do Civil 3D"),
            ("Importar planilha", "PRJIMPORTARCSV", "Rede a partir de CSV (Excel)"),
            ("Exportar planilha", "PRJEXPORTARCSV", "Rede para CSV"),
            ("Modelo de planilha", "PRJMODELOCSV", "Gera um CSV de exemplo"),
        }),
        ("Longitudinais", new[]
        {
            ("Valeta / sarjeta", "PRJLONGITUDINAL", "Lança dispositivo ao longo de um alinhamento"),
            ("Editar longitudinal", "PRJLONGEDITAR", "Altera e regenera um dispositivo"),
            ("Quantitativos", "PRJLONGQUANT", "Concreto, escavação, capacidade"),
        }),
        ("Alinhamento", new[]
        {
            ("Apagar PI", "PRJAPAGARPI", "Apaga o PI clicado mesmo com restrições (refaz o eixo no mesmo alinhamento)"),
            ("Apagar PIV (greide)", "PRJAPAGARPIV", "Apaga o PIV clicado no greide mesmo com restrições (refaz o greide no mesmo perfil)"),
        }),
        ("Superfícies", new[]
        {
            ("Volumes entre superfícies", "PRJVOLUMES", "Corte e aterro (terreno × projeto ou levantamentos), por polígono, com mapa de cores e Excel"),
            ("Projeto × obra", "PRJCONFORMIDADE", "Compara o levantamento da obra com o projeto: o que está fora da tolerância, mapa, pontos críticos, controle por estaca e Excel"),
        }),
        ("Terraplenagem", new[]
        {
            ("Platôs", "PRJPLATO", "Platô em cada polígono: cota que compensa corte e aterro (ou fixa), caimento, taludes até o terreno, superfícies, volumes e Excel"),
            ("Volumes por seções (cubação)", "PRJCUBACAO", "Nota de cubação pelo grupo de seções do Civil: lista de materiais ou áreas entre terreno e projeto, compensação lateral, Brückner e Excel"),
        }),
        ("Pavimentação", new[]
        {
            ("Nota de serviço (corredor)", "PRJNOTASERVICO", "Planilha Excel no layout E-NS: eixo, bordos, sarjeta, meio-fio e passeio a cada 10 m"),
        }),
        ("SWMM / EPANET", new[]
        {
            ("SWMM: simular", "PRJSWMMCALCULAR", "Exporta, roda o SWMM e mostra resultados"),
            ("SWMM: exportar", "PRJSWMMEXPORTAR", "Gera o arquivo .inp"),
            ("SWMM: importar", "PRJSWMMIMPORTAR", "Cria rede a partir de .inp"),
            ("EPANET: simular", "PRJEPANETCALCULAR", "Roda o EPANET (e ajusta diâmetros)"),
            ("EPANET: exportar", "PRJEPANETEXPORTAR", "Gera o arquivo .inp"),
            ("EPANET: importar", "PRJEPANETIMPORTAR", "Cria rede a partir de .inp"),
            ("Projeção populacional", "PRJPOPULACAO", "Aritmético, geométrico, logístico..."),
            ("Configurar motores", "PRJMOTORES", "Caminhos do runswmm/runepanet"),
        }),
        ("Modelador", new[]
        {
            ("Modelador", "PRJMODELADOR", "Cria e edita dispositivos e seções"),
            ("Inserir dispositivo", "PRJDISPOSITIVO", "Insere um dispositivo avulso"),
            ("Demonstração", "PRJDEMO", "Monta uma rede de exemplo completa (autoteste)"),
            ("Ajuda", "PRJAJUDA", "Lista de comandos"),
        }),
    };

    public PainelProjeta()
    {
        Font = UiUtil.FonteNormal;
        var fluxo = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(4) };
        var dicas = new ToolTip();
        foreach (var (grupo, itens) in Grupos)
        {
            fluxo.Controls.Add(new Label { Text = grupo.ToUpperInvariant(), AutoSize = false, Width = 230, Height = 22, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.SteelBlue, Padding = new Padding(0, 6, 0, 0) });
            foreach (var (rotulo, cmd, dica) in itens)
            {
                var b = new Button { Text = rotulo, Width = 230, Height = 26, TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.System };
                string c = cmd;
                b.Click += (_, _) =>
                {
                    var doc = AcApp.DocumentManager.MdiActiveDocument;
                    doc?.SendStringToExecute("\x03\x03" + c + " ", true, false, true);
                };
                dicas.SetToolTip(b, $"{cmd} — {dica}");
                fluxo.Controls.Add(b);
            }
        }
        Controls.Add(fluxo);
    }
}
