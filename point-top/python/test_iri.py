"""Testes do cálculo de IRI. A referência é a resposta analítica do modelo, sem usar a simulação."""
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parent / "greide"))
import iri  # noqa: E402


def senoide(l_onda, amp, dx, extensao=2000.0):
    x = np.arange(0, extensao, dx)
    return x, amp * np.sin(2 * np.pi * x / l_onda)


def esperado(l_onda, amp, dx):
    """IRI analítico de uma senoide: inclinação média retificada vezes o ganho do modelo."""
    # A média móvel de base b atenua a inclinação pelo fator sinc da senoide.
    base = max(1, int(round(iri.BASE_MM / dx))) * dx
    atenua = abs(np.sinc(base / l_onda))
    return (2 / np.pi) * amp * (2 * np.pi / l_onda) * atenua * iri.ganho(l_onda) * 1000


class TesteModelo(unittest.TestCase):
    def test_plano_inclinado_nao_tem_irregularidade(self):
        x = np.arange(0, 500, 0.25)
        self.assertAlmostEqual(iri.iri_global(100 + 0.05 * x, 0.25), 0.0, places=8)

    def test_senoides_conferem_com_a_resposta_analitica(self):
        for l_onda in (1.5, 2.4, 5.0, 10.0, 15.0, 30.0):
            for dx in (0.05, 0.10, 0.25):
                x, y = senoide(l_onda, 0.005, dx)
                r = iri.inclinacao_retificada(y, dx)
                obtido = float(np.mean(r[int(100 / dx):]) * 1000)
                alvo = esperado(l_onda, 0.005, dx)
                # Com poucas amostras por ciclo, a média de |seno| amostrada oscila em torno de 2/pi.
                margem = 0.04 if l_onda / dx < 8 else 0.02
                self.assertAlmostEqual(obtido / alvo, 1.0, delta=margem, msg=f"onda {l_onda} dx {dx}: {obtido} x {alvo}")

    def test_picos_de_sensibilidade_em_ondas_de_cerca_de_2_4_e_15_m(self):
        ondas = np.geomspace(0.5, 100, 800)
        g = np.array([iri.ganho(o) for o in ondas])
        picos = [ondas[i] for i in range(1, len(g) - 1) if g[i] > g[i - 1] and g[i] > g[i + 1]]
        self.assertEqual(len(picos), 2, picos)
        self.assertAlmostEqual(picos[0], 2.4, delta=0.5)
        self.assertAlmostEqual(picos[1], 15.2, delta=3)

    def test_linear_na_amplitude(self):
        x, y = senoide(8.0, 0.004, 0.25)
        self.assertAlmostEqual(iri.iri_global(2 * y, 0.25) / iri.iri_global(y, 0.25), 2.0, places=6)


class TesteArquivo(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.pasta = Path(self.tmp.name)

    def tearDown(self):
        self.tmp.cleanup()

    def arq(self, texto):
        p = self.pasta / "perfil.csv"
        p.write_text(texto, encoding="utf-8")
        return p

    def test_duas_trilhas_e_segmentos(self):
        x, y = senoide(8.0, 0.005, 0.25, 1000.0)
        linhas = ["distancia;LE;LD"] + [f"{a:.2f};{b + 100:.5f};{b * 2 + 100:.5f}".replace(".", ",") for a, b in zip(x, y)]
        r = iri.calcular(self.arq("\n".join(linhas)), segmento=100, limite=1.0)
        self.assertEqual(r["trilhas"], ["Trilha esquerda", "Trilha direita"])
        self.assertEqual(len(r["segmentos"]), 9)
        alvo = esperado(8.0, 0.005, 0.25)
        s = r["segmentos"][5]
        self.assertAlmostEqual(s["iri"]["Trilha esquerda"] / alvo, 1.0, delta=0.02)
        self.assertAlmostEqual(s["iri"]["Trilha direita"] / alvo, 2.0, delta=0.04)
        self.assertTrue(r["segmentos"][0]["aquecimento"])
        self.assertFalse(r["segmentos"][1]["aquecimento"])

    def test_recusa_levantamento_a_cada_5_m(self):
        x = np.arange(0, 500, 5.0)
        p = self.arq("\n".join(f"{a};{100 + 0.001 * a}" for a in x))
        with self.assertRaisesRegex(ValueError, "IRI não calculado"):
            iri.calcular(p)

    def test_classe_2_e_aviso(self):
        x, y = senoide(8.0, 0.005, 0.5, 500.0)
        r = iri.calcular(self.arq("\n".join(f"{a};{b + 100:.5f}" for a, b in zip(x, y))))
        self.assertEqual(r["classe"], "classe2")
        self.assertTrue(any("Classe 2" in a for a in r["avisos"]))

    def test_falha_invalida_so_o_segmento_afetado(self):
        x, y = senoide(8.0, 0.005, 0.25, 600.0)
        m = (x < 250) | (x > 260)
        r = iri.calcular(self.arq("\n".join(f"{a};{b + 100:.5f}" for a, b in zip(x[m], y[m]))))
        estado = [s["valido"] for s in r["segmentos"]]
        self.assertEqual(estado, [True, True, False, True, True])
        self.assertIsNone(r["segmentos"][2]["media"])

    def test_milimetros(self):
        x, y = senoide(8.0, 0.005, 0.25, 500.0)
        a = iri.calcular(self.arq("\n".join(f"{p};{q + 100:.5f}" for p, q in zip(x, y))))
        b = iri.calcular(self.arq("\n".join(f"{p};{(q + 100) * 1000:.2f}" for p, q in zip(x, y))), unidade="mm")
        self.assertAlmostEqual(a["media"], b["media"], places=2)

    def test_aviso_de_resolucao_grosseira(self):
        x, y = senoide(8.0, 0.02, 0.25, 500.0)
        r = iri.calcular(self.arq("\n".join(f"{p};{np.round(q + 100, 2):.2f}" for p, q in zip(x, y))))
        self.assertTrue(any("Resolução" in a for a in r["avisos"]))

    def test_parcela_de_ondas_longas(self):
        s = np.arange(0, 1000, 5.0)
        self.assertAlmostEqual(iri.parcela_ondas_longas(s, 100 + 0.01 * s)["media"], 0.0, places=6)
        z = 100 + 0.02 * np.sin(2 * np.pi * s / 40)
        p = iri.parcela_ondas_longas(s, z)
        alvo = esperado(40.0, 0.02, 0.25)
        self.assertAlmostEqual(p["segmentos"][4]["valor"] / alvo, 1.0, delta=0.05)
        self.assertEqual(p["onda_minima_m"], 10.0)


if __name__ == "__main__":
    unittest.main()
