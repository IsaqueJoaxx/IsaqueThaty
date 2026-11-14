;Author : Joe Pucilowski
;Company: Joseph & Associates
;Address: 7809A River Resort Lane, Tampa, FL 33617-8059
;Phone  : (813) 985-3587
;Date   : 9/19/87
;Note   : This program figures the total number of linear
;         units (feet, inches, etc) of LINEs on a specific layer.
;
(defun c:linear ()
  (setq total 0)
  (setq e (entnext))
  (setq layerpicked
    (strcase
      (getstring "\nAdd up lines on layer: ")
    )
  )
  (while e
    (setq enttyp (cdr (assoc 0 (entget e))))
    (setq layername (cdr (assoc 8 (entget e))))
    (if
      (and
        (equal enttyp "LINE")
        (equal layername layerpicked)
      )
      (progn
        (setq p1 (cdr (assoc 10 (entget e))))
        (setq p2 (cdr (assoc 11 (entget e))))
        (setq length (distance p1 p2))
        (setq total (+ length total))
        (print e)
      )
    )
    (setq e (entnext e))
  )
  (terpri)
  (strcat "     " (rtos total) "     ")
)
