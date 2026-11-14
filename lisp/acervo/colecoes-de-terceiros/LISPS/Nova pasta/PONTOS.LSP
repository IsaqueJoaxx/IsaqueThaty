
;  *************************************************************************
;  *                           ### CURVAS ###                              *
;  *              ***>  Interface VAICIN(Harry) QuicSURF <***              *
;  *                                                                       *
;  *                       Paulo Pinto             inicio:  22/08/93       *
;  *                                     ultima alteracao:                 *
;  *************************************************************************



;
;  ****> LEPONTO  (Le arquivo tipo QS e insere pontos no desenho)
;

(defun c:LePonto ( /
                     NomeArq         ; Nome no arquivo .QS que sera lido
                     NroArq          ; Nro de referencia do arquivo
                     HandArq         ; Handle de abertura do arquivo
                     Ponto           ; Ponto lido no arquivo
                     oldlay          ; LAYER ativa no inicio do processamento
                 )

   ; ***> Guarda nome da LAYER ativa
   (setq oldlay (getvar "CLAYER"))


   ; ***> Verifica LAYER "PONTOS000"
   (if (not (testlay "PONTOS000"))
      (command "LAYER"
               "N"
               "PONTOS000"
               ""
      )
   )



   ; ***> Captura nome do arquivo QS:
   (while (= NomeArq nil)
      (setq NomeArq (getstring "\nNome do arquivo (AAAAA.QS)= "))
      (if (not (setq HandArq (open NomeArq "r")))
         (progn
            (setq NomeArq nil
            )
            (Print "\nArquivo nao encontrado!!!")
         )
      )
   )

   ; ***> Captura NroArq
   (while (= NroArq nil)
      (setq NroArq (getint "\nNro de referencia do arquivo (1 a 255): "))
      (if (and NroArq
               (or (< NroArq 1) (> NroArq 255))
          )
          (setq NroArq nil)
      )
   )

   ; ***> Seta cor p/ pontos que serao lidos:
   (COMMAND "COLOR"
             NroArq
   )


   ; ***> Le pontos do arquivo:
   (while (setq Ponto (strxyz (read-line HandArq)))
      (COMMAND "POINT"
                ponto
      )
   )

   ; ***> Fecha arquivo:
   (close HandArq)

   ; ***> Seta cor p/ "BYLAYER"
   (COMMAND "COLOR"
            "BYLAYER"
   )


   ; ***> Volta para layer original:
   (command "LAYER" "S" oldlay "")

)

;
;         ***> Grava pontos selecionados em arquivo tipo ".QS"  <***
;                                      Paulo Pinto 22/08/93
;                                                    /  /   (ultima alteracao)

(defun c:gravaponto ( /
                      NomeArq         ; Nome no arquivo .QS que sera lido
                      NroArq          ; Nro de referencia do arquivo
                      HandArq         ; Handle de abertura do arquivo
                      texto           ; texto p/ gravar
                      nrolista        ; nro elementos selecionados
                      listasel        ; lista com entidades selecionadas
                      elem            ; elemento da lista
                      l               ; contador
                      total           ; numero de pontos gravados
                    )

   ; ***> Captura nome do arquivo QS:
   (while (or
             (= NomeArq nil)
             (= NomeArq "")
          )
      (setq NomeArq (getstring "\nNome do arquivo (C:/BBB/AAAAA.QS)= ")
      )
      (if (setq HandArq (open NomeArq "r"))
         (progn
            (if (/= (strcase (getstring "\nArquivo Existe. Regrava? (S/N) "))
                     "S"
                )
               (setq NomeArq nil
               )
            )
            (close HandArq)
         )
      )
   )
   (setq HandArq (open NomeArq "w"))

   ; ***> Captura elementos
   (setq listasel  (ssget)
         total      0
   )
 
   (if listasel                                    ; se foi selecionado algo
      (progn
         (setq nrolista (sslength listasel)
               l        0
         )

         (while (< l nrolista)
            (setq elem (entget (ssname listasel l)))

            (if (= "POINT" (cdr(assoc 0 elem)))
                (progn
                   (setq texto (rtos (car (cdr (assoc 10 elem))) 2 3)
                         texto (strcat texto " "
                                       (rtos (cadr  (cdr (assoc 10 elem))) 2 3)
                               )
                         texto (strcat texto " "
                                       ;(rtos (cdr (assoc 38 elem)) 2 3)
                                       (rtos (caddr (cdr (assoc 10 elem))) 2 3)
                               )
                   )
                   (write-line texto HandArq)
                   (print (setq total (1+ total)))
                )
            )                      ; fim do if
            (setq l (1+ l))
         )                            ; fim do while
      )                               ; fim do progn
   )                                  ; fim do if
   (close HandArq)
)                                     ; fim do programa





;
;             ***> Testa existencia de  LAYER <***
;                    (se nao existir, cria)
;

(defun testlay (nome)
   ; teste se LAYER "nome" esta definida
   (if (tblsearch "LAYER" nome)
      1    ; Retorna .T. se existe
      nil  ;Retorna nil se nao existe
   )
)


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;             ***> Testa altura de  ESTILO <***
;  (retirado do disco INSIDE AutoLISP)
;
;
; (if (= (setq txthgt (cdr (assoc 40 (tblsearch "STYLE" (getvar "TEXTSTYLE"))))) 0) ;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;


;
;             ***> Retorna lista (x y z) a partir de um string <***
;

(defun strxyz (st / lista x y z posi comp)
   ; teste se recebeu lista
   (if st
      (progn
                           ; ***> Le "x"
         (setq posi 1
               comp 1
         )
         ; ***> pula eventuais espacos iniciais
         (while (= (substr st posi 1) " ")
            (setq posi (1+ posi)
            )
         )
         ; ***> conta numero de caracteres de "x"
         (while (/= (substr st (+ posi comp) 1) " ")
            (setq comp (1+ comp)
            )
         )
         ; ***> le valor de "x"
         (setq x (atof (substr st posi))
         )

                           ; ***> Le "y"
         (setq posi (+ posi comp 1)
               comp 1
         )
         ; ***> pula espacos intermediarios
         (while (= (substr st posi 1) " ")
            (setq posi (1+ posi)
            )
         )
         ; ***> conta numero de caracteres de "y"
         (while (/= (substr st (+ posi comp) 1) " ")
            (setq comp (1+ comp)
            )
         )
         ; ***> le valor de "y"
         (setq y (atof (substr st posi))
         )

                           ; ***> le valor de "z"
         (setq z (atof (substr st (+ posi comp)))
         )

                           ; ***> retorna lista (x y z)
         (list x y z)
      )

      ; ***> Retorna nil se nao foi enviada string
      nil  
   )
)

