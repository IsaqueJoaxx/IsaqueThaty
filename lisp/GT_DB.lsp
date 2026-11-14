;=====================================================================
; GT_DB.LSP - Banco de dados geometrico de intersecoes
; Fonte: Manual de Projeto de Intersecoes - MT/DNIT/DPP/IPR (IPR 718)
; Tabelas 47, 48, 49, 76 e Apendice A (Projetos de Gotas)
;---------------------------------------------------------------------
; Uso: carregar ANTES do GotaTape.lsp
;   (load "GT_DB.lsp")
;=====================================================================

;---------------------------------------------------------------------
; TABELA 47 - Comprimento do taper nas faixas de mudanca de velocidade
; (velocidade diretriz km/h . comprimento arredondado m)
;---------------------------------------------------------------------
(setq *DB-TAPER-FMV*
 '((40 . 40.0) (50 . 45.0) (60 . 55.0) (70 . 60.0) (80 . 70.0)
   (90 . 80.0) (100 . 85.0) (110 . 90.0) (120 . 100.0)))

;---------------------------------------------------------------------
; TABELA 76 - Comprimento minimo do taper de transicao Z (terminais)
; (velocidade de projeto km/h . comprimento Z m)
;---------------------------------------------------------------------
(setq *DB-TAPER-TRANSICAO*
 '((50 . 15.0) (60 . 20.0) (70 . 23.0) (80 . 25.0) (90 . 28.0)
   (100 . 30.0) (110 . 35.0) (120 . 40.0)))

;---------------------------------------------------------------------
; TABELA 48 - Comprimentos das faixas de mudanca de velocidade (m),
; INCLUSIVE o taper. Greides ate 2%.
; Formato: (Vdiretriz (Taper L0 L20 L30 L40 L50 L60 L70 L80))
;   L0..L80 = comprimento p/ velocidade de seguranca da curva de
;   conversao de 0,20,30,40,50,60,70,80 km/h (nil = nao se aplica)
;---------------------------------------------------------------------
(setq *DB-FAIXA-DESACEL*
 '((40  (40.0  60.0 50.0 40.0 nil  nil  nil  nil  nil))
   (50  (45.0  75.0 70.0 60.0 45.0 nil  nil  nil  nil))
   (60  (55.0  95.0 90.0 80.0 65.0 55.0 nil  nil  nil))
   (70  (60.0  110.0 105.0 95.0 85.0 70.0 60.0 nil  nil))
   (80  (70.0  130.0 125.0 115.0 100.0 90.0 80.0 70.0 nil))
   (90  (80.0  145.0 140.0 135.0 120.0 110.0 100.0 90.0 80.0))
   (100 (85.0  170.0 165.0 155.0 145.0 135.0 120.0 100.0 85.0))
   (110 (90.0  180.0 180.0 170.0 160.0 150.0 140.0 120.0 105.0))
   (120 (100.0 200.0 195.0 185.0 175.0 170.0 155.0 140.0 120.0))))

(setq *DB-FAIXA-ACEL*
 '((40  (40.0  60.0 50.0 40.0 nil  nil  nil  nil  nil))
   (50  (45.0  90.0 70.0 60.0 45.0 nil  nil  nil  nil))
   (60  (55.0  130.0 110.0 100.0 70.0 55.0 nil  nil  nil))
   (70  (60.0  180.0 150.0 140.0 120.0 90.0 60.0 nil  nil))
   (80  (70.0  230.0 210.0 200.0 180.0 140.0 100.0 70.0 nil))
   (90  (80.0  280.0 250.0 240.0 220.0 190.0 140.0 100.0 80.0))
   (100 (85.0  340.0 310.0 290.0 280.0 240.0 200.0 170.0 110.0))
   (110 (90.0  390.0 360.0 350.0 320.0 290.0 250.0 200.0 160.0))
   (120 (100.0 430.0 400.0 390.0 360.0 330.0 290.0 240.0 200.0))))

;---------------------------------------------------------------------
; TABELA 49 - Fatores de ajustamento por greide (multiplicar Tabela 48)
; Desaceleracao: (tipo-rampa . fator)
;---------------------------------------------------------------------
(setq *DB-FATOR-GREIDE-DESACEL*
 '((asc34 . 0.90) (des34 . 1.20) (asc56 . 0.80) (des56 . 1.35)))

; Aceleracao, rampa ascendente 3-4%: (Vdiretriz (f20..f80) f-desc)
(setq *DB-FATOR-GREIDE-ACEL-ASC34*
 '((40  (1.2 1.2 nil nil nil nil nil) 0.70)
   (50  (1.2 1.2 1.2 nil nil nil nil) 0.70)
   (60  (1.3 1.3 1.3 1.4 1.4 nil nil) 0.70)
   (70  (1.3 1.3 1.3 1.4 1.4 1.5 nil) 0.65)
   (80  (1.4 1.4 1.4 1.5 1.5 1.5 1.6) 0.65)
   (90  (1.4 1.4 1.4 1.5 1.5 1.5 1.6) 0.60)
   (100 (1.5 1.5 1.5 1.6 1.7 1.7 1.8) 0.60)
   (110 (1.5 1.5 1.5 1.6 1.7 1.7 1.8) 0.60)
   (120 (1.5 1.5 1.5 1.6 1.7 1.7 1.8) 0.60)))

