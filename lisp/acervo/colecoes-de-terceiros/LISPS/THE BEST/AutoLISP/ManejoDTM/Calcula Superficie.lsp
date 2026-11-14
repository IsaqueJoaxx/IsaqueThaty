(defun datos^entrada ()
  (if (= (findfile "Variables Entrada.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "Variables Entrada.lsp"))
      (variables^entrada)
    )
  )
  (initget "No")
  (if (= (setq opcion (getkword "\nDeseas Seleccionar un Area Puntual? <Si>/No: ")) nil)
    (setq grupo^3dface (ssget))
    (progn
      (if (= (findfile "Captura 3dface.lsp") nil)
	(progn
	  (alert
	    "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada"
	  )
	  (quit)
	)
	(progn
	  (load (findfile "Captura 3dface.lsp"))
	  (captura^3dface)
	)
      )
    )
  )
  (setq indx^3dface 0 area^total 0)
)

(defun desarrollo ()
  (repeat (sslength grupo^3dface)
    (setq entidad^3dface (ssname grupo^3dface indx^3dface)
         listasoc^3dface (entget entidad^3dface)
	               x (distance (cdr (assoc 10 listasoc^3dface)) (cdr (assoc 11 listasoc^3dface)))
	               y (distance (cdr (assoc 11 listasoc^3dface)) (cdr (assoc 12 listasoc^3dface)))
	               z (distance (cdr (assoc 10 listasoc^3dface)) (cdr (assoc 12 listasoc^3dface)))
	      area^total (+ (sqrt (* (/ (+ x y z) 2) (- (/ (+ x y z) 2) x) (- (/ (+ x y z) 2) y) (- (/ (+ x y z) 2) z))) area^total)
	     indx^3dface (1+ indx^3dface)
    )
    (prompt (strcat "\nProcesando " (rtos indx^3dface 2 0) " de " (rtos (sslength grupo^3dface) 2 0)))
  )
  (prompt (strcat "\nSuperficie es = " (rtos area^total 2 2) " m2\n"))
)

(defun datos^salida ()
  (if (= (findfile "Variables Salida.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "Variables Salida.lsp"))
      (variables^salida)
    )
  )
)

(defun c:cs ()
  (datos^entrada)
  (desarrollo)
  (datos^salida)
)

(prompt "\nRutina Calcula Superficie Versión 4.0 Cargada.\nDigita < CS > Para Comenzar")

(princ)


