;PROGRAM NAME: C:SQUARE

;PURPOSE:	Draws a square as either a Line or Polyline

; Copyright (C) 1991, CAD Consulting Services
;	Programmer: Steve McGuinness:
; Home Phone: (203) 757-7365

; Permission to use, copy, modify, and distribute this software and its
; documentation for any purpose and without fee is hereby granted.  

;HISTORY LOG:
;	11-12-87 Steve McGuinness - Initial Coding
;	11-10-91 Steve McGuinness - Code Reduction


(defun C:SQUARE (/ c1 side c2 c3 c4 option)
	(graphscr)
	(setq
		cmde (getvar "cmdecho")
		blip (getvar "blipmode")
		orth (getvar "orthomode")
	)
	(setvar "blipmode" 0)
	(setvar "orthomode" 1)
	(initget 1)
	(setq C1 (getpoint "\nIndicate Lower Left Corner of Square:: "))
	(initget (+ 1 32))
	(setq
		SIDE  (getdist C1 "\nIndicate Length of Side: ")
		C2 (list (+ (car C1) SIDE) (cadr C1))
		C3 (list (car C2) (+ (cadr C1) SIDE))
		C4 (list (car C1) (+ (cadr C1) SIDE))
	)
	(grdraw C1 C2 -1)
	(grdraw C2 C3 -1)
	(grdraw C3 C4 -1)
	(grdraw C4 C1 -1)
	(initget "Line Polyline Exit")
	(setq option (getkword "\nDraw as (<Polyline>/Line/Exit): "))
	(setvar "cmdecho" 0)
	(cond
		((eq option "Exit") (princ "\nExit option chosen.")
			(grdraw C1 C2 -1)
			(grdraw C2 C3 -1)
			(grdraw C3 C4 -1)
			(grdraw C4 C1 -1)
	  )
		((eq option "Line") (command "line" C1 C2 C3 C4 "C"))
		((or (eq option nil)(eq option "Polyline"))
			(command "pline" C1 C2 C3 C4 "C"))
	)
	(setvar "cmdecho" cmde)
	(setvar "blipmode" blip)
	(setvar "orthomode" orth)
	(princ)
)
