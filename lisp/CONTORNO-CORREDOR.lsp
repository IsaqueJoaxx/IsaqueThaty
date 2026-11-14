;;; =====================================================================
;;;  CONTORNO-CORREDOR.lsp                                   versao 1.0
;;;  ---------------------------------------------------------------
;;;  COMANDO:  CC        (alias: CONTORNO)
;;;
;;;  O QUE FAZ: voce clica UM ponto dentro do corredor (ou de qualquer
;;;             ambiente) e o programa desenha uma POLILINHA FECHADA
;;;             contornando o espaco, na camada "CONTORNO-CORREDOR".
;;;             Ilhas internas (pilares, moveis, blocos) sao descartadas:
;;;             fica somente o contorno externo do corredor.
;;;
;;;  OPCAO:     [Tolerancia] - digite T antes de clicar para informar
;;;             uma tolerancia de vao. Serve para "fechar" vaos de
;;;             porta / soleiras. Ex.: desenho em cm, use 100 para
;;;             ignorar aberturas de ate 1,00 m.
;;;
;;;  IMPORTANTE: o motor BOUNDARY do AutoCAD so analisa o que esta
;;;              VISIVEL NA TELA. De um zoom que mostre o corredor
;;;              inteiro antes de rodar o comando.
;;;
;;;  INSTALACAO: APPLOAD > selecione este arquivo > Load.
;;;              Para carregar sempre: APPLOAD > Startup Suite > Add.
;;; =====================================================================

(vl-load-com)

;;; ------------------------- CONFIGURACAO ------------------------------
;;; (altere aqui se quiser outro nome de camada, cor ou espessura)

(setq *cc-camada* "CONTORNO-CORREDOR") ; camada onde a polilinha e criada
(setq *cc-cor*    1)                   ; cor: 1=verm 2=amar 3=verde 4=ciano
                                       ;      5=azul 6=magenta 7=branco/preto
(setq *cc-lw*     50)                  ; espessura de pena (50 = 0.50 mm)

(if (not *cc-gaptol*) (setq *cc-gaptol* 0.0)) ; tolerancia de vao inicial

;;; ---------------------------------------------------------------------
;;;                        FUNCOES AUXILIARES
;;; ---------------------------------------------------------------------

