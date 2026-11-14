;Function to draw the four - bar link, store data and initialise
;global values.
(DEFUN DRAWLINK ()
	(SETQ SCMDE (GETVAR "CMDECHO"))
	(SETVAR "CMDECHO" 0)
	(COMMAND "COLOR" "YELLOW")
	(SETVAR "BLIPMODE" 0)
	(SETVAR "MENUECHO" 1)
	(COMMAND "UNDO" "CONTROL" "ALL")
	(RESETSCL) ;Initialisation of drawing limits
	(SETQ L01 (GETPOINT "\nENTER LINK-1 STATIONARY POINT: "))
	(SETQ L12 (GETPOINT "\nENTER JUNCTION OF LINK-1, LINK-2: "
		   L01))
	(COMMAND "LINE" L01 L12 "")
	(SETQ L23 (GETPOINT "\nENTER JUNCTION OF LINK-2, LINK-3: "
		   L12))
	(COMMAND "LINE" L12 L23 "")
	(SETQ L30 (GETPOINT "\nENTER LINK-3 STATIONARY POINT "
		   L23))
	(COMMAND "LINE" L23 L30 "")
	(COMMAND "UNDO" "3")
	(EVALLT) ;Evaluates link lengths
	(RESETLIM) ;Resets limits of drawing to enable user to view
;the entire mechanism all through the interactive session
	(COMMAND "UNDO" "CONTROL" "ONE")
	(COMMAND "LINE" L01 L12 L23 L30 "")
	(SETQ LINK2 (SSNAME (SSGET L12) 0)) ;Selects intermediate
;link for pick comparison, rotation and other manipulation
)

(DEFUN CONTRLAN () ;Control function for the ANIMATE mode
	(SETQ OPTFLG 1)
		(INSETUP)
		(ANIMATE)
	(SETQ LINK2 (SSNAME (SSGET L12) 0))
)

(DEFUN CNTRLSTP () ;Control function for the STEPPATH mode
	(SETQ OPTFLG 2)
		(INSETUP)
		(DISCRETE)
	(SETQ LINK2 (SSNAME (SSGET L12) 0))
)

(DEFUN CNTRLPTH () ;Control function for the PFINDER mode
	(SETQ OPTFLG 4)
		(INSETUP)
	(PGET)
	(KSET)
)

(DEFUN CNTRLAPTH () ;Control function for the AUTOPATH mode
	(SETQ OPTFLG 5)
		(INSETUP)
	(SETQ LNKLIST (LIST L01 L12 L23 L30))
	(PGET)
	(SETQ PSTRTFLG 0)
	(PATHSTART)
	(SETQ L01 (NTH 0 LNKLIST))
	(SETQ L12 (NTH 1 LNKLIST))
	(SETQ L23 (NTH 2 LNKLIST))
	(SETQ L30 (NTH 3 LNKLIST))
	(COMMAND "ERASE" LINK1 LINK2 LINK3 "")
	(COMMAND "COLOR" "YELLOW")
	(COMMAND "LINE" L01 L12 L23 L30 "")
	(SETQ LINK2 (SSNAME (SSGET L12) 0))
)

(DEFUN INVERT () ;Function draws inversion of the current mechanism
	(SETQ LINK1 (SSNAME (SSGET L01 ) 0))
	(SETQ LINK3 (SSNAME (SSGET L30) 0))
	(COMMAND "LINE" L30 L01 "")
	(SETQ LINK4 (SSNAME (SSGET L30) 0))
 	(SETQ F (ENTSEL "\nPICK LINK TO BE MADE STATIONARY"))
	(IF (NULL F) (SELERR)) ;Check pick
	(SETQ FL (CAR F))
	(SETQ WRPK 0)
	(IF (EQUAL LINK1 FL) (EXCH1))
	(IF (EQUAL LINK2 FL) (EXCH2))
	(IF (EQUAL LINK3 FL) (EXCH3))
	(IF (EQUAL LINK4 FL) (SETQ WRPK 1))
	(IF (= WRPK 0) (SELERR))
	(EVALLT) ;To reset link lengths
	(COMMAND "ERASE" LINK1 LINK2 LINK3 LINK4 "")
	(COMMAND "LINE" L01 L12 L23 L30 "")
	(SETQ LINK2 (SSNAME (SSGET L12) 0))
)

(DEFUN SELERR() ;Error message for invalid pick
	(PROMPT "\nSORRY, INCORRECT CHOICE")
	(INVERT)
)

