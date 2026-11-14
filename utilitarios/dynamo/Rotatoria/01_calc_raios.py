# NÓ 1 – Calc Raios
# Entradas: raio_ilha, largura_faixa, num_faixas
# Saída: [r_ilha, r_pista_interna, r_pista_externa, r_total]

raio_ilha       = IN[0]   # ex: 10.0 m  (borda da ilha central)
largura_faixa   = IN[1]   # ex: 4.0  m
num_faixas      = IN[2]   # ex: 2

r_ilha          = raio_ilha
r_pista_interna = r_ilha
r_pista_externa = r_ilha + largura_faixa * num_faixas
r_total         = r_pista_externa + 1.5   # canteiro externo mínimo

OUT = [r_ilha, r_pista_interna, r_pista_externa, r_total]
