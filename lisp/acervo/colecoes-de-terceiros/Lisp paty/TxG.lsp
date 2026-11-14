(defun c:TxG (/ n SecGrp LenGrp EntGrp EntClr LayNam EntNly)
  (command "UNDO" "BEGIN")
  (setvar "HPSEPARATE" 0)
  (setvar "CMDECHO" 0)
  (if (< (setq OsmVar (getvar "OSMODE")) 16384)
    (setvar "OSMODE" (+ 16384 (getvar "OSMODE")))
  )
  (setq	Fibo01 0.0
	Fibo02 0.2
	Fibo03 0.2
	PntUps 1
	BndUnder nil
	BndOver nil
  )
  (setq CrdLst nil)

  (defun *error* (msg)
    (princ "\nErro: ")
    (princ msg)
    (terpri)
    (setvar "OSMODE" OsmVar)
    (command)
    (command "UNDO" "END")
    (setvar "CMDECHO" 1)
    (setq *error* nil)
    (princ)
  )

  (defun OffSec	()
    (command "layer" "T" "+Terreno" "T" "+Projeto" "T" "+Geóide" "")
    (repeat 10
      (command "OFFSET"
	       "T"
	       (cdar PlGeoi)
	       (polar (cdr (assoc 10 PlGeoi)) (/ (* 3 pi) 2) Fibo03)
	       ""
      )
      (setq OffEnt (entget (entlast)))
      (entdel (entlast))
      (command "extend" (cdar PlGeoi) "" "F")
      (foreach xis OffEnt
	(if (= (car xis) 10)
	  (command (cdr xis))
	)
      )
      (command "" "")
      (setq Fibo01 Fibo02
;;;	    Fibo02 Fibo03
	    Fibo03 (+ Fibo03 Fibo01)
      )
    )
    (setq Fibo01 0.0
	  Fibo02 0.2
	  Fibo03 0.2
    )
    (repeat 10
      (command "OFFSET"
	       "T"
	       (cdar PlTerr)
	       (polar (cdr (assoc 10 PlTerr)) (/ (* 3 pi) 2) Fibo03)
	       ""
      )
      (setq OffEnt (entget (entlast)))
      (entdel (entlast))
      (command "extend" (cdar PlTerr) "" "F")
      (foreach xis OffEnt
	(if (= (car xis) 10)
	  (command (cdr xis))
	)
      )
      (command "" "")
      (setq Fibo01 Fibo02
;;;	    Fibo02 Fibo03
	    Fibo03 (+ Fibo03 Fibo01)
      )
    )
    (command "ZOOM" "OBJECT" (cdar PlProj) "")
    (command "LAYER" "MAKE" "+Hatch Abaixo" "C" "4" "" "")
    (while PntUps
      (command "BOUNDARY" (setq PntUps (getpoint "\nClique nas áreas abaixo do Geóide <ENTER para finalizar>: ")) "")
      (if PntUps
	(setq BndOver (cons (entlast) BndOver))
      )
    )
    (if BndOVER
      (progn
	(command "HATCH" "ANSI31" "" "")
	(foreach xis BndOver (command xis))
	(command "")
	(foreach xis BndOver (entdel xis))
      )
    )
    (setq PntUps 1)
    (command "LAYER" "MAKE" "+Hatch Acima" "C" "30" "" "")
    (while PntUps
      (command "BOUNDARY" (setq PntUps (getpoint "\nClique nas áreas acima do Geóide <ENTER para finalizar>: ")) "")
      (if PntUps
	(setq BndUnder (cons (entlast) BndUnder))
      )
    )
    (if BndUnder
      (progn
	(command "HATCH" "ANSI31" "" "")
	(foreach xis BndUnder (command xis))
	(command "")
	(foreach xis BndUnder (entdel xis))
      )
    )
  )

  (defun LayFix	()
;;;    (setvar "CMDECHO" 0)
    (princ "\nSelecione a Seção:")
    (setq n	 0
	  SecGrp (ssget)
	  LenGrp (sslength SecGrp)
    )
    (command "LAYER" "MAKE" "0" "")
    (repeat LenGrp
      (setq EntGrp (entget (ssname SecGRP n)))
      (if (cdr (assoc 62 EntGrp))
	(setq EntClr (cdr (assoc 62 EntGrp)))
	(setq EntClr 256)
      )
      (if (= EntClr 0)
	(setq LayNam "+By Block")
      )
      (if (= EntClr 256)
	(setq LayNam "+By Layer")
      )
      (if (= EntClr 3)
	(setq LayNam "+Terreno"
	      PlTerr EntGrp
	)
      )
      (if (= EntClr 6)
	(setq LayNam "+Projeto"
	      PlProj EntGrp
	)
      )
      (if (= EntClr 1)
	(setq LayNam "+Geóide"
	      PlGeoi EntGrp
	)
      )
      (if (not (member EntClr '(0 256 3 6 1)))
	(setq LayNam (strcat "+Cor" (rtos EntClr 2 0)))
      )
      (setq EntNly (subst (cons 8 LayNam) (assoc 8 Entgrp) EntGrp))
      (entmod EntNly)
      (setq n (1+ n))
      (command "LAYER" "F" LayNam "")
    )
    (princ)
    (OffSec)
  )
  (LayFix)
  (setvar "CMDECHO" 1)
  (setvar "OSMODE" OsmVar)
  (command "UNDO" "END")
)