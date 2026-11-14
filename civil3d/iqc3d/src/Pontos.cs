using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(IQC3D.Pontos))]

namespace IQC3D
{
    public class Pontos
    {
        // ------------------------------------------------------------------
        // IQ_CONVERTERPONTOS: pontos/blocos do AutoCAD <-> pontos COGO
        // ------------------------------------------------------------------
        [CommandMethod("IQ_CONVERTERPONTOS", CommandFlags.Modal)]
        public void ConverterPontos() => U.Rodar(ConverterPontos_);
        private void ConverterPontos_()
        {
            var modo = U.PedirOpcao("Converter [ParaCogo/ParaAutocad]", "ParaCogo", "ParaCogo", "ParaAutocad");
            if (modo == null) return;
            if (modo == "ParaCogo") ParaCogo(); else ParaAutocad();
        }

        private void ParaCogo()
        {
            var ids = U.Selecionar("Selecione pontos (POINT) e/ou blocos do AutoCAD:", "POINT", "INSERT");
            if (ids == null) return;
            var descPadrao = U.PedirTexto("Descrição dos pontos (Enter = nome do bloco / PT)", "");
            if (descPadrao == null) return;
            var apagar = U.PedirOpcao("Apagar os objetos originais? [Sim/Nao]", "Nao", "Sim", "Nao");
            if (apagar == null) return;

            int n = 0;
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var cogo = U.Civil.CogoPoints;
                foreach (var id in ids)
                {
                    var ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    Point3d p; string desc = descPadrao;
                    if (ent is DBPoint dp) { p = dp.Position; if (desc == "") desc = "PT"; }
                    else if (ent is BlockReference br)
                    {
                        p = br.Position;
                        if (desc == "")
                        {
                            var btr = (BlockTableRecord)tr.GetObject(br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord, OpenMode.ForRead);
                            desc = btr.Name;
                        }
                    }
                    else continue;
                    cogo.Add(p, desc, true, false, true);
                    n++;
                    if (apagar == "Sim") { ent.UpgradeOpen(); ent.Erase(); }
                }
                tr.Commit();
            }
            U.Msg($"{n} ponto(s) COGO criado(s).");
        }

