(vmon)
					;PROGRAMA PARA CONCATENAR TEXTOS - HEBERSON OUTUBRO DE 2004
(prompt "Digite intext e 'enter' para executar programa")
(DEFUN C:intext (/ SEL NSEL IND NOME L TIPO SLT TX NTX NTX1 NSLT)
  (setq
    var	(getstring "\Entre com o texto a ser incrementado:"
	)
  )
  (prompt "Selecione os TEXTOS a serem CONCATENADOS")
  (SETQ SEL (SSGET))
	
 	 (SETQ NSEL (SSLENGTH SEL))
 	 (SETQ IND 0)
 	 (WHILE (< IND NSEL)
	    (SETQ NOME (SSNAME SEL IND))
 	   (SETQ L (ENTGET NOME))
 	   (SETQ TIPO (CdR (ASSOC 0 L)))
 	   (PROGn
 	      (SETQ SLT (ASSOC 1 L))
	      (SETQ TX  (CDR SLT))
	      (SETQ NTX (STRCAT VAR TX))	
	      (SETQ NSLT (CONS 1 NTX))
	      (SETQ L (SUBST NSLT SLT L))
	      (ENTMOD L)
 	   )
	    (SETQ IND (+ IND 1))
	  )

  
  (PRompT "HEBERSON - 2004 --  ")
  (ALERT (STRCAT ""
		 (ITOA NSEL)
		 "  ITEN(s) ALTERADO(s) COM SUCESSO  !! "
	 )

  )
)