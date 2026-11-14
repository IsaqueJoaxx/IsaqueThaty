using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Infra;
using Projeta.Sinalizacao.UI;

namespace Projeta.Sinalizacao.TesteUI;

/// <summary>Abre cada janela do plugin, simula algumas escolhas e salva uma captura PNG.</summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string saida = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "projeta_sin_ui");
        Directory.CreateDirectory(saida);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        int falhas = 0;
        void Capturar(Form f, string nome)
        {
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(20, 20);
            f.Show();
            Application.DoEvents();
            using var bmp = new Bitmap(f.Width, f.Height);
            f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
            bmp.Save(Path.Combine(saida, nome + ".png"));
            Console.WriteLine("ok   " + nome);
        }
        T Priv<T>(object o, string campo) => (T)o.GetType().GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(o)!;
        void Chamar(object o, string metodo) => o.GetType().GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(o, null);

        try
        {
            var cfg = new Config();
            var n = cfg.Normas();
            using (var f = new FormPlaca(cfg, n))
            {
                var busca = Priv<TextBox>(f, "_busca");
                busca.Text = "R-19";
                Application.DoEvents();
                Chamar(f, "Adicionar");
                busca.Text = "R-6a";
                Application.DoEvents();
                Chamar(f, "Adicionar");
                busca.Text = "";
                Priv<ComboBox>(f, "_familia").SelectedIndex = 3;
                Application.DoEvents();
                Capturar(f, "01_inserir_placa");
                Chamar(f, "Concluir");
                var r = f.Resultado;
                if (r is null || r.Placas.Count != 2 || r.Placas[0].Codigo != "R-19") { Console.WriteLine("FALHA conjunto da janela de placas"); falhas++; }
                else Console.WriteLine($"ok   conjunto: {string.Join(" + ", r.Placas.Select(p => p.Codigo))}, suporte {r.Suporte}");
                f.Close();
            }
            using (var f = new FormModelador(n, 60))
            {
                Capturar(f, "02_modelador");
                f.Close();
            }
            var o = new ObjetoSin { Id = "PL-001", Categoria = Categoria.Placa, Codigo = "R-19", Placa = new DadosPlaca { Placas = { new PlacaItem { Codigo = "R-19", Valor = "40", Dimensao = 0.5 } } } };
            Motor.Gerar(o, n, cfg.Exibicao());
            var ed = Edicao.Para(o);
            using (var f = new Form { Text = "Editar", Size = new Size(520, 600) })
            {
                f.Controls.Add(new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = ed, PropertySort = PropertySort.Categorized });
                Capturar(f, "03_editar_placa");
                f.Close();
            }
            var ml = new ObjetoSin { Id = "ML-001", Categoria = Categoria.MarcaLongitudinal, Codigo = "LFO-2", Caminho = new() { new(0, 0), new(100, 0) }, Marca = new DadosMarca { Velocidade = 50 } };
            Motor.Gerar(ml, n, cfg.Exibicao());
            using (var f = new Form { Text = "Editar marca", Size = new Size(520, 600) })
            {
                f.Controls.Add(new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = Edicao.Para(ml), PropertySort = PropertySort.Categorized });
                Capturar(f, "04_editar_marca");
                f.Close();
            }
            using (var f = new Form { Text = "Padrões", Size = new Size(520, 600) })
            {
                f.Controls.Add(new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = cfg, PropertySort = PropertySort.Categorized });
                Capturar(f, "05_padroes");
                f.Close();
            }
            using (var f = new Form { Text = "Painel", Size = new Size(290, 900) })
            {
                f.Controls.Add(new Painel { Dock = DockStyle.Fill });
                Capturar(f, "06_painel");
                f.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("FALHA " + ex);
            falhas++;
        }
        Console.WriteLine(falhas == 0 ? "UI OK" : $"UI COM {falhas} FALHA(S)");
        return falhas;
    }
}
