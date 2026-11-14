;;---------------------------------------------------------------------------------------;;
;;---------------------Coordenadas em (Este, Norte,Cota e Texto)-------------------------;;
;;---------------------   by Yvombek Rebello, 29/março/2004     -------------------------;;
;;---------------------------------------------------------------------------------------;;

(defun C:COORD (/ PNT1 P1X P1Y P STDX STDY XCOORD YCOORD PTXT)
  (setq	olderr	*error*
	*error*	myerror
  ) ;_ end of setq
  (command "_.UNDO" "_GROUP")
  (setq ocmd (getvar "cmdecho"))
  (setq oblp (getvar "blipmode"))
  (setq olay (getvar "clayer"))
  (setvar "ORTHOMODE" 1)
  (setvar "cecolor" "BYLAYER")
  (command "-layer" "m" "COORD_TXT" "C" "" "" "")

(PROGN
    (INITGET "1 2 3 4 5")
    (SETQ FOR
	   (GETPOINT
	     "\nQual o formato: 1(Nome-N-E-Z), 2(N-E-Z), 3(N-E), 4(NOME-Z), 5(Z). "
	   )
    )
    (IF	(= FOR "1")
      (PROGN
  	(while (/= FOR nil)
  	(setq	PNT1 (getpoint
	       	"\nSelecione o ponto: "
	     	)
 	 )
  	(defun sm-WORLD (PNT1 fr to)
    	(setq PNT1 (trans PNT1 fr to))
    	(setq P1Y (cadr PNT1))
    	(setq P1X (car PNT1))
    	(setq P1Z (caddr PNT1))
    	(setq P (getvar "LUPREC"))
    	(setq STDY (rtos P1Y 2 3))
	(setq STDX (rtos P1X 2 3))
    	(setq STDZ (rtos P1Z 2 3))
    	(setq YCOORD (strcat "N=" STDY))
	(setq XCOORD (strcat "E=" STDX))
    	    	(setq ZCOORD (strcat "Z=" STDZ))
  	)
  	(setq th1 (getstring T"\nQual o texto :"))
  	(setq th3 (strcat th1 ))
  	(strcat "World   - " (sm-WORLD PNT1 1 0))
  	(setq	PTXT (getpoint
	       "\nInsira o Local do Texto: "
	     )
  	)
	(command "layer" "m" "COORD_TXT" "")
  	(command "LEADER" PNT1 PTXT "" th3 YCOORD XCOORD ZCOORD "")
  	(command "SETVAR" "osmode" oldsnap)
  	(command "_.UNDO" "_END")
  	(princ)
    	)
      )
    )
    (IF	(= FOR "2")
      (PROGN
  	(while (/= FOR nil)
  	(setq	PNT1 (getpoint
	       	"\nSelecione o ponto: "
	     	)
 	 )
  	(defun sm-WORLD (PNT1 fr to)
   	(setq PNT1 (trans PNT1 fr to))
    	(setq P1Y (cadr PNT1))
    	(setq P1X (car PNT1))
    	(setq P1Z (caddr PNT1))
    	(setq P (getvar "LUPREC"))
    	(setq STDY (rtos P1Y 2 3))
    	(setq STDX (rtos P1X 2 3))
    	(setq STDZ (rtos P1Z 2 3))
    	(setq YCOORD (strcat "N=" STDY))
    	(setq XCOORD (strcat "E=" STDX))
    	(setq ZCOORD (strcat "Z=" STDZ))
  	)
  	(strcat "World   - " (sm-WORLD PNT1 1 0))
  	(setq	PTXT (getpoint
	       "\nInsira o Local do Texto: "
	     )
  	)
	(command "layer" "m" "COORD_TXT" "")
  	(command "LEADER" PNT1 PTXT "" YCOORD XCOORD ZCOORD "")
  	(command "SETVAR" "osmode" oldsnap)
  	(command "_.UNDO" "_END")
  	(princ)
    	)
      )
    )
    (IF	(= FOR "3")
      (PROGN
	(while (/= FOR nil)
  	(setq	PNT1 (getpoint
	       	"\nSelecione o ponto: "
	     	)
 	 )
  	(defun sm-WORLD (PNT1 fr to)
    	(setq PNT1 (trans PNT1 fr to))
    	(setq P1Y (cadr PNT1))
    	(setq P1X (car PNT1))
    	(setq P (getvar "LUPREC"))
    	(setq STDY (rtos P1Y 2 3))
    	(setq STDX (rtos P1X 2 3))
    	(setq YCOORD (strcat "N=" STDY))
    	(setq XCOORD (strcat "E=" STDX))
   	)

  	(strcat "World   - " (sm-WORLD PNT1 1 0))
  	(setq	PTXT (getpoint
	       "\nInsira o Local do Texto: "
	     )
  	)
	(command "layer" "m" "COORD_TXT" "")
  	(command "LEADER" PNT1 PTXT "" YCOORD XCOORD "")
  	(command "SETVAR" "osmode" oldsnap)
  	(command "_.UNDO" "_END")
  	(princ)
    	)
      )
    )
    (IF	(= FOR "4")
      (PROGN
	(while (/= FOR nil)
  	(setq	PNT1 (getpoint
	       	"\nSelecione o ponto: "
	     	)
 	 )
  	(defun sm-WORLD (PNT1 fr to)
    	(setq PNT1 (trans PNT1 fr to))
     	(setq P1Z (caddr PNT1))
    	(setq P (getvar "LUPREC"))
    	(setq STDZ (rtos P1Z 2 3))
    	(setq ZCOORD (strcat "Cota=" STDZ))
  	)
	(setq th1 (getstring T"\nQual o texto :"))
  	(setq th3 (strcat th1 ))
  	(strcat "World   - " (sm-WORLD PNT1 1 0))
  	(setq	PTXT (getpoint
	       "\nInsira o Local do Texto: "
	     )
  	)
	(command "layer" "m" "COORD_TXT" "")
  	(command "LEADER" PNT1 PTXT "" th3 ZCOORD "")
  	(command "SETVAR" "osmode" oldsnap)
  	(command "_.UNDO" "_END")
  	(princ)
    	)
      )
    )
      (IF	(= FOR "5")
      (PROGN
	(while (/= FOR nil)
  	(setq	PNT1 (getpoint
	       	"\nSelecione o ponto: "
	     	)
 	 )
  	(defun sm-WORLD (PNT1 fr to)
    	(setq PNT1 (trans PNT1 fr to))
     	(setq P1Z (caddr PNT1))
    	(setq P (getvar "LUPREC"))
    	(setq STDZ (rtos P1Z 2 3))
    	(setq ZCOORD (strcat "Cota=" STDZ))
  	)
  	(strcat "World   - " (sm-WORLD PNT1 1 0))
  	(setq	PTXT (getpoint
	       "\nInsira o Local do Texto: "
	     )
  	)
	(command "layer" "m" "COORD_TXT" "")
  	(command "LEADER" PNT1 PTXT "" ZCOORD "")
  	(command "SETVAR" "osmode" oldsnap)
  	(command "_.UNDO" "_END")
  	(princ)
    	)
      )
    )

  )
)

;########################################################################################
