; TIP1002.LSP: MP.LSP   Mirror Point   (c)1994, Bill Bratt

; Function resets beginning values if an error occurs or (ctrl-C).
(defun *error* (MSG)
   (if (/= MSG "Function cancelled")
      (princ (strcat "\nAutoLISP Error:  " MSG))
      (terpri)
   )
   (setvar "cmdecho" SAVE_CMDE )
   (setvar "blipmode" SAVE_BLIP )
   (setvar "orthomode" SAVE_ORTHO )
   (setvar "osmode" SAVE_OSMODE )
)  ; end of function
(defun C:MP (/ PNT1 PNT2 PNT3 PNT4 H_DIST V_DIST HH_DIST HV_DIST )
   (start_function)
   (prompt "\nOSNAP is set at Nearest")
   (setvar "osmode" 512 ) 
   (setq PNT1 (getpoint 
   "\nPick point on first line: \n\n"))
   (prompt "\nOSNAP is set at Perpendicular")
   (setvar "osmode" 128 ) 
   (setq PNT3 (getpoint 
   "\nPick perpendicular point on second line: \n\n" PNT1))
   (setq PNT2 (list (car PNT1)(cadr PNT3)))
   (setq PNT4 (list (car PNT3)(cadr PNT1)))
   (setq H_DIST (distance PNT1 PNT4))
   (setq V_DIST (distance PNT1 PNT2))
   (setq HH_DIST(/ H_DIST 2.0))
   (setq HV_DIST(/ V_DIST 2.0))
   (setq MP (list (+ (car PNT1) HH_DIST) (+ (cadr PNT1 ) HV_DIST)))
   (princ)
   (end_function)
)
(defun start_function ()
   (graphscr)
   (setq cl (getvar "clayer"))
   (setq save_cmde (getvar "cmdecho"))
   (setq save_blip (getvar "blipmode"))
   (setq save_ortho (getvar "orthomode"))
   (setq save_osmode (getvar "osmode"))
   (setvar "cmdecho" 0)
   (setvar "blipmode" 0)
   (setvar "orthomode" 0)             ;Ortho off
   (setvar "osmode" 0)   ; NONE
)   ;END OF FUNCTION
(defun end_function ()
   (setvar "cmdecho" SAVE_CMDE )
   (setvar "blipmode" SAVE_BLIP )
   (setvar "orthomode" SAVE_ORTHO )
   (setvar "osmode" SAVE_OSMODE )
   (princ)
)   ; end of function
(princ "MP loaded")(princ)