; Aceleracao, rampa ascendente 5-6%: (Vdiretriz (f20..f80) f-desc)
(setq *DB-FATOR-GREIDE-ACEL-ASC56*
 '((40  (1.3 1.4 nil nil nil nil nil) 0.60)
   (50  (1.3 1.4 1.4 nil nil nil nil) 0.60)
   (60  (1.4 1.5 1.5 1.5 nil nil nil) 0.60)
   (70  (1.4 1.5 1.5 1.6 1.7 nil nil) 0.60)
   (80  (1.4 1.5 1.5 1.7 1.8 1.9 nil) 0.55)
   (90  (1.5 1.6 1.6 1.8 2.0 2.1 2.2) 0.55)
   (100 (1.6 1.7 1.7 1.9 2.2 2.4 2.5) 0.50)
   (110 (1.9 2.0 2.0 2.2 2.6 2.8 3.0) 0.50)
   (120 (2.0 2.1 2.3 2.5 3.0 3.2 3.5) 0.50)))

;---------------------------------------------------------------------
; APENDICE A - Parametros de projeto das gotas (rodovias secundarias)
;---------------------------------------------------------------------
; Gota pequena, angulo entre eixos de 70 a 110 graus
(setq *DB-GOTA-PEQUENA*
 '((angulo-min . 70.0)        ; faixa de aplicacao (graus)
   (angulo-max . 110.0)
   (raio-giro . 12.0)         ; R bordo interno giro a esquerda (m)
   (raio-giro-min . 8.0)      ; R reduzido p/ alfa < 90 (m)
   (raio-ponta . 0.75)        ; R extremidades superior/inferior (m)
   (meia-largura . 1.50)      ; paralelas ao eixo da gota (m)
   (dist-ponta-sup . 10.0)    ; inicio do eixo: dist. ao bordo (m)
   (dist-tangentes . 20.0)    ; ponto das retas tangentes (m)
   (angulo-eixo . 5.5)        ; desvio do eixo da gota (5 a 6 graus)
   (raio-eixo-curvo . 50.0))) ; R min p/ re perpendicular (alfa<70)

; Gota grande, angulo entre eixos de 70 a 110 graus
(setq *DB-GOTA-GRANDE*
 '((faixa-2m . 2.0)          ; faixa central de 2 m
   (raio-ponta . 0.75)        ; R minimo das extremidades (m)
   (largura-min . 1.50)       ; largura minima da gota (m)
   (largura-max . 5.00)       ; largura maxima da gota (m)
   (dist-bordo-min . 2.0)     ; ponta superior ao bordo, min (m)
   (dist-bordo-max . 4.0)     ; ponta superior ao bordo, max (m)
   (dist-tangentes . 40.0)    ; ponto das retas tangentes (m)
   (largura-inferior . 2.50)  ; largura na secao inferior (m)
   (offset-inferior . 1.00)   ; marcar a partir da direita (m)
   (semicirculo . 1.50)       ; diametro semicirculo inferior (m)
   (comprimento-tip . 25.0))) ; comprimento da ordem de (m)

;---------------------------------------------------------------------
; Formula do estreitamento em terminais de entrada (item 9.5.9):
;   F = 15 x (W2 - W1)    [F, W2, W1 em metros]
; Nariz do terminal de saida: 1,20 a 3,60 m do bordo da via principal
;---------------------------------------------------------------------
(setq *DB-NARIZ-TERMINAL* '((min . 1.20) (max . 3.60) (ramo-min . 0.60) (ramo-max . 1.00)))

;---------------------------------------------------------------------
; MANUAL DE PROJETO DE ACESSOS DE AREAS LINDEIRAS (DNIT IPR 728)
; Detalhamento 1 - Gota de canalizacao (pag. 67)
; Igual a gota pequena do Manual de Intersecoes, mas com R = 15 m
; (ampliado de 12 m para ligar a projecao de giro a pista auxiliar).
;---------------------------------------------------------------------
(setq *DB-ACESSO-GOTA*
 '((raio-giro . 15.0)         ; R bordos internos dos giros a esquerda
   (raio-giro-min . 8.0)      ; alfa < 90 graus
   (raio-ponta . 0.75)        ; extremidades superior/inferior
   (meia-largura . 1.50)      ; paralelas ao eixo da gota
   (dist-ponta-sup . 10.0)    ; ponto inicial a 10 m do bordo
   (dist-tangentes . 20.0)    ; ponto das retas tangentes a 20 m
   (angulo-eixo . 5.5)        ; 5 a 6 graus com o eixo secundario
   (pintura-offset . 0.50)    ; sinalizacao afastada 0,50 m da gota
   (raio-pintura-esq . 10.0))) ; curva da linha-limite esquerda

