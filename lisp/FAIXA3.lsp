;;; ============================================================================
;;;  FAIXA3.LSP  -  CRIADOR DE TERCEIRA FAIXA (FAIXA ADICIONAL)
;;;  AutoCAD Civil 3D 2026  (funciona tambem no AutoCAD puro)
;;; ----------------------------------------------------------------------------
;;;  O que faz:
;;;   - Pede o EIXO da rodovia (Alinhamento do Civil 3D ou Polilinha/Arco/Spline)
;;;   - Pede o INICIO e o FIM da terceira faixa (por ponto na tela ou por estaca/km)
;;;   - Pede a DISTANCIA do eixo ate o BORDO existente e a LARGURA da faixa
;;;   - Cria tapers de entrada e saida (comprimento configuravel - padrao 60 m)
;;;   - Desenha o novo bordo, fecha a area, hachura, rotula inicio/fim
;;;     e mostra um relatorio (extensao total, extensao plena, area)
;;;
;;;  Comandos:
;;;   FAIXA3      -> cria a terceira faixa
;;;   FAIXA3AJUDA -> mostra ajuda rapida
;;;
;;;  Camadas criadas automaticamente:
;;;   FX3-BORDO   (novo bordo da terceira faixa)
;;;   FX3-AREA    (poligono fechado da faixa)
;;;   FX3-HACHURA (hachura)
;;;   FX3-TEXTO   (rotulos)
;;; ============================================================================

(vl-load-com)

;;; ---------------------------------------------------------------- utilidades

