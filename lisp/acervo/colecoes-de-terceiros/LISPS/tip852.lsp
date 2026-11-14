; TIP852: BRKFST.LSP (c)1993, James E. Towle
;;   This LISP is for multiple breaks with the F, (FIRST), option
;;         it has been working on AutoCAD 10 & 12 for me.
;;   BY: JAMES E. TOWLE
;;; === NWERR.LSP === Error handler

(DEFUN NWERR (s)
   (if (/= s "Function cancelled")
      (princ (strcat "\nError:" s))
   )
   (setq *error* olderr)
   (PRINC)
)

;;; === BRKFST.LSP ===

(defun C:BRKFST ()
     (setq olderr *error*
           *error* nwerr
           chm     0)
     (setvar "cmdecho" 0)
     (setq ent1 (entsel "\nPick Line To Break: ")
           ent (car ent1)
     )
     (redraw ent 3)
     (setq pt2 (getpoint "\nFirst point: ")
           pt3 (getpoint "\nSecond Point: ")
     )
     (command "BREAK" ent1 "F" pt2 pt3)
     (setvar "cmdecho" 1)
     (setq *error* olderr)
     (princ)
     );END

