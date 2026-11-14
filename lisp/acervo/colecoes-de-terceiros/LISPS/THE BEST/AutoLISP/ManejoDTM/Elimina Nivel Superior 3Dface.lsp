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
  (if (= (findfile "Captura 3dface.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "Captura 3dface.lsp"))
      (captura^3dface)
    )
  )
  (setq indx 0)
)

(defun desarrollo ()
  (initget "No")
  (if (= (getkword "\nBuscar Rango del la DTM? <Si>/No: ") nil)
    (progn
      (if (= (findfile "Rango 3dface.lsp") nil)
	(progn
	  (alert
	    "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada"
	  )
	  (quit)
	)
	(progn
	  (load (findfile "Rango 3dface.lsp"))
	  (rango^3dface)
	)
      )
    )
  )
  (while
    (= (< cota^minima (setq nivel (getreal "\nIngresa Cota Minima del Nivel Superior a Eliminar [m s.n.m.] : ")) cota^maxima) nil)
  )
  (repeat (sslength grupo^3dface)
    (setq entidad (ssname grupo^3dface indx)
         listasoc (entget entidad)
             indx (1+ indx)
cota^maxima^terreno (apply 'max (list (nth 3 (assoc 10 listasoc)) (nth 3 (assoc 11 listasoc)) (nth 3 (assoc 12 listasoc))))
    )
    (prompt (strcat "\n3DFACE " (rtos indx 2 0) " de " (rtos (sslength grupo^3dface) 2 0)))
    (if (<= nivel cota^maxima^terreno)
      (command "erase" entidad "")
    )
  )
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

(defun c:ens ()
  (datos^entrada)
  (desarrollo)
  (datos^salida)
)

(prompt "\nRutina Elimina Nivel Superior 3Dface 2.0 Cargada.\nDigita < ENS > Para Comenzar")

(princ)