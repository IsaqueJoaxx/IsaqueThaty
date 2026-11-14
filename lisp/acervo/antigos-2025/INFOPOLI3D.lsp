(defun c:InfoPoli3D ( / ent obj elev len)
  (setq ent (car (entsel "\nSelecione uma polilinha 3D: ")))
  (if ent
    (progn
      (setq obj (vlax-ename->vla-object ent))
      (if (= (vla-get-objectname obj) "AcDb3dPolyline")
        (progn
          (setq len (vla-get-length obj))
          (setq elev (vla-get-elevation obj))
          (princ (strcat "\nComprimento da polilinha: " (rtos len 2 2)))
          (princ (strcat "\nElevação (cota): " (rtos elev 2 2)))
        )
        (princ "\nA entidade selecionada não é uma polilinha 3D.")
      )
    )
    (princ "\nNenhuma entidade selecionada.")
  )
  (princ)
)
