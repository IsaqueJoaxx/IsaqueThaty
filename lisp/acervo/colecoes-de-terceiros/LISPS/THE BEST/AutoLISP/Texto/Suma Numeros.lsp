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
  (setq indx 0 sumatoria 0)
  (setq grupo (ssget))
  (repeat (sslength grupo)
    (setq entidad (ssname grupo indx)
         listasoc (entget entidad)
    )
    (if (= (cdr (assoc 0 listasoc)) "TEXT")
      (setq numero (atof (cdr (assoc 1 listasoc)))
        sumatoria (+ sumatoria numero)
      )
    )
    (setq indx (+ indx 1))
  )
  (while
    (= (setq xy^texto (getpoint "\nIndica Posicion Resultado : ")) nil)
  )
  (command "layer" "s" "GAP_Texto-General" "")
  (setq escala 5000)
  (command "text" "j" "m" xy^texto (cdr (assoc 40 listasoc)) 0 (rtos sumatoria 2 2))
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

(defun c:sn ()
  (datos^entrada)
  (desarrollo)
  (datos^salida)
)

(prompt "\nRutina Suma Numeros Version 2.0 Cargada.\nDigita < SN > Para Comenzar")

(princ)