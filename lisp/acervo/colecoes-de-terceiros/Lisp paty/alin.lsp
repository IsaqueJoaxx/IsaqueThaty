 ;================================================================================
 ;         Rotina para alinhar textos e blocos e linhas a um alinhamento definido 
 ;================================================================================
(DEFUN c:alin ()
    (SETVAR "cmdecho" 0)
    (DEFUN *error* (msg)
	(COMMAND "_.ucs" "_p")
	(COMMAND "_.undo" "_end")
	(PRINC)
    ) ;_  Fim do DEFUN
    (COMMAND "_.undo" "_group")
    (COMMAND "_.ucs" "")
    (PRINC "\n *** Selecione blocos, textos e linhas para alinhar")
    (SETQ selecao (SSGET))
    (IF	selecao
	(PROGN
	    (SETQ tam	 (SSLENGTH selecao)
		  cont	 0
		  angulo 5000
		  linha	 (ENTSEL "\n Clique no objeto para alinhar os textos")
		  pSel	 (CADR linha)
		  ent	 (ENTGET (CAR linha))
	    ) ;_  Fim do setq
	    (IF	(= (CDR (ASSOC 0 ent)) "TEXT")
		(SETQ angulo (CDR (ASSOC 50 ent)))
	    ) ;_  Fim do IF
	    (IF	(= (CDR (ASSOC 0 ent)) "LINE")
		(PROGN
		    (SETQ p1	 (CDR (ASSOC 10 ent))
			  p2	 (CDR (ASSOC 11 ent))
			  angulo (ANGLE p1 p2)
		    ) ;_  Fim do setq
		) ;_  Fim do PROGN
	    ) ;_  Fim do IF
	    (IF	(= (CDR (ASSOC 0 ent)) "INSERT")
		(SETQ angulo (CDR (ASSOC 50 ent)))
	    ) ;_  Fim do IF
	    (IF	(= (CDR (ASSOC 0 ent)) "LWPOLYLINE")
		(PROGN
		    (SETQ ptm	 (OSNAP pSel "mid")
			  ptf	 (OSNAP pSel "end")
			  angulo (ANGLE ptm ptf)
		    ) ;_  Fim do setq
		) ;_  Fim do PROGN
	    ) ;_  Fim do IF
	    (IF	(= angulo 5000)
		(PROGN
		    (PRINC "\n   Seleção para alinhamento não é válida!!!!")
		    (EXIT)
		) ;_  Fim do progn
	    ) ;_  Fim do if
	    (REPEAT tam
		(SETQ nome (SSNAME selecao cont)
		      ent  (ENTGET nome)
		) ;_  Fim do setq
		(IF (= (CDR (ASSOC 0 ent)) "TEXT")
		    (PROGN
			(IF (AND (> angulo 1.5707963268) (< angulo 4.712388904))
			    (SETQ angulo (+ angulo 3.1415926536))
			) ;_  Fim do IF
			(SETQ ent (SUBST (CONS 50 angulo) (ASSOC 50 ent) ent))
		    ) ;_  Fim do IF
		) ;_  Fim do IF
		(IF (= (CDR (ASSOC 0 ent)) "INSERT")
		    (SETQ ent (SUBST (CONS 50 angulo) (ASSOC 50 ent) ent))
		) ;_  Fim do IF
		(IF (= (CDR (ASSOC 0 ent)) "LINE")
		    (PROGN
			(SETQ p1   (CDR (ASSOC 10 ent))
			      p2   (CDR (ASSOC 11 ent))
			      dist (DISTANCE p1 p2)
			      pt2  (POLAR p1 angulo dist)
			) ;_  Fim do setq
			(SETQ ent (SUBST (CONS 11 pt2) (ASSOC 11 ent) ent))
		    ) ;_  Fim do progn
		) ;_  Fim do IF
		(SETQ cont (+ 1 cont))
		(ENTMOD ent)
		(ENTUPD nome)
	    ) ;_  Fim do REPEAT
	) ;_  Fim do PROGN
	(PRINC "\n  Nada foi selecionado - Função cancelada")
    ) ;_  Fim do if
    (COMMAND "_.ucs" "_p")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;================================================================================
 ;================================================================================
