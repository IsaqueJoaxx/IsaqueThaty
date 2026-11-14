(defun captura^grupo ()
  (while
    (= (setq entidad (car (entsel "\nSelecciona Elemento Tipo: "))) nil)
  )
  (if (/= entidad nil)
    (progn
      (setq grupo (ssget "x" (list
			       (cons 0 (cdr (assoc 0 (entget entidad))))
               	               (cons 8 (cdr (assoc 8 (entget entidad))))
			     )
		  )
      )
    )
  )
)