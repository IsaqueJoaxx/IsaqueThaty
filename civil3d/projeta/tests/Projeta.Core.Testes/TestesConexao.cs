using Projeta.Core.Alinhamentos;

namespace Projeta.Core.Testes;

/// <summary>Testes da conexão entre eixos (rotatórias): arco tangente, greide da conexão e casamento de cotas.</summary>
internal static class TestesConexao
{
    public static void Rodar(Action<bool, string> Ok)
    {
        static bool Q(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;

        // retas perpendiculares, canto x>0 / y>0, raio 10 nos eixos
        var a = EixoGeometrico.Reta("A", -100, 0, 100, 0);
        var b = EixoGeometrico.Reta("B", 0, -100, 0, 100);
        Ok(Q(a.Ponto(100, 3).X, 0) && Q(a.Ponto(100, 3).Y, -3) && Q(b.Ponto(100, 2).X, 2), "afastamento positivo = à direita (como no Civil)");
        var c1 = Conexao.Calcular(a, 100, -1, 0, b, 100, +1, 0, 10);
        Ok(c1 is not null && Q(c1.Centro.X, 10) && Q(c1.Centro.Y, 10) && Q(c1.EstacaA, 110) && Q(c1.EstacaB, 110), $"arco tangente a duas retas ({c1?.Centro})");
        Ok(c1 is not null && !c1.AntiHorario && Q(c1.AnguloCentral, Math.PI / 2) && Q(c1.Desenvolvimento, 5 * Math.PI) && Q(c1.Bulge, -Math.Tan(Math.PI / 8)), "sentido, ângulo central, desenvolvimento e bulge");
        Ok(c1 is not null && c1.SentidoA == -1 && c1.SentidoB == +1 && Q(c1.Meio.X, 10 - 10 / Math.Sqrt(2)), "sentido do arco em relação a A e B e ponto do meio");

        // com afastamento de bordo (3 m em A e 4 m em B)
        var c2 = Conexao.Calcular(a, 105, -1, 3, b, 105, +1, 4, 10);
        Ok(c2 is not null && Q(c2.Centro.X, 14) && Q(c2.Centro.Y, 13) && Q(c2.TangenteA.X, 14) && Q(c2.TangenteA.Y, 3) && Q(c2.TangenteB.X, 4) && Q(c2.TangenteB.Y, 13),
           $"arco entre os bordos afastados ({c2?.TangenteA}; {c2?.TangenteB})");

        // anel de raio 30 (anti-horário) e ramo radial chegando do leste, conexão no bordo externo (4 m) e no bordo do ramo (3,5 m), R 15
        var anel = EixoGeometrico.Circulo("Anel", 0, 0, 30);
        var ramo = EixoGeometrico.Reta("Ramo", 100, 0, 30, 0);
        var c3 = Conexao.Calcular(anel, 5, +1, 4, ramo, 50, +1, 3.5, 15);
        double xc = Math.Sqrt(49 * 49 - 18.5 * 18.5);
        Ok(c3 is not null && Q(c3.Centro.X, xc, 1e-6) && Q(c3.Centro.Y, 18.5, 1e-6) && Q(c3.EstacaB, 100 - xc, 1e-6) && Q(c3.EstacaA, 30 * Math.Atan2(18.5, xc), 1e-6),
           $"anel + ramo: centro ({c3?.Centro.X:F4}; {c3?.Centro.Y:F4}) x ({xc:F4}; 18,5)");
        Ok(c3 is not null && Q(Math.Sqrt(c3.TangenteA.X * c3.TangenteA.X + c3.TangenteA.Y * c3.TangenteA.Y), 34, 1e-6) && Q(c3.TangenteB.Y, 3.5, 1e-9), "tangência no bordo do anel (r = 34) e no bordo do ramo");
        Ok(Conexao.Calcular(a, 100, -1, 0, EixoGeometrico.Reta("C", 0, 50, 0, 100), 20, +1, 0, 10) is null, "sem arco quando o outro eixo não chega ao canto");

        // greide da conexão
        var p1 = PerfilConexao.Calcular(30, 100, 0.04, 100, -0.04);
        Ok(p1.Count == 3 && Q(p1[1].Estaca, 15) && Q(p1[1].Cota, 100.6) && Q(p1[1].Curva, 29.4), "greide: PVI no cruzamento das tangentes com parábola");
        Ok(Q(PerfilConexao.Cota(p1, 0), 100) && Q(PerfilConexao.Cota(p1, 30), 100) && Q(PerfilConexao.Cota(p1, 15), 100.306, 1e-9), $"greide: cotas nas pontas e no vértice ({PerfilConexao.Cota(p1, 15):F4})");
        var p2 = PerfilConexao.Calcular(30, 100, 0.02, 100.6, 0.02);
        Ok(p2.Count == 2 && Q(PerfilConexao.Cota(p2, 15), 100.3), "greide: rampa única quando as tangentes são a mesma reta");
        static double Rampa(IReadOnlyList<PerfilConexao.Pvi> p, double s, double h) => (PerfilConexao.Cota(p, s + h) - PerfilConexao.Cota(p, s)) / h;
        var p3 = PerfilConexao.Calcular(20, 100, 0.10, 100, 0.10);
        Ok(p3.Count == 4 && Q(PerfilConexao.Cota(p3, 0), 100) && Q(PerfilConexao.Cota(p3, 20), 100) && Q(Rampa(p3, 0, 0.01), 0.10, 1e-9) && Q(Rampa(p3, 19.99, 0.01), 0.10, 1e-9),
           "greide: rampas paralelas → dois PVIs, sai e chega com 10 %");
        // caso do teste no Civil: anel plano no bordo (849,92; 0 %) e ramo a +1 % (850,084), L 17,75 → cruzamento a 1,35 m
        var p4 = PerfilConexao.Calcular(17.75, 849.92, 0, 850.084, 0.01);
        Ok(p4.Count == 3 && Q(p4[1].Estaca, 1.35, 1e-9) && Q(Rampa(p4, 0, 0.01), 0, 1e-9) && Q(Rampa(p4, 17.74, 0.01), 0.01, 1e-9) && Q(PerfilConexao.Cota(p4, 17.75), 850.084),
           $"greide: cruzamento perto da ponta ainda casa as rampas (PVI {p4[1].Estaca:F3}, L {p4[1].Curva:F3})");
        var p5 = PerfilConexao.Calcular(30, 100, 0.03, 100.3, 0.02);
        Ok(p5.Count == 4 && Q(Rampa(p5, 0, 0.01), 0.03, 1e-9) && Q(Rampa(p5, 29.99, 0.01), 0.02, 1e-9) && Q(PerfilConexao.Cota(p5, 30), 100.3),
           "greide: cruzamento fora do trecho → dois PVIs que casam as duas rampas");

        // casamento de cotas e rampas
        var v1 = Casamento.Avaliar(850, 0.02, 849.93, 0.021, 0, 3.5, -0.02);
        Ok(v1.CotaCasa && Q(v1.CotaAAjustada, 849.93) && Q(v1.Dz, 0, 1e-9) && v1.RampaComparavel && v1.RampaCasa, "casa: bordo a 3,5 m com caimento de 2 %");
        var v2 = Casamento.Avaliar(850, 0.02, 850.12, 0.0, 0, 0, 0);
        Ok(!v2.CotaCasa && Q(v2.Dz, 0.12) && !v2.RampaCasa, "não casa: 12 cm de diferença e rampa diferente");
        var v3 = Casamento.Avaliar(850, 0.03, 850.005, -0.05, Math.PI / 2, 0, 0);
        Ok(v3.CotaCasa && !v3.RampaComparavel && v3.RampaCasa && Q(v3.RampaANaDirecaoB, 0, 1e-12), "encontro em ângulo reto: rampa não é comparada");
        var v4 = Casamento.Avaliar(850, 0.02, 850, -0.02, Math.PI, 0, 0);
        Ok(v4.RampaCasa && Q(v4.RampaANaDirecaoB, -0.02), "eixos em sentidos opostos: rampa de A vira de sinal");
    }
}
