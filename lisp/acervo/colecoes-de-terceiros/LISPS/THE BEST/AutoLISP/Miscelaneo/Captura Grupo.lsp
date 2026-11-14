(defun c:cg ()
  (while
    (= (setq entidad (car (entsel "\nSelecciona Entidad del Layer: "))) "")
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
  (command "copyclip" grupo)
)

(prompt "\nRutina Captura Grupo Versión 1.0 Cargada.\nDigita < CG > Para Comenzar")