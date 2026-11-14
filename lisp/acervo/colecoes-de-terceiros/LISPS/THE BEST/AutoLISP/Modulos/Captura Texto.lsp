(defun captura^texto ()
  (while
    (= (setq entidad^texto (car (entsel "\nSelecciona Texto: "))) nil)
  )
  (if (= (cdr (assoc 0 (entget entidad^texto))) "TEXT")
    (progn
      (setq grupo^texto (ssget "x" (list (cons 0 (cdr (assoc 0 (entget entidad^texto))))
;;;		                         (cons 7 (cdr (assoc 7 (entget entidad^texto))))
			                 (cons 8 (cdr (assoc 8 (entget entidad^texto))))
				   )
		        )
      )
    )
    (progn
      (alert "\nEntidad Selelecionada no es Texto")
      (quit)
    )
  )
)