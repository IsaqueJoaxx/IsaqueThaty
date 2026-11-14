;=====================================================================
; GotaTape.lsp - Gerador de geometria de acessos e intersecoes
;              (gota de canalizacao + taper / faixa de mud. velocidade)
;
; Bases normativas:
;   - Manual de Projeto de Intersecoes - DNIT IPR 718 (Apendice A)
;   - Manual de Projeto de Acessos de Areas Lindeiras - DNIT IPR 728
;     (Detalhamento 1 - gota com R=15; tapers niveis 1/2 e 3/4)
;
; Depende de: GT_DB.lsp (carregar antes ou deixar na mesma pasta)
;
; Comandos:
;   GOTA   - cria a gota no eixo do acesso/secundaria + taper na rodovia
;   TAPER  - cria taper/faixa de mudanca de velocidade isolada
;
; Compatibilidade: AutoCAD, BricsCAD, ZWCAD, GstarCAD
; (nao usa funcoes vlax/vla - apenas entget/entmake nativos)
;=====================================================================

;---------------------------------------------------------------------
; UTILITARIOS VETORIAIS (2D)
;---------------------------------------------------------------------
(defun gt:2d    (p)     (list (car p) (cadr p)))
(defun gt:dot   (a b)   (+ (* (car a) (car b)) (* (cadr a) (cadr b))))
(defun gt:cross (a b)   (- (* (car a) (cadr b)) (* (cadr a) (car b))))
(defun gt:v+    (a b)   (list (+ (car a) (car b)) (+ (cadr a) (cadr b))))
(defun gt:v-    (a b)   (list (- (car a) (car b)) (- (cadr a) (cadr b))))
(defun gt:v*s   (a s)   (list (* (car a) s) (* (cadr a) s)))
(defun gt:norm  (a)     (sqrt (gt:dot a a)))
(defun gt:unit  (a / n) (setq n (gt:norm a))
                        (if (> n 1e-12) (gt:v*s a (/ 1.0 n)) '(1.0 0.0)))
(defun gt:perp  (a)     (list (- (cadr a)) (car a)))
(defun gt:rot   (a ang / c s)
  (setq c (cos ang) s (sin ang))
  (list (- (* (car a) c) (* (cadr a) s))
        (+ (* (car a) s) (* (cadr a) c)))
)
(defun gt:dist  (a b)   (distance a b))

; Intersecao de duas retas infinitas (p1,u1) x (p2,u2). nil se paralelas.
(defun gt:inters (p1 u1 p2 u2 / d t)
  (setq d (gt:cross u1 u2))
  (if (< (abs d) 1e-12)
    nil
    (progn
      (setq t (/ (gt:cross (gt:v- p2 p1) u2) d))
      (gt:v+ p1 (gt:v*s u1 t))
    )
  )
)

; Pe da perpendicular de c na reta (p,u)
(defun gt:foot (c p u) (gt:v+ p (gt:v*s u (gt:dot (gt:v- c p) u))))

; Tangentes de ponto e ao circulo (c,r) -> (T1 T2).
; Se e estiver dentro do circulo, devolve o ponto do circulo na
; direcao de e (tangente degenerada, perpendicular ao raio).
(defun gt:tang-pt-circ (e c r / d a0 da)
  (setq d (gt:dist e c))
  (if (<= d r)
    (list (polar c (angle c e) r) (polar c (angle c e) r))
    (progn
      (setq a0 (angle c e)
            da (atan (sqrt (- (* d d) (* r r))) r))
      (list (polar c (+ a0 da) r) (polar c (- a0 da) r))
    )
  )
)

; Intersecoes de dois circulos (c1,r1) x (c2,r2) -> (P1 P2) ou nil
(defun gt:circ-circ (c1 r1 c2 r2 / d a ex ey h p2 rx)
  (setq d (gt:dist c1 c2))
  (if (or (> d (+ r1 r2)) (< d (abs (- r1 r2))) (< d 1e-12))
    nil
    (progn
      (setq a (/ (+ (* d d) (* r1 r1) (- (* r2 r2))) (* 2.0 d))
            h (sqrt (max 0.0 (- (* r1 r1) (* a a))))
            ex (gt:unit (gt:v- c2 c1))
            ey (gt:perp ex)
            p2 (gt:v+ c1 (gt:v*s ex a))
            rx (gt:v*s ey h))
      (list (gt:v+ p2 rx) (gt:v- p2 rx))
    )
  )
)

;---------------------------------------------------------------------
; LEITURA DO EIXO (sem vlax): devolve (ponto direcao) do SEGMENTO da
; entidade mais proximo do ponto clicado. Aceita LINE, LWPOLYLINE,
; POLYLINE e ARC.
;---------------------------------------------------------------------
(defun gt:vertices (ent / el typ vlst e)
  (setq el (entget ent)
        typ (cdr (assoc 0 el)))
  (cond
    ((eq typ "LINE")
      (list (gt:2d (cdr (assoc 10 el))) (gt:2d (cdr (assoc 11 el)))))
    ((eq typ "LWPOLYLINE")
      (foreach x el
        (if (= (car x) 10) (setq vlst (cons (gt:2d (cdr x)) vlst))))
      (reverse vlst))
    ((eq typ "POLYLINE")
      (setq e (entnext ent))
      (while (and e (eq "VERTEX" (cdr (assoc 0 (entget e)))))
        (setq vlst (cons (gt:2d (cdr (assoc 10 (entget e)))) vlst)
              e (entnext e)))
      (reverse vlst))
    ((eq typ "ARC")
      (list (polar (cdr (assoc 10 el)) (cdr (assoc 50 el)) (cdr (assoc 40 el)))
            (polar (cdr (assoc 10 el)) (cdr (assoc 51 el)) (cdr (assoc 40 el)))))
    (t nil)
  )
)

; Distancia de p ate o segmento (a,b) e ponto projetado no segmento
(defun gt:proj-seg (p a b / ab t q)
  (setq ab (gt:v- b a)
        t  (/ (gt:dot (gt:v- p a) ab) (max 1e-12 (gt:dot ab ab)))
        t  (max 0.0 (min 1.0 t))
        q  (gt:v+ a (gt:v*s ab t)))
  (list (gt:dist p q) q)
)

; Ponto e direcao do eixo no local clicado
(defun gt:eixo-ent (ent pref / vs pref2 melhor i a b r)
  (setq vs     (gt:vertices ent)
        pref2  (gt:2d pref)
        melhor nil
        i      0)
  (if (and vs (> (length vs) 1))
    (progn
      (while (< i (1- (length vs)))
        (setq a (nth i vs)
              b (nth (1+ i) vs)
              r (gt:proj-seg pref2 a b))
        (if (or (null melhor) (< (car r) (car melhor)))
          (setq melhor (list (car r) (cadr r) (gt:unit (gt:v- b a)))))
        (setq i (1+ i))
      )
      (list (cadr melhor) (caddr melhor))  ; (ponto-projetado direcao)
    )
  )
)

;---------------------------------------------------------------------
; DESENHO (entmake puro)
;---------------------------------------------------------------------
(defun gt:layer (nome cor)
  (if (null (tblsearch "LAYER" nome))
    (entmake (list '(0 . "LAYER") '(100 . "AcDbSymbolTableRecord")
                   '(100 . "AcDbLayerTableRecord") (cons 2 nome)
                   '(70 . 0) (cons 62 cor)))
  )
)

(defun gt:layers ()
  (gt:layer "GT-EIXO"    8)
  (gt:layer "GT-GOTA"    3)
  (gt:layer "GT-GIRO"    4)
  (gt:layer "GT-TAPER"   2)
  (gt:layer "GT-FAIXA"   1)
  (gt:layer "GT-PINTURA" 7)
)

(defun gt:line (p1 p2 lay)
  (entmake (list '(0 . "LINE") (cons 8 lay)
                 (cons 10 (gt:2d p1)) (cons 11 (gt:2d p2))))
)

(defun gt:arc3 (c r a1 a2 lay)
  (entmake (list '(0 . "ARC") (cons 8 lay) (cons 10 (gt:2d c))
                 (cons 40 r) (cons 50 a1) (cons 51 a2)))
)

; Bulge de arco de centro c, de p1 ate p2 (sinal pelo sentido)
(defun gt:bulge (c p1 p2 / v1 v2 ang q)
  (setq v1 (gt:v- p1 c) v2 (gt:v- p2 c)
        ang (atan (gt:cross v1 v2) (gt:dot v1 v2))
        q   (/ ang 4.0))
  (/ (sin q) (cos q))
)

; Polyline fechada: lista de (ponto . bulge-para-o-proximo)
(defun gt:plinha-arcos (segs lay / lst)
  (setq lst (list '(0 . "LWPOLYLINE") '(100 . "AcDbEntity")
                  '(100 . "AcDbPolyline") (cons 90 (length segs))
                  '(70 . 1) (cons 8 lay)))
  (foreach s segs
    (setq lst (append lst (list (cons 10 (gt:2d (car s)))
                                (cons 42 (cdr s))))))
  (entmake lst)
)

; Polyline aberta simples
(defun gt:plinha (pts lay / lst)
  (setq lst (list '(0 . "LWPOLYLINE") '(100 . "AcDbEntity")
                  '(100 . "AcDbPolyline") (cons 90 (length pts))
                  '(70 . 0) (cons 8 lay)))
  (foreach p pts (setq lst (append lst (list (cons 10 (gt:2d p))))))
  (entmake lst)
)

;---------------------------------------------------------------------
; ENTRADAS
;---------------------------------------------------------------------
(defun gt:getreal-default (msg def / v)
  (initget 4)
  (setq v (getreal (strcat "\n" msg " <" (rtos def 2 2) ">: ")))
  (if v v def)
)

(defun gt:sel-eixo (msg / e ok)
  (setq e nil ok nil)
  (while (not ok)
    (setq e (entsel (strcat "\n" msg ": ")))
    (if e
      (if (member (cdr (assoc 0 (entget (car e))))
                  '("LINE" "LWPOLYLINE" "POLYLINE" "ARC"))
        (setq ok T)
        (princ "\nEntidade invalida. Use LINE, PLINE ou ARC.")
      )
    )
  )
  e
)

;---------------------------------------------------------------------
; Centro do arco R tangente a linha de referencia (lb,ub) - eixo da
; via principal (ou bordo da faixa de giro) - e a paralela (pp,ug).
; O centro fica do lado da gota em relacao a referencia e do lado do
; eixo da gota em relacao a paralela. pref = ponto no eixo da gota.
;---------------------------------------------------------------------
(defun gt:centro-arco-giro (lb ub pp ug r lado pref /
                            noff npar cands melhor c d1 d3 s1 s3 ss)
  (setq noff (gt:perp ub)
        npar (gt:perp ug)
        d1 (gt:dot (gt:v- pref lb) noff)
        d3 (gt:dot (gt:v- pref pp) npar)
        s1 (if (> d1 0) 1.0 -1.0)
        s3 (if (> d3 0) 1.0 -1.0)
        cands nil)
  (foreach ss '((1.0 . 1.0) (1.0 . -1.0))
    (setq c (gt:inters (gt:v+ lb (gt:v*s noff (* r s1 (car ss)))) ub
                       (gt:v+ pp (gt:v*s npar (* r s3 (cdr ss)))) ug))
    (if c (setq cands (cons c cands)))
  )
  (setq melhor nil)
  (foreach c cands
    (if (and (> (* (gt:dot (gt:v- c lb) noff) d1) 0.0)
             (> (* (gt:dot (gt:v- c pp) npar) d3) 0.0))
      (setq melhor c)))
  (if (null melhor)
    (foreach c cands
      (if (> (* (gt:dot (gt:v- c lb) noff) d1) 0.0)
        (if (or (null melhor)
                (< (gt:dist (gt:foot c lb ub) pref)
                   (gt:dist (gt:foot melhor lb ub) pref)))
          (setq melhor c)))))
  melhor
)

; Pontos de tangencia das retas que partem de p20 e tocam os arcos R
(defun gt:tangentes-gota (p20 cl cr r ug / t1 t2)
  (setq t1 (gt:tang-pt-circ p20 cl r)
        t2 (gt:tang-pt-circ p20 cr r))
  (if (or (null t1) (null t2))
    nil
    (list
      (if (> (gt:dot (gt:v- (car t1) p20) (gt:perp ug))
             (gt:dot (gt:v- (cadr t1) p20) (gt:perp ug)))
        (car t1) (cadr t1))
      (if (< (gt:dot (gt:v- (car t2) p20) (gt:perp ug))
             (gt:dot (gt:v- (cadr t2) p20) (gt:perp ug)))
        (car t2) (cadr t2))
    )
  )
)

;---------------------------------------------------------------------
; GOTA - Etapas I a IX do Detalhamento 1 (IPR 728) / Apendice A (IPR 718)
;   pb     = intersecao dos eixos
;   us     = unitario do eixo secundario (para fora da principal)
;   bordop/bordou = bordo da rodovia principal (ponto e direcao)
;   eixop/eixou   = eixo da rodovia principal (referencia de tangencia)
;   lado   = +1 esquerda / -1 direita (desvio do eixo da gota)
;   r      = raio dos giros (15 acesso / 12 intersecao / 8 se alfa<90)
; Retorna (r cinf p20) ou nil.
;---------------------------------------------------------------------
(defun gt:gota (pb us bordop bordou eixop eixou lado r grupo /
                ug p0 thg rp ml dts dtt parl parr cl cr tsx tlx
                cands csup tsup1 tsup2 x0 p20 tg tl tr
                v1 v2 beta bis dinf cinf ti1 ti2)
  (setq rp  (gt:par grupo 'raio-ponta)
        ml  (gt:par grupo 'meia-largura)
        dts (gt:par grupo 'dist-ponta-sup)
        dtt (gt:par grupo 'dist-tangentes)
        thg (* (/ pi 180.0) (gt:par grupo 'angulo-eixo)))
  ; II - ponto a 10 m do bordo no eixo secundario
  (setq p0 (gt:v+ pb (gt:v*s us dts)))
  ; III - eixo da gota a 5,5 graus do eixo secundario
  (setq ug (gt:rot us (* lado thg)))
  (gt:line p0 (gt:v+ p0 (gt:v*s ug (+ dtt 15.0))) "GT-EIXO")
  ; IV - paralelas ao eixo da gota, 1,50 m de cada lado
  (setq parl (gt:v+ p0 (gt:v*s (gt:perp ug) ml))
        parr (gt:v+ p0 (gt:v*s (gt:perp ug) (- ml))))
  ; V - arcos R tangentes ao eixo da principal e as paralelas
  (setq cl (gt:centro-arco-giro eixop eixou parl ug r  1 p0)
        cr (gt:centro-arco-giro eixop eixou parr ug r -1 p0))
  (if (or (null cl) (null cr))
    (progn (princ "\nERRO: nao foi possivel tangenciar os arcos R.") nil)
    (progn
      (setq tsx (list (gt:foot cl eixop eixou) (gt:foot cl parl ug))
            tlx (list (gt:foot cr eixop eixou) (gt:foot cr parr ug)))
      (gt:arc3 cl r (angle cl (car tsx)) (angle cl (cadr tsx)) "GT-GIRO")
      (gt:arc3 cr r (angle cr (car tlx)) (angle cr (cadr tlx)) "GT-GIRO")
      ; VI - ponta superior R0,75
      (setq cands (gt:circ-circ cl (+ r rp) cr (+ r rp)))
      (if (null cands)
        (progn (princ "\nERRO: ponta superior nao resolveu.") nil)
        (progn
          (setq csup (if (< (gt:dot (car cands) ug) (gt:dot (cadr cands) ug))
                       (car cands) (cadr cands))
                tsup1 (gt:v+ cl (gt:v*s (gt:unit (gt:v- csup cl)) r))
                tsup2 (gt:v+ cr (gt:v*s (gt:unit (gt:v- csup cr)) r)))
          ; VII - ponto a 20 m do bordo no prolongamento do eixo da gota
          (setq x0  (gt:inters p0 ug bordop bordou)
                p20 (gt:v+ x0 (gt:v*s ug dtt))
                tg  (gt:tangentes-gota p20 cl cr r ug))
          (if (null tg)
            (progn (princ "\nERRO: retas tangentes nao resolveram.") nil)
            (progn
              (setq tl (car tg) tr (cadr tg))
              ; IX - ponta inferior R0,75 entre as retas
              (setq v1 (gt:unit (gt:v- tl p20))
                    v2 (gt:unit (gt:v- tr p20))
                    beta (abs (atan (gt:cross v1 v2) (gt:dot v1 v2)))
                    bis (gt:unit (gt:v+ v1 v2))
                    dinf (/ rp (sin (/ beta 2.0)))
                    cinf (gt:v+ p20 (gt:v*s bis dinf))
                    ti1 (gt:foot cinf p20 v1)
                    ti2 (gt:foot cinf p20 v2))
              ; contorno fechado da gota
              (gt:plinha-arcos
                (list
                  (cons tsup1 (gt:bulge cl tsup1 tl))
                  (cons tl 0.0)
                  (cons ti1 (gt:bulge cinf ti1 ti2))
                  (cons ti2 0.0)
                  (cons tr (gt:bulge cr tr tsup2))
                  (cons tsup2 (gt:bulge csup tsup2 tsup1)))
                "GT-GOTA")
              ; X - linha-limite direita da pintura de aproximacao:
              ; tangente a ponta inferior a partir do eixo secundario
              (gt:pintura-aprox pb us cinf rp grupo)
              (list r cinf p20)
            )
          )
        )
      )
    )
  )
)

; X - pintura de aproximacao: reta do eixo secundario (a 40 m do eixo
; da principal) tangente a ponta inferior da gota
(defun gt:pintura-aprox (pb us cinf rp grupo / q tgs tg)
  (setq q (gt:v+ pb (gt:v*s us 40.0))
        tgs (gt:tang-pt-circ q cinf rp))
  (if tgs
    (progn
      ; escolhe o tangente mais proximo do eixo (menor deslocamento)
      (setq tg (if (< (gt:dist q (car tgs)) (gt:dist q (cadr tgs)))
                 (car tgs) (cadr tgs)))
      (gt:line q tg "GT-PINTURA")
    )
  )
)

;---------------------------------------------------------------------
; FAIXA DE MUDANCA DE VELOCIDADE / TAPER
; Desenha bordo externo: A (no bordo) -> alargamento no taper ->
; trecho paralelo (mesa). a = ponto inicial no bordo, ub = direcao,
; lado = +1/-1 (para onde a faixa abre), lt, lm, w em metros.
;---------------------------------------------------------------------
(defun gt:desenha-faixa (a ub lado lt lm w lay / c1 c2)
  (setq c1 (gt:v+ (gt:v+ a (gt:v*s ub lt))
                  (gt:v*s (gt:perp ub) (* lado w)))
        c2 (gt:v+ c1 (gt:v*s ub lm)))
  (if (> lm 0.0)
    (gt:plinha (list a c1 c2) lay)
    (gt:plinha (list a c1) lay)
  )
  (list a c1 c2)
)

;=====================================================================
; COMANDO PRINCIPAL: GOTA
;=====================================================================
(defun c:GOTA (/ *error* old-os tipo grupo vp vs lp ls lf alfa alfa-min
                 eixo-p eixo-s ex1 ex2 ip up pb us pref-s noff sinal
                 bordop bordou r lado-str lado res
                 resp nv dir-str dir vcurva fmv lt lm a1)

  (defun *error* (msg)
    (if old-os (setvar "OSMODE" old-os))
    (if (and msg (not (member msg '("Function cancelled" "quit / exit abort"))))
      (princ (strcat "\nErro: " msg)))
    (princ)
  )

  ; banco de dados
  (if (not (boundp '*DB-ACESSO-GOTA*))
    (if (findfile "GT_DB.lsp")
      (load (findfile "GT_DB.lsp"))
      (progn (alert "GT_DB.lsp nao encontrado! Coloque-o na pasta de suporte do CAD.")
             (exit))
    )
  )

  (setq old-os (getvar "OSMODE"))
  (setvar "OSMODE" 0)
  (gt:layers)

  ;---------------- 1. TIPO / MANUAL ----------------
  (initget "Acesso Intersecao")
  (setq tipo (getkword "\nGeometria para [Acesso(IPR728,R=15)/Intersecao(IPR718,R=12)] <Acesso>: ")
        grupo (if (eq tipo "Intersecao") 'GOTA-PEQUENA 'ACESSO))

  ;---------------- 2. VELOCIDADES ----------------
  (princ "\n=== DADOS DAS VIAS ===")
  (setq vp (gt:getreal-default "Velocidade diretriz da RODOVIA (km/h)" 60.0)
        vs (gt:getreal-default "Velocidade diretriz do ACESSO/SECUNDARIA (km/h)" 40.0))
  (princ (strcat "\n>> Tabela 47/76 - taper ref.: " (rtos (gt:taper-fmv vp) 2 0)
                 " m | transicao terminal: " (rtos (gt:taper-transicao vp) 2 0)
                 " m | nivel 1/2 (ARTESP): taper 15 m + mesa 15 m"))

  ;---------------- 3. LARGURAS ----------------
  (setq lp (gt:getreal-default "Largura da pista de rolamento da RODOVIA (m)" 7.20)
        ls (gt:getreal-default "Largura da pista do ACESSO (m)" 5.50)
        lf (gt:getreal-default "Largura da faixa adicional W (m)" 3.50))

  ;---------------- 4. EIXOS ----------------
  (setq eixo-p (gt:sel-eixo "Selecione o EIXO da RODOVIA (clique perto da intersecao)")
        ex1    (gt:eixo-ent (car eixo-p) (cadr eixo-p)))
  (if (null ex1) (progn (alert "Nao li o eixo da rodovia.") (exit)))
  (setq ip (car ex1) up (cadr ex1))
  (setq eixo-s (gt:sel-eixo "Selecione o EIXO do ACESSO/SECUNDARIA (clique longe da rodovia)")
        ex2    (gt:eixo-ent (car eixo-s) (cadr eixo-s)))
  (if (null ex2) (progn (alert "Nao li o eixo do acesso.") (exit)))
  (setq us (cadr ex2) pref-s (gt:2d (cadr eixo-s)))

  ; intersecao dos eixos
  (setq pb (gt:inters ip up (car ex2) us))
  (if (null pb)
    (progn (alert "Os eixos nao se interceptam no trecho clicado!")
           (setvar "OSMODE" old-os) (exit)))
  ; us aponta da rodovia para o acesso
  (if (< (gt:dot us (gt:v- pref-s pb)) 0.0)
    (setq us (gt:v*s us -1.0)))
  ; angulo alfa entre a rodovia e a chegada do acesso
  (setq alfa (* (/ 180.0 pi)
                (atan (abs (gt:cross up (gt:v*s us -1.0)))
                      (gt:dot up (gt:v*s us -1.0))))
        alfa-min (min alfa (- 180.0 alfa)))
  (princ (strcat "\n>> Angulo entre os eixos: alfa = " (rtos alfa 2 1)
                 " graus (quadrante oposto: " (rtos (- 180.0 alfa) 2 1) ")"))
  (if (or (< alfa-min 70.0) (> alfa-min 110.0))
    (princ "\n>> AVISO: alfa fora de 70-110 graus - ver Apendice A do Manual de Intersecoes."))

  ;---------------- 5. BORDO DA RODOVIA ----------------
  (setq noff   (gt:perp up)
        sinal  (if (> (gt:dot (gt:v- pref-s pb) noff) 0.0) 1.0 -1.0)
        bordop (gt:v+ pb (gt:v*s noff (* sinal (/ lp 2.0))))
        bordou up)
  (gt:line (gt:v+ bordop (gt:v*s bordou -80.0))
           (gt:v+ bordop (gt:v*s bordou  80.0)) "GT-EIXO")

  ;---------------- 6. GOTA ----------------
  (setq r (if (< alfa-min 90.0)
            (gt:par grupo 'raio-giro-min)
            (gt:par grupo 'raio-giro)))
  (initget "Direita Esquerda")
  (setq lado-str (getkword "\nDesvio do eixo da gota [Direita/Esquerda] <Direita>: ")
        lado (if (eq lado-str "Esquerda") 1.0 -1.0))
  (princ (strcat "\nConstruindo gota (R = " (rtos r 2 0) " m)..."))
  (setq res (gt:gota pb us bordop bordou pb up lado r grupo))
  (if res
    (princ (strcat "\n>> Gota criada. Ponta inferior a "
                   (rtos (gt:dist pb (cadr res)) 2 1)
                   " m da intersecao dos eixos."))
  )

  ;---------------- 7. TAPER / FAIXA NA RODOVIA ----------------
  (initget "N12 N34 Nao")
  (setq nv (getkword "\nFaixa de mudanca de velocidade na rodovia [N12=taper15+mesa15 / N34=tabelas / Nao] <Nao>: "))
  (if (member nv '("N12" "N34"))
    (progn
      (initget "Direita Esquerda")
      (setq dir-str (getkword "\nSentido da faixa a partir da intersecao [Direita/Esquerda] <Direita>: ")
            dir (if (eq dir-str "Esquerda") -1.0 1.0))
      (cond
        ((eq nv "N12")
          (setq lt (cdr (assoc 'taper *DB-ACESSO-FMV-N12*))
                lm (cdr (assoc 'mesa  *DB-ACESSO-FMV-N12*))))
        (t
          (setq vcurva (gt:getreal-default "Velocidade de seguranca da curva (0/20/30/40/50/60/70/80)" 0.0)
                fmv (gt:faixa-mv-tab 'DESACEL vp vcurva))
          (if (cadr fmv)
            (setq lt (car fmv) lm (- (cadr fmv) (car fmv)))
            (progn (princ "\n>> Combinacao nao aplicavel; usando taper minimo.")
                   (setq lt (gt:taper-fmv vp) lm 0.0)))
        )
      )
      (setq a1 (gt:v+ pb (gt:v*s noff (* sinal (/ lp 2.0)))))
      (gt:desenha-faixa a1 (gt:v*s bordou dir) sinal lt lm lf "GT-FAIXA")
      (princ (strcat "\n>> Faixa desenhada: taper " (rtos lt 2 0)
                     " m + mesa " (rtos lm 2 0) " m x W " (rtos lf 2 2) " m"))
    )
  )

  ;---------------- RESUMO ----------------
  (princ "\n================ RESUMO (DNIT IPR 728 / 718) ================")
  (princ (strcat "\n Manual:    " (if (eq grupo 'ACESSO) "Acessos IPR 728 (R=15)" "Intersecoes IPR 718 (R=12)")))
  (princ (strcat "\n V rodovia/acesso: " (rtos vp 2 0) " / " (rtos vs 2 0) " km/h"))
  (princ (strcat "\n Alfa:      " (rtos alfa 2 1) " graus | R dos giros: " (rtos r 2 0) " m"))
  (princ "\n Camadas:   GT-GOTA, GT-GIRO, GT-FAIXA, GT-EIXO, GT-PINTURA")
  (princ "\n=============================================================")
  (setvar "OSMODE" old-os)
  (princ)
)

; Consulta Tabela 6/7 (IPR 728) = Tabela 48 (IPR 718)
(defun gt:faixa-mv-tab (tipo vel vcurva) (gt:faixa-mv tipo vel vcurva))

;=====================================================================
; COMANDO: TAPER ISOLADO
;=====================================================================
(defun c:TAPER (/ *error* old-os vel w nv eixo ex pini dir-str dir lt lm)
  (defun *error* (msg)
    (if old-os (setvar "OSMODE" old-os))
    (princ)
  )
  (if (not (boundp '*DB-ACESSO-GOTA*))
    (if (findfile "GT_DB.lsp") (load (findfile "GT_DB.lsp"))
      (progn (alert "GT_DB.lsp nao encontrado!") (exit)))
  )
  (setq old-os (getvar "OSMODE"))
  (setvar "OSMODE" 512)
  (gt:layers)

  (setq vel (gt:getreal-default "Velocidade diretriz (km/h)" 60.0)
        w   (gt:getreal-default "Largura do deslocamento W (m)" 3.50))
  (princ (strcat "\n>> Nivel 1/2: taper 15 m + mesa 15 m | Tabela 47: "
                 (rtos (gt:taper-fmv vel) 2 0) " m | Tabela 76: "
                 (rtos (gt:taper-transicao vel) 2 0) " m"))
  (initget "N12 Tab47 Tab76")
  (setq nv (getkword "\nCriterio [N12 / Tab47 / Tab76] <N12>: "))
  (cond
    ((eq nv "Tab47") (setq lt (gt:taper-fmv vel) lm 0.0))
    ((eq nv "Tab76") (setq lt (gt:taper-transicao vel) lm 0.0))
    (t (setq lt 15.0 lm 15.0))
  )
  (setq eixo (gt:sel-eixo "Selecione o bordo/eixo de referencia (clique perto do inicio)")
        ex   (gt:eixo-ent (car eixo) (cadr eixo)))
  (if (null ex) (progn (alert "Referencia invalida.") (exit)))
  (setq pini (getpoint "\nPonto inicial do taper: "))
  (if pini
    (progn
      (setq pini (gt:foot (gt:2d pini) (car ex) (cadr ex)))
      (initget "Direita Esquerda")
      (setq dir-str (getkword "\nAbrir a faixa para [Direita/Esquerda] <Direita>: ")
            dir (if (eq dir-str "Esquerda") 1.0 -1.0))
      (gt:desenha-faixa pini (cadr ex) dir lt lm w "GT-TAPER")
      (princ (strcat "\n>> Desenhado: taper " (rtos lt 2 0) " m + mesa "
                     (rtos lm 2 0) " m x W " (rtos w 2 2) " m"))
    )
  )
  (setvar "OSMODE" old-os)
  (princ)
)

(princ "\nGotaTape.lsp carregado (IPR 728 + IPR 718). Comandos: GOTA e TAPER.")
(princ)
