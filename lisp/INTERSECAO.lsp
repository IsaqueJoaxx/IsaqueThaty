;;; ============================================================================
;;;  INTERSECAO.LSP  -  GERADOR DE INTERSECAO RODOVIARIA EM 3D
;;;  AutoCAD Civil 3D 2026  (funciona tambem no AutoCAD puro)
;;; ----------------------------------------------------------------------------
;;;  O que faz:
;;;   - Pede DOIS tracados (alinhamentos do Civil 3D, polilinhas, arcos...)
;;;   - Localiza a JUNCAO automaticamente (ou voce clica, se houver mais de uma)
;;;   - Pede a COTA DE PROJETO da juncao
;;;   - Pede meia-largura de cada via, raio de concordancia (curb return),
;;;     abaulamento (%) e declividades longitudinais a partir da juncao
;;;   - Gera, em 3D, nos 4 quadrantes (ou 2/3 em "T"):
;;;       . bordas de pavimento com curvas de concordancia  (breaklines)
;;;       . eixos com greide aplicado                       (breaklines)
;;;       . fechamentos de extremidade                      (breaklines)
;;;       . SUPERFICIE DE PROJETO em 3DFACES (malha triangulada)
;;;       . rotulos de raio, marcador e cota da juncao
;;;   - Relatorio final: estacas da juncao, quadrantes gerados, area pavimentada
;;;
;;;  Para criar a SUPERFICIE TIN no Civil 3D depois de rodar:
;;;   1. Prospector > Surfaces > botao direito > Create Surface
;;;   2. Definition > Drawing Objects > tipo "3D Faces" > selecione a camada
;;;      INT-FACES  (isole a camada e faca janela)
;;;   3. (Opcional) Definition > Breaklines > adicione as 3D polylines das
;;;      camadas INT-BORDA e INT-EIXO3D
;;;
;;;  Para o CORREDOR, use o script Dynamo "IntersecaoCorredor.py" que acompanha.
;;;
;;;  Comandos:  INTERSECAO   |   INTERSECAOAJUDA
;;;
;;;  Camadas:  INT-BORDA / INT-EIXO3D / INT-FACES / INT-TEXTO
;;; ============================================================================

(vl-load-com)

;;; ---------------------------------------------------------------- utilidades

