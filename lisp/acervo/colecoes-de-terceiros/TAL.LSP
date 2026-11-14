; Modificado em 24/09/96 para simplificar a chamada dos programas
(PRINC "\nESCOLHA A OPÇÃO: (Corte_ Aterro_ Rocha_ Rochalt ).")
 
  (DEFUN C:ATERRO ()
      ; Testa para saber se esta usando o prototipo
      ;  (TESTAPRO)

      ; Prepara as variaveis 
         (PREP) 

      ; Chamada da rotina propriamente dita
         (CORTATER "ATERRO")

      ; Restaura variaveis originais
         (REST)
  )

  (DEFUN C:CORTE ()
      ; Testa para saber se esta usando o prototipo
      ;  (TESTAPRO)

      ; Prepara as variaveis 
         (PREP) 

      ; Chamada da rotina propriamente dita
         (CORTATER "ESCAVA")

      ; Restaura variaveis originais
         (REST)
  )

  (DEFUN C:ROCHA ()
      ; Testa para saber se esta usando o prototipo
      ;  (TESTAPRO)

      ; Prepara as variaveis 
         (PREP) 

      ; Chamada da rotina propriamente dita
         (CORTATER "ROCHAS")

      ; Restaura variaveis originais
         (REST)
  )

  (DEFUN C:ROCHALT ()
      ; Testa para saber se esta usando o prototipo
      ;  (TESTAPRO)

      ; Prepara as variaveis 
         (PREP) 

      ; Chamada da rotina propriamente dita
         (CORTATER "ROCHAL")

      ; Restaura variaveis originais
         (REST)
  )




