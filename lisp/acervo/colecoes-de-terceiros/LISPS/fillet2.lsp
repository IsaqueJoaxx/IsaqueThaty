; FILLET2.LISP
; by Dan Moran
;
(defun C:FILLET2 (/ A B AN BN AE BE AANG BANG TANA TANB TAND NS RS R C)
  (setvar "CMDECHO" 0)
  (setq B (entsel "Pick changing line: "))
  (setq BN (osnap (cadr B) "near"))
  (setq BE (osnap (cadr B) "end"))
  (setq A (entsel "\nPick unchanging line: "))
  (setq AN (osnap (cadr A) "near"))
  (setq AE (osnap (cadr A) "end"))
  (setq NS (inters AN AE BN BE nil))
  (setq RS (strcat "Radius <" (rtos (getvar "FILLETRAD")) ">: "))
  (setq R (getreal RS))
  (if R (setvar "FILLETRAD" R); THEN
       (setq R (getvar "FILLETRAD")); ELSE
     )
  (setq AANG (angle NS AN))
  (setq BANG (angle NS BN))
  (setq C (-BANG AANG))
  (if (> C PI)(setq C (-C (* 2.0 PI))))
  (if (< C(-PI))(setq C (+C (* 2.0 PI))))
  (setq TAND (abs (/(* R (cos (/ C2.0)))(sin(/C 2.0)))))
  (setq TANA (polar NS AANG TAND))
  (setq TANB (polar NS BANG TAND)) 
  ((if (minusp C)(command "ARC" TANA "E" TANB "R" R); Then 
                 (command "ARC" TANB "E" TANA "R" R); Else
  )
  (command "CHANGE" (car B) " " TANB)
  (setvar "CMDECHO" 1)
)
