;; Lisp Calcular/Desenhar Percentual de Inclinação
;; ((Altura Base / Comprimento Base) * 100)
;; André Canalles 08.09.2007 (Revisada por Denis Cesar 17.06.2011)

(defun c:lk (/ Inc-Comp ptox ptoy ptoy-1 dist1 dist2 ang1 ang2 altu perc LnYN OsnVar inclinação distancia)
  (while
  (setvar "DIMZIN" 1)
  (setq OsnVar (getvar "OSMODE"))
  (if (= ptox nil)
    (setq ptox (getpoint "\nClique no primeiro ponto <ENTER para inserir linha>: "))
  )
  (if (= ptox nil)
    (progn
      (setq LnYN 1)
      (setq ptox (getpoint "\nClique no primeiro ponto da rampa: "))
    )
  )
  (if (= Inc-Comp nil)
    (setq ptoy (getpoint ptox "\nClique no segundo ponto da rampa <ENTER para digitar inclinação\\comprimento>: "))
  )
  (if (= ptoy nil)
    (setq Inc-Comp 1)
  )

  (if (= Inc-Comp 1)
    (progn
      (setq LnYN 1
	    inclinação (getreal "\nInforme a inclinação da rampa em porcentagem(\"%\"): ")
	    distancia (getreal "\nInforme o comprimento da rampa: ")
	    ptoy-1 (polar (polar ptox 0 100) (/ pi 2) inclinação)
	    ptoy (polar ptox (angle ptox ptoy-1) distancia)
      )
    )
  )

  (if (= LnYN 1)
    (progn
      (setvar "OSMODE" 0)
      (setvar "CMDECHO" 0)
      (command "line" ptox ptoy "")
      (setvar "CMDECHO" 1)
      (setvar "OSMODE" OsnVar)
    )
  )
  
  (setq dist1 (distance ptox ptoy))
  (setq dist2 (rtos dist1 2 2))
  (setq ang1 (angle ptox ptoy))
  (setq ang2 (angtos ang1 0 4))
  (setq comp (- (car ptoy) (car ptox)))
  (setq altu (- (cadr ptoy) (cadr ptox)))
  (setq perc (rtos (* (/ altu comp) 100) 2 2))

  (princ (strcat "\nComprimento Base = " (rtos comp 2 2) " ; Altura Base = " (rtos altu 2 2) " ; Comprimento Inclinado = " dist2 "\nÂngulo Inclinação = " ang2 " ; Percentual Inclinação = " perc "%"))

  (setvar "OSMODE" 0)
  (setvar "CMDECHO" 0)
;;;  (command "DONUT" 0 (getvar "textsize") ptox ptoy "")
  (command "TEXT" "J" "BC" (polar ptox ang1 (/ dist1 2)) "" ang2 (strcat "i=" perc "%"))
;;;  (command "MTEXT" (polar ptox ang1 (/ dist1 2)) "J" "MC" "R" ang2 "W" dist1 (strcat "i=" perc "%") (strcat "d=" dist2 "m") "")
  (setvar "CMDECHO" 1)
  (setvar "OSMODE" OsnVar)
  (setq ptox ptoy)
  )

  (princ)

);defun