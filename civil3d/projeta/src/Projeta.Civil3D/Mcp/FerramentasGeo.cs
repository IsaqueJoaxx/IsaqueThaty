using System.Text.Json.Nodes;
using Autodesk.AutoCAD.DatabaseServices;
using Projeta.Civil3D.Civil;
using Projeta.Civil3D.Infra;
using Projeta.Core.Geoespacial;
using Projeta.Mcp;

namespace Projeta.Civil3D.Mcp;

/// <summary>Ferramentas de georreferência: sistema de coordenadas do desenho, conversão de pontos e relevo de satélite.</summary>
internal static class FerramentasGeo
{
    public static void Registrar(RegistroFerramentas r)
    {
        r.Adicionar(new FerramentaDelegada("civil_coordenadas", "Coordenadas (SIRGAS 2000 / UTM)",
            "Mostra o sistema de coordenadas do desenho (das configurações do Civil 3D ou da localização geográfica) e converte pontos entre " +
            "latitude/longitude e UTM (SIRGAS 2000 ≈ WGS 84). Cada ponto é um texto: '-19.9208, -43.9378' (lat, long como no Google Maps), " +
            "'19°55'15\"S 43°56'16\"W' ou '611172.06 7796931.78' (E N em UTM). Sem 'fuso', usa o sistema do desenho; se o desenho não tiver, " +
            "o fuso sai da longitude do primeiro ponto geográfico. Só leitura.",
            () => new Esquema()
                .ListaTextos("pontos", "Pontos a converter.", maxItens: 500)
                .Numero("fuso", "Fuso UTM (1 a 60) para forçar o sistema (SIRGAS 2000).", minimo: 1, maximo: 60)
                .Booleano("norte", "Hemisfério norte (com 'fuso').", false),
            Coordenadas, somenteLeitura: true, limite: TimeSpan.FromMinutes(1),
            esquemaSaida: () => new Esquema().Aberto()
                .Texto("sistema_desenho", "Sistema do desenho, se definido.", anulavel: true)
                .ListaObjetos("pontos", "lat, lon, e, n, fuso, fator_escala, convergencia_graus, gms.", new Esquema().Aberto())));

        r.Adicionar(new FerramentaDelegada("civil_relevo_satelite", "Relevo de satélite (ANADEM / Copernicus 30 m)",
            "Cria uma superfície TIN do Civil 3D com relevo gratuito de ~30 m da área pedida, convertido para o UTM do desenho e recortado no contorno. " +
            "Modelos: 'anadem' (padrão na América do Sul) = ANADEM v1.0 da ANA/UFRGS, modelo de TERRENO com a vegetação removida; " +
            "'copernicus' = Copernicus DEM GLO-30, mundial, modelo de SUPERFÍCIE (inclui vegetação e prédios). A área é uma polilinha fechada (handle) " +
            "ou um retângulo [x_min, y_min, x_max, y_max] em coordenadas do desenho (UTM). O desenho precisa estar em UTM (PRJGEORREF). Baixa só os " +
            "blocos da área (cache local). Altitudes derivadas do geoide EGM2008, erro vertical de alguns metros: serve para estudo, traçado e " +
            "anteprojeto, não substitui levantamento. Altera o DWG (desfaz com U).",
            () => new Esquema()
                .Texto("modelo", "'anadem' (terreno, América do Sul; padrão) ou 'copernicus' (superfície, mundial).")
                .Texto("poligono", "Handle da polilinha/círculo fechado da área.")
                .ListaNumeros("retangulo", "Alternativa: x_min, y_min, x_max, y_max (m, UTM do desenho).", maxItens: 4)
                .Numero("margem", "Margem além da área (m) lida antes do recorte.", minimo: 0, maximo: 2000, padrao: 60)
                .Numero("fuso", "Fuso UTM SIRGAS 2000, se o desenho não tiver sistema definido.", minimo: 1, maximo: 60)
                .Booleano("norte", "Hemisfério norte (com 'fuso').", false),
            Relevo, somenteLeitura: false, limite: TimeSpan.FromMinutes(10),
            esquemaSaida: () => new Esquema().Aberto()
                .Texto("superficie", "Nome da superfície criada.")
                .Numero("pontos", "Pontos de relevo usados.")));
    }

    private static SistemaUtm? Sistema(Argumentos a, Transaction tr, Database db)
    {
        if (a.Numero("fuso") is { } f) return new SistemaUtm((int)Math.Round(f), !a.Booleano("norte", false));
        return GeoCivil.Detectar(tr, db).Sistema;
    }

