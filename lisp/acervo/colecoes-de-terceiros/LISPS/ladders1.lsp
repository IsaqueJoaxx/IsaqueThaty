;Ladder.lsp v2.0
;
;	This routine will draw a cable tray, or cable ladder
;       any size, at any elevation. If the 'grtext' command interferes with
;	your graphics board, you may delete that section.  I set a layer to 
;       yellow for my purposes.  Change this to suit your needs. If you add
;       to the routine, please remember me and upload a copy to my BBS.
;					       	   The ACADemy
;  		         			  (512) 445-6000
;						    2400 baud
;					 	    24  hours
;						Allen Andrews SysOp
;
; 
(defun dtr (a)
	(* pi (/ a 180.00))
)
(defun c:ladder ()
;
(grtext 0 "   ^   ")
(grtext 1 "   A   ")
(grtext 2 "routine")
(grtext 3 "  for  ")
(grtext 4 "  CTI  ")
(grtext 5 "   by  ")
(grtext 6 " Allen ")
(grtext 7 "Andrews")
(grtext 8 "-------")
(grtext 9 " ----- ")
(grtext 10 "  ---  ")
(grtext 11 "   -   ")
(grtext 12 "  :-)  ")
(grtext 13 "       ")
;
(if (= lay nil)(setq lay (strcase "yellow")))   ;layer      \
(if (= width nil)(setq width 12.0))             ;ladder width \
(if (= sp nil)(setq sp 9.0))                    ;rung spacing  \ These are 
(if (= x nil)(setq x 0.5))                      ;rail width    / my defaults
(if (= y nil)(setq y 2.0))                      ;rung width  /
(if (= e nil)(setq e 98.0))                     ;elevation /
;
(TERPRI)
;
	(PROMPT "  Enter the layer for the ladder.................... [ ")(princ (strcase lay))(princ " ] -=> ")
		(setq templay (getstring))
		(if (= templay "")(setq templay lay))
		(setq lay templay)
;
	(PROMPT "  Enter the width of the ladder..................... [ ")(princ (rtos width 4 2))(princ " ] -=> ")
		(setq tempwidth (getdist))
		(if (/= tempwidth nil) (setq width tempwidth))
;
	(PROMPT "  Enter the spacing for the rungs................... [ ")(princ (rtos sp 4 2))(princ " ] -=> ")
		(setq tempsp (getdist))
		(if (/= tempsp nil) (setq sp tempsp))
;
	(PROMPT "  Enter the width of the rails...................... [ ")(princ (rtos x 4 2))(princ " ] -=> ")
		(setq tempx (getdist))
		(if (/= tempx nil) (setq x tempx))
;
	(PROMPT "  Enter the width of the rungs...................... [ ")(princ (rtos y 4 2))(princ " ] -=> ")
		(setq tempy (getdist))
		(if (/= tempy nil) (setq y tempy))
;
	(PROMPT "  Enter the elevation to the bottom of the ladder... [ ")(princ (rtos e 4 2))(princ " ] -=> ")
		(setq tempe (getdist))
		(if (/= tempe nil) (setq e tempe))
;
(command "layer""t" lay "s" lay "")
;
(graphscr)
;
(print)
        (setq p1 (getpoint "\nPick the start point of the ladder.... -=> "))
	(setq p2 (getpoint p1 "\nPick the end point of the ladder...... -=> "))
;
		(setvar "cmdecho" 0)
                (setvar "highlight" 0)
		(setvar "blipmode" 0)
		(setq oldzin (getvar "dimzin"))
		(setvar "dimzin" 1)
		(setq om (getvar "orthomode"))
		(setq ob (getvar "snapbase"))
		(setvar "snapbase" p1)
		(setq oa (getvar "snapang"))
		(setvar "snapang" (angle p1 p2))
		(setq ang (angle p1 p2))
		(setvar "orthomode" 1)
		(setq oldel (getvar "elevation"))
		(setvar "elevation" e)
                (setq oldthk (getvar "thickness"))
		(setvar "thickness" 1.5)
;
			(setq p3 (polar p1 (+ ang (dtr 90)) x))
			(setq p4 (polar p2 (+ ang (dtr 90)) x))
			(setq p5 (polar p1 (+ ang (dtr 90)) (- width x)))
			(setq p6 (polar p2 (+ ang (dtr 90)) (- width x)))
			(setq p7 (polar p1 (+ ang (dtr 90)) width))
			(setq p8 (polar p2 (+ ang (dtr 90)) width))
	(setq number (fix(/ (distance p3 p4) sp)))
	(setq rd (- (/ (- (distance p1 p2)(* (- number 1) sp)) 2)(/ y 2)))
			(setq p9 (polar p3 (+ ang (dtr 0)) rd))
			(setq p10 (polar p5 (+ ang (dtr 0)) rd))
			(setq p11 (polar p9 (+ ang (dtr 0)) y))
			(setq p12 (polar p10 (+ ang (dtr 0)) y))
;
        (command"line" p1 p2 "")
	(command"line" p3 p4 "")
	(command"line" p5 p6 "")
	(command"line" p7 p8 "")
(setvar "elevation" (+ e 1.0))
(setvar "thickness" 0.5)
	(command"line" p9 p10 "")
(setq temp (entlast))
	(command"line" p11 p12 "")
(setq temp2 (entlast))
(command"select" temp temp2 "")
;
	(setq rungs (ssget "p"))
		(command"array" rungs "" "r" "1" number sp)
;
(grtext)
 			(setvar "dimzin" oldzin)
			(setvar "snapbase" ob)
			(setvar "snapang" oa)
			(setvar "orthomode" om)
			(setvar "blipmode" 1)
			(setvar "highlight" 1)
			(setvar "elevation" oldel)
			(setvar "thickness" oldthk)
 		(princ)
(print)
(print)
)
