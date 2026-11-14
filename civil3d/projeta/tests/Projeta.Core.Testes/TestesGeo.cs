using System.Text.Json;
using Projeta.Core.Comum;
using Projeta.Core.Geoespacial;
using Projeta.Core.Superficies;

namespace Projeta.Core.Testes;

/// <summary>Testes do módulo Geo: UTM, leitura de coordenadas, Helmert, GeoTIFF, relevo, KML e faixas.</summary>
internal static class TestesGeo
{
    public static void Rodar(Action<bool, string> Ok, string? saida = null)
    {
        static bool Q(double a, double b, double tol) => Math.Abs(a - b) <= tol;
        saida ??= Path.GetTempPath();

        // ---------------------------------------------------------------- UTM contra o PROJ (GDAL 3.x)
        var refs = new (double Lat, double Lon, int Epsg, double E, double N)[]
        {
            (-19.9208, -43.9378, 31983, 611172.0613, 7796931.7837),
            (-15.0, -48.5, 31983, 123519.6795, 8338695.1688),
            (2.8, -60.67, 31975, 91825.4261, 310125.8568),
            (-19.9208, -43.9378, 32723, 611172.0613, 7796931.7836),
            (-33.7, -53.4, 31982, 277563.9099, 6268520.8577),
            (-5.0, -42.0, 31984, 167286.2013, 9446576.0131),
            (-23.55, -46.63, 31983, 333624.1812, 7394647.5221),
        };
        foreach (var r in refs)
        {
            var sis = SistemaUtm.DoEpsg(r.Epsg)!;
            var p = Utm.ParaUtm(r.Lat, r.Lon, sis);
            Ok(Q(p.E, r.E, 0.002) && Q(p.N, r.N, 0.002), $"UTM {sis} de ({r.Lat}; {r.Lon}): {p.E:F4} {p.N:F4} x {r.E} {r.N}");
            var (la, lo) = Utm.ParaGeo(p.E, p.N, sis);
            Ok(Q(la, r.Lat, 1e-9) && Q(lo, r.Lon, 1e-9), $"volta ao geográfico ({la:F10}; {lo:F10})");
        }
        var bh = Utm.ParaUtm(-19.9208, -43.9378, new SistemaUtm(23, true));
        Ok(Q(bh.FatorEscala, 0.99984, 0.0002) && Math.Abs(bh.Convergencia) < 1, $"fator de escala e convergência ({bh.FatorEscala:F6}; {bh.Convergencia:F4}°)");
        Ok(SistemaUtm.FusoDe(-43.9) == 23 && SistemaUtm.FusoDe(-48.0) == 23 && SistemaUtm.FusoDe(-48.01) == 22 && SistemaUtm.FusoDe(-60.67) == 20, "fuso pela longitude");
        Ok(new SistemaUtm(23, true).Epsg == 31983 && new SistemaUtm(22, false).Epsg == 31976 && new SistemaUtm(17, true).Epsg == 31977 && SistemaUtm.DoEpsg(31975) == new SistemaUtm(21, false) && new SistemaUtm(23, true, SistemaUtm.Wgs84).Epsg == 32723, "códigos EPSG");

        // ---------------------------------------------------------------- sistema por texto
        Ok(SistemaUtm.DeTexto("SIRGAS2000.UTM-23S") == new SistemaUtm(23, true), "código SIRGAS2000.UTM-23S");
        Ok(SistemaUtm.DeTexto("UTM84-24S") == new SistemaUtm(24, true, SistemaUtm.Wgs84), "código UTM84-24S");
        Ok(SistemaUtm.DeTexto("EPSG:31982") == new SistemaUtm(22, true), "EPSG:31982");
        Ok(SistemaUtm.DeTexto("SIRGAS 2000 / UTM zone 23S") == new SistemaUtm(23, true), "nome EPSG");
        Ok(SistemaUtm.DeTexto("WGS 84 / UTM zone 20 North") == new SistemaUtm(20, false, SistemaUtm.Wgs84), "nome WGS 84 norte");
        Ok(SistemaUtm.DeTexto("SIRGAS 2000 UTM fuso 22 sul") == new SistemaUtm(22, true), "fuso em português");
        Ok(SistemaUtm.DeTexto("SAD69-UTM-23S") is null && SistemaUtm.DeTexto("") is null && SistemaUtm.DeTexto("LL84") is null, "não reconhece SAD 69 nem geográfico");

        // ---------------------------------------------------------------- ângulos e coordenadas digitadas
        Ok(Q(Angulos.LerGraus("19°55'15\"S"), -(19 + 55 / 60.0 + 15 / 3600.0), 1e-12), "GMS com hemisfério S");
        Ok(Q(Angulos.LerGraus("43º 56' 08,5\" O"), -(43 + 56 / 60.0 + 8.5 / 3600.0), 1e-12), "GMS com º e Oeste");
        Ok(Q(Angulos.LerGraus("S 19 55 15.1"), -(19 + 55 / 60.0 + 15.1 / 3600.0), 1e-12), "hemisfério antes");
        Ok(Q(Angulos.LerGraus("43d56m08sW"), -(43 + 56 / 60.0 + 8 / 3600.0), 1e-12), "notação d m s");
        Ok(Q(Angulos.LerGraus("19d55m15s"), 19 + 55 / 60.0 + 15 / 3600.0, 1e-12), "s final = segundos");
        Ok(Q(Angulos.LerGraus("-19,9208"), -19.9208, 1e-12) && Q(Angulos.LerGraus("43.5 L"), 43.5, 1e-12), "decimal e Leste");
        Ok(double.IsNaN(Angulos.LerGraus("19°75'")) && double.IsNaN(Angulos.LerGraus("abc")), "rejeita minuto ≥ 60 e texto");
        Ok(Angulos.Gms(-19.920833333, true) == "19°55'15,000\"S", $"formata GMS ({Angulos.Gms(-19.920833333, true)})");

        var c1 = CoordenadaDigitada.Ler("-19.9208, -43.9378");
        Ok(c1 is { Geografica: true } && Q(c1.Value.Lat, -19.9208, 1e-12) && Q(c1.Value.Lon, -43.9378, 1e-12), "lat, long do Google Maps");
        var c2 = CoordenadaDigitada.Ler("19°55'15\"S 43°56'08\"W");
        Ok(c2 is { Geografica: true } && c2.Value.Lat < -19.9 && c2.Value.Lon < -43.9, "par em GMS");
        var c3 = CoordenadaDigitada.Ler("43°56'08\"W, 19°55'15\"S");
        Ok(c3 is { Geografica: true } && c3.Value.Lat < -19.9 && c3.Value.Lon < -43.9, "par em GMS com longitude primeiro");
        var c4 = CoordenadaDigitada.Ler("611172,06 7796931,78");
        Ok(c4 is { Geografica: false } && Q(c4.Value.E, 611172.06, 1e-9) && Q(c4.Value.N, 7796931.78, 1e-9), "UTM com vírgula decimal");
        var c5 = CoordenadaDigitada.Ler("611172.06; 7796931.78");
        Ok(c5 is { Geografica: false } && Q(c5.Value.N, 7796931.78, 1e-9), "UTM com ponto e vírgula");
        var c6 = CoordenadaDigitada.Ler("-19,9208, -43,9378");
        Ok(c6 is { Geografica: true } && Q(c6.Value.Lon, -43.9378, 1e-12), "vírgula decimal e vírgula separadora");
        var c7 = CoordenadaDigitada.Ler("7796931.78 611172.06");
        Ok(c7 is { Geografica: false } && Q(c7.Value.E, 611172.06, 1e-9), "UTM com N antes de E");
        Ok(CoordenadaDigitada.Ler("abc") is null && CoordenadaDigitada.Ler("1 2 3") is null, "rejeita lixo");

        // ---------------------------------------------------------------- Helmert
        double ang = 12.5 * Math.PI / 180, esc = 1.0003;
        var orig = new List<(double X, double Y)> { (0, 0), (100, 0), (100, 80), (-20, 50) };
        var dest = orig.Select(p => (esc * (Math.Cos(ang) * p.X - Math.Sin(ang) * p.Y) + 611000, esc * (Math.Sin(ang) * p.X + Math.Cos(ang) * p.Y) + 7796000)).ToList();
        var hl = Transformacao2D.Ajustar(orig, dest, false);
        Ok(Q(hl.Escala, esc, 1e-12) && Q(hl.Rotacao, 12.5, 1e-10) && hl.Rms < 1e-6, $"Helmert recupera escala e rotação ({hl.Escala:F7}; {hl.Rotacao:F6}°)");
        var rg = Transformacao2D.Ajustar(orig, dest, true);
        Ok(Q(rg.Escala, 1, 1e-12) && Q(rg.Rotacao, 12.5, 1e-10) && rg.Rms > 0.001 && rg.Rms < 0.05, $"escala fixa: resíduos da escala ignorada ({rg.Rms:F4} m)");
        var dois = Transformacao2D.Ajustar(orig.Take(2).ToList(), dest.Take(2).ToList(), false);
        Ok(dois.Rms < 1e-6 && Q(dois.Tx, hl.Tx, 1e-6), "dois pontos bastam");

        // ---------------------------------------------------------------- GeoTIFF próprio (gravação e leitura)
        var z = new float[300, 520];
        for (int l = 0; l < 300; l++)
            for (int c = 0; c < 520; c++) z[l, c] = (float)(850 + 0.01 * c * l - 3.5 * Math.Sin(c / 17.0));
        z[5, 7] = -9999;
        var bytes = GeoTiff.Gravar(z, -44.0, -19.0, 1 / 3600.0, 1 / 3600.0, 4326, true, -9999);
        using (var g = GeoTiff.Abrir(new FonteMemoria(bytes)))
        {
            Ok(g.Largura == 520 && g.Altura == 300 && g.Ladrilhado && g.Compressao == 8 && g.Preditor == 3 && g.Epsg == 4326 && g.Geografico, "cabeçalho do GeoTIFF gravado");
            var (x0, y0) = g.Centro(0, 0);
            Ok(Q(x0, -44 + 0.5 / 3600, 1e-12) && Q(y0, -19 - 0.5 / 3600, 1e-12), "centro do pixel (pixel como área)");
            var jan = g.LerJanela(250, 250, 300, 60);
            bool iguais = true;
            for (int l = 0; l < 60; l++)
                for (int c = 0; c < 300; c++)
                {
                    int gl = 250 + l, gc = 250 + c;
                    float esp = gl < 300 && gc < 520 ? z[gl, gc] : float.NaN;
                    if (!(float.IsNaN(esp) && float.IsNaN(jan[l, c])) && esp != jan[l, c]) iguais = false;
                }
            Ok(iguais, "janela cruzando ladrilhos e a borda da imagem");
            Ok(float.IsNaN(g.Valor(7, 5)) && g.Valor(8, 5) == z[5, 8], "sem dados vira NaN");
            var (pc, pl) = g.Pixel(x0 + 10 / 3600.0, y0 - 3 / 3600.0);
            Ok(Q(pc, 10, 1e-9) && Q(pl, 3, 1e-9), "pixel de uma coordenada");
        }
        File.WriteAllBytes(Path.Combine(saida, "geotiff_teste.tif"), bytes);

        // LZW em bytes conhecidos: "TOBEORNOTTOBEORTOBEORNOT" (codificado pelo libtiff, 9 bits, com Clear e EOI)
        var lzw = CodificarLzw(System.Text.Encoding.ASCII.GetBytes("TOBEORNOTTOBEORTOBEORNOT#ABABABABABABABAB"));
        var dec = GeoTiff.Lzw(lzw, 41);
        Ok(System.Text.Encoding.ASCII.GetString(dec) == "TOBEORNOTTOBEORTOBEORNOT#ABABABABABABABAB", "LZW (inclusive o caso KwKwK)");
        var longo = new byte[20000];
        var rnd = new Random(7);
        for (int i = 0; i < longo.Length; i++) longo[i] = (byte)(i % 97 < 50 ? rnd.Next(4) : rnd.Next(256));
        Ok(GeoTiff.Lzw(CodificarLzw(longo), longo.Length).SequenceEqual(longo), "LZW longo (troca de largura até 12 bits e Clear)");

        // ---------------------------------------------------------------- GeoTIFFs gerados pelo GDAL (se existirem nesta máquina)
        var pastaRef = Environment.GetEnvironmentVariable("PROJETA_GEOTIFF_REF") ?? @"C:\Temp\geo\tif";
        var arqRef = Path.Combine(pastaRef, "ref.json");
        if (File.Exists(arqRef))
        {
            using var js = JsonDocument.Parse(File.ReadAllText(arqRef));
            foreach (var caso in js.RootElement.EnumerateObject())
            {
                var arq = Path.Combine(pastaRef, caso.Name);
                if (!File.Exists(arq)) continue;
                try
                {
                    using var g = GeoTiff.Abrir(arq);
                    var e = caso.Value;
                    var (cx, cy) = g.Centro(0, 0);
                    bool geo = g.Largura == e.GetProperty("largura").GetInt32() && g.Altura == e.GetProperty("altura").GetInt32()
                               && Q(cx, e.GetProperty("cx0").GetDouble(), 1e-10) && Q(cy, e.GetProperty("cy0").GetDouble(), 1e-10)
                               && Q(g.Dx, e.GetProperty("dx").GetDouble(), 1e-14);
                    int erradas = 0;
                    foreach (var a in e.GetProperty("amostras").EnumerateArray())
                    {
                        int c = a[0].GetInt32(), l = a[1].GetInt32();
                        float v = g.Valor(c, l);
                        if (a[2].ValueKind == JsonValueKind.Null) { if (!float.IsNaN(v)) erradas++; }
                        else if (!Q(v, a[2].GetDouble(), Math.Abs(a[2].GetDouble()) * 1e-6)) erradas++;
                    }
                    Ok(geo && erradas == 0, $"GDAL {caso.Name}: georreferência {(geo ? "ok" : "ERRADA")}, {erradas} amostra(s) diferente(s) (compressão {g.Compressao}, preditor {g.Preditor}, {g.Bits} bits)");
                }
                catch (Exception ex) { Ok(false, $"GDAL {caso.Name}: {ex.Message}"); }
            }
        }

        // ---------------------------------------------------------------- relevo (ladrilho simulado)
        var lad = new float[360, 360];
        for (int l = 0; l < 360; l++)
            for (int c = 0; c < 360; c++) lad[l, c] = 900 - l + c * 0.5f;
        var tif = GeoTiff.Gravar(lad, -44, -19, 1 / 360.0, 1 / 360.0, 4326, true);
        var rel = Copernicus.Ler(-19.52, -19.48, -43.92, -43.88, Path.Combine(saida, "cache_relevo"),
            nome => nome == Copernicus.Ladrilho(-20, -44) ? new FonteMemoria(tif) : throw new FileNotFoundException());
        Ok(rel.Ladrilhos.Count == 1 && rel.Pontos.Count > 150 && rel.Pontos.All(p => p.Lat >= -19.52 && p.Lat <= -19.48 && p.Lon >= -43.92 && p.Lon <= -43.88),
           $"relevo: pontos só dentro da área ({rel.Pontos.Count})");
        var pr = rel.Pontos[0];
        int lin = (int)Math.Round((-19 - pr.Lat) * 360 - 0.5), col = (int)Math.Round((pr.Lon + 44) * 360 - 0.5);
        Ok(Q(pr.Z, 900 - lin + col * 0.5, 1e-3), "relevo: cota do ponto confere com o ladrilho");
        var rel2 = Copernicus.Ler(-20.2, -19.9, -43.95, -43.9, null, nome => throw new FileNotFoundException());
        Ok(rel2.Ausentes.Count == 2 && rel2.Pontos.Count == 0, "relevo: ladrilho inexistente (mar) não quebra");
        Ok(Copernicus.Ladrilho(-20, -44) == "Copernicus_DSM_COG_10_S20_00_W044_00_DEM" && Copernicus.Ladrilho(2, 10) == "Copernicus_DSM_COG_10_N02_00_E010_00_DEM", "nome dos ladrilhos");

        // ---------------------------------------------------------------- KML/KMZ
        var doc = new DocumentoKml { Nome = "Teste" };
        var m1 = new MarcadorKml { Nome = "PV-01", Pasta = "Drenagem / PVs", Cor = "FF0000" };
        m1.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Ponto, Coordenadas = { new CoordKml(-43.9378, -19.9208, 850.5) } });
        var m2 = new MarcadorKml { Nome = "Eixo", Pasta = "Eixos", Cor = "00FF00", Largura = 3, Absoluto = true };
        m2.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Linha, Coordenadas = { new(-43.9, -19.9, 800), new(-43.91, -19.91, 805.25) } });
        var m3 = new MarcadorKml { Nome = "Lote & área \"1\"", Cor = "0000FF", Descricao = "Área: 360 m²" };
        m3.Geometrias.Add(new GeometriaKml { Tipo = TipoGeometriaKml.Poligono, Coordenadas = { new(-43.9, -19.9), new(-43.9, -19.901), new(-43.901, -19.901) }, Furos = { new() { new(-43.9002, -19.9002), new(-43.9002, -19.9004), new(-43.9004, -19.9004) } } });
        doc.Itens.AddRange(new[] { m1, m2, m3 });
        foreach (var ext in new[] { ".kml", ".kmz" })
        {
            var arq = Path.Combine(saida, "teste" + ext);
            doc.Salvar(arq);
            var lido = DocumentoKml.Ler(arq);
            var a1 = lido.Itens.FirstOrDefault(i => i.Nome == "PV-01");
            var a2 = lido.Itens.FirstOrDefault(i => i.Nome == "Eixo");
            var a3 = lido.Itens.FirstOrDefault(i => i.Nome.StartsWith("Lote"));
            Ok(lido.Itens.Count == 3 && a1 is not null && a1.Pasta == "Drenagem / PVs" && a1.Cor == "FF0000" && Q(a1.Geometrias[0].Coordenadas[0].Lat, -19.9208, 1e-9),
               $"{ext}: ponto com pasta e cor");
            Ok(a2 is not null && a2.Absoluto && a2.Geometrias[0].Coordenadas.Count == 2 && Q(a2.Geometrias[0].Coordenadas[1].Alt, 805.25, 1e-6) && a2.Cor == "00FF00", $"{ext}: linha 3D");
            Ok(a3 is not null && a3.Nome == "Lote & área \"1\"" && a3.Geometrias[0].Coordenadas.Count == 3 && a3.Geometrias[0].Furos.Count == 1 && a3.Descricao == "Área: 360 m²", $"{ext}: polígono com furo e acentos");
        }
        const string ge = """
            <?xml version="1.0" encoding="UTF-8"?>
            <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
            <Document><name>Meus lugares</name>
              <Style id="s_n"><LineStyle><color>ff0000ff</color><width>2</width></LineStyle></Style>
              <StyleMap id="m_1"><Pair><key>normal</key><styleUrl>#s_n</styleUrl></Pair><Pair><key>highlight</key><styleUrl>#s_n</styleUrl></Pair></StyleMap>
              <Folder><name>Obra</name>
                <Placemark><name>Caminho</name><styleUrl>#m_1</styleUrl><LineString><tessellate>1</tessellate><coordinates>
                  -43.93,-19.92,0 -43.94,-19.93,0
                  -43.95,-19.925,0
                </coordinates></LineString></Placemark>
                <Placemark><name>Multi</name><MultiGeometry><Point><coordinates>-43.9,-19.9</coordinates></Point><LinearRing><coordinates>-43.9,-19.9 -43.91,-19.9 -43.91,-19.91 -43.9,-19.9</coordinates></LinearRing></MultiGeometry></Placemark>
                <Placemark><name>Trilha</name><gx:Track><when>2026-01-01</when><gx:coord>-43.9 -19.9 850</gx:coord><gx:coord>-43.91 -19.91 852</gx:coord></gx:Track></Placemark>
              </Folder>
            </Document></kml>
            """;
        var dg = DocumentoKml.LerTexto(ge);
        var cam = dg.Itens.First(i => i.Nome == "Caminho");
        Ok(dg.Nome == "Meus lugares" && cam.Pasta == "Obra" && cam.Cor == "FF0000" && cam.Geometrias[0].Coordenadas.Count == 3, "KML do Google Earth: StyleMap, pasta, coordenadas em várias linhas");
        var mul = dg.Itens.First(i => i.Nome == "Multi");
        Ok(mul.Geometrias.Count == 2 && mul.Geometrias[1].Tipo == TipoGeometriaKml.Poligono && mul.Geometrias[1].Coordenadas.Count == 3, "MultiGeometry com anel fechado");
        var tri = dg.Itens.First(i => i.Nome == "Trilha");
        Ok(tri.Geometrias[0].Tipo == TipoGeometriaKml.Linha && Q(tri.Geometrias[0].Coordenadas[1].Alt, 852, 1e-9), "gx:Track vira linha");

        // ---------------------------------------------------------------- faixas de cota e declividade (áreas exatas)
        P3 P(double x, double y, double zz) => new(x, y, zz);
        var plano = new List<(P3, P3, P3)> { (P(0, 0, 0), P(10, 0, 10), P(10, 10, 10)), (P(0, 0, 0), P(10, 10, 10), P(0, 10, 0)) };
        var fc = MapaFaixas.CotasComPasso(0, 10, 2.5);
        double tot = MapaFaixas.AreasPorCota(plano, fc);
        Ok(fc.Count == 4 && fc.All(f => Q(f.Area, 25, 1e-9)) && Q(tot, 100, 1e-9), $"faixas de cota em plano inclinado ({string.Join("; ", fc.Select(f => f.Area.ToString("0.###")))})");
        var fq = MapaFaixas.CotasComPasso(0, 10, 3);
        MapaFaixas.AreasPorCota(plano, fq);
        Ok(fq.Count == 4 && Q(fq[0].Area, 30, 1e-9) && Q(fq[3].Area, 10, 1e-9) && Q(fq.Sum(f => f.Area), 100, 1e-9), "faixas que não dividem o intervalo");
        var plana = new List<(P3, P3, P3)> { (P(0, 0, 5), P(4, 0, 5), P(0, 4, 5)) };
        var fp = MapaFaixas.CotasComPasso(0, 10, 5);
        MapaFaixas.AreasPorCota(plana, fp);
        Ok(Q(fp[0].Area, 0, 1e-12) && Q(fp[1].Area, 8, 1e-12), "triângulo plano no limite conta uma vez");
        var suave = new List<(P3, P3, P3)> { (P(0, 0, 0), P(100, 0, 5), P(100, 100, 5)), (P(0, 0, 0), P(0, 100, 0), P(100, 100, 30)) };
        var fd = MapaFaixas.Declividades();
        MapaFaixas.AreasPorDeclividade(suave, fd);
        double s2 = MapaFaixas.Declividade(P(0, 0, 0), P(0, 100, 0), P(100, 100, 30));
        Ok(fd.Count == 6 && Q(fd[1].Area, 5000, 1e-6) && fd[1].Classe == "Suave ondulado" && Q(s2, 30, 1e-9) && Q(fd[3].Area, 5000, 1e-6), "classes de declividade da EMBRAPA");
        var ft = MapaFaixas.Cotas(812.3, 897.6, 8);
        Ok(ft.Count >= 7 && ft.Count <= 10 && ft[0].De <= 812.3 && ft[^1].Ate >= 897.6 && Q(ft[1].De - ft[0].De, 10, 1e-9), $"passo redondo das faixas ({ft.Count} de {ft[1].De - ft[0].De})");
        var rp = MapaFaixas.Rampa(3, "000000", "FFFFFF");
        Ok(rp[0] == "000000" && rp[1] == "808080" && rp[2] == "FFFFFF", "rampa de cores");
        var tb = MapaFaixas.Tabela("Declividade", fd, "%");
        Ok(tb.Colunas.Count == 5 && tb.Linhas.Count == 7 && tb.Linhas[1][1] == "Suave ondulado" && tb.Linhas[^1][4] == "100,0", "tabela das faixas");

        // contorno de uma malha 3 × 3 com o quadrado do meio vazio: anel externo (12 vértices) e furo (4)
        var malha = new List<(P3, P3, P3)>();
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                if (i == 1 && j == 1) continue;
                if ((i + j) % 2 == 0) { malha.Add((P(i, j, 0), P(i + 1, j, 0), P(i + 1, j + 1, 0))); malha.Add((P(i, j, 0), P(i, j + 1, 0), P(i + 1, j + 1, 0))); }
                else { malha.Add((P(i, j, 0), P(i + 1, j, 0), P(i, j + 1, 0))); malha.Add((P(i + 1, j + 1, 0), P(i, j + 1, 0), P(i + 1, j, 0))); }
            }
        var aneis = ContornoTin.Aneis(malha);
        Ok(aneis.Count == 2 && aneis[0].Count == 12 && aneis[1].Count == 4, $"contorno da malha ({aneis.Count} anéis: {string.Join(", ", aneis.Select(a => a.Count))})");
    }

    /// <summary>Codificador LZW no formato do TIFF (só para testar o decodificador).</summary>
    private static byte[] CodificarLzw(byte[] dados)
    {
        var saida = new List<byte>();
        int acumulado = 0, nbits = 0;
        int largura = 9;
        void Emitir(int codigo)
        {
            acumulado = (acumulado << largura) | codigo;
            nbits += largura;
            while (nbits >= 8) { saida.Add((byte)(acumulado >> (nbits - 8))); nbits -= 8; acumulado &= (1 << nbits) - 1; }
        }
        var tabela = new Dictionary<string, int>();
        void Limpar() { tabela.Clear(); for (int i = 0; i < 256; i++) tabela[((char)i).ToString()] = i; }
        Limpar();
        int proximo = 258;
        Emitir(256);
        string w = "";
        foreach (var b in dados)
        {
            string wc = w + (char)b;
            if (tabela.ContainsKey(wc)) { w = wc; continue; }
            Emitir(tabela[w]);
            tabela[wc] = proximo++;
            // como o libtiff: tabela cheia → Clear; senão aumenta a largura quando o próximo código não cabe
            if (proximo == 4094)
            {
                Emitir(256);
                Limpar(); proximo = 258; largura = 9;
            }
            else if (proximo > (1 << largura) - 1 && largura < 12) largura++;
            w = ((char)b).ToString();
        }
        if (w.Length > 0)
        {
            Emitir(tabela[w]);
            proximo++;
            if (proximo > (1 << largura) - 1 && largura < 12) largura++;
        }
        Emitir(257);
        if (nbits > 0) saida.Add((byte)(acumulado << (8 - nbits)));
        return saida.ToArray();
    }
}