        private void ParaAutocad()
        {
            var ids = U.Selecionar("Selecione os pontos COGO:", "AECC_COGO_POINT");
            if (ids == null) return;
            int n = 0;
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var lay = U.GarantirLayer(tr, "IQ_PONTOS_ACAD", 7);
                foreach (var id in ids)
                {
                    var cp = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
                    var dp = new DBPoint(cp.Location) { LayerId = lay };
                    U.Adicionar(tr, dp);
                    n++;
                }
                tr.Commit();
            }
            U.Msg($"{n} POINT(s) do AutoCAD criado(s) no layer IQ_PONTOS_ACAD.");
        }

        // ------------------------------------------------------------------
        // IQ_PONTOS3DPOLY: liga pontos COGO numa polilinha 3D
        // ------------------------------------------------------------------
        [CommandMethod("IQ_PONTOS3DPOLY", CommandFlags.Modal)]
        public void Pontos3dPoly() => U.Rodar(Pontos3dPoly_);
        private void Pontos3dPoly_()
        {
            var ids = U.Selecionar("Selecione os pontos COGO:", "AECC_COGO_POINT");
            if (ids == null || ids.Length < 2) { U.Msg("Selecione pelo menos 2 pontos."); return; }
            var ordem = U.PedirOpcao("Ordem de ligação [Numero/Selecao]", "Numero", "Numero", "Selecao");
            if (ordem == null) return;
            var fechar = U.PedirOpcao("Fechar a polilinha? [Sim/Nao]", "Nao", "Sim", "Nao");
            if (fechar == null) return;

            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var pts = ids.Select(i => (CogoPoint)tr.GetObject(i, OpenMode.ForRead)).ToList();
                if (ordem == "Numero") pts = pts.OrderBy(p => p.PointNumber).ToList();
                var col = new Point3dCollection();
                foreach (var p in pts) col.Add(p.Location);
                var pl = new Polyline3d(Poly3dType.SimplePoly, col, fechar == "Sim");
                U.Adicionar(tr, pl);
                tr.Commit();
            }
            U.Msg($"Polilinha 3D criada com {ids.Length} vértices.");
        }

        // ------------------------------------------------------------------
        // IQ_APAGARPROXIMOS: apaga pontos COGO duplicados/próximos
        // ------------------------------------------------------------------
        [CommandMethod("IQ_APAGARPROXIMOS", CommandFlags.Modal)]
        public void ApagarProximos() => U.Rodar(ApagarProximos_);
        private void ApagarProximos_()
        {
            var tol = U.PedirDouble("Distância máxima entre pontos (m)", 0.05, false);
            if (tol == null) return;
            var ids = U.Selecionar("Selecione os pontos COGO (Enter = todos):", "AECC_COGO_POINT");
            if (ids == null) ids = U.Civil.GetAllPointIds().Cast<ObjectId>().ToArray();
            if (ids.Length < 2) { U.Msg("Pontos insuficientes."); return; }

            double t = tol.Value;
            var apagar = new List<ObjectId>();
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var pts = ids.Select(i => (CogoPoint)tr.GetObject(i, OpenMode.ForRead))
                             .OrderBy(p => p.PointNumber)   // mantém o de menor número
                             .ToList();
                var grade = new Dictionary<(long, long), List<Point3d>>();
                foreach (var p in pts)
                {
                    var loc = p.Location;
                    long gx = (long)Math.Floor(loc.X / t), gy = (long)Math.Floor(loc.Y / t);
                    bool dup = false;
                    for (long dx = -1; dx <= 1 && !dup; dx++)
                        for (long dy = -1; dy <= 1 && !dup; dy++)
                            if (grade.TryGetValue((gx + dx, gy + dy), out var lista))
                                dup = lista.Any(q => Math.Sqrt((q.X - loc.X) * (q.X - loc.X) + (q.Y - loc.Y) * (q.Y - loc.Y)) <= t);
                    if (dup) { apagar.Add(p.ObjectId); continue; }
                    if (!grade.TryGetValue((gx, gy), out var l)) grade[(gx, gy)] = l = new List<Point3d>();
                    l.Add(loc);
                }
                if (apagar.Count == 0) { U.Msg("Nenhum ponto próximo encontrado."); return; }
                var conf = U.PedirOpcao($"{apagar.Count} ponto(s) a menos de {U.F(t)} m de outro (fica o de menor número). Apagar? [Sim/Nao]", "Nao", "Sim", "Nao");
                if (conf != "Sim") { U.Msg("Nada foi apagado."); return; }
                foreach (var id in apagar) ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase();
                tr.Commit();
            }
            U.Msg($"{apagar.Count} ponto(s) apagado(s). Use U (desfazer) se precisar.");
        }

        // ------------------------------------------------------------------
        // IQ_CRIARPOINTGROUPS: um point group por descrição
        // ------------------------------------------------------------------
        [CommandMethod("IQ_CRIARPOINTGROUPS", CommandFlags.Modal)]
        public void CriarPointGroups() => U.Rodar(CriarPointGroups_);
        private void CriarPointGroups_()
        {
            var modo = U.PedirOpcao("Agrupar pela descrição [Completa/PrimeiraPalavra]", "PrimeiraPalavra", "Completa", "PrimeiraPalavra");
            if (modo == null) return;
            var prefixo = U.PedirTexto("Prefixo do nome dos grupos (Enter = sem prefixo)", "");
            if (prefixo == null) return;

            int criados = 0, existentes = 0;
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var civ = U.Civil;
                var chaves = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ObjectId id in civ.GetAllPointIds())
                {
                    var cp = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
                    var d = (cp.RawDescription ?? "").Trim();
                    if (d == "") continue;
                    if (modo == "PrimeiraPalavra") d = d.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    if (d.IndexOfAny(new[] { ',', '*', '?' }) >= 0) continue; // caracteres especiais do filtro
                    chaves.Add(d);
                }
                var grupos = civ.PointGroups;
                foreach (var k in chaves)
                {
                    var nome = prefixo + k;
                    if (grupos.Contains(nome)) { existentes++; continue; }
                    var gid = grupos.Add(nome);
                    var g = (PointGroup)tr.GetObject(gid, OpenMode.ForWrite);
                    var q = new StandardPointGroupQuery();
                    q.IncludeRawDescriptions = modo == "PrimeiraPalavra" ? $"{k},{k} *" : k;
                    g.SetQuery(q);
                    criados++;
                }
                grupos.UpdateAllPointGroups();
                tr.Commit();
            }
            U.Msg($"{criados} point group(s) criado(s); {existentes} já existia(m).");
        }

        // ------------------------------------------------------------------
        // IQ_MOVERLABELS: desloca, gira ou reseta rótulos de pontos
        // ------------------------------------------------------------------
        [CommandMethod("IQ_MOVERLABELS", CommandFlags.Modal)]
        public void MoverLabels() => U.Rodar(MoverLabels_);
        private void MoverLabels_()
        {
            var ids = U.Selecionar("Selecione os pontos COGO:", "AECC_COGO_POINT");
            if (ids == null) return;
            var modo = U.PedirOpcao("Rótulos [Deslocar/Girar/Resetar]", "Deslocar", "Deslocar", "Girar", "Resetar");
            if (modo == null) return;
            Vector3d vet = Vector3d.XAxis * 0;
            double ang = 0;
            if (modo == "Deslocar")
            {
                var a = U.Ed.GetPoint(new PromptPointOptions("\nPonto base do deslocamento: "));
                if (a.Status != PromptStatus.OK) return;
                var b = U.Ed.GetPoint(new PromptPointOptions("\nPara onde: ") { BasePoint = a.Value, UseBasePoint = true });
                if (b.Status != PromptStatus.OK) return;
                vet = b.Value - a.Value;
            }
            else if (modo == "Girar")
            {
                var r = U.Ed.GetAngle(new PromptAngleOptions("\nÂngulo dos rótulos: ") { DefaultValue = 0, UseDefaultValue = true });
                if (r.Status != PromptStatus.OK) return;
                ang = r.Value;
            }
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    var cp = (CogoPoint)tr.GetObject(id, OpenMode.ForWrite);
                    if (modo == "Resetar") cp.ResetLabel();
                    else if (modo == "Girar") cp.LabelRotation = ang;
                    else cp.LabelLocation = cp.LabelLocation + vet;
                }
                tr.Commit();
            }
            U.Msg($"{ids.Length} rótulo(s) ajustado(s).");
        }
    }
}
