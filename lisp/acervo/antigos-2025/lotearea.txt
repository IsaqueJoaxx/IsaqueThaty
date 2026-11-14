(defun c:LOTEAREA ( / ptList ent areaText cenPt area poly)
  (setq ptList nil)
  (prompt "\nClique os pontos do lote (ENTER para finalizar):")
  (while (setq pt (getpoint "\nPonto: "))
    (setq ptList (append ptList (list pt)))
  )
  (if (and ptList (> (length ptList) 2))
    (progn
      ;; Fecha o polígono
      (setq poly (entmakex
        (append
          (list '(0 . "LWPOLYLINE")
                (cons 100 "AcDbEntity")
                (cons 100 "AcDbPolyline")
                (cons 90 (length ptList))
                (cons 70 1) ; Fechado
          )
          (mapcar '(lambda (pt i)
                     (list (cons (nth (+ 10 (* 2 i)) '(10 20)) (car pt))
                           (cons (nth (+ 11 (* 2 i)) '(10 20)) (cadr pt))))
                  ptList
                  (number-sequence 0 (1- (length ptList)))
          )
          (apply 'append ptList)
        )
      ))

      ;; Calcula área
      (command "AREA" "O" poly)
      (setq area (getvar "AREA"))
      (setq cenPt (getvar "LASTPOINT"))

      ;; Insere o texto da área
      (setq areaText (strcat (rtos area 2 2) " m²"))
      (entmakex
        (list
          '(0 . "TEXT")
          (cons 10 cenPt)
          (cons 40 1.0)
          (cons 1 areaText)
          (cons 7 "Standard")
          (cons 72 1)
          (cons 73 1)
          (cons 11 cenPt)
        )
      )

      (prompt (strcat "\nÁrea do lote: " areaText))
    )
    (prompt "\nNúmero de pontos insuficiente.")
  )
  (princ)
)
