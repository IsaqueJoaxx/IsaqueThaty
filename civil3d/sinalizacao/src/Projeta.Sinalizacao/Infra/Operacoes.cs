using Autodesk.AutoCAD.DatabaseServices;
using Projeta.Sinalizacao.Civil;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Quantitativos;
using Projeta.Sinalizacao.Core.Vertical;

namespace Projeta.Sinalizacao.Infra;

/// <summary>Operações de alto nível usadas pelos comandos e pela demonstração.</summary>
internal static class Operacoes
{
    /// <summary>Renumera IDs repetidos (objetos copiados com COPY carregam o mesmo ID).</summary>
    public static int CorrigirIdsRepetidos(Sessao s, List<(ObjectId Id, ObjetoSin Obj)> todos)
    {
        int n = 0;
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // garante que os contadores estejam à frente dos IDs existentes
        foreach (var (_, o) in todos)
        {
            var partes = o.Id.Split('-');
            if (partes.Length == 2 && int.TryParse(partes[1], out int k))
            {
                s.Cfg.Contadores.TryGetValue(partes[0], out int atual);
                if (k > atual) s.Cfg.Contadores[partes[0]] = k;
            }
        }
        foreach (var (id, o) in todos.OrderBy(x => x.Id.Handle.Value))
        {
            if (string.IsNullOrWhiteSpace(o.Id) || !vistos.Add(o.Id))
            {
                string antigo = o.Id;
                o.Id = s.Cfg.ProximoId(o.Categoria);
                vistos.Add(o.Id);
                s.Atualizar(id, o);
                s.Avisos.Add($"ID repetido {antigo} renumerado para {o.Id}.");
                n++;
            }
        }
        return n;
    }

    /// <summary>Localização em relação ao eixo escolhido (ou ao mais próximo, se <paramref name="automatico"/>).</summary>
    public static Localizacao Localizar(Transaction tr, ObjetoSin o, List<(ObjectId Id, string Nome)> eixos)
    {
        V2 p0, p1, pm;
        if (o.Caminho is { Count: >= 2 } && o.Categoria is not Categoria.MarcaTransversal)
        {
            var c = o.CaminhoObj!;
            p0 = c.Ponto(0); p1 = c.Ponto(c.Comprimento); pm = c.Ponto(c.Comprimento / 2);
        }
        else if (o.Categoria == Categoria.MarcaTransversal && o.Marca is not null && o.Codigo != "ZPA")
        {
            p0 = new V2(o.Marca.X1, o.Marca.Y1); p1 = new V2(o.Marca.X2, o.Marca.Y2); pm = (p0 + p1) / 2; p0 = p1 = pm;
        }
        else if (o.Caminho is { Count: > 0 })
        {
            double sx = 0, sy = 0; foreach (var v in o.Caminho) { sx += v.X; sy += v.Y; }
            pm = p0 = p1 = new V2(sx / o.Caminho.Count, sy / o.Caminho.Count);
        }
        else pm = p0 = p1 = new V2(o.X, o.Y);

        if (eixos.Count == 0) return new Localizacao("", null, null, null, pm.Y, pm.X);
        (string nome, double? e0, double? e1, double? off) melhor = ("", null, null, null);
        double melhorD = double.MaxValue;
        foreach (var (id, nome) in eixos)
        {
            var m = Eixos.EstacaAfast(tr, id, pm.X, pm.Y);
            if (m is null) continue;
            if (Math.Abs(m.Value.Afast) < melhorD)
            {
                melhorD = Math.Abs(m.Value.Afast);
                var a = Eixos.EstacaAfast(tr, id, p0.X, p0.Y);
                var b = Eixos.EstacaAfast(tr, id, p1.X, p1.Y);
                double? ea = a?.Estaca ?? m.Value.Estaca, eb = b?.Estaca ?? m.Value.Estaca;
                if (ea > eb) (ea, eb) = (eb, ea);
                bool linear = o.Caminho is { Count: >= 2 } && o.Categoria is not Categoria.MarcaTransversal;
                melhor = (nome, linear ? ea : m.Value.Estaca, linear ? eb : null, m.Value.Afast);
            }
        }
        return new Localizacao(melhor.nome, melhor.e0, melhor.e1, melhor.off, pm.Y, pm.X);
    }

    public static string ExportarQuantitativos(Sessao s, List<(ObjectId Id, ObjetoSin Obj)> objs, List<(ObjectId Id, string Nome)> eixos, string arquivo, string titulo)
    {
        var itens = objs.Select(x => new ItemQuant(x.Obj, Localizar(s.Tr, x.Obj, eixos))).ToList();
        var abas = PlanilhaQuantitativos.Montar(itens, titulo, s.Cfg.PassoEstaca, s.Normas, s.Cfg.Via);
        Xlsx.Salvar(arquivo, abas);
        return arquivo;
    }

    /// <summary>Insere as placas de um cadastro (CSV/XLSX).</summary>
    public static int ImportarCadastro(Sessao s, List<LinhaCadastro> linhas)
    {
        int n = 0;
        foreach (var l in linhas)
        {
            var o = new ObjetoSin
            {
                Categoria = Categoria.Placa, Codigo = l.Codigo, X = l.X, Y = l.Y, Situacao = l.Situacao,
                Id = l.Id ?? "",
                Placa = new DadosPlaca
                {
                    Placas = { new PlacaItem { Codigo = Catalogo.Obter(l.Codigo)?.Codigo ?? l.Codigo, Dimensao = l.Dimensao ?? 0, Valor = l.Valor } },
                    Suporte = l.Suporte, Chapa = s.Cfg.Chapa, Pelicula = s.Cfg.Pelicula, MaterialSuporte = s.Cfg.MaterialSuporte,
                    Azimute = Num.Rad(l.Rotacao ?? 90),
                },
            };
            s.Criar(o);
            n++;
        }
        return n;
    }
}
