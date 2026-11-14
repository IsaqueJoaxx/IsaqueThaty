;;; ============================================================
;;;  RETORNO_BULBO.lsp
;;;  Desenha praca de retorno tipo BULBO (cul-de-sac)
;;;  Projeto: Trevo Est. 850 - Brumadinho
;;;  Comando: RETORNO
;;; ------------------------------------------------------------
;;;  Geometria gerada:
;;;    - 2 bordos retos de aproximacao
;;;    - 2 raios de concordancia (fillets) tangentes
;;;    - arco maior do bulbo (raio externo R)
;;;  Saida: 1 LWPOLYLINE unica com bulges (pronta para virar
;;;         Feature Line, Alinhamento ou Corredor no Civil 3D)
;;; ============================================================

(defun RB:loc2w (p cen ang / x y)
  (setq x (car p) y (cadr p))
  (list (+ (car  cen) (- (* x (cos ang)) (* y (sin ang))))
        (+ (cadr cen) (+ (* x (sin ang)) (* y (cos ang))))
        0.0))

(defun RB:layer (nm cor)
  (if (not (tblsearch "LAYER" nm))
    (entmake (list '(0 . "LAYER") '(100 . "AcDbSymbolTableRecord")
                   '(100 . "AcDbLayerTableRecord") (cons 2 nm)
                   '(70 . 0) (cons 62 cor) '(6 . "Continuous"))))
  nm)

