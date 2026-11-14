;;;Rotina para modificar pontos exportados pelo programa Topograph 
;;;no refere-se à altura dos pontos                                
;;;Escrita por Alex do Carmo a pedido do Sr. Heberson em 04/06/2005
;;;================================================================

;-----------------------Apaga o ponto-----------------------
(defun sbs-ptpn (p / cnt tmp)
   (setq cnt 0 tmp "")
   (while (< cnt (strlen p))
      (setq cnt (1+ cnt))
      (if (= (substr p cnt 1) ".")
         (setq p (strcat tmp "" (substr p  (+ cnt 1)))
            cnt (strlen p)
         )
         (setq tmp (strcat tmp (substr p cnt 1)))
      )
   )
   (setq p p)
)                        ;rotina utilitária3 (variação)
;------------------------------------------------------

;--------------Substitui virgula por ponto-------------
(defun sbs-virpt (p / cnt tmp)
   (setq cnt 0 tmp "")
   (while (< cnt (strlen p))
      (setq cnt (1+ cnt))
      (if (= (substr p cnt 1) ",")
         (setq p (strcat tmp "." (substr p  (+ cnt 1)))
            cnt (strlen p)
         )
         (setq tmp (strcat tmp (substr p cnt 1)))
      )
   )
   (setq p p)
)                        ;rotina utilitária3 (variação)
;------------------------------------------------------

(defun c:tg3d (/ ss1 tmh cnt ent ent1 at att att1 att2 att3 x y z)
  
   (setq 
	 ss1 (ssget (list (cons 2  "SIMB_PONTO,SIMB_BOLINHA,SIMB_BOLINHA,SIMB_QUADRADO,SIMB_ARVORE,SIMB_BARRAS,SIMB_CIRCULO,SIMB_CRUZETA,SIMB_POSTE,SIMB_TRIANGULO,SIMB_XIS")))
         cnt 0
   );setq

  (if ss1
   (progn
    (setq tmh (sslength ss1))
    (repeat  tmh
       (setq ent  (ssname ss1 cnt)
             ent1 (entget ent)
             at ent

	     at   (entnext at)
             att (entget at)
	     att1 (cdr (assoc 1 att))

	     at   (entnext at)
             att  (entget at)
	     att2 (cdr (assoc 1 att))

     	     at   (entnext at)
             att  (entget at)
	     att3 (cdr (assoc 1 att))
  	     
             x    (cadr  (assoc 10 ent1))
             y    (caddr (assoc 10 ent1))
 
	     z (sbs-ptpn  att3)
	     z (sbs-virpt z)
	     z (read z)

	     coord (list 10 x y z) ;coordenadas 3d
	     
             ent1 (subst coord (assoc 10 ent1) ent1)
             cnt  (1+ cnt )

      );setq

      (entmod ent1)
    );repeat


    (if (> tmh 1)
     (princ (strcat "\n" (rtos cnt 2 0) " pontos modificados."))
     (princ (strcat "\n" (rtos cnt 2 0) " ponto modificado.")) 
    );if  (> tmh 1)
    (princ)

    );progn ss1
    (progn
    (Alert "Não foi selecionado nenhum ponto válido...")
    (princ "Não foi selecionado nenhum ponto válido...")
    (princ)
    );progn
   );if ss1
);defun


(princ "\n                                                  Digite tg3d para iniciar... ")