(defun int:make-layer (name color / )
  (if (not (tblsearch "LAYER" name))
    (entmake (list '(0 . "LAYER")
                   '(100 . "AcDbSymbolTableRecord")
                   '(100 . "AcDbLayerTableRecord")
                   (cons 2 name) (cons 62 color) '(70 . 0)
                   '(6 . "Continuous"))))
  name)

(defun int:fmt (sta / km m)
  (setq km (fix (/ sta 1000.0)) m (- sta (* km 1000.0)))
  (if (< m 0.0) (setq m 0.0))
  (strcat "km " (itoa km) "+" (rtos m 2 2)))

;; tenta ler a estaca inicial de um alinhamento Civil 3D
(defun int:start-station (ent / r)
  (setq r (vl-catch-all-apply 'vlax-get
            (list (vlax-ename->vla-object ent) 'StartingStation)))
  (if (vl-catch-all-error-p r) nil r))

;; ponto 2D do eixo na distancia d
(defun int:axis-xy (ent d / p)
  (setq p (vlax-curve-getPointAtDist ent d))
  (list (car p) (cadr p)))

;; tangente unitaria na distancia d
(defun int:tan (ent d / pr der len)
  (setq pr  (vlax-curve-getParamAtDist ent d)
        der (vlax-curve-getFirstDeriv ent pr)
        len (distance '(0.0 0.0 0.0) der))
  (if (< len 1e-10) (setq len 1.0))
  (list (/ (car der) len) (/ (cadr der) len)))

;; ponto 2D deslocado w para o lado s (+1 esquerda / -1 direita)
(defun int:off-xy (ent d s w / p u)
  (setq p (int:axis-xy ent d)
        u (int:tan ent d))
  (list (- (car p)  (* s w (cadr u)))
        (+ (cadr p) (* s w (car u)))))

;; estaca (distancia ao longo do eixo) do ponto 2D mais proximo
(defun int:sta-of (ent ptxy / cp)
  (setq cp (vlax-curve-getClosestPointTo ent (list (car ptxy) (cadr ptxy) 0.0)))
  (vlax-curve-getDistAtPoint ent cp))

;; lista de distancias de a ate b (qualquer sentido), passo "step"
(defun int:rng (a b step / l d)
  (setq l (list a))
  (cond ((> b a) (setq d (+ a step))
                 (while (< d (- b 1e-6)) (setq l (cons d l) d (+ d step))))
        (t       (setq d (- a step))
                 (while (> d (+ b 1e-6)) (setq l (cons d l) d (- d step)))))
  (reverse (cons b l)))

;; cota do eixo: cota da juncao + greide (%) x distancia ate a juncao
(defun int:z (zj g dj d)
  (+ zj (* 0.01 g (abs (- d dj)))))

;; polilinha 3D (breakline)
(defun int:3dpoly (pts layer color / )
  (entmake (list '(0 . "POLYLINE") '(100 . "AcDbEntity")
                 (cons 8 layer) (cons 62 color)
                 '(100 . "AcDb3dPolyline") '(70 . 8) '(10 0.0 0.0 0.0)))
  (foreach p pts
    (entmake (list '(0 . "VERTEX") '(100 . "AcDbEntity") (cons 8 layer)
                   '(100 . "AcDbVertex") '(100 . "AcDb3dPolylineVertex")
                   (cons 10 p) '(70 . 32))))
  (entmake (list '(0 . "SEQEND") '(100 . "AcDbEntity") (cons 8 layer)))
  (entlast))

(defun int:tri-area (a b c)
  (abs (* 0.5 (- (* (- (car b) (car a)) (- (cadr c) (cadr a)))
                 (* (- (cadr b) (cadr a)) (- (car c) (car a)))))))

;; cria 3DFACE (triangulo) e devolve sua area em planta
(defun int:face (p1 p2 p3 layer / a)
  (setq a (int:tri-area p1 p2 p3))
  (if (> a 1e-6)
    (entmake (list '(0 . "3DFACE") (cons 8 layer) (cons 62 8)
                   (cons 10 p1) (cons 11 p2) (cons 12 p3) (cons 13 p3))))
  a)

;; fracoes acumuladas (0..1) ao longo de uma cadeia de pontos
(defun int:cum (pts / tot acc prev)
  (setq tot 0.0 acc '(0.0) prev (car pts))
  (foreach p (cdr pts)
    (setq tot (+ tot (distance prev p))
          acc (cons tot acc)
          prev p))
  (if (< tot 1e-9) (setq tot 1.0))
  (reverse (mapcar '(lambda (x) (/ x tot)) acc)))

;; triangulacao "ziper" entre duas cadeias 3D -> 3DFACES; devolve area total
(defun int:zip (ca cb layer / fa fb ia ib la lb area)
  (setq fa (int:cum ca) fb (int:cum cb)
        ia 0 ib 0
        la (1- (length ca)) lb (1- (length cb))
        area 0.0)
  (while (or (< ia la) (< ib lb))
    (if (and (< ia la)
             (or (>= ib lb) (<= (nth (1+ ia) fa) (nth (1+ ib) fb))))
      (progn
        (setq area (+ area (int:face (nth ia ca) (nth (1+ ia) ca) (nth ib cb) layer)))
        (setq ia (1+ ia)))
      (progn
        (setq area (+ area (int:face (nth ia ca) (nth (1+ ib) cb) (nth ib cb) layer)))
        (setq ib (1+ ib)))))
  area)

(defun int:text (pt str h layer color / )
  (entmake (list '(0 . "TEXT") '(100 . "AcDbEntity")
                 (cons 8 layer) (cons 62 color)
                 '(100 . "AcDbText")
                 (cons 10 (list (car pt) (cadr pt) 0.0))
                 (cons 40 h) (cons 1 str) '(50 . 0.0))))

(defun int:mtext (pt str h layer color / )
  (entmake (list '(0 . "MTEXT") '(100 . "AcDbEntity")
                 (cons 8 layer) (cons 62 color)
                 '(100 . "AcDbMText")
                 (cons 10 (list (car pt) (cadr pt) 0.0))
                 (cons 40 h) (cons 71 5) (cons 1 str))))

;; intersecoes entre duas entidades -> lista de pontos 3D
(defun int:intersections (e1 e2 / var arr pts)
  (setq var (vl-catch-all-apply 'vla-IntersectWith
              (list (vlax-ename->vla-object e1)
                    (vlax-ename->vla-object e2) 0)))   ; 0 = acExtendNone
  (if (vl-catch-all-error-p var)
    nil
    (progn
      (setq arr (vl-catch-all-apply 'vlax-safearray->list
                  (list (vlax-variant-value var))))
      (if (vl-catch-all-error-p arr)
        nil
        (progn
          (setq pts '())
          (while arr
            (setq pts (cons (list (car arr) (cadr arr) (caddr arr)) pts)
                  arr (cdddr arr)))
          (reverse pts))))))

;;; ---------------------------------------------------------------- ajuda

(defun C:INTERSECAOAJUDA ()
  (princ "\n=============================================================")
  (princ "\n INTERSECAO - Gerador de intersecao rodoviaria 3D")
  (princ "\n-------------------------------------------------------------")
  (princ "\n 1. Selecione os dois eixos (alinhamento ou polilinha)")
  (princ "\n 2. Informe a cota de projeto da juncao")
  (princ "\n 3. Informe meia-larguras, raio de concordancia, abaulamento")
  (princ "\n    e declividades longitudinais a partir da juncao")
  (princ "\n 4. A rotina gera breaklines 3D + superficie em 3DFACES")
  (princ "\n Dicas: raio curb return urbano 6-12 m / rodovia 15-30 m")
  (princ "\n        declividade negativa = agua escoa PARA FORA da juncao")
  (princ "\n Superficie TIN: Create Surface > Definition > Drawing Objects")
  (princ "\n                 (3D Faces, camada INT-FACES)")
  (princ "\n Corredor: use o script Dynamo IntersecaoCorredor.py")
  (princ "\n=============================================================")
  (princ))

;;; ---------------------------------------------------------------- principal

(defun C:INTERSECAO (/ *error* oldecho
   sel e1 e2 test len1 len2 sta01 sta02
   ipts ptmp p1 p2 p px py zj
   w1 w2 rr ee g1 g2 l1 l2 step th tmp fok
   dj1 dj2 u1 u2 n1 n2 det
   c1 c2 vx vy cx cy t1 t2 st1 st2 q1 q2 de1 de2 errw
   ang1 ang2 delta narc zt1 zt2 k aa arcpts
   chout chin areatot nquad capkeys capdata key
   via ent djx g lx lent a b dmin)

  (defun *error* (msg)
    (if oldecho (setvar "CMDECHO" oldecho))
    (command-s "_.UNDO" "_End")
    (if (and msg (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*QUIT*")))
      (princ (strcat "\nErro: " msg)))
    (princ))

  (setq oldecho (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)
  (command "_.UNDO" "_Begin")

  (princ "\n+------------------------------------------------+")
  (princ "\n|  INTERSECAO - Intersecao Rodoviaria 3D  v1.0   |")
  (princ "\n|  (digite INTERSECAOAJUDA para dicas)           |")
  (princ "\n+------------------------------------------------+")

  ;; ---------- 1. eixos
  (setq sel (entsel "\nSelecione o eixo da VIA 1 (principal): "))
  (if (null sel) (progn (*error* nil) (exit)))
  (setq e1 (car sel))
  (setq sel (entsel "\nSelecione o eixo da VIA 2 (secundaria): "))
  (if (null sel) (progn (*error* nil) (exit)))
  (setq e2 (car sel))
  (foreach ent (list e1 e2)
    (setq test (vl-catch-all-apply 'vlax-curve-getEndParam (list ent)))
    (if (vl-catch-all-error-p test)
      (progn (princ "\nUm dos objetos nao e curva valida.") (*error* nil) (exit))))
  (setq len1 (vlax-curve-getDistAtParam e1 (vlax-curve-getEndParam e1))
        len2 (vlax-curve-getDistAtParam e2 (vlax-curve-getEndParam e2)))

  ;; ---------- 2. juncao
  (setq ipts (int:intersections e1 e2))
  (cond
    ((null ipts)
     (princ "\nNenhuma intersecao automatica encontrada.")
     (setq ptmp (getpoint "\nClique no ponto de juncao aproximado: "))
     (if (null ptmp) (progn (*error* nil) (exit)))
     (setq p1 (vlax-curve-getClosestPointTo e1 (trans ptmp 1 0))
           p2 (vlax-curve-getClosestPointTo e2 p1))
     (if (> (distance p1 p2) 1.0)
       (progn (princ "\nOs eixos nao se tocam nesse ponto (afastamento > 1 m).")
              (*error* nil) (exit)))
     (setq p (mapcar '(lambda (x y) (/ (+ x y) 2.0)) p1 p2)))
    ((= (length ipts) 1)
     (setq p (car ipts)))
    (t
     (princ (strcat "\nForam encontradas " (itoa (length ipts)) " intersecoes."))
     (setq ptmp (getpoint "\nClique proximo a juncao desejada: "))
     (if (null ptmp) (progn (*error* nil) (exit)))
     (setq ptmp (trans ptmp 1 0) dmin 1e99)
     (foreach tmp ipts
       (if (< (distance tmp ptmp) dmin)
         (setq dmin (distance tmp ptmp) p tmp)))))
  (setq px (car p) py (cadr p)
        dj1 (int:sta-of e1 (list px py))
        dj2 (int:sta-of e2 (list px py)))
  (princ (strcat "\nJuncao localizada em X=" (rtos px 2 3) " Y=" (rtos py 2 3)))

  ;; checa esconsidade
  (setq u1 (int:tan e1 dj1) u2 (int:tan e2 dj2)
        n1 (list (- (cadr u1)) (car u1))
        n2 (list (- (cadr u2)) (car u2))
        det (- (* (car n1) (cadr n2)) (* (cadr n1) (car n2))))
  (if (< (abs det) 0.05)
    (progn (princ "\nOs eixos sao quase paralelos na juncao. Abortado.")
           (*error* nil) (exit)))
  (if (< (abs det) 0.34)
    (princ "\nAVISO: intersecao muito esconsa (<20 graus). Confira o resultado."))

  ;; ---------- 3. dados de projeto
  (setq zj (getreal "\nCOTA de projeto da juncao (m): "))
  (if (null zj) (progn (*error* nil) (exit)))

  (if (null *int-w1*) (setq *int-w1* 3.50))
  (setq tmp (getdist (strcat "\nMeia-largura da VIA 1 (eixo->bordo) <"
                             (rtos *int-w1* 2 2) ">: ")))
  (setq w1 (if tmp tmp *int-w1*) *int-w1* w1)

  (if (null *int-w2*) (setq *int-w2* 3.50))
  (setq tmp (getdist (strcat "\nMeia-largura da VIA 2 (eixo->bordo) <"
                             (rtos *int-w2* 2 2) ">: ")))
  (setq w2 (if tmp tmp *int-w2*) *int-w2* w2)

  (if (null *int-rr*) (setq *int-rr* 15.0))
  (setq tmp (getdist (strcat "\nRaio de concordancia (curb return) <"
                             (rtos *int-rr* 2 2) ">: ")))
  (setq rr (if tmp tmp *int-rr*) *int-rr* rr)

  (if (null *int-ee*) (setq *int-ee* 2.0))
  (setq tmp (getreal (strcat "\nAbaulamento / declividade transversal (%) <"
                             (rtos *int-ee* 2 2) ">: ")))
  (setq ee (if tmp tmp *int-ee*) *int-ee* ee)

  (if (null *int-g1*) (setq *int-g1* -1.0))
  (setq tmp (getreal (strcat "\nDeclividade longitudinal VIA 1 a partir da juncao (%; - desce) <"
                             (rtos *int-g1* 2 2) ">: ")))
  (setq g1 (if tmp tmp *int-g1*) *int-g1* g1)

  (if (null *int-g2*) (setq *int-g2* -1.0))
  (setq tmp (getreal (strcat "\nDeclividade longitudinal VIA 2 a partir da juncao (%; - desce) <"
                             (rtos *int-g2* 2 2) ">: ")))
  (setq g2 (if tmp tmp *int-g2*) *int-g2* g2)

  (if (null *int-l1*) (setq *int-l1* (+ rr 30.0)))
  (setq tmp (getdist (strcat "\nExtensao do modelo na VIA 1 a partir da juncao <"
                             (rtos *int-l1* 2 2) ">: ")))
  (setq l1 (if tmp tmp *int-l1*) *int-l1* l1)

  (if (null *int-l2*) (setq *int-l2* (+ rr 30.0)))
  (setq tmp (getdist (strcat "\nExtensao do modelo na VIA 2 a partir da juncao <"
                             (rtos *int-l2* 2 2) ">: ")))
  (setq l2 (if tmp tmp *int-l2*) *int-l2* l2)

  (if (null *int-step*) (setq *int-step* 2.0))
  (setq tmp (getdist (strcat "\nPasso de discretizacao <"
                             (rtos *int-step* 2 2) ">: ")))
  (setq step (if tmp tmp *int-step*))
  (if (< step 0.25) (setq step 0.25))
  (setq *int-step* step)

  (initget "Sim Nao")
  (setq tmp (getkword "\nGerar superficie de projeto (3DFACES)? [Sim/Nao] <Sim>: "))
  (setq fok (or (null tmp) (= tmp "Sim")))

  ;; ---------- 4. camadas
  (int:make-layer "INT-BORDA"  1)   ; vermelho  - bordas/concordancias
  (int:make-layer "INT-EIXO3D" 3)   ; verde     - eixos com greide
  (int:make-layer "INT-FACES"  8)   ; cinza     - superficie de projeto
  (int:make-layer "INT-TEXTO"  2)   ; amarelo   - rotulos
  (setq th (max 1.0 (* 0.3 (min w1 w2))))

  ;; ---------- 5. quadrantes
  (princ "\nGerando quadrantes...")
  (setq areatot 0.0 nquad 0 capkeys '() capdata '())
  (foreach s1 '(1.0 -1.0)
    (foreach s2 '(1.0 -1.0)
      ;; centro da concordancia: dist (w+R) de cada eixo, lado s
      (setq c1 (* s1 (+ w1 rr))
            c2 (* s2 (+ w2 rr))
            vx (/ (- (* c1 (cadr n2)) (* c2 (cadr n1))) det)
            vy (/ (- (* (car n1) c2) (* (car n2) c1)) det)
            cx (+ px vx) cy (+ py vy)
            t1 (list (- cx (* s1 rr (car n1))) (- cy (* s1 rr (cadr n1))))
            t2 (list (- cx (* s2 rr (car n2))) (- cy (* s2 rr (cadr n2))))
            st1 (int:sta-of e1 t1)
            st2 (int:sta-of e2 t2))
      (setq q1  (if (> st1 dj1) 1.0 -1.0)
            q2  (if (> st2 dj2) 1.0 -1.0)
            de1 (max 0.0 (min len1 (+ dj1 (* q1 l1))))
            de2 (max 0.0 (min len2 (+ dj2 (* q2 l2)))))
      (if (and (> (abs (- st1 dj1)) 0.05)
               (> (abs (- st2 dj2)) 0.05)
               (> (abs (- de1 dj1)) (+ (abs (- st1 dj1)) 0.01))
               (> (abs (- de2 dj2)) (+ (abs (- st2 dj2)) 0.01)))
        (progn
          ;; sanidade em eixos curvos
          (setq errw (abs (- (distance t1 (int:axis-xy e1 st1)) w1)))
          (if (> errw 0.30)
            (princ (strcat "\n  AVISO: eixo curvo na concordancia (desvio "
                           (rtos errw 2 2) " m). Confira.")))
          ;; --- borda externa: via1 -> arco -> via2
          (setq chout '())
          (foreach a (int:rng de1 st1 step)
            (setq chout (cons (append (int:off-xy e1 a s1 w1)
                                      (list (- (int:z zj g1 dj1 a) (* 0.01 ee w1))))
                              chout)))
          (setq chout (reverse chout))
          (setq ang1 (angle (list cx cy) t1)
                ang2 (angle (list cx cy) t2)
                delta (- ang2 ang1))
          (while (> delta pi) (setq delta (- delta pi pi)))
          (while (< delta (- pi)) (setq delta (+ delta pi pi)))
          (setq narc (max 8 (fix (/ (* rr (abs delta)) step)))
                zt1 (- (int:z zj g1 dj1 st1) (* 0.01 ee w1))
                zt2 (- (int:z zj g2 dj2 st2) (* 0.01 ee w2))
                arcpts '()
                k 1)
          (while (<= k narc)
            (setq aa (+ ang1 (* delta (/ (float k) narc))))
            (setq arcpts (cons (list (+ cx (* rr (cos aa)))
                                     (+ cy (* rr (sin aa)))
                                     (+ zt1 (* (- zt2 zt1) (/ (float k) narc))))
                               arcpts)
                  k (1+ k)))
          (setq arcpts (reverse arcpts))
          (setq tmp '())
          (foreach a (int:rng st2 de2 step)
            (setq tmp (cons (append (int:off-xy e2 a s2 w2)
                                    (list (- (int:z zj g2 dj2 a) (* 0.01 ee w2))))
                            tmp)))
          (setq chout (append chout arcpts (cdr (reverse tmp))))
          ;; --- cadeia interna: eixo1 -> juncao -> eixo2
          (setq chin '())
          (foreach a (int:rng de1 dj1 step)
            (setq chin (cons (append (int:axis-xy e1 a)
                                     (list (int:z zj g1 dj1 a)))
                             chin)))
          (setq chin (reverse chin))
          (setq tmp '())
          (foreach a (int:rng dj2 de2 step)
            (setq tmp (cons (append (int:axis-xy e2 a)
                                    (list (int:z zj g2 dj2 a)))
                            tmp)))
          (setq chin (append chin (cdr (reverse tmp))))
          ;; --- desenho
          (int:3dpoly chout "INT-BORDA" 1)
          (if fok
            (setq areatot (+ areatot (int:zip chin chout "INT-FACES"))))
          (setq aa (+ ang1 (* delta 0.5)))
          (int:text (list (+ cx (* (+ rr 1.0) (cos aa)))
                          (+ cy (* (+ rr 1.0) (sin aa))))
                    (strcat "R=" (rtos rr 2 2)) th "INT-TEXTO" 2)
          (setq nquad (1+ nquad))
          ;; registra extremidades p/ fechamento
          (setq key (strcat "1" (if (> q1 0.0) "+" "-")))
          (if (not (member key capkeys))
            (setq capkeys (cons key capkeys)
                  capdata (cons (list e1 w1 g1 dj1 de1) capdata)))
          (setq key (strcat "2" (if (> q2 0.0) "+" "-")))
          (if (not (member key capkeys))
            (setq capkeys (cons key capkeys)
                  capdata (cons (list e2 w2 g2 dj2 de2) capdata))))
        (princ (strcat "\n  - Quadrante (" (rtos s1 2 0) "," (rtos s2 2 0)
                       ") ignorado: perna ausente ou extensao insuficiente.")))))

  (if (= nquad 0)
    (progn (princ "\nNenhum quadrante valido gerado. Verifique raio/extensoes.")
           (*error* nil) (exit)))

  ;; ---------- 6. eixos 3D com greide
  (foreach via (list (list e1 dj1 g1 l1 len1) (list e2 dj2 g2 l2 len2))
    (setq ent  (car via)  djx (cadr via) g (caddr via)
          lx   (cadddr via) lent (last via)
          a    (max 0.0 (- djx lx))
          b    (min lent (+ djx lx))
          tmp  '())
    (foreach k (int:rng a b step)
      (setq tmp (cons (append (int:axis-xy ent k) (list (int:z zj g djx k))) tmp)))
    (int:3dpoly (reverse tmp) "INT-EIXO3D" 3))

  ;; ---------- 7. fechamentos de extremidade (breaklines transversais)
  (foreach via capdata
    (setq ent (car via) lx (cadr via) g (caddr via)
          djx (cadddr via) b (last via))
    (int:3dpoly
      (list (append (int:off-xy ent b  1.0 lx)
                    (list (- (int:z zj g djx b) (* 0.01 ee lx))))
            (append (int:axis-xy ent b) (list (int:z zj g djx b)))
            (append (int:off-xy ent b -1.0 lx)
                    (list (- (int:z zj g djx b) (* 0.01 ee lx)))))
      "INT-BORDA" 1))

  ;; ---------- 8. marcador e rotulo da juncao
  (entmake (list '(0 . "CIRCLE") (cons 8 "INT-TEXTO") (cons 62 2)
                 (cons 10 (list px py 0.0))
                 (cons 40 (* 0.3 (min w1 w2)))))
  (int:mtext (list (+ px (* 2.0 (min w1 w2))) (+ py (* 2.0 (min w1 w2))))
             (strcat "JUNCAO"
                     "\\PCota projeto: " (rtos zj 2 3) " m"
                     "\\PVia 1: greide " (rtos g1 2 2) "%"
                     "\\PVia 2: greide " (rtos g2 2 2) "%")
             th "INT-TEXTO" 2)

  ;; ---------- 9. relatorio
  (setq sta01 (int:start-station e1) sta02 (int:start-station e2))
  (if (null sta01) (setq sta01 0.0))
  (if (null sta02) (setq sta02 0.0))
  (princ "\n================ RELATORIO DA INTERSECAO =================")
  (princ (strcat "\n Juncao..........: X=" (rtos px 2 3) "  Y=" (rtos py 2 3)))
  (princ (strcat "\n Cota de projeto.: " (rtos zj 2 3) " m"))
  (princ (strcat "\n Estaca via 1....: " (int:fmt (+ sta01 dj1))))
  (princ (strcat "\n Estaca via 2....: " (int:fmt (+ sta02 dj2))))
  (princ (strcat "\n Quadrantes......: " (itoa nquad) " de 4"))
  (princ (strcat "\n Raio concord....: " (rtos rr 2 2) " m"))
  (if fok
    (princ (strcat "\n Area pavimentada: " (rtos areatot 2 2) " m2 (aprox.)")))
  (princ "\n----------------------------------------------------------")
  (princ "\n PROXIMOS PASSOS NO CIVIL 3D:")
  (princ "\n 1. Create Surface > Definition > Drawing Objects >")
  (princ "\n    tipo '3D Faces' > selecione a camada INT-FACES")
  (princ "\n 2. (Opcional) adicione INT-BORDA e INT-EIXO3D como breaklines")
  (princ "\n 3. Para o corredor, rode o Dynamo: IntersecaoCorredor.py")
  (princ "\n==========================================================")

  (setvar "CMDECHO" oldecho)
  (command "_.UNDO" "_End")
  (princ))

(princ "\nINTERSECAO.LSP carregado. Digite INTERSECAO para comecar, INTERSECAOAJUDA para dicas.")
(princ)
