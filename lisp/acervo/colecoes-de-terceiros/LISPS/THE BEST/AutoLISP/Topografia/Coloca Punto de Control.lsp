(defun datos^entrada ()
  (setq coordenadas '())
  (setq attdia_old (getvar "attdia"))
  (setvar "attdia" 0)
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
)

(defun captura^vertices ()
  (while
    (= (setq entidad (car (entsel "\nSelecciona Alineamiento : "))) nil)
  )
  (setq listasoc (entget entidad)
           ciclo (length listasoc)
  )
  (repeat ciclo
    (setq par (car listasoc))
    (if (= (car par) 10)
      (setq coordenadas (cons (cdr par) coordenadas))
    )
    (setq listasoc (cdr listasoc))
  )
  (setq coordenadas (reverse coordenadas))
)

(defun desarrollo ()
  (while
    (= (setq inicio (getint "\nIngresa Numero Punto de Control : ")) nil)
  )
  (while
    (= (setq escala (getint "\nIngresa Escala de la Viewport : ")) nil)
  )
  (while
    (= (setq rotacion (getreal "\nIngresa Rotacion del Punto de Control : ")) nil)
  )
  (setq rotacion (- 360 rotacion))
  (setq factor (/ (* 1.0 escala) 1000.0))
  (command "layer" "s" "GAP_Simbolo" "")
  (repeat (length coordenadas)
    (command "insert" "H:\\Informacion_GOLDER\\04-Info_Tecnica_Golder\\4600_Geomática\\Libreria\\Ingenieria\\Punto de Control.dwg"
                      (car coordenadas) factor factor rotacion (strcat "V-" (itoa inicio))
    )
    (setq coordenadas (cdr coordenadas)
               inicio (+ inicio 1)
    )
  )
)

(defun datos^salida ()
  (setvar "attdia" attdia_old)
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

(defun c:cpc ()
  (datos^entrada)
  (captura^vertices)
  (desarrollo)
  (datos^salida)
  (princ)
)

(prompt "\nRutina Coloca Punto de Control Version 2.0 Cargada.\nDigita < CPC > Para Comenzar")

(princ)