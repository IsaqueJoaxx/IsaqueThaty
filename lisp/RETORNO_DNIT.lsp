;;; ============================================================================
;;;  RETORNO_DNIT.lsp  -  Geracao de geometria de RETORNO em rodovias
;;;  Base normativa: DNIT IPR-718 (Manual de Projeto de Intersecoes), item 8.9
;;;    - Tabela 65: larguras minimas do canteiro por veiculo de projeto
;;;    - Tabela 66: curva composta de 3 centros (bullet-nose):
;;;                 W = 9 ...... R = 15 - 0,2W - 15
;;;                 9 < W <= 18  R = 23 - 0,2W - 23
;;;                 18 < W <= 24 R = 37 - 0,2W - 37
;;;  Comando: RETDNIT
;;;  Autor: gerado p/ fluxo Civil 3D (AutoLISP puro - roda em AutoCAD/Civil 3D)
;;;
;;;  O QUE FAZ:
;;;   1) Voce seleciona o EIXO (polilinha/linha) -> direcao vem da tangente.
;;;   2) Clica o ponto de inicio do retorno sobre o eixo.
;;;   3) Escolhe o tipo:
;;;        [B] Bullet-nose (abertura de canteiro em "U", IPR-718 Tab.66)
;;;        [L] Laco / teardrop (meia-alca com raio livre)
;;;   4) Informa parametros (W, R, largura, velocidade...) -> desenha tudo.
;;;
;;;  OBS: comprimentos de desacel/acel = Tabelas 48 e 49 do IPR-718 (DNIT),
;;;       oficialmente adotadas pelo DER-MG (Manual Vol.VI, item 4.4).
;;;       Raio do bullet-nose = norma (Tab.66). Laco/teardrop = esquematico.
;;; ============================================================================

(vl-load-com)

;;; --------------------------- utilitarios de vetor ---------------------------
(defun v- (a b) (list (- (car a) (car b)) (- (cadr a) (cadr b))))
(defun v+ (a b) (list (+ (car a) (car b)) (+ (cadr a) (cadr b))))
(defun v* (a s) (list (* (car a) s) (* (cadr a) s)))
(defun uv (v / m)
  (setq m (sqrt (+ (* (car v) (car v)) (* (cadr v) (cadr v)))))
  (if (< m 1e-9) (list 0.0 0.0) (list (/ (car v) m) (/ (cadr v) m))))

(defun n2pi (a) (rem (+ (rem a (* 2 pi)) (* 2 pi)) (* 2 pi)))

;; a esta dentro da varredura anti-horaria de s ate e ?
(defun inccw (s e a)
  (setq s (n2pi s) e (n2pi e) a (n2pi a))
  (if (<= s e) (and (>= a s) (<= a e)) (or (>= a s) (<= a e))))

