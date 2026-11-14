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
  (command "layer" "m" "GAP_nivel^1" "c"  "51" "GAP_nivel^1"
	   	   "m" "GAP_nivel^2" "c"  "80" "GAP_nivel^2"
	   	   "m" "GAP_nivel^3" "c"  "42" "GAP_nivel^3"
	           "m" "GAP_nivel^4" "c"  "56" "GAP_nivel^4"
	           "m" "GAP_nivel^5" "c"  "34" "GAP_nivel^5" ""
  )
)

(defun captura^elevacion ()
  (initget "No")
  (if (= (setq opcion (getkword "\nBuscar Rango del la DTM? <Si>/No: ")) nil)
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
)

(defun desarrollo ()
  (setq indx 0)
  (repeat (sslength grupo^3dface)
    (setq entidad (ssname grupo^3dface indx)
	 listasoc (entget entidad)
             indx (1+ indx)
	    delta (/ (- cota^maxima cota^minima) 5)
	        Z (/ (apply '+ (list (nth 2 (cdr (assoc 10 listasoc))) (nth 2 (cdr (assoc 11 listasoc))) (nth 2 (cdr (assoc 12 listasoc))))) 3)
    )
    (cond
      ((and (> z cota^minima) (< z (+ cota^minima delta))) (entmod (setq listasoc (subst (cons 8  "GAP_nivel^1") (assoc 8 listasoc) listasoc))))
      ((and (>= z (+ cota^minima delta)) (< z (+ cota^minima (* delta 2)))) (entmod (setq listasoc (subst (cons 8  "GAP_nivel^2") (assoc 8 listasoc) listasoc))))
      ((and (>= z (+ cota^minima (* delta 2))) (< z (+ cota^minima (* delta 3)))) (entmod (setq listasoc (subst (cons 8  "GAP_nivel^3") (assoc 8 listasoc) listasoc))))
      ((and (>= z (+ cota^minima (* delta 3))) (< z (+ cota^minima (* delta 4)))) (entmod (setq listasoc (subst (cons 8  "GAP_nivel^4") (assoc 8 listasoc) listasoc))))
      ((and (>= z (+ cota^minima (* delta 4))) (<= z (+ cota^minima (* delta 5)))) (entmod (setq listasoc (subst (cons 8  "GAP_nivel^5") (assoc 8 listasoc) listasoc))))
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

(defun c:ae ()
  (datos^entrada)
  (captura^elevacion)
  (desarrollo)
  (datos^salida)
)

(prompt "\nRutina Analisis Elevaciones Versión 3.0 Cargada.\nDigita < AE > Para Comenzar")

(princ)

