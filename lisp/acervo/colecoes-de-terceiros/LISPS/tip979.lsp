; TIP979.LSP: BISECT.LSP   Bisect Two Lines   (c)1994, C.D. Iddings

(defun C:BISECT()
(setvar "cmdecho" 0)
(setvar "osmode" 512)

(defun *error* (msg)
(princ "error : ")
(princ msg)
(terpri)
)

(prompt "\nSelect lines of ANGLE TO BISECT in CCW order about Intercection")
(setq   LN (entsel
                "\nSelect First Line: ")
        PA (cadr LN)
        ED (entget (car LN))
        P1 (cdr (assoc 10 ED))
        P2 (cdr (assoc 11 ED))
        LN (entsel
                "\nSelect Second Line: ")
        PB (cadr LN)
        ED (entget (car LN))
        P3 (cdr (assoc 10 ED))
        P4 (cdr (assoc 11 ED))
        P5 (inters P1 P2 P3 P4 nil))
  (if (> (distance P5 P1)(distance P5 P2))
                            (setq W1 (angle P5 P1))(setq W1 (angle P5 P2)))
  (if (> (distance P5 P3)(distance P5 P4))
                            (setq W2 (angle P5 P3))(setq W2 (angle P5 P4)))
   (setq WW (/ (+ W1 W2) 2))
     (if (< WW W1)(setq WW (+ WW pi)))
   (setq
        L1 (distance P1 P5)
        L2 (distance P2 P5)
        L3 (distance P3 P5)
        L4 (distance P4 P5)
        L5 (max L1 L2 L3 L4)
        P6 (polar P5 WW L5))
(command "line" P6 P5 "")
(prin1)
(setvar "osmode" 0)
)

