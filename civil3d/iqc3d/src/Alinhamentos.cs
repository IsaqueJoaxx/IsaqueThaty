using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using CivSurface = Autodesk.Civil.DatabaseServices.Surface;

[assembly: CommandClass(typeof(IQC3D.Alinhamentos))]

namespace IQC3D
{
    public class Alinhamentos
    {
        // ------------------------------------------------------------------
        // IQ_MARCAINTERSECOES: marca os cruzamentos entre alinhamentos com a estaca de cada um
        // ------------------------------------------------------------------
        [CommandMethod("IQ_MARCAINTERSECOES", CommandFlags.Modal)]
        public void MarcaIntersecoes() => U.Rodar(MarcaIntersecoes_);
        private void MarcaIntersecoes_()
        {
            var ids = U.Selecionar("Selecione os alinhamentos (2 ou mais):", "AECC_ALIGNMENT");
            if (ids == null || ids.Length < 2) { U.Msg("Selecione pelo menos 2 alinhamentos."); return; }
            var raio = U.PedirDouble("Raio do círculo de marcação (m)", 2.0, false);
            if (raio == null) return;
            var alt = U.PedirDouble("Altura do texto (m)", 1.5, false);
            if (alt == null) return;

            int n = 0;
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var lay = U.GarantirLayer(tr, "IQ_INTERSECOES", 1);
                var als = ids.Select(i => (Alignment)tr.GetObject(i, OpenMode.ForRead)).ToList();
                // polilinhas temporárias para calcular as interseções
                var polis = new List<Polyline>();
                foreach (var a in als)
                {
                    var pid = a.GetPolyline();
                    polis.Add((Polyline)tr.GetObject(pid, OpenMode.ForWrite));
                }
                var vistos = new List<Point3d>();
                for (int i = 0; i < als.Count; i++)
                    for (int j = i + 1; j < als.Count; j++)
                    {
                        var pts = new Point3dCollection();
                        polis[i].IntersectWith(polis[j], Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                        foreach (Point3d p in pts)
                        {
                            if (vistos.Any(v => v.DistanceTo(p) < 0.01)) continue;
                            vistos.Add(p);
                            double si = 0, oi = 0, sj = 0, oj = 0;
                            als[i].StationOffset(p.X, p.Y, ref si, ref oi);
                            als[j].StationOffset(p.X, p.Y, ref sj, ref oj);
                            U.Adicionar(tr, new Circle(new Point3d(p.X, p.Y, 0), Vector3d.ZAxis, raio.Value) { LayerId = lay });
                            var txt = new MText
                            {
                                Location = new Point3d(p.X + raio.Value * 1.2, p.Y + raio.Value * 1.2, 0),
                                TextHeight = alt.Value,
                                Contents = $"{als[i].Name}: est. {U.Estaca(si)}\\P{als[j].Name}: est. {U.Estaca(sj)}",
                                LayerId = lay,
                                Attachment = AttachmentPoint.BottomLeft
                            };
                            U.Adicionar(tr, txt);
                            n++;
                        }
                    }
                foreach (var pl in polis) pl.Erase();
                tr.Commit();
            }
            U.Msg(n == 0 ? "Nenhuma interseção encontrada." : $"{n} interseção(ões) marcada(s) no layer IQ_INTERSECOES.");
        }

        // ------------------------------------------------------------------
        // IQ_EXTRAIRPOLILINHA: gera polilinha a partir de alinhamentos
        // ------------------------------------------------------------------
        [CommandMethod("IQ_EXTRAIRPOLILINHA", CommandFlags.Modal)]
        public void ExtrairPolilinha() => U.Rodar(ExtrairPolilinha_);
        private void ExtrairPolilinha_()
        {
            var ids = U.Selecionar("Selecione os alinhamentos:", "AECC_ALIGNMENT");
            if (ids == null) return;
            var layer = U.PedirTexto("Layer das polilinhas", "IQ_EIXOS_POLI", false);
            if (layer == null) return;
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var lay = U.GarantirLayer(tr, layer, 4);
                foreach (var id in ids)
                {
                    var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
                    var pid = a.GetPolyline();
                    var pl = (Entity)tr.GetObject(pid, OpenMode.ForWrite);
                    pl.LayerId = lay;
                }
                tr.Commit();
            }
            U.Msg($"{ids.Length} polilinha(s) criada(s) no layer {layer}.");
        }

