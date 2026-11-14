;|

	PROGRAMAÇÃO DE SONDAGENS: Este programa insere no ponto desejado
	o símbolo do tipo da sondagem e os dados relativos a ela.
	FASE BETA (versão 1.0)

	Versão 1.0: versão inicial;

	 _______________________________________________________________
	|								|
	|	   Criado por: Humberto Guireli Gottardello		|
	|								|
	|		31/AGOSTO/2009 - versão 1.0			|
	|								|
	|      Este software é protegido pelas leis de "copyright".	|
	| Qualquer problema, favor reportar ao desenvolvedor. Obrigado!	|
	|								|
	|		   gottardello@hotmail.com			|
	|								|
	*_______________________________________________________________*

|;

(defun c:SP ()

(setq OSMODEANTERIOR (getvar "osmode"))

(command "units" "2" "4" "5" "4" "e" "y")

(setvar "osmode" 0)

(setvar "textstyle" (strcat "standard"))

(setvar "textsize" 2)

(command ".ucs" "w")

(setq POINT (getpoint "\nSelecione o ponto de inserção da sondagem à percussão: "))

(setq POINTx (car POINT)
      POINTy (cadr POINT))

(setq NOME (getstring "\nDigite o sequencial de sondagem: "))

(command ".ucs" "m" (strcat (rtos POINTx) "," (rtos POINTy)))

(command ".ucs" "v")

(command ".circle" "0,0" "2.5")
(command ".line" "0,4" "0,-4" ""
	 ".line" "-4,0" "4,0" ""
	 ".line" "6,-6" "34,-6" "")
(command ".text" "6,-5.25" "2" "0" (strcat "SP-" NOME)
	 ".text" "6,-8.75" "2" "0" (strcat "N=" (rtos POINTy))
	 ".text" "6,-11.5" "2" "0" (strcat "E=" (rtos POINTx)))

(command ".zoom" "w" "-4,4" "4,-4")

(setq P1 (list 1 0)
      P2 (list 0 1))

(setq SELECTION (ssget "C" P1 P2))

(command ".layiso" "p" "")

(command ".boundary" "1,-1" "-1,1" "")

(command ".hatch" "solid" "1,0" "0,1" "")

(command ".erase" "1,0" "0,1" "")

(command ".layuniso")

(command ".zoom" "p")

(command ".line" "0,0" "6,-6" "")

(command ".ucs" "w")

(setvar "osmode" OSMODEANTERIOR)

)

;--------------------------------------------------------------------

(defun c:SM ()

(setq OSMODEANTERIOR (getvar "osmode"))

(command "units" "2" "4" "5" "4" "e" "y")

(setvar "osmode" 0)

(setvar "textstyle" (strcat "standard"))

(setvar "textsize" 2)

(command ".ucs" "w")

(setq POINT (getpoint "\nSelecione o ponto de inserção da sondagem mista: "))

(setq POINTx (car POINT)
      POINTy (cadr POINT))

(setq NOME (getstring "\nDigite o sequencial de sondagem: "))

(command ".ucs" "m" (strcat (rtos POINTx) "," (rtos POINTy)))

(command ".ucs" "v")

(command ".circle" "0,0" "2.5")
(command ".line" "0,4" "0,-4" ""
	 ".line" "-4,0" "4,0" ""
	 ".line" "6,-6" "34,-6" "")
(command ".text" "6,-5.25" "2" "0" (strcat "SM-" NOME)
	 ".text" "6,-8.75" "2" "0" (strcat "N=" (rtos POINTy))
	 ".text" "6,-11.5" "2" "0" (strcat "E=" (rtos POINTx)))

(command ".zoom" "w" "-4,4" "4,-4")

(setq P1 (list 1 0)
      P2 (list 0 1))

(setq SELECTION (ssget "C" P1 P2))

(command ".layiso" "p" "")

(command ".boundary" "1,-1" "-1,1" "")

(command ".hatch" "solid" "1,0" "0,1" "")

(command ".erase" "1,0" "0,1" "")

(command ".layuniso")

(command ".zoom" "p")

(command ".line" "0,0" "6,-6" "")

(command ".ucs" "w")

(setvar "osmode" OSMODEANTERIOR)

)

;------------------------------------------------------------------

(defun c:ST ()

(setq OSMODEANTERIOR (getvar "osmode"))

(command "units" "2" "4" "5" "4" "e" "y")

(setvar "osmode" 0)

(setvar "textstyle" (strcat "standard"))

(setvar "textsize" 2)

(command ".ucs" "w")

(setq POINT (getpoint "\nSelecione o ponto de inserção da sondagem a trado: "))

(setq POINTx (car POINT)
      POINTy (cadr POINT))

(setq NOME (getstring "\nDigite o sequencial de sondagem: "))

(command ".ucs" "m" (strcat (rtos POINTx) "," (rtos POINTy)))

(command ".ucs" "v")

(command ".circle" "0,0" "2.5")
(command ".line" "6,-6" "34,-6" "")
(command ".text" "6,-5.25" "2" "0" (strcat "ST-" NOME)
	 ".text" "6,-8.75" "2" "0" (strcat "N=" (rtos POINTy))
	 ".text" "6,-11.5" "2" "0" (strcat "E=" (rtos POINTx)))

(command ".line" "0,0" "6,-6" "")

(command ".ucs" "w")

(setvar "osmode" OSMODEANTERIOR)

)

;-------------------------------------------------------------------

(defun c:PI ()

(setq OSMODEANTERIOR (getvar "osmode"))

(command "units" "2" "4" "5" "4" "e" "y")

(setvar "osmode" 0)

(setvar "textstyle" (strcat "standard"))

(setvar "textsize" 2)

(command ".ucs" "w")

(setq POINT (getpoint "\nSelecione o ponto de inserção do poço de inspeção: "))

(setq POINTx (car POINT)
      POINTy (cadr POINT))

(setq NOME (getstring "\nDigite o sequencial de poço de inspeção: "))

(command ".ucs" "m" (strcat (rtos POINTx) "," (rtos POINTy)))

(command ".ucs" "v")

(command ".rectangle" "-2,2" "2,-2")
(command ".line" "6,-6" "34,-6" "")
(command ".text" "6,-5.25" "2" "0" (strcat "PI-" NOME)
	 ".text" "6,-8.75" "2" "0" (strcat "N=" (rtos POINTy))
	 ".text" "6,-11.5" "2" "0" (strcat "E=" (rtos POINTx)))

(command ".line" "0,0" "6,-6" "")

(command ".ucs" "w")

(setvar "osmode" OSMODEANTERIOR)

)

;-------------------------------------------------------------------

(princ "\nDesenvolvido por Humberto Guireli Gottardello")(princ)