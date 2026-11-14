# NÓ 2 – Pontos de Acesso
# Calcula os ângulos e pontos de entrada/saída de cada ramal
# Entradas: centro [x,y], num_acessos, r_pista_externa, larg_acesso, angulo_inicial_graus
# Saída: lista de dicionários com geometria de cada acesso

import math

centro          = IN[0]   # [x, y]
num_acessos     = IN[1]   # ex: 4
r_externo       = IN[2]   # raio externo da pista
larg_acesso     = IN[3]   # ex: 7.0 m (dois sentidos)
ang_inicial     = IN[4]   # ex: 0.0 graus (Norte = 90, Leste = 0)

cx, cy = centro[0], centro[1]
acessos = []

passo = 360.0 / num_acessos

for i in range(num_acessos):
    ang_deg  = ang_inicial + i * passo
    ang_rad  = math.radians(ang_deg)

    # ponto central do acesso na borda externa
    px = cx + r_externo * math.cos(ang_rad)
    py = cy + r_externo * math.sin(ang_rad)

    # vetor perpendicular ao raio (tangente)
    tang_x = -math.sin(ang_rad)
    tang_y =  math.cos(ang_rad)

    # bordas esquerda e direita do acesso
    meio = larg_acesso / 2.0
    esq  = [px + tang_x * meio, py + tang_y * meio]
    dir_ = [px - tang_x * meio, py - tang_y * meio]

    # ponto externo do ramal (50 m para fora)
    comprimento_ramal = 50.0
    ext_cx = px + math.cos(ang_rad) * comprimento_ramal
    ext_cy = py + math.sin(ang_rad) * comprimento_ramal
    ext_esq = [ext_cx + tang_x * meio, ext_cy + tang_y * meio]
    ext_dir = [ext_cx - tang_x * meio, ext_cy - tang_y * meio]

    acessos.append({
        "indice"      : i,
        "angulo_graus": ang_deg,
        "centro"      : [px, py],
        "esq_interno" : esq,
        "dir_interno" : dir_,
        "esq_externo" : ext_esq,
        "dir_externo" : ext_dir,
    })

OUT = acessos
