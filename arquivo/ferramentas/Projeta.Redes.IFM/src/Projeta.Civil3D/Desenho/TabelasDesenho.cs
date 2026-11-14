using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Projeta.Civil3D.Infra;
using Projeta.Core.Relatorios;

namespace Projeta.Civil3D.Desenho;

/// <summary>Insere tabelas do AutoCAD (Table) a partir de <see cref="TabelaDados"/>.</summary>
internal static class TabelasDesenho
{
    public static ObjectId Inserir(Sessao s, TabelaDados dados, Point3d pos, double alturaTexto, string layer, int maxColunas = 40)
    {
        var tr = s.Tr; var db = s.Db;
        Ctx.GarantirLayer(tr, db, layer, 7);
        int nc = Math.Min(dados.Colunas.Count, maxColunas);
        int nl = dados.Linhas.Count + 2;
        var tb = new Table();
        tb.SetDatabaseDefaults();
        tb.TableStyle = db.Tablestyle;
        tb.Position = pos;
        tb.SetSize(nl, nc);
        tb.Layer = layer;

        // título
        tb.MergeCells(CellRange.Create(tb, 0, 0, 0, nc - 1));
        tb.Cells[0, 0].TextString = dados.Titulo;
        tb.Cells[0, 0].TextHeight = alturaTexto * 1.4;
        tb.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;

        for (int c = 0; c < nc; c++)
        {
            tb.Cells[1, c].TextString = dados.Colunas[c];
            tb.Cells[1, c].TextHeight = alturaTexto;
            tb.Cells[1, c].Alignment = CellAlignment.MiddleCenter;
        }
        for (int l = 0; l < dados.Linhas.Count; l++)
        {
            var linha = dados.Linhas[l];
            for (int c = 0; c < nc; c++)
            {
                var cel = tb.Cells[l + 2, c];
                cel.TextString = c < linha.Length ? linha[c] : "";
                cel.TextHeight = alturaTexto;
                cel.Alignment = c == 0 ? CellAlignment.MiddleLeft : CellAlignment.MiddleRight;
            }
        }
        // larguras proporcionais ao texto mais longo de cada coluna
        for (int c = 0; c < nc; c++)
        {
            int maior = Math.Max(dados.Colunas[c].Length, dados.Linhas.Select(x => c < x.Length ? x[c].Length : 0).DefaultIfEmpty(0).Max());
            tb.Columns[c].Width = Math.Clamp(maior, 4, 60) * alturaTexto * 0.9 + alturaTexto * 1.5;
        }
        for (int r = 0; r < nl; r++) tb.Rows[r].Height = alturaTexto * (r == 0 ? 2.6 : 2.0);
        tb.GenerateLayout();
        var ms = Ctx.EspacoModelo(tr, db);
        return Ctx.Adicionar(tr, ms, tb);
    }
}