(defun fx3:make-layer (name color / )
  (if (not (tblsearch "LAYER" name))
    (entmake (list '(0 . "LAYER")
                   '(100 . "AcDbSymbolTableRecord")
                   '(100 . "AcDbLayerTableRecord")
                   (cons 2 name)
                   (cons 62 color)
                   '(70 . 0)
                   '(6 . "Continuous"))))
  name)

;; formata estaca como "km X+YYY,YY"
(defun fx3:fmt (sta / km m)
  (setq km (fix (/ sta 1000.0))
        m  (- sta (* km 1000.0)))
  (if (< m 0.0) (setq m 0.0))
  (strcat "km " (itoa km) "+" (rtos m 2 2)))

;; ponto sobre a curva a uma distancia "dist" do inicio, deslocado "off" p/ o lado
(defun fx3:pt-offset (ent dist off side / param pt der len tx ty)
  (setq param (vlax-curve-getParamAtDist ent dist)
        pt    (vlax-curve-getPointAtParam ent param)
        der   (vlax-curve-getFirstDeriv ent param)
        len   (distance '(0.0 0.0 0.0) der))
  (if (< len 1e-10) (setq len 1.0))
  (setq tx (/ (car der) len)
        ty (/ (cadr der) len))
  ;; normal esquerda = (-ty, tx) ; normal direita = (ty, -tx)
  (if (= side "Esquerda")
    (list (+ (car pt)  (* off (- ty))) (+ (cadr pt) (* off tx)))
    (list (+ (car pt)  (* off ty))     (- (cadr pt) (* off tx)))))

;; alargamento w(d): 0 -> largura (taper entrada) -> largura -> 0 (taper saida)
(defun fx3:wid (d d1 d2 tin tout w)
  (cond ((or (<= d (+ d1 1e-8)) (>= d (- d2 1e-8))) 0.0)
        ((and (> tin 0.0) (< d (+ d1 tin)))  (* w (/ (- d d1) tin)))
        ((and (> tout 0.0) (> d (- d2 tout))) (* w (/ (- d2 d) tout)))
        (t w)))

;; lista de distancias de amostragem (inclui inicio, fim e quebras dos tapers)
(defun fx3:dists (d1 d2 step tin tout / l d)
  (setq l (list d1)
        d (+ d1 step))
  (while (< d (- d2 1e-6))
    (setq l (cons d l)
          d (+ d step)))
  (setq l (cons d2 l))
  (foreach b (list (+ d1 tin) (- d2 tout))
    (if (and (> b (+ d1 1e-6)) (< b (- d2 1e-6)))
      (setq l (cons b l))))
  (vl-sort l '<))

;; cria LWPOLYLINE a partir de lista de pontos 2D
(defun fx3:pline (pts layer color closed / )
  (entmake
    (append
      (list '(0 . "LWPOLYLINE")
            '(100 . "AcDbEntity")
            (cons 8 layer)
            (cons 62 color)
            '(100 . "AcDbPolyline")
            (cons 90 (length pts))
            (cons 70 (if closed 1 0)))
      (mapcar '(lambda (p) (cons 10 (list (car p) (cadr p)))) pts)))
  (entlast))

;; texto simples
(defun fx3:text (pt str h layer color / )
  (entmake (list '(0 . "TEXT")
                 '(100 . "AcDbEntity")
                 (cons 8 layer)
                 (cons 62 color)
                 '(100 . "AcDbText")
                 (cons 10 (list (car pt) (cadr pt) 0.0))
                 (cons 40 h)
                 (cons 1 str)
                 '(50 . 0.0)))
  (entlast))

;; mtext (rotulo central)
(defun fx3:mtext (pt str h layer color / )
  (entmake (list '(0 . "MTEXT")
                 '(100 . "AcDbEntity")
                 (cons 8 layer)
                 (cons 62 color)
                 '(100 . "AcDbMText")
                 (cons 10 (list (car pt) (cadr pt) 0.0))
                 (cons 40 h)
                 (cons 71 5)          ; ancorado no meio
                 (cons 1 str)))
  (entlast))

;; converte distancia ao longo do eixo em ponto-de-tela -> distancia
(defun fx3:dist-from-pick (ent msg / pt ptc)
  (setq pt (getpoint msg))
  (if pt
    (progn
      (setq ptc (vlax-curve-getClosestPointTo ent (trans pt 1 0) T))
      (vlax-curve-getDistAtPoint ent ptc))
    nil))

;; tenta ler a estaca inicial de um Alinhamento do Civil 3D (se houver COM)
(defun fx3:start-station (ent / obj r)
  (setq obj (vlax-ename->vla-object ent))
  (setq r (vl-catch-all-apply 'vlax-get (list obj 'StartingStation)))
  (if (vl-catch-all-error-p r) nil r))

;;; ---------------------------------------------------------------- ajuda

(defun C:FAIXA3AJUDA ()
  (princ "\n=========================================================")
  (princ "\n FAIXA3 - Terceira Faixa / Faixa Adicional")
  (princ "\n---------------------------------------------------------")
  (princ "\n 1. Selecione o eixo (alinhamento Civil 3D ou polilinha)")
  (princ "\n 2. Escolha o lado (Esquerda/Direita no sentido do estaqueamento)")
  (princ "\n 3. Informe dist. eixo->bordo, largura da faixa e tapers")
  (princ "\n 4. Defina inicio e fim clicando na tela ou digitando a estaca")
  (princ "\n Dica DNIT: taper minimo ~ largura x 25 (3,50 m -> ~ 90 m)")
  (princ "\n            largura usual da faixa adicional: 3,00 a 3,60 m")
  (princ "\n=========================================================")
  (princ))

;;; ---------------------------------------------------------------- principal

(defun C:FAIXA3 (/ *error* olderr oldecho
                   sel ent test lentot sta0 side bordo larg tin tout step th
                   mode d1 d2 s1 s2 tmp dl outer inner bound pts p pl-bordo
                   pl-area area ext extplena pmid plab hok r)

  (defun *error* (msg)
    (if oldecho (setvar "CMDECHO" oldecho))
    (command-s "_.UNDO" "_End")
    (if (and msg (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*QUIT*")))
      (princ (strcat "\nErro: " msg)))
    (princ))

  (setq oldecho (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)
  (command "_.UNDO" "_Begin")

  (princ "\n+---------------------------------------------+")
  (princ "\n|  FAIXA3 - Criador de Terceira Faixa  v1.0   |")
  (princ "\n|  (digite FAIXA3AJUDA para dicas de uso)     |")
  (princ "\n+---------------------------------------------+")

  ;; ---------- 1. eixo
  (setq sel (entsel "\nSelecione o EIXO da rodovia (alinhamento ou polilinha): "))
  (if (null sel) (progn (*error* nil) (exit)))
  (setq ent (car sel))
  (setq test (vl-catch-all-apply 'vlax-curve-getEndParam (list ent)))
  (if (vl-catch-all-error-p test)
    (progn
      (princ "\nObjeto selecionado nao e uma curva valida (use alinhamento, polilinha, arco ou spline).")
      (*error* nil)
      (exit)))
  (setq lentot (vlax-curve-getDistAtParam ent (vlax-curve-getEndParam ent)))
  (princ (strcat "\nExtensao do eixo: " (rtos lentot 2 2) " m"))

  ;; estaca inicial do eixo (le do alinhamento se possivel)
  (setq sta0 (fx3:start-station ent))
  (if sta0
    (princ (strcat "\nEstaca inicial lida do alinhamento: " (fx3:fmt sta0)))
    (progn
      (setq tmp (getreal "\nEstaca/km inicial do eixo em metros <0.00>: "))
      (setq sta0 (if tmp tmp 0.0))))

  ;; ---------- 2. lado
  (if (null *fx3-side*) (setq *fx3-side* "Direita"))
  (initget "Esquerda Direita")
  (setq side (getkword (strcat "\nLado da terceira faixa (sentido do estaqueamento) [Esquerda/Direita] <"
                               *fx3-side* ">: ")))
  (if (null side) (setq side *fx3-side*))
  (setq *fx3-side* side)

  ;; ---------- 3. geometria
  (if (null *fx3-bordo*) (setq *fx3-bordo* 3.60))
  (setq tmp (getdist (strcat "\nDistancia do EIXO ate o BORDO existente <"
                             (rtos *fx3-bordo* 2 2) ">: ")))
  (setq bordo (if tmp tmp *fx3-bordo*))
  (setq *fx3-bordo* bordo)

  (if (null *fx3-larg*) (setq *fx3-larg* 3.50))
  (setq tmp (getdist (strcat "\nLargura da terceira faixa <"
                             (rtos *fx3-larg* 2 2) ">: ")))
  (setq larg (if tmp tmp *fx3-larg*))
  (setq *fx3-larg* larg)

  (if (null *fx3-tin*) (setq *fx3-tin* 60.0))
  (setq tmp (getdist (strcat "\nComprimento do TAPER de entrada <"
                             (rtos *fx3-tin* 2 2) ">: ")))
  (setq tin (if tmp tmp *fx3-tin*))
  (setq *fx3-tin* tin)

  (if (null *fx3-tout*) (setq *fx3-tout* 60.0))
  (setq tmp (getdist (strcat "\nComprimento do TAPER de saida <"
                             (rtos *fx3-tout* 2 2) ">: ")))
  (setq tout (if tmp tmp *fx3-tout*))
  (setq *fx3-tout* tout)

  (if (null *fx3-step*) (setq *fx3-step* 2.0))
  (setq tmp (getdist (strcat "\nPasso de discretizacao em curva <"
                             (rtos *fx3-step* 2 2) ">: ")))
  (setq step (if tmp tmp *fx3-step*))
  (if (< step 0.1) (setq step 0.1))
  (setq *fx3-step* step)

  ;; ---------- 4. inicio e fim
  (initget "Ponto Estaca")
  (setq mode (getkword "\nDefinir inicio/fim por [Ponto na tela/Estaca digitada] <Ponto>: "))
  (if (null mode) (setq mode "Ponto"))

  (if (= mode "Ponto")
    (progn
      (setq d1 (fx3:dist-from-pick ent "\nClique no INICIO da terceira faixa (proximo ao eixo): "))
      (setq d2 (fx3:dist-from-pick ent "\nClique no FIM da terceira faixa: ")))
    (progn
      (setq s1 (getreal "\nEstaca de INICIO (m): ")
            s2 (getreal "\nEstaca de FIM (m): "))
      (if (and s1 s2)
        (setq d1 (- s1 sta0)
              d2 (- s2 sta0)))))

  (if (or (null d1) (null d2))
    (progn (princ "\nInicio/fim nao definidos. Comando cancelado.")
           (*error* nil) (exit)))

  (if (> d1 d2) (setq tmp d1 d1 d2 d2 tmp))           ; garante ordem
  (if (< d1 0.0) (setq d1 0.0))                       ; limita ao eixo
  (if (> d2 lentot) (setq d2 lentot))

  (if (< (- d2 d1) (+ tin tout))
    (progn
      (princ (strcat "\nAVISO: extensao (" (rtos (- d2 d1) 2 2)
                     " m) menor que a soma dos tapers ("
                     (rtos (+ tin tout) 2 2) " m)."))
      (princ "\nOs tapers serao reduzidos proporcionalmente.")
      (setq tmp  (/ (- d2 d1) (+ tin tout))
            tin  (* tin tmp 0.999)
            tout (* tout tmp 0.999))))

  ;; ---------- 5. geracao dos pontos
  (princ "\nGerando geometria...")
  (setq dl (fx3:dists d1 d2 step tin tout)
        outer '()
        inner '())
  (foreach d dl
    (setq outer (cons (fx3:pt-offset ent d (+ bordo (fx3:wid d d1 d2 tin tout larg)) side) outer)
          inner (cons (fx3:pt-offset ent d bordo side) inner)))
  (setq outer (reverse outer))   ; novo bordo (com tapers)
  ;; inner ficou na ordem reversa - perfeito para fechar o poligono

  ;; ---------- 6. camadas e desenho
  (fx3:make-layer "FX3-BORDO"   1)   ; vermelho
  (fx3:make-layer "FX3-AREA"    8)   ; cinza
  (fx3:make-layer "FX3-HACHURA" 252)
  (fx3:make-layer "FX3-TEXTO"   2)   ; amarelo

  (setq pl-bordo (fx3:pline outer "FX3-BORDO" 1 nil))
  (setq pts (append outer inner))
  (setq pl-area (fx3:pline pts "FX3-AREA" 8 T))
  (setq area (vl-catch-all-apply 'vlax-curve-getArea (list pl-area)))
  (if (vl-catch-all-error-p area) (setq area 0.0))

  ;; ---------- 7. hachura (opcional)
  (initget "Sim Nao")
  (setq hok (getkword "\nHachurar a area da faixa? [Sim/Nao] <Sim>: "))
  (if (or (null hok) (= hok "Sim"))
    (progn
      (setq r (vl-catch-all-apply
                'vl-cmdf
                (list "_.-HATCH" "_P" "ANSI31" "2.0" "45" "_S" pl-area "" "")))
      (if (and (not (vl-catch-all-error-p r)) (entlast))
        (progn
          (setq r (entget (entlast)))
          (if (= (cdr (assoc 0 r)) "HATCH")
            (entmod (subst (cons 8 "FX3-HACHURA") (assoc 8 r) r)))))))

  ;; ---------- 8. rotulos
  (setq th (max 1.5 (* larg 0.5)))
  (fx3:text (fx3:pt-offset ent d1 (+ bordo larg 2.0) side)
            (strcat "INICIO 3a FAIXA " (fx3:fmt (+ sta0 d1)))
            th "FX3-TEXTO" 2)
  (fx3:text (fx3:pt-offset ent d2 (+ bordo larg 2.0) side)
            (strcat "FIM 3a FAIXA " (fx3:fmt (+ sta0 d2)))
            th "FX3-TEXTO" 2)
  (setq pmid (fx3:pt-offset ent (/ (+ d1 d2) 2.0) (+ bordo larg 4.0 th) side))
  (fx3:mtext pmid
             (strcat "TERCEIRA FAIXA - LADO " (strcase side)
                     "\\PLargura: " (rtos larg 2 2) " m"
                     "\\PTapers: " (rtos tin 2 1) " m / " (rtos tout 2 1) " m"
                     "\\P" (fx3:fmt (+ sta0 d1)) " a " (fx3:fmt (+ sta0 d2)))
             th "FX3-TEXTO" 2)

  ;; ---------- 9. relatorio
  (setq ext      (- d2 d1)
        extplena (- ext tin tout))
  (princ "\n=============== RELATORIO DA TERCEIRA FAIXA ===============")
  (princ (strcat "\n Lado............: " side))
  (princ (strcat "\n Inicio..........: " (fx3:fmt (+ sta0 d1))))
  (princ (strcat "\n Fim.............: " (fx3:fmt (+ sta0 d2))))
  (princ (strcat "\n Extensao total..: " (rtos ext 2 2) " m"))
  (princ (strcat "\n Largura plena...: " (rtos extplena 2 2) " m (descontados tapers)"))
  (princ (strcat "\n Largura da faixa: " (rtos larg 2 2) " m"))
  (princ (strcat "\n Area aproximada.: " (rtos area 2 2) " m2"))
  (princ "\n Camadas: FX3-BORDO / FX3-AREA / FX3-HACHURA / FX3-TEXTO")
  (princ "\n===========================================================")

  (setvar "CMDECHO" oldecho)
  (command "_.UNDO" "_End")
  (princ))

(princ "\nFAIXA3.LSP carregado. Digite FAIXA3 para criar a terceira faixa, ou FAIXA3AJUDA para ajuda.")
(princ)
