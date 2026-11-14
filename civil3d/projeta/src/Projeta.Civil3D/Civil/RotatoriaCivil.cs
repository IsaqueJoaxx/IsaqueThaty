using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Projeta.Core.Alinhamentos;
using Projeta.Core.Comum;

namespace Projeta.Civil3D.Civil;

/// <summary>Geometria de alinhamentos para rotatórias: encontros entre eixos, lados, greides, conexões.</summary>
internal static class RotatoriaCivil
{
    /// <summary>Encontro entre dois eixos: a ponta (ou o cruzamento) de um sobre o outro.</summary>
    /// <param name="Principal">Eixo de referência (onde o outro chega).</param>
    /// <param name="Ponta">Eixo que chega (para cruzamento, o segundo clicado).</param>
    /// <param name="Afastamento">Distância assinada do ponto ao eixo principal (+ à direita).</param>
    internal sealed record Encontro(Alignment Principal, Alignment Ponta, Point3d P, double SPrincipal, double SPonta, double Afastamento, string Tipo)
    {
        public bool EhPonta => Tipo != "cruzamento";
    }

    public static bool Fechado(Alignment al)
    {
        var a = Ponto(al, al.StartingStation, 0);
        var b = Ponto(al, al.EndingStation, 0);
        return al.EndingStation - al.StartingStation > 1 && Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) < 1e-3;
    }

    /// <summary>Estaca dentro do alinhamento (dá a volta em eixo fechado, como o anel da rotatória).</summary>
    public static double Ajustar(Alignment al, double s) => Ajustar(al, s, Fechado(al));

    private static double Ajustar(Alignment al, double s, bool fechado)
    {
        double ini = al.StartingStation, fim = al.EndingStation, l = fim - ini;
        if (!fechado || l <= 0) return Math.Clamp(s, ini, fim);
        double r = (s - ini) % l;
        if (r < 0) r += l;
        return ini + r;
    }

    public static Point3d Ponto(Alignment al, double s, double o)
    {
        double x = 0, y = 0;
        al.PointLocation(Math.Clamp(s, al.StartingStation, al.EndingStation), o, ref x, ref y);
        return new Point3d(x, y, 0);
    }

    /// <summary>Estaca e afastamento de um ponto; null quando ele fica fora do alcance do alinhamento.</summary>
    public static (double S, double O)? EstacaAfast(Alignment al, Point3d p)
    {
        double s = 0, o = 0;
        try { al.StationOffset(p.X, p.Y, ref s, ref o); return (s, o); }
        catch { return null; }
    }

    /// <summary>Estaca mais próxima de um ponto (ponta do alinhamento quando o ponto está além dele).</summary>
    public static double EstacaPerto(Alignment al, Point3d p)
    {
        if (EstacaAfast(al, p) is { } so) return so.S;
        return Ponto(al, al.StartingStation, 0).DistanceTo(p) <= Ponto(al, al.EndingStation, 0).DistanceTo(p) ? al.StartingStation : al.EndingStation;
    }

    /// <summary>+1 se o ponto fica à direita do alinhamento, −1 à esquerda.</summary>
    public static int Lado(Alignment al, Point3d p)
    {
        if (EstacaAfast(al, p) is { } so && Math.Abs(so.O) > 1e-6) return so.O > 0 ? 1 : -1;
        double s = EstacaPerto(al, p);
        var (dx, dy) = Eixo(al).Direcao(s);
        var q = Ponto(al, s, 0);
        double cruz = dx * (p.Y - q.Y) - dy * (p.X - q.X);
        return cruz > 0 ? -1 : 1;
    }

    /// <summary>O alinhamento como função estaca/afastamento → ponto, para o cálculo da conexão.</summary>
    public static EixoGeometrico Eixo(Alignment al)
    {
        double ini = al.StartingStation, fim = al.EndingStation, l = fim - ini;
        bool fechado = Fechado(al);
        return new EixoGeometrico
        {
            Nome = al.Name,
            Inicio = fechado ? ini - l : ini,
            Fim = fechado ? fim + l : fim,
            Ponto = (s, o) =>
            {
                double x = 0, y = 0;
                al.PointLocation(Ajustar(al, s, fechado), o, ref x, ref y);
                return (x, y);
            },
        };
    }

    public static (double Dx, double Dy) Direcao(Alignment al, double s) => Eixo(al).Direcao(Ajustar(al, s));

    /// <summary>Greides de projeto do alinhamento (tudo que não é perfil do terreno).</summary>
    public static List<Profile> Greides(Transaction tr, Alignment al)
    {
        var r = new List<Profile>();
        foreach (ObjectId id in al.GetProfileIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Profile p && p.ProfileType != ProfileType.EG) r.Add(p);
        return r;
    }

    /// <summary>Cota e rampa (fração, no sentido das estacas) do greide; null fora do greide.</summary>
    public static (double Z, double G)? CotaRampa(Profile p, double s)
    {
        double ini = p.StartingStation, fim = p.EndingStation;
        if (s < ini - 1e-4 || s > fim + 1e-4) return null;
        s = Math.Clamp(s, ini, fim);
        try
        {
            double z = p.ElevationAt(s);
            const double h = 0.05;
            double a = Math.Max(ini, s - h), b = Math.Min(fim, s + h);
            double g = b - a > 1e-6 ? (p.ElevationAt(b) - p.ElevationAt(a)) / (b - a) : 0;
            return (z, g);
        }
        catch { return null; }
    }

    /// <summary>Encontros entre dois eixos: pontas de um sobre o outro (até <paramref name="busca"/> m), pontas com pontas e cruzamentos.</summary>
    public static List<Encontro> Encontros(Transaction tr, Alignment a, Alignment b, double busca)
    {
        var r = new List<Encontro>();
        bool Repetido(Point3d p, double folga) => r.Any(e => e.P.DistanceTo(p) <= Math.Max(0.05, folga + 1e-6));

        void PontasDe(Alignment ponta, Alignment princ)
        {
            if (Fechado(ponta)) return;
            foreach (bool fim in new[] { false, true })
            {
                double s = fim ? ponta.EndingStation : ponta.StartingStation;
                var p = Ponto(ponta, s, 0);
                if (EstacaAfast(princ, p) is { } so && Math.Abs(so.O) <= busca)
                {
                    if (!Repetido(p, 0)) r.Add(new Encontro(princ, ponta, p, so.S, s, so.O, fim ? "ponta final" : "ponta inicial"));
                    continue;
                }
                if (Fechado(princ)) continue;
                foreach (double sp in new[] { princ.StartingStation, princ.EndingStation })
                {
                    double d = Ponto(princ, sp, 0).DistanceTo(p);
                    if (d <= busca && !Repetido(p, d)) r.Add(new Encontro(princ, ponta, p, sp, s, d, "ponta com ponta"));
                }
            }
        }
        PontasDe(b, a);
        PontasDe(a, b);

        // cruzamentos (polilinhas temporárias, apagadas no fim)
        var temp = new List<Polyline>();
        try
        {
            foreach (var al in new[] { a, b })
                if (tr.GetObject(al.GetPolyline(), OpenMode.ForWrite) is Polyline pl) temp.Add(pl);
            if (temp.Count == 2)
            {
                var pts = new Point3dCollection();
                temp[0].IntersectWith(temp[1], Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                foreach (Point3d q in pts)
                {
                    var p = new Point3d(q.X, q.Y, 0);
                    if (Repetido(p, 0)) continue;
                    if (EstacaAfast(a, p) is not { } sa || EstacaAfast(b, p) is not { } sb) continue;
                    r.Add(new Encontro(a, b, p, sa.S, sb.S, 0, "cruzamento"));
                }
            }
        }
        finally { foreach (var pl in temp) pl.Erase(); }
        return r;
    }

    /// <summary>Menor distância entre as pontas de um eixo e o outro eixo (para dizer quanto falta quando não se encontram).</summary>
    public static double MenorDistancia(Alignment a, Alignment b)
    {
        double m = double.MaxValue;
        foreach (var (x, y) in new[] { (a, b), (b, a) })
            foreach (double s in new[] { x.StartingStation, x.EndingStation })
            {
                var p = Ponto(x, s, 0);
                if (EstacaAfast(y, p) is { } so) m = Math.Min(m, Math.Abs(so.O));
                m = Math.Min(m, Math.Min(Ponto(y, y.StartingStation, 0).DistanceTo(p), Ponto(y, y.EndingStation, 0).DistanceTo(p)));
            }
        return m;
    }

    /// <summary>Ângulo (rad, 0..π) entre as direções dos dois eixos no encontro.</summary>
    public static double Angulo(Encontro e)
    {
        var (ax, ay) = Direcao(e.Principal, e.SPrincipal);
        var (bx, by) = Direcao(e.Ponta, e.SPonta);
        return Math.Acos(Math.Clamp(ax * bx + ay * by, -1, 1));
    }

    public static string NomeUnico(Transaction tr, Autodesk.Civil.ApplicationServices.CivilDocument civ, string nome)
    {
        var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ObjectId id in civ.GetAlignmentIds())
            if (tr.GetObject(id, OpenMode.ForRead) is Alignment al) usados.Add(al.Name);
        if (!usados.Contains(nome)) return nome;
        for (int i = 2; ; i++) if (!usados.Contains($"{nome} ({i})")) return $"{nome} ({i})";
    }

    public static string Est(double s) => Num.Estaca(s);
}