(defun c:RETORNO ( / *error* osm cmde ecopy
                     cen dirp ang R L w rf Lb
                     alfa xf bF sB bB
                     v0 v1 v2 v3 v4 v5 dat )

  (defun *error* (m)
    (if osm  (setvar "OSMODE" osm))
    (if cmde (setvar "CMDECHO" cmde))
    (if (and m (not (wcmatch m "*cancel*,*quit*")))
      (princ (strcat "\nErro: " m)))
    (princ))

  (setq osm  (getvar "OSMODE")
        cmde (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)

  (princ "\n=== RETORNO EM BULBO ===")

  ;; --- 1. Centro: por selecao de circulo existente ou por ponto
  (initget "Circulo Ponto")
  (setq ecopy (getkword "\nDefinir centro por [Circulo existente/Ponto] <Circulo>: "))
  (if (or (null ecopy) (= ecopy "Circulo"))
    (progn
      (setq ecopy (car (entsel "\nSelecione o CIRCULO de referencia: ")))
      (if (and ecopy (= (cdr (assoc 0 (entget ecopy))) "CIRCLE"))
        (progn
          (setq cen (cdr (assoc 10 (entget ecopy))))
          (princ (strcat "\n  Centro herdado. Raio do circulo = "
                         (rtos (cdr (assoc 40 (entget ecopy))) 2 3) " m")))
        (progn (princ "\n  Nao e um circulo. Informe o ponto.")
               (setq cen (getpoint "\nCentro do bulbo: ")))))
    (setq cen (getpoint "\nCentro do bulbo: ")))
  (if (null cen) (progn (*error* nil) (exit)))
  (setq cen (list (car cen) (cadr cen) 0.0))

  ;; --- 2. Direcao: ponto sobre o eixo, do lado da aproximacao
  (setq dirp (getpoint cen "\nPonto sobre o EIXO da via, no lado de APROXIMACAO: "))
  (if (null dirp) (progn (*error* nil) (exit)))
  (setq ang (angle dirp cen))   ; local +X aponta da via para o centro

  ;; --- 3. Parametros
  (setvar "OSMODE" 0)
  (initget 6)
  (setq R (getdist cen "\nRaio EXTERNO do bulbo (bordo do pavimento) <9.0>: "))
  (if (null R) (setq R 9.0))

  (initget 6)
  (setq L (getdist "\nLARGURA TOTAL da via de acesso <7.0>: "))
  (if (null L) (setq L 7.0))
  (setq w (/ L 2.0))

  (initget 6)
  (setq rf (getdist "\nRaio de CONCORDANCIA (fillet) <3.0>: "))
  (if (null rf) (setq rf 3.0))

  (initget 6)
  (setq Lb (getdist "\nComprimento dos bordos retos de aproximacao <15.0>: "))
  (if (null Lb) (setq Lb 15.0))

  ;; --- 4. Validacao
  (cond
    ((<= R w)
     (princ "\n*** ERRO: raio do bulbo menor/igual a meia-largura da via. ***")
     (*error* nil) (exit))
    ((<= (+ R rf) (+ w rf))
     (princ "\n*** ERRO: geometria impossivel. ***")
     (*error* nil) (exit)))

  ;; --- 5. Geometria (coordenadas locais, centro do bulbo = origem)
  (setq xf   (- (sqrt (- (expt (+ R rf) 2.0) (expt (+ w rf) 2.0))))
        alfa (atan (+ w rf) (- xf))
        sB   (- (* 2.0 pi) (* 2.0 alfa))          ; angulo do arco do bulbo
        bF   (- (/ (sin (/ (- (/ pi 2.0) alfa) 4.0))
                   (cos (/ (- (/ pi 2.0) alfa) 4.0))))   ; bulge fillet (horario)
        bB   (/ (sin (/ sB 4.0)) (cos (/ sB 4.0))))      ; bulge bulbo (anti-horario)

  (setq v0 (list (- Lb) (- w))
        v1 (list xf     (- w))
        v2 (list (* (/ R (+ R rf)) xf) (* (/ R (+ R rf)) (- (+ w rf))))
        v3 (list (* (/ R (+ R rf)) xf) (* (/ R (+ R rf)) (+ w rf)))
        v4 (list xf     w)
        v5 (list (- Lb) w))

  ;; --- 6. Desenho
  (RB:layer "PRJ-RETORNO" 4)
  (setq dat
    (append
      (list '(0 . "LWPOLYLINE")
            '(100 . "AcDbEntity")
            '(8 . "PRJ-RETORNO")
            '(100 . "AcDbPolyline")
            '(90 . 6)
            '(70 . 0))
      (list (cons 10 (RB:loc2w v0 cen ang)) '(42 . 0.0))
      (list (cons 10 (RB:loc2w v1 cen ang)) (cons 42 bF))
      (list (cons 10 (RB:loc2w v2 cen ang)) (cons 42 bB))
      (list (cons 10 (RB:loc2w v3 cen ang)) (cons 42 bF))
      (list (cons 10 (RB:loc2w v4 cen ang)) '(42 . 0.0))
      (list (cons 10 (RB:loc2w v5 cen ang)) '(42 . 0.0))))
  (entmake dat)

  ;; eixo de referencia
  (entmake (list '(0 . "LINE") '(8 . "PRJ-RETORNO") '(62 . 1)
                 (cons 10 (RB:loc2w (list (- Lb) 0.0) cen ang))
                 (cons 11 (RB:loc2w (list R 0.0) cen ang))))

  ;; --- 7. Relatorio
  (princ "\n------------------------------------------")
  (princ (strcat "\n Raio externo do bulbo R  = " (rtos R 2 2) " m"))
  (princ (strcat "\n Largura da via        L  = " (rtos L 2 2) " m"))
  (princ (strcat "\n Raio de concordancia  rf = " (rtos rf 2 2) " m"))
  (princ (strcat "\n Recuo do PC do fillet    = " (rtos (abs xf) 2 3) " m (a partir do centro)"))
  (princ (strcat "\n Angulo do arco do bulbo  = " (rtos (/ (* sB 180.0) pi) 2 2) " graus"))
  (princ (strcat "\n Diametro externo         = " (rtos (* 2.0 R) 2 2) " m"))
  (princ (strcat "\n Area pavimentada aprox.  = "
                 (rtos (+ (* pi R R) (* L Lb)) 2 1) " m2"))
  (princ "\n Polilinha criada na layer PRJ-RETORNO")
  (princ "\n------------------------------------------")

  (setvar "OSMODE" osm)
  (setvar "CMDECHO" cmde)
  (princ))

(princ "\nRETORNO_BULBO.lsp carregado. Digite RETORNO para executar.")
(princ)
