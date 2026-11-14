;From CADENCE Magazine November , 1991
;Page 156

(defun c:movedim (/ e1 as more pnt)
 (setq e1(car(entsel "\nSelect DIM text to move: ")))
 (setq as(entget e1))
(if(< (cdr(assoc 70 as)) 128)
 (setq as(subst (cons 70 (+ 128 (cdr(assoc 70 as))))(assoc 70 as)as))
)
 (setq more T)
 (while more
  (setq pnt (grread T))
  (if(= (car pnt) 3)(setq more nil))
  (setq as(subst (cons 11 (cadr pnt))(assoc 11 as)as))
  (entmod as)
 )
(prin1)
)
  
