(defun MB ()
(SETQ RRR "S")
(SETQ DDD "S")  
  
 ; (SETQ RRR (GETSTRING "\n DIGITE (N) SE NÃO QUISER ROTACIONAR OS BLOCOS "))
  ; (IF (and (/= RRR "N") (/= RRR "n"))
  ;   (setq rrr "s")
  ; )  
;(SETQ DDD (GETSTRING "\n DIGITE (N) SE NÃO QUISER DESLOCAR AS COTAS "))
;   (IF (and (/= DDD "N") (/= DDD "n"))
;     (PROGN
;     (setq DDD "s")
;      (SETQ DELTA_X (GETREAL "\nINFORME O DESLOCAMENTO EM X")) 
;     (SETQ DELTA_Y (GETREAL "\nINFORME O DESLOCAMENTO EM Y")) 
;   )
;   )  
  
;(COMMAND "UCS" "R" "FORMATO")
(defun rotaciona (Lista / Pto_Base Angulo_Old Pto_Aux Pto_Aux1 Pto_Aux2 Pto_Aux3)
 ; (COMMAND "UCS" "R" "FORMATO")
  (setq Pto_Base (list ( nth 1 (assoc 10 lista)) ( nth 2 (assoc 10 lista))))
  (setq Angulo_Old (cdr (assoc 50 lista)))
  (setq Pto_Aux (polar Pto_base Angulo_Old 10.0))
  (setq Pto_Aux1 (trans Pto_Base 1 0))
  (setq Pto_Aux2 (trans Pto_Aux 1 0))
  (setq Angulo_New (Angle Pto_Aux1 Pto_Aux2))
  ;(COMMAND "UCS" "w")
  )

  (defun DESLOCAMENTO (Lista / Pto_Base Angulo_Old Pto_Aux Pto_Aux1 Pto_Aux2 Pto_Aux3)
  ;(COMMAND "UCS" "R" "FORMATO")
  (setq Pto_Base (list ( nth 1 (assoc 10 lista)) ( nth 2 (assoc 10 lista))))
  (setq Pto_NEW (trans Pto_Base 0 1))
  (setq Pto_New (list (+ ( nth 0 Pto_New) Delta_X) (+ ( nth 1 Pto_New) Delta_Y)))
    (setq Pto_NEW (trans Pto_New 1 0))
   ;(COMMAND "UCS" "w")
  )


  
  
 (SETQ NOMEB (CAR (ENTSEL "\nSELECIONE O BLOCO"))
       NOMEB (CDR (ASSOC 2 (ENTGET  NOMEB)))
 )

 (SETQ ALTURA (GETREAL "\nINFORME O TAMANHO DO TEXTO: ")) 
;(SETQ FSC (GETREAL "\nINFORME O FATOR DE SCALA: ")) 

  (setq elem (ssget "x" '((2 . "simb*"))))
  (setq	ssl	 (sslength elem)
	tot_cont (- (sslength elem) 1)
  )
  (repeat ssl
    (setq temp (ssname elem tot_cont))
    (setq tempB (ssname elem tot_cont))
    
    (setq lista1 (entget TEMP))

;(if (OR (= RRR "S") (= RRR "s"))
;     (rotaciona lista1)
;)    
    (setq listaB (entget TEMP))
    (SETQ CONDBB (CDR (ASSOC 2 LISTA1)))
    (SETQ CONDBBp (CDR (ASSOC 8 LISTA1)))
        (IF	(= CONDBB NOMEB)
      (PROGN
	(SETQ LISTA (ASSOC 8 LISTA1))
	(setq lista1
	       (subst (cons 8 CONDBB)
		      lista
		      lista1
	       )
	)

(SETQ LISTA (ASSOC 62 LISTA1))
	(setq lista1
	       (subst (cons 62 256)
		      lista
		      lista1
	       )
	)

(SETQ LISTA (ASSOC 41 LISTA1))
	(setq lista1
	       (subst (cons 41 fsc)
		      lista
		      lista1
	       )
	)

(SETQ LISTA (ASSOC 42 LISTA1))
	(setq lista1
	       (subst (cons 42 fsc)
		      lista
		      lista1
	       )
	)

;	(if (OR (= RRR "S") (= RRR "s"))
;                  (PROGN
	;(SETQ LISTA (ASSOC 50 LISTA1))
	;(setq lista1
	 ;      (subst (cons 50 Angulo_New)
	;	      lista
	;	      lista1
	 ;      )
;	)
;	(entmod lista1)
 ;             )
;	  )
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;	(setq nb (cdr (assoc 2 listaB)))
;	(setq el (tblsearch "block" nb))
;	(setq en (cdr (assoc -2 el)))
	(REPEAT 4
;(COMMAND "UCS" "r" "formato")
	  (setq el (entget TEMPB))
;(if (OR (= RRR "S") (= RRR "s"))
 ;              
;	  (rotaciona el)
;)	  
	  (setq TOBJ (cdr (assoc 0 el)))
           (IF (= TOBJ NIL)
	     (SETQ TEMPB TOBJ)
	   ) 

	  (if (= TOBJ "ATTRIB")
	    (progn
	      (setq TTEXTO (cdr (assoc 2 EL)))

              (if (= condbbp "_POLIGONAL")
		(SETQ LAYERF (STRCAT "TOPO-" "POLIGONAL" TTEXTO))
               (SETQ LAYERF (STRCAT "TOPO-" TTEXTO))
		)

		 (SETQ LISTA (ASSOC 8 EL))
		 (setq EL
			(subst (cons 8 LAYERF)
			       lista
			       EL
			)
		 )
		 (entmod EL)

		 (SETQ LISTA (ASSOC 62 EL))
		 (setq EL
			(subst (cons 62 256)
			       lista
			       EL
			)
		 )

;	      (if (OR (= RRR "S") (= RRR "s"))
 ;                 (PROGN
		    
;		 (SETQ LISTA (ASSOC 50 EL))
;		 (setq EL
		;(subst (cons 50 (* 2 PI) )
;		(subst (cons 50 Angulo_New )
;			       lista
;			       EL
;			)
;		 )
	      		 
;		 (entmod EL)
 ;                 )
;		)

;(if (OR (= DDD "S") (= DDD "s"))
 ;                 (PROGN
;		    (DESLOCAMENTO EL)
;		     (COMMAND "UCS" "w")
;		 (SETQ LISTA (ASSOC 10 EL))
;		 (setq EL
;		  (subst (cons 10 Pto_New )
;			       lista
;			       EL
;			)
;		 )
;	      		 
;		 (entmod EL)
 ;                 )
;		)
	      

	      (SETQ LISTA (ASSOC 70 EL))
		 (setq EL
			(subst (cons 70 8)
			       lista
			       EL
			)
		 )
		 (entmod EL)

	      
		 (SETQ LISTA (ASSOC 40 EL))
		 (setq EL
			(subst (cons 40 ALTURA)
			       lista
			       EL
			)
		 )
		 (entmod EL)
		 (SETQ LISTA (ASSOC 41 EL))
		 (setq EL
			(subst (cons 41 1.0)
			       lista
			       EL
			)
		 )

	     	    
	       (entmod EL)
		
	    (if ( = nomeb "SIMB_ARVORE")
                (progn
 (SETQ LISTA (ASSOC 70 EL))
          (setq EL
	     (subst	(cons 70 9)
		lista
		EL
	     )
          )


 (SETQ LISTA (ASSOC 41 EL))
		 (setq EL
			(subst (cons 41 0.4)
			       lista
			       EL
			)
		 )

(SETQ LISTA (ASSOC 42 EL))
		 (setq EL
			(subst (cons 42 0.4)
			       lista
			       EL
			)
		 )

          (entmod EL)
           )
         )		    


	      
	    )
	  )
	  (SETQ TEMPB (ENTNEXT TEMPB))
	)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;	
      )
    )
    (SETQ TOT_CONT (1- TOT_CONT))
    (SETQ PERC (/ (FLOAT (* TOT_CONT 100)) SSL))
    (SETQ PERCE (STRCAT (RTOS PERC 2 0) "%"))
    (prompt (strcat "\n\n" PERCE "\n"))
    (princ)
  )
  (prompt
    (strcat "\n\n" "FORAM ALTERADOS " (itoa SSL) " BLOCOS" "\n")
  )

(command "regen")
  (PRINC)
)