(DEFUN EXCH1() ;Reset global base for inversion - link 1 stationary
	(SETQ TLINK L12)
	(SETQ L12 L30)
	(SETQ L30 TLINK)
	(SETQ WRPK 1)
)

(DEFUN EXCH2() ;Reset global base for inversion - link 2 stationary
	(SETQ TLINK L12)
	(SETQ L12 L01)
	(SETQ L01 TLINK)
	(SETQ TLINK L23)
	(SETQ L23 L30)
	(SETQ L30 TLINK)
	(SETQ WRPK 1)
)

(DEFUN EXCH3() ;Reset global base for inversion - link 3 stationary
	(SETQ TLINK L01)
	(SETQ L01 L23)
	(SETQ L23 TLINK)
	(SETQ WRPK 1)
)

(DEFUN RESETSCL () ;Function to set drawing limits
	(SETVAR "LIMMIN" OLIMIN)
	(SETVAR "LIMMAX" OLIMAX)
	(COMMAND "ZOOM" "W" OLIMIN OLIMAX)
)

(DEFUN INSETUP () ;Function identifies and accomodates for any
; drawing configuration (clockwise/anticlockwise)
	(LINKSEL) ;To pick input link (link to rotate)
	(IF (AND (= ANGSEQ PI) (<= (CAR L01) (CAR L30))) (RITLINK))
	(IF (AND (= ANGSEQ 0.0) (<  (CAR L30) (CAR L01))) (RITLINK))
	(LINKLT) ;Identifies configuration and resets data
	(SETQ ANG (ANGLE L01 L12)) ;Initialise input angle
	(EVALROT)
	(STORLAST)
)

(DEFUN ANIMATE () ;Continous animation function
		(SETQ CHECK 5)
	(WHILE (= CHECK 5)
	(PROMPT "\nMOVE CURSOR TO ANIMATE (DEPRESS LEFT BUTTON OF MOUSE TO EXIT)")
	(PROMPT "\n ")
 (SETQ INPUT (GRREAD 12))
	(SETQ CHECK (CAR INPUT))
		(SETQ INPUT (CADR INPUT))
 (SETQ ANG (ANGLE L01 INPUT))
	(SIMULATE)
	)
)

(DEFUN DISCRETE () ;Discrete simulation function
	(WHILE (/= ANG NIL)
	(PROMPT "\nENTER ANGLE IN DEGREES OR USE MOUSE (PRESS RETURN TO EXIT) :")
	(PROMPT "\n ")
 (SETQ ANG (GETANGLE L01))
	(IF (/= ANG NIL) (SIMULATE))
	)
)

(DEFUN SIMULATE () ;Function to evaluate, erase and redraw mechanism
; in new configuration
	(SETQ L12 (POLAR L01 ANG L1))
	(EVALROT) ;Math model of four - bar link
	(IF (= ANGSEQ 0.0)
	(SETQ TANG (- (+ 3.1415 REFANG) (+ ANG3 ANG2)))
	(SETQ TANG (- (+ ANG2 ANG3) REFANG))
	)
	(SETQ L23 (POLAR L30 TANG L3))
		(COMMAND "UNDO" "")
	(PROMPT "\n ")
		(COMMAND "LINE" L01 L12 L23 L30 "")
	(STORLAST)
)

(DEFUN STORLAST () ;Stores critical values of last unconstrained
; position of mechanism
	(SETQ LSTL12 L12)
	(SETQ LSTANG ANG)
	(SETQ LCANG1 CANG1)
	(SETQ LCANG2 CANG2)
	(SETQ LCANG3 CANG3)
)

(DEFUN LINKSEL () ;Function facilitates selection of input link
 	(SETQ H (ENTSEL "\nCHOOSE LINK TO ROTATE"))
	(IF (NULL H) (SELERROR))
	(SETQ E (CAR H))
	(SETQ LINK1 (SSNAME (SSGET L01 ) 0))
	(SETQ LINK3 (SSNAME (SSGET L30) 0))
	(IF (EQUAL LINK1 E) (SETQ SFL 1) (SETQ SFL 0))
	(IF (EQUAL LINK3 E) (SETQ SFL 2))
	(IF (= SFL 0) (SELERROR))
	(IF (AND (<= (CAR L01) (CAR L30)) (= SFL 1))
		(SETQ ANGSEQ 0.0) (SETQ ANGSEQ PI))
	(IF (AND (<  (CAR L30) (CAR L01)) (= SFL 2))
		(SETQ ANGSEQ 0.0))
)

