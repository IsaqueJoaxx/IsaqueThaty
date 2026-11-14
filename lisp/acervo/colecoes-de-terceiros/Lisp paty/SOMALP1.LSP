;------------------Soma Linhas e Polilinhas ----------------------------
; Criado por Antonio J. Barbosa          -  03/10/02
; Computa lines, polilynes, lwpolilynes, splines e outros objetos como circulos e retangulos
; Deve-se selecionar individualmente cada objeto a ser computado e será mostrado o comprimento
; acumulado.
; Para um melhor controle, as entidades selecionadas terao sua cor alteradas para MAGENTA
; ENTER ou seleção vazia finaliza a seleção

(defun desArc()
	  	 
		 (command "lengthen" des "")
		 (princ des)
                 (setq des(getvar 'perimeter))
		 (setq temp des)
	   	 (setq des(rtos des))
	   	 (setq des(strcat "\nDesenvolvimento: " des))
	   	 (PRINC des)
	  	 (PRINC)
 ) ; defun

(defun perLin()
	(setq p1 (cdr (assoc 10 (entget (car objeto)))))
	(setq p2 (cdr (assoc 11 (entget (car objeto)))))
	(command "dist" p1 p2)
	(setq distancia(getvar 'distance))
	(setq temp distancia)
	(princ "\nA distancia é: ")
	(princ (rtos distancia 2 3))
) ; defun

(defun perOutro()
       (command "AREA" "O" objeto)
       (command "perimeter")
       (setq temp (getvar 'perimeter))
) ; defun


(defun C:SomaLP(/ temp SomaP)
   (PROMPT "\nSoma Linhas e Polilinhas.")
   (setq somaP 0)
   (setq txt "")
   (while 
         (if (= SomaP 0)
               (setq objeto(entsel "\nSelecione a linha/polilinha.."))	
               (setq objeto(entsel "\nSelecione a proxima linha/polilinha ou [ENTER] para sair"))
         )
         (setq tipo  (cdr (assoc 0 (entget (car objeto)))))
         (command "change" objeto "" "p" "c" "magenta" "")
         (princ tipo)
         (princ " selecionada")

         (if (= tipo "LINE") 
             (perLin)
          ) ; if

          (if (= tipo "ARC") 
              (desArc)
          ) ; if 
	
          (if(and(/= tipo "LINE")(/= tipo "ARC"))
  	(perOutro)
           ) ; if

           (setq SomaP (+ SomaP temp))
           (setq txt (rtos SomaP 2 3))
           (princ (strcat "\nPerimetro = " (rtos temp 2 3)))
           (princ (strcat "\nDistância Anterior = " (rtos (- SomaP temp) 2 3)))
           (princ (strcat "\nDistância Acumulada = " txt))
    )

    (setq local(getpoint "\nInforme o local para inserir o total"))
    (initget 1)
    (setq alturatexto (getreal "\nQual a altura do texto: "))
    (initget 1)
    (setq rottexto (getreal "\nQual o angulo de rotação do texto: "))
    (setq txt(strcat "P=" txt))
    (command "text" "c" local alturatexto rottexto txt)     ; Imprime a distância total
    (princ)
    (princ "\n")
    (princ (strcat "\nDistância Acumulada = " txt))
    (princ)
) ; defun
  
(prompt "\nPARA EXECUTAR: SomaLP")(PRINC)
