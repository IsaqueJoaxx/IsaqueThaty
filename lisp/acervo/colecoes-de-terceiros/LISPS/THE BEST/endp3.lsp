(defun c:endPoint(/ listaPontos p1) 
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
  (setq listaPontos nil) 
  (prompt "\nSelecione o Elemento a Ser Copiado:") 
   (setq #elemento (entsel)) 
  (if #elemento 
    (progn 
      (setq #name (car #elemento)) 
      (SETQ ent#name (entget #name)) 
      (SETQ coordenada (cdr(assoc 10 ent#name))) 

         (if coordenada 
                (progn 
                     (setq contador 0) 
                           (while (/= contador nil) 
                            (setq p1 (getpoint "\nIndique Ponto para Inserção ou <enter> para Sair !")) 
                            
                            (if (or(= p1 nil)(= p1 "")) 
                              (progn 
                                     (setq contador nil) 
                               );progn 
                               (progn 
                                      (setq listaPontos (cons p1 listaPontos)) 
                                      (princ (strcat "\rFora(m) inserido(s) < "(itoa (1+ contador)) " >Coordenada(s) na Lista")) 
                                    (setq contador (1+ contador)) 
                               );progn 
                            );if 
                            );while 
                    
                );progn 
          );if 

               (if listaPontos 
                (progn 
                      (setq copia 0) 
                     (setq contador (-(length listaPontos)1)) 
                           (while (>= contador 0) 
                            (setq p1 (nth contador listaPontos)) 
                            (command "copy" #name "" coordenada p1 ) 
                            (princ (strcat "\rFora(m) inserido(s) < "(itoa (1+ copia)) " > Elemento(s) no Desenho!")) 
                             (setq contador (- contador 1)) 
                            (setq copia (1+ copia)) 
                            );while 
                    
                );progn 
          );if 
      
    );progn 
    (progn 
     (Alert "Nenhum Elemento Selecionando!") 
     (errovao) 
    );progn 
  );if 
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