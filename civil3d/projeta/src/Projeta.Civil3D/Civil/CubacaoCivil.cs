using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Core.Terraplenagem;
using Section = Autodesk.Civil.DatabaseServices.Section;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace Projeta.Civil3D.Civil;

/// <summary>
/// Volumes por seções a partir dos grupos de linhas de amostragem (sample lines) do Civil 3D: ou lendo a lista de
/// materiais do próprio Civil (áreas e volumes dele), ou calculando as áreas entre duas fontes amostradas (terreno e
/// projeto) e os volumes pelas áreas médias. Monta a nota de cubação com compensação lateral e Brückner.
/// </summary>
internal static class CubacaoCivil
{
    public sealed record Grupo(ObjectId Id, string Nome, ObjectId Alinhamento, string NomeAlinhamento, int Secoes)
    {
        public string Rotulo => $"{NomeAlinhamento} - {Nome} ({Secoes} seções)";
    }

    /// <summary>Fonte de seção: superfície TIN, superfície em grade ou superfície de corredor.</summary>
    public sealed record Fonte(ObjectId Id, string Nome, SectionSourceType Tipo, bool Amostrada)
    {
        public string Rotulo => Nome + (Tipo == SectionSourceType.CorridorSurface ? " (superfície do corredor)" : "") + (Amostrada ? "" : " (não amostrada: amostrar)");
    }

    public sealed record Lista(Guid Guid, string Nome, string Materiais, bool CorrecaoCurva);

    public sealed class Entrada
    {
        public ObjectId Grupo { get; init; }
        /// <summary>Lista de materiais do Civil (volumes dele); null = calcular entre Terreno e Projeto.</summary>
        public Guid? Lista { get; init; }
        public ObjectId Terreno { get; init; }
        public ObjectId Projeto { get; init; }
        public double FatorCorte { get; init; } = 1;
        public double FatorAterro { get; init; } = 1.25;
        public double EstacaInicial { get; init; } = double.NaN;
        public double EstacaFinal { get; init; } = double.NaN;
        /// <summary>No cálculo por superfícies, compara as áreas com a primeira lista de materiais do grupo, se houver.</summary>
        public bool Conferir { get; init; } = true;
    }

    public static List<Grupo> Grupos(Transaction tr)
    {
        var r = new List<Grupo>();
        foreach (ObjectId aid in CivilApplication.ActiveDocument.GetAlignmentIds())
        {
            if (tr.GetObject(aid, OpenMode.ForRead) is not Alignment al) continue;
            foreach (ObjectId gid in al.GetSampleLineGroupIds())
                if (tr.GetObject(gid, OpenMode.ForRead) is SampleLineGroup g)
                    r.Add(new Grupo(gid, g.Name, aid, al.Name, g.GetSampleLineIds().Count));
        }
        return r;
    }

    private static bool EhSuperficie(SectionSourceType t) => t is SectionSourceType.TinSurface or SectionSourceType.GridSurface or SectionSourceType.CorridorSurface;

    /// <summary>
    /// Identidade da fonte de uma seção. Na superfície de corredor o SourceId é o do corredor (o mesmo da seção do próprio
    /// corredor e das outras superfícies dele: topo, datum...); o que distingue é a superfície do corredor.
    /// </summary>
    private static ObjectId Chave(Section sec)
    {
        if (sec.SourceType == SectionSourceType.CorridorSurface)
            try { return sec.CorridorSurfaceId; } catch (System.InvalidOperationException) { }
        return sec.SourceId;
    }

