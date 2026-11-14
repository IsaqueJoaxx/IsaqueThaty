(defun c:cut ()
  (while (ssget "x" (list '(0 . "LINE")))
    (if (= (findfile "Unir Topografia.lsp") nil)
      (progn
        (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
        (quit)
      )
      (progn
        (load (findfile "Unir Topografia.lsp"))
        (c:ut)
      )
    )
  )
)

(prompt "\nRutina Ciclo Unir Topografia Version 1.0 Cargada.\nDigita < CUT > Para Comenzar")