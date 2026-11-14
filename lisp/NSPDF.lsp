;;; ============================================================================
;;; NSPDF.LSP
;;; Publica as folhas A4 de Notas de Servico (geradas no MODEL SPACE pelo
;;; Country Kit Brasil / DER-SP) em um UNICO PDF multipagina.
;;;
;;; Como funciona:
;;;   1. Voce seleciona UM exemplo do bloco de margem/carimbo da folha;
;;;   2. A rotina localiza todas as insercoes desse bloco no model space;
;;;   3. Ordena as folhas (por linhas ou por colunas);
;;;   4. Cria um layout A4 paisagem "NS-###" para cada folha, com viewport
;;;      travado enquadrando exatamente o bloco;
;;;   5. Salva o desenho, gera um arquivo .DSD e roda -PUBLISH com o
;;;      "DWG To PDF.pc3", produzindo um PDF unico multipagina na mesma
;;;      pasta do DWG.
;;;
;;; Comandos:
;;;   NSPDF     -> fluxo completo (cria layouts + publica o PDF)
;;;   NSLAYOUTS -> so cria os layouts (para conferir antes / publicar manual)
;;;   NSLIMPAR  -> apaga todos os layouts NS-### criados pela rotina
;;;
;;; Requisitos: desenho salvo em disco; blocos das folhas no model space.
;;; Autor: gerado para Isaque Ferraz Moreira - Civil 3D (Country Kit BR)
;;; ============================================================================

(vl-load-com)

;; ---------------------------------------------------------------- utilidades
(defun ns:pad3 (i)
  (cond ((< i 10) (strcat "00" (itoa i)))
        ((< i 100) (strcat "0" (itoa i)))
        (t (itoa i))))

