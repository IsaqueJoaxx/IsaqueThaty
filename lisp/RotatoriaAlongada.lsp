;;;============================================================================
;;; ROTATORIA-ALONGADA.LSP
;;; Rotina para criacao de Rotatoria Alongada conforme normas DER/DNIT
;;; 
;;; Entradas:
;;;   - Eixo da rodovia (linha ou polilinha selecionada)
;;;   - Velocidade de projeto (km/h)
;;;   - Comprimento da rotatoria (eixo maior, em metros)
;;;   - Cota de entrada e saida (m)
;;;   - Raios interno e externo (ou calculados automaticamente)
;;;   - Pontos de entrada e saida (selecionados no eixo)
;;;
;;; Cria:
;;;   - Ilha central (elipse)
;;;   - Pista de rolamento (elipse externa)
;;;   - Bermas (se especificado)
;;;   - Marcadores de entrada/saida
;;;   - Cotas e anotacoes
;;;   - Camadas padronizadas
;;;
;;; Autor: Gerado por IA conforme especificacoes tecnicas
;;; Data: 2026-08-26
;;;============================================================================

;;;----------------------------------------------------------------------------
;;; VARIAVEIS GLOBAIS
;;;----------------------------------------------------------------------------

(setq *ROAL-CAMADAS* '(
  ("ROAL-EIXO"        "Continuous"  1   "Eixo da rodovia e referencias")
  ("ROAL-ILHA"        "Continuous"  3   "Ilha central da rotatoria")
  ("ROAL-PISTA"       "Continuous"  2   "Pista de rolamento")
  ("ROAL-BERMA"       "Continuous"  8   "Bermas e acostamentos")
  ("ROAL-MARCADOR"    "Continuous"  4   "Marcadores de entrada/saida")
  ("ROAL-COTA"        "Continuous"  6   "Cotas e elevacoes")
  ("ROAL-TEXTO"       "Continuous"  7   "Textos e anotacoes")
  ("ROAL-HACHURA"     "Continuous"  9   "Hachuras de pavimento")
))

;;;----------------------------------------------------------------------------
;;; UTILITARIOS
;;;----------------------------------------------------------------------------

(defun ROAL-CriarCamadas ()
  "Cria as camadas padronizadas se nao existirem"
  (foreach item *ROAL-CAMADAS*
    (setq nome (nth 0 item)
          tipo (nth 1 item)
          cor  (nth 2 item)
          desc (nth 3 item))
    (if (null (tblsearch "LAYER" nome))
      (entmake (list '(0 . "LAYER")
                     '(100 . "AcDbSymbolTableRecord")
                     '(100 . "AcDbLayerTableRecord")
                     (cons 2 nome)
                     (cons 70 0)
                     (cons 62 cor)
                     (cons 6 tipo)))
    )
  )
  (princ "\nCamadas criadas com sucesso.")
)

(defun ROAL-PontoNoMeio (p1 p2)
  "Retorna o ponto medio entre dois pontos 3D"
  (list (/ (+ (car p1) (car p2)) 2.0)
        (/ (+ (cadr p1) (cadr p2)) 2.0)
        (/ (+ (caddr p1) (caddr p2)) 2.0))
)

(defun ROAL-Distancia2D (p1 p2)
  "Distancia horizontal entre dois pontos"
  (sqrt (+ (expt (- (car p2) (car p1)) 2)
           (expt (- (cadr p2) (cadr p1)) 2)))
)

(defun ROAL-Angulo2D (p1 p2)
  "Angulo horizontal entre dois pontos (rad)"
  (angle p1 p2)
)

(defun ROAL-PontoPolar2D (p ang dist)
  "Ponto a uma distancia e angulo horizontal de p"
  (polar p ang dist)
)

(defun ROAL-Perpendicular (p1 p2 p3)
  "Retorna a projecao perpendicular de p3 sobre a reta p1-p2"
  (setq ang (angle p1 p2))
  (setq ang-perp (+ ang (/ pi 2.0)))
  (setq dist-proj (* (distance p1 p3) (cos (- (angle p1 p3) ang))))
  (polar p1 ang dist-proj)
)

(defun ROAL-EscalarPonto (p fator)
  "Multiplica as coordenadas X,Y de um ponto por um fator"
  (list (* (car p) fator) (* (cadr p) fator) (caddr p))
)