;;; ------------------------- criacao de camadas -------------------------------
(defun mklayer (name col)
  (if (not (tblsearch "LAYER" name))
    (entmake
      (list '(0 . "LAYER") '(100 . "AcDbSymbolTableRecord")
            '(100 . "AcDbLayerTableRecord") (cons 2 name)
            (cons 70 0) (cons 62 col) '(6 . "Continuous")))))

;;; ------------------------- transformacao local->mundo -----------------------
;; usa variaveis globais *pb* (ponto base), *ang* (direcao rad), *sgn* (+1 esq / -1 dir)
(defun L2W (p / x y)
  (setq x (car p) y (* (cadr p) *sgn*))
  (list (+ (car *pb*)  (- (* x (cos *ang*)) (* y (sin *ang*))))
        (+ (cadr *pb*) (+ (* x (sin *ang*)) (* y (cos *ang*))))
        0.0))

;;; ------------------------- arco por 3 pontos (mundo) ------------------------
(defun arc3p (p1 p2 p3 lay / ax ay bx by cx cy d ux uy c rad a1 a2 a3 sa ea)
  (setq ax (car p1) ay (cadr p1)
        bx (car p2) by (cadr p2)
        cx (car p3) cy (cadr p3)
        d  (* 2.0 (+ (* ax (- by cy)) (* bx (- cy ay)) (* cx (- ay by)))))
  (if (< (abs d) 1e-9)
    (entmake (list '(0 . "LINE") (cons 8 lay) (cons 10 p1) (cons 11 p3)))
    (progn
      (setq ux (/ (+ (* (+ (* ax ax) (* ay ay)) (- by cy))
                     (* (+ (* bx bx) (* by by)) (- cy ay))
                     (* (+ (* cx cx) (* cy cy)) (- ay by))) d)
            uy (/ (+ (* (+ (* ax ax) (* ay ay)) (- cx bx))
                     (* (+ (* bx bx) (* by by)) (- ax cx))
                     (* (+ (* cx cx) (* cy cy)) (- bx ax))) d)
            c   (list ux uy)
            rad (distance c p1)
            a1  (angle c p1) a2 (angle c p2) a3 (angle c p3))
      (if (inccw a1 a3 a2) (setq sa a1 ea a3) (setq sa a3 ea a1))
      (entmake (list '(0 . "ARC") (cons 8 lay)
                     (cons 10 (list ux uy 0.0)) (cons 40 rad)
                     (cons 50 sa) (cons 51 ea))))))

;;; ------------------------- helpers de desenho -------------------------------
(defun dline (pa pb lay)
  (entmake (list '(0 . "LINE") (cons 8 lay) (cons 10 (L2W pa)) (cons 11 (L2W pb)))))

(defun arcmid (oc pa pb r) ; ponto medio do arco menor (bissetriz)
  (v+ oc (v* (uv (v+ (uv (v- pa oc)) (uv (v- pb oc)))) r)))

(defun darc (oc pa pb r lay) ; arco por centro+2 extremos (arco menor)
  (arc3p (L2W pa) (L2W (arcmid oc pa pb r)) (L2W pb) lay))

(defun darc2 (pa pm pb lay) ; arco por 3 pontos locais explicitos (p/ 180 graus)
  (arc3p (L2W pa) (L2W pm) (L2W pb) lay))

(defun dcircle (p r lay)
  (entmake (list '(0 . "CIRCLE") (cons 8 lay) (cons 10 (L2W p)) (cons 40 r))))

(defun dtext (p h str lay)
  (entmake (list '(0 . "TEXT") (cons 8 lay) (cons 10 (L2W p))
                 (cons 40 h) (cons 1 str) (cons 50 *ang*))))

(defun dmtext (p h wid str lay)
  (entmake (list '(0 . "MTEXT") '(100 . "AcDbEntity") (cons 8 lay)
                 '(100 . "AcDbMText") (cons 10 (L2W p)) (cons 40 h)
                 (cons 41 wid) (cons 71 1) (cons 1 str))))

;;; ------------------------- prompts com default -----------------------------
(defun getr (msg def / v)
  (setq v (getreal (strcat msg " <" (rtos def 2 2) ">: ")))
  (if v v def))

(defun gets (msg def / v)
  (setq v (getstring T (strcat msg " <" def ">: ")))
  (if (= v "") def v))

;;; ------------------------- Tabela 66 (raio externo) -------------------------
(defun tab66R (w)
  (cond ((<= w 9.0) 15.0) ((<= w 18.0) 23.0) ((<= w 24.0) 37.0) (t 37.0)))

;;; --------- Tabelas 48/49 DNIT IPR-718 (faixas de mudanca de velocidade) ------
;; Adotadas pelo DER/MG (Manual Vol.VI, item 4.4 remete ao Manual DNIT/2005).
;; Formato: (Vdiretriz taper ((Vsaida . L) ...))  - L = compr. INCLUSIVE taper (m)
;; DESACELERACAO (rampas ate 2%)
(setq *T48D*
  (list
    (list 40  40 (list (cons 0 60)(cons 20 50)(cons 30 40)))
    (list 50  45 (list (cons 0 75)(cons 20 70)(cons 30 60)(cons 40 45)))
    (list 60  55 (list (cons 0 95)(cons 20 90)(cons 30 80)(cons 40 65)(cons 50 55)))
    (list 70  60 (list (cons 0 110)(cons 20 105)(cons 30 95)(cons 40 85)(cons 50 70)(cons 60 60)))
    (list 80  70 (list (cons 0 130)(cons 20 125)(cons 30 115)(cons 40 100)(cons 50 90)(cons 60 80)(cons 70 70)))
    (list 90  80 (list (cons 0 145)(cons 20 140)(cons 30 135)(cons 40 120)(cons 50 110)(cons 60 100)(cons 70 90)(cons 80 80)))
    (list 100 85 (list (cons 0 170)(cons 20 165)(cons 30 155)(cons 40 145)(cons 50 135)(cons 60 120)(cons 70 100)(cons 80 85)))
    (list 110 90 (list (cons 0 180)(cons 20 180)(cons 30 170)(cons 40 160)(cons 50 150)(cons 60 140)(cons 70 120)(cons 80 105)))
    (list 120 100 (list (cons 0 200)(cons 20 195)(cons 30 185)(cons 40 175)(cons 50 170)(cons 60 155)(cons 70 140)(cons 80 120)))))
;; ACELERACAO (rampas ate 2%)
(setq *T48A*
  (list
    (list 40  40 (list (cons 0 60)(cons 20 50)(cons 30 40)))
    (list 50  45 (list (cons 0 90)(cons 20 70)(cons 30 60)(cons 40 45)))
    (list 60  55 (list (cons 0 130)(cons 20 110)(cons 30 100)(cons 40 70)(cons 50 55)))
    (list 70  60 (list (cons 0 180)(cons 20 150)(cons 30 140)(cons 40 120)(cons 50 90)(cons 60 60)))
    (list 80  70 (list (cons 0 230)(cons 20 210)(cons 30 200)(cons 40 180)(cons 50 140)(cons 60 100)(cons 70 70)))
    (list 90  80 (list (cons 0 280)(cons 20 250)(cons 30 240)(cons 40 220)(cons 50 190)(cons 60 140)(cons 70 100)(cons 80 80)))
    (list 100 85 (list (cons 0 340)(cons 20 310)(cons 30 290)(cons 40 280)(cons 50 240)(cons 60 200)(cons 70 170)(cons 80 110)))
    (list 110 90 (list (cons 0 390)(cons 20 360)(cons 30 350)(cons 40 320)(cons 50 290)(cons 60 250)(cons 70 200)(cons 80 160)))
    (list 120 100 (list (cons 0 430)(cons 20 400)(cons 30 390)(cons 40 360)(cons 50 330)(cons 60 290)(cons 70 240)(cons 80 200)))))

(defun snapkey (v lst / best bd)
  (setq bd 1e9)
  (foreach k lst (if (< (abs (- k v)) bd) (setq bd (abs (- k v)) best k)))
  best)

;; consulta Tabela 48 -> (list L taper)  (L inclusive taper)
(defun t48 (tbl vd vs / row cols vk)
  (setq vk   (snapkey vd (mapcar 'car tbl))
        row  (assoc vk tbl)
        cols (caddr row)
        vs   (snapkey vs (mapcar 'car cols)))
  (list (cdr (assoc vs cols)) (cadr row)))

;; Tabela 49 - fator de greide p/ DESACELERACAO (g em %, + sobe / - desce)
(defun gfdec (g)
  (cond ((>= g 5.0) 0.80) ((>= g 3.0) 0.90)
        ((<= g -5.0) 1.35) ((<= g -3.0) 1.20) (t 1.0)))

;;; ===========================================================================
;;;                          COMANDO PRINCIPAL
;;; ===========================================================================
(defun c:RETDNIT (/ e est pk par der tipo lado
                    w r rn fa vd vs gr ldec ltap lacc lp lpa lback tip xn
                    dd aa o1 o2 t1 t2 p1 p2 w2 rl larg redge txt info)
  ;; tratador de erro (silencia ESC/cancelar)
  (setq *olderr* *error*)
  (defun *error* (m)
    (setq *error* *olderr*)
    (if (and m (not (wcmatch (strcase m) "*QUIT*,*EXIT*,*ABORT*,*CANCEL*")))
      (princ (strcat "\n** " m)))
    (princ))

  ;; memoria de sessao
  (or *rw*   (setq *rw*   12.0))
  (or *rfa*  (setq *rfa*  3.60))
  (or *rvd*  (setq *rvd*  80.0))
  (or *rraio* (setq *rraio* 30.0))
  (or *rlarg* (setq *rlarg* 7.00))

  ;; camadas
  (mklayer "RET-EIXO"  8)
  (mklayer "RET-PAV"   5)   ; azul   - bordas de pavimento / arcos
  (mklayer "RET-BORDA" 3)   ; verde  - bordas internas / referencia
  (mklayer "RET-AUX"   30)  ; laranja- faixa auxiliar (desaceleracao)
  (mklayer "RET-TEXTO" 2)   ; amarelo- textos/rotulos

  ;; 1) eixo
  (setq e (car (entsel "\nSelecione o EIXO (polilinha/linha): ")))
  (if (null e) (progn (princ "\nNada selecionado.") (exit)))
  (if (vl-catch-all-error-p
        (vl-catch-all-apply 'vlax-curve-getStartPoint (list e)))
    (progn (princ "\nObjeto invalido (nao e uma curva/eixo).") (exit)))

  ;; 2) estaca (rotulo)
  (setq est (gets "\nEstaca inicial (rotulo)" "0+000.00"))

  ;; 3) ponto de inicio sobre o eixo
  (setq pk  (getpoint "\nClique o ponto de INICIO do retorno sobre o eixo: "))
  (if (null pk) (progn (princ "\nPonto nao informado.") (exit)))
  (setq *pb* (vlax-curve-getClosestPointTo e pk)
        par  (vlax-curve-getParamAtPoint e *pb*)
        der  (vlax-curve-getFirstDeriv e par)
        *ang* (atan (cadr der) (car der)))

  ;; 4) lado
  (initget "Esquerda Direita")
  (setq lado (cond ((getkword "\nLado do retorno [Esquerda/Direita] <Esquerda>: ")) (t "Esquerda")))
  (setq *sgn* (if (= lado "Direita") -1.0 1.0))

  ;; 5) tipo
  (initget "Bullet Laco")
  (setq tipo (cond ((getkword "\nTipo [Bullet/Laco] <Bullet>: ")) (t "Bullet")))

  ;; =========================================================================
  ;;  TIPO BULLET-NOSE  (abertura de canteiro em "U" - IPR-718 Tab.66)
  ;; =========================================================================
  (if (= tipo "Bullet")
    (progn
      (setq w  (getr "\nLargura do canteiro central W (m)" *rw*)   *rw* w)
      (setq rn (tab66R w))
      (setq r  (getr (strcat "\nRaio externo R (m) [Tab.66 sugere " (rtos rn 2 2) "]") rn))
      (setq fa (getr "\nLargura da faixa auxiliar (m)" *rfa*)      *rfa* fa)
      (setq vd (getr "\nVelocidade diretriz (km/h)" *rvd*)         *rvd* vd)
      (setq vs (getr "\nVel. de seguranca da curva de saida do retorno (km/h)" 20.0))
      (setq gr (getr "\nGreide na aproximacao (%, + sobe / - desce)" 0.0))

      ;; comprimentos NORMATIVOS - Tabela 48 (DNIT IPR-718) + fator greide Tab.49
      (setq dd   (t48 *T48D* vd vs)
            ldec (* (car dd) (gfdec gr))         ; desacel. inclusive taper (Tab.48x49)
            ltap (cadr dd)                       ; taper (Tab.48)
            aa   (t48 *T48A* vd vs)
            lacc (car aa)                        ; acel. inclusive taper (Tab.48)
            lp   (max 0.0 (- ldec ltap))
            lpa  (max 0.0 (- lacc ltap)))

      (setq w2 (/ w 2.0)
            rl (* 0.2 w))                       ; raio do nariz = 0,2W
      ;; geometria bullet-nose (coord. local: x=frente, y=esquerda)
      (setq o1  (list 0.0 (- w2 r))
            t1  (list 0.0 w2)
            xn  (- (* (- r rl) (- r rl)) (* (- r w2) (- r w2))))
      (if (< xn 0.0)
        (progn (princ "\nGeometria impossivel (R muito pequeno p/ este W).") (exit)))
      (setq xn (sqrt xn)
            o2  (list 0.0 (- r w2))
            t2  (list 0.0 (- w2))
            p1  (v+ o1 (v* (uv (v- (list xn 0.0) o1)) r))
            p2  (list (car p1) (- (cadr p1)))
            tip (list (+ xn rl) 0.0))
      ;; arcos do bullet-nose
      (darc  o1 t1 p1 r  "RET-PAV")           ; arco externo superior
      (darc2 p1 tip p2 "RET-PAV")             ; nariz (passa pela ponta)
      (darc  o2 p2 t2 r  "RET-PAV")           ; arco externo inferior
      (dcircle tip 0.5 "RET-EIXO")            ; marcador da ponta do nariz

      ;; bordas internas de referencia + eixo (ao longo das faixas auxiliares)
      (dline (list (- 0.0 ldec) w2)      t1 "RET-BORDA")
      (dline (list (- 0.0 lacc) (- w2))  t2 "RET-BORDA")
      (dline (list (- 0.0 (max ldec lacc)) 0.0) (list (+ xn rl) 0.0) "RET-EIXO")

      ;; FAIXA DE DESACELERACAO (Tipo A) - lado de aproximacao (+W2)
      (dline (list 0.0 (- w2 fa)) (list (- 0.0 lp) (- w2 fa)) "RET-AUX")
      (dline (list (- 0.0 lp) (- w2 fa)) (list (- 0.0 ldec) w2) "RET-AUX")
      ;; FAIXA DE ACELERACAO (Tipo A) - lado de saida (-W2)
      (dline (list 0.0 (+ (- 0.0 w2) fa)) (list (- 0.0 lpa) (+ (- 0.0 w2) fa)) "RET-AUX")
      (dline (list (- 0.0 lpa) (+ (- 0.0 w2) fa)) (list (- 0.0 lacc) (- 0.0 w2)) "RET-AUX")

      ;; rotulos
      (dtext (list (- 0.0 (max ldec lacc)) (+ w2 2.0)) 2.0
             (strcat "EST " est) "RET-TEXTO")
      (setq info
        (strcat "RETORNO EM \"U\" (bullet-nose)\\P"
                "DNIT IPR-718 (Tab.66) - adotado p/ DER-MG (Vol.VI item 4.4)\\P"
                "Estaca: " est "\\P"
                "Canteiro W = " (rtos w 2 2) " m\\P"
                "Raio externo R = " (rtos r 2 2) " m | nariz r=0,2W = " (rtos rl 2 2) " m\\P"
                "Faixa auxiliar = " (rtos fa 2 2) " m\\P"
                "V diretriz=" (rtos vd 2 0) " | V saida=" (rtos vs 2 0)
                " km/h | greide=" (rtos gr 2 1) "%\\P"
                "L desacel. (Tab.48/49) = " (rtos ldec 2 1) " m  (taper " (rtos ltap 2 1) " m)\\P"
                "L acel. (Tab.48) = " (rtos lacc 2 1) " m"))
      (dmtext (list (+ xn rl 4.0) (- 0.0 4.0)) 1.6 50.0 info "RET-TEXTO")

      (princ (strcat "\n>> Bullet-nose criado. W=" (rtos w 2 2)
                     "  R=" (rtos r 2 2) "  r=" (rtos rl 2 2)
                     "  (nariz a " (rtos (+ xn rl) 2 2) " m do inicio da abertura)"))
    )
    ;; =======================================================================
    ;;  TIPO LACO / TEARDROP  (meia-alca com raio livre)
    ;; =======================================================================
    (progn
      (setq r    (getr "\nRaio do retorno R (eixo do laco) (m)" *rraio*) *rraio* r)
      (setq larg (getr "\nLargura da pista do retorno (m)" *rlarg*)      *rlarg* larg)
      (setq vd   (getr "\nVelocidade diretriz da rodovia (km/h)" *rvd*)  *rvd* vd)

      ;; coord. local: entrada Pe=(0,0) rumo +x, giro p/ +y (lado tratado por *sgn*)
      ;; centro do laco C=(0,R); saida Px=(0,2R) rumo -x
      (setq redge (/ larg 2.0))
      (setq txt (list 0.0 r)) ; centro C
      ;; eixo do laco (arco de 180 graus) - mid explicito = (R,R)
      (darc2 (list 0.0 0.0) (list r r) (list 0.0 (* 2.0 r)) "RET-EIXO")
      ;; borda interna (R - larg/2)
      (arc3p (L2W (v+ txt (v* (uv (v- (list 0.0 0.0) txt)) (- r redge))))
             (L2W (v+ txt (v* (uv (v- (list r r)     txt)) (- r redge))))
             (L2W (v+ txt (v* (uv (v- (list 0.0 (* 2 r)) txt)) (- r redge))))
             "RET-PAV")
      ;; borda externa (R + larg/2)
      (arc3p (L2W (v+ txt (v* (uv (v- (list 0.0 0.0) txt)) (+ r redge))))
             (L2W (v+ txt (v* (uv (v- (list r r)     txt)) (+ r redge))))
             (L2W (v+ txt (v* (uv (v- (list 0.0 (* 2 r)) txt)) (+ r redge))))
             "RET-PAV")
      ;; tocos de entrada/saida do eixo (15 m)
      (dline (list -15.0 0.0)        (list 0.0 0.0)        "RET-EIXO")
      (dline (list 0.0 (* 2.0 r))    (list -15.0 (* 2.0 r)) "RET-EIXO")

      ;; rotulos
      (dtext (list -15.0 2.0) 2.0 (strcat "EST " est) "RET-TEXTO")
      (setq info
        (strcat "RETORNO EM LACO (teardrop / meia-alca)\\P"
                "Estaca inicial: " est "\\P"
                "Raio do eixo R = " (rtos r 2 2) " m\\P"
                "Largura da pista = " (rtos larg 2 2) " m\\P"
                "Bordas: " (rtos (- r redge) 2 2) " / " (rtos (+ r redge) 2 2) " m\\P"
                "Vel. diretriz = " (rtos vd 2 0) " km/h\\P"
                "(geometria esquematica - ajustar aos veic. de projeto)"))
      (dmtext (list (+ r redge 3.0) r) 1.6 45.0 info "RET-TEXTO")

      (princ (strcat "\n>> Laco criado. R=" (rtos r 2 2)
                     "  largura=" (rtos larg 2 2) " m"))
    )
  )
  (setq *error* *olderr*)
  (princ)
)

(princ "\nRETORNO_DNIT carregado. Digite  RETDNIT  para executar.")
(princ)
