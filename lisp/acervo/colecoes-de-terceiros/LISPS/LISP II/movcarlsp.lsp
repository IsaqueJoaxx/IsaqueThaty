(defun c:movcar (/ coor1 coor2 coor3 coor4 ;variaveis para as coordenadas 
pt1aux pt2aux pt3aux pt4aux ;variaveis auxiliares 
ang dist ang2 dist2 ;variaveis para angulo e distancia 
ent1 ent2 ;variaveis para seleção dos objetos 
largbl largbl1 largbl2 largblaux 
num num_ins num_ins2 cmd) 
(initget 1) 
(setq CMD (getvar "CMDECHO")) 
(setvar "CMDECHO" 0) 
;obtem as coordenadas 
(setq coor2 (getpoint "\nLocal do acidente : ")) 
;insere um circulo no local do acidente 
(command "circle" coor2 "6") 
;obtem as coordenadas dos ptos de origem 
(setq coor1 (getpoint "\nPonto de origem do primeiro envolvido : ")) 
(setq coor3 (getpoint "\nPonto de origem do segundo envolvido : ")) 
(setq coor4 coor2) 
;obtendo os angulos e distancias 
(setq ang (angtos (angle coor2 coor1) 0)) 
(setq dist (distance coor1 coor2)) 
(setq ang2 (angtos (angle coor4 coor3) 0)) 
(setq dist2 (distance coor3 coor4)) 
;desenha a rota do objeto 
(command "line" coor1 coor2 "") 
(command "line" coor3 coor4 "") 
;define a distancia a ser inserido os objetos 
(setq largbl 0.999) ;obs: este nro tem que estar com ponto e um valor após o ponto 
;calcula a distancia a ser inserido cada bloco 
(setq largblaux (/ dist dist2)) 
;(alert (rtos largblaux)) 
(setq largbl1 largblaux) 
(setq largbl2 largbl) 

;define pontos auxiliares 
(setq pt1aux coor1) 
(setq pt3aux coor3) 
;define nro de inserções do objeto 
(setq num_ins (/ dist largbl1)) 
(setq num_ins2 (/ dist2 largbl2)) 
;obtem a distancias do objeto em relação a x e y 
(setq larg_x (/ (-(car coor2) (car coor1)) num_ins)) 
(setq larg_y (/ (-(cadr coor2) (cadr coor1)) num_ins)) 
(setq larg_x2 (/ (-(car coor4) (car coor3)) num_ins2)) 
(setq larg_y2 (/ (-(cadr coor4) (cadr coor3)) num_ins2)) 
;a variavel num recebe o maior nro de inserções do objeto 
(if (> num_ins num_ins2) 
(setq num num_ins) 
) 
(if (< num_ins num_ins2) 
(setq num num_ins2) 
) 
;insere o objeto na coordenada inicial 
(command "_.INSERT" "fusca-azul" pt3aux "1" "1" ang2) 
(setq ent2 (entlast)) 
(command "_.INSERT" "fusca-verde" pt1aux "1" "1" ang) 
(setq ent1 (entlast)) 
;vai para o while 
(while (< 0 num) 
;define a hora atual 
(setq s (getvar "DATE")) 
(setq seconds (* 86400.0 (- s (fix s)))) 
;pausa o sistema 
(setq tempo_aux (+ seconds 0.1)) 
(while (<= seconds tempo_aux) 
(setq s (getvar "DATE")) 
(setq seconds (* 86400.0 (- s (fix s)))) 
) 
;move os objetos 
(if (< 0 num_ins) 
(command "_.move" ent1 "si" pt2aux pt1aux) 
(setq ent1 (entlast)) 
) 
(if (< 0 num_ins2) 
(command "_.move" ent2 "si" pt4aux pt3aux) 
(setq ent2 (entlast)) 
) 
;define -1 as variaveis de contagem 
(setq num (1- num)) 
(setq num_ins (1- num_ins)) 
(setq num_ins2 (1- num_ins2)) 
;calcula as novas coordenadas para mover o objeto 
(setq pt2aux pt1aux) 
(setq pt1aux (list (+(car pt1aux) larg_x) (+(cadr pt1aux) larg_y))) 
(setq pt4aux pt3aux) 
(setq pt3aux (list (+(car pt3aux) larg_x2) (+(cadr pt3aux) larg_y2))) 
;sai do while 
) 
(setvar "CMDECHO" CMD) 
(princ) 
) 


