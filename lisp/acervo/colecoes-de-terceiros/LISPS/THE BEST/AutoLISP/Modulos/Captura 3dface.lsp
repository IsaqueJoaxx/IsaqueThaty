(defun captura^3dface ()
  (while
    (= (setq entidad^3dface (car (entsel "\nSelecciona 3dface: "))) nil)
  )
  (if (= (cdr (assoc 0 (entget entidad^3dface))) "3DFACE")
    (progn
      (setq grupo^3dface (ssget "x" (list (cons 0 (cdr (assoc 0 (entget entidad^3dface))))
		                          (cons 8 (cdr (assoc 8 (entget entidad^3dface))))
				   )
		        )
      )
    )
    (progn
      (alert "\nEntidad Selelecionada no es 3dface")
      (quit)
    )
  )
)