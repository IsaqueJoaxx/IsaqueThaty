;;; ==========================================================================
;;;  LEGFIX.LSP  -  v1.0
;;;  Correcao da rotacao de rotulos exportados do Civil 3D para AutoCAD / DXF
;;;
;;;  COMANDOS
;;;    LEGINFO -> diagnostico: quantos textos, em quantos angulos, e quantos
;;;               blocos ainda tem texto "preso" dentro da definicao
;;;    LEGEXP  -> explode os blocos de rotulo (por padrao os anonimos *U...)
;;;    LEGFIX  -> ajusta a rotacao de TEXT / MTEXT / ATRIBUTOS
;;;
;;;  FLUXO RECOMENDADO:   LEGINFO  ->  LEGEXP  ->  LEGFIX
;;;
;;;  MODOS DO LEGFIX
;;;    Horizontal -> tudo em 0 grau
;;;    Legivel    -> mantem o alinhamento com o eixo, mas gira 180 graus o que
;;;                  estiver de cabeca para baixo (angulo final entre -90 e +90)
;;;    Angulo     -> forca um angulo qualquer informado em graus
;;;
;;;  A rotacao e feita em torno do ponto de insercao / justificacao do texto,
;;;  entao o texto continua "grudado" no tique ou na linha de chamada.
;;; ==========================================================================

(vl-load-com)

;;; -------------------------- utilitarios ---------------------------------

(defun lf:d2r (a) (* pi (/ a 180.0)))
(defun lf:r2d (a) (/ (* a 180.0) pi))

(defun lf:norm (a)
  (setq a (rem a (* 2.0 pi)))
  (if (< a 0.0) (setq a (+ a (* 2.0 pi))))
  a
)

;; angulo legivel em planta: resultado sempre entre -90 e +90 graus
(defun lf:legivel (a / n)
  (setq n (lf:norm a))
  (if (and (> n (/ pi 2.0)) (<= n (* 1.5 pi)))
    (lf:norm (+ n pi))
    n
  )
)

(defun lf:doc () (vla-get-ActiveDocument (vlax-get-acad-object)))

