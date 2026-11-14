(defun c:txch (/ ss n i e1 sl sl2 e2)
     (setq ss (ssget))
     (setq n (sslength ss))
     (setq i 0)
     (repeat n
          (setq e1 (entget (ssname ss i)))
          (setq i (+ i 1))
          (setq sl (assoc 0 e1))
          (if (= "TEXT" (cdr sl))
               (progn
                    (setq sl2 (assoc 40 e1))
                    (setq e2 (subst '(40 . 3.0) sl2 e1))
                    (entmod e2)
               )
          )
     )
)
