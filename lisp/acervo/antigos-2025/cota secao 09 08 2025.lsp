;;; SECCOTA2.LSP – Cotas e distâncias em seção (MTEXT)
;;; Fluxo:
;;; 1) Seleciona polilinha da seção
;;; 2) Clica o ponto do eixo (sobre a polilinha) e informa a cota do eixo
;;; 3) Clica pontos na seção (Enter para finalizar)
;;; 4) Cria MTEXT em cada ponto com d (offset) e z (cota) + resumo final
;;; Obs.: Esquerda = negativo, Direita = positivo (eixo como origem)
;;; Requer: vlax-curve

(vl-load-com)

(defun _fmt (n) (rtos n 2 2))

(defun _toWCS (p) (trans p 1 0)) ; UCS -> WCS
(defun _toUCS (p) (trans p 0 1)) ; WCS -> UCS

(defun _nearest-on-curve (e pW / obj param)
  (setq obj (vlax-ename->vla-object e))
  (setq param (vlax-curve-getparamatpoint
                obj
                (vlax-curve-getclosestpointto obj pW)))
  (vlax-curve-getpointatparam obj param)
)

(defun _bbox (lstW / xs ys)
  (setq xs (mapcar 'car lstW)
        ys (mapcar 'cadr lstW))
  (list
    (list (apply 'min xs) (apply 'min ys) 0.0)
    (list (apply 'max xs) (apply 'max ys) 0.0)
  )
)

(defun c:SECCOTA2 (/ oldos ent dxeixoW pEixoU pEixoW eixoW zaxis sh sv
                    ptsU ptsW labels pU pW offx dz elev sign txtline
                    allW bbW baseW sumtxt)

  (setq oldos (getvar 'osmode))
  (setvar 'osmode 0)

  (prompt "\n[SECCOTA2] Selecione a POLILINHA da seção...")
  (setq ent (car (entsel "\nClique na polilinha: ")))
  (if (or (null ent)
          (not (wcmatch (strcase (cdr (assoc 0 (entget ent))))
                        "LWPOLYLINE,3DPOLYLINE,POLYLINE")))
    (progn (princ "\nEntidade inválida. Use POLYLINE.") (setvar 'osmode oldos) (princ))
    (progn
      (setq pEixoU (getpoint "\nClique no PONTO DO EIXO sobre a polilinha: "))
      (if (null pEixoU)
        (progn (princ "\nPonto inválido.") (setvar 'osmode oldos) (princ))
        (progn
          (setq pEixoW (_toWCS pEixoU))
          ;; Projeta o clique sobre a polilinha (ponto do eixo “no traçado”)
          (setq eixoW (_nearest-on-curve ent pEixoW))

          (setq zaxis (getreal "\nInforme a COTA do EIXO: "))
          (if (null zaxis) (setq zaxis 0.0))

          (setq sh (getreal "\nEscala HORIZONTAL (m por unidade do desenho) <1>: "))
          (if (null sh) (setq sh 1.0))
          (setq sv (getreal "\nEscala VERTICAL (m por unidade do desenho) <1>: "))
          (if (null sv) (setq sv 1.0))

          (prompt "\nAgora clique os pontos da seção (Enter para finalizar)...")
          (setq ptsU '() ptsW '() labels '())

          (while (setq pU (getpoint "\nPonto na seção <Enter encerra>: "))
            (setq pW (_toWCS pU))
            ;; offset em X relativo ao eixo (WCS). Esquerda<eixoX => negativo
            (setq offx (* (- (car pW) (car eixoW)) sh))
            ;; delta de cota em Y relativo ao eixo
            (setq dz   (* (- (cadr pW) (cadr eixoW)) sv))
            (setq elev (+ zaxis dz))
            (setq sign (if (< offx 0.0) "-" "+"))
            (setq txtline (strcat "d=" sign (_fmt (abs offx)) " m | z=" (_fmt elev) " m"))

            ;; MTEXT junto ao ponto (em UCS)
            (command "_.-layer" "_M" "SECCOTA" "_C" "3" "" "")
            (setvar 'clayer "SECCOTA")
            (command "_.mtext" pU "" txtline "")
            ;; acumula
            (setq ptsU (cons pU ptsU))
            (setq ptsW (cons pW ptsW))
            (setq labels (cons txtline labels))
          )

          ;; Resumo final
          (if labels
            (progn
              (setq labels (reverse labels)
                    ptsW   (reverse ptsW))
              (setq allW (cons eixoW ptsW))
              (setq bbW (_bbox allW))
              ;; canto superior-esquerdo da bbox (um pouco deslocado)
              (setq baseW (list
                            (car (car bbW))
                            (+ (cadr (cadr bbW)) (* 0.8 (max 1.0 (/ 1.0 sh))))
                            0.0))
              (setq sumtxt
                    (strcat
                      "SEÇÃO – Resumo\n"
                      (strcat "Eixo: z=" (_fmt zaxis) " m\n")
                      (apply 'strcat (mapcar '(lambda (s) (strcat s "\n")) labels))
                    ))
              (command "_.mtext" (_toUCS baseW) "" sumtxt "")
              (princ "\n[SECCOTA2] Concluído.")
            )
            (princ "\nNenhum ponto informado; nada a resumir.")
          )
        )
      )
    )
  )
  (setvar 'osmode oldos)
  (princ)
)
