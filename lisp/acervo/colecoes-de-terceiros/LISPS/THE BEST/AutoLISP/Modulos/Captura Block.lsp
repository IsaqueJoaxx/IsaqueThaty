(defun captura^block ()
  (while
    (= (setq entidad^block (car (entsel "\nSelecciona Block: "))) nil)
  )
  (if (= (cdr (assoc 0 (entget entidad^block))) "INSERT")
    (progn
      (setq grupo^block (ssget "x" (list (cons 0 (cdr (assoc 0 (entget entidad^block))))
		                         (cons 8 (cdr (assoc 8 (entget entidad^block))))
			                 (cons 2 (cdr (assoc 2 (entget entidad^block))))
				   )
		        )
      )
    )
    (progn
      (alert "\nEntidad Selelecionada no es Block")
      (quit)
    )
  )
)