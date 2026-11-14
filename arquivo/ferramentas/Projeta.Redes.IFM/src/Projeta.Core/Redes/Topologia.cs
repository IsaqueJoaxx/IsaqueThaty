using Projeta.Core.Comum;

namespace Projeta.Core.Redes;

/// <summary>Análise topológica de redes de gravidade (árvores dirigidas montante → jusante).</summary>
public sealed class Topologia
{
    public Rede Rede { get; }
    public List<Trecho> OrdemMontanteJusante { get; } = new();
    public List<No> Exutorios { get; } = new();
    public List<Mensagem> Mensagens { get; } = new();
    public bool TemCiclo { get; private set; }

    private readonly Dictionary<string, List<Trecho>> _chegam = new();
    private readonly Dictionary<string, List<Trecho>> _saem = new();

    public Topologia(Rede rede)
    {
        Rede = rede;
        foreach (var n in rede.Nos) { _chegam[n.Id] = new(); _saem[n.Id] = new(); }
        foreach (var t in rede.Trechos)
        {
            if (!_saem.ContainsKey(t.NoMontanteId) || !_chegam.ContainsKey(t.NoJusanteId))
            {
                Mensagens.Add(new(Gravidade.Erro, "Trecho ligado a nó inexistente", t.Nome));
                continue;
            }
            if (t.NoMontanteId == t.NoJusanteId)
            {
                Mensagens.Add(new(Gravidade.Erro, "Trecho com montante igual à jusante", t.Nome));
                continue;
            }
            _saem[t.NoMontanteId].Add(t);
            _chegam[t.NoJusanteId].Add(t);
        }
        foreach (var n in rede.Nos)
        {
            if (_saem[n.Id].Count > 1 && rede.Tipo != TipoRede.Agua)
                Mensagens.Add(new(Gravidade.Erro, $"Nó com {_saem[n.Id].Count} trechos de saída (divisores de vazão não são suportados em redes de gravidade)", n.Nome));
            if (_saem[n.Id].Count == 0 && _chegam[n.Id].Count > 0) Exutorios.Add(n);
            if (_saem[n.Id].Count == 0 && _chegam[n.Id].Count == 0 && rede.Tipo != TipoRede.Agua)
                Mensagens.Add(new(Gravidade.Aviso, "Nó isolado (sem trechos)", n.Nome));
        }
        OrdenarKahn();
    }

    public IReadOnlyList<Trecho> Chegam(string noId) => _chegam.TryGetValue(noId, out var l) ? l : Array.Empty<Trecho>();
    public IReadOnlyList<Trecho> Saem(string noId) => _saem.TryGetValue(noId, out var l) ? l : Array.Empty<Trecho>();

    private void OrdenarKahn()
    {
        var grau = Rede.Nos.ToDictionary(n => n.Id, n => _chegam[n.Id].Count);
        var fila = new Queue<string>(Rede.Nos.Where(n => grau[n.Id] == 0).Select(n => n.Id));
        var visitados = new HashSet<string>();
        while (fila.Count > 0)
        {
            var id = fila.Dequeue();
            visitados.Add(id);
            foreach (var t in _saem[id])
            {
                OrdemMontanteJusante.Add(t);
                if (--grau[t.NoJusanteId] == 0) fila.Enqueue(t.NoJusanteId);
            }
        }
        if (visitados.Count < Rede.Nos.Count)
        {
            TemCiclo = true;
            var nomes = Rede.Nos.Where(n => !visitados.Contains(n.Id)).Select(n => n.Nome).Take(6);
            Mensagens.Add(new(Gravidade.Erro, "A rede possui ciclo (laço) entre os nós: " + string.Join(", ", nomes)));
        }
    }