    /// <summary>Fontes amostradas no grupo (lidas das seções das primeiras linhas) e, depois, as superfícies do desenho que ainda não são.</summary>
    public static List<Fonte> Fontes(Transaction tr, ObjectId grupo)
    {
        var g = (SampleLineGroup)tr.GetObject(grupo, OpenMode.ForRead);
        var r = new List<Fonte>();
        var vistas = new HashSet<ObjectId>(); // fontes e superfícies de corredor já amostradas
        int k = 0;
        foreach (ObjectId slid in g.GetSampleLineIds())
        {
            if (k++ >= 6) break;
            var sl = (SampleLine)tr.GetObject(slid, OpenMode.ForRead);
            foreach (ObjectId secId in sl.GetSectionIds())
            {
                if (tr.GetObject(secId, OpenMode.ForRead) is not Section sec || !EhSuperficie(sec.SourceType)) continue;
                var chave = Chave(sec);
                if (!vistas.Add(chave)) continue;
                string nome;
                try { nome = sec.SourceName; } catch { nome = sec.Name; }
                r.Add(new Fonte(chave, nome, sec.SourceType, true));
            }
        }
        foreach (var (id, nome) in VolumesCivil.Superficies(tr))
        {
            if (vistas.Contains(id)) continue;
            var o = tr.GetObject(id, OpenMode.ForRead);
            if (o is TinSurface && !o.GetType().Name.Contains("Volume", StringComparison.OrdinalIgnoreCase))
                r.Add(new Fonte(id, nome, SectionSourceType.TinSurface, false));
        }
        return r;
    }