(defun ROAL-Texto (ponto alt texto layer rotacao just)
  "Insere texto no desenho"
  (entmake
    (list
      '(0 . "TEXT")
      '(100 . "AcDbEntity")
      (cons 8 layer)
      '(100 . "AcDbText")
      (cons 10 ponto)
      (cons 40 alt)
      (cons 1 texto)
      (cons 50 (or rotacao 0.0))
      (cons 72 (case just
                 ("C" 1) ("R" 2) ("M" 4) ("MR" 3) ("MC" 2) (t 0)))
      '(100 . "AcDbText")
      (cons 11 ponto)
      (cons 73 (case just
                 ("M" 2) ("TC" 2) ("BC" 1) ("MC" 1) (t 0)))
    )
  )
)

(defun ROAL-CotaRapida (p1 p2 texto layer offset)
  "Cria uma cota alinhada entre dois pontos"
  (setq ang (angle p1 p2))
  (setq mid (ROAL-PontoNoMeio p1 p2))
  (setq pos-texto (polar mid (+ ang (/ pi 2.0)) offset))
  (ROAL-Texto pos-texto 1.5 texto layer 0 "C")
  (entmake (list '(0 . "LINE")
                 '(100 . "AcDbEntity")
                 (cons 8 layer)
                 '(100 . "AcDbLine")
                 (cons 10 (polar p1 (- ang (/ pi 2.0)) 0.5))
                 (cons 11 (polar p1 (+ ang (/ pi 2.0)) 0.5))))
  (entmake (list '(0 . "LINE")
                 '(100 . "AcDbEntity")
                 (cons 8 layer)
                 '(100 . "AcDbLine")
                 (cons 10 (polar p2 (- ang (/ pi 2.0)) 0.5))
                 (cons 11 (polar p2 (+ ang (/ pi 2.0)) 0.5))))
  (entmake (list '(0 . "LINE")
                 '(100 . "AcDbEntity")
                 (cons 8 layer)
                 '(100 . "AcDbLine")
                 (cons 10 p1)
                 (cons 11 p2)))
)

;;;----------------------------------------------------------------------------
;;; CALCULOS TECNICOS - BASEADO EM NORMAS DER/DNIT
;;;----------------------------------------------------------------------------

(defun ROAL-RaioMinimo (velocidade)
  "Retorna o raio minimo recomendado baseado na velocidade de projeto (km/h)
   Fonte: Adaptado de normas DER-SP / DNIT de geometria rodoviaria
   para rotatorias urbanas e rurais"
  (cond
    ;; Velocidade muito baixa - areas de trafego local
    ((< velocidade 20) 6.0)
    ;; Baixa velocidade - areas urbanas densas
    ((< velocidade 30) 8.0)
    ;; Media-baixa - vias urbanas coletoras
    ((< velocidade 40) 10.0)
    ;; Media - vias urbanas principais / vicinais
    ((< velocidade 50) 12.0)
    ;; Media-alta - vias arteriais urbanas / rodovias rurais
    ((< velocidade 60) 15.0)
    ;; Alta velocidade - rodovias (uso limitado para rotatorias)
    ((< velocidade 70) 18.0)
    ;; Muito alta - evitar rotatoria, usar outra solucao
    (T 20.0)
  )
)

(defun ROAL-LarguraPista (velocidade num-faixas)
  "Retorna a largura da pista de rolamento por faixa (m)
   num-faixas: numero de faixas de circulacao na rotatoria"
  (setq larg-faixa
    (cond
      ((< velocidade 30) 3.0)
      ((< velocidade 50) 3.3)
      ((< velocidade 70) 3.5)
      (T 3.6)
    )
  )
  (* larg-faixa num-faixas)
)

(defun ROAL-LarguraBerma (velocidade)
  "Retorna a largura da berma/acostamento (m)"
  (cond
    ((< velocidade 30) 0.50)
    ((< velocidade 50) 0.75)
    (T 1.00)
  )
)

(defun ROAL-RaioExterno (raio-interno larg-pista bermas)
  "Calcula o raio externo da rotatoria"
  (+ raio-interno larg-pista (* 2.0 bermas))
)

(defun ROAL-Superelevacao (velocidade raio)
  "Calcula a superelevacao teorica (%) para o raio e velocidade
   Formula aproximada: e = V^2 / (127 * R) - f
   onde f = atrito lateral (assumido 0.15 para baixas velocidades)"
  (setq f-atrito 0.15)
  (setq e-teorica (- (/ (* velocidade velocidade) (* 127.0 raio)) f-atrito))
  ;; Limitar a valores praticos (max 4% para rotatorias)
  (min 0.04 (max 0.02 e-teorica))
)

