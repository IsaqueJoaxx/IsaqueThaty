(defun c:be ()
  (setq grupo (ssget "x" (list (cons 8 (cdr (assoc 8 (entget (car (entsel "\nSeleccione Entidad a Borrar : "))))))))
        layer (cdr (assoc 8 (entget (ssname grupo 0))))
  )
  (command "erase" grupo "")
  (alert
    (if (> (sslength grupo) 1)
      (strcat "Se eliminaron " (itoa (sslength grupo)) " elementos\nLos cuales pertenecian al layer " layer)
      (strcat "Se elimino " (itoa (sslength grupo)) " elemento\nEl cual pertenecia al layer " layer)
    )
  )
  (princ)
)

(prompt "\nRutina Borrar Entidad Versión 2.0 Cargada.\nDigita < BE > Para Comenzar")

(princ)


