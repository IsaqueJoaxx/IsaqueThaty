(defun c:ip ()
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
  (if (/= (setq grupo (ssget "x" (list '(0 . "INSERT") '(2 . "PATH")))) nil)
    (progn
      (setq entidad (ssname grupo 0)
            entidad (entnext entidad)
           listasoc (entget entidad)
           par_path (assoc 1 listasoc)
      )
      (if (equal (cdr par_path) (strcat (getvar "dwgprefix") (getvar "dwgname")))
        ()
        (progn
          (setq nuevo_par_path (cons 1 (strcase (strcat (getvar "dwgprefix") (getvar "dwgname"))))
                      listasoc (subst nuevo_par_path par_path listasoc)
          )
          (entmod listasoc)
	  (entupd entidad)
          (command "regen")
        )
      )
    )
    (progn
      (setvar "attdia" 0)
      (command "clayer" "GAP_Formato")
      (command "insert" "H:\\Informacion_GOLDER\\04-Info_Tecnica_Golder\\4600_Geomática\\Libreria\\Ingenieria\\path.dwg"
      pause 1 1 pause  
      (strcase (strcat (getvar "dwgprefix") (getvar "dwgname")))
      )
    )
  )
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

(prompt "\nRutina Path Version 2.0 Cargada.\nDigita < IP > Para Comenzar")

(princ)