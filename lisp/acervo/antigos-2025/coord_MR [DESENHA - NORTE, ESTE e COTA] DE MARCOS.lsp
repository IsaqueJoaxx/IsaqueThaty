;PRODUZIDO POR JMAF.
;DATA 17/05/2013.
;@
(defun c:coord ()
(setq pt1 (getpoint "\nIndique o Base"))
(command "layer" "m" "SETA" "c" "3" "" "")
(setq norte (cadr pt1))
(setq txt_norte (strcat "N = " (rtos norte 2 3) " m"))
(setq este (car pt1))
(setq txt_este (strcat "E = " (rtos este 2 3) " m"))
(setq cota (caddr pt1))
(setq txt_cota (strcat "Z = " (rtos cota 2 3) " m"))
(command "leader" pt1 pause "" txt_norte txt_este txt_cota "")
(command "layer" "s" "0" "")
)