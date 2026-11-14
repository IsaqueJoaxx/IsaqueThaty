(defun cntrlkey () ;Control function for STEPPTH2 mode
	(setq optflg 3)
		(insetup)
		(kset)
)

(defun kset () ;Initialisation for STEPPTH2 mode
	(setq ltemp1 l12)
	(setq ltemp2 l23)
	(setq ref (/ (* (angle l01 l12) 180.) pi))
	(setq ang 1)
	(keybod)
)

(defun keybod () ;This function achieves the same end as the
;function Discrete, but uses the AutoCAD commands ROTATE and MOVE
;to simulate the mechanism to its new configuration instead of
;erasing and redrawing
	( while (/= ang nil)
	(prompt "\nENTER ANGLE IN DEGREES OR USE MOUSE (PRESS RETURN TO EXIT): ")
	(prompt "\n")
	(setq ang (getangle l01))
	(if (/= ang nil) (recur3))
	)
	(command "erase" link1 link2 link3 "")
	(command "color" "yellow")
	(command "line" l01 l12 l23 l30 "")
	(setq link2 (ssname (ssget l12) 0))
)

(defun recur3 () ;Subfunction of keybod
	(setq l12 (polar l01 ang l1))
 	(evalrot)
	(if (/= ang nil) (execute))
)

(defun execute () ;Function rotates link 1 and link 3 and moves
;and rotates link 2 to reach new configuration
		(setq pt (/ (* 180. ang) 3.1415))
	(if (= angseq 0.0)
	(setq tang (- (+ pi refang) (+ ang3 ang2)))
	(setq tang (- (+ ang2 ang3) refang))
	)
	(setq l23 (polar l30 tang l3))
	(setq tang (/ (* tang 180.) pi))
	(setq angl2 (/ (* (angle l12 l23) 180.) pi))
	(setq ref2 (/ (* (angle ltemp1 ltemp2) 180.) pi))
	(setq ref3 (/ (* (angle l30 ltemp2) 180.) pi))
	(command "rotate" link1 "" l01 "ref" ref pt)
	(command "move" link2 "" ltemp1 l12)
	(command "rotate" link2 "" l12 "ref" ref2 angl2)
	(command "rotate" link3 "" l30 "ref" ref3 tang)
	(if (= optflg 4) (ptrace)) ;To mark path in PFINDER mode
	(setq ltemp1 l12)
	(setq ltemp2 l23)
	(setq ref pt)
	(storlast)
)

(DEFUN RESETLIM () ;Function to reset limits. Limits are reset to 
;ensure that the entire mechanism is on the screen no matter what
;the angular orientation of the input link is.
	(SETQ LKMINX (MIN (- (CAR L01) L1) (- (CAR L30) L3)))
	(SETQ LKMINY (MIN (- (CADR L01) L1) (- (CADR L30) L3)))
	(SETQ LKMAXX (MAX (+ (CAR L01) L1) (+ (CAR L30) L3)))
	(SETQ LKMAXY (MAX (+ (CADR L01) L1) (+ (CADR L30) L3)))
	(SETLIM)
	(SETQ NLIMINX (MIN (CAR (GETVAR "LIMMIN")) LKMINX))
	(SETQ NLIMINY (MIN (CADR (GETVAR "LIMMIN")) LKMINY))
	(SETQ NLIMAXX (MAX (CAR (GETVAR "LIMMAX")) LKMAXX))
	(SETQ NLIMAXY (MAX (CADR (GETVAR "LIMMAX")) LKMAXY))
	(SETVAR "LIMMIN" (LIST NLIMINX NLIMINY))
	(SETVAR "LIMMAX" (LIST NLIMAXX NLIMAXY))
	(COMMAND "ZOOM" "W" (GETVAR "LIMMIN") (GETVAR "LIMMAX"))
)

(DEFUN SETLIM () ;Function stores system variables LIMMIN and LIMMAX
;to restore them before another linkage is drawn
	(SETQ OLIMIN (GETVAR "LIMMIN"))
	(SETQ OLIMAX (GETVAR "LIMMAX"))
)

(DEFUN ALTERLIMS ()
	(PROMPT "\nLOWER LIMITS: ")
	(PRINT (GETVAR "LIMMIN"))
	(SETQ TLMIN (GETPOINT "\nNEW LOWER LIMITS: "))
	(PROMPT "\nUPPER LIMITS: ")
	(PRINT (GETVAR "LIMMAX"))
	(SETQ TLMAX (GETPOINT "\nNEW UPPER LIMITS: "))
	(IF (/= TLMIN NIL) (SETVAR "LIMMIN" TLMIN))
	(IF (/= TLMAX NIL) (SETVAR "LIMMAX" TLMAX))
	(SETLIM)
	(COMMAND "ZOOM" "A")
)