;; aplica a nova rotacao em um objeto de texto (vla). Retorna T se alterou.
(defun lf:aplica (obj modo alvo / cur new r)
  (setq cur (vl-catch-all-apply 'vla-get-Rotation (list obj)))
  (if (vl-catch-all-error-p cur)
    nil
    (progn
      (setq new (if (= modo "L") (lf:legivel cur) alvo))
      (if (> (abs (- (lf:norm cur) (lf:norm new))) 1e-8)
        (progn
          (setq r (vl-catch-all-apply 'vla-put-Rotation (list obj new)))
          (not (vl-catch-all-error-p r))
        )
        nil
      )
    )
  )
)

;; cache de definicoes de bloco ja verificadas
(setq lf:cache nil)

;; T se a DEFINICAO do bloco contem TEXT ou MTEXT (texto que o LEGFIX
;; nao alcanca sem explodir)
(defun lf:temTexto (obj / nome hit bd res)
  (setq nome (vl-catch-all-apply 'vla-get-EffectiveName (list obj)))
  (if (vl-catch-all-error-p nome)
    (setq nome (vl-catch-all-apply 'vla-get-Name (list obj)))
  )
  (if (vl-catch-all-error-p nome)
    nil
    (if (setq hit (assoc nome lf:cache))
      (cdr hit)
      (progn
        (setq res nil)
        (setq bd (vl-catch-all-apply
                   'vla-Item (list (vla-get-Blocks (lf:doc)) nome)))
        (if (not (vl-catch-all-error-p bd))
          (vlax-for ent bd
            (if (member (vla-get-ObjectName ent) '("AcDbText" "AcDbMText"))
              (setq res T)
            )
          )
        )
        (setq lf:cache (cons (cons nome res) lf:cache))
        res
      )
    )
  )
)

;; selecao padrao usada pelo LEGFIX e LEGINFO
(defun lf:selecao ( / esc)
  (initget "Tudo Selecionar")
  (setq esc (getkword "\nAbrangencia [Tudo/Selecionar] <Selecionar>: "))
  (if (= esc "Tudo")
    (ssget "_X" '((0 . "TEXT,MTEXT,INSERT")))
    (progn
      (princ "\nSelecione os rotulos / textos: ")
      (ssget '((0 . "TEXT,MTEXT,INSERT")))
    )
  )
)

(defun lf:erro (msg)
  (if (and msg (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*QUIT*")))
    (princ (strcat "\nErro: " msg))
  )
  (vl-catch-all-apply 'vla-EndUndoMark (list (lf:doc)))
  (setq *error* lf:olderr)
  (princ)
)

;;; ------------------------------ LEGINFO ---------------------------------

(defun c:LEGINFO ( / lf:olderr ss i e obj nome tipo cntT cntA cntB ang lst hit)
  (setq lf:olderr *error* *error* lf:erro)
  (setq lf:cache nil)
  (princ "\n--- LEGINFO : diagnostico dos rotulos ---")
  (setq ss (lf:selecao))
  (setq cntT 0 cntA 0 cntB 0 i 0 lst nil)
  (if ss
    (progn
      (repeat (sslength ss)
        (setq e   (ssname ss i)
              i   (1+ i)
              obj (vlax-ename->vla-object e)
              tipo (vla-get-ObjectName obj))
        (cond
          ((member tipo '("AcDbText" "AcDbMText"))
           (setq cntT (1+ cntT))
           (setq ang (fix (+ 0.5 (lf:r2d (lf:norm (vla-get-Rotation obj))))))
           (if (setq hit (assoc ang lst))
             (setq lst (subst (cons ang (1+ (cdr hit))) hit lst))
             (setq lst (cons (cons ang 1) lst))
           )
          )
          ((= tipo "AcDbBlockReference")
           (if (= (vla-get-HasAttributes obj) :vlax-true)
             (setq cntA (+ cntA (length (vlax-invoke obj 'GetAttributes))))
           )
           (if (lf:temTexto obj) (setq cntB (1+ cntB)))
          )
        )
      )
      (princ (strcat "\n  Textos soltos (TEXT/MTEXT) ..: " (itoa cntT)))
      (princ (strcat "\n  Atributos em blocos .........: " (itoa cntA)))
      (princ (strcat "\n  Blocos com texto interno ....: " (itoa cntB)))
      (princ (strcat "\n  Angulos distintos encontrados: " (itoa (length lst))))
      (if (> cntB 0)
        (princ "\n  >> Rode LEGEXP antes do LEGFIX para alcancar esses textos.")
      )
    )
    (princ "\nNada selecionado.")
  )
  (setq *error* lf:olderr)
  (princ)
)

;;; ------------------------------- LEGEXP ---------------------------------

(defun c:LEGEXP ( / lf:olderr esc pat ss i e obj cnt pul r pas voltas)
  (setq lf:olderr *error* *error* lf:erro)
  (princ "\n--- LEGEXP : explode blocos de rotulo ---")
  (initget "Anonimos Selecionar Padrao")
  (setq esc (getkword "\nQuais blocos [Anonimos/Selecionar/Padrao] <Anonimos>: "))
  (setq voltas (cond ((getint "\nPassadas (blocos aninhados) <2>: ")) (2)))
  (vla-StartUndoMark (lf:doc))
  (setq cnt 0 pul 0 pas 0)
  (while (< pas voltas)
    (setq pas (1+ pas))
    (setq ss
      (cond
        ((= esc "Selecionar")
         (if (= pas 1)
           (progn (princ "\nSelecione os blocos: ") (ssget '((0 . "INSERT"))))
           nil
         )
        )
        ((= esc "Padrao")
         (if (= pas 1)
           (setq pat (getstring T "\nPadrao do nome do bloco (ex: *Label*): "))
         )
         (if (and pat (/= pat ""))
           (ssget "_X" (list '(0 . "INSERT") (cons 2 pat)))
           nil
         )
        )
        (T (ssget "_X" '((0 . "INSERT") (2 . "`*U*"))))
      )
    )
    (if ss
      (progn
        (setq i 0)
        (repeat (sslength ss)
          (setq e (ssname ss i) i (1+ i))
          (if (entget e)
            (progn
              (setq obj (vlax-ename->vla-object e))
              ;; blocos com atributos NAO sao explodidos (perderiam o texto);
              ;; o LEGFIX ja trata atributos diretamente.
              (if (= (vla-get-HasAttributes obj) :vlax-true)
                (setq pul (1+ pul))
                (progn
                  (setq r (vl-catch-all-apply 'vla-Explode (list obj)))
                  (if (not (vl-catch-all-error-p r))
                    (progn
                      (vl-catch-all-apply 'vla-Delete (list obj))
                      (setq cnt (1+ cnt))
                    )
                  )
                )
              )
            )
          )
        )
      )
      (setq pas voltas)
    )
  )
  (vla-EndUndoMark (lf:doc))
  (princ (strcat "\n  " (itoa cnt) " bloco(s) explodido(s)."))
  (if (> pul 0)
    (princ (strcat "\n  " (itoa pul)
                   " bloco(s) com atributo foram preservados (use LEGFIX neles)."))
  )
  (setq *error* lf:olderr)
  (princ)
)

;;; ------------------------------- LEGFIX ---------------------------------

(defun c:LEGFIX ( / lf:olderr modo alvo ss i e obj tipo cntT cntA cntB)
  (setq lf:olderr *error* *error* lf:erro)
  (setq lf:cache nil)
  (princ "\n--- LEGFIX : corrige a rotacao dos rotulos ---")
  (initget "Horizontal Legivel Angulo")
  (setq modo (getkword "\nModo [Horizontal/Legivel/Angulo] <Horizontal>: "))
  (setq modo (cond ((= modo "Legivel") "L") ((= modo "Angulo") "A") (T "H")))
  (setq alvo 0.0)
  (if (= modo "A")
    (setq alvo (lf:d2r (cond ((getreal "\nAngulo desejado, em graus <0>: ")) (0.0))))
  )
  (setq ss (lf:selecao))
  (vla-StartUndoMark (lf:doc))
  (setq cntT 0 cntA 0 cntB 0 i 0)
  (if ss
    (progn
      (repeat (sslength ss)
        (setq e   (ssname ss i)
              i   (1+ i)
              obj (vlax-ename->vla-object e)
              tipo (vla-get-ObjectName obj))
        (cond
          ((member tipo '("AcDbText" "AcDbMText"))
           (if (lf:aplica obj modo alvo) (setq cntT (1+ cntT)))
          )
          ((= tipo "AcDbBlockReference")
           (if (= (vla-get-HasAttributes obj) :vlax-true)
             (foreach a (vlax-invoke obj 'GetAttributes)
               (if (lf:aplica a modo alvo) (setq cntA (1+ cntA)))
             )
           )
           (if (lf:temTexto obj) (setq cntB (1+ cntB)))
          )
        )
      )
      (vla-Regen (lf:doc) acActiveViewport)
      (princ (strcat "\n  " (itoa cntT) " texto(s) corrigido(s)."))
      (princ (strcat "\n  " (itoa cntA) " atributo(s) corrigido(s)."))
      (if (> cntB 0)
        (princ (strcat "\n  >> ATENCAO: " (itoa cntB)
                       " bloco(s) ainda tem texto interno. Rode LEGEXP e repita."))
      )
    )
    (princ "\nNada selecionado.")
  )
  (vla-EndUndoMark (lf:doc))
  (setq *error* lf:olderr)
  (princ)
)

(princ "\nLEGFIX.LSP carregado.  Comandos: LEGINFO | LEGEXP | LEGFIX\n")
(princ)
