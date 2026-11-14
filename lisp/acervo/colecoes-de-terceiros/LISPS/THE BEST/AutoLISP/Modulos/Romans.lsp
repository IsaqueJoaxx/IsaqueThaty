(defun c:romans ()
  (if (= (findfile "Texto Estilo Romans.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "Texto Estilo Romans.lsp"))
      (texto^estilo^romans)
    )
  )
)