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
  (if (= (findfile "Captura Block.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "Captura Block.lsp"))
      (captura^block)
    )
  )
  (if (= (findfile "GAP_Romans.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "GAP_Romans.lsp"))
      (GAP_Romans)
    )
  )
  (setq lunits^antiguo (getvar "lunits")
     lista^datos^block '()
	    indx^block 0
  )
  (setvar "lunits" 2)
  (initget (+ 1 4))
  (setq decimales (getint "\nIngresa Numero Decimales: "))
  (while (= (setq ubicacion^cuadro (getpoint "\nUbicacion Cuadro Coordenadas: ")) nil)
    (alert "\nUbicacion Cuadro Coordenadas")
  )
)

(defun proceso ()
  (repeat (sslength grupo^block)
    (setq entidad^block (ssname grupo^block indx^block)
	     indx^block (1+ indx^block)
         listasoc^block (entget entidad^block)
	     este^block (rtos (nth 0 (cdr (assoc 10 listasoc^block))) 2 decimales)
	    norte^block (rtos (nth 1 (cdr (assoc 10 listasoc^block))) 2 decimales)
          entidad^block (entnext entidad^block)
         listasoc^block (entget entidad^block)
   nombre^vertice^block (cdr (assoc 1 listasoc^block))
    )
    (numero)
    (setq lista^datos^block (cons (list numero^vertice^block nombre^vertice^block este^block norte^block) lista^datos^block))
  )
)

(defun numero ()
  (setq posicion^string 1
	  lista^vertice '()
  )
  (repeat (strlen nombre^vertice^block)
    (setq caracter (substr nombre^vertice^block posicion^string 1)
   posicion^string (1+ posicion^string)
    )
    (if (and (>= (ascii caracter) 48) (<= (ascii caracter) 57))
      (setq lista^vertice (cons caracter lista^vertice))
    )
    (setq numero^vertice^block (atoi (apply 'strcat (reverse lista^vertice))))
  )
)

(defun genera^lineas^cuadro^coordenadas ()
  (setq vertice^opuesto^ubicacion^cuadro (polar (polar ubicacion^cuadro 0 65) (* (/ pi 2) 3) (+ 12 (* 5 (sslength grupo^block))))
	           indx^linea^horizontal 0
  )
  (command "layer" "s" "GAP_Contorno" ""
           "rectang" ubicacion^cuadro vertice^opuesto^ubicacion^cuadro
	   "line" (polar ubicacion^cuadro (* (/ pi 2) 3) 6)
	          (polar (polar ubicacion^cuadro (* (/ pi 2) 3) 6) 0 65) ""
	   "line" (polar ubicacion^cuadro (* (/ pi 2) 3) 12)
	          (polar (polar ubicacion^cuadro (* (/ pi 2) 3) 12) 0 65) ""
	   "layer" "s" "GAP_Contorno-Secundario" ""
	   "line" (polar vertice^opuesto^ubicacion^cuadro pi 26)
	          (polar (polar vertice^opuesto^ubicacion^cuadro pi 26) (/ pi 2) (+ 6 (* 5 (sslength grupo^block)))) ""
	   "line" (polar vertice^opuesto^ubicacion^cuadro pi 50)
	          (polar (polar vertice^opuesto^ubicacion^cuadro pi 50) (/ pi 2) (+ 6 (* 5 (sslength grupo^block)))) ""
  )
  (repeat (- (sslength grupo^block) 1)
    (command "line" (polar ubicacion^cuadro (* (/ pi 2) 3) (+ 12 (* 5 (car (nth indx^linea^horizontal lista^datos^block)))))
                    (polar (polar ubicacion^cuadro (* (/ pi 2) 3) (+ 12 (* 5 (car (nth indx^linea^horizontal lista^datos^block))))) 0 65) ""
    )
    (setq indx^linea^horizontal (+ indx^linea^horizontal 1))
  )
  (command "layer" "s" "GAP_Texto-General" ""
	   "text" "j" "m" (polar (polar ubicacion^cuadro 0 32.5) (* (/ pi 2) 3) 3) 2.5 0 "CUADRO DE COORDENADAS"
	   "change" (entlast) "" "p" "c" 3 ""
	   "text" "j" "m" (polar (polar ubicacion^cuadro 0 7.5) (* (/ pi 2) 3) 9) 2.5 0 "VERTICE"
	   "text" "j" "m" (polar (polar ubicacion^cuadro 0 27) (* (/ pi 2) 3) 9) 2.5 0 "ESTE"
	   "text" "j" "m" (polar (polar ubicacion^cuadro 0 52) (* (/ pi 2) 3) 9) 2.5 0 "NORTE"
  )
)

(defun genera^datos^cuadro^coordenadas ()
  (setq indx^dato^horizontal 0)
  (repeat (sslength grupo^block)
    (command "text" (polar
		      (polar ubicacion^cuadro 0 3.5) (* (/ pi 2) 3) (+ 10.75 (* 5 (car (nth indx^dato^horizontal lista^datos^block))))
	            )
		    2.5 0 (nth 1 (nth indx^dato^horizontal lista^datos^block))
             "text" (polar
		      (polar ubicacion^cuadro 0 17) (* (/ pi 2) 3) (+ 10.75 (* 5 (car (nth indx^dato^horizontal lista^datos^block))))
	            )
		    2.5 0 (nth 2 (nth indx^dato^horizontal lista^datos^block))
             "text" (polar
		      (polar ubicacion^cuadro 0 41) (* (/ pi 2) 3) (+ 10.75 (* 5 (car (nth indx^dato^horizontal lista^datos^block))))
	            )
		    2.5 0 (nth 3 (nth indx^dato^horizontal lista^datos^block))
    )	     
    (setq indx^dato^horizontal (+ indx^dato^horizontal 1))
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
  (setvar "lunits" lunits^antiguo)
)

(defun c:cvc ()
  (datos^entrada)
  (proceso)
  (genera^lineas^cuadro^coordenadas)
  (genera^datos^cuadro^coordenadas)
  (datos^salida)
)

(prompt "\nRutina Captura Vertice Coordenada Version 6.0 Cargada.\nDigita < CVC > Para Comenzar")

(princ)