(defun PREP () ; Prepara as variaveis
      ; Guardar variaveis internas do AutoCad
         (SALV '("blipmode" "cecolor" "osmode" "textstyle" "cmdecho" "clayer" "orthomode"))
      ; Criar o layer chamado "TALUDES"
         (command "layer" "n" "TALUDES" "")
      ; Setar variaveis internas do AutoCad
         (setvar "blipmode" 0)
         (setvar "cmdecho" 0)
         (setvar "clayer" "TALUDES")
         (setvar "orthomode" 0)
         (command "color" 8)
         (command "osnap" "none")
)


; ***************************** INICIO  (SALV)  *****************************
(defun SALV (VS / VS VI VALOR) ; Salva variaveis internas do AutoCad
   (setq LISTAV '())
   (while VS
      (setq VI (car VS)
            VALOR (getvar VI)
            LISTAV (append LISTAV (list (list VI VALOR)))
            VS (cdr VS)
      )
   )
)
; ***************************** FIM  (SALV)  ********************************


; ***************************** INICIO  (REST)  *****************************
(defun REST ()  ; Recupera variaveis internas do AutoCad
  (while LISTAV
     (setq VI (car (car LISTAV))
           VALOR (car (cdr (car LISTAV)))
     )
     (setvar VI VALOR)
     (setq LISTAV (cdr LISTAV))
  )
)
; ***************************** FIM  (REST)  ********************************














































;-----------------------------------------------------------------------------
; CORTATER - Line Pattern especifica para planta e vista
;            de aterro (ATERRO) e escavacao: comum (ESCAVA),
;            rocha sa (ROCHAS) e rocha alterada (ROCHAL)
;
; sintaxe  : (cortater tipo)
;
; entradas : tipo        - aterro   : "ATERRO"
;                          escavacao: "ESCAVA","ROCHAS","ROCHAL"
;          : perguntadas
;            linha-proj  - linha do projeto (pe do corte ou topo do aterro)
;            linha-terr  - linha no terreno (topo do corte ou saia do aterro)
;
; obs
;   . se variavel externa lpattern_fatesc estiver definida,
;     e' o default do fator de escala
;-----------------------------------------------------------------------------
(defun cortater (tipo / linha endlin midtri endtri leftri leftri2
                        rigtri rigtri2 tri lefang rigang sinal tri2
                        midpnt p0 p1 oldce oldbp oldui btri larg bln
                        fatesc lprojsel lterr lproj conj n i ent
                )
   ;----------------------------------------------------------------------------
   ; DESEXT - desenha e estende linhas e triangulos
   (defun desext ()
      ;--------------------------------------------------
      ; funcao oddp - verifica se num e' par
      (defun evenp (num) (= (/ (fix num) 2) (/ num 2.0)))

      ; desenha e estende linha
      (command "ucs" "e" ent
               "line" p0 p1 ""
               "extend" lterr "" (list (setq linha (entlast)) p1) ""
      )
      (setq endlin (trans (cdr (assoc 11 (entget linha))) 0 1))

      ; se nao for aterro, usa triangulo (polyline ou solid)
      ; (se conseguiu estender linha maior que btri*fatesc)
      (if (and (/= tipo "ATERRO")
               (not (equal p1 endlin (* btri fatesc)))
          )
         (progn
            ; define pontos, angulos e limite, etc
            (setq midtri (polar endlin (/ pi -2) (* fatesc (* 0.5 btri)))
                  endtri (polar endlin (/ pi -2) (* fatesc    btri     ))
                  sinal  (/ fatesc (abs fatesc)) ; +-1
                  lefang (+    (atan 0.5)  (* sinal (/ pi 2)))
                  rigang (+ (- (atan 0.5)) (* sinal (/ pi 2)))
                  leftri (polar endtri lefang (abs (* fatesc (* 2 btri)))) ; forcado
                  rigtri (polar endtri rigang (abs (* fatesc (* 2 btri)))) ; forcado
            )

            ; desenha triangulos e corta linhas
            (command "line" leftri endtri "")
            (setq tri (entlast))
            (command "line" rigtri endtri ""
                     "trim" (setq tri2 (entlast)) lterr "" ; edges
                     (list linha midtri) (list tri leftri) (list tri2 rigtri) ""
            )
            ; novos leftri/rigtri
            (setq leftri2 (trans (cdr (assoc 10 (entget tri ))) 0 1))
            (setq rigtri2 (trans (cdr (assoc 10 (entget tri2))) 0 1))

            ; testa leftri e reduz lefang se necessario
            (if (equal leftri leftri2 1e-6)
               (progn
                  (princ "\nAjustando angulo ")(princ i)
                  (setq lefang (+ (atan 0.2) (* sinal (/ pi 2))) ;redef lefang (atan 0.2)
                        leftri (polar endtri lefang (abs (* fatesc (* 2 btri)))) ; refdef leftri
                  )
                  (command "erase" tri "" ;apaga tri
                           "line" leftri endtri "" ;refaz tri
                           "trim" lterr "" (list (setq tri (entlast)) leftri) "";trim
                  )
                  (setq leftri (trans (cdr (assoc 10 (entget tri ))) 0 1)) ; novo leftri
               )
               ;else: leftri ok
               (setq leftri leftri2)
            )

            ; testa rigtri e reduz lefang se necessario
            (if (equal rigtri rigtri2 1e-6)
               (progn
                  (princ " Ajustando angulo ")(princ i)
                  (setq rigang (+ (- (atan 0.2)) (* sinal (/ pi 2))) ;redef rigang (atan 0.2)
                        rigtri (polar endtri rigang (abs (* fatesc (* 2 btri)))) ; refdef leftri
                  )
                  (command "erase" tri2 "" ;apaga tri2
                           "line" rigtri endtri "" ;refaz tri2
                           "trim" lterr "" (list (setq tri2 (entlast)) rigtri) "";trim
                  )
                  (setq rigtri (trans (cdr (assoc 10 (entget tri2))) 0 1)) ; novo rigtri
               )
               ;else: rigtri ok
               (setq rigtri rigtri2)
            )

            ; se for rocha, transforma em solido
            (if (or (= tipo "ROCHAS")
                    (and (= tipo "ROCHAL")
                         (evenp i) ;i=par (primeiro = 0)
                    )
                )
               (command "erase" tri ""
                        "solid" leftri rigtri endtri "" ""
               )
            )
         )
      );end triangulos

      ;seja quem for, corta 50% da linha nos pares (primeiro=0)
      (if (evenp i)
         (progn
            (setq midpnt (list 0 (/ (cadr endlin) 2)))
            (command "line" (polar midpnt 0 -1) (polar midpnt 0 1) "")
            (if (= tipo "ATERRO")
               (command "trim" (entlast) "" (list linha endlin) "") ; aterro
               ; corte (se comprimento > 2*btri)
               (if (not (equal p1 endlin (abs (* fatesc 2 btri))))
                  (command "trim" (entlast) "" (list linha '(0 0)) "")
               )
            )
            (command "erase" (entlast) "")
         )
      ); end if par
   );end desext
   ;-----------------------------------------------------------------------------
   ; DEFBLK - (re)define bloco referencia | (vertical, h=0.001, insercao na base)
   (defun defblk ()
      (setq p0 '(0 0 0) p1 (polar p0 (* (/ fatesc (abs fatesc)) (/ pi 2)) 0.001))
      (command "line" p0 p1 ""
                "block" bln)
      (if (tblsearch "BLOCK" bln) (command "Y"))
      (command p0 (entlast) "")
   )
   ;-----------------------------------------------------------------------------
   ; CORTATER - inicio
   ; inicia
   (setq oldce (getvar "cmdecho")
         oldbp (getvar "blipmode")
;+         oldui (getvar "ucsicon")
   )
   (setvar "cmdecho" 0)
   (command "undo" "g"
            "ucs" "d" "ucsatual" ; deleta se existir
            "ucs" "s" "ucsatual"); salvar ucs
   (setvar "blipmode" 0)
;+   (setvar "ucsicon" 0)

   ; settings gerais
   (setq btri 2          ; base do triangulo
         larg (* btri 3) ; espacamento
         tipo (strcase tipo)
         bln "$$$ca$$$"
   )

   ; obtem fator de escala
   (if (not lpattern_fatesc) (setq lpattern_fatesc 1.0))
   (princ "\nFator de escala <")(princ (abs lpattern_fatesc))(setq fatesc (getreal ">: "))
   (if (not fatesc)
      (setq fatesc lpattern_fatesc)
      (setq lpattern_fatesc fatesc)
   )

   ; obtem entidades para hachura (linhas de topo e base)
   (if (= tipo "ATERRO")
      ;aterro
      (setq lprojsel (entsel "\nLinha de topo do aterro: ")
            lterr (car (entsel "\nLinha de saia do aterro: "))
      )
      ;else: corte
      (setq lterr (car (entsel "\nLinha de topo do corte: "))
            lprojsel (entsel "\nLinha de pe' do corte: ")
      )
   )
   (setq lproj (car lprojsel)) ; em qquer caso

   ; (re)define bloco referencia
   (defblk)

   ; measure alinhado para marcar pontos com espacamento larg*fatesc
   (command "MEASURE" lprojsel "B" bln "Y" (* larg (abs fatesc)))

   ; logo apos measure
   (setq conj (ssget "p")
         n (sslength conj)
         i 0)

   ; desenha linha, verifica e corrige sinal de fatesc se necessario
   (setq ent (ssname conj 0))
   (command "undo" "m" ; mark
            "ucs" "e" ent
            "line" p0 p1 ""
            "extend" lterr "" (list (setq linha (entlast)) p1) ""
   )
   (setq endlin (trans (cdr (assoc 11 (entget linha))) 0 1)); ucs=ent
   (if (equal p1 endlin 1e-6) ; tolerancia 1e-6
      (progn
         (princ " Invertendo direcao...")
         (setq fatesc (* -1 fatesc)
               lpattern_fatesc fatesc)
         (defblk)
      )
   )
   (command "undo" "b"); release

   ; desenha e estende linhas (e triangulos)
   (repeat n ; para cada ent restante...
      (setq ent (ssname conj i))
      (desext)
      (setq i (1+ i))
   ) ; end repeat

   ; finaliza
   (command "erase" conj ""
            "redraw"
            "ucs" "r" "ucsatual"
            "ucs" "d" "ucsatual"
            "undo" "e")
;+   (setvar "ucsicon" oldui)
   (setvar "blipmode" oldbp)
   (setvar "cmdecho" oldce)
   (princ)
) ; end cortater
;----------------------------------------------------------------------------
;(defun c:lc () (load "cgm1cort"))
;(defun c:c1 () (cortater (princ "ATERRO")))
;(defun c:c2 () (cortater (princ "ESCAVA")))
;(defun c:c3 () (cortater (princ "ROCHAS")))
;(defun c:c4 () (cortater (princ "ROCHAL")))
