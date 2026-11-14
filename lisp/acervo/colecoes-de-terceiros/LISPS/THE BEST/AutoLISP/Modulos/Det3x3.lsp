(defun det3x3 (a b c d e f g h i)
  (if (= (findfile "Det2x2.lsp") nil)
    (progn
      (alert "\nDebes revisar las opciones de configuración del sistema, alguna aplicación fue borrada o renombrada")
      (quit)
    )
    (progn
      (load (findfile "Det2x2.lsp"))
      (+ (- (* a (det2x2 e f h i)) (* b (det2x2 d f g i))) (* c (det2x2 d e g h)))
    )
  )
)