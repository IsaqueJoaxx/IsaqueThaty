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

        // interseção: centro da esquina entre braços a 0° e 90° (bordos 3 e 4 m, R 10) = (4 + 10; 3 + 10); em linha não há esquina
        var ce = GeometriaIntersecao.CentroEsquina(0, Math.PI / 2, 3, 4, 10);
        Ok(ce is { } e1 && Q(e1.X, 14) && Q(e1.Y, 13), $"centro da esquina da interseção ({ce})");
        Ok(GeometriaIntersecao.CentroEsquina(0, Math.PI, 3, 3, 10) is null, "braços em linha: sem esquina");
        var ce2 = GeometriaIntersecao.CentroEsquina(Math.PI / 2, Math.PI, 3.5, 3.5, 12);
        Ok(ce2 is { } e2 && Q(e2.X, -15.5) && Q(e2.Y, 15.5), $"esquina do quadrante noroeste ({ce2})");
        Ok(Q(GeometriaIntersecao.DistanciaVisibilidade(40), 83.4, 1e-9), "visibilidade de cruzamento: 0,278 × 40 × 7,5 = 83,4 m");
        Ok(GeometriaIntersecao.SegmentoCortaCirculo((0, 0), (10, 0), (5, 1), 2) && !GeometriaIntersecao.SegmentoCortaCirculo((0, 0), (10, 0), (5, 3), 2), "visada cortando a esquina");

        // faixa de conversão: Tabela 6 do DNIT (desaceleração até parar e teiper) e alargamento com teiper linear
        Ok(GeometriaIntersecao.DesaceleracaoDnit(40) == (60, 40, 40) && GeometriaIntersecao.DesaceleracaoDnit(45) == (75, 45, 50)
           && GeometriaIntersecao.DesaceleracaoDnit(30) == (60, 40, 40) && GeometriaIntersecao.DesaceleracaoDnit(130) == (200, 100, 120), "Tabela 6 do DNIT por velocidade");
        Ok(Q(GeometriaIntersecao.MeiaLarguraAlargada(3.5, 3.6, 50, 40, 10), 5.3) && Q(GeometriaIntersecao.MeiaLarguraAlargada(3.5, 3.6, 50, 40, 70), 4.4)
           && Q(GeometriaIntersecao.MeiaLarguraAlargada(3.5, 3.6, 50, 40, 95), 3.5), "alargamento da faixa de conversão com teiper");

        // ilha canalizadora a 90°: bordos a 3,5 m, raio 25, pista de conversão de 5,4 m, afastamento 0,6 m
        var ilha = GeometriaIntersecao.Ilha(0, Math.PI / 2, 3.5, 3.5, 25, 5.4, 0.6);
        double ladoIlha = 24.4 - Math.Sqrt(30.4 * 30.4 - 24.4 * 24.4);
        Ok(ilha is { Cabe: true } il && Q(il.Nariz.X, 4.1) && Q(il.Nariz.Y, 4.1) && Q(il.LadoA, ladoIlha, 1e-6) && Q(il.LadoB, ladoIlha, 1e-6)
           && Q(il.Area, 17.715, 0.02) && Q(il.LadoArco, 30.4 * 2 * Math.Asin(ladoIlha * Math.Sqrt(2) / 2 / 30.4), 1e-6),
           $"ilha canalizadora: área {ilha?.Area:0.000} m², lados {ilha?.LadoA:0.000} m");
        Ok(GeometriaIntersecao.Ilha(0, Math.PI / 2, 3.5, 3.5, 10, 5.4, 0.6) is { Cabe: false }, "ilha que não cabe com raio de 10 m");

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

        // geometria da rotatória
        Ok(GeometriaRotatoria.Rumo(0) == "L" && GeometriaRotatoria.Rumo(Math.PI / 2) == "N" && GeometriaRotatoria.Rumo(Math.PI) == "O"
           && GeometriaRotatoria.Rumo(-Math.PI / 4) == "SE" && GeometriaRotatoria.Rumo(3 * Math.PI / 4) == "NO" && GeometriaRotatoria.Rumo(2 * Math.PI - 0.1) == "L", "rumo dos braços");
        Ok(Q(GeometriaRotatoria.AnguloEmenda(new[] { 0.0, Math.PI / 2, Math.PI }), 1.5 * Math.PI) && Q(GeometriaRotatoria.AnguloEmenda(new[] { 1.0 }), 1 + Math.PI)
           && Q(GeometriaRotatoria.AnguloEmenda(new[] { 0.2, 6.0 }), 3.1), "emenda do anel no meio do maior vão");
        var ch = GeometriaRotatoria.ChuteCentroConexao(17, 0, 1, 0, +1, 18.5, 0, 0, 35);
        var chs = GeometriaRotatoria.ChuteCentroConexao(17, 0, 1, 0, -1, 18.5, 0, 0, 35);
        Ok(Q(ch.X, Math.Sqrt(882.75), 1e-9) && Q(ch.Y, 18.5) && Q(chs.Y, -18.5), $"palpite do centro da conexão de entrada/saída ({ch.X:F3}; {ch.Y:F3})");
        Ok(Q(GeometriaRotatoria.Folga(0, 0.3, Math.PI / 2, Math.PI / 2 - 0.4), Math.PI / 2 - 0.7) && GeometriaRotatoria.Folga(0, 0.9, Math.PI / 2, Math.PI / 2 - 0.8) < 0
           && Q(GeometriaRotatoria.Folga(1.5 * Math.PI, 1.5 * Math.PI + 0.2, 0.1, 0.05), 0.5 * Math.PI + 0.1 - 0.25), "folga entre conexões vizinhas no bordo do anel (inclusive passando pelo leste)");

        // greide do anel passando nas cotas dos braços
        double cAnel = 2 * Math.PI * 17;
        var alvos = new[] { new PerfilAnel.Alvo(13.35, 850.2), new PerfilAnel.Alvo(40.05, 850.0), new PerfilAnel.Alvo(66.76, 849.7), new PerfilAnel.Alvo(93.46, 850.1) };
        var ga = PerfilAnel.Calcular(cAnel, alvos);
        double erroAlvo = alvos.Max(x => Math.Abs(PerfilConexao.Cota(ga, x.Estaca) - x.Cota));
        Ok(ga.Count == 6 && erroAlvo < 1e-7 && ga.Skip(1).Take(4).All(v => v.Curva > 0), $"anel: a curva passa na cota de cada braço (erro {erroAlvo:E1})");
        Ok(Q(Rampa(ga, 0, 0.01), Rampa(ga, cAnel - 0.01, 0.01), 1e-9) && Q(ga[0].Cota, ga[^1].Cota), "anel: sem quebra de rampa na emenda");
        bool semSobrepor = true;
        for (int i = 1; i < ga.Count - 1; i++)
        {
            double ini = ga[i].Estaca - ga[i].Curva / 2, fim = ga[i].Estaca + ga[i].Curva / 2;
            if (ini < ga[i - 1].Estaca + ga[i - 1].Curva / 2 - 1e-9 || fim > ga[i + 1].Estaca - ga[i + 1].Curva / 2 + 1e-9) semSobrepor = false;
        }
        var rm = PerfilAnel.RampaMaxima(ga);
        Ok(semSobrepor && Math.Abs(rm.Rampa) > 0.005 && Math.Abs(rm.Rampa) < 0.05, $"anel: curvas sem se sobrepor, rampa máxima {rm.Rampa * 100:F2}%");
        var plano = PerfilAnel.Calcular(cAnel, new[] { new PerfilAnel.Alvo(30, 851) });
        Ok(plano.Count == 2 && Q(plano[0].Cota, 851) && Q(plano[1].Cota, 851), "anel com um braço só fica plano");

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
