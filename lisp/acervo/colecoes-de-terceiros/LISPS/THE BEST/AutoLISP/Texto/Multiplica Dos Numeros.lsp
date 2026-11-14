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
    (= (setq entidad^mayor (car (entsel "\nSelecciona Primer Numero : "))) nil)
  )
  (while
    (= (setq entidad^menor (car (entsel "\nSelecciona Segundo Numero : "))) nil)
  )
  (while
    (= (setq xy^texto (getpoint "\nIndica Posicion Resultado : ")) nil)
  )
  (command "layer" "s" "GAP_Texto-General" "")
  (command "text" "j" "m" xy^texto
	                  (cdr (assoc 40 (entget entidad^menor)))
	                  0
	                  (rtos (abs (* (atof (cdr (assoc 1 (entget entidad^mayor)))) (atof (cdr (assoc 1 (entget entidad^menor)))))) 2 2)
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

(defun c:mdn ()
  (datos^entrada)
  (desarrollo)
  (datos^salida)
)

(prompt "\nRutina Multiplica Dos Numeros Version 3.0 Cargada.\nDigita < MDN > Para Comenzar")

(princ)
  