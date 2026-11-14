;;; ============================================================
;;; VolumeSeccoes.lsp  -  Volume de Seções - Método da Semidistância
;;; Civil 3D / AutoCAD  -  gera planilha Excel ao final
;;;
;;; Comando:  VOLSEC
;;;
;;; Fórmula: V_i = A_i × d_i  (área × semidistância)
;;; ============================================================

(vl-load-com)

;; Garante ponto como separador decimal (para JSON)
(defun vs:dot (val / s)
  (vl-string-subst "." "," (rtos val 2 6)))

;; Roda script externo via WScript.Shell (aguarda conclusão)
(defun vs:shell (cmd / wsh)
  (setq wsh (vlax-create-object "WScript.Shell"))
  (vlax-invoke wsh 'Run cmd 1 :vlax-true)
  (vlax-release-object wsh))

;; Formata número com 3 casas para exibição no prompt
(defun vs:fmt (val) (rtos val 2 3))

;; ============================================================
;;  Comando principal
;; ============================================================
(defun c:VOLSEC (/ lst n sta area sd vol total fh path py-path item i sep)

  (setq lst   '()
        n     1
        total 0.0)

  (princ "\n")
  (princ "╔══════════════════════════════════════════════════════╗\n")
  (princ "║  VOLUME DE SEÇÕES - Método da Semidistância          ║\n")
  (princ "║  V_i = Área_i × Semidistância_i                      ║\n")
  (princ "║  Digite ENTER vazio na estaca para finalizar.         ║\n")
  (princ "╚══════════════════════════════════════════════════════╝\n")

  ;; loop de coleta
  (while
    (progn
      (setq sta (getstring T (strcat "\nEstaca / Seção [" (itoa n) "] — ENTER para finalizar: ")))
      (not (= sta ""))
    )

    ;; área
    (initget (+ 2 4))
    (setq area (getreal (strcat "  Área da seção [" sta "] (m²): ")))
    (if (null area) (setq area 0.0))

    ;; semidistância
    (initget (+ 2 4))
    (setq sd (getreal (strcat "  Semidistância [" sta "] (m): ")))
    (if (null sd) (setq sd 0.0))

    (setq vol   (* area sd)
          total (+ total vol))

    (setq lst (append lst (list (list n sta area sd vol))))
    (princ (strcat "  → Volume parcial: " (vs:fmt vol) " m³   |   Acumulado: " (vs:fmt total) " m³"))
    (setq n (1+ n))
  )

  ;; nenhuma seção informada
  (if (null lst)
    (progn
      (princ "\nNenhuma seção informada. Encerrando.")
      (princ)
      (exit)
    )
  )

  ;; ---- escreve JSON temporário ----
  (setq path    "D:\\Claude ai\\vol_temp.json"
        py-path "D:\\Claude ai\\vol_to_excel.py")

  (setq fh (open path "w"))
  (write-line "[" fh)
  (setq i 0)
  (foreach item lst
    (setq i   (1+ i)
          sep (if (< i (length lst)) "," ""))
    (write-line
      (strcat "  {\"n\":"    (itoa   (car   item))
              ",\"sta\":\""  (cadr   item) "\""
              ",\"area\":"   (vs:dot (caddr  item))
              ",\"sd\":"     (vs:dot (cadddr item))
              ",\"vol\":"    (vs:dot (car (cddddr item)))
              "}" sep)
      fh)
  )
  (write-line "]" fh)
  (close fh)

  ;; ---- resumo na tela ----
  (princ (strcat "\n\n══════════════════════════════════════════\n"))
  (princ (strcat "  VOLUME TOTAL: " (vs:fmt total) " m³\n"))
  (princ (strcat "  Seções      : " (itoa (length lst)) "\n"))
  (princ (strcat "══════════════════════════════════════════\n"))

  ;; ---- gera Excel via Python ----
  (vs:shell (strcat "py \"" py-path "\""))

  (princ "\nPlanilha gerada: D:\\Claude ai\\volume_seccoes.xlsx")
  (princ)
)

(princ "\nVolumeSeccoes.lsp carregado.  Comando: VOLSEC")
(princ)
