(DEFUN C:AROBM2 () (command "AREA" "O"))
(prompt "Tecle <AROBM2> para iniciar o comando.")
(setq S1 (entsel)
(command "area" "e" S1)
(princ)
(alert
  "CALCULO DE ÁREA - POLIGONO FECHADO.\n\nDigite ( AROBM2 ) para iniciar o programa!!!"
)