;
; Arquivo: MALHA.LSP
;
; Esta rotina cria uma malha sobre as curvas de nível .
; Considerações: - as curvas deverão estar cada uma em sua respectiva altura e numa vista de topo
;                           - será gerada uma malha poligonal com uso de 3DMESH no layer malha 
;                           - para visualizar o relevo use ddvpoint ou outro comando de visualização
;
;
;
;setagens iniciais
(setq colorold (getvar "cecolor"))
(setq layerold (getvar "clayer"))
(setq apertold (getvar "aperture"))



(setq W (getdist "\nEntre com largura de cada unidade da malha:  "))
(setq P1 (getpoint "\nPick um ponto retangular da inicio da malha:  "))
(setq P3 (getpoint "\nPick ponto oposto da malha:  "))
(setq P2 (list (car P1) (cadr P3)))
(setq P4 (list (car P3) (cadr P1)))
(setq dx (distance p1 p4))
(setq dy (distance p1 p2))
(setq nx (fix (/ dx w)))
(setq ny (fix (/ dy w)))
(command  "layer" "n" "malha" "s" "malha" "")
(command  "pline" p1 p2 p3 p4  "c" )
(command "osnap" "nea" )
(command "aperture" "10" )

; desenha linhas em X para interpolação
(setq x1 0)
(setq y1 0)
(setq pt p1)
(while  (< x1 nx)
   (progn 
     (setq px pt)
     (setq px (list  (+ (car px) w)  (cadr px)))
     (setq px1 px)
     (setq pt (list  (+ (car pt) w)  (cadr pt)))
     (setq x1 (+ x1 1))
     (setq y1 0)
     (while (< y1 ny)
        (progn 
           (setq temp px1)
           (setq px1 (list (car px1) (+ (cadr px1) w)))
           (setq px1 temp)
           (command "line" px px1 "" )
           (setq y1 (+ y1 1))
         )
       )
    )
)
(command "redraw")

; desenha linhas em Y para interpolação
(command "aperture" "10")
(setq x1 0)
(setq y1 0)
(setq pt p1)
(while  (< y1 ny)
   (progn 
     (setq py pt)
     (setq py (list (car py) (+ (cadr py) w) ))
     (setq py1 py)
     (setq pt (list (car pt) (+ (cadr pt) w) ))
     (setq y1 (+ y1 1))
     (setq x1 0)
     (while (< x1 nx)
        (progn 
           (setq temp py1)
           (setq py1 (list  (+ (car py1) w) (cadr py1)))
           (setq py1 temp )
           (command "line" py py1 "" )
           (setq x1 (+ x1 1))
         )
       )
    )
)
(command "redraw")



; determina os pontos de recobrimento
(command "aperture" "10")
(command "color" "4")
(command "layer" "n" "malha" "s" "malha" "")
(setq malhafile (open "malha.scr" "w"))
(write-line "3dmesh" malhafile)
(write-line (rtos ny 2 0) malhafile)
(write-line (rtos nx 2 0) malhafile)
(setq x1 0)
(setq y1 0)
(setq pt (list (- (car p1) w) (- (cadr p1) w) (caddr p)))
(while (< y1 ny)
    (progn
      (setq py pt)
      (setq py (list (car py) (+ (cadr py) w) ))
      (setq py1 py)
      (setq pt (list (car pt) (+ (cadr pt) w) ))
      (setq y1 (+ y1 1))
      (setq x1 0)
      (while (< x1 nx)
        (progn
           (setq temp py1)
           (setq py1(list (+ (car py1) w) (cadr py1)))
           (setq py temp) 
           (command "ID" py1)
           (setq ponto (getvar "lastpoint"))
            (write-line (strcat (rtos (car py1)) "," (rtos (cadr py1)) "," (rtos (caddr ponto))  )
malhafile)
           (setq x1 (+ x1 1 ))
        )
     )
   )
)
(write-line (strcat "color " colorold) malhafile)
(write-line (strcat "layer s " layerold " ") malhafile)
(write-line (strcat "aperture " (rtos apertold 2 0)) malhafile)
(close malhafile)
(command "osnap" "")
(setq sset (ssget "x" (list (cons 8 "malha"))))
(command "erase" sset "")
(command "script" "malha.scr") 







