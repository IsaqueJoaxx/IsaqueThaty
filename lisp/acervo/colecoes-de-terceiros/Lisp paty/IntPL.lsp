
;------------------ Interpola Polilinhas ----------------------------
; Criado por Antonio J. Barbosa          -  11/08/03
; Interpola pontos em uma determinada polilinha.
; Deve-se selecionar individualmente cada polilinha e informar as cotas de início e final e
; em seguida clicar no ponto a interpolar.
; As cotas devem estar no plano 0.



(defun perpoly()
       (command "AREA" "O" objeto)
       (command "perimeter")
       (setq temp (getvar 'perimeter))
) ; defun



(defun C:intPL(/ temp cota_refp1 cota_refp2 desnTot ctot cparc ptext)
   (PROMPT "\nInterpola pontos de uma Polilinha")
   (setq txt "")

   (setq osmo (getvar "osmode"))   ; Armazena o Osmode
   (setq layerold (getvar "clayer"))   ; armazena o layer atual
   

   (command "osmode" "63")    ;Seta o osmode 
   (setq objeto(entsel "\nSelecione a linha/polilinha.."))	
   (setq tipo  (cdr (assoc 0 (entget (car objeto)))))
   (princ tipo)
   (princ " selecionada")

   ;Calcula o desenvolvimento da entidade
		
         (if(and(/= tipo "LINE")(/= tipo "ARC"))
  	   (perPoly)
	   (alert "Não é uma Polilinha")
          ) ; if

          (setq ctot temp)  
          (princ (strcat "\nPerimetro = " (rtos temp 2 3)))
          
  
   ;Pede os pontos extremos de referência e cotas 
   ;(setq i_dec (/ i 100))
    (setq p1 (getpoint "\nIndique o ponto inicial "))
    (setq cota_refP1 (getreal "\nCota do Ponto Inicial..."))
    (setq p2 (getpoint "\nIndique o ponto Final: "))	  
    (setq cota_refP2 (getreal "\nCota do Ponto Final...")) 

   ;Calcula o Desnível entre os pontos de referência
    (setq desnTot(- cota_refp2 cota_refp1))

	 
    ;Cria uma nova camada
    (command "-layer" "n" "_templay" "c" "6" "" "")  
    (command "-layer" "s" "_templay" "")

;;;  Inicia o processo

(while
  (progn

    (command "osmode" "32")    ;Seta o osmode 


    
    ; Ponto a interpolar
     (setq pint (getpoint "\nIndique o ponto a interpolar: "))
     (setq local (getpoint pint "\nInforme o local para inserir a cota "))
     ;(command "layer" "s" "_templay" "")

    ;Copia a entidade
    ;Muda a camada da nova entidade copiada
     (command "copy" objeto "" "@" "@")
     (command "change" "last" "" "p" "la" "_templay" "")

     (command "break" "last" pint p2)      ; Verificar se está na linha

   ;Calcula o desenvolvimento da entidade
     (command "AREA" "O" "last")
     (command "perimeter")
     (setq temp (getvar 'perimeter))
     (setq cparc temp)
     (princ (strcat "\nPerimetro = " (rtos temp 2 3)))

     (setq temp ctot)
     (princ (strcat "\nCompr. P1-P2 = " (rtos temp 2 3)))
     (setq temp cparc)
     (princ (strcat "\nCompr. P1-P3 = " (rtos temp 2 3)))
     (setq rampa (* (/ desnTot ctot) 100))   ; calcula a rampa entre os pontos
     (setq temp rampa)
     (princ (strcat "\nRampa  = " (rtos temp 2 2)))
     (princ " % ")


     (if (> cota_refp2 cota_refp1)             ;Calcula a cota do ponto interpolado
       (setq cint (+ cota_refp1 (/(* cparc desnTot) ctot)))
       (setq cint (- cota_refp1 (/(* cparc desnTot) ctot)))
     )

     (setq temp cint)
     (princ (strcat "\nCota Interpolada = " (rtos temp 2 3)))
     (command "erase" "last" "")
     (setvar "clayer" layerold)   ;Restaura o layer original
     (setvar "cecolor" "1")       ; muda a cor para red
     (command "line" pint local "")    ; Linha de chamada para inserção da cota
     (setvar "cecolor" "2")     ; muda a cor para yellow 
  
    ;(setq local(getpoint "\nInforme o local para inserir a cota"))
    ;(initget 1)
    ;(setq alturatexto (getreal "\nQual a altura do texto: "))
    ;(initget 1)
    ;(setq rottexto (getreal "\nQual o angulo de rotação do texto: "))

    ;(command "text" "c" local alturatexto rottexto txt)     ; Imprime a Cota Interpolada
     (setq txt (rtos cint 2 3))
     (setq temp (strcat "CT=" txt))
     (command "text" local "" "" temp)     ; Imprime a Cota Interpolada	 
     (princ)
     (princ "\n ")
     (princ (strcat "\nCota Interpolada = " txt))
     (princ)
     (setvar "osmode" osmo)
     (setvar "cecolor" "BYLAYER")
   )
  )

) ; defun
  
(prompt "\nPARA EXECUTAR: IntPL")(PRINC)