(defun datos_de_entrada ()
  (setvar "blipmode" 1)
  (setvar "aunits" 0)
  (setq attdia^old (getvar "attdia"))
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
  (setq lista^distancia '()
             lista^cota '()
          indx_distancia 0
               indx_cota 0
  )
)

(defun leer^archivo^entrada ()
  (setq archivo^datos (open (getfiled "Selecciona Archivo de Datos" "c:\\" "csv" 8) "r"))
  (while (setq linea (read-line archivo^datos))
    (setq indx 1)
    (repeat (strlen linea)
      (if (= (substr linea indx 1) ";")
        (setq posicion indx)
      )
      (setq indx (1+ indx))
    )
    (setq distancia (substr linea 1 (- posicion 1))
    lista^distancia (cons (atof distancia) lista^distancia)
               cota (substr linea (+ posicion 1) (- (strlen linea) posicion))
         lista^cota (cons (atof cota) lista^cota)
    )
  )
  (setq lista^distancia (reverse lista^distancia)
             lista^cota (reverse lista^cota)
  )
)

(defun modificar_referencia ()
  (setq	cota^minima (apply 'min lista^cota)
	cota^maxima (apply 'max lista^cota)
	referencia  (- cota^minima 20)
  )
  (initget 128)
  (if
    (=
      (setq referencia^nueva
	(getkword
	  (strcat
	    "\nCota Minima Perfil: "
	    (rtos cota^minima 2 1)
	    ". Cota Maxima Perfil: "
	    (rtos cota^maxima 2 1)
	    "\nCota Referencia < "
	    (rtos referencia 2 0)
            " > : "
	  )
	)
      )
      nil
    )
    (setq delta_y (- cota^maxima referencia))
    (progn
      (setq delta_y (- cota^maxima (atoi referencia^nueva))
         referencia (atoi referencia^nueva)
      )
    )
  )
)

(defun escalas ()
  (initget 1 "100 125 200 250 500 1000 1250 2000 2500 4000 5000 7500 10000 25000 50000")
  (setq escala_horizontal (getkword  "\nEscala Horizontal < 100 125 200 250 500 1000 1250 2000 2500 4000 5000 7500 10000 25000 50000 > : "))
  (initget "100 125 200 250 500 1000 1250 2000 2500 4000 5000 7500 10000 25000 50000")
  (if (= (setq escala_vertical (getkword  (strcat "\nEscala Vertical < " escala_horizontal " > : "))) nil)
    (setq escala_vertical escala_horizontal)
  )
  (setq escala_horizontal (atoi escala_horizontal)
	  escala_vertical (atoi escala_vertical)
  )	
)

(defun define_grillas ()
  (while
    (= (setq grilla_vertical (getint (strcat "\nEspaciamiento Valores Grilla Vertical < Altura Perfil desde Referencia " (rtos delta_y 2 1) " > : "))) nil)
    (alert "\nEspaciamiento Grilla Vertical")
  )
  (while
    (= (setq grilla_horizontal (getint (strcat "\nEspaciamiento Valores Grilla Horizontal < Largo del Perfil " (rtos (last lista^distancia) 2 1) " > : "))) nil)
    (alert "\nEspaciamiento Grilla Horizontal")
  )
  (initget (+ 2 4) "No")
  (setq grilla (getkword "\nMostrar Grilla No/<Si> : "))
)  

(defun ubicacion_Perfil ()
  (while
    (= (setq ubicacion (getpoint "\nUbicacion del Perfil : ")) nil)
    (alert "\nUbicacion del Perfil")
  )
)

(defun dibuja_Terreno ()
  (repeat (- (length lista^distancia) 1)
    (setq xy_referencia1 (polar ubicacion 0 (/ (* 1000.0 (nth indx_distancia lista^distancia)) escala_horizontal))
             xy_Terreno1 (polar xy_referencia1 (/ pi 2) (/ (* 1000.0 (- (nth indx_cota lista^cota) referencia)) escala_vertical))
          indx_distancia (1+ indx_distancia)
	       indx_cota (1+ indx_cota)
          xy_referencia2 (polar ubicacion 0 (/ (* 1000.0 (nth indx_distancia lista^distancia)) escala_horizontal))
             xy_Terreno2 (polar xy_referencia2 (/ pi 2) (/ (* 1000.0 (- (nth indx_cota lista^cota) referencia)) escala_vertical))
    )
    (command "layer" "s" "GAP_Terreno" ""
             "line" xy_Terreno1 xy_Terreno2 ""
    )
  )
  (command "pedit" "l" "" "j" "all" "" "")
)

(defun dibuja_Perfil ()
  (command "layer" "s" "GAP_Perfil" "")
  (setq distancia_horizontal (/ (* 1000.0 (last lista^distancia)) escala_horizontal)
  distancia_horizontal_final (polar ubicacion 0 distancia_horizontal)
  distancia_vertical_inicial (polar ubicacion (/ pi 2) (/ (* 1000.0 delta_y) escala_vertical))
    distancia_vertical_final (polar distancia_horizontal_final (/ pi 2) (/ (* 1000.0 delta_y) escala_vertical))
  )
  (command "line" distancia_vertical_inicial ubicacion distancia_horizontal_final distancia_vertical_final "")
)

(defun escribe_Texto_vertical ()
  (setq indx_cota_vertical grilla_vertical)
  (setq ubicacion_referencia (polar ubicacion pi 2)
   ubicacion_Texto_elevacion (polar distancia_vertical_inicial (/ pi 2) 9)
      ubicacion_Texto_unidad (polar distancia_vertical_inicial (/ pi 2) 5)
  )
  (command "layer" "s" "GAP_Texto-General" ""
           "text" "j" "mr" ubicacion_referencia 2.5 0 (rtos referencia 2 0)
	   "text" "j" "m" ubicacion_Texto_elevacion 2.5 0 "Elevacion"
	   "text" "j" "m" ubicacion_Texto_unidad 2 0 "[m s.n.m.]"
  )
  (repeat (fix (/ delta_y grilla_vertical))
    (setq cota_nueva (+ referencia indx_cota_vertical)
ubicacion_cota_nueva (polar ubicacion_referencia (/ pi 2) (/ (* 1000.0 indx_cota_vertical) escala_vertical))
inicio_ubicacion_marca_vertical (polar ubicacion_cota_nueva 0 1)
   fin_ubicacion_marca_vertical (polar inicio_ubicacion_marca_vertical 0 1)
    )
    (command "text" "j" "mr" ubicacion_cota_nueva 2.5 0 (rtos cota_nueva 2 0))
    (command "layer" "s" "GAP_Perfil" "")
    (command "line" inicio_ubicacion_marca_vertical fin_ubicacion_marca_vertical "")
    (if (= grilla nil)
      (progn 
        (command "layer" "s" "GAP_Malla-Perfil" "")
        (setq inicio_grilla (polar ubicacion (/ pi 2) (/ (* 1000.0 indx_cota_vertical) escala_vertical))
	         fin_grilla (polar inicio_grilla 0 (/ (* 1000.0 (last lista^distancia)) escala_horizontal))
        )
        (command "line" inicio_grilla fin_grilla "")
      )
    )  
    (command "layer" "s" "GAP_Texto-General" "")
    (setq indx_cota_vertical (+ indx_cota_vertical grilla_vertical))
  )
)

(defun escribe_Texto_horizontal ()
  (setq indx_cota_horizontal grilla_horizontal)
  (setq ubicacion_cero (polar ubicacion (* (/ pi 2) 3) 2)
ubicacion_Texto_distancia (polar distancia_horizontal_final (* (/ pi 2) 3) 10)
ubicacion_Texto_unidad (polar distancia_horizontal_final (* (/ pi 2) 3) 14)
     ubicacion_seccion (polar (polar ubicacion 0 (/ distancia_horizontal 2)) (* (/ pi 2) 3) 20)
  )	 
  (command "layer" "s" "GAP_Texto-General" ""
           "text" "j" "mr" ubicacion_cero 2.5 90 "0"
	   "text" "j" "m" ubicacion_Texto_distancia 2.5 0 "Distancia"
	   "text" "j" "m" ubicacion_Texto_unidad 2 0 "[m]"
  )
  (repeat (fix (/ (/ (* escala_horizontal distancia_horizontal) 1000.0) grilla_horizontal))
    (setq distancia_nueva (+ 0 indx_cota_horizontal)
ubicacion_distancia_nueva (polar ubicacion_cero 0 (/ (* 1000.0 indx_cota_horizontal) escala_horizontal))
inicio_ubicacion_marca_horizontal (polar ubicacion_distancia_nueva (/ pi 2) 1)
   fin_ubicacion_marca_horizontal (polar inicio_ubicacion_marca_horizontal (/ pi 2) 1)
    )
    (command "text" "j" "mr" ubicacion_distancia_nueva 2.5 90 (rtos distancia_nueva 2 0))
    (command "layer" "s" "GAP_Perfil" "")
    (command "line" inicio_ubicacion_marca_horizontal fin_ubicacion_marca_horizontal "")
    (if (= grilla nil)
      (progn
        (command "layer" "s" "GAP_Malla-Perfil" "")
        (setq inicio_grilla (polar ubicacion 0 (/ (* 1000.0 indx_cota_horizontal) escala_horizontal))
	         fin_grilla (polar inicio_grilla (/ pi 2) (/ ( * 1000.0 delta_y) escala_vertical))
        )
        (command "line" inicio_grilla fin_grilla "")
      )
    )  
    (command "layer" "s" "GAP_Texto-General" "")
    (setq indx_cota_horizontal (+ indx_cota_horizontal grilla_horizontal))
  )
  (command "clayer" 0)
)

(defun block ()
  (command "layer" "s" "GAP_Simbolo" "")
  (command "insert" "H:\\Informacion_GOLDER\\04-Info_Tecnica_Golder\\4600_Geomática\\Libreria\\Ingenieria\\Seccion Mostrada hv.dwg"
	            ubicacion_seccion
	            1
	            1
	            0
                    (strcat "Escala Horizontal 1:" (rtos escala_horizontal 2 0))
	            "A"
	            ""
                    (strcat "Escala Vertical 1:" (rtos escala_vertical 2 0))
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

(defun c:gpae ()
  (datos_de_entrada)
  (leer^archivo^entrada)
  (modificar_referencia)
  (escalas)
  (define_grillas)
  (ubicacion_Perfil)
  (dibuja_Terreno)
  (dibuja_Perfil)
  (escribe_Texto_vertical)
  (escribe_Texto_horizontal)
  (block)
  (datos^salida)
)

(prompt "\nRutina Generar Perfil con Archivo Entrada Versión 1.0 Cargada.\nDigita < GPAE > Para Comenzar")

(princ)