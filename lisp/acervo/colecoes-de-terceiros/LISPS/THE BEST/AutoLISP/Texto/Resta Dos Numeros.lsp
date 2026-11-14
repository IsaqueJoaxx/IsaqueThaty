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
  (if (= (findfile "insertar layer.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "insertar layer.lsp"))
      (c:il)
    )
  )
  (if (= (findfile "GAP_Arial.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "GAP_Arial.lsp"))
      (GAP_arial)
    )
  ) 
)

(defun desarrollo ()
  (while
    (= (setq entidad^mayor (car (entsel "\nSelecciona Numero Mayor : "))) nil)
  )
  (while
    (= (setq entidad^menor (car (entsel "\nSelecciona Numero Menor : "))) nil)
  )
  (while
    (= (setq xy^texto (getpoint "\nIndica Posicion Resultado : ")) nil)
  )
  (command "layer" "s" "GAP_Texto-General" "")
  (command "text" "j" "m" xy^texto
	                  (cdr (assoc 40 (entget entidad^menor)))
	                  0
	                  (rtos (- (atof (cdr (assoc 1 (entget entidad^mayor)))) (atof (cdr (assoc 1 (entget entidad^menor))))) 2 2)
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

(defun c:rdn ()
  (datos^entrada)
  (desarrollo)
  (datos^salida)
)

(prompt "\nRutina Resta Dos Numeros Version 3.0 Cargada.\nDigita < RDN > Para Comenzar")

(princ)