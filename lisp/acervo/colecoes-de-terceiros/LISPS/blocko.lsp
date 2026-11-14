
(defun BLOCKOUT(/ e out ent)
;==================================================================
;THIS FUNCTION SIFTS ALL BLOCK INSTANCES AND THEIR COORDINATES FROM
;     THE CURRENT DRAWING AND WRITES THEM TO THE LINE PRINTER.
;CHAZ HENRY - NORTH CAROLINA STATE UNIV. / SIEMENS     919-365-6660
;==================================================================
       (setq out (open "LPT1" "w"))
       (setq e (entnext))
       (while e
         (setq ent (entget e))
         (if (= "INSERT" (cdr(assoc 0 ent)))
            (progn
              (princ (cdr (assoc 2 ent)) out)
              (princ "   " out)
              (princ (cdr (assoc 10 ent)) out)
              (princ "\n" out)
            )
         )
         (setq e (entnext e))
       )
       (close out)
       (princ "DONE")
       (print)
)
