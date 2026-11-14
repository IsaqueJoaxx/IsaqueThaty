;;; ==========================================================================
;;;  GreideAuto.lsp  -  Geracao automatica de greide (perfil de projeto)
;;;  AutoCAD Civil 3D 2026  (funciona tambem em versoes anteriores)
;;;
;;;  Estrategia: o greide parte de uma COTA INICIAL informada e segue o
;;;  terreno por tangentes, com a rampa de cada trecho ajustada por minimos
;;;  quadrados ao perfil natural e truncada na RAMPA MAXIMA. Nos PIVs entram
;;;  curvas verticais parabolicas simetricas dimensionadas pelo K minimo da
;;;  velocidade diretriz.
;;;
;;;  COMANDOS
;;;    GREIDEAUTO  - gera o greide no alinhamento selecionado
;;;    GREIDEEXP   - exporta apenas o perfil do terreno para CSV
;;;    GREIDECFG   - mostra/zera os parametros memorizados
;;;
;;;  SAIDAS (gravadas na pasta do desenho)
;;;    <align>_terreno.csv     perfil do terreno amostrado
;;;    <align>_greide_pvis.csv tabela de PIVs (estaca, cota, rampas, A, K, L)
;;;    <align>_greide_pvis.txt formato do "Create Profile from File"
;;;
;;;  Desenvolvido para Isaque Ferraz Moreira.
;;; ==========================================================================

(vl-load-com)

;;; --------------------------------------------------------------------------
;;; Utilitarios
;;; --------------------------------------------------------------------------

(defun gr:r (v n) (rtos v 2 n))

(defun gr:sgn (v) (cond ((> v 0.0) 1.0) ((< v 0.0) -1.0) (T 0.0)))

(defun gr:abs (v) (if (< v 0.0) (- v) v))

(defun gr:est (s / n r)
  (setq n (fix (/ s *gr-corda*))
        r (- s (* n *gr-corda*)))
  (if (< r 0.0) (setq n (1- n) r (+ r *gr-corda*)))
  (strcat (itoa n) "+" (rtos r 2 3)))

(defun gr:msg (s) (princ (strcat "\n" s)))

