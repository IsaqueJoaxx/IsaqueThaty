;;; ==============================================================
;;; SEPARAPONTOS.LSP
;;; Importa pontos de arquivo .TXT/.CSV, separa por codigo
;;; (descricao), cria uma LAYER por codigo e um GRUPO por codigo.
;;;
;;; Comando: SEPPTS
;;;
;;; Formatos aceitos por linha:
;;;   PNEZD -> Ponto,Norte,Este,Cota,Descricao
;;;   PENZD -> Ponto,Este,Norte,Cota,Descricao
;;;
;;; Delimitadores aceitos: virgula, ponto-e-virgula, TAB ou espaco
;;; (detectado automaticamente linha a linha).
;;; Decimal com virgula e tratado automaticamente quando o
;;; delimitador nao for virgula.
;;;
;;; Linhas de cabecalho ou invalidas sao ignoradas.
;;; ==============================================================

(vl-load-com)

;; ---------- utilidades ----------------------------------------

(defun sp:split (s d / p r)
  (setq r '())
  (while (setq p (vl-string-search d s))
    (setq r (cons (substr s 1 p) r))
    (setq s (substr s (+ p 1 (strlen d)))))
  (reverse (cons s r))
)

(defun sp:trim (s)
  (vl-string-trim " \t\r\n" s)
)

(defun sp:delim (s)
  (cond
    ((vl-string-search ";" s) ";")
    ((vl-string-search "\t" s) "\t")
    ((vl-string-search "," s) ",")
    (T " ")
  )
)

;; converte string em numero, aceitando decimal com virgula
(defun sp:num (s del)
  (if (/= del ",")
    (setq s (vl-string-translate "," "." s))
  )
  (distof s)
)

;; remove caracteres invalidos p/ nome de layer/grupo
(defun sp:clean-name (s / c r)
  (setq r "")
  (foreach c (vl-string->list s)
    (if (member (chr c) '("<" ">" "/" "\\" "\"" ":" ";" "?" "*" "|" "," "=" "`" " "))
      (setq r (strcat r "_"))
      (setq r (strcat r (chr c)))
    )
  )
  (if (= r "") "SEM_CODIGO" r)
)

;; verifica se grupo ja existe
(defun sp:group-exists (n / d)
  (if (setq d (dictsearch (namedobjdict) "ACAD_GROUP"))
    (dictsearch (cdr (assoc -1 d)) n)
  )
)

;; gera nome de grupo unico (acrescenta _1, _2... se ja existir)
(defun sp:unique-group (n / i nn)
  (setq nn n i 1)
  (while (sp:group-exists nn)
    (setq nn (strcat n "_" (itoa i))
          i  (1+ i))
  )
  nn
)

;; ---------- comando principal ---------------------------------

(defun c:SEPPTS (/ arq fmt rotulo fp lin del cps nome x y z cod pt
                   grupos par cores cor i lay gname ss total alt)

  ;; 1) arquivo
  (setq arq (getfiled "Selecione o arquivo de pontos" "" "txt;csv" 8))
  (if (not arq)
    (progn (prompt "\n*Cancelado* - nenhum arquivo selecionado.") (exit))
  )

  ;; 2) formato
  (initget "PNEZD PENZD")
  (setq fmt (getkword "\nFormato do arquivo [PNEZD/PENZD] <PNEZD>: "))
  (if (not fmt) (setq fmt "PNEZD"))

  ;; 3) rotulo com nome do ponto?
  (initget "Sim Nao")
  (setq rotulo (getkword "\nInserir texto com o nome do ponto? [Sim/Nao] <Nao>: "))
  (if (not rotulo) (setq rotulo "Nao"))
  (if (= rotulo "Sim")
    (progn
      (setq alt (getreal "\nAltura do texto <0.5>: "))
      (if (not alt) (setq alt 0.5))
    )
  )

  ;; 4) leitura do arquivo -> lista ((codigo (nome pt) (nome pt)...) ...)
  (setq fp (open arq "r") grupos '() total 0)
  (while (setq lin (read-line fp))
    (setq lin (sp:trim lin))
    (if (> (strlen lin) 0)
      (progn
        (setq del (sp:delim lin))
        (setq cps (vl-remove "" (mapcar 'sp:trim (sp:split lin del))))
        (if (>= (length cps) 4)
          (progn
            (setq nome (nth 0 cps))
            (if (= fmt "PNEZD")
              (setq y (sp:num (nth 1 cps) del)   ; Norte -> Y
                    x (sp:num (nth 2 cps) del))  ; Este  -> X
              (setq x (sp:num (nth 1 cps) del)   ; Este  -> X
                    y (sp:num (nth 2 cps) del))  ; Norte -> Y
            )
            (setq z (sp:num (nth 3 cps) del))
            (setq cod (if (>= (length cps) 5)
                        (strcase (nth 4 cps))
                        "SEM_CODIGO"))
            (if (and x y)
              (progn
                (if (not z) (setq z 0.0))
                (setq pt (list x y z))
                (setq par (assoc cod grupos))
                (if par
                  (setq grupos (subst (cons cod (cons (list nome pt) (cdr par)))
                                      par grupos))
                  (setq grupos (cons (cons cod (list (list nome pt))) grupos))
                )
                (setq total (1+ total))
              )
            )
          )
        )
      )
    )
  )
  (close fp)

  (if (= total 0)
    (progn (prompt "\nNenhum ponto valido encontrado no arquivo.") (exit))
  )

  ;; 5) estilo de exibicao dos pontos (X). Ajuste se quiser outro.
  (setvar "PDMODE" 3)

  ;; 6) criacao de layers, pontos e grupos
  (setq cores '(1 2 3 4 5 6 30 40 90 130 200 210 11 51 71 91 151 171)
        i 0)

  (prompt (strcat "\n--- " (itoa total) " pontos lidos ---"))

  (foreach par (reverse grupos)
    (setq cod  (car par)
          lay  (sp:clean-name (strcat "PTS_" cod))
          cor  (nth (rem i (length cores)) cores)
    )

    ;; layer
    (if (not (tblsearch "LAYER" lay))
      (entmake (list '(0 . "LAYER")
                     '(100 . "AcDbSymbolTableRecord")
                     '(100 . "AcDbLayerTableRecord")
                     (cons 2 lay)
                     (cons 62 cor)
                     '(70 . 0)))
    )

    ;; pontos (+ rotulos opcionais)
    (setq ss (ssadd))
    (foreach reg (cdr par)
      (setq nome (car reg) pt (cadr reg))
      (entmake (list '(0 . "POINT") (cons 10 pt) (cons 8 lay)))
      (ssadd (entlast) ss)
      (if (= rotulo "Sim")
        (progn
          (entmake (list '(0 . "TEXT")
                         (cons 10 (list (+ (car pt) (* alt 0.6))
                                        (+ (cadr pt) (* alt 0.6))
                                        (caddr pt)))
                         (cons 40 alt)
                         (cons 1 nome)
                         (cons 8 lay)))
          (ssadd (entlast) ss)
        )
      )
    )

    ;; grupo
    (setq gname (sp:unique-group (sp:clean-name cod)))
    (command "_.-GROUP" "_Create" gname "" ss "")

    (prompt (strcat "\n  " gname
                    " -> " (itoa (length (cdr par)))
                    " pontos | layer: " lay
                    " | cor: " (itoa cor)))
    (setq i (1+ i))
  )

  (command "_.ZOOM" "_Extents")
  (prompt (strcat "\nConcluido: " (itoa total) " pontos em "
                  (itoa (length grupos)) " grupos/layers."))
  (princ)
)

(prompt "\nSEPARAPONTOS.lsp carregado. Digite SEPPTS para executar.")
(princ)