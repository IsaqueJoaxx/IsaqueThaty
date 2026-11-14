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

(defun seleccion^entidad ()
  (while
    (= (setq entidad (car (entsel "\nSelecciona Linea Talud : "))) nil)
  )
  (setq listasoc (entget entidad))
  (cond
    ((= (cdr (assoc 0 listasoc)) "LINE")
     (setq inicio (cdr (assoc 10 listasoc))
	      fin (cdr (assoc 11 listasoc))
     )
    )
    ((= (cdr (assoc 0 listasoc)) "LWPOLYLINE")
     (setq inicio (cdr (assoc 10 listasoc))
         listasoc (cdr (member (assoc 10 listasoc) listasoc))
	      fin (cdr (assoc 10 listasoc))
      )
    )
    ((= (cdr (assoc 0 listasoc)) "POLYLINE")
     (setq entidad (entnext entidad)
	  listasoc (entget entidad)
    lista_vertices '()
     )
     (while (= (cdr (assoc 0 listasoc)) "VERTEX")
       (setq lista_vertices (cons (cdr (assoc 10 listasoc)) lista_vertices)
	            entidad (entnext entidad)
	           listasoc (entget entidad)
       )
     )
     (setq inicio (nth 1 lista_vertices)
	      fin (nth 0 lista_vertices)
     )
    )
    (T (alert "Entidad Seleccionada no es Linea") (quit))
  )
)

(defun procesa^datos ()
  (while
    (= (setq posicion (getpoint "\nIndica Posicion Datos Talud : ")) nil)
  )
  (initget 1 "50 75 100 125 200 250 500 750 1000 1250 2000 2500 4000 5000 7500 10000")
  (setq escala (/ (atoi (getkword  "\nIngresa del Dibujo < 50 75 100 125 200 250 500 750 1000 1250 2000 2500 4000 5000 7500 10000 > : ")) 1000.0))
  (setq delta_horizontal (- (nth 0 fin) (nth 0 inicio))
	  delta_vertical (- (nth 1 fin) (nth 1 inicio))
	talud_horizontal (/ (* 1 delta_horizontal) delta_vertical)
  )
  (if (= (minusp delta_horizontal) nil)
    (progn
      (setq fin_posicion (polar posicion 0 (* (* (abs talud_horizontal) 2.5) escala))
       angulo_horizontal 0
      )
      (command "layer" "s" "GAP_Simbolo" ""
               "line" posicion fin_posicion ""
      )
      (setq entidad_linea (entlast))
    )
    (progn
      (setq fin_posicion (polar posicion pi (* (* (abs talud_horizontal) 2.5) escala))
       angulo_horizontal pi
      )
      (command "layer" "s" "GAP_Simbolo" ""
               "line" posicion fin_posicion ""
      )
      (setq entidad_linea (entlast))
    )
  )
  (if (= (minusp delta_vertical) nil)
    (progn
      (setq posicion_Texto_horizontal (polar (polar posicion angulo_horizontal (* (* (/ (abs talud_horizontal) 2) 2.5) escala)) (* (/ pi 2) 3) (* 2.25 escala))
	      posicion_Texto_vertical (polar (polar posicion (/ pi 2) (* 1.25 escala)) angulo_horizontal (* (+ (* (abs talud_horizontal) 2.5) 1.25) escala))
      )
      (command "layer" "s" "GAP_Simbolo" ""
               "line" fin_posicion (polar fin_posicion (/ pi 2) (* 2.5 escala)) posicion ""
	       "layer" "s" "GAP_Texto-General" ""
               "text" "j" "m" posicion_Texto_horizontal (* 2.5 escala) 0 (rtos (abs talud_horizontal) 2 1)
	       "text" "j" "m" posicion_Texto_vertical (* 2.5 escala) 0 (rtos 1 2)
      )
    )

    
    (progn
      (setq posicion_Texto_horizontal (polar (polar posicion angulo_horizontal (* (* (/ (abs talud_horizontal) 2) 2.5) escala)) (/ pi 2) (* 2.25 escala))
	      posicion_Texto_vertical (polar (polar posicion (* (/ pi 2) 3) (* 1.25 escala)) angulo_horizontal (* (+ (* (abs talud_horizontal) 2.5) 1.25) escala))
      )
      (command "layer" "s" "GAP_Simbolo" ""
               "line" fin_posicion (polar fin_posicion (* (/ pi 2) 3) (* 2.5 escala)) posicion ""
	       "layer" "s" "GAP_Texto-General" ""
	       "text" "j" "m" posicion_Texto_horizontal (* 2.5 escala) 0 (rtos (abs talud_horizontal) 2 2)
	       "text" "j" "m" posicion_Texto_vertical (* 2.5 escala) 0 (rtos 1 2 0)
      )
    )
  )
  (command "pedit" entidad_linea "" "j" "all" "" "")
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

(defun c:cvt ()
  (datos^entrada)
  (seleccion^entidad)
  (procesa^datos)
  (datos^salida)
)

(prompt "\nRutina Colocar Valores Talud Versión 1.0 Cargada.\nDigita < CVT > Para Comenzar")

(princ)