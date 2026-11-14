"""Parâmetros e custos do gerador. Todos os valores padrão são PREMISSAS de teste:
troque pelos valores da norma/classe da via e pelas composições SICRO/SINAPI."""
from __future__ import annotations

from dataclasses import dataclass, field, fields


@dataclass
class Parametros:
    rampa_max: float = 0.08            # m/m — rampa máxima do greide
    tolerancia_rampa: float = 0.06     # acima de rampa_max + tolerância (terreno suavizado) a aresta é proibida
    sigma_suavizacao: float = 25.0     # m — escala do terreno "visto pelo greide"
    plataforma: float = 8.0            # m — largura da plataforma
    talude_corte: float = 1.0          # H:V
    talude_aterro: float = 1.5         # H:V
    giro_max_grau: float = 45.0        # mudança máxima de direção por passo da busca
    raio_min: float = 60.0             # m — raio mínimo admitido
    raio_desejado: float = 120.0       # m — raio usado sempre que couber
    tangente_min: float = 20.0         # m — reta mínima entre duas curvas consecutivas
    area_bueiro_ha: float = 2.0        # área de contribuição que define talvegue (travessia = bueiro)
    area_ponte_ha: float = 200.0       # acima disso a travessia é marcada como ponte
    fator_corredor: float = 1.8        # elipse de busca: dA + dB <= fator * |AB|
    n_alternativas: int = 3
    dist_penalizacao: float = 60.0     # m — faixa em volta das alternativas já achadas
    fator_penalizacao: float = 0.6     # x custo de extensão, por metro dentro da faixa
    tolerancia_dp: float | None = None # m — simplificação Douglas-Peucker (padrão 1,2 dx)
    passo_amostragem: float = 5.0      # m — estacas para greide e seções
    k_vertical: float = 15.0           # m/% — suaviza o greide expedito na escala de curvas com esse K
    meia_largura_secao: float = 60.0   # m — até onde procurar o pé/crista do talude
    peso_curva: float = 20000.0        # R$/rad² por mudança de direção na busca (suaviza o traçado)
    penal_borda: float = 2.0           # x custo de extensão, por metro a menos de meia plataforma da borda do MDT


@dataclass
class Custos:
    extensao: float = 1500.0           # R$/m de via (pavimento, drenagem superficial etc.)
    terraplenagem: float = 30.0        # R$/m³ (corte ou aterro, média)
    bueiro: float = 60000.0            # R$/un
    ponte: float = 1500000.0           # R$/un
    contencao: float = 8000.0          # R$/m onde o talude não fecha
    faixa_talvegue: float = 400.0      # R$/m percorrido na faixa do talvegue (proxy de APP)


def montar(cls, dados: dict | None):
    dados = dados or {}
    nomes = {f.name for f in fields(cls)}
    desconhecidos = set(dados) - nomes
    if desconhecidos:
        raise ValueError(f"{cls.__name__}: chaves desconhecidas {sorted(desconhecidos)}")
    return cls(**dados)