(setq *cc-vars* '("CMDECHO" "OSMODE" "CLAYER" "HPBOUND" "HPGAPTOL"))

;; getvar protegido (retorna nil se a variavel nao existir nesta versao)
(defun cc:gv (v / r)
  (setq r (vl-catch-all-apply 'getvar (list v)))
  (if (vl-catch-all-error-p r) nil r)
)

;; setvar protegido
(defun cc:sv (v n)
  (if n (vl-catch-all-apply 'setvar (list v n)))
  nil
)

(defun cc:salva ()
  (setq *cc-old* (mapcar 'cc:gv *cc-vars*))
)

(defun cc:undo-fim ()
  (command "_.UNDO" "_E")
)

(defun cc:restaura ()
  ;; fecha o grupo de UNDO, se estiver aberto
  (if *cc-undo*
    (progn
      (setq *cc-undo* nil)
      (vl-catch-all-apply 'cc:undo-fim '())
    )
  )
  ;; devolve as variaveis de sistema como estavam
  (if *cc-old*
    (progn
      (mapcar 'cc:sv *cc-vars* *cc-old*)
      (setq *cc-old* nil)
    )
  )
  (princ)
)

;; cria a camada de destino; se ja existir, garante ligada / descongelada /
;; desbloqueada, para que a polilinha possa ser desenhada nela
(defun cc:camada (nome cor lw / e d f)
  (if (tblsearch "LAYER" nome)
    (progn
      (setq e (tblobjname "LAYER" nome)
            d (entget e)
            f (cdr (assoc 70 d))
      )
      (if (= 1 (logand 1 f)) (setq f (- f 1))) ; descongela
      (if (= 4 (logand 4 f)) (setq f (- f 4))) ; desbloqueia
      (setq d (subst (cons 70 f) (assoc 70 d) d))
      (if (minusp (cdr (assoc 62 d)))          ; liga (ON)
        (setq d (subst (cons 62 (abs (cdr (assoc 62 d)))) (assoc 62 d) d))
      )
      (entmod d)
      (entupd e)
    )
    (entmake
      (list '(0 . "LAYER")
            '(100 . "AcDbSymbolTableRecord")
            '(100 . "AcDbLayerTableRecord")
            (cons 2 nome)
            '(70 . 0)
            (cons 62 cor)
            '(6 . "Continuous")
            (cons 370 lw)
      )
    )
  )
  nome
)

;; lista os objetos criados depois da entidade "ult"
(defun cc:novos (ult / e lst)
  (setq lst '())
  (if ult
    (while (setq ult (entnext ult)) (setq lst (cons ult lst)))
    (progn
      (setq e (entnext))
      (while e (setq lst (cons e lst)) (setq e (entnext e)))
    )
  )
  (reverse lst)
)

;; area de um objeto fechado (0.0 se nao conseguir medir)
(defun cc:area1 (en)
  (vla-get-Area (vlax-ename->vla-object en))
)

(defun cc:area (en / a)
  (setq a (vl-catch-all-apply 'cc:area1 (list en)))
  (if (numberp a) a 0.0)
)

;;; ---------------------------------------------------------------------
;;;                         COMANDO PRINCIPAL
;;; ---------------------------------------------------------------------

(defun c:CC (/ *error* pt ult novos maior amaior a e d n k)

  (defun *error* (msg / k)
    (setq k 0)
    (while (and (> (getvar "CMDACTIVE") 0) (< k 5))
      (command)
      (setq k (1+ k))
    )
    (cc:restaura)
    (if (and msg
             (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*QUIT*,*ESCAPE*"))
        )
      (princ (strcat "\n** Erro: " msg " **"))
    )
    (princ)
  )

  (cc:salva)
  (cc:camada *cc-camada* *cc-cor* *cc-lw*)

  (setvar "CMDECHO" 0)
  (setvar "OSMODE" 0)      ; sem snap: o clique deve cair no vazio
  (cc:sv "HPBOUND" 1)      ; objeto gerado = POLILINHA (e nao region)
  (cc:sv "HPGAPTOL" *cc-gaptol*)

  ;; ---------------- pede o ponto interno -----------------------------
  (setq pt "?")
  (while (and pt (not (listp pt)))
    (initget "Tolerancia")
    (setq pt (getpoint
               (strcat "\nClique DENTRO do corredor ou [Tolerancia] <tol="
                       (rtos *cc-gaptol* 2 2) ">: ")
             )
    )
    (if (and pt (not (listp pt)))          ; digitou T -> ajusta tolerancia
      (progn
        (initget 4)
        (setq n (getreal
                  (strcat "\nTolerancia para fechar vaos de porta <"
                          (rtos *cc-gaptol* 2 2) ">: ")
                )
        )
        (if n (setq *cc-gaptol* n))
        (cc:sv "HPGAPTOL" *cc-gaptol*)
        (princ (strcat "\nTolerancia = " (rtos *cc-gaptol* 2 2)))
      )
    )
  )

  ;; ---------------- gera o contorno ----------------------------------
  (if (and pt (listp pt))
    (progn
      ;; agrupa tudo em um unico UNDO (um Ctrl+Z desfaz o comando inteiro)
      (if (= 1 (logand 1 (getvar "UNDOCTL")))
        (progn
          (command "_.UNDO" "_BE")
          (setq *cc-undo* T)
        )
      )

      (setvar "CLAYER" *cc-camada*)
      (setq ult (entlast))

      (command "_.-BOUNDARY" pt "")
      (setq k 0)
      (while (and (> (getvar "CMDACTIVE") 0) (< k 5))
        (command "")
        (setq k (1+ k))
      )

      (setq novos (cc:novos ult))

      (cond
        ;; --- nada foi gerado -> orienta o usuario ---
        ((null novos)
         (princ "\n>> Nao foi possivel fechar o contorno neste ponto.")
         (princ "\n   1) O corredor inteiro precisa estar VISIVEL na tela.")
         (princ "\n   2) Vaos de porta deixam o contorno aberto:")
         (princ "\n      rode CC de novo, digite T e informe a tolerancia.")
         (princ "\n   3) O ponto clicado deve cair em area vazia,")
         (princ "\n      sem tocar em linhas, textos ou hachuras.")
        )

        ;; --- gerado: mantem so o contorno externo (o de maior area) ---
        (T
         (setq maior  (car novos)
               amaior (cc:area (car novos))
         )
         (foreach e (cdr novos)
           (if (> (setq a (cc:area e)) amaior)
             (setq maior e
                   amaior a
             )
           )
         )
         ;; so descarta as ilhas se as areas puderam mesmo ser medidas
         (if (> amaior 0.0)
           (foreach e novos
             (if (not (equal e maior)) (entdel e))
           )
         )
         (setq d (entget maior))
         (if (/= (strcase (cdr (assoc 8 d))) (strcase *cc-camada*))
           (entmod (subst (cons 8 *cc-camada*) (assoc 8 d) d))
         )
         (entupd maior)
         (sssetfirst nil (ssadd maior))
         (princ (strcat "\n>> Contorno criado na camada \""
                        *cc-camada*
                        "\"."))
         (if (and (> amaior 0.0) (> (length novos) 1))
           (princ (strcat " ("
                          (itoa (1- (length novos)))
                          " ilha(s) interna(s) descartada(s))"))
         )
        )
      )
    )
  )

  (cc:restaura)
  (princ)
)

;; alias
(defun c:CONTORNO () (c:CC))

(princ "\n---------------------------------------------------------")
(princ "\n CONTORNO-CORREDOR carregado.")
(princ "\n Digite  CC  e clique dentro do corredor.")
(princ "\n (opcao T = tolerancia para vaos de porta)")
(princ "\n---------------------------------------------------------")
(princ)
