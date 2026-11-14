defun c:vao() 
  (SETQ attreq (GETVAR "ATTREQ") 
       attdia (GETVAR "ATTDIA") 
       osmode (GETVAR "OSMODE") 
        eco    (GETVAR "CMDECHO") 
        blip   (GETVAR "BLIPMODE") 
  ) 
  (SETQ erro_original *error*) 
  (SETVAR "cmdecho" 0) 
  (SETVAR "blipmode" 0) 
  (SETVAR "attdia" 0) 
  (setvar "attreq" 1) 
  (setvar "filedia" 1) 
  (SETQ *error* errovao) 

  (setq distVao (getdist  "\nEntre com a Distância do vão: ")) 
  (if (or (= distVao "")(= distVao nil)) 
  (errovao)) 

  (setq P1 (getpoint "\nIndique o Inicio: ")) 
  (princ "\n") 

  (if (= P1 nil) 
  (errovao)) 
  
  (setq P2 (getpoint P1 "\nIndique o Fim: ")) 
  (if (= p2 nil) 
  (errovao)) 

  (setq pInsert nil) 
  (if (and(/= P2 nil)(/= P1 P2)(/= P1 nil)) 
         (progn 
                 (setq angulo  (rem (angle P1 P2) pi));; quadrantes 1 e 3 - calcula o ângulo 
              (if (> angulo (/ pi 2)) 
              (setq angulo (+ pi angulo)));; quadrantes 2 e 4 - calcula o ângulo 
              (SETQ capturarotacao (/ (* angulo 180) PI));; transforma em Grau 
               (setq distancia (distance P1 P2)) 
                (setq qtdePoint (/ distancia distVao)) 
               (setq controlador (atoi(rtos qtdePoint 2 0))) 

                (if (<= controlador 0) 
                (errovao) 
              );if 
          
              (setq contador 0) 
                    (while (> controlador 0) 
                     (if (/= pInsert nil) 
                     (setq pInsert (polar p1 angulo distVao)) 
                     (setq pInsert p1)) 
                     (setq p1 pInsert) 
                     (command "POINT" p1) 
                     (princ (strcat "\rInstalados "(itoa (1+ contador)) " de "(itoa(atoi(rtos qtdePoint 2 0))) " Pontos ...")) 
                     (setq controlador (- controlador 1)) 
                      (setq contador (1+ contador)) 
                     );while 
              
         );progn 
    );if 
   (princ"\nExecutado com Sucesso!") 
   (princ) 
    
  );defun 

(DEFUN errovao () 
  (PRINC "\n") 
  (PRINC "Comando Cancelado!") 
  (command "layer" "m" "0" "") 
  (COMMAND "regen") 
  (SETVAR "cmdecho" eco) 
  (SETVAR "blipmode" blip) 
  (SETQ *error* erro_original) 
  (PRINC) 
)