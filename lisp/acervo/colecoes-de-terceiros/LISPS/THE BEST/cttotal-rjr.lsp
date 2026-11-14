(princ "\n----- by RJRENGENHARIA  -----  http://rjrengenharia.com.br/  -----" )
(princ "\n----- rjrengenharia.com.br -- rubinhoengenheiro@yahoo.com.br -----" )
(princ "\n----- Digite \"<SOMALINHAS> para iniciar o comando....\"")

( defun c:SOMALINHAS ( / n ss soma ent ) 
( vl-load-com ) 
( setq ss ( ssget ' ( ( -4 . "<or" ) ( 0 . "circle" ) ( 0 . "arc" ) ( 0 . "LINE" ) ( 0 . "SPLINE" ) ( 0 . "LWPOLYLINE" ) ( 0 . "POLYLINE" ) ( -4 . "or>" ) ) ) 
n 0 
soma 0.0 ) 
( repeat ( sslength ss ) 
( setq ent ( ssname ss n ) 
n ( 1+ n ) 
soma ( + soma ( vlax-curve-getdistatparam ent ( vlax-curve-getendparam ent ) ) ) ) 
) 0
( prompt ( rtos soma ) ) 


) 

;(prompt "Tecle <SOMALINHAS> para iniciar o comando.")
;(princ)