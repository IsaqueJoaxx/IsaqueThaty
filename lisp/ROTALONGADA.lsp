;;; ===========================================================================
;;;  ROTALONGADA.LSP
;;;  Geracao automatica de ROTATORIA ALONGADA (rotula convencional / rotatoria
;;;  nao vazada) sobre o eixo de uma rodovia.
;;;
;;;  Referencias adotadas:
;;;    - DNIT / IPR-718 (2005) - Manual de Projeto de Intersecoes
;;;        * velocidade de projeto da pista rotatoria x velocidade da rodovia
;;;        * comprimento minimo do trecho de entrecruzamento
;;;        * superelevacao dos ramos
;;;        * distancia de visibilidade minima (180 m da ilha de aproximacao)
;;;        * largura de faixa de acomodacao / giro a esquerda: 3,00 a 6,00 m
;;;        * retorno em "U" tipo A - largura minima de canteiro por veiculo
;;;    - DER-SP - Manual de Seguranca Viaria (cap. 9) e Manual de Sinalizacao
;;;        Rodoviaria, Projeto-tipo 22 (Intersecao em Nivel - Rotatoria Alongada)
;;;        * o dispositivo opera como DOIS RETORNOS ASSOCIADOS
;;;        * faixas duplas no trecho entre os retornos (entrelacamento)
;;;    - DER/MG (2013) - "rotatoria nao vazada": indicada para VMD de 1.000 a
;;;        1.500 veic/dia no eixo, atendendo acessos de um ou ambos os lados
;;;    - CTB, art. 29, III, "b" - preferencia de quem circula (uso urbano)
;;;
;;;  IMPORTANTE: as verificacoes emitidas pela rotina sao ADVISORIAS. Elas
;;;  conferem os valores tabelados, nao substituem estudo de trafego, de
;;;  visibilidade nem a analise de capacidade / nivel de servico.
;;;
;;;  Comando principal : ROTALONGADA   (alias RTA)
;;;  Comando auxiliar  : RTANORMAS     (imprime as tabelas adotadas)
;;;
;;;  Codigo em ASCII puro (sem acentos) para evitar problemas de codificacao.
;;;  Testado logicamente; rode primeiro em desenho de teste.
;;; ===========================================================================

(vl-load-com)

;;; ---------------------------------------------------------------------------
;;; 1. UTILITARIOS DE VETOR / PONTO (tudo em 2D)
;;; ---------------------------------------------------------------------------
(defun rta:2d  (p) (list (car p) (cadr p)))
(defun rta:v+  (a b) (list (+ (car a) (car b)) (+ (cadr a) (cadr b))))
(defun rta:v-  (a b) (list (- (car a) (car b)) (- (cadr a) (cadr b))))
(defun rta:v*  (a k) (list (* (car a) k) (* (cadr a) k)))
(defun rta:mod (a)   (sqrt (+ (* (car a) (car a)) (* (cadr a) (cadr a)))))
(defun rta:un  (a / m) (setq m (rta:mod a))
  (if (> m 1e-9) (rta:v* a (/ 1.0 m)) (list 1.0 0.0)))
(defun rta:perp (a) (list (- (cadr a)) (car a)))   ; gira +90 graus (esquerda)
(defun rta:dot (a b) (+ (* (car a) (car b)) (* (cadr a) (cadr b))))
(defun rta:crs (a b) (- (* (car a) (cadr b)) (* (cadr a) (car b))))

(defun rta:nang (a)
  (while (<  a 0.0)       (setq a (+ a (* 2.0 pi))))
  (while (>= a (* 2.0 pi))(setq a (- a (* 2.0 pi))))
  a)

;; projecao ortogonal do ponto p sobre a reta (po, dir unitario du)
(defun rta:proj (p po du) (rta:v+ po (rta:v* du (rta:dot (rta:v- p po) du))))

