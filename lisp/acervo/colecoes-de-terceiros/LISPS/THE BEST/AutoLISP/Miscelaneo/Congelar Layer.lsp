(defun c:fl (/ entidad)
  (setq entidad (car (entsel "\nSelecciona Entidad de Layer a Congelar : ")))
  (if entidad
    (progn
      (if (/= (cdr (assoc 8 (entget entidad))) (getvar "clayer"))
        (command "layer" "Freeze" (cdr (assoc 8 (entget entidad))) "")
	(alert "No Puedes Congelar el Layer Activo")
      )
    )
  )
  (princ)
)