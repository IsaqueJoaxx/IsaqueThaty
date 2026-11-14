;;;Listing 1: ARCFLIP.LSP
;;; example function to use (arcflip), by Peter Sheerin
;;; March 17, 1993

(defun C:FLIP ()
  (setq ss (ssget))
  (setq cnt 0)
  (while (< cnt (sslength ss))
    (arcflip (ssname ss cnt))
    (setq cnt (+ 1 cnt))
  )
);defun

;;; (arcflip), by David Sieracki
;;; March 9, 1993
;;; Tips & Trics
(defun arcflip (ent1 / vec opt cpt sang eang)

(setq elst (entget ent1))

(if (and
    (or (equal (cdr (assoc 0 elst)) "ARC")
        (equal (cdr (assoc 0 elst)) "CIRCLE")
    )
    (equal '(0 0 -1) (trans (cdr (assoc 210 elst)) 0 1 t) 0.1e-12)
    );and
(progn

(setq vec (mapcar '- (cdr (assoc 210 elst))))
(setq cpt (trans (setq opt (cdr (assoc 10 elst))) ent1 vec))


(if (= (cdr (assoc 0 elst)) "ARC")
(progn
	(setq sang
		(angle cpt
			(trans
				(polar opt (cdr (assoc 51 elst)) 1.0)
			ent1 vec)
		)
	)
	(setq eang
		(angle cpt
			(trans
				(polar opt (cdr (assoc 50 elst)) 1.0)
			ent1 vec)
		)
	)
);progn
);if

(entmod
	(subst (cons 10 cpt) (assoc 10 elst)
		(subst (cons 50 sang) (assoc 51 elst)
			(subst (cons 210 vec) (assoc 210 elst) elst)
    )
  )
);entmod
);progn
);if

);defun