;;; ---------------------------------------------------------------------------
;;; 2. CRIACAO DE ENTIDADES
;;; ---------------------------------------------------------------------------
(defun rta:layer (nom cor)
  (if (not (tblsearch "LAYER" nom))
    (entmakex (list '(0 . "LAYER")
                    '(100 . "AcDbSymbolTableRecord")
                    '(100 . "AcDbLayerTableRecord")
                    (cons 2 nom) (cons 70 0) (cons 62 cor)
                    '(6 . "Continuous"))))
  nom)

;; LWPOLYLINE com bulges (pts e bulges com o mesmo comprimento)
(defun rta:lw (pts bulges fechada lay / d i b)
  (setq d (list '(0 . "LWPOLYLINE") '(100 . "AcDbEntity") (cons 8 lay)
                '(100 . "AcDbPolyline") (cons 90 (length pts))
                (cons 70 (if fechada 1 0)))
        i 0)
  (foreach p pts
    (setq b (if bulges (nth i bulges) 0.0))
    (setq d (append d (list (cons 10 (rta:2d p)))))
    (if (/= b 0.0) (setq d (append d (list (cons 42 b)))))
    (setq i (1+ i)))
  (entmakex d))

;; "obround" (retangulo com as duas pontas em semicirculo) = geometria da
;; ilha central alongada e de qualquer offset dela. bulge 1.0 = arco de 180.
(defun rta:obround (c1 c2 r lay / u n p1 p2 p3 p4)
  (setq u  (rta:un (rta:v- c2 c1))
        n  (rta:perp u)
        p1 (rta:v+ c1 (rta:v* n (- r)))
        p2 (rta:v+ c2 (rta:v* n (- r)))
        p3 (rta:v+ c2 (rta:v* n r))
        p4 (rta:v+ c1 (rta:v* n r)))
  (rta:lw (list p1 p2 p3 p4) (list 0.0 1.0 0.0 1.0) T lay))

(defun rta:line (p1 p2 lay)
  (entmakex (list '(0 . "LINE") '(100 . "AcDbEntity") (cons 8 lay)
                  '(100 . "AcDbLine")
                  (cons 10 (list (car p1) (cadr p1) 0.0))
                  (cons 11 (list (car p2) (cadr p2) 0.0)))))

(defun rta:arc (c r a1 a2 lay)
  (entmakex (list '(0 . "ARC") '(100 . "AcDbEntity") (cons 8 lay)
                  '(100 . "AcDbCircle")
                  (cons 10 (list (car c) (cadr c) 0.0)) (cons 40 r)
                  '(100 . "AcDbArc") (cons 50 a1) (cons 51 a2))))

;; arco menor (< 180) entre dois pontos de tangencia
(defun rta:arc2p (c tp1 tp2 lay / r a1 a2 dif)
  (setq r   (distance c tp1)
        a1  (angle c tp1)
        a2  (angle c tp2)
        dif (rta:nang (- a2 a1)))
  (if (> dif pi) (rta:arc c r a2 a1 lay) (rta:arc c r a1 a2 lay)))

(defun rta:text (p h str lay rot)
  (entmakex (list '(0 . "TEXT") '(100 . "AcDbEntity") (cons 8 lay)
                  '(100 . "AcDbText")
                  (cons 10 (list (car p) (cadr p) 0.0))
                  (cons 40 h) (cons 1 str) (cons 50 (if rot rot 0.0)))))

(defun rta:3dpoly (pts lay)
  (entmake (list '(0 . "POLYLINE") '(100 . "AcDbEntity") (cons 8 lay)
                 '(100 . "AcDb3dPolyline") '(66 . 1) '(70 . 8)
                 '(10 0.0 0.0 0.0)))
  (foreach p pts
    (entmake (list '(0 . "VERTEX") '(100 . "AcDbEntity") (cons 8 lay)
                   '(100 . "AcDbVertex") '(100 . "AcDb3dPolylineVertex")
                   (cons 10 p) '(70 . 32))))
  (entmake (list '(0 . "SEQEND") '(100 . "AcDbEntity") (cons 8 lay))))

;;; ---------------------------------------------------------------------------
;;; 3. CONCORDANCIA (arco tangente a duas retas) - usado nas entradas/saidas
;;;    p1/d1 e p2/d2 : ponto e direcao unitaria de cada reta (retas infinitas)
;;;    r             : raio da concordancia
;;;    hint          : ponto dentro do quadrante onde o arco deve ficar
;;;    retorna (centro tang1 tang2) ou nil
;;; ---------------------------------------------------------------------------
(defun rta:fillet (p1 d1 p2 d2 r hint / n1 n2 c best dmin d tp1 tp2)
  (setq n1 (rta:perp d1) n2 (rta:perp d2) dmin 1e99)
  (foreach s1 (list r (- r))
    (foreach s2 (list r (- r))
      (setq c (inters (rta:v+ p1 (rta:v* n1 s1))
                      (rta:v+ (rta:v+ p1 (rta:v* n1 s1)) d1)
                      (rta:v+ p2 (rta:v* n2 s2))
                      (rta:v+ (rta:v+ p2 (rta:v* n2 s2)) d2)
                      nil))
      (if c
        (progn (setq d (distance c hint))
               (if (< d dmin) (setq dmin d best c))))))
  (if best
    (progn (setq tp1 (rta:proj best p1 d1)
                 tp2 (rta:proj best p2 d2))
           (list best tp1 tp2))))

;;; ---------------------------------------------------------------------------
;;; 4. TABELAS NORMATIVAS - DNIT IPR-718 (2005)
;;; ---------------------------------------------------------------------------
;; velocidade de projeto da pista rotatoria: (minima desejavel)
(defun rta:vel-rot (v)
  (cond ((<= v 57.0) (list 30.0 50.0))     ; rodovia 50 km/h
        ((<= v 72.0) (list 50.0 55.0))     ; rodovia 65 km/h
        (t           (list 55.0 65.0))))   ; rodovia >= 80 km/h

;; comprimento minimo do trecho de entrecruzamento (m) x velocidade da rotatoria
(defun rta:entre-min (vr)
  (cond ((<= vr 30.0) 35.0)
        ((<= vr 40.0) 45.0)
        ((<= vr 50.0) 55.0)
        ((<= vr 60.0) 65.0)
        (t            75.0)))

;; superelevacao recomendada nos ramos
(defun rta:superelev (vr)
  (cond ((<= vr 30.0) "7% a 8%")
        ((<= vr 50.0) "6% a 7%")
        (t            "5% a 6%")))

;; retorno em "U" tipo A - largura minima de canteiro (m) por veiculo de projeto
(defun rta:w-retorno (cod)
  (cond ((= cod "VP") 16.0)    ; veiculo de passeio      (5,80 m)
        ((= cod "CO") 26.0)    ; caminhao / onibus rigido(9,10 m)
        ((= cod "O")  28.0)    ; onibus longo            (12,20 m)
        ((= cod "SR") 29.0)    ; semirreboque            (16,80 m)
        (t            29.0)))

(defun rta:nome-veic (cod)
  (cond ((= cod "VP") "VP - veiculo de passeio")
        ((= cod "CO") "CO - caminhao/onibus rigido")
        ((= cod "O")  "O  - onibus longo")
        (t            "SR - semirreboque")))

(defun C:RTANORMAS ()
  (princ "\n=== ROTATORIA ALONGADA - PARAMETROS ADOTADOS (DNIT IPR-718/2005) ===")
  (princ "\n Vel. rodovia -> vel. pista rotatoria (min/desejavel):")
  (princ "\n   50 km/h  -> 30 / 50      65 km/h -> 50 / 55      >=80 km/h -> 55 / 65")
  (princ "\n Comprimento minimo de entrecruzamento:")
  (princ "\n   30 km/h=35 m | 40=45 m | 50=55 m | 60=65 m | 65=75 m")
  (princ "\n Superelevacao dos ramos:")
  (princ "\n   ate 30 km/h = 7-8% | 40 a 50 = 6-7% | 55 a 65 = 5-6%")
  (princ "\n Visibilidade minima: 180 m da ilha de aproximacao (ou maior).")
  (princ "\n Faixa de acomodacao / giro a esquerda: 3,00 a 6,00 m.")
  (princ "\n Retorno tipo A - largura min. de canteiro: VP=16 CO=26 O=28 SR=29 m.")
  (princ "\n====================================================================")
  (princ))

;;; ---------------------------------------------------------------------------
;;; 5. ENTRADA DE DADOS
;;; ---------------------------------------------------------------------------
(defun rta:getr (msg def / v)
  (setq v (getreal (strcat "\n" msg " <" (rtos def 2 2) ">: ")))
  (if v v def))

(defun rta:getk (msg lst def / v)
  (initget (strcat "" lst))
  (setq v (getkword (strcat "\n" msg " [" lst "] <" def ">: ")))
  (if v v def))

(defun rta:fmt (v n) (rtos v 2 n))

;;; ---------------------------------------------------------------------------
;;; 6. COMANDO PRINCIPAL
;;; ---------------------------------------------------------------------------
(defun C:ROTALONGADA (/ *error* osm cme cly
                        ent obj pe ps pce pcs de ds db pb par u n
                        vel vrot vmin vdes lent lreq
                        lcomp rilha wanel wgalg rent rsai lacess
                        cote cots ivert veic wnec wdisp
                        c1 c2 lst txt htxt pq y
                        ta1 ta2 lw1 lw2 lwmin msg1 msg2 msg3
                        ptsz z1 z2 s p l)

  (defun *error* (m)
    (if osm (setvar "OSMODE" osm))
    (if cme (setvar "CMDECHO" cme))
    (if (and m (/= m "Function cancelled") (/= m "quit / exit abort"))
      (princ (strcat "\nErro: " m)))
    (princ))

  (setq osm (getvar "OSMODE") cme (getvar "CMDECHO") cly (getvar "CLAYER"))
  (setvar "CMDECHO" 0)

  ;; --- camadas -------------------------------------------------------------
  (rta:layer "ROT-EIXO"      4)
  (rta:layer "ROT-ILHA"      3)
  (rta:layer "ROT-GALGAVEL"  2)
  (rta:layer "ROT-ANEL"      1)
  (rta:layer "ROT-ACESSO"    5)
  (rta:layer "ROT-NARIZ"     6)
  (rta:layer "ROT-COTAS"     7)
  (rta:layer "ROT-QUADRO"    7)

  (princ "\n--- ROTATORIA ALONGADA (rotula convencional / nao vazada) ---")

  ;; --- 6.1 eixo da rodovia -------------------------------------------------
  (setq ent nil)
  (while (not ent)
    (setq ent (car (entsel "\nSelecione o EIXO da rodovia (linha/polilinha/alinhamento): ")))
    (if (and ent (not (vl-catch-all-error-p
                        (vl-catch-all-apply 'vlax-curve-getEndParam (list ent)))))
      nil
      (progn (princ "\nEntidade invalida - selecione uma curva (o sentido do eixo = sentido do trafego principal).")
             (setq ent nil))))

  ;; --- 6.2 dados de projeto ------------------------------------------------
  (setq vel   (rta:getr "Velocidade diretriz da rodovia (km/h)"
                        (if *rta-vel* *rta-vel* 60.0))
        *rta-vel* vel)
  (setq lst   (rta:vel-rot vel)
        vmin  (car lst)
        vdes  (cadr lst))
  (princ (strcat "\n  > IPR-718: pista rotatoria com V min = " (rta:fmt vmin 0)
                 " km/h e V desejavel = " (rta:fmt vdes 0) " km/h"))
  (setq vrot  (rta:getr "Velocidade de projeto da PISTA ROTATORIA (km/h)" vdes))
  (setq lreq  (rta:entre-min vrot))
  (princ (strcat "\n  > entrecruzamento minimo para " (rta:fmt vrot 0)
                 " km/h = " (rta:fmt lreq 2) " m"))

  (setq veic  (rta:getk "Veiculo de projeto do retorno" "VP CO O SR"
                        (if *rta-veic* *rta-veic* "SR"))
        *rta-veic* veic
        wnec  (rta:w-retorno veic))

  (setq lcomp (rta:getr "Comprimento TOTAL da rotatoria (m)"
                        (if *rta-comp* *rta-comp* 160.0))
        *rta-comp* lcomp)
  (setq rilha (rta:getr "Raio das extremidades da ILHA CENTRAL (m)"
                        (if *rta-rilha* *rta-rilha* 12.0))
        *rta-rilha* rilha)
  (setq wanel (rta:getr "Largura da PISTA ROTATORIA - anel (m)"
                        (if *rta-wanel* *rta-wanel* 7.20))
        *rta-wanel* wanel)
  (setq wgalg (rta:getr "Largura da FAIXA GALGAVEL (0 = sem faixa) (m)"
                        (if *rta-galg* *rta-galg* 0.0))
        *rta-galg* wgalg)
  (setq rent  (rta:getr "Raio de ENTRADA (m)"
                        (if *rta-rent* *rta-rent* 25.0))
        *rta-rent* rent)
  (setq rsai  (rta:getr "Raio de SAIDA (m)"
                        (if *rta-rsai* *rta-rsai* 30.0))
        *rta-rsai* rsai)
  (setq lacess (rta:getr "Largura do ACESSO secundario (m)"
                        (if *rta-lac* *rta-lac* 7.00))
        *rta-lac* lacess)
  (setq htxt  (rta:getr "Altura dos textos (unidades de desenho)"
                        (if *rta-h* *rta-h* 1.50))
        *rta-h* htxt)

  ;; --- 6.3 pontos e cotas --------------------------------------------------
  (setvar "OSMODE" 0)
  (setq pe (rta:2d (getpoint "\nPONTO DE ENTRADA (eixo do acesso, lado externo): ")))
  (setq cote (rta:getr "Cota no ponto de ENTRADA (m)"
                       (if *rta-ce* *rta-ce* 0.0))
        *rta-ce* cote)
  (setq ps (rta:2d (getpoint "\nPONTO DE SAIDA (eixo do acesso, lado externo): ")))
  (setq cots (rta:getr "Cota no ponto de SAIDA (m)"
                       (if *rta-cs* *rta-cs* 0.0))
        *rta-cs* cots)

  ;; --- 6.4 geometria de referencia sobre o eixo ----------------------------
  (setq pce (vlax-curve-getClosestPointTo ent (list (car pe) (cadr pe) 0.0) T)
        pcs (vlax-curve-getClosestPointTo ent (list (car ps) (cadr ps) 0.0) T)
        de  (vlax-curve-getDistAtPoint ent pce)
        ds  (vlax-curve-getDistAtPoint ent pcs)
        db  (/ (+ de ds) 2.0)
        pb  (rta:2d (vlax-curve-getPointAtDist ent db))
        par (vlax-curve-getParamAtDist ent db)
        u   (rta:un (rta:2d (vlax-curve-getFirstDeriv ent par)))
        n   (rta:perp u))

  ;; centros das calotas (retornos) da ilha central
  (setq c1 (rta:v+ pb (rta:v* u (- (- (/ lcomp 2.0) rilha))))
        c2 (rta:v+ pb (rta:v* u    (- (/ lcomp 2.0) rilha))))

  (if (<= (- (/ lcomp 2.0) rilha) 0.0)
    (progn (princ "\n*** Comprimento insuficiente: L deve ser maior que 2 x raio da ilha. ***")
           (*error* nil) (exit)))

  ;; --- 6.5 desenho do nucleo do dispositivo --------------------------------
  (rta:obround c1 c2 rilha "ROT-ILHA")                          ; ilha central
  (if (> wgalg 0.0)
    (rta:obround c1 c2 (+ rilha wgalg) "ROT-GALGAVEL"))         ; faixa galgavel
  (rta:obround c1 c2 (+ rilha wgalg wanel) "ROT-ANEL")          ; bordo externo

  ;; bordos de aproximacao (tangentes as calotas), 60 m para cada lado
  (setq wdisp (+ rilha wgalg wanel))
  (foreach s (list 1.0 -1.0)
    (rta:line (rta:v+ c1 (rta:v* n (* s wdisp)))
              (rta:v+ (rta:v+ c1 (rta:v* n (* s wdisp))) (rta:v* u -60.0))
              "ROT-ANEL")
    (rta:line (rta:v+ c2 (rta:v* n (* s wdisp)))
              (rta:v+ (rta:v+ c2 (rta:v* n (* s wdisp))) (rta:v* u 60.0))
              "ROT-ANEL"))

  ;; eixo do dispositivo (2D, para conferencia)
  (rta:line (rta:v+ pb (rta:v* u (- (/ lcomp 2.0))))
            (rta:v+ pb (rta:v* u    (/ lcomp 2.0))) "ROT-EIXO")

  ;; --- 6.6 acessos: eixo, bordos, concordancias e nariz --------------------
  (setq ta1 (rta:acesso pe u n pb wdisp lacess rent rsai htxt)
        ta2 (rta:acesso ps u n pb wdisp lacess rent rsai htxt))

  ;; --- 6.7 greide: interpolacao linear entre as cotas dadas ----------------
  (if (> (abs (- ds de)) 0.001)
    (setq ivert (/ (- cots cote) (- ds de)))
    (setq ivert 0.0))
  (setq z1 (+ cote (* ivert (- (- db (/ lcomp 2.0)) de)))   ; cota no inicio
        z2 (+ cote (* ivert (- (+ db (/ lcomp 2.0)) de))))  ; cota no fim
  (setq ptsz
        (list (list (car (rta:v+ pb (rta:v* u (- (/ lcomp 2.0)))))
                    (cadr (rta:v+ pb (rta:v* u (- (/ lcomp 2.0))))) z1)
              (list (car c1) (cadr c1)
                    (+ cote (* ivert (- (- db (- (/ lcomp 2.0) rilha)) de))))
              (list (car pb) (cadr pb) (+ cote (* ivert (- db de))))
              (list (car c2) (cadr c2)
                    (+ cote (* ivert (- (+ db (- (/ lcomp 2.0) rilha)) de))))
              (list (car (rta:v+ pb (rta:v* u (/ lcomp 2.0))))
                    (cadr (rta:v+ pb (rta:v* u (/ lcomp 2.0)))) z2)))
  (rta:3dpoly ptsz "ROT-EIXO")
  (foreach p ptsz
    (rta:text (rta:v+ (rta:2d p) (rta:v* n (* 1.2 htxt)))
              htxt (strcat "C=" (rta:fmt (caddr p) 3)) "ROT-COTAS" (angle '(0 0) u)))
  (rta:text (rta:v+ pe (list htxt htxt)) htxt
            (strcat "ENTRADA C=" (rta:fmt cote 3)) "ROT-COTAS" 0.0)
  (rta:text (rta:v+ ps (list htxt htxt)) htxt
            (strcat "SAIDA C=" (rta:fmt cots 3)) "ROT-COTAS" 0.0)

  ;; --- 6.8 verificacoes ----------------------------------------------------
  ;; entrecruzamento disponivel = distancia do acesso ao centro da calota
  (setq lw1 (min (abs (- (abs (- de db)) (- (/ lcomp 2.0) rilha)))
                 (+ (abs (- de db)) (- (/ lcomp 2.0) rilha)))
        lw2 (min (abs (- (abs (- ds db)) (- (/ lcomp 2.0) rilha)))
                 (+ (abs (- ds db)) (- (/ lcomp 2.0) rilha)))
        lwmin (min lw1 lw2))

  (setq msg1 (if (>= lwmin lreq) "OK" "ATENCAO"))
  (setq msg2 (if (>= (+ (* 2.0 (+ rilha wgalg)) (* 2.0 wanel)) wnec) "OK" "ATENCAO"))
  (setq msg3 (if (<= (abs (* 100.0 ivert)) 6.0) "OK" "ATENCAO"))

  ;; --- 6.9 quadro de dados -------------------------------------------------
  (setvar "OSMODE" 0)
  (setq pq (rta:2d (getpoint "\nPonto de insercao do QUADRO DE DADOS: ")))
  (setq txt
    (list
      "ROTATORIA ALONGADA - QUADRO DE DADOS"
      "-------------------------------------------------"
      (strcat "Velocidade diretriz da rodovia .... " (rta:fmt vel 0) " km/h")
      (strcat "Vel. de projeto da pista rotatoria  " (rta:fmt vrot 0) " km/h"
              "  (IPR-718: min " (rta:fmt vmin 0) " / des " (rta:fmt vdes 0) ")")
      (strcat "Comprimento total do dispositivo .. " (rta:fmt lcomp 2) " m")
      (strcat "Raio das extremidades da ilha ..... " (rta:fmt rilha 2) " m")
      (strcat "Largura da pista rotatoria ........ " (rta:fmt wanel 2) " m")
      (strcat "Faixa galgavel .................... " (rta:fmt wgalg 2) " m")
      (strcat "Raio de entrada / saida ........... " (rta:fmt rent 2) " / "
              (rta:fmt rsai 2) " m")
      (strcat "Largura do acesso ................. " (rta:fmt lacess 2) " m")
      (strcat "Veiculo de projeto do retorno ..... " (rta:nome-veic veic))
      "-------------------------------------------------"
      (strcat "Cota de entrada ................... " (rta:fmt cote 3) " m")
      (strcat "Cota de saida ..................... " (rta:fmt cots 3) " m")
      (strcat "Rampa resultante .................. " (rta:fmt (* 100.0 ivert) 3) " %")
      (strcat "Cota no inicio / fim do dispositivo " (rta:fmt z1 3) " / "
              (rta:fmt z2 3) " m")
      "-------------------------------------------------"
      "VERIFICACOES (advisorias):"
      (strcat "1. Entrecruzamento disponivel ..... " (rta:fmt lwmin 2)
              " m  x  minimo " (rta:fmt lreq 2) " m  -> " msg1)
      (strcat "2. Largura p/ retorno tipo A ...... "
              (rta:fmt (+ (* 2.0 (+ rilha wgalg)) (* 2.0 wanel)) 2)
              " m  x  minimo " (rta:fmt wnec 2) " m  -> " msg2)
      (strcat "3. Rampa longitudinal ............. "
              (rta:fmt (abs (* 100.0 ivert)) 2) " %  x  referencia 6% -> " msg3)
      (strcat "4. Superelevacao dos ramos ........ " (rta:superelev vrot)
              " (IPR-718)")
      "5. Visibilidade: conferir 180 m da ilha de aproximacao."
      "6. Conferir capacidade / nivel de servico (HCM nao cobre o tipo)."
      "-------------------------------------------------"
      "Ref.: DNIT IPR-718/2005; DER Projeto-tipo Rotatoria Alongada;"
      "      DER/MG rotatoria nao vazada; CTB art. 29, III, b."))
  (setq y 0.0)
  (foreach l txt
    (rta:text (list (car pq) (- (cadr pq) y)) htxt l "ROT-QUADRO" 0.0)
    (setq y (+ y (* 1.6 htxt))))

  ;; --- 6.10 resumo no prompt ----------------------------------------------
  (princ "\n-------------------------------------------------------------")
  (princ (strcat "\n Entrecruzamento disponivel: " (rta:fmt lwmin 2)
                 " m (minimo " (rta:fmt lreq 2) " m) -> " msg1))
  (princ (strcat "\n Largura para retorno tipo A: "
                 (rta:fmt (+ (* 2.0 (+ rilha wgalg)) (* 2.0 wanel)) 2)
                 " m (minimo " (rta:fmt wnec 2) " m) -> " msg2))
  (princ (strcat "\n Rampa longitudinal: " (rta:fmt (* 100.0 ivert) 3)
                 " % -> " msg3))
  (princ (strcat "\n Superelevacao recomendada nos ramos: " (rta:superelev vrot)))
  (princ "\n Verifique visibilidade (180 m) e capacidade separadamente.")
  (princ "\n-------------------------------------------------------------")

  (setvar "OSMODE" osm)
  (setvar "CMDECHO" cme)
  (setvar "CLAYER" cly)
  (princ))

;;; ---------------------------------------------------------------------------
;;; 7. GERACAO DE UM ACESSO (eixo, bordos, concordancias e ilha divisoria)
;;;    pa    : ponto do acesso escolhido pelo usuario
;;;    u,n   : direcao do eixo e normal (n = 90 graus a esquerda de u)
;;;    pb    : ponto base do dispositivo sobre o eixo
;;;    wdisp : semilargura do dispositivo (raio da ilha + galgavel + anel)
;;; ---------------------------------------------------------------------------
(defun rta:acesso (pa u n pb wdisp lac rent rsai htxt
                   / s ns pr da nda pl dl a sgn pd pu qd qu
                     fd fu f nar1 nar2 g lsp wsp)
  (setq s   (if (> (rta:dot (rta:v- pa pb) n) 0.0) 1.0 -1.0) ; lado do acesso
        ns  (rta:v* n s)                                     ; normal p/ o acesso
        f   (rta:v* u (- s))                                 ; sentido do fluxo adjacente
        pr  (rta:proj pa pb u)                               ; projecao no eixo
        da  (rta:un (rta:v- pr pa))                          ; direcao do acesso
        nda (rta:perp da)
        pl  (rta:v+ pb (rta:v* ns wdisp))                    ; reta do bordo do anel
        dl  u)
  ;; eixo do acesso ate o bordo do dispositivo
  (setq a (inters pl (rta:v+ pl dl) pa (rta:v+ pa da) nil))
  (if (not a) (setq a (rta:v+ pa (rta:v* da (- (rta:mod (rta:v- pr pa)) wdisp)))))
  (rta:line pa a "ROT-ACESSO")

  ;; bordos do acesso: sgn separa o bordo de jusante (entrada) do de montante
  (setq sgn (if (> (rta:dot nda f) 0.0) 1.0 -1.0)
        pd  (rta:v+ pa (rta:v* nda (*    sgn  (/ lac 2.0))))   ; bordo de jusante
        pu  (rta:v+ pa (rta:v* nda (* (- sgn) (/ lac 2.0)))))  ; bordo de montante

  ;; concordancia de ENTRADA (bordo de jusante x bordo do anel)
  (setq qd (inters pd (rta:v+ pd da) pl (rta:v+ pl dl) nil))
  (if qd
    (progn
      (setq fd (rta:fillet pd da pl dl rent
                 (rta:v+ (rta:v+ qd (rta:v* ns rent)) (rta:v* f rent))))
      (if fd
        (progn (rta:line pd (cadr fd) "ROT-ACESSO")
               (rta:arc2p (car fd) (cadr fd) (caddr fd) "ROT-ACESSO")))))

  ;; concordancia de SAIDA (bordo de montante x bordo do anel)
  (setq qu (inters pu (rta:v+ pu da) pl (rta:v+ pl dl) nil))
  (if qu
    (progn
      (setq fu (rta:fillet pu da pl dl rsai
                 (rta:v+ (rta:v+ qu (rta:v* ns rsai)) (rta:v* f (- rsai)))))
      (if fu
        (progn (rta:line pu (cadr fu) "ROT-ACESSO")
               (rta:arc2p (car fu) (cadr fu) (caddr fu) "ROT-ACESSO")))))

  ;; ilha divisoria (nariz) no acesso: obround ao longo do eixo do acesso
  (setq g 1.00 lsp 15.0 wsp 2.00)
  (if (> lsp (+ wsp 1.0))
    (progn
      (setq nar1 (rta:v+ a (rta:v* da (- (+ g (/ wsp 2.0)))))
            nar2 (rta:v+ a (rta:v* da (- (+ g (- lsp (/ wsp 2.0)))))))
      (rta:obround nar1 nar2 (/ wsp 2.0) "ROT-NARIZ")))
  T)

;;; ---------------------------------------------------------------------------
(defun C:RTA () (C:ROTALONGADA))

(princ "\nROTALONGADA.lsp carregado.")
(princ "\n  ROTALONGADA (ou RTA) = gera a rotatoria alongada.")
(princ "\n  RTANORMAS            = imprime as tabelas normativas adotadas.")
(princ)
;;; ===========================================================================