(DEFUN SELERROR () ;Error message for invalid pick of input link
	(PROMPT "\nSORRY, INCORRECT CHOICE")
	(LINKSEL)
)

(DEFUN RITLINK () ;Interchanges link data if user draws mechanism 
; in anticlockwise sequence
	(SETQ T13 (REVERSE (LIST L01 L30)))
	(SETQ T22 (REVERSE (LIST L12 L23)))
	(SETQ L01 (CAR T13))
	(SETQ L30 (CADR T13))
	(SETQ L12 (CAR T22))
	(SETQ L23 (CADR T22))
	(SETQ LR (REVERSE (LIST L1 L3)))
	(SETQ L1 (CAR LR))
	(SETQ LINK (REVERSE (LIST LINK1 LINK3)))
	(SETQ LINK1 (CAR LINK))
	(SETQ LINK3 (CADR LINK))
	(SETQ L3 (CADR LR))
)

(DEFUN PGET () ;To pick/store point on intermediate (coupler)
; link for path tracing - for PFINDER and AUTOPATH modes
	(SETQ PP (ENTSEL "\nPICK POINT ON INTERMEDIATE LINK
		FOR PATH TRACING"))
	(IF (EQUAL LINK2 (CAR PP)) (SETQ CHPATH 1) (SETQ CHPATH 0))
	(IF (= CHPATH 0) (PERROR))
	(SETVAR "PDMODE" 3)
	(COMMAND "COLOR" "RED")
	(SETQ PTHPT (CADR PP))
	(SETQ PD (DISTANCE L12 PTHPT))
	(PTRACE)
)

(DEFUN PERROR () ;Error message for invalid pick
	(PROMPT "\nPOINT DOES NOT LIE ON INTERMEDIATE LINK")
	(PGET)
)

( DEFUN PTRACE () ;Function evaluates coupler curve point and echoes
	(SETQ TRANG (ANGLE L12 L23))
	(SETQ PATHTRACE (POLAR L12 TRANG PD))
	(COMMAND "POINT" PATHTRACE)
)

(DEFUN PATHSTART () ;Subfunction for AUTOPATH mode. Searches for
;one limit of constraint to begin curve generation
	(PROMPT "\nDON'T GO AWAY, WILL BE WITH YOU IN A MOMENT....")
	(SETQ ANG (ANGLE L01 L12))
	(SETQ START (+ ANG (* PI 2)))
	(WHILE (AND (< ANG START) (< PSTRTFLG 2))
	(SETQ ANG (+ ANG (/ (* 2 PI) 60))) ;Angular step for search =
;           6 degrees
	(SETQ L12 (POLAR L01 ANG L1))
	(EVALROT)
	)
	(IF (= PSTRTFLG 0) (GENPATH))
)

(DEFUN GENPATH () ;Function to generate coupler curve
	(SETQ FPT 1)
	(PROMPT "\nGENERATING COUPLER CURVE...")
	(WHILE (AND (<  PSTRTFLG 2) (<= FPT 61)) ;Condition to exit -
;second limit of constraint. For unconstrained mechanisms - 360 deg
	(SETQ ANG (- ANG (/ (* 2 PI) 60)))
	(SETQ L12 (POLAR L01 ANG L1))
	(EVALROT)
	(STORLAST)
	(IF (= ANGSEQ 0.0)
	(SETQ TANG (- (+ 3.1415 REFANG) (+ ANG3 ANG2)))
	(SETQ TANG (- (+ ANG2 ANG3) REFANG))
	)
	(SETQ L23 (POLAR L30 TANG L3))
	(SETQ TRANG (ANGLE L12 L23))
	(SETQ PATHTRACE (POLAR L12 TRANG PD))
	(IF (AND (> FPT 1) (< PSTRTFLG 2)) (DRAWPATH))
	(SETQ LASTPATH PATHTRACE)
	(SETQ FPT (+ FPT 1))
	)
)

(DEFUN DRAWPATH () ;Curve generated as series of short line segments
	(COMMAND  "LINE" LASTPATH PATHTRACE "")
)

(DEFUN ERSCR () ;Function to erase drawing on screen
	(COMMAND "ERASE" (SSGET "W" (GETVAR "LIMMIN")
		(GETVAR "LIMMAX")) "")
	(REDRAW)
)