    private static ResultadoFerramenta Coordenadas(Argumentos a)
    {
        var db = Ctx.Db;
        using var tr = db.TransactionManager.StartTransaction();
        var det = GeoCivil.Detectar(tr, db);
        var sis = Sistema(a, tr, db);
        var lista = new JsonArray();
        foreach (var t in a.ListaTextos("pontos"))
        {
            var c = CoordenadaDigitada.Ler(t) ?? throw new ErroArgumento($"Não entendi a coordenada '{t}'.");
            double lat, lon;
            if (c.Geografica)
            {
                lat = c.Lat; lon = c.Lon;
                sis ??= SistemaUtm.DoPonto(lat, lon);
            }
            else
            {
                if (sis is null) throw new ErroArgumento("Ponto em UTM sem sistema: informe 'fuso' (o desenho não tem sistema definido).");
                (lat, lon) = Utm.ParaGeo(c.E, c.N, sis);
            }
            var u = Utm.ParaUtm(lat, lon, sis!);
            lista.Add(new JsonObject
            {
                ["entrada"] = t, ["lat"] = Math.Round(lat, 9), ["lon"] = Math.Round(lon, 9), ["e"] = Math.Round(u.E, 4), ["n"] = Math.Round(u.N, 4),
                ["sistema"] = sis!.Nome, ["fator_escala"] = Math.Round(u.FatorEscala, 8), ["convergencia_graus"] = Math.Round(u.Convergencia, 6),
                ["gms"] = $"{Angulos.Gms(lat, true)} {Angulos.Gms(lon, false)}",
            });
        }
        tr.Commit();
        var texto = (det.Sistema is null ? "O desenho não tem sistema de coordenadas definido." : $"Sistema do desenho: {det.Sistema} (das {det.Origem}).")
                    + (lista.Count > 0 ? $" {lista.Count} ponto(s) convertido(s)." : "");
        return ResultadoFerramenta.Ok(texto, new JsonObject { ["sistema_desenho"] = det.Sistema?.Nome, ["pontos"] = lista });
    }

    private static ResultadoFerramenta Relevo(Argumentos a)
    {
        var db = Ctx.Db;
        using var tr = db.TransactionManager.StartTransaction();
        var sis = Sistema(a, tr, db) ?? throw new ErroFerramenta("O desenho não tem sistema de coordenadas: informe 'fuso' (SIRGAS 2000) ou rode PRJGEORREF.");
        List<(double X, double Y)>? contorno;
        var h = a.Texto("poligono");
        if (!string.IsNullOrWhiteSpace(h))
        {
            var id = Ctx.IdDeHandle(db, h);
            if (id.IsNull || tr.GetObject(id, OpenMode.ForRead) is not Curve c) throw new ErroFerramenta($"Handle {h} não é uma curva do desenho.");
            contorno = VolumesCivil.Vertices(c) ?? throw new ErroFerramenta($"A curva {h} não é fechada.");
        }
        else
        {
            var rr = a.ListaNumeros("retangulo");
            if (rr.Count != 4) throw new ErroArgumento("Informe 'poligono' (handle) ou 'retangulo' com 4 números.");
            double x0 = Math.Min(rr[0], rr[2]), x1 = Math.Max(rr[0], rr[2]), y0 = Math.Min(rr[1], rr[3]), y1 = Math.Max(rr[1], rr[3]);
            contorno = new List<(double X, double Y)> { (x0, y0), (x1, y0), (x1, y1), (x0, y1) };
        }
        var nm = a.Texto("modelo");
        ModeloRelevo modelo;
        if (!string.IsNullOrWhiteSpace(nm)) modelo = ModeloRelevo.DoId(nm) ?? throw new ErroArgumento("modelo: use 'anadem' ou 'copernicus'.");
        else
        {
            var (la, lo) = Utm.ParaGeo(contorno[0].X, contorno[0].Y, sis);
            modelo = ModeloRelevo.Anadem.Cobre(la, la, lo, lo) ? ModeloRelevo.Anadem : ModeloRelevo.Copernicus;
        }
        GeoCivil.Relevo r;
        try { r = GeoCivil.CriarRelevo(tr, db, sis, contorno, a.Numero("margem", 60), null, modelo); }
        catch (InvalidOperationException ex) { throw new ErroFerramenta(ex.Message); }
        tr.Commit();
        return ResultadoFerramenta.Ok(
            $"Superfície '{r.Nome}' criada em {sis}: {r.Pontos} pontos (grade de ~30 m), cotas de {r.Zmin:0.0} a {r.Zmax:0.0} m. " +
            (modelo.Terreno ? "Modelo de terreno (vegetação removida). " : "Modelo de superfície (inclui vegetação e prédios). ") +
            "Altitudes derivadas do geoide EGM2008. Fonte: " + modelo.Atribuicao + ".",
            new JsonObject
            {
                ["superficie"] = r.Nome, ["pontos"] = r.Pontos, ["cota_min"] = Math.Round(r.Zmin, 2), ["cota_max"] = Math.Round(r.Zmax, 2),
                ["area_km2"] = Math.Round(r.AreaKm2, 4), ["ladrilhos"] = JsonMcp.Array(r.Ladrilhos, l => JsonValue.Create(l)),
                ["kb_baixados"] = Math.Round(r.Baixados / 1024.0, 1), ["sistema"] = sis.Nome, ["modelo"] = modelo.Nome,
            });
    }
}
