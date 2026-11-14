using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Projeta.Civil3D.Infra;

namespace Projeta.Civil3D.Civil;

/// <summary>Trecho de corredor: em que eixo, de que estaca a que estaca, com que montagem e quais alvos de largura.</summary>
internal sealed record TrechoCorredor(ObjectId Alinhamento, string Eixo, double De, double Ate, string Montagem, string Obs)
{
    /// <summary>Alvos de largura das faixas (polilinhas ou alinhamentos).</summary>
    public List<ObjectId> AlvosLargura { get; init; } = new();
    /// <summary>true = a faixa vai até o alvo mais perto (conexões); false = cada lado usa o alvo do seu lado.</summary>
    public bool AlvoMaisPerto { get; init; }
}

/// <summary>
/// Coloca os trechos de uma rotatória ou interseção num corredor (existente ou novo): baseline por eixo (com o greide de
/// projeto), região por trecho com a montagem, alvos de largura e a superfície do terreno nos taludes. Nas baselines que
/// já existiam, as regiões que passam pelo trecho são encurtadas, divididas ou retiradas (só no trecho).
/// </summary>
internal static class CorredorProjeta
{
    /// <summary>Pergunta onde pôr: [Existente/Novo/Nao]. null = não colocar no corredor.</summary>
    public static ObjectId? Escolher(Sessao s, CivilDocument civ, string nomeNovo)
    {
        var lista = new List<(ObjectId Id, string Nome)>();
        foreach (ObjectId id in civ.CorridorCollection)
            try { lista.Add((id, ((Corridor)s.Tr.GetObject(id, OpenMode.ForRead)).Name)); } catch { }
        var opcoes = lista.Count > 0 ? new[] { "Existente", "Novo", "Nao" } : new[] { "Novo", "Nao" };
        var r = s.PedirPalavra("Colocar no corredor", opcoes, lista.Count > 0 ? "Existente" : "Novo");
        if (r is null || r == "Nao") return null;
        if (r == "Novo")
        {
            string nome = nomeNovo;
            for (int i = 2; lista.Any(c => c.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase)); i++) nome = $"{nomeNovo} ({i})";
            return civ.CorridorCollection.Add(nome);
        }
        int k = lista.Count == 1 ? 0 : s.EscolherDaLista("Corredor", lista.Select(c => c.Nome).ToList(), 0);
        return k < 0 ? null : lista[k].Id;
    }

    /// <summary>Superfície do terreno para os taludes (uma só: usa; várias: pergunta). Null = sem superfície.</summary>
    public static ObjectId Superficie(Sessao s)
    {
        var sups = VolumesCivil.Superficies(s.Tr);
        if (sups.Count == 0) return ObjectId.Null;
        if (sups.Count == 1) return sups[0].Id;
        int k = s.EscolherDaLista("Superfície do terreno para os taludes", sups.Select(x => x.Nome).ToList(), 0);
        return k < 0 ? ObjectId.Null : sups[k].Id;
    }

    /// <param name="greideDe">Greide de projeto de um alinhamento (null = sem greide: o eixo fica fora).</param>
    /// <param name="montagens">Montagem por tipo (nome sem o prefixo).</param>
    /// <param name="recortar">Se as regiões que já existem nas baselines devem ser recortadas no trecho (rotatória: sim).</param>
    public static List<string> Inserir(Transaction tr, ObjectId corredorId, IReadOnlyList<TrechoCorredor> trechos,
        Func<Alignment, ObjectId?> greideDe, IReadOnlyDictionary<string, ObjectId> montagens, ObjectId superficie, string prefixo,
        bool recortar, out int regioes)
    {
        var msgs = new List<string>();
        regioes = 0;
        var corr = (Corridor)tr.GetObject(corredorId, OpenMode.ForWrite);
        foreach (var g in trechos.GroupBy(t => t.Alinhamento))
        {
            var al = (Alignment)tr.GetObject(g.Key, OpenMode.ForRead);
            Baseline? bl = null;
            foreach (Baseline b in corr.Baselines) if (b.AlignmentId == g.Key) { bl = b; break; }
            if (bl is null)
            {
                if (greideDe(al) is not { } pid || pid.IsNull) { msgs.Add($"'{al.Name}' não tem greide de projeto: os trechos dele ficaram fora do corredor."); continue; }
                try { bl = corr.Baselines.Add(Unico(corr.Baselines.Cast<Baseline>().Select(b => b.Name), al.Name), g.Key, pid); }
                catch (System.Exception e) { msgs.Add($"Não consegui pôr '{al.Name}' no corredor: {e.Message}"); continue; }
            }
            else if (recortar)
            {
                double a = g.Min(t => t.De), b = g.Max(t => t.Ate);
                foreach (var r in bl.BaselineRegions.Cast<BaselineRegion>().ToList())
                {
                    double s0 = r.StartStation, s1 = r.EndStation;
                    if (b <= s0 + 1e-3 || a >= s1 - 1e-3) continue;
                    string nr = r.Name;
                    try
                    {
                        if (a <= s0 + 1e-3 && b >= s1 - 1e-3)
                        {
                            bl.BaselineRegions.Remove(r);
                            msgs.Add($"a região '{nr}' de '{bl.Name}' ({RotatoriaCivil.Est(s0)} a {RotatoriaCivil.Est(s1)}) saiu: fica toda no trecho novo.");
                        }
                        else if (a > s0 && b < s1)
                        {
                            r.Split(b);
                            r.EndStation = a;
                            msgs.Add($"a região '{nr}' de '{bl.Name}' foi dividida: {RotatoriaCivil.Est(s0)} a {RotatoriaCivil.Est(a)} e {RotatoriaCivil.Est(b)} a {RotatoriaCivil.Est(s1)}.");
                        }
                        else if (a <= s0)
                        {
                            r.StartStation = b;
                            msgs.Add($"a região '{nr}' de '{bl.Name}' agora começa em {RotatoriaCivil.Est(b)} (antes {RotatoriaCivil.Est(s0)}).");
                        }
                        else
                        {
                            r.EndStation = a;
                            msgs.Add($"a região '{nr}' de '{bl.Name}' agora termina em {RotatoriaCivil.Est(a)} (antes {RotatoriaCivil.Est(s1)}).");
                        }
                    }
                    catch (System.Exception e) { msgs.Add($"não consegui recortar a região '{nr}' de '{bl.Name}': {e.Message}"); }
                }
            }
            foreach (var t in g.OrderBy(t => t.De))
            {
                if (!montagens.TryGetValue(t.Montagem, out var asmId)) { msgs.Add($"sem a montagem '{t.Montagem}' para '{al.Name}' {RotatoriaCivil.Est(t.De)} a {RotatoriaCivil.Est(t.Ate)}."); continue; }
                double de = Math.Max(t.De, al.StartingStation), ate = Math.Min(t.Ate, al.EndingStation);
                if (ate - de < 0.05) continue;
                BaselineRegion reg;
                try { reg = bl.BaselineRegions.Add(Unico(bl.BaselineRegions.Cast<BaselineRegion>().Select(r => r.Name), $"{prefixo} - {t.Montagem}"), asmId, de, ate); }
                catch (System.Exception e) { msgs.Add($"a região {t.Montagem} de '{al.Name}' ({RotatoriaCivil.Est(de)} a {RotatoriaCivil.Est(ate)}) não entrou: {e.Message}"); continue; }
                regioes++;
                // frequência: anel e conexões são curvas curtas, a seção a cada 1 m nas curvas deixa o corredor redondo
                try
                {
                    var ap = reg.AppliedAssemblySetting;
                    ap.FrequencyAlongTangents = 5;
                    ap.FrequencyAlongCurves = 1;
                    ap.FrequencyAlongSpirals = 2;
                    ap.FrequencyAlongTargetCurves = 1;
                    ap.FrequencyAlongProfileCurves = 5;
                }
                catch { }
                string nomeMontagem = "";
                try { nomeMontagem = ((Autodesk.Civil.DatabaseServices.Assembly)tr.GetObject(asmId, OpenMode.ForRead)).Name; } catch { }
                try { Alvos(reg, t, superficie, nomeMontagem); }
                catch (System.Exception e) { msgs.Add($"alvos da região {t.Montagem} de '{al.Name}': {e.Message}"); }
            }
        }
        try { corr.Rebuild(); }
        catch (System.Exception e) { msgs.Add($"o corredor não reconstruiu: {e.Message}"); }
        return msgs;
    }

    private static void Alvos(BaselineRegion reg, TrechoCorredor t, ObjectId superficie, string nomeMontagem)
    {
        var tg = reg.GetTargets();
        bool mudou = false;
        foreach (SubassemblyTargetInfo ti in tg)
        {
            if (ti.TargetType == SubassemblyLogicalNameType.Surface && !superficie.IsNull)
            {
                ti.TargetIds = new ObjectIdCollection(new[] { superficie });
                mudou = true;
            }
            else if (ti.TargetType == SubassemblyLogicalNameType.Offset && t.AlvosLargura.Count > 0 && EhFaixa(ti.SubassemblyName, nomeMontagem))
            {
                ti.TargetIds = new ObjectIdCollection(t.AlvosLargura.ToArray());
                if (t.AlvoMaisPerto) { ti.UseSameSideTarget = false; ti.TargetToOption = SubassemblyTargetToOption.Nearest; }
                else ti.UseSameSideTarget = true;
                mudou = true;
            }
        }
        if (mudou) reg.SetTargets(tg);
    }

    /// <summary>
    /// Se a peça é uma faixa (recebe o alvo de largura): olha só o nome da peça, tirando o da montagem do começo do nome
    /// da subassembly ("{montagem} - {peça} (D)"). O nome da montagem pode ter "faixas" ("Pista só faixas com meio-fio...")
    /// e não pode puxar o alvo para o meio-fio e o passeio.
    /// </summary>
    private static bool EhFaixa(string nomeSubassembly, string nomeMontagem)
    {
        string peca = nomeMontagem.Length > 0 && nomeSubassembly.StartsWith(nomeMontagem + " - ", StringComparison.OrdinalIgnoreCase)
            ? nomeSubassembly[(nomeMontagem.Length + 3)..] : nomeSubassembly;
        return peca.Contains("Faixa", StringComparison.OrdinalIgnoreCase) || peca.Contains("Pista anelar", StringComparison.OrdinalIgnoreCase);
    }

    private static string Unico(IEnumerable<string> usados, string nome)
    {
        var h = new HashSet<string>(usados, StringComparer.OrdinalIgnoreCase);
        string n = nome;
        for (int i = 2; h.Contains(n); i++) n = $"{nome} ({i})";
        return n;
    }
}