    /// <summary>Comprimento do maior caminho de montante até cada nó (m).</summary>
    public Dictionary<string, double> ComprimentoMontante()
    {
        var res = Rede.Nos.ToDictionary(n => n.Id, _ => 0.0);
        foreach (var t in OrdemMontanteJusante)
        {
            double l = res[t.NoMontanteId] + Rede.ComprimentoHorizontal(t);
            if (l > res[t.NoJusanteId]) res[t.NoJusanteId] = l;
        }
        return res;
    }

    /// <summary>
    /// Ordem de numeração "por coletor": primeiro o coletor principal (maior extensão a montante),
    /// da cabeceira até o exutório; depois cada ramal, da cabeceira até o encontro.
    /// </summary>
    public List<List<No>> Coletores()
    {
        var compMont = ComprimentoMontante();
        var coletores = new List<List<No>>();
        var jaNumerado = new HashSet<string>();
        // exutórios ordenados pela maior extensão a montante
        var pilhaFins = new List<No>(Exutorios.OrderByDescending(e => compMont[e.Id]));
        // inclui nós isolados ao final
        foreach (var n in Rede.Nos.Where(n => Saem(n.Id).Count == 0 && Chegam(n.Id).Count == 0)) pilhaFins.Add(n);

        var fila = new LinkedList<No>(pilhaFins);
        while (fila.Count > 0)
        {
            var fim = fila.First!.Value; fila.RemoveFirst();
            // sobe pelo ramo de maior extensão até a cabeceira
            var caminho = new List<No> { fim };
            var atual = fim;
            while (true)
            {
                var entram = Chegam(atual.Id).Where(t => !jaNumerado.Contains(t.NoMontanteId)).ToList();
                if (entram.Count == 0) break;
                var principal = entram.OrderByDescending(t => compMont[t.NoMontanteId] + Rede.ComprimentoHorizontal(t)).First();
                atual = Rede.NoPorId(principal.NoMontanteId)!;
                caminho.Add(atual);
            }
            caminho.Reverse(); // cabeceira -> fim
            var coletor = caminho.Where(n => !jaNumerado.Contains(n.Id)).ToList();
            foreach (var n in coletor) jaNumerado.Add(n.Id);
            if (coletor.Count > 0) coletores.Add(coletor);
            // ramais que chegam em nós deste coletor (de jusante para montante, para manter ordem)
            foreach (var n in caminho)
                foreach (var t in Chegam(n.Id).Where(t => !jaNumerado.Contains(t.NoMontanteId))
                                              .OrderByDescending(t => compMont[t.NoMontanteId]))
                    fila.AddLast(Rede.NoPorId(t.NoMontanteId)!);
        }
        return coletores;
    }
}

public static class Numeracao
{
    /// <summary>Renomeia nós (por coletor, de montante para jusante) e trechos (pelo nó de montante).</summary>
    public static void Numerar(Rede rede, int inicioNos = 1, int inicioTrechos = 1, bool nomearTrechoPelosNos = false)
    {
        var top = new Topologia(rede);
        string Fmt(int i) => i.ToString(new string('0', Math.Max(1, rede.Digitos)));
        int kPv = inicioNos, kBl = inicioNos;
        var ordemNos = new List<No>();
        foreach (var col in top.Coletores())
            foreach (var n in col)
            {
                ordemNos.Add(n);
                n.Nome = n.Tipo == TipoNo.BocaDeLobo ? rede.PrefixoBocaLobo + Fmt(kBl++) : rede.PrefixoNo + Fmt(kPv++);
            }
        int kT = inicioTrechos;
        var posicao = ordemNos.Select((n, i) => (n.Id, i)).ToDictionary(x => x.Id, x => x.i);
        foreach (var t in rede.Trechos.OrderBy(t => posicao.TryGetValue(t.NoMontanteId, out var p) ? p : int.MaxValue))
        {
            if (nomearTrechoPelosNos)
                t.Nome = $"{rede.NoPorId(t.NoMontanteId)?.Nome}-{rede.NoPorId(t.NoJusanteId)?.Nome}";
            else
                t.Nome = rede.PrefixoTrecho + Fmt(kT++);
        }
    }
}
