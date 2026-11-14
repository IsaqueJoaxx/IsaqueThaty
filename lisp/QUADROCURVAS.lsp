;;; ============================================================
;;; QUADROCURVAS.lsp
;;; Gera quadro de curvas horizontais de alinhamento Civil 3D
;;; Colunas: N° | Delta | Raio | Tangente | Comprimento | Corda | Az.Corda
;;; ============================================================

(defun c:QUADROCURVAS (/ doc site aligns alig ents ent
                         tipo curvas ncurvas i
                         delta raio tang comp corda chord-dir
                         pt0 x0 y0
                         cw rh txth
                         headers ncols)

  (vl-load-com)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))

  ;; ── 1. Seleciona alinhamento ─────────────────────────────
  (setq alig (qc:select-alignment doc))
  (if (null alig)
    (progn (alert "Nenhum alinhamento selecionado.") (exit)))

  ;; ── 2. Coleta curvas do alinhamento ──────────────────────
  (setq curvas (qc:get-curves alig))
  (setq ncurvas (length curvas))
  (if (zerop ncurvas)
    (progn (alert "O alinhamento não possui curvas circulares.") (exit)))

  ;; ── 3. Ponto de inserção da tabela ───────────────────────
  (setq pt0 (getpoint "\nPonto de inserção do quadro: "))
  (if (null pt0) (exit))
  (setq x0 (car pt0)
        y0 (cadr pt0))

  ;; ── 4. Parâmetros gráficos ───────────────────────────────
  (setq txth  1.8          ; altura do texto (ajuste pela escala)
        rh    (* txth 2.2) ; altura da linha
        cw    (list 10 22 22 22 22 22 30)) ; larguras das colunas (mm)

  (setq headers (list "N\U+00BA" "\U+0394" "R (m)" "T (m)"
                      "L (m)" "C (m)" "Az. Corda"))
  (setq ncols 7)

  ;; ── 5. Desenha tabela ────────────────────────────────────
  (qc:draw-table x0 y0 headers cw rh txth curvas ncurvas)

  (princ (strcat "\n>> Quadro gerado com " (itoa ncurvas) " curva(s)."))
  (princ)
)

;; ============================================================
;; Seleciona alinhamento via nome (lista todos disponíveis)
;; ============================================================
(defun qc:select-alignment (doc / aligns names aname)
  (setq aligns (qc:get-all-alignments))
  (if (null aligns)
    (progn (alert "Nenhum alinhamento encontrado no desenho.\nVerifique se o desenho possui alinhamentos Civil 3D.") (exit)))

  (if (= (length aligns) 1)
    (car aligns)
    (progn
      (setq names (mapcar (function (lambda (a)
                    (vlax-get-property a "Name"))) aligns))
      (setq aname (qc:listbox "Selecione o Alinhamento" names))
      (if (null aname) nil
        (nth (vl-position aname names) aligns))
    )
  )
)

