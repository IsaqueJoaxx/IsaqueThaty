;   this program will change the size of pre writen text
;   writen by DAVID CARLILE for his encouagment write him
;   @ 2110 newport landing alpharretta GA 30201

(defun c:txsize (/ ss txsize n index ent type oldsize newsize ent1)
  (setq ss (ssget))
  (setq texsize (getdist "\nENTER NEW TEXT SIZE:"))
  (setq n (sslength ss))
  (setq index 0)
  (repeat n
    (setq ent (entget (ssname ss index)))
    (setq index (+ 1 index))
    (setq type (assoc 0 ent))
    (if (= "TEXT" (cdr type))
      (progn
        (setq oldsize (assoc 40 ent))
        (setq newsize (cons (car oldsize) texsize))
        (setq ent1 (subst newsize oldsize ent))
        (entmod ent1)
      )
    )
  )
)