(defun ns:effname (e / o)
  (setq o (vlax-ename->vla-object e))
  (if (vlax-property-available-p o 'EffectiveName)
      (vla-get-EffectiveName o)
      (vla-get-Name o)))

;; retorna (xmin ymin xmax ymax ename)
(defun ns:bbox (e / o mn mx a b)
  (setq o (vlax-ename->vla-object e))
  (vla-GetBoundingBox o 'mn 'mx)
  (setq a (vlax-safearray->list mn)
        b (vlax-safearray->list mx))
  (list (car a) (cadr a) (car b) (cadr b) e))

;; ordena por LINHAS: de cima p/ baixo, esquerda p/ direita
(defun ns:sort-rows (lst tol)
  (vl-sort lst
    (function (lambda (a b)
      (if (> (abs (- (cadr a) (cadr b))) tol)
          (> (cadr a) (cadr b))
          (< (car a) (car b)))))))

;; ordena por COLUNAS: esquerda p/ direita, de cima p/ baixo
(defun ns:sort-cols (lst tol)
  (vl-sort lst
    (function (lambda (a b)
      (if (> (abs (- (car a) (car b))) tol)
          (< (car a) (car b))
          (> (cadr a) (cadr b)))))))

;; coleta todas as insercoes do bloco no model space
(defun ns:collect (bname / ss i e lst)
  (setq ss (ssget "_X" '((0 . "INSERT") (410 . "Model")))
        i  0
        lst nil)
  (if ss
      (repeat (sslength ss)
        (setq e (ssname ss i) i (1+ i))
        (if (= (strcase (ns:effname e)) (strcase bname))
            (setq lst (cons (ns:bbox e) lst)))))
  lst)

;; escolhe midia A4 do DWG To PDF.pc3; retorna (nome rotacao)
(defun ns:pickmedia (lay / medias m mname rot)
  (setq medias (vlax-safearray->list
                 (vlax-variant-value (vla-GetCanonicalMediaNames lay)))
        mname nil rot 0)
  ;; 1a opcao: A4 paisagem (297 x 210)
  (foreach m medias
    (if (and (not mname) (wcmatch (strcase m) "*A4*297.00_X_210.00*"))
        (setq mname m rot 0)))
  ;; 2a opcao: A4 retrato (210 x 297) girado 90
  (if (not mname)
      (foreach m medias
        (if (and (not mname) (wcmatch (strcase m) "*A4*210.00_X_297.00*"))
            (setq mname m rot 1))))
  (list mname rot))

;; cria um layout A4 paisagem com viewport travado sobre o bbox
;; item = (xmin ymin xmax ymax ename)
(defun ns:makelayout (doc lname item / lays lay md mname rot pw ph mll mur
                       uw uh tmp bx1 by1 bx2 by2 bw bh sc vw vh x0 y0
                       ssv j vp vpobj)
  (setq lays (vla-get-Layouts doc))
  ;; remove layout homonimo de execucao anterior
  (if (tblsearch "LAYOUT" lname)
      (vl-catch-all-apply 'vla-Delete (list (vla-Item lays lname))))
  (setq lay (vla-Add lays lname))
  (vla-put-ConfigName lay "DWG To PDF.pc3")
  (vla-RefreshPlotDeviceInfo lay)
  (setq md (ns:pickmedia lay) mname (car md) rot (cadr md))
  (if (not mname)
      (progn (alert "Nao encontrei midia A4 no DWG To PDF.pc3.") (exit)))
  (vla-put-CanonicalMediaName lay mname)
  (vla-put-PaperUnits lay 1)                        ; milimetros
  (vla-put-PlotRotation lay rot)                    ; 0 = 0deg / 1 = 90deg
  (vla-put-PlotType lay 5)                          ; plotar o Layout
  (vl-catch-all-apply 'vla-put-UseStandardScale (list lay :vlax-true))
  (vl-catch-all-apply 'vla-put-StandardScale (list lay 16)) ; 1:1
  (vl-catch-all-apply 'vla-put-StyleSheet (list lay (vla-get-StyleSheet lay)))
  ;; area util do papel (mm)
  (vla-GetPaperSize lay 'pw 'ph)
  (vla-GetPaperMargins lay 'mll 'mur)
  (setq mll (vlax-safearray->list mll)
        mur (vlax-safearray->list mur)
        uw  (- pw (car mll) (car mur))
        uh  (- ph (cadr mll) (cadr mur)))
  (if (= rot 1) (setq tmp uw uw uh uh tmp))         ; papel girado 90
  ;; geometria da folha no model
  (setq bx1 (car item) by1 (cadr item)
        bx2 (caddr item) by2 (cadddr item)
        bw  (- bx2 bx1) bh (- by2 by1)
        sc  (min (/ uw bw) (/ uh bh))
        vw  (* bw sc) vh (* bh sc)
        x0  (/ (- uw vw) 2.0)
        y0  (/ (- uh vh) 2.0))
  ;; ativa o layout e limpa viewports automaticos
  (setvar 'CTAB lname)
  (setq ssv (ssget "_X" (list '(0 . "VIEWPORT") (cons 410 lname)
                              '(-4 . ">") '(69 . 1))))
  (if ssv
      (progn (setq j 0)
             (repeat (sslength ssv)
               (entdel (ssname ssv j))
               (setq j (1+ j)))))
  ;; cria o viewport enquadrando o bloco
  (command "._MVIEW" (list x0 y0) (list (+ x0 vw) (+ y0 vh)))
  (setq vp (entlast) vpobj (vlax-ename->vla-object vp))
  (command "._MSPACE")
  (command "._ZOOM" "_W" (list bx1 by1) (list bx2 by2))
  (command "._PSPACE")
  (vla-put-DisplayLocked vpobj :vlax-true)
  lname)

;; grava o arquivo DSD para o -PUBLISH (PDF unico multipagina)
(defun ns:writedsd (dwg pdf laynames / path f base ln)
  (setq path (strcat (getvar 'DWGPREFIX) "NSPDF_publish.dsd")
        base (vl-filename-base dwg)
        f    (open path "w"))
  (write-line "[DWF6Version]" f)
  (write-line "Ver=1" f)
  (write-line "[DWF6MinorVersion]" f)
  (write-line "MinorVer=1" f)
  (foreach ln laynames
    (write-line (strcat "[DWF6Sheet:" base "-" ln "]") f)
    (write-line (strcat "DWG=" dwg) f)
    (write-line (strcat "Layout=" ln) f)
    (write-line "Setup=" f)
    (write-line (strcat "OriginalSheetPath=" dwg) f)
    (write-line "Has Plot Port=0" f)
    (write-line "Has3DDWF=0" f))
  (write-line "[Target]" f)
  (write-line "Type=6" f)                            ; 6 = PDF
  (write-line (strcat "DWF=" pdf) f)
  (write-line (strcat "OUT=" (vl-filename-directory pdf) "\\") f)
  (write-line "PWD=" f)
  (write-line "[PdfOptions]" f)
  (write-line "IncludeHyperlinks=FALSE" f)
  (write-line "CreateBookmarks=TRUE" f)
  (write-line "CaptureFontsInDrawing=TRUE" f)
  (write-line "ConvertTextToGeometry=FALSE" f)
  (write-line "VectorResolution=1200" f)
  (write-line "RasterResolution=400" f)
  (write-line "[AutoCAD Block Data]" f)
  (write-line "IncludeBlockInfo=0" f)
  (write-line "BlockTmplFilePath=" f)
  (write-line "[SheetSet Properties]" f)
  (write-line "IsSheetSet=FALSE" f)
  (write-line "IsHomogeneous=FALSE" f)
  (close f)
  path)

;; nucleo: cria os layouts; retorna a lista de nomes criados
(defun ns:buildlayouts (doc / sel ent bname lst tol sorted ord n i laynames
                          hmed old-osm old-cmd)
  (setq sel (entsel "\nSelecione um exemplo do bloco de margem/carimbo da folha: "))
  (if (not sel) (progn (princ "\nNada selecionado.") (exit)))
  (setq ent (car sel))
  (if (/= "INSERT" (cdr (assoc 0 (entget ent))))
      (progn (alert "O objeto selecionado nao e um bloco (INSERT).") (exit)))
  (setq bname (ns:effname ent))
  (princ (strcat "\nBloco: " bname))
  (setq lst (ns:collect bname))
  (setq n (length lst))
  (princ (strcat "\nFolhas encontradas no model space: " (itoa n)))
  (if (= n 0) (progn (alert "Nenhuma folha encontrada.") (exit)))
  (if (> n 250)
      (progn (alert (strcat "Sao " (itoa n) " folhas - acima do limite de "
                            "layouts do AutoCAD (255).\nDivida o desenho ou "
                            "processe por partes."))
             (exit)))
  ;; altura media das folhas -> tolerancia de agrupamento
  (setq hmed 0.0)
  (foreach x lst (setq hmed (+ hmed (- (cadddr x) (cadr x)))))
  (setq hmed (/ hmed n) tol (* 0.4 hmed))
  ;; ordem de leitura
  (initget "Linhas Colunas")
  (setq ord (getkword "\nOrdem das folhas [Linhas/Colunas] <Linhas>: "))
  (setq sorted (if (= ord "Colunas")
                   (ns:sort-cols lst tol)
                   (ns:sort-rows lst tol)))
  ;; cria os layouts
  (setq old-osm (getvar 'OSMODE) old-cmd (getvar 'CMDECHO))
  (setvar 'OSMODE 0) (setvar 'CMDECHO 0)
  (vl-catch-all-apply 'setvar '(LAYOUTCREATEVIEWPORT 0))
  (setq i 0 laynames nil)
  (foreach item sorted
    (setq i (1+ i))
    (princ (strcat "\nCriando layout NS-" (ns:pad3 i) " ..."))
    (ns:makelayout doc (strcat "NS-" (ns:pad3 i)) item)
    (setq laynames (cons (strcat "NS-" (ns:pad3 i)) laynames)))
  (setvar 'OSMODE old-osm) (setvar 'CMDECHO old-cmd)
  (setvar 'CTAB "Model")
  (reverse laynames))

;; ------------------------------------------------------------ comando NSPDF
(defun C:NSPDF ( / doc laynames dwg pdf dsd old-fd old-bp *error*)
  (defun *error* (msg)
    (vl-catch-all-apply 'setvar (list 'FILEDIA (if old-fd old-fd 1)))
    (vl-catch-all-apply 'setvar (list 'BACKGROUNDPLOT (if old-bp old-bp 2)))
    (if (and msg (/= msg "Function cancelled"))
        (princ (strcat "\nErro: " msg)))
    (princ))
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (if (= 0 (getvar 'DWGTITLED))
      (progn (alert "Salve o desenho em disco antes de rodar o NSPDF.") (exit)))
  (setq laynames (ns:buildlayouts doc))
  (princ (strcat "\n" (itoa (length laynames)) " layouts criados. Salvando e publicando..."))
  ;; PUBLISH le o DWG do disco -> precisa salvar
  (vla-Save doc)
  (setq dwg (strcat (getvar 'DWGPREFIX) (getvar 'DWGNAME))
        pdf (strcat (getvar 'DWGPREFIX)
                    (vl-filename-base (getvar 'DWGNAME))
                    "_NotasServico.pdf")
        dsd (ns:writedsd dwg pdf laynames))
  (setq old-fd (getvar 'FILEDIA) old-bp (getvar 'BACKGROUNDPLOT))
  (setvar 'FILEDIA 0)
  (setvar 'BACKGROUNDPLOT 0)
  (vl-catch-all-apply 'setvar '(PUBLISHCOLLATE 1)) ; PDF unico multipagina
  (command "._-PUBLISH" dsd)
  (setvar 'FILEDIA old-fd)
  (setvar 'BACKGROUNDPLOT old-bp)
  (princ (strcat "\nPDF gerado: " pdf))
  (princ))

;; -------------------------------------------------------- comando NSLAYOUTS
(defun C:NSLAYOUTS ( / doc laynames)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (if (= 0 (getvar 'DWGTITLED))
      (progn (alert "Salve o desenho em disco antes de rodar.") (exit)))
  (setq laynames (ns:buildlayouts doc))
  (princ (strcat "\n" (itoa (length laynames))
                 " layouts criados. Confira e use PUBLISH (ou NSPDF) para gerar o PDF."))
  (princ))

;; --------------------------------------------------------- comando NSLIMPAR
(defun C:NSLIMPAR ( / doc lays n item nome)
  (setq doc  (vla-get-ActiveDocument (vlax-get-acad-object))
        lays (vla-get-Layouts doc)
        n    0)
  (setvar 'CTAB "Model")
  (vlax-for item lays
    (setq nome (vla-get-Name item))
    (if (wcmatch (strcase nome) "NS-###")
        (progn (vl-catch-all-apply 'vla-Delete (list item))
               (setq n (1+ n)))))
  (princ (strcat "\n" (itoa n) " layouts NS-### apagados."))
  (princ))

(princ "\nNSPDF.lsp carregado. Comandos: NSPDF | NSLAYOUTS | NSLIMPAR")
(princ)