    public static List<Lista> Listas(Transaction tr, ObjectId grupo)
    {
        var g = (SampleLineGroup)tr.GetObject(grupo, OpenMode.ForRead);
        var r = new List<Lista>();
        try
        {
            foreach (QTOMaterialList ml in g.MaterialLists)
            {
                var mats = new List<string>();
                foreach (QTOMaterial m in ml)
                {
                    string tipo = m.QuantityType switch { MaterialQuantityType.Cut => "corte", MaterialQuantityType.Fill => "aterro", _ => m.QuantityType.ToString() };
                    mats.Add($"{m.Name} ({tipo})");
                }
                r.Add(new Lista(ml.Guid, ml.Name, string.Join(", ", mats), ml.IsCurveCorrectionEnabled));
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception) { }
        return r;
    }

    /// <summary>Passa a amostrar a superfície no grupo (o Civil cria as seções dela). Altera o desenho; U desfaz.</summary>
    public static bool Amostrar(Transaction tr, ObjectId grupo, ObjectId superficie)
    {
        var g = (SampleLineGroup)tr.GetObject(grupo, OpenMode.ForWrite);
        foreach (SectionSource src in g.GetSectionSources())
            if (src.SourceId == superficie)
            {
                if (!src.IsSampled) src.IsSampled = true;
                return true;
            }
        return false;
    }

    private static List<(ObjectId Id, string Nome, double Estaca)> Linhas(Transaction tr, SampleLineGroup g, Entrada e)
    {
        var r = new List<(ObjectId, string, double)>();
        foreach (ObjectId id in g.GetSampleLineIds())
        {
            var sl = (SampleLine)tr.GetObject(id, OpenMode.ForRead);
            double st = sl.Station;
            if (!double.IsNaN(e.EstacaInicial) && st < e.EstacaInicial - 1e-6) continue;
            if (!double.IsNaN(e.EstacaFinal) && st > e.EstacaFinal + 1e-6) continue;
            r.Add((id, sl.Name, st));
        }
        return r.OrderBy(x => x.Item3).ToList();
    }

    public static Cubacao Calcular(Transaction tr, Entrada e)
    {
        var g = (SampleLineGroup)tr.GetObject(e.Grupo, OpenMode.ForRead);
        var al = (Alignment)tr.GetObject(g.ParentAlignmentId, OpenMode.ForRead);
        var sls = Linhas(tr, g, e);
        if (sls.Count < 2) throw new InvalidOperationException("O trecho escolhido tem menos de duas seções.");
        var listas = Listas(tr, e.Grupo);
        Cubacao c;
        if (e.Lista is Guid guid)
        {
            var lista = listas.FirstOrDefault(l => l.Guid == guid) ?? throw new InvalidOperationException("Lista de materiais não encontrada no grupo.");
            c = DaLista(g, lista, sls);
        }
        else
        {
            var (t, nt) = Secoes(tr, sls, e.Terreno);
            var (p, np) = Secoes(tr, sls, e.Projeto);
            if (t.Values.All(x => x is null)) throw new InvalidOperationException($"O grupo não tem seções de '{nt}'. Amostre essa superfície no grupo de seções.");
            if (p.Values.All(x => x is null)) throw new InvalidOperationException($"O grupo não tem seções de '{np}'. Amostre essa superfície no grupo de seções.");
            var linhas = new List<(string, double, double, double, string)>();
            int semT = 0, semP = 0;
            foreach (var (id, nome, st) in sls)
            {
                var lt = t[id]; var lp = p[id];
                if (lt is null) { semT++; linhas.Add((nome, st, 0, 0, "sem seção do terreno")); continue; }
                if (lp is null) { semP++; linhas.Add((nome, st, 0, 0, "sem projeto nesta estaca")); continue; }
                var a = Projeta.Core.Terraplenagem.Secoes.Areas(lt, lp);
                linhas.Add((nome, st, a.Corte, a.Aterro, double.IsNaN(a.Inicio) ? "terreno e projeto não se sobrepõem" : ""));
            }
            c = Cubacao.PorAreas(linhas, e.FatorCorte, e.FatorAterro);
            c.Terreno = nt; c.Projeto = np;
            c.Origem = $"superfícies amostradas: terreno '{nt}' × projeto '{np}'";
            if (semT > 0) c.Avisos.Add($"{semT} estaca(s) sem seção do terreno (área zero).");
            if (semP > 0) c.Avisos.Add($"{semP} estaca(s) sem seção de projeto (área zero) — fora das regiões do corredor ou da superfície.");
            if (e.Conferir && listas.Count > 0)
            {
                try
                {
                    var res = g.GetTotalVolumeResultDataForMaterialList(listas[0].Guid).GetResultsAlongSampleLines();
                    foreach (var r in res)
                        if (sls.Any(x => Math.Abs(x.Estaca - r.Station) < 0.01))
                            c.Conferencia.Add((r.Station, r.AreaResult.CutArea, r.AreaResult.FillArea));
                    c.ConferenciaNome = listas[0].Nome;
                }
                catch (System.Exception ex) { c.Avisos.Add($"Conferência com a lista {listas[0].Nome} não feita ({ex.Message})."); }
            }
        }
        c.Alinhamento = al.Name;
        c.Grupo = g.Name;
        return c;
    }

    /// <summary>Linhas de seção de uma fonte em cada sample line (null onde não há seção válida), recortadas na faixa válida.</summary>
    private static (Dictionary<ObjectId, LinhaSecao?> Linhas, string Nome) Secoes(Transaction tr, List<(ObjectId Id, string Nome, double Estaca)> sls, ObjectId fonte)
    {
        var r = new Dictionary<ObjectId, LinhaSecao?>();
        string nome = "";
        foreach (var (id, _, _) in sls)
        {
            var sl = (SampleLine)tr.GetObject(id, OpenMode.ForRead);
            LinhaSecao? ln = null;
            foreach (ObjectId secId in sl.GetSectionIds())
            {
                if (tr.GetObject(secId, OpenMode.ForRead) is not Section sec || !EhSuperficie(sec.SourceType) || Chave(sec) != fonte) continue;
                if (nome.Length == 0) { try { nome = sec.SourceName; } catch { nome = sec.Name; } }
                double ini = double.NegativeInfinity, fim = double.PositiveInfinity;
                try { ini = sec.LeftOffset; fim = sec.RightOffset; } catch (System.InvalidOperationException) { }
                // o Civil diz que ponto se liga a qual (SegmentTo); pontos soltos ficam fora da superfície (contorno, buracos)
                var sp = sec.SectionPoints;
                var pts = new List<(double X, double Z, int Para)>();
                foreach (SectionPoint p in sp) pts.Add((p.Location.X, p.Location.Y, p.SegmentTo));
                var trechos = new List<(double, double, double, double)>();
                for (int i = 0; i < pts.Count; i++)
                {
                    int j = pts[i].Para;
                    if (j >= 0 && j < pts.Count && j != i) trechos.Add((pts[i].X, pts[i].Z, pts[j].X, pts[j].Z));
                }
                // nenhum trecho ligado = a superfície não existe nesta estaca (vão, fora da região do corredor)
                try { ln = trechos.Count > 0 ? LinhaSecao.DeTrechos(trechos, ini, fim) : null; }
                catch (ArgumentException) { ln = null; }
                break;
            }
            r[id] = ln;
        }
        if (nome.Length == 0 && tr.GetObject(fonte, OpenMode.ForRead) is Surface s) nome = s.Name;
        return (r, nome);
    }

    /// <summary>Nota com as áreas e volumes da lista de materiais do Civil 3D (já com os fatores e a correção de curva dela).</summary>
    private static Cubacao DaLista(SampleLineGroup g, Lista lista, List<(ObjectId Id, string Nome, double Estaca)> sls)
    {
        QTOSectionalResult[] res;
        try { res = g.GetTotalVolumeResultDataForMaterialList(lista.Guid).GetResultsAlongSampleLines(); }
        catch (System.Exception ex) { throw new InvalidOperationException($"O Civil não devolveu os volumes da lista {lista.Nome} ({ex.Message}). Calcule os materiais no Civil e tente de novo."); }
        var dentro = res.Where(r => sls.Any(x => Math.Abs(x.Estaca - r.Station) < 0.01)).OrderBy(r => r.Station).ToList();
        if (dentro.Count < 2) throw new InvalidOperationException($"A lista {lista.Nome} não tem resultados nas seções escolhidas. Calcule os materiais no Civil.");
        // fator de cada tipo = área com fator ÷ área (o Civil não expõe o fator do total da lista)
        static (double F, bool Uniforme) Fator(IEnumerable<(double A, double Af)> v)
        {
            var l = v.Where(x => x.A > 1e-6).ToList();
            if (l.Count == 0) return (1, true);
            double f = l.Sum(x => x.Af) / l.Sum(x => x.A);
            bool u = l.All(x => Math.Abs(x.Af / x.A - f) <= 1e-3 * f);
            return (f, u);
        }
        var (fc, uc) = Fator(dentro.Select(r => (r.AreaResult.CutArea, r.AreaResult.FactoredCutArea)));
        var (fa, ua) = Fator(dentro.Select(r => (r.AreaResult.FillArea, r.AreaResult.FactoredFillArea)));
        var c = new Cubacao
        {
            VolumesDoCivil = true, CorrecaoCurva = lista.CorrecaoCurva,
            FatorCorte = Math.Round(fc, 4), FatorAterro = Math.Round(fa, 4),
            Origem = $"lista de materiais '{lista.Nome}' do Civil 3D: {lista.Materiais}",
        };
        for (int i = 0; i < dentro.Count; i++)
        {
            var r = dentro[i];
            double vc = i == 0 ? 0 : r.VolumeResult.IncrementalCutVolume, va = i == 0 ? 0 : r.VolumeResult.IncrementalFillVolume;
            c.Linhas.Add(new LinhaCubacao
            {
                Nome = r.SampleLineName, Estaca = r.Station, AreaCorte = r.AreaResult.CutArea, AreaAterro = r.AreaResult.FillArea,
                Distancia = i == 0 ? 0 : r.Station - dentro[i - 1].Station,
                VolumeCorte = vc / fc, VolumeAterro = va / fa, CorteFator = vc, AterroFator = va,
            });
        }
        c.Acumular();
        if (!uc) c.Avisos.Add("Os materiais de corte da lista têm fatores diferentes; o volume de corte sem fator é aproximado (volume do Civil ÷ fator médio).");
        if (!ua) c.Avisos.Add("Os materiais de aterro da lista têm fatores diferentes; o volume de aterro sem fator é aproximado (volume do Civil ÷ fator médio).");
        if (dentro[0].VolumeResult.IncrementalCutVolume + dentro[0].VolumeResult.IncrementalFillVolume > 1e-6)
            c.Avisos.Add($"O volume do trecho antes da estaca inicial ({NotaServicoFormat(dentro[0].Station)}) ficou de fora.");
        c.Avisos.Add("Áreas e volumes como estão na lista de materiais do Civil; se o projeto mudou depois do último cálculo de materiais, recalcule no Civil.");
        return c;
    }

    private static string NotaServicoFormat(double m) => Projeta.Core.Pavimentacao.NotaServico.FormatarEstaca(m, 20, 2);
}
