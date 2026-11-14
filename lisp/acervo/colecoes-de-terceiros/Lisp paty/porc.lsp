(defun w-porc (PT1 PT2 ESC-HOR ESC-VER TXT-H / porc pdif)
(setq pdif (list 	(* esc-hor (car (std-pt-  pt2 pt1))) 
			(* esc-ver (cadr (std-pt-  pt2 pt1))) 
	))

(setq porc (* (/ 100 (car pdif)) (cadr pdif) ) )

(txtjm "Porcentagem" 2 
    (polar 
	;;(std-pt+ pt1 
        (std-pt+ pt1 
		(list     (/ (car (std-pt- pt2 pt1)) 2) 
			(/ (cadr (std-pt- pt2 pt1)) 2)  
		) 
	)
	(+ (angle pt1 pt2) (/ pi 2))     
    3.0)  ;;; ponto (fin polar)

    (strcat (rtos porc 2 1) "%") ;;;texto
    (angle pt1 pt2) ;;; angulo
    txt-h ;;; altura - txt 
)

);;; end func

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun c:porc ( / pt1 pt2 str)
(if (not (jmf)) (std-error "Syntax Error"))
(if (or (not ESC-HOR) (not ESC-VER) (not txt-h)) (progn
(setq ESC-HOR 1)
(setq ESC-VER 1)
(setq txt-h 1.5)
))
;;;;---STR ESCREVA UM NUMERO:

  (if (= (setq str (getstring (STRCAT "Escala Hor: 1:<"(rtos (* 1000 ESC-hor)2 0)">"))) "") 
	(progn (print 1)) 
	(progn (setq ESC-HOR (/ (atof str) 1000 )) ))
  (if (= (setq str (getstring (STRCAT "Escala Ver: 1:<"(rtos (* 1000 ESC-VER)2 0)">"))) "") 
	(progn (print 2))
	(progn (setq ESC-VER (/ (atof str) 1000 )) ))
  (if (= (setq str (getstring (STRCAT "Altura Texto:<"(rtos txt-h 2 3)">"))) "") 
	(progn (print 3))
	(progn (setq txt-h (atof str))  ))


(WHILE T
(SETQ Pt2 (std-get2point (SETQ pt1 (getpoint "Pick no ponto inicial:")) "Pick no ponto final"))
(w-porc pt1 pt2 ESC-HOR ESC-VER txt-h)
) ;;; end while
) ;;; end main
(prompt "PORC para marcar porcentajem")
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;