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
  (setq archivo^datos (open (getfiled "Selecciona Archivo de Datos" "c:\\" "csv" 8) "r"))
  (setq archivo^scr (open (getfiled "Selecciona Carpeta de Destino" "c:\\" "scr" 1) "w"))
  (setq path^block (getfiled "Selecciona Block" "H:\\Informacion_GOLDER\\04-Info_Tecnica_Golder\\4600_Geomática\\Libreria\\Ingenieria\\" "dwg" 8))
  (initget 7)
  (setq escala (getreal "\nIngresa Escala del Block : "))
)

(defun desarrollo ()
  (write-line "SETVAR ANGDIR 1\nSETVAR AUNITS 0\nSETVAR OSMODE 0\nLAYER S GAP_Calicatas " archivo^scr)
  (while (setq linea (read-line archivo^datos))
    (setq lista^posicion '()
                    indx 1
    )
    (repeat (strlen linea)
      (if (= (substr linea indx 1) ";")
        (setq lista^posicion (cons indx lista^posicion))
      )
      (setq indx (1+ indx))
    )
    (setq lista^posicion (reverse lista^posicion))
    (write-line (strcat "Insert "
			path^block
			" "
			(substr linea 1 (- (nth 0 lista^posicion) 1))
			","
			(substr linea (+ (nth 0 lista^posicion) 1) (- (- (nth 1 lista^posicion) (nth 0 lista^posicion)) 1))
			" "
			(rtos (/ (* 1.0 escala) 1000.0) 2 2)
			" "
			(rtos (/ (* 1.0 escala) 1000.0) 2 2)
			" "
			(rtos 0 2 0)
			" "
			(substr linea (+ (nth 1 lista^posicion) 1) (- (strlen linea) (nth 1 lista^posicion)))
		)
      archivo^scr
    )
  )
  (write-line "SETVAR ANGDIR 0\nCLAYER 0" archivo^scr)
  (alert "Se ha creado archivo *.scr")
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
  (close archivo^scr)
  (close archivo^datos)
)

(defun c:icsa ()
  (datos^entrada)
  (desarrollo)
  (datos^salida)
  (princ)
)

(prompt "\nRutina Insertar Calicata-Sondaje con Atributos Versión 6.0 Cargada.\nDigita < ICSA > Para Comenzar")

(princ)