;; substitui o elemento de indice i (0-based) de uma lista
(defun gr:setnth (lst i val / j out)
  (setq j 0 out '())
  (foreach e lst
    (setq out (cons (if (= j i) val e) out) j (1+ j)))
  (reverse out))

;; leitura segura de propriedade COM
(defun gr:prop (obj p / r)
  (setq r (vl-catch-all-apply 'vlax-get (list obj p)))
  (if (vl-catch-all-error-p r) nil r))

;; chamada segura de metodo COM
(defun gr:met (obj m arg / r)
  (setq r (vl-catch-all-apply 'vlax-invoke (list obj m arg)))
  (if (vl-catch-all-error-p r) nil r))

;;; --------------------------------------------------------------------------
;;; Rastreamento de etapas: se algo falhar, gr:roda diz ONDE falhou
;;; --------------------------------------------------------------------------

(defun gr:etapa (s)
  (setq *gr-etapa* s)
  (princ (strcat "\n-> " s))
  (princ))

(defun gr:safe (s / i c out)
  (setq i 0 out "")
  (while (< i (strlen s))
    (setq c (substr s (1+ i) 1))
    (setq out (strcat out (if (vl-string-search c "\\/:*?\"<>|") "_" c)))
    (setq i (1+ i)))
  out)

;; le um real com valor padrao
(defun gr:getreal (msg def / v)
  (setq v (getreal (strcat "\n" msg " <" (gr:r def 3) ">: ")))
  (if v v def))

(defun gr:getint (msg def / v)
  (setq v (getint (strcat "\n" msg " <" (itoa def) ">: ")))
  (if v v def))

;;; --------------------------------------------------------------------------
;;; Tabelas de K minimo (parada) - AASHTO metrico / DNIT
;;; Altere aqui se a norma do seu projeto pedir outros valores.
;;; --------------------------------------------------------------------------

(defun gr:kconvexa (v)
  (cond ((<= v 30)  2.0) ((<= v 40)  4.0) ((<= v 50)  7.0)
        ((<= v 60) 11.0) ((<= v 70) 17.0) ((<= v 80) 26.0)
        ((<= v 90) 39.0) ((<= v 100) 52.0) ((<= v 110) 74.0) (T 95.0)))

(defun gr:kconcava (v)
  (cond ((<= v 30)  6.0) ((<= v 40)  9.0) ((<= v 50) 13.0)
        ((<= v 60) 18.0) ((<= v 70) 23.0) ((<= v 80) 30.0)
        ((<= v 90) 38.0) ((<= v 100) 45.0) ((<= v 110) 55.0) (T 63.0)))

;;; --------------------------------------------------------------------------
;;; Acesso ao Civil 3D
;;; --------------------------------------------------------------------------

;; devolve o vla-object do alinhamento selecionado (ou nil)
(defun gr:pega-alinhamento ( / e obj nome pr)
  (gr:msg "Selecione o ALINHAMENTO:")
  (setq e (entsel "\n> "))
  (if (null e)
    (progn (gr:msg "Nada selecionado.") nil)
    (progn
      (setq obj  (vlax-ename->vla-object (car e))
            nome (vl-catch-all-apply 'vlax-get (list obj 'Name))
            pr   (vl-catch-all-apply 'vlax-get (list obj 'Profiles)))
      (if (or (vl-catch-all-error-p nome) (vl-catch-all-error-p pr) (null pr))
        (progn
          (gr:msg "O objeto selecionado nao e um alinhamento do Civil 3D.")
          nil)
        (progn
          (gr:msg (strcat "Alinhamento: " nome))
          obj)))))

;; ---- estrategia A: metodo ElevationAt ------------------------------------
(defun gr:le-elevationat (prof s0 s1 passo / s lst r)
  (setq s s0 lst '())
  (while (< s (- s1 1e-6))
    (setq r (gr:met prof 'ElevationAt s))
    (if (numberp r) (setq lst (cons (list s r) lst)))
    (setq s (+ s passo)))
  (setq r (gr:met prof 'ElevationAt s1))
  (if (numberp r) (setq lst (cons (list s1 r) lst)))
  (reverse lst))

;; ---- estrategia B: vertices do perfil (colecao PVIs) ----------------------
(defun gr:le-pvis (prof / col n i p s z lst)
  (setq col (gr:prop prof 'PVIs))
  (if (null col) nil
    (progn
      (setq n (gr:prop col 'Count) i 0 lst '())
      (if (null n) nil
        (progn
          (while (< i n)
            (setq p (vl-catch-all-apply 'vlax-invoke (list col 'Item i)))
            (if (not (vl-catch-all-error-p p))
              (progn
                (setq s (gr:prop p 'Station) z (gr:prop p 'Elevation))
                (if (and (numberp s) (numberp z)) (setq lst (cons (list s z) lst)))))
            (setq i (1+ i)))
          (reverse lst))))))

;; ---- estrategia C: entidades do perfil ------------------------------------
(defun gr:le-entidades (prof / col n i e s z lst)
  (setq col (gr:prop prof 'Entities))
  (if (null col) nil
    (progn
      (setq n (gr:prop col 'Count) i 0 lst '())
      (if (null n) nil
        (progn
          (while (< i n)
            (setq e (vl-catch-all-apply 'vlax-invoke (list col 'Item i)))
            (if (not (vl-catch-all-error-p e))
              (progn
                (setq s (gr:prop e 'StartStation) z (gr:prop e 'StartElevation))
                (if (and (numberp s) (numberp z)) (setq lst (cons (list s z) lst)))
                (setq s (gr:prop e 'EndStation) z (gr:prop e 'EndElevation))
                (if (and (numberp s) (numberp z)) (setq lst (cons (list s z) lst)))))
            (setq i (1+ i)))
          (reverse lst))))))

;; ordena por estacao e remove estacoes repetidas
(defun gr:limpa (lst / ord out ant)
  (setq ord (vl-sort lst '(lambda (a b) (< (car a) (car b)))) out '() ant nil)
  (foreach p ord
    (if (or (null ant) (> (- (car p) ant) 1e-4))
      (progn (setq out (cons p out)) (setq ant (car p)))))
  (reverse out))

;; le o terreno tentando as tres estrategias, em silencio.
;; Devolve lista ((est cota) ...) ja ordenada, ou nil.
(defun gr:le-terreno (prof s0 s1 passo / lst)
  (setq lst (gr:le-elevationat prof s0 s1 passo))
  (if (< (length lst) 3) (setq lst (gr:le-pvis prof)))
  (if (< (length lst) 3) (setq lst (gr:le-entidades prof)))
  (if (>= (length lst) 3) (gr:limpa lst) nil))

;;; --------------------------------------------------------------------------
;;; Nucleo de calculo
;;; --------------------------------------------------------------------------

;; rampa por minimos quadrados de sub ate tail (inclusive), ancorada em (s0 z0)
(defun gr:rampa (sub tail s0 z0 imax imin sinal / num den cur p ds g stop)
  (setq num 0.0 den 0.0 cur sub stop (cdr tail))
  (while (and cur (not (eq cur stop)))
    (setq p (car cur) ds (- (car p) s0))
    (if (> ds 1e-9)
      (setq num (+ num (* ds (- (cadr p) z0)))
            den (+ den (* ds ds))))
    (setq cur (cdr cur)))
  (setq g (if (> den 1e-9) (/ num den) 0.0))
  (if (> (gr:abs g) imax) (setq g (* imax (gr:sgn g))))
  (if (and (> imin 0.0) (< (gr:abs g) imin))
    (setq g (* imin (if (/= 0.0 (gr:sgn g)) (gr:sgn g) (if (/= 0.0 sinal) sinal 1.0)))))
  g)

;; desvio maximo entre a tangente e o terreno no trecho sub..tail
(defun gr:desvio (sub tail s0 z0 g / cur p d dmax stop)
  (setq dmax 0.0 cur sub stop (cdr tail))
  (while (and cur (not (eq cur stop)))
    (setq p (car cur)
          d (gr:abs (- (+ z0 (* g (- (car p) s0))) (cadr p))))
    (if (> d dmax) (setq dmax d))
    (setq cur (cdr cur)))
  dmax)

;; gera a poligonal do greide: lista de PIVs (est cota gin gout L tipo Kmin Lteo)
;; imax/imin em decimal. Devolve (list pvis avisos)
(defun gr:gerar (pts cota0 imax imin ltmin tol
                 / n sfim res sub s0 z0 tail tbest gbest t0 g0 dev0 dev g
                   sinal avisos cur limite ok s1 z1 achou ant)
  (setq sfim   (car (last pts))
        res    (list (list (car (car pts)) cota0 0.0 0.0 0.0 "-" 0.0 0.0))
        sub    pts
        z0     cota0
        sinal  0.0
        avisos '())

  (while (cdr sub)
    (setq s0    (car (car sub))
          z0    (cadr (car res))
          tail  (cdr sub)
          tbest nil gbest nil t0 nil g0 nil dev0 nil achou nil)

    ;; 1a passada: maior tangente dentro da tolerancia
    (while (and tail (not achou))
      (if (>= (- (car (car tail)) s0) (- ltmin 1e-6))
        (progn
          (setq g   (gr:rampa sub tail s0 z0 imax imin sinal)
                dev (gr:desvio sub tail s0 z0 g))
          (if (null t0) (setq t0 tail g0 g dev0 dev))
          (if (<= dev tol)
            (setq tbest tail gbest g)
            (if tbest (setq achou T)))))
      (if (not achou) (setq tail (cdr tail))))

    ;; 2a passada: terreno nao cabe na tolerancia - estende enquanto o desvio
    ;; nao piorar mais que 35 %, para nao picotar o greide
    (if (and (null tbest) t0)
      (progn
        (setq limite (max tol (* dev0 1.35))
              tbest  t0
              gbest  g0
              tail   (cdr t0)
              ok     T)
        (while (and tail ok)
          (setq g   (gr:rampa sub tail s0 z0 imax imin sinal)
                dev (gr:desvio sub tail s0 z0 g))
          (if (<= dev limite)
            (progn (setq tbest tail gbest g) (setq tail (cdr tail)))
            (setq ok nil)))
        (setq avisos
              (cons (strcat "Est. " (gr:est s0) ": terreno nao cabe na tolerancia de "
                            (gr:r tol 2) " m; tangente de "
                            (gr:r (- (car (car tbest)) s0) 0) " m com desvio de "
                            (gr:r (gr:desvio sub tbest s0 z0 gbest) 2) " m.")
                    avisos))))

    ;; nao fechou nem a tangente minima: vai ate o fim
    (if (null tbest)
      (progn
        (setq tbest sub)
        (while (cdr tbest) (setq tbest (cdr tbest)))
        (setq gbest (gr:rampa sub tbest s0 z0 imax imin sinal))))

    ;; se o resto ate o fim for menor que a tangente minima, estende ate o fim
    (if (< (- sfim (car (car tbest))) (- ltmin 1e-6))
      (progn
        (setq tbest sub)
        (while (cdr tbest) (setq tbest (cdr tbest)))
        (setq gbest (gr:rampa sub tbest s0 z0 imax imin sinal))))

    (setq s1 (car (car tbest))
          z1 (+ z0 (* gbest (- s1 s0))))
    ;; grava a rampa de saida no PIV anterior (e a de entrada, se for o 1o)
    (setq ant (car res))
    (setq ant (list (nth 0 ant) (nth 1 ant)
                    (if (null (cdr res)) gbest (nth 2 ant))
                    gbest 0.0 "-" 0.0 0.0))
    (setq res (cons (list s1 z1 gbest gbest 0.0 "-" 0.0 0.0)
                    (cons ant (cdr res))))
    (if (/= 0.0 gbest) (setq sinal (gr:sgn gbest)))
    (setq sub tbest))

  (setq res (reverse res))
  (list res (reverse avisos)))

;; dimensiona as curvas verticais. pvis = lista mutavel de listas
;; devolve (list pvis avisos)
(defun gr:curvas (pvis vel kconv kconc lminabs
                  / n i p A tipo kmin L lmin res avisos it mudou
                    a b tg need fator)
  (setq lmin (max lminabs (* 0.6 vel))
        n    (length pvis)
        res  '()
        avisos '()
        i    0)
  (while (< i n)
    (setq p (nth i pvis))
    (if (or (= i 0) (= i (1- n)))
      (setq res (cons (list (nth 0 p) (nth 1 p) (nth 2 p) (nth 3 p) 0.0 "-" 0.0 0.0) res))
      (progn
        (setq A    (* 100.0 (- (nth 3 p) (nth 2 p))))
        (if (< (gr:abs A) 0.05)
          (setq res (cons (list (nth 0 p) (nth 1 p) (nth 2 p) (nth 3 p) 0.0 "-" 0.0 0.0) res))
          (progn
            (setq tipo (if (< A 0.0) "convexa" "concava")
                  kmin (if (< A 0.0) kconv kconc)
                  L    (max (* kmin (gr:abs A)) lmin))
            (setq res (cons (list (nth 0 p) (nth 1 p) (nth 2 p) (nth 3 p) L tipo kmin L) res))))))
    (setq i (1+ i)))
  (setq res (reverse res))

  ;; resolve sobreposicao das curvas nas tangentes
  (setq it 0)
  (while (< it 30)
    (setq mudou nil i 0)
    (while (< i (1- n))
      (setq a (nth i res) b (nth (1+ i) res)
            tg (- (nth 0 b) (nth 0 a))
            need (+ (/ (nth 4 a) 2.0) (/ (nth 4 b) 2.0)))
      (if (and (> need 0.0) (> need (- tg 1e-6)))
        (progn
          (setq fator (/ (* tg 0.995) need))
          (if (> (nth 4 a) 0.0)
            (setq res (gr:setnth res i
                        (list (nth 0 a)(nth 1 a)(nth 2 a)(nth 3 a)
                              (* (nth 4 a) fator)(nth 5 a)(nth 6 a)(nth 7 a)))))
          (setq b (nth (1+ i) res))
          (if (> (nth 4 b) 0.0)
            (setq res (gr:setnth res (1+ i)
                        (list (nth 0 b)(nth 1 b)(nth 2 b)(nth 3 b)
                              (* (nth 4 b) fator)(nth 5 b)(nth 6 b)(nth 7 b)))))
          (setq mudou T)))
      (setq i (1+ i)))
    (if mudou (setq it (1+ it)) (setq it 99)))

  ;; avisos de curva reduzida
  (setq i 1)
  (while (< i (1- n))
    (setq p (nth i res))
    (if (and (> (nth 4 p) 0.0) (< (nth 4 p) (- (nth 7 p) 1e-6)))
      (setq avisos
            (cons (strcat "Est. " (gr:est (nth 0 p)) ": curva reduzida de "
                          (gr:r (nth 7 p) 1) " m para " (gr:r (nth 4 p) 1)
                          " m por falta de tangente"
                          (if (< (/ (nth 4 p) (gr:abs (* 100.0 (- (nth 3 p) (nth 2 p)))))
                                 (nth 6 p))
                            " - K ABAIXO DO MINIMO, REVISAR." "."))
                  avisos)))
    (setq i (1+ i)))
  (list res (reverse avisos)))

;; cota do greide em uma estacao (com as parabolas)
(defun gr:cota (pvis s / n i p a b pcv zpcv x A r)
  (setq n (length pvis) r nil i 1)
  (setq a (car pvis))
  (if (<= s (nth 0 a)) (setq r (+ (nth 1 a) (* (nth 3 a) (- s (nth 0 a))))))
  (setq b (nth (1- n) pvis))
  (if (and (null r) (>= s (nth 0 b)))
    (setq r (+ (nth 1 b) (* (nth 2 b) (- s (nth 0 b))))))
  (while (and (null r) (< i (1- n)))
    (setq p (nth i pvis))
    (if (and (> (nth 4 p) 0.0) (<= (gr:abs (- s (nth 0 p))) (/ (nth 4 p) 2.0)))
      (progn
        (setq pcv  (- (nth 0 p) (/ (nth 4 p) 2.0))
              zpcv (- (nth 1 p) (* (nth 2 p) (/ (nth 4 p) 2.0)))
              x    (- s pcv)
              A    (- (nth 3 p) (nth 2 p))
              r    (+ zpcv (* (nth 2 p) x) (* (/ A (* 2.0 (nth 4 p))) x x)))))
    (setq i (1+ i)))
  (setq i 0)
  (while (and (null r) (< i (1- n)))
    (setq a (nth i pvis) b (nth (1+ i) pvis))
    (if (and (>= s (nth 0 a)) (<= s (nth 0 b)))
      (setq r (+ (nth 1 a) (* (nth 3 a) (- s (nth 0 a))))))
    (setq i (1+ i)))
  (if r r (nth 1 (nth (1- n) pvis))))

;;; --------------------------------------------------------------------------
;;; Criacao do perfil no Civil 3D (COM)
;;; --------------------------------------------------------------------------

(defun gr:criar-perfil (align nome pvis / profs np r pvcol pvi i n L ok nc ents e1 e2)
  (setq profs (vlax-get align 'Profiles))
  (setq np (vl-catch-all-apply 'vlax-invoke (list profs 'Add nome)))
  (if (vl-catch-all-error-p np)
    (setq np (vl-catch-all-apply 'vlax-invoke (list profs 'Add nome "0" "" ""))))
  (if (vl-catch-all-error-p np)
    (progn
      (gr:msg "*** Nao foi possivel criar o perfil pela API COM.")
      (gr:msg "    Use o LandXML ou o 'Create Profile from File' (arquivos gerados abaixo).")
      nil)
    (progn
      (setq pvcol (vl-catch-all-apply 'vlax-get (list np 'PVIs)))
      (if (vl-catch-all-error-p pvcol)
        (progn (gr:msg "*** Perfil criado, mas a colecao de PVIs nao respondeu.") nil)
        (progn
          (foreach p pvis
            (setq r (vl-catch-all-apply 'vlax-invoke
                      (list pvcol 'Add (nth 0 p) (nth 1 p))))
            (if (vl-catch-all-error-p r)
              (gr:msg (strcat "  ! PVI na est. " (gr:est (nth 0 p)) " nao foi criado."))))
          ;; curvas verticais
          (setq n (length pvis) i 1 nc 0)
          (while (< i (1- n))
            (setq L (nth 4 (nth i pvis)))
            (if (> L 0.0)
              (progn
                (setq ok nil)
                ;; tentativa 1: propriedade CurveLength do PVI
                (setq pvi (vl-catch-all-apply 'vlax-invoke (list pvcol 'Item i)))
                (if (not (vl-catch-all-error-p pvi))
                  (progn
                    (setq r (vl-catch-all-apply 'vlax-put (list pvi 'CurveLength L)))
                    (if (not (vl-catch-all-error-p r)) (setq ok T))))
                ;; tentativa 2: colecao de entidades do perfil
                (if (not ok)
                  (progn
                    (setq ents (vl-catch-all-apply 'vlax-get (list np 'Entities)))
                    (if (not (vl-catch-all-error-p ents))
                      (progn
                        (setq e1 (vl-catch-all-apply 'vlax-invoke (list ents 'Item (1- i)))
                              e2 (vl-catch-all-apply 'vlax-invoke (list ents 'Item i)))
                        (if (and (not (vl-catch-all-error-p e1))
                                 (not (vl-catch-all-error-p e2)))
                          (progn
                            (setq r (vl-catch-all-apply 'vlax-invoke
                                      (list ents 'AddFreeSymmetricParabolaByLength e1 e2 L)))
                            (if (not (vl-catch-all-error-p r)) (setq ok T))))))))
                (if ok (setq nc (1+ nc)))))
            (setq i (1+ i)))
          (gr:msg (strcat "  Perfil '" nome "' criado com " (itoa (length pvis))
                          " PIVs e " (itoa nc) " curva(s) vertical(is)."))
          (if (< nc (gr:conta-curvas pvis))
            (progn
              (gr:msg "  ! Nem todas as curvas verticais foram aplicadas pela API.")
              (gr:msg "    Importe o LandXML gerado pelo greide_auto.py para ter as curvas,")
              (gr:msg "    ou ajuste manualmente pelo Profile Layout Tools.")))
          np)))))

(defun gr:conta-curvas (pvis / c)
  (setq c 0)
  (foreach p pvis (if (> (nth 4 p) 0.0) (setq c (1+ c))))
  c)

;;; --------------------------------------------------------------------------
;;; Gravacao de arquivos
;;; --------------------------------------------------------------------------

(defun gr:grava-terreno (arq pts / f)
  (setq f (open arq "w"))
  (if f
    (progn
      (write-line "estaca;cota" f)
      (foreach p pts
        (write-line (strcat (gr:r (car p) 4) ";" (gr:r (cadr p) 4)) f))
      (close f) T)
    nil))

(defun gr:grava-pvis-csv (arq pvis / f A k i)
  (setq f (open arq "w"))
  (if f
    (progn
      (write-line "PIV;Estaca;Progressiva(m);Cota(m);i_entrada(%);i_saida(%);A(%);Tipo;Kmin;L(m);Kreal;PCV(m);PTV(m)" f)
      (setq i 1)
      (foreach p pvis
        (setq A (* 100.0 (- (nth 3 p) (nth 2 p)))
              k (if (> (gr:abs A) 1e-9) (/ (nth 4 p) (gr:abs A)) 0.0))
        (write-line
          (strcat (itoa i) ";" (gr:est (nth 0 p)) ";" (gr:r (nth 0 p) 3) ";"
                  (gr:r (nth 1 p) 3) ";" (gr:r (* 100.0 (nth 2 p)) 3) ";"
                  (gr:r (* 100.0 (nth 3 p)) 3) ";" (gr:r A 3) ";" (nth 5 p) ";"
                  (gr:r (nth 6 p) 1) ";" (gr:r (nth 4 p) 2) ";"
                  (if (> (nth 4 p) 0.0) (gr:r k 1) "") ";"
                  (if (> (nth 4 p) 0.0) (gr:r (- (nth 0 p) (/ (nth 4 p) 2.0)) 3) "") ";"
                  (if (> (nth 4 p) 0.0) (gr:r (+ (nth 0 p) (/ (nth 4 p) 2.0)) 3) ""))
          f)
        (setq i (1+ i)))
      (close f) T)
    nil))

(defun gr:grava-pvis-txt (arq pvis / f)
  (setq f (open arq "w"))
  (if f
    (progn
      (foreach p pvis
        (write-line (strcat (gr:r (nth 0 p) 4) " " (gr:r (nth 1 p) 4)) f))
      (close f) T)
    nil))

;;; --------------------------------------------------------------------------
;;; Parametros memorizados na sessao
;;; --------------------------------------------------------------------------

(if (null *gr-corda*)   (setq *gr-corda*   20.0))
(if (null *gr-imax*)    (setq *gr-imax*     8.0))
(if (null *gr-imin*)    (setq *gr-imin*     0.5))
(if (null *gr-vel*)     (setq *gr-vel*     40.0))
(if (null *gr-passo*)   (setq *gr-passo*   10.0))
(if (null *gr-tol*)     (setq *gr-tol*      0.20))
(if (null *gr-ltmin*)   (setq *gr-ltmin*   40.0))
(if (null *gr-lmin*)    (setq *gr-lmin*     0.0))
(if (null *gr-tipo*)    (setq *gr-tipo*     1))

;;; --------------------------------------------------------------------------
;;; Executor blindado: nenhum comando aborta calado. Se der erro, ele diz
;;; qual foi a etapa e qual foi a mensagem.
;;; --------------------------------------------------------------------------

(defun gr:roda (fn / r)
  (setq *gr-etapa* "iniciando")
  (setq r (vl-catch-all-apply fn nil))
  (if (vl-catch-all-error-p r)
    (progn
      (princ "\n")
      (princ "\n*******************************************************")
      (princ "\n*** A ROTINA PAROU AQUI:")
      (princ (strcat "\n***   ETAPA ...: " (if *gr-etapa* *gr-etapa* "(desconhecida)")))
      (princ (strcat "\n***   MENSAGEM : " (vl-catch-all-error-message r)))
      (princ "\n*** Me mande estas duas linhas que eu corrijo.")
      (princ "\n*******************************************************")))
  (princ))

;;; --------------------------------------------------------------------------
;;; Escolha do perfil do terreno - SEM perguntar numero.
;;; Testa cada perfil, ve qual devolve terreno legivel e escolhe o melhor.
;;; Devolve (perfil nome pontos) ou nil.
;;; --------------------------------------------------------------------------

(defun gr:acha-terreno (align s0 s1 passo / profs n i p nome pts cand melhor nota)
  (gr:etapa "abrindo a colecao de perfis do alinhamento")
  (setq profs (gr:prop align 'Profiles))
  (if (null profs)
    (progn (gr:msg "*** O objeto selecionado nao tem perfis (nao e um alinhamento).") nil)
    (progn
      (setq n (gr:prop profs 'Count))
      (if (or (null n) (zerop n))
        (progn (gr:msg "*** O alinhamento nao tem nenhum perfil. Crie o perfil do terreno primeiro.") nil)
        (progn
          (gr:msg (strcat "Perfis encontrados: " (itoa n)))
          (setq i 0 cand '())
          (while (< i n)
            (gr:etapa (strcat "lendo o perfil " (itoa (1+ i)) " de " (itoa n)))
            (setq p (vl-catch-all-apply 'vlax-invoke (list profs 'Item i)))
            (if (vl-catch-all-error-p p)
              (setq p (vl-catch-all-apply 'vlax-invoke (list profs 'Item (1+ i)))))
            (if (vl-catch-all-error-p p) (setq p nil))
            (if p
              (progn
                (setq nome (gr:prop p 'Name))
                (if (null nome) (setq nome (strcat "perfil " (itoa (1+ i)))))
                (setq pts (gr:le-terreno p s0 s1 passo))
                ;; nota: prioriza nome de terreno e quantidade de pontos
                (setq nota (length pts))
                (if (or (vl-string-search "EG" (strcase nome))
                        (vl-string-search "TERR" (strcase nome))
                        (vl-string-search "EXIST" (strcase nome))
                        (vl-string-search "NATUR" (strcase nome))
                        (vl-string-search "SUPERF" (strcase nome)))
                  (setq nota (+ nota 100000)))
                (if (or (vl-string-search "GREIDE" (strcase nome))
                        (vl-string-search "PROJET" (strcase nome))
                        (vl-string-search "LAYOUT" (strcase nome))
                        (vl-string-search "FG" (strcase nome)))
                  (setq nota (- nota 50000)))
                (gr:msg (strcat "  " (itoa (1+ i)) ") " nome
                                "  ->  " (itoa (length pts)) " pontos de terreno"))
                (if (>= (length pts) 3)
                  (setq cand (cons (list nota p nome pts) cand)))))
            (setq i (1+ i)))

          (if (null cand)
            (progn
              (gr:msg "*** Nenhum perfil devolveu terreno legivel.")
              nil)
            (progn
              (setq melhor (car (vl-sort cand '(lambda (a b) (> (car a) (car b))))))
              (gr:msg (strcat "Perfil de terreno adotado: " (nth 2 melhor)))
              (list (nth 1 melhor) (nth 2 melhor) (nth 3 melhor)))))))))

;;; --------------------------------------------------------------------------
;;; GREIDECFG
;;; --------------------------------------------------------------------------

(defun c:GREIDECFG ()
  (gr:msg "Parametros memorizados:")
  (gr:msg (strcat "  Corda da estaca ......... " (gr:r *gr-corda* 1) " m"))
  (gr:msg (strcat "  Rampa maxima ............ " (gr:r *gr-imax* 3) " %"))
  (gr:msg (strcat "  Rampa minima ............ " (gr:r *gr-imin* 3) " %"))
  (gr:msg (strcat "  Velocidade diretriz ..... " (gr:r *gr-vel* 0) " km/h"))
  (gr:msg (strcat "  Passo de amostragem ..... " (gr:r *gr-passo* 1) " m"))
  (gr:msg (strcat "  Tolerancia ao terreno ... " (gr:r *gr-tol* 2) " m"))
  (gr:msg (strcat "  Tangente minima ......... " (gr:r *gr-ltmin* 1) " m"))
  (gr:msg (strcat "  L minimo de curva ....... " (gr:r *gr-lmin* 1) " m"))
  (if (= (strcase (getstring "\nZerar para os padroes? [S/N] <N>: ")) "S")
    (progn (setq *gr-imax* 8.0 *gr-imin* 0.5 *gr-vel* 40.0 *gr-passo* 10.0
                 *gr-tol* 0.20 *gr-ltmin* 40.0 *gr-lmin* 0.0 *gr-corda* 20.0)
           (gr:msg "Padroes restaurados.")))
  (princ))

;;; --------------------------------------------------------------------------
;;; GREIDETESTE - diagnostico
;;; --------------------------------------------------------------------------

(defun gr:teste ( / e obj nome profs n i p s0 s1 z lst arq)
  (gr:msg "=== DIAGNOSTICO GREIDEAUTO ===")
  (gr:msg (strcat "AutoCAD ....... " (getvar "ACADVER")))
  (gr:msg (strcat "Desenho ....... " (getvar "DWGNAME")))
  (gr:msg (strcat "Pasta ......... " (getvar "DWGPREFIX")))

  (gr:etapa "[1] selecionando o alinhamento")
  (gr:msg "Selecione o alinhamento:")
  (setq e (entsel "\n> "))
  (if (null e)
    (gr:msg "    FALHOU: nada selecionado.")
    (progn
      (setq obj (vlax-ename->vla-object (car e)))
      (gr:msg (strcat "    ObjectName .... " (vl-princ-to-string (gr:prop obj 'ObjectName))))
      (setq nome (gr:prop obj 'Name))
      (gr:msg (strcat "    Name .......... " (vl-princ-to-string nome)))
      (gr:msg (strcat "    Length ........ " (vl-princ-to-string (gr:prop obj 'Length))))
      (gr:msg (strcat "    StartingStation " (vl-princ-to-string (gr:prop obj 'StartingStation))))
      (gr:msg (strcat "    EndingStation . " (vl-princ-to-string (gr:prop obj 'EndingStation))))

      (gr:etapa "[2] abrindo a colecao de perfis")
      (setq profs (gr:prop obj 'Profiles))
      (gr:msg (strcat "    Profiles ...... " (if profs "OK" "FALHOU")))

      (if profs
        (progn
          (setq n (gr:prop profs 'Count))
          (gr:msg (strcat "    Profiles.Count  " (vl-princ-to-string n)))

          (if (and n (> n 0))
            (progn
              (gr:etapa "[3] lendo cada perfil")
              (setq i 0)
              (while (< i n)
                (setq p (vl-catch-all-apply 'vlax-invoke (list profs 'Item i)))
                (if (vl-catch-all-error-p p)
                  (gr:msg (strcat "    Item " (itoa i) " -> FALHOU (indice pode ser 1-based)"))
                  (progn
                    (gr:msg (strcat "    Item " (itoa i) " -> "
                                    (vl-princ-to-string (gr:prop p 'Name))))
                    (setq s0 (gr:prop p 'StartingStation)
                          s1 (gr:prop p 'EndingStation))
                    (gr:msg (strcat "        StartingStation " (vl-princ-to-string s0)
                                    "  EndingStation " (vl-princ-to-string s1)))
                    (if (and (numberp s0) (numberp s1) (> s1 s0))
                      (progn
                        (setq z (gr:met p 'ElevationAt (/ (+ s0 s1) 2.0)))
                        (gr:msg (strcat "        A) ElevationAt ... "
                                        (if (numberp z) (gr:r z 3) "FALHOU"))))
                      (gr:msg "        A) ElevationAt ... nao testado (estacas nulas)"))
                    (setq lst (gr:le-pvis p))
                    (gr:msg (strcat "        B) PVIs .......... "
                                    (if (and lst (> (length lst) 2))
                                      (strcat (itoa (length lst)) " pontos") "FALHOU")))
                    (setq lst (gr:le-entidades p))
                    (gr:msg (strcat "        C) Entities ...... "
                                    (if (and lst (> (length lst) 2))
                                      (strcat (itoa (length lst)) " pontos") "FALHOU")))))
                (setq i (1+ i)))))))

      (gr:etapa "[4] testando gravacao de arquivo")
      (setq arq (strcat (getvar "DWGPREFIX") "_greide_teste.csv"))
      (if (gr:grava-terreno arq (list (list 0.0 100.0) (list 20.0 101.0)))
        (gr:msg (strcat "    OK -> " arq))
        (gr:msg "    FALHOU: sem permissao de escrita nesta pasta."))))
  (gr:msg "\n=== FIM DO DIAGNOSTICO ===")
  (princ))

(defun c:GREIDETESTE () (gr:roda 'gr:teste))

;;; --------------------------------------------------------------------------
;;; GREIDEEXP - so exporta o terreno
;;; --------------------------------------------------------------------------

(defun gr:exp ( / align ach s0 s1 arq)
  (gr:etapa "selecionando o alinhamento")
  (setq align (gr:pega-alinhamento))
  (if align
    (progn
      (setq s0 (gr:prop align 'StartingStation)
            s1 (gr:prop align 'EndingStation))
      (if (null s0) (setq s0 0.0))
      (if (null s1) (setq s1 (+ s0 (cond ((gr:prop align 'Length)) (1000.0)))))
      (setq *gr-passo* (gr:getreal "Passo de amostragem (m)" *gr-passo*))
      (setq ach (gr:acha-terreno align s0 s1 *gr-passo*))
      (if ach
        (progn
          (setq arq (strcat (getvar "DWGPREFIX")
                            (gr:safe (cond ((gr:prop align 'Name)) ("eixo")))
                            "_terreno.csv"))
          (gr:etapa "gravando o CSV do terreno")
          (if (gr:grava-terreno arq (nth 2 ach))
            (gr:msg (strcat "Terreno exportado (" (itoa (length (nth 2 ach)))
                            " pontos): " arq))
            (gr:msg "*** Falha ao gravar o arquivo."))))))
  (princ))

(defun c:GREIDEEXP () (gr:roda 'gr:exp))

;;; --------------------------------------------------------------------------
;;; GREIDEAUTO - comando principal
;;; --------------------------------------------------------------------------

(defun gr:auto ( / align ach prof pts s0 s1 cota0 imax imin res pvis avisos
                   kconv kconc base arq1 arq2 arq3 nome cmax amax scmax samax
                   d n i p a pior tmin)
  (gr:msg "=== GREIDE AUTOMATICO ===")

  (gr:etapa "selecionando o alinhamento")
  (setq align (gr:pega-alinhamento))

  (if (null align)
    (gr:msg "Comando cancelado.")
    (progn
      (gr:etapa "lendo estacas do alinhamento")
      (setq s0 (gr:prop align 'StartingStation)
            s1 (gr:prop align 'EndingStation))
      (if (null s0) (setq s0 0.0))
      (if (null s1) (setq s1 (+ s0 (cond ((gr:prop align 'Length)) (1000.0)))))

      ;; --- parametros ---------------------------------------------------
      (gr:etapa "perguntando os parametros")
      (setq *gr-corda* (gr:getreal "Corda da estaca (m)" *gr-corda*))
      (setq *gr-passo* (gr:getreal "Passo de amostragem (m)" *gr-passo*))

      ;; --- terreno (escolha automatica do perfil) -----------------------
      (setq ach (gr:acha-terreno align s0 s1 *gr-passo*))

      (if (null ach)
        (progn
          (gr:msg "*** Nao consegui ler nenhum perfil de terreno neste alinhamento.")
          (gr:msg "    Rode GREIDETESTE e me mande o resultado."))
        (progn
          (setq prof (nth 0 ach)
                pts  (nth 2 ach)
                s0   (car (car pts))
                s1   (car (last pts)))
          (gr:msg (strcat "Trecho: est. " (gr:est s0) " a " (gr:est s1)
                          "  (" (gr:r (- s1 s0) 2) " m)"))

          (setq cota0 (cadr (car pts)))
          (setq cota0      (gr:getreal "COTA INICIAL do greide (m)" cota0))
          (setq *gr-imax*  (gr:getreal "RAMPA MAXIMA (%)" *gr-imax*))
          (setq *gr-imin*  (gr:getreal "Rampa minima p/ drenagem (%, 0 = sem minima)" *gr-imin*))
          (setq *gr-vel*   (gr:getreal "Velocidade diretriz (km/h)" *gr-vel*))
          (setq *gr-ltmin* (gr:getreal "Tangente minima entre PIVs (m)" *gr-ltmin*))
          (setq *gr-tol*   (gr:getreal "Tolerancia de desvio ao terreno (m)" *gr-tol*))

          (gr:msg "Tipo de curva vertical:")
          (gr:msg "  1 - Parabolica simetrica, L = K x A (K minimo pela velocidade)")
          (gr:msg "  2 - Parabolica simetrica, K informado manualmente")
          (gr:msg "  3 - Parabolica simetrica, L fixo em todos os PIVs")
          (setq *gr-tipo* (gr:getint "Opcao" *gr-tipo*))

          (cond
            ((= *gr-tipo* 2)
             (setq kconv (gr:getreal "K das curvas CONVEXAS" (gr:kconvexa *gr-vel*))
                   kconc (gr:getreal "K das curvas CONCAVAS" (gr:kconcava *gr-vel*))
                   *gr-lmin* (gr:getreal "Comprimento minimo de curva (m)" *gr-lmin*)))
            ((= *gr-tipo* 3)
             (setq *gr-lmin* (gr:getreal "Comprimento FIXO da curva (m)" (max 40.0 *gr-lmin*))
                   kconv 0.0 kconc 0.0))
            (T
             (setq kconv (gr:kconvexa *gr-vel*)
                   kconc (gr:kconcava *gr-vel*)
                   *gr-lmin* 0.0)
             (gr:msg (strcat "K minimo adotado -> convexa " (gr:r kconv 1)
                             " | concava " (gr:r kconc 1)
                             " | L min (0,6V) " (gr:r (* 0.6 *gr-vel*) 1) " m"))))

          (setq imax (/ (gr:abs *gr-imax*) 100.0)
                imin (/ (gr:abs *gr-imin*) 100.0))
          (if (>= imin imax) (setq imin 0.0))

          ;; --- calculo ---------------------------------------------------
          (gr:etapa (strcat "gerando a poligonal do greide com " (itoa (length pts)) " pontos"))
          (setq res    (gr:gerar pts cota0 imax imin *gr-ltmin* *gr-tol*)
                pvis   (car res)
                avisos (cadr res))

          (gr:etapa "dimensionando as curvas verticais")
          (setq res    (gr:curvas pvis *gr-vel* kconv kconc *gr-lmin*)
                pvis   (car res)
                avisos (append avisos (cadr res)))

          ;; --- arquivos (antes de mexer no desenho) ----------------------
          (gr:etapa "gravando os arquivos de apoio")
          (setq base (strcat (getvar "DWGPREFIX")
                             (gr:safe (cond ((gr:prop align 'Name)) ("eixo"))))
                arq1 (strcat base "_terreno.csv")
                arq2 (strcat base "_greide_pvis.csv")
                arq3 (strcat base "_greide_pvis.txt"))
          (gr:grava-terreno  arq1 pts)
          (gr:grava-pvis-csv arq2 pvis)
          (gr:grava-pvis-txt arq3 pvis)

          ;; --- tabela ----------------------------------------------------
          (gr:etapa "montando a tabela de PIVs")
          (gr:msg "")
          (gr:msg "  #  Estaca         Cota      i_ent%  i_sai%     A%   Tipo      L(m)")
          (setq i 1)
          (foreach p pvis
            (princ (strcat "\n  " (itoa i) "\t" (gr:est (nth 0 p)) "\t" (gr:r (nth 1 p) 3)
                           "\t" (gr:r (* 100.0 (nth 2 p)) 3)
                           "\t" (gr:r (* 100.0 (nth 3 p)) 3)
                           "\t" (gr:r (* 100.0 (- (nth 3 p) (nth 2 p))) 3)
                           "\t" (nth 5 p) "\t" (gr:r (nth 4 p) 2)))
            (setq i (1+ i)))

          ;; --- verificacoes ----------------------------------------------
          (gr:etapa "verificando rampas, tangentes e corte/aterro")
          (setq pior 0.0 tmin 1e12 n (length pvis) i 0)
          (while (< i (1- n))
            (if (> (gr:abs (nth 3 (nth i pvis))) pior)
              (setq pior (gr:abs (nth 3 (nth i pvis)))))
            (setq d (- (nth 0 (nth (1+ i) pvis)) (nth 0 (nth i pvis))))
            (if (< d tmin) (setq tmin d))
            (setq i (1+ i)))

          (setq cmax 0.0 amax 0.0 scmax s0 samax s0)
          (foreach p pts
            (setq d (- (gr:cota pvis (car p)) (cadr p)))
            (if (< d 0.0)
              (if (> (- d) cmax) (setq cmax (- d) scmax (car p)))
              (if (> d amax) (setq amax d samax (car p)))))

          (gr:msg "")
          (gr:msg (strcat "  Rampa maxima utilizada .. " (gr:r (* 100.0 pior) 3)
                          " % (limite " (gr:r *gr-imax* 3) " %) -> "
                          (if (<= pior (+ imax 1e-6)) "OK" "EXCEDE")))
          (gr:msg (strcat "  Menor tangente .......... " (gr:r tmin 2) " m"))
          (gr:msg (strcat "  Corte maximo ............ " (gr:r cmax 3)
                          " m na est. " (gr:est scmax)))
          (gr:msg (strcat "  Aterro maximo ........... " (gr:r amax 3)
                          " m na est. " (gr:est samax)))
          (if avisos
            (progn (gr:msg "  AVISOS:")
                   (foreach a avisos (gr:msg (strcat "   ! " a))))
            (gr:msg "  AVISOS: nenhum."))

          (gr:msg "")
          (gr:msg (strcat "  Terreno .... " arq1 "   -> entrada do greide_auto.py"))
          (gr:msg (strcat "  PIVs (csv) . " arq2))
          (gr:msg (strcat "  PIVs (txt) . " arq3 "   -> Create Profile from File"))

          ;; --- cria o perfil ---------------------------------------------
          (gr:etapa "perguntando se cria o perfil")
          (if (= (strcase (getstring "\nCriar o perfil de projeto no desenho? [S/N] <S>: ")) "N")
            (gr:msg "Perfil nao criado. Use os arquivos gerados.")
            (progn
              (setq nome (getstring T "\nNome do perfil de projeto <GREIDE-AUTO>: "))
              (if (= nome "") (setq nome "GREIDE-AUTO"))
              (gr:etapa "criando o perfil de projeto no desenho")
              (gr:criar-perfil align nome pvis)))

          (gr:msg "=== FIM ===")))))
  (princ))

(defun c:GREIDEAUTO () (gr:roda 'gr:auto))

(gr:msg "GreideAuto.lsp v3 carregado.")
(gr:msg "  GREIDEAUTO  - gera o greide      GREIDETESTE - diagnostico")
(gr:msg "  GREIDEEXP   - exporta o terreno  GREIDECFG   - parametros")
(princ)
