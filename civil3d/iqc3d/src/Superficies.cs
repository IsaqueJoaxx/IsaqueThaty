using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using CivSurface = Autodesk.Civil.DatabaseServices.Surface;

[assembly: CommandClass(typeof(IQC3D.Superficies))]

namespace IQC3D
{
    public class Superficies
    {
        // ------------------------------------------------------------------
        // IQ_SOLIDOSUPERFICIE: sólido 3D entre a superfície e uma cota/profundidade/outra superfície
        // ------------------------------------------------------------------
        [CommandMethod("IQ_SOLIDOSUPERFICIE", CommandFlags.Modal)]
        public void SolidoSuperficie() => U.Rodar(SolidoSuperficie_);
        private void SolidoSuperficie_()
        {
            var sid = U.EscolherSuperficie("Superfície de topo");
            if (sid.IsNull) return;
            var modo = U.PedirOpcao("Base do sólido [Profundidade/Cota/Superficie]", "Profundidade", "Profundidade", "Cota", "Superficie");
            if (modo == null) return;
            var layer = U.PedirTexto("Layer dos sólidos", "IQ_SOLIDO_SUP", false);
            if (layer == null) return;

            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                U.GarantirLayer(tr, layer, 30);
                var s = tr.GetObject(sid, OpenMode.ForRead) as TinSurface;
                if (s == null) { U.Msg("Só funciona com superfícies TIN."); return; }
                ObjectIdCollection criados;
                if (modo == "Profundidade")
                {
                    var d = U.PedirDouble("Profundidade abaixo da superfície (m)", 1.0, false);
                    if (d == null) return;
                    criados = s.CreateSolidsAtDepth(d.Value, layer, 30);
                }
                else if (modo == "Cota")
                {
                    var c = U.PedirDouble("Cota da base (m)", Math.Floor(s.GetGeneralProperties().MinimumElevation) - 1, true, true);
                    if (c == null) return;
                    criados = s.CreateSolidsAtFixedElevation(c.Value, layer, 30);
                }
                else
                {
                    var bid = U.EscolherSuperficie("Superfície de base");
                    if (bid.IsNull) return;
                    criados = s.CreateSolidsAtSurface(bid, layer, 30);
                }
                tr.Commit();
                U.Msg($"{criados?.Count ?? 0} sólido(s) criado(s) no layer {layer}.");
            }
        }

        // ------------------------------------------------------------------
        // IQ_PERFISSUPERFICIE: perfil de superfície em vários alinhamentos
        // ------------------------------------------------------------------
        [CommandMethod("IQ_PERFISSUPERFICIE", CommandFlags.Modal)]
        public void PerfisSuperficie() => U.Rodar(PerfisSuperficie_);
        private void PerfisSuperficie_()
        {
            var ids = U.Selecionar("Selecione os alinhamentos:", "AECC_ALIGNMENT");
            if (ids == null) return;
            var sid = U.EscolherSuperficie("Superfície");
            if (sid.IsNull) return;
            var offTxt = U.PedirTexto("Offsets adicionais separados por ; (ex.: -5;5) ou Enter para só o eixo", "");
            if (offTxt == null) return;
            var offsets = new[] { 0.0 }.Concat(offTxt.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => double.TryParse(x.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : double.NaN)
                .Where(v => !double.IsNaN(v) && Math.Abs(v) > 1e-9)).ToList();

            int n = 0, falhas = 0;
            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var civ = U.Civil;
                var surf = (CivSurface)tr.GetObject(sid, OpenMode.ForRead);
                ObjectId estilo = ObjectId.Null, rotulos = ObjectId.Null;
                try { estilo = civ.Styles.ProfileStyles[0]; } catch { }
                try { rotulos = civ.Styles.LabelSetStyles.ProfileLabelSetStyles[0]; } catch { }
                foreach (var aid in ids)
                {
                    var al = (Alignment)tr.GetObject(aid, OpenMode.ForRead);
                    foreach (var off in offsets)
                    {
                        var nome = off == 0 ? $"{surf.Name} - {al.Name}" : $"{surf.Name} - {al.Name} ({(off > 0 ? "+" : "")}{U.F(off, 2)})";
                        try
                        {
                            if (off == 0) Profile.CreateFromSurface(nome, aid, sid, al.LayerId, estilo, rotulos);
                            else Profile.CreateFromSurface(nome, aid, sid, al.LayerId, estilo, rotulos, off, al.StartingStation, al.EndingStation);
                            n++;
                        }
                        catch (System.Exception ex) { falhas++; U.Msg($"  {al.Name} offset {off}: {ex.Message}"); }
                    }
                }
                tr.Commit();
            }
            U.Msg($"{n} perfil(is) de superfície criado(s){(falhas > 0 ? $", {falhas} falha(s)" : "")}.");
        }

        // ------------------------------------------------------------------
        // IQ_BREAKLINELAYER: adiciona como breakline tudo o que está num layer
        // ------------------------------------------------------------------
        [CommandMethod("IQ_BREAKLINELAYER", CommandFlags.Modal)]
        public void BreaklineNoLayer() => U.Rodar(BreaklineNoLayer_);
        private void BreaklineNoLayer_()
        {
            var sid = U.EscolherSuperficie("Superfície que recebe as breaklines");
            if (sid.IsNull) return;
            var r = U.Ed.GetEntity(new PromptEntityOptions("\nClique numa linha do layer desejado: "));
            if (r.Status != PromptStatus.OK) return;
            var mo = U.PedirDouble("Distância de meia-ordenada para arcos (m)", 0.1, false);
            if (mo == null) return;

            using (var tr = U.Db.TransactionManager.StartTransaction())
            {
                var layer = ((Entity)tr.GetObject(r.ObjectId, OpenMode.ForRead)).Layer;
                var col = new ObjectIdCollection();
                foreach (ObjectId id in U.ModelSpace(tr, OpenMode.ForRead))
                {
                    var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e == null || !e.Layer.Equals(layer, StringComparison.OrdinalIgnoreCase)) continue;
                    if (e is Polyline || e is Polyline3d || e is Polyline2d || e is Line || e is Arc || e is FeatureLine) col.Add(id);
                }
                if (col.Count == 0) { U.Msg($"Nenhuma linha/polilinha/feature line no layer {layer}."); return; }
                var s = (TinSurface)tr.GetObject(sid, OpenMode.ForWrite);
                s.BreaklinesDefinition.AddStandardBreaklines(col, mo.Value, 0, 0, 0);
                s.Rebuild();
                tr.Commit();
                U.Msg($"{col.Count} objeto(s) do layer {layer} adicionado(s) como breakline em {s.Name}.");
            }
        }
    }
}