(defun ROAL-DiferencaCota (cota-entrada cota-saida comprimento)
  "Calcula a rampa longitudinal entre entrada e saida (%)
   Alerta se exceder limites recomendados"
  (setq rampa (* 100.0 (/ (- cota-saida cota-entrada) comprimento)))
  (if (> (abs rampa) 6.0)
    (princ (strcat "\n*** ATENCAO: Rampa longitudinal de " 
                   (rtos (abs rampa) 2 1) 
                   "% excede o recomendado maximo de 6% para rotatorias. ***")))
  rampa
)

;;;----------------------------------------------------------------------------
;;; GEOMETRIA DA ROTATORIA ALONGADA (ELIPSE)
;;;----------------------------------------------------------------------------

(defun ROAL-CriarElipse (centro eixo-maior eixo-menor rotacao layer tipo)
  "Cria uma elipse (aproximada por polilinha) no AutoCAD
   tipo: 'interna ou 'externa"
  (setq num-pontos 72)  ; 72 segmentos = 5 graus cada
  (setq ang-incremento (/ (* 2.0 pi) num-pontos))
  (setq pontos nil)
  
  ;; Gerar pontos da elipse
  (setq i 0)
  (repeat num-pontos
    (setq ang (* i ang-incremento))
    (setq x (* eixo-maior (cos ang)))
    (setq y (* eixo-menor (sin ang)))
    ;; Rotacionar o ponto
    (setq x-rot (- (* x (cos rotacao)) (* y (sin rotacao))))
    (setq y-rot (+ (* x (sin rotacao)) (* y (cos rotacao))))
    (setq p (list (+ (car centro) x-rot)
                  (+ (cadr centro) y-rot)
                  (caddr centro)))
    (setq pontos (append pontos (list p)))
    (setq i (1+ i))
  )
  
  ;; Criar polilinha fechada (LWPOLYLINE)
  (setq vertices nil)
  (foreach p pontos
    (setq vertices (append vertices (list (list (car p) (cadr p)))))
  )
  
  (setq dados (list
    '(0 . "LWPOLYLINE")
    '(100 . "AcDbEntity")
    (cons 8 layer)
    '(100 . "AcDbPolyline")
    '(90 . 72)         ; numero de vertices
    '(70 . 1)          ; fechada
    '(43 . 0.0)        ; largura constante
  ))
  
  ;; Adicionar vertices
  (foreach v vertices
    (setq dados (append dados (list (cons 10 v))))
  )
  
  (entmake dados)
  (entlast)
)

(defun ROAL-CriarElipseSpline (centro eixo-maior eixo-menor rotacao layer)
  "Cria elipse usando entidade ELLIPSE nativa do AutoCAD (mais precisa)"
  ;; Para ELLIPSE precisamos: centro, eixo maior (vetor), e razao eixo menor/maior
  (setq eixo-maior-vetor (list (* eixo-maior (cos rotacao))
                               (* eixo-maior (sin rotacao))
                               0.0))
  (setq razao (/ eixo-menor eixo-maior))
  
  (entmake (list
    '(0 . "ELLIPSE")
    '(100 . "AcDbEntity")
    (cons 8 layer)
    '(100 . "AcDbEllipse")
    (cons 10 centro)
    (cons 11 eixo-maior-vetor)
    (cons 40 razao)
    '(41 . 0.0)
    '(42 . (* 2.0 pi))
  ))
  (entlast)
)

;;;----------------------------------------------------------------------------
;;; MARCADORES DE ENTRADA E SAIDA
;;;----------------------------------------------------------------------------

