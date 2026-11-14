; TIP981.LSP: CLW.LSP    Change Pline Width   (c)1994, Wally Agy

;CLW  Change Pline Width to specified input.
;;;    Wally Agy 1-27-93 
(defun C:CLW ()
(setvar "cmdecho" 0)
  (setq LW (getdist "\nEnter new line width:  "))
(setq A 1)
  (prompt "\nSelect Poly Lines to chg " )
  (while (/= A nil)
    (progn
      (setq A (entsel))
      (if (/= A nil)
         (progn
           (setq B (entget (car A)))
           (setq C (cdr (assoc 40 B)))
           (princ C)
           (command "pedit" A "W" LW "")
         )
      )
    ) 
  )  
(princ)
)
