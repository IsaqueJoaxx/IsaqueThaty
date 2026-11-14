;;; Rotina escrita por Eduardo Fernal em 03/01/2005
;;; Uma rotina similar, mais elaborada, está presente no
;;; Curso E.Fernal de AutoLisp, fornecido em cd
;;; http://www.gr-acad.arq.br
;;; http://www.cadklein.com.br
;;; *****************************************************
(DEFUN c:pegapontos (/ arquivo ponto nome cn tipo blipmode)
  ;; globais
  ;;			g::tipo::03_01_2005
  ;;			g::formato::03_01_2005
  (IF (SETQ arquivo (GETFILED "Entre com nome do arquivo .txt"
			      ;;
			      (GETVAR "DWGPREFIX")
			      ;;
			      "TXT"
			      ;;
			      8
		    )
      )
    (PROGN (SETQ nome arquivo
		 cn   0
	   )
	   (IF (NOT g::tipo::03_01_2005)
	     (SETQ g::tipo::03_01_2005 "Novo")
	   )
	   (INITGET "Acrescentar Novo")
	   (SETQ tipo (GETKWORD	;;
				(STRCAT	"\n-> Arquivo para Acréscimo "
					;;
					"ou arquivo Novo? < "
					g::tipo::03_01_2005
					" > : "
				)
		      )
	   )
	   (IF (= tipo nil)
	     (SETQ tipo "Novo")
	   )
	   (SETQ g::tipo::03_01_2005 tipo)
	   (IF (NOT g::formato::03_01_2005)
	     (SETQ g::formato::03_01_2005 "Pontoevírgula")
	   )
	   (INITGET "Lista Pontoevírgula Doispontos")
	   (SETQ formato (GETKWORD (STRCAT
				     "\n-> Formato deve ser Lista, "
				     "Pontoevírgula ou Doispontos ? "
				     "< "
				     g::formato::03_01_2005
				     " > : "
				    )
			 )
	   )
	   (IF (NOT formato)
	     (SETQ formato "Pontoevírgula")
	   )
	   (SETQ g::formato::03_01_2005 formato)
	   (IF (AND nome tipo formato)
	     (PROGN (PRINC "\n")
		    (SETQ arquivo  (OPEN arquivo
					 (COND ((= g::formato::03_01_2005
						   ;;
						   "Novo"
						)
						;;
						"w"
					       )
					       ((= g::formato::03_01_2005
						   ;;
						   "Acréscimo"
						)
						;;
						"a"
					       )
					       (T "w")
					 )
				   )
			  blipmode (GETVAR "BLIPMODE")
		    )
		    (SETVAR "BLIPMODE" 1)
		    (WHILE ;;
			   (SETQ ponto ;;
				       (GETPOINT ;;
						 (STRCAT "\r-> Ponto número "
							 ;;
							 (ITOA (SETQ cn (1+ cn)))
							 " : "
						 )
				       )
			   )
		      (COND ((= formato "Lista") (PRINT ponto arquivo))
			    ((= formato "Pontoevírgula")
			     (PRINC (STRCAT (RTOS (CAR ponto) 2 2)
					    ";"
					    (RTOS (CADR ponto) 2 2)
					    ";"
					    (RTOS (CADDR ponto) 2 2)
					    "\n"
				    )
				    arquivo
			     )
			    )
			    ((= formato "Doispontos")
			     (PRINC (STRCAT (RTOS (CAR ponto) 2 2)
					    ":"
					    (RTOS (CADR ponto) 2 2)
					    ":"
					    (RTOS (CADDR ponto) 2 2
					    "\n"
				    )
				    arquivo
			     )
			    )
		      )
		    )
		    (SETVAR "BLIPMODE" blipmode)
		    (REDRAW)
		    (IF	(AND arquivo (= (TYPE arquivo) 'file))
		      (CLOSE arquivo)
		    )
		    (IF	(FINDFILE nome)
		      (STARTAPP "NOTEPAD.EXE" nome)
		    )
	     )
	     nil
	   )
    )
    (ALERT "Você não forneceu o nome do arquivo a salvar...")
  )
  (PRINC)
)
(PRINC "\n-> Digite Pegapontos e tecle enter para começar...")
(PRINC)