        // ------------------------------------------------------------------
        // IQ_ESTENDERTANGENTES: desenha a poligonal de PIs (prolongamento das tangentes)
        // ------------------------------------------------------------------
        [CommandMethod("IQ_ESTENDERTANGENTES", CommandFlags.Modal)]
        public void EstenderTangentes() => U.Rodar(EstenderTangentes_);
        private void EstenderTangentes_()
        {
            var ids = U.Selecionar("Selecione os alinhamentos:", "AECC_ALIGNMENT");
            if (ids == null) return;
            var marcar = U.PedirOpcao("Marcar e numerar os PIs? [Sim/Nao]", "Sim", "Sim", "Nao");
            if (marcar == null) return;
            double alt = 1.5;
            if (marcar == "Sim") { var a = U.PedirDouble("Altura do texto (m)", 1.5, false); if (a == null) return; alt = a.Value; }

            int totalPI = 0;
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var lay = U.GarantirLayer(tr, "IQ_TANGENTES", 6);
                foreach (var id in ids)
                {
                    var al = (Alignment)tr.GetObject(id, OpenMode.ForRead);
                    // tangentes (subentidades do tipo reta) na ordem do alinhamento
                    var retas = new List<(Point2d a, Point2d b)>();
                    for (int k = 0; k < al.Entities.Count; k++)
                    {
                        var ent = al.Entities.GetEntityByOrder(k);
                        for (int s = 0; s < ent.SubEntityCount; s++)
                        {
                            var sub = ent[s];
                            if (sub.SubEntityType == AlignmentSubEntityType.Line) retas.Add((sub.StartPoint, sub.EndPoint));
                        }
                    }
                    if (retas.Count < 2) { U.Msg($"  {al.Name}: menos de 2 tangentes, ignorado."); continue; }
                    var verts = new List<Point2d> { retas[0].a };
                    for (int k = 0; k < retas.Count - 1; k++)
                    {
                        var r1 = retas[k]; var r2 = retas[k + 1];
                        if (r1.b.GetDistanceTo(r2.a) < 1e-6) { verts.Add(r1.b); continue; } // tangentes coladas
                        var pi = U.IntersecaoRetas(r1.b, r1.b - r1.a, r2.a, r2.b - r2.a);
                        if (pi == null) continue; // paralelas
                        verts.Add(pi.Value);
                    }
                    verts.Add(retas[retas.Count - 1].b);
                    var pl = new Polyline { LayerId = lay };
                    for (int k = 0; k < verts.Count; k++) pl.AddVertexAt(k, verts[k], 0, 0, 0);
                    U.Adicionar(tr, pl);
                    if (marcar == "Sim")
                        for (int k = 1; k < verts.Count - 1; k++)
                        {
                            U.Adicionar(tr, new Circle(new Point3d(verts[k].X, verts[k].Y, 0), Vector3d.ZAxis, alt * 0.4) { LayerId = lay });
                            U.Adicionar(tr, new DBText { Position = new Point3d(verts[k].X + alt * 0.6, verts[k].Y + alt * 0.6, 0), Height = alt, TextString = $"PI-{k}", LayerId = lay });
                            totalPI++;
                        }
                }
                tr.Commit();
            }
            U.Msg($"Poligonal das tangentes criada no layer IQ_TANGENTES{(totalPI > 0 ? $" ({totalPI} PIs)" : "")}.");
        }

        // ------------------------------------------------------------------
        // IQ_NOTASERVICO: nota de serviço em Excel (terreno x projeto por estaca e afastamento)
        // ------------------------------------------------------------------
        [CommandMethod("IQ_NOTASERVICO", CommandFlags.Modal)]
        public void NotaServico() => U.Rodar(NotaServico_);
        private void NotaServico_()
        {
            var aid = U.PegarObjeto<Alignment>("Selecione o alinhamento:", "Isso não é um alinhamento.");
            if (aid.IsNull) return;
            var tid = U.EscolherSuperficie("Superfície do TERRENO");
            if (tid.IsNull) return;

            ObjectId gid = ObjectId.Null;
            using (var tr = U.Db.TransactionManager.StartOpenCloseTransaction())
            {
                var al = (Alignment)tr.GetObject(aid, OpenMode.ForRead);
                var perfis = al.GetProfileIds().Cast<ObjectId>()
                    .Select(i => (Profile)tr.GetObject(i, OpenMode.ForRead))
                    .Where(p => p.ProfileType != ProfileType.EG)
                    .ToList();
                if (perfis.Count == 0) U.Msg("O alinhamento não tem greide (perfil de projeto); o eixo sai só com o terreno.");
                else
                {
                    for (int i = 0; i < perfis.Count; i++) U.Msg($"  {i + 1} - {perfis[i].Name}");
                    var r = U.Ed.GetInteger(new PromptIntegerOptions("\nNúmero do greide de projeto: ") { LowerLimit = 1, UpperLimit = perfis.Count, DefaultValue = 1, UseDefaultValue = true });
                    if (r.Status != PromptStatus.OK) return;
                    gid = perfis[r.Value - 1].ObjectId;
                }
            }
            var usaSup = U.PedirOpcao("Cotas de projeto nos afastamentos vêm de uma superfície de projeto (corredor)? [Sim/Nao]", "Sim", "Sim", "Nao");
            if (usaSup == null) return;
            ObjectId pid = ObjectId.Null;
            if (usaSup == "Sim") { pid = U.EscolherSuperficie("Superfície de PROJETO (ex.: superfície do corredor)"); if (pid.IsNull) return; }

            var intervalo = U.PedirDouble("Intervalo entre seções (m)", 20.0, false);
            if (intervalo == null) return;
            var offTxt = U.PedirTexto("Afastamentos separados por ; (ex.: -7;-3,5;3,5;7)", "-3.5;3.5");
            if (offTxt == null) return;
            var offsets = offTxt.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => double.TryParse(x.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN)
                .Where(v => !double.IsNaN(v) && Math.Abs(v) > 1e-9).Distinct().OrderBy(v => v).ToList();

            var xl = new Xlsx();
            string nomeAl;
            int linhas = 0;
            using (var tr = U.Db.TransactionManager.StartOpenCloseTransaction())
            {
                var al = (Alignment)tr.GetObject(aid, OpenMode.ForRead);
                nomeAl = al.Name;
                var terreno = (CivSurface)tr.GetObject(tid, OpenMode.ForRead);
                var greide = gid.IsNull ? null : (Profile)tr.GetObject(gid, OpenMode.ForRead);
                var proj = pid.IsNull ? null : (CivSurface)tr.GetObject(pid, OpenMode.ForRead);

                var aba = xl.NovaAba(al.Name);
                var cab = new List<object> { "Estaca", "Station (m)", "Este (E)", "Norte (N)", "Eixo: Terreno", "Eixo: Projeto", "Eixo: Corte(-)/Aterro(+)" };
                foreach (var o in offsets)
                {
                    var lado = o < 0 ? "LE" : "LD";
                    var t = $"{lado} {U.F(Math.Abs(o), 2)}";
                    cab.Add($"{t}: Terreno");
                    if (proj != null) { cab.Add($"{t}: Projeto"); cab.Add($"{t}: C(-)/A(+)"); }
                }
                aba.Add(cab.ToArray());

                // estações: intervalo regular + início e fim
                var estacoes = new List<double>();
                double ini = al.StartingStation, fim = al.EndingStation, dI = intervalo.Value;
                for (double s = Math.Ceiling(ini / dI - 1e-9) * dI; s <= fim + 1e-6; s += dI) estacoes.Add(s);
                if (estacoes.Count == 0 || estacoes[0] - ini > 1e-3) estacoes.Insert(0, ini);
                if (fim - estacoes[estacoes.Count - 1] > 1e-3) estacoes.Add(fim);

                double Cota(CivSurface s, double x, double y) { try { return s.FindElevationAtXY(x, y); } catch { return double.NaN; } }

                foreach (var s in estacoes)
                {
                    double e = 0, n = 0;
                    al.PointLocation(s, 0, ref e, ref n);
                    double ct = Cota(terreno, e, n);
                    double cp = double.NaN;
                    if (greide != null && s >= greide.StartingStation - 1e-6 && s <= greide.EndingStation + 1e-6) { try { cp = greide.ElevationAt(s); } catch { } }
                    else if (proj != null) cp = Cota(proj, e, n);
                    var lin = new List<object> { U.Estaca(s), s, e, n, ct, cp, (double.IsNaN(ct) || double.IsNaN(cp)) ? double.NaN : cp - ct };
                    foreach (var o in offsets)
                    {
                        double eo = 0, no = 0;
                        al.PointLocation(s, o, ref eo, ref no);
                        double cto = Cota(terreno, eo, no);
                        lin.Add(cto);
                        if (proj != null)
                        {
                            double cpo = Cota(proj, eo, no);
                            lin.Add(cpo);
                            lin.Add(double.IsNaN(cto) || double.IsNaN(cpo) ? double.NaN : cpo - cto);
                        }
                    }
                    aba.Add(lin.ToArray());
                    linhas++;
                }
            }
            string pasta = null;
            try { if (U.Db.Filename != null && File.Exists(U.Db.Filename) && !U.Db.Filename.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)) pasta = Path.GetDirectoryName(U.Db.Filename); } catch { }
            if (pasta == null) pasta = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var sfd = new Autodesk.AutoCAD.Windows.SaveFileDialog("Salvar nota de serviço", Path.Combine(pasta, $"Nota de servico - {nomeAl}.xlsx"), "xlsx", "IQNotaServico", Autodesk.AutoCAD.Windows.SaveFileDialog.SaveFileDialogFlags.DoNotTransferRemoteFiles);
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) { U.Msg("Cancelado."); return; }
            try
            {
                xl.Salvar(sfd.Filename);
                U.Msg($"Nota de serviço com {linhas} seção(ões) salva em {sfd.Filename}");
            }
            catch (System.Exception ex) { U.Msg("Não consegui salvar (o arquivo está aberto no Excel?): " + ex.Message); }
        }
    }
}
