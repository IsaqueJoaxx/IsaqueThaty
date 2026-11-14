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
)

(defun desarrollo ()
  (while
    (= (setq entidad (car (entsel "\nSeleciona Patrón Achurado : "))) nil)
  )
  (setq indx 0)
  (setq lista '())
  (if (= (cdr (assoc 0 (entget entidad))) "HATCH")
    (progn
      (setq listasoc (entget entidad))
      (setvar "HPNAME" (cdr (assoc 2 listasoc)))
      (if (/= (cdr (assoc 2 listasoc)) "SOLID")
	(progn
          (setvar "HPSCALE" (cdr (assoc 41 listasoc)))
          (setvar "HPANG" (cdr (assoc 52 listasoc)))
        )
      )	
      (repeat (length listasoc)
	(if (or (and (= (car (nth indx listasoc)) 10)
		     (/= (nth 2 (nth indx listasoc)) 0)
		)
		(and
		  (= (car (nth indx listasoc)) 11)
		  (/= (nth 2 (nth indx listasoc)) 0)
		)  
	    )
	  (progn
	    (if	(= (member (cdr (nth indx listasoc)) lista) nil)
	      (setq lista (cons (cdr (nth indx listasoc)) lista))
	    )
	  )
	)
	(setq indx (1+ indx))
      )
      (setq lista (reverse lista)
	    ciclo (- (length lista) 1)
      )
      (command "layer" "s" (cdr (assoc 8 listasoc)) "")
      (repeat ciclo
        (command "line" (nth 0 lista) (nth 1 lista) "")
        (setq lista (cdr lista))
      )
      (command "pedit" "l"  "" "j" "all" "" "c" ""
	       "erase" entidad ""
               "bhatch" "s" "l" "" ""
      )
    )
    (alert "No Achurado")
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

(defun c:rp ()
  (datos^entrada)
  (desarrollo)
  (datos^salida)
)

(prompt "\nRutina Recuperar Patron Versión 3.0 Cargada.Digita < RP > Para Comenzar")

(princ)