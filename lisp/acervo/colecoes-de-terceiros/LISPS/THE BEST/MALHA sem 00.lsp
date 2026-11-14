(DEFUN C:MALHA00 ()
  (command "ucs" "w")
  (SETQ clayer (GETVAR "CLAYER"))
  (SETQ COSMODE (GETVAR "OSMODE"))
  (SETVAR "OSMODE" 1)
  (SETQ	VETN NIL
	VETE NIL
	VETMAXN	NIL
	VETMAXE	NIL
	VETM NIL
	VETDY NIL
	VETDX NIL
  ) ;_ end of SETQ
  (SETQ	MALHAE NIL
	MALHAN NIL
  ) ;_ end of SETQ
  (SETQ OP (GETINT "\n1 - NE , 2 - XY : "))
  (IF (= OP 1)
    (SETQ ABSC "E=" ORDE "N=" ) ;_ end of SETQ
    (SETQ ABSC "X=" ORDE "Y=" ) ;_ end of SETQ
  ) ;_ end of IF
  
  (SETQ INTERV (GETINT "\nINTERVALO DA MALHA = "))
  (SETQ ALT (GETREAL "\nALTURA DO TEXTO = "))
  (SETQ PT1 (GETPOINT "\nPRIMEIRO VERTICE DA AREA :"))
  (SETQ PT2 (GETPOINT PT1 "\nSEGUNDO VERTICE DA AREA :"))
  (SETQ PT3 (GETPOINT PT2"\nTERCEIRO VERTICE DA AREA :"))
  (SETQ PT4 (GETPOINT PT3"\nQUARTO VERTICE DA AREA :"))
  (SETQ ANGX 90)
  (SETQ ANGY 0)
  (SETQ PTOS (LIST PT1 PT2 PT3 PT4))
  (COMMAND "STYLE" "ROMANS" "ROMANS" ALT "1" "0" "N" "N" "N")
(IF (NOT (TBLSEARCH "LAYER" "MALHA"))
    (COMMAND "_.LAYER"
	     "_MAKE"
	     "MALHA"
	     "_COLOR"
	     "1"
	     "MALHA"
	     ""
	     ) ;_ end of COMMAND
    
    ) ;_ end of IF				
  (IF (NOT (TBLSEARCH "LAYER" "TEXTO-MALHA"))
    (COMMAND "_.LAYER"
	     "_MAKE"
	     "TEXTO-MALHA"
	     "_COLOR"
	     "2"
	     "TEXTO-MALHA"
	     ""
	     ) ;_ end of COMMAND
   
    ) ;_ end of IF				
  (SETVAR "OSMODE" 0)
  (SETQ ESP " ")
  (SETQ I 0)
  (WHILE (/= I 4)
    (SETQ VETN (CONS (CADR (NTH I PTOS)) VETN))
    (SETQ VETE (CONS (CAR (NTH I PTOS)) VETE))
    (SETQ I (+ 1 I))
  ) ;_ end of WHILE
  (SETQ	VETN (REVERSE VETN)
	VETE (REVERSE VETE)
  ) ;_ end of SETQ
  (SETQ I 0)
  (WHILE (/= I 4)
    (IF	(= I 3)
      (SETQ J 0)
      (SETQ J (1+ I))
    ) ;_ end of IF
    (SETQ VETMAXN (CONS (MAX (NTH I VETN) (NTH J VETN)) VETMAXN))
    (SETQ VETMAXE (CONS (MAX (NTH I VETE) (NTH J VETE)) VETMAXE))
    (SETQ I (+ 1 I))
  ) ;_ end of WHILE
  (SETQ VETMAXN (CONS (MAX (NTH 3 VETMAXN) (NTH 1 VETMAXN)) VETMAXN))
  (SETQ VETMAXE (CONS (MAX (NTH 3 VETMAXE) (NTH 1 VETMAXE)) VETMAXE))
  (SETQ	VETMAXN	(REVERSE VETMAXN)
	VETMAXE	(REVERSE VETMAXE)
  ) ;_ end of SETQ
  (SETQ I 0)
  (WHILE (/= I 4)
    (IF	(= I 3)
      (SETQ J 0)
      (SETQ J (1+ I))
    ) ;_ end of IF
    (SETQ VETDY (CONS (- (NTH J VETN) (NTH I VETN)) VETDY))
    (SETQ VETDX (CONS (- (NTH J VETE) (NTH I VETE)) VETDX))
    (SETQ I (+ 1 I))
  ) ;_ end of WHILE
  (SETQ	VETDY (REVERSE VETDY)
	VETDX (REVERSE VETDX)
  ) ;_ end of SETQ
  (SETQ I 0)
  (WHILE (/= I 4)
    (SETQ VARDX (NTH I VETDX))
    (IF	(= VARDX 0.0)
      (SETQ VARDX 0.000001)
    ) ;_ end of IF
					; (SETQ VETM (CONS (/ (NTH I
					; VETDY) (NTH I VETDX))
					; VETM))
    (SETQ VETM (CONS (/ (NTH I VETDY) VARDX) VETM))
    (SETQ I (+ 1 I))
  ) ;_ end of WHILE
  (SETQ VETM (REVERSE VETM))
  (SETQ I 0)
  (WHILE (/= I 4)
    (SETQ IN INTERV)
    (IF	(= I 3)
      (SETQ J 0)
      (SETQ J (1+ I))
    ) ;_ end of IF
    (SETQ FLAG 0)
    (WHILE (= FLAG 0)
      (SETQ X
	     (*	(FIX (/ (+ (MIN (NTH I VETE) (NTH J VETE)) IN) INTERV))
		INTERV
	     ) ;_ end of *
      ) ;_ end of SETQ
      (IF (> X (NTH I VETMAXE))
	(SETQ FLAG 1)
	(PROGN
	  (SETQ	Y
		 (+ (NTH I VETN) (* (NTH I VETM) (- X (NTH I VETE))))
	  ) ;_ end of SETQ
	  (SETQ PT (LIST X Y))
	  (SETQ MALHAE (CONS PT MALHAE))
	  (SETQ IN (+ IN INTERV))
	) ;_ end of PROGN
      ) ;_ end of IF
    ) ;_ end of WHILE
    (SETQ I (+ 1 I))
  ) ;_ end of WHILE
  (SETQ MALHAE (REVERSE MALHAE))
  (SETQ QUANTI (LENGTH MALHAE))
  (SETQ I 0)
  (IF (= ANGX 90)
    (SETQ ACR (* (/ ALT 2) -1))
    (SETQ ACR (/ ALT 2))
  ) ;_ end of IF
  (SETQ CONTAD 1)
  (WHILE (<= CONTAD (/ QUANTI 2))
    (SETQ J (1+ I))
    (SETQ X (CAR (NTH I MALHAE)))
    (WHILE (/= X (CAR (NTH J MALHAE)))
      (SETQ J (+ 1 J))
    ) ;_ end of WHILE
    (SETVAR "CLAYER" "MALHA")
    (COMMAND "LINE" (NTH I MALHAE) (NTH J MALHAE) "")
					; (IF (= ANGX 90) (SETQ V1
					; "R"
					; V2
					; "") (SETQ V1 "" V2
					; "R"))
    (SETQ CY (MAX (CADR (NTH I MALHAE)) (CADR (NTH J MALHAE))))
    (SETQ PX (LIST (+ (CAR (NTH I MALHAE)) ACR) CY))
    (SETVAR "CLAYER" "TEXTO-MALHA")
    (IF	(= ANGX 90)
      (COMMAND "TEXT"
	       "R"
	       PX
	       ANGX
	       (STRCAT ABSC (frmt(CAR (NTH J MALHAE))) ESP)
      ) ;_ end of COMMAND
      (COMMAND "TEXT"
	       PX
	       ANGX
	       (STRCAT ESP ABSC (frmt(CAR (NTH J MALHAE))))
      ) ;_ end of COMMAND
    ) ;_ end of IF
    (SETQ CY (MIN (CADR (NTH I MALHAE)) (CADR (NTH J MALHAE))))
    (SETQ PX (LIST (+ (CAR (NTH I MALHAE)) ACR) CY))
    (IF	(= ANGX 90)
      (COMMAND "TEXT"
	       PX
	       ANGX
	       (STRCAT ESP ABSC (frmt(CAR (NTH J MALHAE))))
      ) ;_ end of COMMAND
      (COMMAND "TEXT"
	       "R"
	       PX
	       ANGX
	       (STRCAT ABSC (frmt(CAR (NTH J MALHAE))) ESP)
      ) ;_ end of COMMAND
    ) ;_ end of IF
    (SETQ AUX1 NIL
	  AUX2 NIL
	  AUX  NIL
    ) ;_ end of SETQ
    (SETQ AUX1 (MEMBER (NTH (1+ I) MALHAE) MALHAE))
    (IF	(/= I 0)
      (PROGN
	(SETQ PAR (NTH (1- I) MALHAE))
	(SETQ AUX (REVERSE MALHAE))
	(SETQ AUX2 (REVERSE (MEMBER PAR AUX)))
	(SETQ MALHAE (APPEND AUX2 AUX1))
      ) ;_ end of PROGN
      (PROGN
	(SETQ MALHAE AUX1)
      ) ;_ end of PROGN
    ) ;_ end of IF
    (SETQ AUX1 NIL
	  AUX2 NIL
	  AUX  NIL
    ) ;_ end of SETQ
    (IF	(= J 1)
      (SETQ MALHAE (MEMBER (NTH J MALHAE) MALHAE))
      (PROGN
	(SETQ J (1- J))
	(IF (< (1+ J) (LENGTH MALHAE))
	  (PROGN
	    (SETQ AUX1 (MEMBER (NTH (1+ J) MALHAE) MALHAE))
	    (SETQ PAR (NTH (1- J) MALHAE))
	    (SETQ AUX (REVERSE MALHAE))
	    (SETQ AUX2 (REVERSE (MEMBER PAR AUX)))
	    (SETQ MALHAE (APPEND AUX2 AUX1))
	  ) ;_ end of PROGN
	) ;_ end of IF
      ) ;_ end of PROGN
    ) ;_ end of IF
    (SETQ CONTAD (1+ CONTAD))
  ) ;_ end of WHILE
  (SETQ I 0)
  (WHILE (/= I 4)
    (SETQ IN INTERV)
    (IF	(= I 3)
      (SETQ J 0)
      (SETQ J (1+ I))
    ) ;_ end of IF
    (SETQ FLAG 0)
    (WHILE (= FLAG 0)
      (SETQ Y
	     (*	(FIX (/ (+ (MIN (NTH I VETN) (NTH J VETN)) IN) INTERV))
		INTERV
	     ) ;_ end of *
      ) ;_ end of SETQ
      (IF (> Y (NTH I VETMAXN))
	(SETQ FLAG 1)
	(PROGN
	  (SETQ	X
		 (/ (+ (- (* (NTH I VETM) (NTH I VETE)) (NTH I VETN))
		       Y
		    ) ;_ end of +
		    (NTH I VETM)
		 ) ;_ end of /
	  ) ;_ end of SETQ
	  (SETQ PT (LIST X Y))
	  (SETQ MALHAN (CONS PT MALHAN))
	  (SETQ IN (+ IN INTERV))
	) ;_ end of PROGN
      ) ;_ end of IF
    ) ;_ end of WHILE
    (SETQ I (+ 1 I))
  ) ;_ end of WHILE
  (SETQ MALHAN (REVERSE MALHAN))
  (SETQ VERI MALHAN)
  (SETQ QUANTI (LENGTH MALHAN))
  (SETQ I 0)
  (IF (= ANGY 180)
    (SETQ ACR (* (/ ALT 2) -1))
    (SETQ ACR (/ ALT 2))
  ) ;_ end of IF
  (SETQ CONTAD 1)
  (WHILE (<= CONTAD (/ QUANTI 2))
    (SETQ J (1+ I))
    (SETQ Y (CADR (NTH I MALHAN)))
    (WHILE (/= Y (CADR (NTH J MALHAN)))
      (SETQ J (+ 1 J))
    ) ;_ end of WHILE
    (SETVAR "CLAYER" "MALHA")
    (COMMAND "LINE" (NTH I MALHAN) (NTH J MALHAN) "")
    (SETQ CX (MAX (CAR (NTH I MALHAN)) (CAR (NTH J MALHAN))))
    (SETQ PY (LIST CX (+ (CADR (NTH I MALHAN)) ACR)))
(SETVAR "CLAYER" "TEXTO-MALHA")					
    (IF	(= ANGY 180)
      (COMMAND "TEXT"
	       PY
	       ANGY
	       (strcat ESP ORDE (frmt(CADR (NTH J MALHAN))))
      ) ;_ end of COMMAND
      (COMMAND "TEXT"
	       "R"
	       PY
	       ANGY
	       (strcat ORDE (frmt(CADR (NTH J MALHAN))) ESP)
      ) ;_ end of COMMAND
    ) ;_ end of IF
    (SETQ CX (MIN (CAR (NTH I MALHAN)) (CAR (NTH J MALHAN))))
    (SETQ PY (LIST CX (+ (CADR (NTH I MALHAN)) ACR)))
					; (COMMAND "TEXT" PY ANGY
					; (strcat
					; ORDE (rtos (CADR(NTH J
					; MALHAN))
					; 2
					; 0)))
    (IF	(= ANGY 180)
      (COMMAND "TEXT"
	       "R"
	       PY
	       ANGY
	       (strcat ORDE (frmt(CADR (NTH J MALHAN))) ESP)
      ) ;_ end of COMMAND
      (COMMAND "TEXT"
	       PY
	       ANGY
	       (strcat ESP ORDE (frmt(CADR (NTH J MALHAN))))
      ) ;_ end of COMMAND
    ) ;_ end of IF
    (SETQ AUX1 NIL
	  AUX2 NIL
	  AUX  NIL
    ) ;_ end of SETQ
    (SETQ AUX1 (MEMBER (NTH (1+ I) MALHAN) MALHAN))
    (IF	(/= I 0)
      (PROGN
	(SETQ PAR (NTH (1- I) MALHAN))
	(SETQ AUX (REVERSE MALHAN))
	(SETQ AUX2 (REVERSE (MEMBER PAR AUX)))
	(SETQ MALHAN (APPEND AUX2 AUX1))
      ) ;_ end of PROGN
      (PROGN
	(SETQ MALHAN AUX1)
      ) ;_ end of PROGN
    ) ;_ end of IF
    (SETQ AUX1 NIL
	  AUX2 NIL
	  AUX  NIL
    ) ;_ end of SETQ
    (IF	(= J 1)
      (SETQ MALHAN (MEMBER (NTH J MALHAN) MALHAN))
      (PROGN
	(SETQ J (1- J))
	(IF (< (1+ J) (LENGTH MALHAN))
	  (PROGN
	    (SETQ AUX1 (MEMBER (NTH (1+ J) MALHAN) MALHAN))
	    (SETQ PAR (NTH (1- J) MALHAN))
	    (SETQ AUX (REVERSE MALHAN))
	    (SETQ AUX2 (REVERSE (MEMBER PAR AUX)))
	    (SETQ MALHAN (APPEND AUX2 AUX1))
	  ) ;_ end of PROGN
	) ;_ end of IF
      ) ;_ end of PROGN
    ) ;_ end of IF
    (SETQ CONTAD (1+ CONTAD))
  ) ;_ end of WHILE
  (SETVAR "CLAYER" clayer)
  (SETVAR "OSMODE" COSMODE)
) ;_ end of DEFUN
 ;|«Visual LISP© Format Options»
(
2 2 40 2 T "end of " 60 9 0 0 2 T T T T)
;*** DO NOT add text below the comment! ***|;



;;;=======================================================================

(defun frmt (n)
  (setq
  	n1  (rtos n 2 0)           ;numero (string)
	cm  (strlen n1)	           ;n de caracteres
	nl  (fix (/ cm 3))         ;n de loop + 1
	1pt (- cm (* 3 nl))        ;posicao do ponto
	2pt (+ 1 1pt)              ;posicao do ponto
	tmp (substr  n1 1 1pt)     ;temp
   );setq

  (if (= 1pt 0)                     ;testa a posicao do 1pt...
        (setq 1pt 1)                ;se sim seta em 1
        (setq tmp (strcat tmp ".")) ;se nao anexa o ponto a tmp
  );if

  (repeat (- nl 1)                        ;repete (nl - 1) vezes
    (setq         
          tmp (strcat tmp (substr  n1 2pt 3))
	  tmp (strcat tmp  ".")
          2pt (+ 2pt 3)
     );setq
  );repeat

  (setq tmp (strcat tmp (substr  n1 2pt 3) ));

);defun

;;;=======================================================================
