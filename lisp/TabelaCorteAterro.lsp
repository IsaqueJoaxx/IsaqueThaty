;;; ============================================================================
;;;  TABELA DE VOLUMES DE CORTE E ATERRO
;;; ----------------------------------------------------------------------------
;;;  Comando: TABCA
;;;
;;;  Coleta as areas de corte e aterro SELECIONANDO os textos no desenho e
;;;  monta uma tabela nativa do AutoCAD com as colunas:
;;;
;;;        ESTACA | VOL. CORTE (m3) | VOL. ATERRO (m3)
;;;
;;;  Volumes calculados pelo metodo da media das areas (average end area):
;;;        V = (A1 + A2) / 2 * distancia_entre_estacas
;;;
;;;  Entende numeros no formato brasileiro (virgula decimal, ponto de milhar)
;;;  e ignora texto extra (ex.: "A = 12,50 m2").  Aceita TEXT e MTEXT.
;;; ============================================================================

(vl-load-com)

;;; ---------------------------------------------------------------------------
;;; Remove codigos de formatacao de MTEXT, retornando o texto puro
;;; ---------------------------------------------------------------------------
(defun tabca:strip-mtext (s / lst out c c2)
  (setq lst (vl-string->list s) out "")
  (while lst
    (setq c (car lst) lst (cdr lst))
    (cond
      ;; '\' = inicio de codigo de formatacao
      ((= c 92)
       (if lst
         (progn
           (setq c2 (car lst))
           (cond
             ;; \P e \p -> quebra de paragrafo vira espaco
             ((or (= c2 80) (= c2 112))
              (setq out (strcat out " ") lst (cdr lst)))
             ;; codigos com parametro terminados em ';'
             ;; (f F C c H h A a Q q W w T t L O K N X)
             ((member c2 '(102 70 67 99 72 104 65 97 81 113 87 119 84 116 76 79 75 78 88))
              (setq lst (cdr lst))                       ; consome o c2
              (while (and lst (/= (car lst) 59))         ; ate o ';'
                (setq lst (cdr lst)))
              (if lst (setq lst (cdr lst))))             ; consome o ';'
             ;; \\ \{ \}  -> caractere literal escapado
             ((member c2 '(92 123 125))
              (setq out (strcat out (chr c2)) lst (cdr lst)))
             ;; qualquer outro -> apenas ignora a barra
             (t nil))))
      )
      ;; remove chaves de agrupamento { }
      ((or (= c 123) (= c 125)) nil)
      ;; caractere comum
      (t (setq out (strcat out (chr c))))
    )
  )
  out
)

;;; ---------------------------------------------------------------------------
;;; Converte string em numero entendendo formato BR (1.234,56) e US (1,234.56)
;;; ---------------------------------------------------------------------------
(defun tabca:str->num (s / r c temvirg)
  (setq temvirg (vl-string-search "," s)
        r "")
  (foreach c (vl-string->list s)
    (cond
      ((and (>= c 48) (<= c 57)) (setq r (strcat r (chr c))))      ; 0-9
      ((= c 45) (setq r (strcat r "-")))                           ; sinal
      ((= c 44) (setq r (strcat r ".")))                           ; virgula -> decimal
      ((= c 46) (if (not temvirg) (setq r (strcat r "."))))        ; ponto: decimal so se nao houver virgula
    )
  )
  (atof r)
)

;;; ---------------------------------------------------------------------------
;;; Le o valor numerico de uma entidade de texto (TEXT ou MTEXT)
;;; ---------------------------------------------------------------------------
(defun tabca:le-area (en / s)
  (setq s (cdr (assoc 1 (entget en))))
  (if (or (null s) (= s ""))
    0.0
    (tabca:str->num (tabca:strip-mtext s)))
)

;;; ---------------------------------------------------------------------------
;;; Agrupa a parte inteira em milhares com ponto (1234567 -> "1.234.567")
;;; ---------------------------------------------------------------------------
(defun tabca:milhar (s / n out)
  (setq out "" n (strlen s))
  (while (> n 3)
    (setq out (strcat "." (substr s (- n 2) 3) out)
          n   (- n 3)))
  (strcat (substr s 1 n) out)
)

;;; ---------------------------------------------------------------------------
;;; Formata numero no padrao BR com 2 casas: 1234.5 -> "1.234,50"
;;; ---------------------------------------------------------------------------
(defun tabca:fmt (n / neg ip fp s)
  (setq neg (minusp n) n (abs n))
  (setq ip (fix n))
  (setq fp (fix (+ 0.5 (* (- n ip) 100.0))))      ; 2 casas, arredondado
  (if (>= fp 100) (setq ip (1+ ip) fp (- fp 100)))
  (setq s (strcat (tabca:milhar (itoa ip)) ","
                  (if (< fp 10) (strcat "0" (itoa fp)) (itoa fp))))
  (if neg (setq s (strcat "-" s)))
  s
)

;;; ---------------------------------------------------------------------------
;;; Rotulo da estaca (inteiro quando possivel)
;;; ---------------------------------------------------------------------------
(defun tabca:est (e)
  (if (= e (fix e)) (itoa (fix e)) (rtos e 2 2))
)

;;; ===========================================================================
;;;  COMANDO PRINCIPAL
;;; ===========================================================================
(defun c:TABCA ( / *error* dist est0 h dados cont continua estv
                   e e2 ac aa ins sp doc tbl nrows idx r pd vc va totc tota )

  (defun *error* (msg)
    (and tbl (vlax-write-enabled-p tbl)
         (vla-put-RegenerateTableSuppressed tbl :vlax-false))
    (if (and msg (not (wcmatch (strcase msg) "*CANCEL*,*QUIT*,*EXIT*ABORT*")))
      (princ (strcat "\nErro: " msg)))
    (princ)
  )

  ;; ---- parametros ----------------------------------------------------------
  (initget 6)                                   ; sem zero, sem negativo
  (setq dist (getreal "\nComprimento entre estacas em metros <20.0>: "))
  (if (null dist) (setq dist 20.0))

  (initget 4)                                   ; sem negativo
  (setq est0 (getint "\nNumero da estaca inicial <0>: "))
  (if (null est0) (setq est0 0))

  (initget 6)
  (setq h (getreal "\nAltura do texto da tabela <2.5>: "))
  (if (null h) (setq h 2.5))

  ;; ---- coleta dos dados ----------------------------------------------------
  (princ "\n----------------------------------------------------------")
  (princ "\nSelecione os textos. Enter = area 0. Digite Fim para encerrar.")
  (princ "\n----------------------------------------------------------")
  (setq dados '() cont 0 continua t)

  (while continua
    (setq estv (+ est0 cont))
    (initget "Fim")
    (setq e (entsel (strcat "\nEstaca " (tabca:est estv)
                            "  >> texto da AREA DE CORTE  [Fim/Enter=0]: ")))
    (cond
      ((= e "Fim") (setq continua nil))
      (t
        (setq ac (if e (tabca:le-area (car e)) 0.0))
        (initget "Fim")
        (setq e2 (entsel (strcat "\nEstaca " (tabca:est estv)
                                 "  >> texto da AREA DE ATERRO [Fim/Enter=0]: ")))
        (if (= e2 "Fim")
          (setq continua nil)                   ; encerra sem gravar esta estaca
          (progn
            (setq aa (if e2 (tabca:le-area (car e2)) 0.0))
            (princ (strcat "\n   -> corte = " (tabca:fmt ac)
                           "   aterro = " (tabca:fmt aa)))
            (setq dados (cons (list estv ac aa) dados)
                  cont  (1+ cont))))
      )
    )
  )

  (setq dados (reverse dados))

  (if (< (length dados) 1)
    (progn (princ "\nNenhuma estaca informada. Comando encerrado.") (princ))
    (progn
      ;; ---- ponto de insercao ----------------------------------------------
      (setq ins (getpoint "\nPonto de insercao da tabela (canto superior esquerdo): "))
      (if (null ins) (setq ins (getvar "VIEWCTR")))

      ;; ---- cria a tabela ---------------------------------------------------
      (setq doc (vla-get-activedocument (vlax-get-acad-object))
            sp  (vla-get-block (vla-get-activelayout doc))
            nrows (+ (length dados) 3))          ; titulo + cabecalho + dados + total

      (setq tbl (vla-addtable sp (vlax-3d-point ins) nrows 3 (* h 2.0) (* h 12.0)))
      (vla-put-RegenerateTableSuppressed tbl :vlax-true)

      ;; larguras das colunas
      (vla-SetColumnWidth tbl 0 (* h 8.0))
      (vla-SetColumnWidth tbl 1 (* h 14.0))
      (vla-SetColumnWidth tbl 2 (* h 14.0))

      ;; altura de texto (1=titulo, 2=cabecalho, 4=dados)
      (vla-SetTextHeight tbl 1 (* h 1.25))
      (vla-SetTextHeight tbl 6 h)               ; 2 + 4 = cabecalho + dados
      ;; alinhamento centralizado (5 = acMiddleCenter)
      (vla-SetAlignment  tbl 7 5)               ; 1 + 2 + 4 = todas

      ;; titulo e cabecalho
      (vla-SetText tbl 0 0 "TABELA DE VOLUMES - CORTE E ATERRO")
      (vla-SetText tbl 1 0 "ESTACA")
      (vla-SetText tbl 1 1 "VOL. CORTE (m3)")
      (vla-SetText tbl 1 2 "VOL. ATERRO (m3)")

      ;; ---- preenche os dados e calcula volumes ----------------------------
      (setq totc 0.0 tota 0.0 idx 0)
      (foreach d dados
        (setq r    (+ 2 idx)
              estv (car d)
              ac   (cadr d)
              aa   (caddr d))
        (if (= idx 0)
          (setq vc 0.0 va 0.0)                   ; 1a estaca: sem volume anterior
          (progn
            (setq pd (nth (1- idx) dados))
            (setq vc (* (/ (+ (cadr pd)  ac) 2.0) dist)
                  va (* (/ (+ (caddr pd) aa) 2.0) dist))))
        (setq totc (+ totc vc) tota (+ tota va))
        (vla-SetText tbl r 0 (tabca:est estv))
        (vla-SetText tbl r 1 (if (= idx 0) "-" (tabca:fmt vc)))
        (vla-SetText tbl r 2 (if (= idx 0) "-" (tabca:fmt va)))
        (setq idx (1+ idx))
      )

      ;; ---- linha de total --------------------------------------------------
      (setq r (+ 2 (length dados)))
      (vla-SetText tbl r 0 "TOTAL")
      (vla-SetText tbl r 1 (tabca:fmt totc))
      (vla-SetText tbl r 2 (tabca:fmt tota))

      (vla-put-RegenerateTableSuppressed tbl :vlax-false)

      (princ (strcat "\nTabela criada com " (itoa (length dados)) " estaca(s)."))
      (princ (strcat "\nVolume total de corte:  " (tabca:fmt totc) " m3"))
      (princ (strcat "\nVolume total de aterro: " (tabca:fmt tota) " m3"))
    )
  )
  (princ)
)

(princ "\nTabela de Corte e Aterro carregada. Digite TABCA para executar.")
(princ)
