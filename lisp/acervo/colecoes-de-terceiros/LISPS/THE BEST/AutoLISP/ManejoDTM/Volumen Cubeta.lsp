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
  (setq volumen^laguna 0
                  indx 0
  )
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
  (setq cota^laguna (getreal "\nIngresa Cota Laguna [m s.n.m.] : "))
  (repeat (sslength grupo^3dface)
    (setq listasoc (entget (ssname grupo^3dface indx))
              indx (1+ indx)
cota^media^terreno (/ (+ (nth 3 (assoc 10 listasoc)) (nth 3 (assoc 11 listasoc)) (nth 3 (assoc 12 listasoc))) 3)
        delta^cota (- cota^laguna cota^media^terreno)
    )
    (prompt (strcat "\n3DFACE " (rtos indx 2 0) " de " (rtos (sslength grupo^3dface) 2 0)))
    (if (> cota^laguna cota^media^terreno)
      (progn
        (command "area" (list (nth 1 (assoc 10 listasoc)) (nth 2 (assoc 10 listasoc))) (list (nth 1 (assoc 11 listasoc)) (nth 2 (assoc 11 listasoc))) (list (nth 1 (assoc 12 listasoc)) (nth 2 (assoc 12 listasoc))) "")
        (setq area (getvar "area"))
        (setq volumen^parcial (* area delta^cota)
               volumen^laguna (+ volumen^laguna volumen^parcial)
        )
      )
    )
  )
  (prompt (strcat "\nVolumen Laguna a cota " (rtos cota^laguna 2 2) " m s.n.m. Es : " (rtos volumen^laguna 2 2) " m3\n"))
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

(defun c:vc ()
  (datos^entrada)
  (desarrollo)
  (datos^salida)
)

(prompt "\nRutina Volúmen Cubeta Versión 2.0 Cargada.\nDigita < VC > Para Comenzar")

(princ)