;; ============================================================
;; Retorna lista de objetos AeccAlignment via ssget (DXF type)
;; Suporta: AECC_ALIGNMENT e variantes de versão
;; ============================================================
(defun qc:get-all-alignments (/ ss result i ename obj nome)
  (setq result '())

  ;; Tenta encontrar pelo tipo DXF nativo do Civil 3D
  (setq ss (ssget "_X" '((0 . "AECC_ALIGNMENT"))))

  ;; Fallback: alguns builds usam sufixo de versão no tipo
  (if (null ss)
    (setq ss (ssget "_X" '((0 . "AECC_COGO_ALIGNMENT")))))

  (if ss
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (setq ename (ssname ss i))
        (setq obj   (vlax-ename->vla-object ename))
        ;; Confirma que tem propriedade "Name" (é objeto válido)
        (if (not (vl-catch-all-error-p
                   (vl-catch-all-apply 'vlax-get-property (list obj "Name"))))
          (setq result (cons obj result))
        )
        (setq i (1+ i))
      )
    )
    ;; Fallback 2: varre o ModelSpace verificando ObjectName dinamicamente
    (progn
      (princ "\n[aviso] ssget não encontrou alinhamentos, varrendo ModelSpace...")
      (setq result (qc:scan-modelspace-for-alignments))
    )
  )
  (reverse result)
)

;; ============================================================
;; Varredura do ModelSpace procurando qualquer objeto com
;; "Alignment" ou "alignment" no ObjectName e propriedade "Entities"
;; ============================================================
(defun qc:scan-modelspace-for-alignments (/ doc ms result obj oname)
  (setq result '())
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq ms  (vla-get-ModelSpace doc))
  (vlax-for obj ms
    (setq oname
      (vl-catch-all-apply 'vlax-get-property (list obj "ObjectName")))
    (if (and (not (vl-catch-all-error-p oname))
             (vl-string-search "lignment" oname)
             (not (vl-catch-all-error-p
                    (vl-catch-all-apply 'vlax-get-property (list obj "Entities")))))
      (setq result (cons obj result))
    )
  )
  result
)

;; ============================================================
;; Dialog simples de seleção por lista (GET via GETSTRING)
;; ============================================================
(defun qc:listbox (title items / i str choice)
  (setq str (strcat title "\n\nAlinhamentos disponíveis:\n"))
  (setq i 1)
  (foreach item items
    (setq str (strcat str "  " (itoa i) " - " item "\n"))
    (setq i (1+ i))
  )
  (setq str (strcat str "\nDigite o número: "))
  (setq choice (getint str))
  (if (and choice (>= choice 1) (<= choice (length items)))
    (nth (1- choice) items)
    nil
  )
)

;; ============================================================
;; Coleta dados das curvas circulares do alinhamento
;; Usa apenas a presença de "Radius" e "Delta" como critério —
;; independente do ObjectName exato (varia por versão do Civil 3D)
;; ============================================================
(defun qc:get-curves (alig / ents ent curvas i nents r-ok d-ok)
  (setq curvas '())
  (setq ents  (vl-catch-all-apply 'vlax-get-property (list alig "Entities")))
  (if (vl-catch-all-error-p ents)
    (progn (alert "Não foi possível acessar as entidades do alinhamento.") (exit)))

  (setq nents (vlax-get-property ents "Count"))
  (princ (strcat "\n[info] Total de entidades no alinhamento: " (itoa nents)))

  (setq i 0)
  (while (< i nents)
    (setq ent (vl-catch-all-apply 'vlax-invoke (list ents "Item" i)))
    (if (not (vl-catch-all-error-p ent))
      (progn
        (setq r-ok (qc:has-property ent "Radius"))
        (setq d-ok (qc:has-property ent "Delta"))
        (if (and r-ok d-ok)
          ;; É uma curva circular → extrai dados
          (setq curvas (append curvas
                  (list (qc:extract-curve-data ent (length curvas)))))
        )
      )
    )
    (setq i (1+ i))
  )
  curvas
)

;; ============================================================
;; Extrai dados de uma curva com fallback geométrico
;;   R e Delta sempre vêm da API; demais valores são calculados
;;   se a propriedade não existir nessa versão do Civil 3D
;; ============================================================
(defun qc:extract-curve-data (ent num / delta raio tang comp corda chord-dir v)
  ;; --- Raio e Delta (obrigatórios) ---
  (setq raio  (vlax-get-property ent "Radius"))
  (setq delta (vlax-get-property ent "Delta"))   ; radianos

  ;; --- Tangente: tenta nomes alternativos, senão calcula ---
  (setq tang (qc:try-props ent '("TangentLength" "Tangent" "TangentIn" "TangentOut")))
  (if (null tang)
    (setq tang (* raio (tan (/ delta 2.0)))))

  ;; --- Comprimento do arco ---
  (setq comp (qc:try-props ent '("Length" "ArcLength" "CurveLength")))
  (if (null comp)
    (setq comp (* raio delta)))

  ;; --- Corda ---
  (setq corda (qc:try-props ent '("Chord" "ChordLength" "ChordDistance")))
  (if (null corda)
    (setq corda (* 2.0 raio (sin (/ delta 2.0)))))

  ;; --- Direção da corda (radianos, AutoCAD: E=0, CCW+) ---
  (setq chord-dir (qc:try-props ent '("ChordDirection" "ChordBearing" "ChordAzimuth")))
  (if (null chord-dir)
    (setq chord-dir 0.0))   ; sem direção: coloca 0 e avisa

  (list (1+ num) delta raio tang comp corda chord-dir)
)

;; Tenta obter o valor de uma lista de nomes de propriedade.
;; Retorna o primeiro que funcionar, ou nil se nenhum existir.
(defun qc:try-props (obj props / v)
  (setq v nil)
  (foreach p props
    (if (null v)
      (progn
        (setq v (vl-catch-all-apply 'vlax-get-property (list obj p)))
        (if (vl-catch-all-error-p v) (setq v nil))
      )
    )
  )
  v
)

;; ============================================================
;; Verifica se propriedade existe no objeto
;; ============================================================
(defun qc:has-property (obj prop / r)
  (setq r (vl-catch-all-apply
    'vlax-get-property (list obj prop)))
  (not (vl-catch-all-error-p r))
)

;; ============================================================
;; Converte ângulo AutoCAD (rad, CCW de Leste) → azimute (°)
;;   Azimute: Norte=0, sentido horário
;; ============================================================
(defun qc:angle->az (rad)
  (setq deg (- 90.0 (* rad (/ 180.0 pi))))
  (while (< deg 0.0)    (setq deg (+ deg 360.0)))
  (while (>= deg 360.0) (setq deg (- deg 360.0)))
  deg
)

;; ============================================================
;; Formata ângulo em graus → "GGG°MM'SS\""
;; ============================================================
(defun qc:fmt-angle (rad / deg g m s)
  (setq deg (* (abs rad) (/ 180.0 pi)))
  (setq g (fix deg))
  (setq m (fix (* (- deg g) 60.0)))
  (setq s (fix (* (- (* (- deg g) 60.0) m) 60.0)))
  (strcat (itoa g) "\U+00B0" (qc:z2 m) "'" (qc:z2 s) "\"")
)

;; Formata azimute (graus decimais) → "GGG°MM'SS\""
(defun qc:fmt-az (deg / g m s)
  (setq g (fix deg))
  (setq m (fix (* (- deg g) 60.0)))
  (setq s (fix (* (- (* (- deg g) 60.0) m) 60.0)))
  (strcat (qc:z3 g) "\U+00B0" (qc:z2 m) "'" (qc:z2 s) "\"")
)

;; Zero-pad 2 dígitos
(defun qc:z2 (n)
  (if (< n 10) (strcat "0" (itoa n)) (itoa n)))

;; Zero-pad 3 dígitos
(defun qc:z3 (n)
  (cond ((< n 10)  (strcat "00" (itoa n)))
        ((< n 100) (strcat "0"  (itoa n)))
        (t (itoa n))))

;; Formata distância com 3 casas decimais
(defun qc:fmt-dist (v)
  (rtos v 2 3))

;; ============================================================
;; DESENHO DA TABELA
;; ============================================================
(defun qc:draw-table (x0 y0 headers cw rh txth curvas ncurvas
                      / ncols tw x1 y1 yi xi i j c row
                        num delta raio tang comp corda chord-dir vals)

  (setq ncols (length headers))

  ;; Largura total
  (setq tw (apply '+ cw))

  ;; Linha do título
  (setq y1 (+ y0 (* rh 2))) ; topo do quadro

  ;; ── Título ──────────────────────────────────────────────
  (qc:draw-rect x0 (- y1 rh) tw rh)
  (qc:draw-text
    (+ x0 (/ tw 2.0))
    (+ (- y1 rh) (* rh 0.3))
    "QUADRO DE CURVAS HORIZONTAIS"
    (* txth 1.1)
    "MC"
  )

  ;; ── Cabeçalho das colunas ────────────────────────────────
  (setq yi (- y1 rh)) ; topo do cabeçalho
  (qc:draw-row-lines x0 (- yi rh) cw rh)
  (setq xi x0)
  (foreach h headers
    (setq coli-w (nth (vl-position h headers) cw))
    (qc:draw-text
      (+ xi (/ coli-w 2.0))
      (+ (- yi rh) (* rh 0.3))
      h txth "MC")
    (setq xi (+ xi coli-w))
  )

  ;; Linha separadora dupla abaixo do cabeçalho
  (setq yi (- yi rh))
  (entmake (list '(0 . "LINE")
    (cons 10 (list x0 yi 0.0))
    (cons 11 (list (+ x0 tw) yi 0.0))
    '(8 . "QUADRO_CURVAS")))
  (entmake (list '(0 . "LINE")
    (cons 10 (list x0 (- yi 0.3) 0.0))
    (cons 11 (list (+ x0 tw) (- yi 0.3) 0.0))
    '(8 . "QUADRO_CURVAS")))

  ;; ── Linhas de dados ──────────────────────────────────────
  (setq i 0)
  (foreach c curvas
    (setq num       (nth 0 c)
          delta     (nth 1 c)
          raio      (nth 2 c)
          tang      (nth 3 c)
          comp      (nth 4 c)
          corda     (nth 5 c)
          chord-dir (nth 6 c))

    (setq vals (list
      (itoa num)
      (qc:fmt-angle delta)
      (qc:fmt-dist raio)
      (qc:fmt-dist tang)
      (qc:fmt-dist comp)
      (qc:fmt-dist corda)
      (qc:fmt-az (qc:angle->az chord-dir))
    ))

    (setq row-y (- yi (* (1+ i) rh)))
    (qc:draw-row-lines x0 row-y cw rh)

    (setq xi x0 j 0)
    (foreach val vals
      (setq coli-w (nth j cw))
      (qc:draw-text
        (+ xi (/ coli-w 2.0))
        (+ row-y (* rh 0.3))
        val txth "MC")
      (setq xi (+ xi coli-w))
      (setq j (1+ j))
    )
    (setq i (1+ i))
  )

  ;; Borda externa do quadro inteiro
  (setq total-h (* rh (+ 2 ncurvas))) ; título + cabeçalho + dados
  (qc:draw-rect x0 (- y1 total-h) tw total-h)

  ; Cria layer se não existir
  (qc:ensure-layer "QUADRO_CURVAS" 3)
)

;; ============================================================
;; Desenha linhas verticais e horizontais de uma linha da tabela
;; ============================================================
(defun qc:draw-row-lines (x0 y cw rh / xi tw)
  (setq tw (apply '+ cw))
  ;; Linha horizontal inferior
  (entmake (list '(0 . "LINE")
    (cons 10 (list x0 y 0.0))
    (cons 11 (list (+ x0 tw) y 0.0))
    '(8 . "QUADRO_CURVAS")))
  ;; Linhas verticais
  (setq xi x0)
  (entmake (list '(0 . "LINE")
    (cons 10 (list xi y 0.0))
    (cons 11 (list xi (+ y rh) 0.0))
    '(8 . "QUADRO_CURVAS")))
  (foreach w cw
    (setq xi (+ xi w))
    (entmake (list '(0 . "LINE")
      (cons 10 (list xi y 0.0))
      (cons 11 (list xi (+ y rh) 0.0))
      '(8 . "QUADRO_CURVAS")))
  )
)

;; ============================================================
;; Desenha retângulo (apenas borda, sem preenchimento)
;; ============================================================
(defun qc:draw-rect (x y w h)
  (entmake (list '(0 . "LWPOLYLINE") '(100 . "AcDbEntity")
    '(8 . "QUADRO_CURVAS") '(100 . "AcDbPolyline")
    '(90 . 4) '(70 . 1) '(43 . 0.0)
    (cons 10 (list x y))
    (cons 10 (list (+ x w) y))
    (cons 10 (list (+ x w) (+ y h)))
    (cons 10 (list x (+ y h)))
  ))
)

;; ============================================================
;; Insere texto MTEXT simples
;;   justify: "ML" (mid-left) | "MC" (mid-center) | "MR"
;; ============================================================
(defun qc:draw-text (x y str h justify / obj)
  (entmake (list
    '(0 . "TEXT")
    '(100 . "AcDbEntity")
    '(8 . "QUADRO_CURVAS")
    (cons 10 (list x y 0.0))
    (cons 11 (list x y 0.0))
    (cons 40 h)
    (cons 1 str)
    (cons 72 1)  ; horizontal: centered
    (cons 73 2)  ; vertical:   middle
  ))
)

;; ============================================================
;; Garante que o layer existe
;; ============================================================
(defun qc:ensure-layer (lname color / doc layers lay)
  (setq doc    (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq layers (vla-get-Layers doc))
  (if (vl-catch-all-error-p
        (vl-catch-all-apply 'vla-item (list layers lname)))
    (progn
      (setq lay (vla-add layers lname))
      (vla-put-color lay color)
    )
  )
)

(princ "\n:: QUADROCURVAS carregado. Digite QUADROCURVAS para executar.")
(princ)
