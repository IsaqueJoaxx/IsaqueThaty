"""Testes do gerador de greide, sempre com dados sintéticos."""
import copy
import json
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np
from openpyxl import load_workbook

sys.path.insert(0, str(Path(__file__).parent / "greide"))
import analise  # noqa: E402
import demo  # noqa: E402
import gerador  # noqa: E402
from geometria import Eixo  # noqa: E402
from otimizador import Parametros, processar  # noqa: E402
from terreno import Levantamento  # noqa: E402


class Base(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        cls.pasta = Path(cls.tmp.name)
        cls.eixo, cls.lev, cls.perfil = demo.gerar(cls.pasta / "dados")
        cls.saida, cls.r, cls.arquivos = gerador.executar(cls.eixo, cls.lev, cls.pasta / "res", perfil_iri=cls.perfil, sintetico=True)

    @classmethod
    def tearDownClass(cls):
        cls.tmp.cleanup()


class TesteFluxo(Base):
    def test_gera_todas_as_saidas(self):
        for nome in ("Notas_de_servico.xlsx", "Greides_e_corredor.xml", "Corredor_3D.dxf", "Pontos_PNEZD.csv", "Perfil.png", "iri.csv", "relatorio.html", "resultado.json"):
            self.assertIn(nome, self.arquivos)
            self.assertGreater((self.saida / nome).stat().st_size, 0)
        self.assertEqual(self.r["avisos"], [])

    def test_json_sem_nan(self):
        json.loads((self.saida / "resultado.json").read_text(encoding="utf-8"), parse_constant=lambda c: self.fail(c))

    def test_nota_excel_confere_com_a_espessura_minima(self):
        # Recalcula a partir da planilha entregue, sem usar o otimizador.
        wb = load_workbook(self.saida / "Notas_de_servico.xlsx", data_only=True, read_only=True)
        linhas = [r for r in wb["NS Final"].iter_rows(min_row=5, values_only=True) if r[0] is not None]
        wb.close()
        col = {"eixo": (2, 3), "le35": (5, 6), "ld35": (8, 9), "le55": (11, 12), "ld55": (14, 15)}
        for t, p in col.values():
            esp = [round(l[p] - l[t], 3) for l in linhas if l[p] is not None and l[t] is not None]
            self.assertGreaterEqual(min(esp), 0.23)
        self.assertEqual(len(linhas), self.r["resumo"]["n_estacas"])

    def test_conferencias_aprovam_o_exemplo(self):
        falhas = [i for i in self.r["verificacoes"] if i["situacao"] == "falha"]
        self.assertEqual(falhas, [])
        nomes = " ".join(i["item"] for i in self.r["verificacoes"])
        for trecho in ("Espessura mínima", "Rampa máxima", "K mínimo", "Caimento transversal", "Raio mínimo"):
            self.assertIn(trecho, nomes)

    def test_pico_proposital_e_encontrado_e_governa(self):
        picos = self.r["levantamento"]["picos"]
        alvo = [q for q in picos if abs(q["estaca"] - demo.ESTACA_PICO) < 0.01 and q["ponto"] == "Eixo"]
        self.assertEqual(len(alvo), 1, picos)
        self.assertAlmostEqual(alvo[0]["residuo_mm"], 80, delta=12)
        self.assertTrue(alvo[0]["governa"])

    def test_greide_fica_mais_liso_que_a_pista(self):
        o = self.r["ondas_longas"]
        self.assertLess(o["final"]["media"], o["terreno"]["media"] / 2)
        self.assertEqual(o["onda_minima_m"], 10.0)

    def test_iri_calculado_por_segmento(self):
        i = self.r["iri"]
        self.assertEqual(i["status"], "calculado")
        self.assertEqual(i["classe"], "iri")
        self.assertEqual(len(i["segmentos"]), 9)
        self.assertGreater(i["media"], 0.3)
        self.assertLess(i["media"], 15)

    def test_relatorio_marca_dados_sinteticos(self):
        texto = (self.saida / "relatorio.html").read_text(encoding="utf-8")
        self.assertIn("DADOS SINTÉTICOS", texto)
        self.assertIn("não é IRI", texto)


class TesteConferencias(Base):
    def resultado(self, **kw):
        p = Parametros(**kw)
        ex = Eixo(self.eixo)
        res = processar(ex, Levantamento(self.lev), p, log=lambda _: None)
        return res, ex.pontos(res.estacas)[4]

    def achar(self, itens, trecho):
        return next(i for i in itens if trecho in i["item"])

    def test_detecta_espessura_abaixo_do_minimo(self):
        res, k = self.resultado()
        ruim = copy.copy(res)
        ruim.cotas_final = res.cotas_final.copy()
        ruim.cotas_final[100, 2] -= 0.05
        itens = analise.verificar(ruim, k, gerador.LIMITES_PADRAO)
        self.assertEqual(self.achar(itens, "Espessura mínima sobre a pista")["situacao"], "falha")
        self.assertEqual(analise.situacao_geral(itens), "falha")

    def test_detecta_rampa_e_k(self):
        res, k = self.resultado()
        itens = analise.verificar(res, k, {**gerador.LIMITES_PADRAO, "rampa_max": 0.01, "k_convexa": 1e7, "k_concava": 1e7})
        self.assertEqual(self.achar(itens, "Rampa máxima")["situacao"], "falha")
        self.assertEqual(self.achar(itens, "K mínimo em curva convexa")["situacao"], "falha")

    def test_k_tem_valor_certo(self):
        # Parábola com K conhecido: a variação de rampa por metro é 1/K.
        res, k = self.resultado()
        ruim = copy.copy(res)
        s = res.estacas
        ruim.greide_final = 500 - (s - s[0]) ** 2 / (2 * 100 * 200.0)
        itens = analise.verificar(ruim, k, gerador.LIMITES_PADRAO)
        self.assertEqual(self.achar(itens, "K mínimo em curva convexa")["valor"], "200 m/%")

    def test_detecta_caimento_errado(self):
        res, k = self.resultado()
        ruim = copy.copy(res)
        ruim.cotas_final = res.cotas_final.copy()
        ruim.cotas_final[50, 1] += 0.02
        self.assertEqual(self.achar(analise.verificar(ruim, k, gerador.LIMITES_PADRAO), "Caimento transversal da pista, final")["situacao"], "falha")

    def test_raio_e_superelevacao(self):
        res, k = self.resultado()
        itens = analise.verificar(res, k, gerador.LIMITES_PADRAO)
        self.assertEqual(self.achar(itens, "Superelevação")["situacao"], "atencao")
        self.assertEqual(self.achar(itens, "Raio mínimo")["valor"], "500 m")
        self.assertIn("0,14", self.achar(itens, "Raio mínimo")["nota"])
        self.assertRegex(self.achar(itens, "Espessura mínima sobre a pista")["valor"], r"^0,\d{3} m$")
        apertado = analise.verificar(res, k * 4, gerador.LIMITES_PADRAO)
        self.assertEqual(self.achar(apertado, "Raio mínimo")["situacao"], "falha")


class TesteEntradas(Base):
    def test_levantamento_a_5_m_nao_vira_iri(self):
        estacas = np.arange(0, 1000, 5.0)
        p = self.pasta / "esparso.csv"
        p.write_text("\n".join(f"{s};{500 + 0.001 * s:.3f}" for s in estacas), encoding="utf-8")
        _, r, arquivos = gerador.executar(self.eixo, self.lev, self.pasta / "res2", perfil_iri=p)
        self.assertEqual(r["iri"]["status"], "nao_calculado")
        self.assertIn("IRI não calculado", r["iri"]["motivo"])
        self.assertNotIn("iri.csv", arquivos)

    def test_sem_perfil_explica_o_motivo(self):
        _, r, _ = gerador.executar(self.eixo, self.lev, self.pasta / "res3")
        self.assertEqual(r["iri"]["status"], "sem_dados")
        self.assertIn("0,30 m", r["iri"]["motivo"])

    def test_parametros_recusados(self):
        for ruim in ({"passo": 0}, {"esp_min": "x"}, {"modo": "outro"}, {"passo": float("nan")}, {"sta_ini": 10, "sta_fim": 5}):
            with self.assertRaises(ValueError, msg=str(ruim)):
                gerador.montar_parametros(ruim)
        with self.assertRaises(ValueError):
            gerador.montar_limites({"rampa_max": -1})

    def test_parametros_mudam_o_resultado(self):
        _, r, _ = gerador.executar(self.eixo, self.lev, self.pasta / "res4", parametros={"esp_min": 0.30})
        self.assertGreaterEqual(r["resumo"]["esp_min"], 0.30)
        self.assertGreater(r["resumo"]["vol_final_terreno"], self.r["resumo"]["vol_final_terreno"])


if __name__ == "__main__":
    unittest.main()