; Faixa de mudanca de velocidade niveis 1 e 2 (solucao ARTESP 2013):
; taper de 15 m + trecho paralelo (mesa) de 15 m
(setq *DB-ACESSO-FMV-N12* '((taper . 15.0) (mesa . 15.0)))
; Niveis 3 e 4: usar *DB-FAIXA-DESACEL* / *DB-FAIXA-ACEL* (Tab. 6 e 7
; do IPR 728, identicas a Tabela 48 do IPR 718)

;=====================================================================
; FUNCOES DE CONSULTA AO BANCO DE DADOS
;=====================================================================

; Interpolacao linear em tabela ((x . y) ...) ordenada por x.
; Fora da faixa: devolve o valor do extremo mais proximo.
(defun gt:interp (tab x / a b x1 y1 x2 y2)
  (setq tab (vl-sort tab (function (lambda (p q) (< (car p) (car q))))))
  (cond
    ((<= x (caar tab)) (cdar tab))
    ((>= x (car (last tab))) (cdr (last tab)))
    (t
      (setq a nil b nil)
      (foreach p tab
        (if (<= (car p) x) (setq a p))
        (if (and (>= (car p) x) (null b)) (setq b p))
      )
      (setq x1 (car a) y1 (cdr a) x2 (car b) y2 (cdr b))
      (if (= x1 x2)
        y1
        (+ y1 (* (- y2 y1) (/ (- x x1) (- x2 x1))))
      )
    )
  )
)

; Taper de faixa de mudanca de velocidade (Tabela 47), com interpolacao
(defun gt:taper-fmv (vel) (gt:interp *DB-TAPER-FMV* vel))

; Taper de transicao de terminal de saida (Tabela 76)
(defun gt:taper-transicao (vel) (gt:interp *DB-TAPER-TRANSICAO* vel))

; Estreitamento de terminal de entrada: F = 15 x (W2 - W1)
(defun gt:estreitamento (w2 w1) (* 15.0 (- w2 w1)))

; Comprimento de faixa de acel/desacel (Tabela 48).
; tipo = 'ACEL ou 'DESACEL ; vel = velocidade diretriz ;
; vcurva = velocidade de seguranca da curva (0 20 30 40 50 60 70 80)
; Retorna (taper comprimento-total) ou nil se nao aplicavel.
(defun gt:faixa-mv (tipo vel vcurva / tab vbase idx row)
  (setq tab (if (eq tipo 'ACEL) *DB-FAIXA-ACEL* *DB-FAIXA-DESACEL*))
  ; velocidade diretriz: usa a linha da tabela imediatamente <= vel
  (foreach r (vl-sort tab (function (lambda (p q) (< (car p) (car q)))))
    (if (<= (car r) vel) (setq row r))
  )
  (if (null row) (setq row (car tab)))
  (setq idx (cond ((<= vcurva 0) 1)
                  ((>= vcurva 80) 8)
                  (t (1+ (fix (/ (+ vcurva 10.0) 20.0))))))
  (list (car (cadr row)) (nth idx (cadr row)))
)

; Fator de greide para faixa de desaceleracao (Tabela 49)
; greide em %, positivo = ascendente no sentido do veiculo
(defun gt:fator-greide-desacel (greide / g)
  (setq g (abs greide))
  (cond
    ((< g 3.0) 1.0)
    ((<= g 4.0) (if (> greide 0) 0.90 1.20))
    (t           (if (> greide 0) 0.80 1.35))
  )
)

; Parametro do banco de gotas: (gt:par 'GOTA-PEQUENA 'raio-giro)
; grupos: GOTA-PEQUENA (IPR 718), GOTA-GRANDE (IPR 718), ACESSO (IPR 728)
(defun gt:par (grupo chave)
  (cdr (assoc chave
    (cond ((eq grupo 'GOTA-PEQUENA) *DB-GOTA-PEQUENA*)
          ((eq grupo 'ACESSO)       *DB-ACESSO-GOTA*)
          (t                        *DB-GOTA-GRANDE*))))
)

(princ "\nGT_DB.LSP carregado - Banco DNIT IPR 718 (Intersecoes) + IPR 728 (Acessos).")
(princ)