/// <summary>Últimas escolhas dos comandos de rotatória em %APPDATA%\Projeta\rotatoria.json.</summary>
internal sealed class ConfigRotatoria
{
    public double Raio { get; set; } = 15;
    /// <summary>Caimento transversal do eixo até o bordo (fração; −0,02 = desce 2 %).</summary>
    public double Caimento { get; set; } = -0.02;
    public double Busca { get; set; } = 5;
    public double TolPlanta { get; set; } = 0.005;
    public double TolCota { get; set; } = 0.01;
    public double TolRampa { get; set; } = 0.005;
    /// <summary>Distância do eixo ao bordo onde a conexão encosta, por nome de alinhamento.</summary>
    public Dictionary<string, double> Bordos { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public double Bordo(string alinhamento) => Bordos.TryGetValue(alinhamento, out var v) ? v : 0;

    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Projeta", "rotatoria.json");

    public static ConfigRotatoria Carregar()
    {
        try
        {
            if (File.Exists(Arquivo) && JsonProjeta.Desserializar<ConfigRotatoria>(File.ReadAllText(Arquivo)) is { } c)
            {
                c.Bordos = new Dictionary<string, double>(c.Bordos ?? new(), StringComparer.OrdinalIgnoreCase);
                return c;
            }
        }
        catch { }
        return new ConfigRotatoria();
    }

    public static void Salvar(ConfigRotatoria c)
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!); File.WriteAllText(Arquivo, JsonProjeta.Serializar(c)); }
        catch { }
    }
}
