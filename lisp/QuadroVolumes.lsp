;;; ============================================================================
;;;  QUADRO RESUMO DE VOLUMES  (Corte / Aterro / Liquido / Area)
;;; ----------------------------------------------------------------------------
;;;  Comando: QVOL
;;;
;;;  Monta um quadro resumo (tabela nativa do AutoCAD) com os totais que voce
;;;  ja tem no Painel de Volume do Civil 3D (superficie de volume TIN):
;;;
;;;        Total de Corte
;;;        Total de Aterro
;;;        Volume Liquido  -> calculado: Aterro - Corte
;;;                           (Fill) = falta aterro / (Cut) = sobra corte
;;;        Area 2D  (opcional)
;;;
;;;  Aceita entrada no formato brasileiro (virgula decimal).
;;; ============================================================================

(vl-load-com)

;;; ---- agrupa milhares: "1234567" -> "1.234.567" ----------------------------
(defun qv:milhar (s / n out)
  (setq out "" n (strlen s))
  (while (> n 3)
    (setq out (strcat "." (substr s (- n 2) 3) out)
          n   (- n 3)))
  (strcat (substr s 1 n) out)
)

;;; ---- formata numero BR com 2 casas: 1234.5 -> "1.234,50" ------------------
(defun qv:fmt (n / neg ip fp s)
  (setq neg (minusp n) n (abs n))
  (setq ip (fix n))
  (setq fp (fix (+ 0.5 (* (- n ip) 100.0))))
  (if (>= fp 100) (setq ip (1+ ip) fp (- fp 100)))
  (setq s (strcat (qv:milhar (itoa ip)) ","
                  (if (< fp 10) (strcat "0" (itoa fp)) (itoa fp))))
  (if neg (setq s (strcat "-" s)))
  s
)

;;; ---- le um numero aceitando virgula ou ponto decimal ----------------------
(defun qv:getnum (msg permitir-nulo / s r c temvirg)
  (setq s (getstring (strcat "\n" msg)))
  (if (and permitir-nulo (= s ""))
    nil
    (progn
      (setq temvirg (vl-string-search "," s) r "")
      (foreach c (vl-string->list s)
        (cond
          ((and (>= c 48) (<= c 57)) (setq r (strcat r (chr c))))
          ((= c 45) (setq r (strcat r "-")))
          ((= c 44) (setq r (strcat r ".")))
          ((= c 46) (if (not temvirg) (setq r (strcat r "."))))
        )
      )
      (atof r)
    )
  )
)

;;; ===========================================================================
;;;  COMANDO PRINCIPAL
;;; ===========================================================================
(defun c:QVOL ( / *error* corte aterro area h liq nat rows ins doc sp tbl
                  nrows i row )

  (defun *error* (msg)
    (and tbl (vlax-write-enabled-p tbl)
         (vla-put-RegenerateTableSuppressed tbl :vlax-false))
    (if (and msg (not (wcmatch (strcase msg) "*CANCEL*,*QUIT*,*EXIT*ABORT*")))
      (princ (strcat "\nErro: " msg)))
    (princ)
  )

  ;; ---- entrada dos valores -------------------------------------------------
  (setq corte (qv:getnum "Total de CORTE (m3): " nil))
  (while (null corte) (setq corte (qv:getnum "Valor invalido. Total de CORTE (m3): " nil)))

  (setq aterro (qv:getnum "Total de ATERRO (m3): " nil))
  (while (null aterro) (setq aterro (qv:getnum "Valor invalido. Total de ATERRO (m3): " nil)))

  (setq area (qv:getnum "Area 2D (m2) [Enter para omitir]: " t))

  (initget 6)
  (setq h (getreal "\nAltura do texto da tabela <2.5>: "))
  (if (null h) (setq h 2.5))

  ;; ---- calcula o liquido ---------------------------------------------------
  (setq liq (- aterro corte))                       ; >0 falta aterro / <0 sobra corte
  (cond
    ((> liq 0.001) (setq nat "(Fill)"))             ; precisa importar aterro
    ((< liq -0.001)(setq nat "(Cut)"))              ; excedente de corte (bota-fora)
    (t             (setq nat "(Compensado)")))

  ;; ---- monta as linhas -----------------------------------------------------
  (setq rows (list
               (list "Total de Corte (m3)"  (qv:fmt corte))
               (list "Total de Aterro (m3)" (qv:fmt aterro))
               (list (strcat "Volume Liquido " nat) (qv:fmt (abs liq)))))
  (if area
    (setq rows (append rows (list (list "Area 2D (m2)" (qv:fmt area))))))

  ;; ---- ponto de insercao ---------------------------------------------------
  (setq ins (getpoint "\nPonto de insercao do quadro (canto superior esquerdo): "))
  (if (null ins) (setq ins (getvar "VIEWCTR")))

  ;; ---- cria a tabela -------------------------------------------------------
  (setq doc   (vla-get-activedocument (vlax-get-acad-object))
        sp    (vla-get-block (vla-get-activelayout doc))
        nrows (+ (length rows) 2))                   ; titulo + cabecalho + linhas

  (setq tbl (vla-addtable sp (vlax-3d-point ins) nrows 2 (* h 2.0) (* h 16.0)))
  (vla-put-RegenerateTableSuppressed tbl :vlax-true)

  (vla-SetColumnWidth tbl 0 (* h 16.0))
  (vla-SetColumnWidth tbl 1 (* h 12.0))

  (vla-SetTextHeight tbl 1 (* h 1.25))              ; titulo
  (vla-SetTextHeight tbl 6 h)                       ; cabecalho + dados
  (vla-SetAlignment  tbl 3 5)                       ; titulo + cabecalho centralizados

  ;; titulo e cabecalho
  (vla-SetText tbl 0 0 "QUADRO DE VOLUMES")
  (vla-SetText tbl 1 0 "DESCRICAO")
  (vla-SetText tbl 1 1 "VALOR")

  ;; ---- preenche as linhas --------------------------------------------------
  (setq i 0)
  (foreach row rows
    (vla-SetText tbl (+ 2 i) 0 (car row))
    (vla-SetText tbl (+ 2 i) 1 (cadr row))
    (vla-SetCellAlignment tbl (+ 2 i) 0 4)          ; descricao a esquerda
    (vla-SetCellAlignment tbl (+ 2 i) 1 6)          ; valor a direita
    (setq i (1+ i))
  )

  (vla-put-RegenerateTableSuppressed tbl :vlax-false)

  (princ (strcat "\nQuadro criado.  Liquido = " (qv:fmt (abs liq)) " m3 " nat))
  (princ)
)

(princ "\nQuadro de Volumes carregado. Digite QVOL para executar.")
(princ)