(defun ROAL-MarcadorEntradaSaida (ponto angulo tipo velocidade layer)
  "Cria um marcador visual para entrada ou saida na rotatoria
   tipo: 'entrada ou 'saida"
  (setq tamanho (if (= tipo 'entrada) 2.0 -2.0))
  (setq largura-seta 1.0)
  
  ;; Ponta da seta (direcao do fluxo)
  (setq ponta (polar ponto angulo (abs tamanho)))
  
  ;; Base da seta
  (setq base1 (polar ponto (+ angulo (/ pi 2.0)) largura-seta))
  (setq base2 (polar ponto (- angulo (/ pi 2.0)) largura-seta))
  
  ;; Criar seta como polilinha
  (entmake (list
    '(0 . "LWPOLYLINE")
    '(100 . "AcDbEntity")
    (cons 8 layer)
    '(100 . "AcDbPolyline")
    '(90 . 3)
    '(70 . 0)
    (cons 10 (list (car base1) (cadr base1)))
    (cons 10 (list (car ponta) (cadr ponta)))
    (cons 10 (list (car base2) (cadr base2)))
  ))
  
  ;; Texto indicador
  (setq texto (if (= tipo 'entrada) "ENTRADA" "SAIDA"))
  (setq pos-texto (polar ponto (+ angulo (/ pi 2.0)) 2.5))
  (ROAL-Texto pos-texto 1.2 texto layer angulo "MC")
  
  ;; Velocidade de entrada/saida
  (setq pos-vel (polar ponto (- angulo (/ pi 2.0)) 2.5))
  (ROAL-Texto pos-vel 1.0 
    (strcat "V=" (rtos (* 0.6 velocidade) 2 0) "km/h")
    layer angulo "MC")
)

(defun ROAL-Defletor (ponto angulo layer)
  "Cria um defletor/ilha de canalizacao na entrada"
  (setq comp 3.0)
  (setq larg 1.5)
  
  (setq p1 ponto)
  (setq p2 (polar p1 angulo comp))
  (setq p3 (polar p2 (+ angulo (/ pi 2.0)) larg))
  (setq p4 (polar p1 (+ angulo (/ pi 2.0)) larg))
  
  (entmake (list
    '(0 . "LWPOLYLINE")
    '(100 . "AcDbEntity")
    (cons 8 layer)
    '(100 . "AcDbPolyline")
    '(90 . 4)
    '(70 . 1)
    (cons 10 (list (car p1) (cadr p1)))
    (cons 10 (list (car p2) (cadr p2)))
    (cons 10 (list (car p3) (cadr p3)))
    (cons 10 (list (car p4) (cadr p4)))
  ))
  (entlast)
)

;;;----------------------------------------------------------------------------
;;; FUNCAO PRINCIPAL
;;;----------------------------------------------------------------------------

(defun C:ROTALONGADA (/ 
  ;; Variaveis locais
  eixo-ent eixo-dados eixo-tipo eixo-vertices
  pt-inicio pt-fim centro-rotatoria
  velocidade comprimento cota-entrada cota-saida
  raio-interno raio-externo larg-pista larg-berma
  eixo-maior eixo-menor razao-elipse
  pt-entrada pt-saida ang-eixo
  num-faixas superelevacao rampa
  ilha pista-externa berma-externa
  i p1 p2)

  (princ "\n========================================")
  (princ "\n  ROTATORIA ALONGADA - DER/DNIT")
  (princ "\n========================================")
  
  ;; Criar camadas
  (ROAL-CriarCamadas)
  
  ;;==========================================================================
  ;; PASSO 1: SELECIONAR O EIXO DA RODOVIA
  ;;==========================================================================
  (princ "\n\n[1/7] Selecione o eixo da rodovia (linha, arco ou polilinha): ")
  (setq eixo-ent (car (entsel)))
  
  (if (null eixo-ent)
    (progn
      (princ "\n*** Nenhum eixo selecionado. Comando cancelado. ***")
      (exit)
    )
  )
  
  ;; Verificar tipo de entidade
  (setq eixo-dados (entget eixo-ent))
  (setq eixo-tipo (cdr (assoc 0 eixo-dados)))
  
  (if (not (member eixo-tipo '("LINE" "LWPOLYLINE" "POLYLINE" "ARC")))
    (progn
      (princ (strcat "\n*** Tipo de entidade '" eixo-tipo "' nao suportado. Use linha, arco ou polilinha. ***"))
      (exit)
    )
  )
  
  ;; Extrair vertices do eixo
  (cond
    ((= eixo-tipo "LINE")
     (setq pt-inicio (cdr (assoc 10 eixo-dados))
           pt-fim (cdr (assoc 11 eixo-dados))))
    ((= eixo-tipo "ARC")
     ;; Para arco, usar centro e pontos inicial/final
     (setq centro-arc (cdr (assoc 10 eixo-dados))
           raio-arc (cdr (assoc 40 eixo-dados))
           ang-ini (cdr (assoc 50 eixo-dados))
           ang-fim (cdr (assoc 51 eixo-dados))
           pt-inicio (polar centro-arc ang-ini raio-arc)
           pt-fim (polar centro-arc ang-fim raio-arc)))
    (T
     ;; Polilinha - pegar primeiro e ultimo vertices
     (setq eixo-vertices nil)
     (setq subent (entnext eixo-ent))
     (while subent
       (setq subdados (entget subent))
       (if (= (cdr (assoc 0 subdados)) "VERTEX")
         (setq eixo-vertices (append eixo-vertices (list (cdr (assoc 10 subdados))))))
       (setq subent (entnext subent)))
     (if eixo-vertices
       (progn
         (setq pt-inicio (car eixo-vertices))
         (setq pt-fim (last eixo-vertices)))
       (progn
         (princ "\n*** Erro ao extrair vertices da polilinha. ***")
         (exit))))
  )
  
  ;; Calcular centro e angulo do eixo
  (setq centro-rotatoria (ROAL-PontoNoMeio pt-inicio pt-fim))
  (setq ang-eixo (ROAL-Angulo2D pt-inicio pt-fim))
  (setq comp-eixo (ROAL-Distancia2D pt-inicio pt-fim))
  
  (princ (strcat "\n    Eixo identificado: comprimento = " (rtos comp-eixo 2 2) "m"))
  (princ (strcat "\n    Centro aproximado: " (rtos (car centro-rotatoria) 2 2) 
                 ", " (rtos (cadr centro-rotatoria) 2 2)))
  
  ;;==========================================================================
  ;; PASSO 2: VELOCIDADE DE PROJETO
  ;;==========================================================================
  (princ "\n\n[2/7] Velocidade de projeto da via (km/h) [40]: ")
  (setq velocidade (getint))
  (if (null velocidade) (setq velocidade 40))
  
  (if (or (< velocidade 10) (> velocidade 80))
    (progn
      (princ "\n*** Velocidade fora da faixa recomendada (10-80 km/h). Usando 40 km/h. ***")
      (setq velocidade 40)
    )
  )
  
  (princ (strcat "\n    Velocidade de projeto: " (itoa velocidade) " km/h"))
  
  ;;==========================================================================
  ;; PASSO 3: COMPRIMENTO DA ROTATORIA (EIXO MAIOR)
  ;;==========================================================================
  (princ (strcat "\n\n[3/7] Comprimento da rotatoria - eixo maior (m) [" 
                 (rtos (* comp-eixo 1.2) 2 1) "] (minimo recomendado: " 
                 (rtos (* 2.0 (ROAL-RaioMinimo velocidade)) 2 1) "m): "))
  (setq comprimento (getreal))
  
  ;; Sugerir valor padrao baseado no eixo
  (setq comp-sugerido (max (* comp-eixo 1.2) (* 2.0 (ROAL-RaioMinimo velocidade))))
  (if (null comprimento) (setq comprimento comp-sugerido))
  
  ;; Validar comprimento minimo
  (setq comp-minimo (* 2.0 (ROAL-RaioMinimo velocidade)))
  (if (< comprimento comp-minimo)
    (progn
      (princ (strcat "\n*** Comprimento ajustado para o minimo recomendado: " 
                     (rtos comp-minimo 2 1) "m ***"))
      (setq comprimento comp-minimo)
    )
  )
  
  (princ (strcat "\n    Comprimento (eixo maior): " (rtos comprimento 2 2) "m"))
  
  ;;==========================================================================
  ;; PASSO 4: COTAS DE ENTRADA E SAIDA
  ;;==========================================================================
  (princ "\n\n[4/7] Cotas de terreno:")
  (princ "\n    Cota de entrada (m) [100.00]: ")
  (setq cota-entrada (getreal))
  (if (null cota-entrada) (setq cota-entrada 100.0))
  
  (princ "\n    Cota de saida (m) [100.50]: ")
  (setq cota-saida (getreal))
  (if (null cota-saida) (setq cota-saida (+ cota-entrada 0.5)))
  
  (princ (strcat "\n    Cota de entrada: " (rtos cota-entrada 2 3) "m"))
  (princ (strcat "\n    Cota de saida: " (rtos cota-saida 2 3) "m"))
  
  ;; Calcular e verificar rampa
  (setq rampa (ROAL-DiferencaCota cota-entrada cota-saida comprimento))
  (princ (strcat "\n    Rampa longitudinal: " (rtos rampa 2 2) "%"))
  
  ;;==========================================================================
  ;; PASSO 5: NUMERO DE FAIXAS E PARAMETROS GEOMETRICOS
  ;;==========================================================================
  (princ "\n\n[5/7] Numero de faixas de circulacao na rotatoria [1]: ")
  (setq num-faixas (getint))
  (if (null num-faixas) (setq num-faixas 1))
  (if (< num-faixas 1) (setq num-faixas 1))
  (if (> num-faixas 3) 
    (progn
      (princ "\n*** Maximo 3 faixas recomendado para rotatorias. Usando 3. ***")
      (setq num-faixas 3)
    )
  )
  
  ;; Calcular larguras
  (setq larg-pista (ROAL-LarguraPista velocidade num-faixas))
  (setq larg-berma (ROAL-LarguraBerma velocidade))
  
  (princ (strcat "\n    Numero de faixas: " (itoa num-faixas)))
  (princ (strcat "\n    Largura da pista: " (rtos larg-pista 2 2) "m"))
  (princ (strcat "\n    Largura da berma: " (rtos larg-berma 2 2) "m"))
  
  ;;==========================================================================
  ;; PASSO 6: RAIOS
  ;;==========================================================================
  (princ "\n\n[6/7] Raios da rotatoria alongada:")
  
  ;; Raio interno (semi-eixo menor da ilha central)
  (setq raio-min (ROAL-RaioMinimo velocidade))
  (princ (strcat "\n    Raio interno da ilha central (m) [" (rtos raio-min 2 1) "]: "))
  (setq raio-interno (getreal))
  (if (null raio-interno) (setq raio-interno raio-min))
  (if (< raio-interno raio-min)
    (progn
      (princ (strcat "\n*** Raio ajustado para minimo recomendado (" 
                     (rtos raio-min 2 1) "m) ***"))
      (setq raio-interno raio-min)
    )
  )
  
  ;; Raio externo = raio interno + pista + bermas
  (setq raio-externo (ROAL-RaioExterno raio-interno larg-pista larg-berma))
  
  ;; Para rotatoria alongada: eixo maior = comprimento/2, eixo menor = raio
  (setq semi-eixo-maior (/ comprimento 2.0))
  (setq semi-eixo-menor-int raio-interno)
  (setq semi-eixo-menor-ext raio-externo)
  
  ;; Razao de alongamento
  (setq razao-along (/ semi-eixo-maior semi-eixo-menor-int))
  
  (princ (strcat "\n    Raio interno (semi-eixo menor ilha): " (rtos semi-eixo-menor-int 2 2) "m"))
  (princ (strcat "\n    Raio externo calculado: " (rtos semi-eixo-menor-ext 2 2) "m"))
  (princ (strcat "\n    Semi-eixo maior: " (rtos semi-eixo-maior 2 2) "m"))
  (princ (strcat "\n    Razao de alongamento: " (rtos razao-along 2 2)))
  
  ;; Alerta para razoes extremas
  (if (> razao-along 3.0)
    (princ "\n*** ATENCAO: Razao de alongamento > 3 pode comprometer a operacao. Considere outra solucao. ***"))
  
  ;; Calcular superelevacao
  (setq superelevacao (ROAL-Superelevacao velocidade raio-interno))
  (princ (strcat "\n    Superelevacao teorica: " (rtos (* 100.0 superelevacao) 2 2) "%"))
  
  ;;==========================================================================
  ;; PASSO 7: PONTOS DE ENTRADA E SAIDA
  ;;==========================================================================
  (princ "\n\n[7/7] Pontos de entrada e saida:")
  (princ "\n    Selecione o ponto de ENTRADA na rotatoria (ou ENTER para automatico): ")
  (setq pt-entrada (getpoint centro-rotatoria))
  (if (null pt-entrada)
    (setq pt-entrada (polar centro-rotatoria (+ ang-eixo pi) semi-eixo-maior)))
  
  (princ "\n    Selecione o ponto de SAIDA na rotatoria (ou ENTER para automatico): ")
  (setq pt-saida (getpoint centro-rotatoria))
  (if (null pt-saida)
    (setq pt-saida (polar centro-rotatoria ang-eixo semi-eixo-maior)))
  
  ;; Recalcular angulo baseado nos pontos reais
  (setq ang-eixo (ROAL-Angulo2D pt-entrada pt-saida))
  (setq centro-rotatoria (ROAL-PontoNoMeio pt-entrada pt-saida))
  
  (princ (strcat "\n    Ponto de entrada: " (rtos (car pt-entrada) 2 2) ", " (rtos (cadr pt-entrada) 2 2)))
  (princ (strcat "\n    Ponto de saida: " (rtos (car pt-saida) 2 2) ", " (rtos (cadr pt-saida) 2 2)))
  
  ;;==========================================================================
  ;; CRIAR A ROTATORIA ALONGADA
  ;;==========================================================================
  (princ "\n\n--- Criando geometria da rotatoria alongada... ---")
  
  ;; Ajustar centro para a cota media
  (setq cota-media (/ (+ cota-entrada cota-saida) 2.0))
  (setq centro-3d (list (car centro-rotatoria) (cadr centro-rotatoria) cota-media))
  
  ;; --- ILHA CENTRAL (elipse interna) ---
  (princ "\n  Criando ilha central...")
  (setq ilha (ROAL-CriarElipseSpline centro-3d semi-eixo-maior semi-eixo-menor-int ang-eixo "ROAL-ILHA"))
  
  ;; --- PISTA DE ROLAMENTO (elipse externa - raio externo no semi-eixo menor) ---
  (princ "\n  Criando pista de rolamento...")
  ;; Para pista alongada: eixo maior aumenta pela largura da pista
  (setq semi-eixo-maior-pista (+ semi-eixo-maior larg-pista larg-berma))
  (setq semi-eixo-menor-pista semi-eixo-menor-ext)
  (setq pista-externa (ROAL-CriarElipseSpline centro-3d semi-eixo-maior-pista semi-eixo-menor-pista ang-eixo "ROAL-PISTA"))
  
  ;; --- BERMA EXTERNA (se largura > 0) ---
  (if (> larg-berma 0.0)
    (progn
      (princ "\n  Criando berma externa...")
      (setq semi-eixo-maior-berma (+ semi-eixo-maior-pista larg-berma))
      (setq semi-eixo-menor-berma (+ semi-eixo-menor-pista larg-berma))
      (setq berma-externa (ROAL-CriarElipseSpline centro-3d semi-eixo-maior-berma semi-eixo-menor-berma ang-eixo "ROAL-BERMA"))
    )
  )
  
  ;; --- MARCADORES DE ENTRADA E SAIDA ---
  (princ "\n  Criando marcadores de entrada e saida...")
  (ROAL-MarcadorEntradaSaida pt-entrada ang-eixo 'entrada velocidade "ROAL-MARCADOR")
  (ROAL-MarcadorEntradaSaida pt-saida (+ ang-eixo pi) 'saida velocidade "ROAL-MARCADOR")
  
  ;; --- ANOTACOES TECNICAS ---
  (princ "\n  Criando anotacoes tecnicas...")
  
  ;; Titulo
  (ROAL-Texto (polar centro-3d (+ ang-eixo (/ pi 2.0)) (+ semi-eixo-menor-pista 4.0))
              2.0
              "ROTATORIA ALONGADA"
              "ROAL-TEXTO"
              ang-eixo
              "BC")
  
  ;; Dados tecnicos
  (setq pos-info (polar centro-3d (- ang-eixo (/ pi 2.0)) (+ semi-eixo-menor-pista 6.0)))
  (ROAL-Texto pos-info 1.2 
    (strcat "V= " (itoa velocidade) " km/h  |  "
            "L= " (rtos comprimento 2 1) "m  |  "
            "Rint= " (rtos raio-interno 2 1) "m")
    "ROAL-TEXTO" ang-eixo "TC")
  
  (setq pos-info2 (polar pos-info (- ang-eixo (/ pi 2.0)) 2.5))
  (ROAL-Texto pos-info2 1.2
    (strcat "Pista= " (rtos larg-pista 2 1) "m  |  "
            "Berma= " (rtos larg-berma 2 1) "m  |  "
            "e= " (rtos (* 100.0 superelevacao) 2 1) "%")
    "ROAL-TEXTO" ang-eixo "TC")
  
  ;; Cotas de entrada e saida
  (setq pos-cota-ent (polar pt-entrada (- ang-eixo (/ pi 2.0)) 5.0))
  (setq pos-cota-sai (polar pt-saida (- ang-eixo (/ pi 2.0)) 5.0))
  
  (ROAL-Texto pos-cota-ent 1.5
    (strcat "Cota Entrada: " (rtos cota-entrada 2 3) "m")
    "ROAL-COTA" ang-eixo "MC")
  
  (ROAL-Texto pos-cota-sai 1.5
    (strcat "Cota Saida: " (rtos cota-saida 2 3) "m")
    "ROAL-COTA" ang-eixo "MC")
  
  ;; Rampa
  (setq pos-rampa (polar centro-3d (- ang-eixo (/ pi 2.0)) (+ semi-eixo-menor-pista 10.0)))
  (ROAL-Texto pos-rampa 1.2
    (strcat "Rampa Longitudinal: " (rtos rampa 2 2) "%")
    "ROAL-COTA" ang-eixo "TC")
  
  ;; Dimensao do comprimento
  (setq pt-dim1 (polar centro-3d (+ ang-eixo pi) semi-eixo-maior-pista))
  (setq pt-dim2 (polar centro-3d ang-eixo semi-eixo-maior-pista))
  (ROAL-CotaRapida pt-dim1 pt-dim2 
    (strcat "L= " (rtos comprimento 2 1) "m")
    "ROAL-COTA"
    (+ semi-eixo-menor-pista 2.0))
  
  ;; --- REFORCAR EIXO VISUAL ---
  ;; Linha do eixo principal com linha tracejada ou mais grossa
  (entmake (list
    '(0 . "LINE")
    '(100 . "AcDbEntity")
    '(8 . "ROAL-EIXO")
    '(100 . "AcDbLine")
    (cons 10 (list (car pt-entrada) (cadr pt-entrada) cota-media))
    (cons 11 (list (car pt-saida) (cadr pt-saida) cota-media))))
  
  ;;==========================================================================
  ;; RELATORIO FINAL
  ;;==========================================================================
  (princ "\n\n========================================")
  (princ "\n  ROTATORIA ALONGADA CRIADA COM SUCESSO")
  (princ "\n========================================")
  (princ (strcat "\n  Centro: " (rtos (car centro-3d) 2 3) ", " (rtos (cadr centro-3d) 2 3)))
  (princ (strcat "\n  Comprimento (eixo maior): " (rtos comprimento 2 2) "m"))
  (princ (strcat "\n  Raio interno (semi-eixo menor ilha): " (rtos raio-interno 2 2) "m"))
  (princ (strcat "\n  Raio externo (semi-eixo menor pista): " (rtos semi-eixo-menor-ext 2 2) "m"))
  (princ (strcat "\n  Largura da pista: " (rtos larg-pista 2 2) "m"))
  (princ (strcat "\n  Largura da berma: " (rtos larg-berma 2 2) "m"))
  (princ (strcat "\n  Cota media (Z): " (rtos cota-media 2 3) "m"))
  (princ (strcat "\n  Superelevacao: " (rtos (* 100.0 superelevacao) 2 2) "%"))
  (princ (strcat "\n  Rampa longitudinal: " (rtos rampa 2 2) "%"))
  (princ (strcat "\n  Numero de faixas: " (itoa num-faixas)))
  (princ "\n========================================\n")
  
  (princ)
)

;;;----------------------------------------------------------------------------
;;; COMANDO ALTERNATIVO SIMPLIFICADO
;;;----------------------------------------------------------------------------

(defun C:ROASIMPLES (/ velocidade raio-interno comprimento pt-centro)
  "Cria uma rotatoria alongada simplificada com menos parametros"
  (princ "\n=== ROTATORIA ALONGADA - MODO SIMPLES ===")
  
  (ROAL-CriarCamadas)
  
  (princ "\nVelocidade de projeto (km/h) [40]: ")
  (setq velocidade (getint))
  (if (null velocidade) (setq velocidade 40))
  
  (princ "\nRaio interno da ilha (m) [10]: ")
  (setq raio-interno (getreal))
  (if (null raio-interno) (setq raio-interno 10.0))
  
  (princ "\nComprimento da rotatoria - eixo maior (m) [30]: ")
  (setq comprimento (getreal))
  (if (null comprimento) (setq comprimento 30.0))
  
  (princ "\nPonto central da rotatoria: ")
  (setq pt-centro (getpoint))
  (if (null pt-centro) (setq pt-centro '(0.0 0.0 0.0)))
  
  (princ "\nAngulo de orientacao (graus) [0]: ")
  (setq ang-graus (getreal))
  (if (null ang-graus) (setq ang-graus 0.0))
  (setq angulo (* ang-graus (/ pi 180.0)))
  
  ;; Criar elipses
  (setq semi-maior (/ comprimento 2.0))
  (setq semi-menor raio-interno)
  (setq larg-pista 3.5)
  (setq larg-berma 0.75)
  
  ;; Ilha
  (ROAL-CriarElipseSpline pt-centro semi-maior semi-menor angulo "ROAL-ILHA")
  
  ;; Pista
  (ROAL-CriarElipseSpline pt-centro (+ semi-maior larg-pista larg-berma) 
                          (+ semi-menor larg-pista larg-berma) angulo "ROAL-PISTA")
  
  ;; Texto
  (ROAL-Texto (polar pt-centro (/ pi 2.0) (+ semi-menor larg-pista larg-berma 2.0))
              1.5
              (strcat "ROT. ALONGADA V=" (itoa velocidade) "km/h")
              "ROAL-TEXTO" 0 "BC")
  
  (princ (strcat "\nRotatoria alongada criada: " (rtos comprimento 2 1) " x " (rtos (* 2.0 raio-interno) 2 1) "m"))
  (princ)
)

;;;----------------------------------------------------------------------------
;;; CARREGAMENTO
;;;----------------------------------------------------------------------------

(princ "\nRotatoria Alongada carregada.")
(princ "\nComandos disponiveis:")
(princ "\n  ROTALONGADA - Criacao completa com todos os parametros")
(princ "\n  ROASIMPLES  - Criacao simplificada (rapida)")
(princ "\n")

;;; FIM DO ARQUIVO
