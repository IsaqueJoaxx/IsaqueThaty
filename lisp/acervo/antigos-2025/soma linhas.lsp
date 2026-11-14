(defun C:SUMPL (/ ss n ent len total)
  (vl-load-com)
  (princ "\nSelecione as polilinhas para somar: ")
  (setq ss (ssget '((0 . "LWPOLYLINE,POLYLINE"))))
  (if ss
    (progn
      (setq total 0.0)
      (setq n 0)
      (while (< n (sslength ss))
        (setq ent (ssname ss n))
        (setq len (vlax-curve-getdistatparam ent 
                   (vlax-curve-getendparam ent)))
        (setq total (+ total len))
        (setq n (1+ n))
      )
      (princ (strcat "\nComprimento total: " 
                    (rtos total 2 2) 
                    " unidades"))
    )
    (princ "\nNenhuma polilinha selecionada!"))
  (princ)
)