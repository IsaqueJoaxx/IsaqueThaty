
;  *************************************************************************
;  *                           ###  DIGIZ  ###                             *
;  *         ***> Digitalizacao de pontos com coordenada "Z" <***          *
;  *                                                                       *
;  *         Paulo Pinto (031) 283-2777            inicio:  11/03/95       *
;  *                                     ultima alteracao:                 *
;  *************************************************************************

(DEFUN C:digiz  (
                 /
                 oldlay oldech oldblp oldpbx olderr
                 pto
                 z
                 lay
                )


   ; ***> Captura variaveis ambientais:
   (setq oldlay (getvar "CLAYER")
         oldech (getvar "CMDECHO")
         oldblp (getvar "BLIPMODE")
         olderr *ERROR*
   )

   ; ***> Configura ambiente:
   (mapcar 'setvar (list "CMDECHO" "BLIPMODE") (list 0 1))

   ; ***> Define nova rotina para tratamento de erros:
   (defun *ERROR* (msg)
      (princ " \n")
      (princ msg)
      (command "LAYER" "S" oldlay "")
      (mapcar 'setvar (list "CMDECHO" "BLIPMODE")
                      (list  oldech    oldblp   )
      )
      (setq *ERROR*    olderr)        ; Restaura funcao de erros original
   )


                     ; ***> inicio do processamento: <***

   ; ***> Captura Layer dos pontos
   (while (NOT lay)
      (setq Lay (getstring "\nLAYER dos pontos: "))
      (if (NOT (TestLay Lay))
         (setq Lay nil)
      )
   )


   (while (setq pto (getpoint "\nPonto: "))

      (entmake (list (cons  0 "POINT")
                     (cons 10 (list
                                 (car pto)
                                 (cadr pto)
                                 (getreal "\nCota: ")
                              )
                     )
                     (cons  8 lay)
               )
      )
   )

                 ***> Fim do LOOP




   ; ***> Restaura ambiente original:
   (command "LAYER" "S" oldlay "")
   (mapcar 'setvar (list "CMDECHO" "BLIPMODE")
                   (list  oldech    oldblp   )
   )
   (setq *ERROR*    olderr)
   (princ)

)




;
;             ***> Testa existencia de  LAYER <***
;                    (se nao existir, cria)
;

(defun testlay (nome / aux)
   ; teste se LAYER "nome" esta definida
   (if (tblsearch "LAYER" nome)
      ; Retorna .T. se existe
      (quote 1)

      ;Se nao existe, entao:
      (progn

         ; Testa se deseja criar
         (setq aux "z")
         (while (and  (/= (strcase aux) "N")  (/= (strcase aux) "S")  )
            (setq aux (strcase
                         (getstring
                            (strcat "\nCria LAYER " nome "<s/n>?")
                         )
                      )
            )
         )
         ; se for para criar:
         (if (= aux "S")
            (progn
               (COMMAND "LAYER"
                        "Make"
                         nome
                        "Color"
                         7
                        ""
                        ""
               )
               (quote 1)
            )
            (quote nil)
         )
      )
   )
)

