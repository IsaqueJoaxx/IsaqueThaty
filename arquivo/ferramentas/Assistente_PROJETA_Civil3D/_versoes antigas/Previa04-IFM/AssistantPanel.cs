using System.Drawing;
using System.Windows.Forms;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Projeta.Civil3D;

internal sealed class AssistantPanel : UserControl
{
    private readonly Label status;
    private readonly ListView surfaces;

    public AssistantPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(24, 34, 45);
        ForeColor = Color.FromArgb(230, 237, 245);
        Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(16),
            BackColor = BackColor, ForeColor = ForeColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.Controls.Add(new Label { Text = "PROJETA / ASSISTENTE CIVIL — IFM", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(101, 212, 186) }, 0, 0);
        layout.Controls.Add(new Label { Text = "BASE EXPERIMENTAL · SOMENTE LEITURA\nListe as superfícies do desenho ativo. Nenhum cálculo ou alteração é realizado.", Dock = DockStyle.Fill }, 0, 1);
        var refresh = new Button { Text = "Consultar superfícies do desenho ativo", Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(101, 212, 186), ForeColor = Color.FromArgb(16, 45, 39), FlatStyle = FlatStyle.Flat };
        refresh.Click += (_, _) => QueueRead();
        layout.Controls.Add(refresh, 0, 2);
        status = new Label { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0), AutoEllipsis = true };
        layout.Controls.Add(status, 0, 3);
        surfaces = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
            GridLines = true, BackColor = Color.FromArgb(16, 26, 36), ForeColor = ForeColor };
        surfaces.Columns.Add("Superfície", 185);
        surfaces.Columns.Add("Função sugerida", 250);
        surfaces.Columns.Add("Tipo", 125);
        surfaces.Columns.Add("Handle", 80);
        layout.Controls.Add(surfaces, 0, 4);
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0),
            Text = "Consulta pontual: clique novamente após editar o desenho.\nVolumes: próxima etapa. Drenagem: aguardando planilha e critérios. Conversa: disponível na prévia HTML, sem IA conectada." }, 0, 5);
        // Set explicit colors: the host can replace inherited container colors.
        foreach (Control control in layout.Controls)
        {
            if (control is Label label)
            {
                label.BackColor = Color.FromArgb(24, 34, 45);
                if (label.Font.Size < 14)
                    label.ForeColor = Color.FromArgb(230, 237, 245);
            }
        }
        Controls.Add(layout);
        InvalidateSnapshot("Nenhuma consulta realizada.");
    }

    private void QueueRead()
    {
        var document = AcadApplication.DocumentManager.MdiActiveDocument;
        if (document is null) { InvalidateSnapshot("Abra um desenho no Civil 3D."); return; }
        if (document.CommandInProgress.Length != 0)
        {
            InvalidateSnapshot("Conclua o comando atual e consulte novamente.");
            return;
        }
        InvalidateSnapshot("Consulta solicitada. Aguardando execução do comando no desenho ativo…");
        // Fixed command only: user input never becomes command text. No cancellation or write command.
        document.SendStringToExecute("PROJETASUPERFICIES ", true, false, false);
    }

    public void InvalidateSnapshot(string message)
    {
        surfaces.Items.Clear();
        status.Text = message;
    }

    public void Display(SurfaceSnapshot snapshot)
    {
        surfaces.BeginUpdate();
        try
        {
            surfaces.Items.Clear();
            foreach (var row in snapshot.Rows)
                surfaces.Items.Add(new ListViewItem(new[] { row.Name,
                    SurfaceNaming.Suggestion(row.Name, snapshot.Rows), row.Type, row.Handle }));
        }
        finally { surfaces.EndUpdate(); }
        status.Text = $"Desenho: {snapshot.Drawing}\nConsulta: {snapshot.CapturedAt:HH:mm:ss} · {snapshot.Rows.Count} superfície(s)" +
            (snapshot.Rows.Count == 0 ? "\nNenhuma superfície encontrada neste desenho." : "");
    }
}

