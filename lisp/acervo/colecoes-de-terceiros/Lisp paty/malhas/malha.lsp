;; Modificada em 03/02/2005 por Eduardo Fernal
;; Acrescentado quadro de diálogo e o uso da subr GETCORNER
;; http://www.gr-acad.arq.br
;; *********************************************************************
(IF (NOT (FINDFILE "malha002_lisp_livre_2005.dcl"))
  (PROGN (SETQ ef::lista (LIST
                           "malha002:dialog{label=\"Gerador de malhas\";width=70;"
                           ":column{alignment=centered;fixed_width=true;"
                           "initial_focus=\"interv\";spacer_1;"
                           ":edit_box{label=\"Intervalo da malha\";key=\"interv\";edit_width=12;}"
                           ":edit_box{label=\"Altura da fonte\";key=\"alt\";edit_width=12;}spacer_1;"
                           ":text{label=\"Nomenclatura para coordenadas :\";}"
                           ":radio_row{alignment=centered;fixed_width=true;"
                           ":radio_button{label=\"N,E\";key=\"1\";}"
                           ":radio_button{label=\"X,Y\";key=\"0\";}}spacer_1;"
                           ":row{fixed_width=true;fixed_height=true;alignment=centered;"
                           ":button{label=\"Como usar\";mnemonic=\"u\";key=\"help\";action=\"(ALERT msg)\";width=16;height=2.5;}"
                           ":button{label=\"Cancelar\";mnemonic=\"C\";key=\"cancel\";is_cancel=true;action=\"(DONE_DIALOG 0)\";width=16;height=2.5;}"
                           ":button{label=\"Prosseguir\";mnemonic=\"P\";key=\"accept\";action=\"(Verif)\";width=16;height=2.5;}}}}")
               ef::path  (FINDFILE "ACAD.EXE")
               ef::path  (SUBSTR ef::path 1 (- (STRLEN ef::path) 8))
               ef::path  (STRCAT ef::path "Malha002_Lisp_Livre_2005.dcl")
               ef::path  (OPEN ef::path "w")
         )
         (FOREACH x ef::lista (PRINC x ef::path))
         (CLOSE ef::path)
         (SETQ ef::lista nil
               ef::path nil
         )
  )
  nil
)
(DEFUN c:malha (/      clayer cosmode       vetn   vete   vetmaxn
                vetmaxe       vetm   vetdy  vetdx  malhae malhan op
                absc   orde   interv alt    pt1    pt2    pt2    pt4
                angx   angy   ptos   esp    i      j      in     x
                y      vardx  vardy  flag   quanti acr    contadi
                cx     px     py     par    aux    aux1   aux2   malhan
                veri   exec   para   pontos w@     dh
               )
;;;  (IF (= op 1)
;;;    (SETQ absc "E="
;;;          orde "N="
;;;    )
;;;    (SETQ absc "X="
;;;          orde "Y="
;;;    )
;;;  )
;;;  (SETQ interv (GETINT "\nIntervalo da malha = "))
;;;  (SETQ alt (GETREAL "\nAltura do texto = "))
;;;  (SETQ pt1 (GETPOINT "\nPrimeiro vértice da área : "))
;;;  (SETQ pt2 (GETPOINT pt1 "\nSegundo vértice da área : "))
;;;  (SETQ pt3 (GETPOINT pt2 "\nTerceiro vértice da área : "))
;;;  (SETQ pt4 (GETPOINT pt3 "\nQuarto vértice da área : "))
  (DEFUN pontos ()
    (IF (SETQ pt1
               (GETPOINT "\n-> Vértice esquerdo inferior do retângulo : ")
        )
      (IF (SETQ pt3
                 (GETCORNER pt1
                            "\n-> Vértice superior direito do retângulo : "
                 )
          )
        (SETQ pt2 (LIST (CAR pt3) (CADR pt1) 0.)
              pt4 (LIST (CAR pt1) (CADR pt3) 0.)
              pt1 (LIST (CAR pt1) (CADR pt1) 0.)
              pt3 (LIST (CAR pt3) (CADR pt3) 0.)
        )
        (SETQ pt1 nil
              pt2 nil
              pt3 nil
              pt4 nil
        )
      )
    )
  )
  (DEFUN verif ()
    (IF (AND (SETQ interv (GET_TILE "interv"))
             (> (ATOI interv) 0)
             (SETQ alt (GET_TILE "alt"))
             (> (ATOF alt) 0.)
             op
        )
      (PROGN (SETQ interv (ATOI interv)
                   alt    (ATOF alt)
             )
             (DONE_DIALOG 1)
      )
      (ALERT
        (STRCAT
          "Erro:\n\n\t"
          "Você deve informar o intervalo da malha com um número\t\n\t"
          "inteiro maior que zero, a altura da fonte com um número maior que zero\t\n\t"
          "e o tipo de nomenclatura a utilizar...\t\n\n"
        )
      )
    )
  )
  (DEFUN para (/ dh w@ msg)
    (SETQ msg
           (STRCAT
             "Atenção:\n\n\t"
             "Você deve informar o intervalo da malha com um número\t\n\t"
             "inteiro maior que zero, a altura da fonte com um número maior que zero\t\n\t"
             "e o tipo de nomenclatura a utilizar...\t\n\n"
           )
    )
    (IF (> (SETQ dh (LOAD_DIALOG "malha002_lisp_livre_2005.dcl")) 0)
      (IF (NEW_DIALOG "malha002" dh)
        (PROGN (ACTION_TILE "accept" "(VERIF)")
               (ACTION_TILE "cancel" "(DONE_DIALOG 0)")
               (FOREACH x '("0" "1") (ACTION_TILE x "(SETQ op $key)"))
               (ACTION_TILE "help" "(alert msg)")
               (SETQ w@ (START_DIALOG))
               (UNLOAD_DIALOG dh)
               (COND ((= w@ 0) (PRINC "\n-> Cancelado..."))
                     ((= w@ 1)
                      (pontos)
                      (IF (AND pt1 pt2 pt3 pt4)
                        (exec)
                        (PRINC "\n-> Pontos não fornecidos...")
                      )
                     )
               )
        )
        nil
      )
      (ALERT
        "Erro:\n\n\tArquivo dcl não pôde ser carregado!\t\n\n"
      )
    )
  )
  (DEFUN exec ()
    (COMMAND "UCS" "W" "_.UNDO" "_BEGIN")
    (SETQ clayer  (GETVAR "CLAYER")
          cosmode (GETVAR "OSMODE")
    )
    (SETVAR "OSMODE" 1)
    (IF (= op "1")
      (SETQ absc "N="
            orde "E="
      )
      (SETQ absc "X="
            orde "Y="
      )
    )
    (SETQ angx 90)
    (SETQ angy 0)
    (SETQ ptos (LIST pt1 pt2 pt3 pt4))
    (COMMAND "STYLE" "ROMANS" "ROMANS" alt "1" "0" "N" "N" "N")
    (IF (NOT (TBLSEARCH "LAYER" "MALHA"))
      (COMMAND "_.LAYER" "_MAKE" "MALHA" "_COLOR" "158" "MALHA" "")
    )
    (IF (NOT (TBLSEARCH "LAYER" "TEXTO-MALHA"))
      (COMMAND "_.LAYER" "_MAKE" "TEXTO-MALHA" "_COLOR" "2"
               "TEXTO-MALHA" "")
    )
    (SETVAR "OSMODE" 0)
    (SETQ esp " ")
    (SETQ i 0)
    (WHILE (/= i 4)
      (SETQ vetn (CONS (CADR (NTH i ptos)) vetn))
      (SETQ vete (CONS (CAR (NTH i ptos)) vete))
      (SETQ i (+ 1 i))
    )
    (SETQ vetn (REVERSE vetn)
          vete (REVERSE vete)
    )
    (SETQ i 0)
    (WHILE (/= i 4)
      (IF (= i 3)
        (SETQ j 0)
        (SETQ j (1+ i))
      )
      (SETQ vetmaxn (CONS (MAX (NTH i vetn) (NTH j vetn)) vetmaxn))
      (SETQ vetmaxe (CONS (MAX (NTH i vete) (NTH j vete)) vetmaxe))
      (SETQ i (+ 1 i))
    )
    (SETQ vetmaxn (CONS (MAX (NTH 3 vetmaxn) (NTH 1 vetmaxn)) vetmaxn))
    (SETQ vetmaxe (CONS (MAX (NTH 3 vetmaxe) (NTH 1 vetmaxe)) vetmaxe))
    (SETQ vetmaxn (REVERSE vetmaxn)
          vetmaxe (REVERSE vetmaxe)
    )
    (SETQ i 0)
    (WHILE (/= i 4)
      (IF (= i 3)
        (SETQ j 0)
        (SETQ j (1+ i))
      )
      (SETQ vetdy (CONS (- (NTH j vetn) (NTH i vetn)) vetdy))
      (SETQ vetdx (CONS (- (NTH j vete) (NTH i vete)) vetdx))
      (SETQ i (+ 1 i))
    )
    (SETQ vetdy (REVERSE vetdy)
          vetdx (REVERSE vetdx)
    )
    (SETQ i 0)
    (WHILE (/= i 4)
      (SETQ vardx (NTH i vetdx))
      (IF (= vardx 0.0)
        (SETQ vardx 0.000001)
      )
      (SETQ vetm (CONS (/ (NTH i vetdy) vardx) vetm))
      (SETQ i (+ 1 i))
    )
    (SETQ vetm (REVERSE vetm))
    (SETQ i 0)
    (WHILE (/= i 4)
      (SETQ in interv)
      (IF (= i 3)
        (SETQ j 0)
        (SETQ j (1+ i))
      )
      (SETQ flag 0)
      (WHILE (= flag 0)
        (SETQ
          x (* (FIX (/ (+ (MIN (NTH i vete) (NTH j vete)) in) interv))
               interv
            )
        )
        (IF (> x (NTH i vetmaxe))
          (SETQ flag 1)
          (PROGN (SETQ
                   y (+ (NTH i vetn) (* (NTH i vetm) (- x (NTH i vete))))
                 )
                 (SETQ pt (LIST x y))
                 (SETQ malhae (CONS pt malhae))
                 (SETQ in (+ in interv))
          )
        )
      )
      (SETQ i (+ 1 i))
    )
    (SETQ malhae (REVERSE malhae))
    (SETQ quanti (LENGTH malhae))
    (SETQ i 0)
    (IF (= angx 90)
      (SETQ acr (* (/ alt 2) -1))
      (SETQ acr (/ alt 2))
    )
    (SETQ contad 1)
    (WHILE (<= contad (/ quanti 2))
      (SETQ j (1+ i))
      (SETQ x (CAR (NTH i malhae)))
      (WHILE (/= x (CAR (NTH j malhae))) (SETQ j (+ 1 j)))
      (SETVAR "CLAYER" "MALHA")
      (COMMAND "LINE" (NTH i malhae) (NTH j malhae) "")
      (SETQ cy (MAX (CADR (NTH i malhae)) (CADR (NTH j malhae))))
      (SETQ px (LIST (+ (CAR (NTH i malhae)) acr) cy))
      (SETVAR "CLAYER" "TEXTO-MALHA")
      (IF (= angx 90)
        (COMMAND "TEXT"
                 "R"
                 px
                 angx
                 (STRCAT esp
                         absc
                         (RTOS (CAR (NTH j malhae)) 2 0)
                         esp
                         "m"
                         esp
                 )
        )
        (COMMAND
          "TEXT"
          px
          angx
          (STRCAT esp absc (RTOS (CAR (NTH j malhae)) 2 0) esp "m")
        )
      )
      (SETQ cy (MIN (CADR (NTH i malhae)) (CADR (NTH j malhae))))
      (SETQ px (LIST (+ (CAR (NTH i malhae)) acr) cy))
      (IF (= angx 90)
        (COMMAND
          "TEXT"
          px
          angx
          (STRCAT esp absc (RTOS (CAR (NTH j malhae)) 2 0) esp "m")
        )
        (COMMAND "TEXT"
                 "R"
                 px
                 angx
                 (STRCAT esp
                         absc
                         (RTOS (CAR (NTH j malhae)) 2 0)
                         esp
                         "m"
                         esp
                 )
        )
      )
      (SETQ aux1 nil
            aux2 nil
            aux  nil
      )
      (SETQ aux1 (MEMBER (NTH (1+ i) malhae) malhae))
      (IF (/= i 0)
        (PROGN (SETQ par (NTH (1- i) malhae))
               (SETQ aux (REVERSE malhae))
               (SETQ aux2 (REVERSE (MEMBER par aux)))
               (SETQ malhae (APPEND aux2 aux1))
        )
        (PROGN (SETQ malhae aux1))
      )
      (SETQ aux1 nil
            aux2 nil
            aux  nil
      )
      (IF (= j 1)
        (SETQ malhae (MEMBER (NTH j malhae) malhae))
        (PROGN (SETQ j (1- j))
               (IF (< (1+ j) (LENGTH malhae))
                 (PROGN (SETQ aux1 (MEMBER (NTH (1+ j) malhae) malhae))
                        (SETQ par (NTH (1- j) malhae))
                        (SETQ aux (REVERSE malhae))
                        (SETQ aux2 (REVERSE (MEMBER par aux)))
                        (SETQ malhae (APPEND aux2 aux1))
                 )
               )
        )
      )
      (SETQ contad (1+ contad))
    )
    (SETQ i 0)
    (WHILE (/= i 4)
      (SETQ in interv)
      (IF (= i 3)
        (SETQ j 0)
        (SETQ j (1+ i))
      )
      (SETQ flag 0)
      (WHILE (= flag 0)
        (SETQ
          y (* (FIX (/ (+ (MIN (NTH i vetn) (NTH j vetn)) in) interv))
               interv
            )
        )
        (IF (> y (NTH i vetmaxn))
          (SETQ flag 1)
          (PROGN (SETQ x
                        (/ (+ (- (* (NTH i vetm) (NTH i vete)) (NTH i vetn)) y)
                           (NTH i vetm)
                        )
                 )
                 (SETQ pt (LIST x y))
                 (SETQ malhan (CONS pt malhan))
                 (SETQ in (+ in interv))
          )
        )
      )
      (SETQ i (+ 1 i))
    )
    (SETQ malhan (REVERSE malhan))
    (SETQ veri malhan)
    (SETQ quanti (LENGTH malhan))
    (SETQ i 0)
    (IF (= angy 180)
      (SETQ acr (* (/ alt 2) -1))
      (SETQ acr (/ alt 2))
    )
    (SETQ contad 1)
    (WHILE (<= contad (/ quanti 2))
      (SETQ j (1+ i))
      (SETQ y (CADR (NTH i malhan)))
      (WHILE (/= y (CADR (NTH j malhan))) (SETQ j (+ 1 j)))
      (SETVAR "CLAYER" "MALHA")
      (COMMAND "LINE" (NTH i malhan) (NTH j malhan) "")
      (SETQ cx (MAX (CAR (NTH i malhan)) (CAR (NTH j malhan))))
      (SETQ py (LIST cx (+ (CADR (NTH i malhan)) acr)))
      (SETVAR "CLAYER" "TEXTO-MALHA")
      (IF (= angy 180)
        (COMMAND
          "TEXT"
          py
          angy
          (STRCAT esp orde (RTOS (CADR (NTH j malhan)) 2 0) esp "m")
        )
        (COMMAND "TEXT"
                 "R"
                 py
                 angy
                 (STRCAT esp
                         orde
                         (RTOS (CADR (NTH j malhan)) 2 0)
                         esp
                         "m"
                         esp
                 )
        )
      )
      (SETQ cx (MIN (CAR (NTH i malhan)) (CAR (NTH j malhan))))
      (SETQ py (LIST cx (+ (CADR (NTH i malhan)) acr)))
      (IF (= angy 180)
        (COMMAND "TEXT"
                 "R"
                 py
                 angy
                 (STRCAT esp
                         orde
                         (RTOS (CADR (NTH j malhan)) 2 0)
                         esp
                         "m"
                         esp
                 )
        )
        (COMMAND
          "TEXT"
          py
          angy
          (STRCAT esp orde (RTOS (CADR (NTH j malhan)) 2 0) esp "m")
        )
      )
      (SETQ aux1 nil
            aux2 nil
            aux  nil
      )
      (SETQ aux1 (MEMBER (NTH (1+ i) malhan) malhan))
      (IF (/= i 0)
        (PROGN (SETQ par (NTH (1- i) malhan))
               (SETQ aux (REVERSE malhan))
               (SETQ aux2 (REVERSE (MEMBER par aux)))
               (SETQ malhan (APPEND aux2 aux1))
        )
        (PROGN (SETQ malhan aux1))
      )
      (SETQ aux1 nil
            aux2 nil
            aux  nil
      )
      (IF (= j 1)
        (SETQ malhan (MEMBER (NTH j malhan) malhan))
        (PROGN (SETQ j (1- j))
               (IF (< (1+ j) (LENGTH malhan))
                 (PROGN (SETQ aux1 (MEMBER (NTH (1+ j) malhan) malhan))
                        (SETQ par (NTH (1- j) malhan))
                        (SETQ aux (REVERSE malhan))
                        (SETQ aux2 (REVERSE (MEMBER par aux)))
                        (SETQ malhan (APPEND aux2 aux1))
                 )
               )
        )
      )
      (SETQ contad (1+ contad))
    )
    (COMMAND "_.UNDO" "_END")
    (SETVAR "CLAYER" clayer)
    (SETVAR "OSMODE" cosmode)
  )
  (para)
  (PRINC "\n-> Conheça http://www.gr-acad.arq.br")
  (PRINC)
)
