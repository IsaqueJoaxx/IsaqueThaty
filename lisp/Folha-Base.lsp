;;; ============================================================================
;;;  FOLHA-BASE.LSP
;;;  Propaga as configuracoes da FOLHA (layout) ATUAL para todas as outras
;;;  folhas do projeto (mesmo desenho).
;;;
;;;  Modulos aplicados:
;;;    1) PAGE SETUP / PLOTAGEM ...... papel, area, escala, CTB/STB, orientacao
;;;    2) CARIMBO / ATRIBUTOS ........ copia os valores dos atributos do carimbo
;;;    3) VIEWPORT / ESCALA .......... copia a escala da viewport principal
;;;    4) LAYERS POR VIEWPORT ........ copia o congelamento de layers (VP Freeze)
;;;
;;;  Comando:  FOLHABASE     (alias: FB)
;;;  Civil 3D / AutoCAD 2026 - pt-BR
;;; ============================================================================
;;;  OBS.: cores, estilos de texto/cota e estados de camada sao GLOBAIS ao
;;;  desenho (a mesma layer vale para todas as folhas), por isso o modulo de
;;;  layers trata do unico aspecto que e por-folha: o VP Freeze de cada viewport.
;;; ============================================================================

(vl-load-com)

;;; ----------------------------------------------------------------------------
;;; CONFIGURACAO DO USUARIO
;;; ----------------------------------------------------------------------------
;;; Tags de atributo do carimbo que NAO devem ser copiadas (ficam diferentes em
;;; cada folha, ex.: numero da prancha). Comparacao por "contem" (case-insensitive).
(setq *fb-carimbo-excluir*
  '("FOLHA" "PRANCHA" "NUMERO" "Nº" "N°" "SHEET" "ESCALA" "ESC"))

;;; ----------------------------------------------------------------------------
;;; HELPERS
;;; ----------------------------------------------------------------------------

;; Lista de layouts (vla) do desenho, exceto o Model. Mantem ordem por TabOrder.
(defun fb:layouts (doc / lst)
  (vlax-for lo (vla-get-Layouts doc)
    (if (/= (strcase (vla-get-Name lo)) "MODEL")
      (setq lst (cons lo lst))))
  (vl-sort lst '(lambda (a b) (< (vla-get-TabOrder a) (vla-get-TabOrder b)))))

;; Retorna o layout (vla) com o nome dado.
(defun fb:layout-by-name (doc nm / r)
  (vlax-for lo (vla-get-Layouts doc)
    (if (= (strcase (vla-get-Name lo)) (strcase nm)) (setq r lo)))
  r)

;; T se a tag deve ser excluida do carimbo.
(defun fb:tag-excluida (tag / ex hit)
  (setq tag (strcase tag))
  (foreach ex *fb-carimbo-excluir*
    (if (vl-string-search (strcase ex) tag) (setq hit T)))
  hit)

;; Bloco de paper space do layout.
(defun fb:ps (layout) (vla-get-Block layout))

;; ename da viewport "principal" de um layout: maior viewport flutuante,
;; ignorando a viewport de fundo do paper space (id 69 = 1).
(defun fb:main-vp (layout / best bestA en num area)
  (setq bestA -1.0)
  (vlax-for o (fb:ps layout)
    (if (= (vla-get-ObjectName o) "AcDbViewport")
      (progn
        (setq en  (vlax-vla-object->ename o)
              num (cdr (assoc 69 (entget en))))
        (if (or (null num) (/= num 1))
          (progn
            (setq area (* (vla-get-Width o) (vla-get-Height o)))
            (if (> area bestA) (setq bestA area  best en)))))))
  best)

;; Primeiro bloco com atributos do paper space que casa com o nome dado.
(defun fb:blkref-by-name (layout nm / r)
  (vlax-for o (fb:ps layout)
    (if (and (null r)
             (= (vla-get-ObjectName o) "AcDbBlockReference")
             (= (vla-get-HasAttributes o) :vlax-true)
             (= (strcase (vla-get-EffectiveName o)) (strcase nm)))
      (setq r o)))
  r)

;;; ----------------------------------------------------------------------------
;;; MODULO 1 - PAGE SETUP / PLOTAGEM
;;; ----------------------------------------------------------------------------
(defun fb:copy-pagesetup (base tgt)
  (if (vl-catch-all-error-p
        (vl-catch-all-apply 'vla-CopyFrom (list tgt base)))
    nil T))

;;; ----------------------------------------------------------------------------
;;; MODULO 2 - CARIMBO / ATRIBUTOS
;;; ----------------------------------------------------------------------------

;; Acha o bloco de carimbo na folha base = bloco atributado com mais atributos.
(defun fb:detecta-carimbo (layout / best bestN n nm)
  (setq bestN -1)
  (vlax-for o (fb:ps layout)
    (if (and (= (vla-get-ObjectName o) "AcDbBlockReference")
             (= (vla-get-HasAttributes o) :vlax-true))
      (progn
        (setq n (length (vlax-invoke o 'GetAttributes)))
        (if (> n bestN)
          (setq bestN n  best (vla-get-EffectiveName o))))))
  best) ; retorna o NOME do bloco (ou nil)

;; Le atributos (tag . valor) de um blockref, ignorando tags excluidas.
(defun fb:le-carimbo (blkref / res tag)
  (foreach a (vlax-invoke blkref 'GetAttributes)
    (setq tag (vla-get-TagString a))
    (if (not (fb:tag-excluida tag))
      (setq res (cons (cons (strcase tag) (vla-get-TextString a)) res))))
  res)

;; Aplica os valores no carimbo de uma folha alvo. Retorna nº de campos alterados.
(defun fb:aplica-carimbo (blkref data / n tag par)
  (setq n 0)
  (foreach a (vlax-invoke blkref 'GetAttributes)
    (setq tag (strcase (vla-get-TagString a))
          par (assoc tag data))
    (if (and par (not (fb:tag-excluida tag)))
      (progn (vla-put-TextString a (cdr par)) (setq n (1+ n)))))
  n)

;;; ----------------------------------------------------------------------------
;;; MODULO 3 - ESCALA DA VIEWPORT PRINCIPAL
;;; ----------------------------------------------------------------------------
(defun fb:copy-vp-escala (baseVp tgtVp / sc)
  (if (and baseVp tgtVp)
    (if (not (vl-catch-all-error-p
               (setq sc (vl-catch-all-apply
                          'vla-get-CustomScale
                          (list (vlax-ename->vla-object baseVp))))))
      (not (vl-catch-all-error-p
             (vl-catch-all-apply
               'vla-put-CustomScale
               (list (vlax-ename->vla-object tgtVp) sc)))))))

;;; ----------------------------------------------------------------------------
;;; MODULO 4 - VP FREEZE (congelamento de layers por viewport)
;;; ----------------------------------------------------------------------------
;; Copia os codigos 341 (layers congeladas na viewport) do base p/ o alvo.
(defun fb:copy-vp-freeze (baseVp tgtVp / base341 ed)
  (if (and baseVp tgtVp)
    (progn
      (setq base341 (vl-remove-if-not
                      '(lambda (x) (= 341 (car x)))
                      (entget baseVp)))
      (setq ed (entget tgtVp))
      (setq ed (vl-remove-if '(lambda (x) (= 341 (car x))) ed))
      (setq ed (append ed base341))
      (if (not (vl-catch-all-error-p (vl-catch-all-apply 'entmod (list ed))))
        (progn (entupd tgtVp) T)))))

;;; ----------------------------------------------------------------------------
;;; COMANDO PRINCIPAL
;;; ----------------------------------------------------------------------------
(defun c:FOLHABASE ( / doc baseName base alvos carimboNm carimboData
                       baseVp nFolhas nPS nCar nEsc nVpf )
  (vl-load-com)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq baseName (getvar "CTAB"))

  (cond
    ((= (strcase baseName) "MODEL")
     (princ "\n** Ative a FOLHA (layout) que sera a base - nao o Model. **"))

    ((progn
       (setq base  (fb:layout-by-name doc baseName)
             alvos (vl-remove-if '(lambda (l) (= (strcase (vla-get-Name l))
                                                 (strcase baseName)))
                                 (fb:layouts doc)))
       (null alvos))
     (princ "\n** Nao ha outras folhas no projeto para receber as configuracoes. **"))

    (T
     (princ (strcat "\n>> Folha base: \"" baseName "\"  |  alvos: "
                    (itoa (length alvos)) " folha(s)."))

     ;; --- prepara dados do carimbo da base ------------------------------------
     (setq carimboNm (fb:detecta-carimbo base))
     (if carimboNm
       (progn
         (setq carimboData (fb:le-carimbo (fb:blkref-by-name base carimboNm)))
         (princ (strcat "\n>> Carimbo detectado: \"" carimboNm "\" ("
                        (itoa (length carimboData)) " campos a copiar).")))
       (princ "\n>> Carimbo: nenhum bloco atributado encontrado na base (modulo ignorado)."))

     ;; --- viewport principal da base ------------------------------------------
     (setq baseVp (fb:main-vp base))

     ;; --- contadores ----------------------------------------------------------
     (setq nFolhas 0  nPS 0  nCar 0  nEsc 0  nVpf 0)

     ;; --- loop nas folhas alvo ------------------------------------------------
     (foreach lo alvos
       (setq nFolhas (1+ nFolhas))

       ;; 1) page setup
       (if (fb:copy-pagesetup base lo) (setq nPS (1+ nPS)))

       ;; 2) carimbo
       (if (and carimboNm carimboData)
         (let ((br (fb:blkref-by-name lo carimboNm)))
           (if (and br (> (fb:aplica-carimbo br carimboData) 0))
             (setq nCar (1+ nCar)))))

       ;; 3) escala + 4) vp freeze (usam a viewport principal do alvo)
       (let ((tgtVp (fb:main-vp lo)))
         (if (fb:copy-vp-escala  baseVp tgtVp) (setq nEsc (1+ nEsc)))
         (if (fb:copy-vp-freeze  baseVp tgtVp) (setq nVpf (1+ nVpf))))
     )

     ;; --- regenera para refletir escala/freeze --------------------------------
     (vla-Regen doc acAllViewports)

     ;; --- relatorio -----------------------------------------------------------
     (princ "\n----------------------------------------------------")
     (princ (strcat "\n RESUMO - " (itoa nFolhas) " folha(s) processada(s):"))
     (princ (strcat "\n   Page setup aplicado ..... " (itoa nPS)))
     (princ (strcat "\n   Carimbo preenchido ...... " (itoa nCar)))
     (princ (strcat "\n   Escala de viewport ...... " (itoa nEsc)))
     (princ (strcat "\n   VP Freeze copiado ....... " (itoa nVpf)))
     (princ "\n----------------------------------------------------")
    )
  )
  (princ)
)

;; Alias curto
(defun c:FB () (c:FOLHABASE))

(princ "\nFOLHA-BASE.LSP carregado.  Digite  FOLHABASE  (ou  FB) na folha base.")
(princ)
