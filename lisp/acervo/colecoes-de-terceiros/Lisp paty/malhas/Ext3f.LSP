;;;    EXT3F.LSP
;;;    Criado em 27/12/2004 por Roger Scapini Marques
;;;
;;;Esse programa pode ser usado gratuitamente.
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;ext3f
;Transforma 3dfaces em sólidos.
;
(defun c:ext3f (/ sel cont lista3dface prec ed pt1 pt2 pt3 pt4 lista novalista flag menorz maiorzlocal altura)
  (command "_.UNDO" "_BEGIN")
  (setq sel         (ssget '((0 . "3DFACE")))
        cont        0
        lista3dface ()
        prec        0.00001
  )
  (repeat (sslength sel)
    (setq ed        (entget (ssname sel cont))
          pt1       (cdr (assoc 10 ed))
          pt2       (cdr (assoc 11 ed))
          pt3       (cdr (assoc 12 ed))
          pt4       (cdr (assoc 13 ed))
          cont      (1+ cont)
          lista     (list pt1 pt2 pt3 pt4)
          novalista lista
          flag      T
    )
    (if (equal (nth 0 lista) (nth 1 lista) prec)
      (setq novalista (list (nth 1 lista) (nth 2 lista) (nth 3 lista)))
    )
;;;;;;;;;;
    (if (= (length novalista) 4)
      (if (equal (nth 0 lista) (nth 2 lista) prec)
        (setq
          novalista (list (nth 1 lista) (nth 2 lista) (nth 3 lista))
        )
      )
      (if (= (length novalista) 3)
        (if (equal (nth 0 lista) (nth 2 lista) prec)
          (setq flag nil)
        )
      )
    )
;;;;;;;;;;
    (if (= (length novalista) 4)
      (if (equal (nth 0 lista) (nth 3 lista) prec)
        (setq
          novalista (list (nth 1 lista) (nth 2 lista) (nth 3 lista))
        )
      )
      (if (= (length novalista) 3)
        (if (equal (nth 0 lista) (nth 3 lista) prec)
          (setq flag nil)
        )
      )
    )
;;;;;;;;;;
    (if (= (length novalista) 4)
      (if (equal (nth 1 lista) (nth 2 lista) prec)
        (setq
          novalista (list (nth 0 lista) (nth 2 lista) (nth 3 lista))
        )
      )
      (if (= (length novalista) 3)
        (if (equal (nth 1 lista) (nth 2 lista) prec)
          (setq flag nil)
        )
      )
    )
;;;;;;;;;;
    (if (= (length novalista) 4)
      (if (equal (nth 1 lista) (nth 3 lista) prec)
        (setq
          novalista (list (nth 0 lista) (nth 2 lista) (nth 3 lista))
        )
      )
      (if (= (length novalista) 3)
        (if (equal (nth 1 lista) (nth 3 lista) prec)
          (setq flag nil)
        )
      )
    )
;;;;;;;;;;
    (if (= (length novalista) 4)
      (if (equal (nth 2 lista) (nth 3 lista) prec)
        (setq
          novalista (list (nth 0 lista) (nth 1 lista) (nth 3 lista))
        )
      )
      (if (= (length novalista) 3)
        (if (equal (nth 2 lista) (nth 3 lista) prec)
          (setq flag nil)
        )
      )
    )
;;;;;;;;;;
    (if flag
           (if (= (length novalista) 4)
             (progn
               (setq novalista (list (nth 0 lista) (nth 1 lista) (nth 2 lista))
                     novalista (vl-sort novalista (function (lambda (e1 e2) (> (caddr e1) (caddr e2)))))
                     lista3dface (cons novalista lista3dface)
                     novalista (list (nth 0 lista) (nth 2 lista) (nth 3 lista))
                     novalista (vl-sort novalista (function (lambda (e1 e2) (> (caddr e1) (caddr e2)))))
                     lista3dface (cons novalista lista3dface)
               )
             )
             (progn
               (setq novalista (vl-sort novalista (function (lambda (e1 e2) (> (caddr e1) (caddr e2)))))
                     lista3dface (cons novalista lista3dface)
               )
             )
           )
    )
  )
  (if lista3dface
    (progn (setq menorz (caddr (car (car lista3dface))))
           (foreach item lista3dface
             (if (< (caddr (caddr item)) menorz)
               (setq menorz (caddr (caddr item)))
             )
           )
           (foreach item lista3dface
             (setq pt1         (car item)
                   pt2         (cadr item)
                   pt3         (caddr item)
                   maiorzlocal (caddr pt1)
                   altura      (- maiorzlocal menorz)
             )
             (if (> altura 0.0)
               (progn (command "_pline" pt1 pt2 pt3 "_close")
                      (command "_extrude" "_last" "" (* -1 altura) "")
                      (command "_slice" "_last" "" "_none" pt1 "_none" pt2 "_none" pt3 "_none" (list (car pt3) (cadr pt3) (- (caddr pt3) 100.0)))
               )
             )
           )
    )
  )
  (command "_.UNDO" "_END")
  (prin1)
)
