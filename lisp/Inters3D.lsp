;===========================================================================
; INTERS3D.LSP  -  Intersecao viaria parametrica para AutoCAD Civil 3D
;---------------------------------------------------------------------------
; O que a rotina faz:
;   1. Pede os eixos das vias (principal e secundaria), as meia-larguras
;      das pistas, os raios dos 4 cantos (iguais ou individuais) e a
;      cota (greide) da intersecao;
;   2. Desenha os retornos de meio-fio (arcos) e os trechos retos de guia
;      ja na cota informada (camada INTERS3D-FL);
;   3. Gera o limite fechado do patio da intersecao (camada INTERS3D-LIMITE);
;   4. Cria uma superficie TIN e adiciona as breaklines + limite (via COM);
;   5. Opcionalmente cria os alinhamentos das duas vias (via COM);
;   6. Opcionalmente abre o assistente CreateIntersection no final.
;
; LIMITACAO DA API: a Autodesk nao expoe a criacao de objetos
; "Intersection" (intersecao dinamica) nem de "Corridor" (corredor) para
; AutoLISP/COM - eles so sao criados pelo assistente CreateIntersection.
; Por isso esta rotina prepara tudo (geometria, superficie do patio e
; alinhamentos) e, no final, abre o assistente com os parametros ja
; impressos na linha de comando (raios e cota) para voce confirmar.
;
; Requisitos: Civil 3D 2018 ou superior, desenho em metros, UCS = WCS.
; Uso: APPLOAD -> selecione Inters3D.lsp -> comando: INTERS3D
;===========================================================================

(vl-load-com)

;------------------------- utilitarios -------------------------------------

(defun i3d:catch (f a) (vl-catch-all-apply f a))
(defun i3d:ok? (x) (not (vl-catch-all-error-p x)))

(defun i3d:v+ (a b) (list (+ (car a) (car b)) (+ (cadr a) (cadr b))))
(defun i3d:v- (a b) (list (- (car a) (car b)) (- (cadr a) (cadr b))))
(defun i3d:v*s (v s) (list (* (car v) s) (* (cadr v) s)))
(defun i3d:perp (v) (list (- (cadr v)) (car v)))
(defun i3d:mid2 (a b)
  (list (/ (+ (car a) (car b)) 2.0) (/ (+ (cadr a) (cadr b)) 2.0)))

(defun i3d:vunit (v / d)
  (if (> (setq d (distance '(0.0 0.0) v)) 1e-9)
    (list (/ (car v) d) (/ (cadr v) d))))

; intersecao de duas retas 2D (ponto + direcao unitaria); nil se paralelas
(defun i3d:inters2d (p1 d1 p2 d2 / dx dy den t)
  (setq dx  (- (car p2) (car p1))
        dy  (- (cadr p2) (cadr p1))
        den (- (* (car d1) (cadr d2)) (* (cadr d1) (car d2))))
  (if (> (abs den) 1e-9)
    (progn
      (setq t (/ (- (* dx (cadr d2)) (* dy (car d2))) den))
      (list (+ (car p1) (* t (car d1))) (+ (cadr p1) (* t (cadr d1)))))))

; pe da perpendicular de c sobre a reta (p, d unitaria)
(defun i3d:foot (c p d)
  (i3d:v+ p (i3d:v*s d (+ (* (- (car c) (car p)) (car d))
                          (* (- (cadr c) (cadr p)) (cadr d))))))

;--------------------------- interface -------------------------------------

(defun i3d:sn (msg def / r)
  (initget "Sim Nao")
  (setq r (getkword (strcat msg " [Sim/Nao] <" def ">: ")))
  (if (null r) (= def "Sim") (= r "Sim")))

(defun i3d:getreal (msg def / r)
  (initget 6)                                   ; nao nulo, nao negativo
  (setq r (getreal (strcat msg " <" (rtos def 2 2) ">: ")))
  (if r r def))

;----------------------- leitura dos eixos ---------------------------------
; devolve (ponto2d direcao ename) a partir de LINE / LWPOLYLINE / XLINE / RAY
(defun i3d:eixo-de-ent (es / ent pk el tp pts a b bp dmin i mid)
  (setq ent (car es)
        pk  (cadr es)
        el  (entget ent)
        tp  (cdr (assoc 0 el)))
  (cond
    ((= tp "LINE")
     (setq a (cdr (assoc 10 el))
           b (cdr (assoc 11 el)))
     (list (i3d:mid2 a b)
           (i3d:vunit (list (- (car b) (car a)) (- (cadr b) (cadr a))))
           ent))
    ((or (= tp "XLINE") (= tp "RAY"))
     (list (cdr (assoc 10 el))
           (i3d:vunit (cdr (assoc 11 el)))
           ent))
    ((= tp "LWPOLYLINE")
     (setq pts (mapcar 'cdr (vl-remove-if-not '(lambda (x) (= (car x) 10)) el)))
     (if (> (length pts) 1)
       (progn
         (setq i 0 dmin 1e99 bp nil)
         (while (< i (1- (length pts)))
           (setq a   (nth i pts)
                 b   (nth (1+ i) pts)
                 mid (i3d:mid2 a b))
           (if (< (distance (list (car pk) (cadr pk)) mid) dmin)
             (setq dmin (distance (list (car pk) (cadr pk)) mid)
                   bp   (list a b)))
           (setq i (1+ i)))
         (setq a (car bp)
               b (cadr bp))
         (list (i3d:mid2 a b)
               (i3d:vunit (list (- (car b) (car a)) (- (cadr b) (cadr a))))
               ent))))))

(defun i3d:pega-eixo (msg / es r)
  (while (and (setq es (entsel msg)) (null (setq r (i3d:eixo-de-ent es))))
    (prompt "\nEntidade invalida - selecione LINE, LWPOLYLINE, XLINE ou RAY."))
  (if (null es)
    (progn (prompt "\nSelecao cancelada.") (exit))
    r))

;--------------------------- desenho ---------------------------------------

(defun i3d:layer (nome cor / doc r)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq r (i3d:catch 'vla-Add (list (vla-get-Layers doc) nome)))
  (if (i3d:ok? r) (i3d:catch 'vla-put-Color (list r cor)))
  nome)

(defun i3d:arco3d (cen r a1 a2 z lay)
  (entmakex (list '(0 . "ARC") (cons 8 lay)
                  (list 10 (car cen) (cadr cen) z)
                  (cons 40 r) (cons 50 a1) (cons 51 a2))))

(defun i3d:linha3d (p q z lay)
  (entmakex (list '(0 . "LINE") (cons 8 lay)
                  (list 10 (car p) (cadr p) z)
                  (list 11 (car q) (cadr q) z))))

(defun i3d:texto (p h s lay)
  (entmakex (list '(0 . "TEXT") (cons 8 lay)
                  (list 10 (car p) (cadr p) 0.0)
                  (cons 40 h) (cons 1 s) (cons 50 0.0)
                  (cons 72 4) (cons 73 2)
                  (list 11 (car p) (cadr p) 0.0))))

; conecta dois pontos com LINE na cota z (se comprimento valido)
(defun i3d:conecta (p q z lay)
  (if (> (distance p q) 0.01)
    (i3d:linha3d p q z lay)
    (prompt "\n  AVISO: trecho reto de guia nulo (raio grande demais para o vao).")))

; polilinha fechada a partir de lista de (ponto . bulge), na cota z
(defun i3d:pline-fechada (vbs z lay / l vb)
  (setq l (list '(0 . "LWPOLYLINE") '(100 . "AcDbEntity") (cons 8 lay)
                '(100 . "AcDbPolyline") (cons 90 (length vbs))
                (cons 70 1) (cons 38 z)))
  (foreach vb vbs
    (setq l (append l (list (list 10 (caar vb) (cadar vb)) (cons 42 (cdr vb))))))
  (entmakex l))

; bulge do arco de um canto percorrido a partir do ponto "de"
; canto = (im is raio ta tb pa1 pa2 sweep pij)
(defun i3d:bulge (canto de / s)
  (setq s (/ (sin (/ (nth 7 canto) 4.0)) (cos (/ (nth 7 canto) 4.0))))
  (if (< (distance de (nth 5 canto)) 1e-6) s (- s)))

;------------------------- Civil 3D (COM) ----------------------------------

(defun i3d:aecc-app (/ versoes app r)
  (setq versoes '("13.8" "13.7" "13.6" "13.5" "13.4" "13.3" "13.2" "13.1"
                  "13.0" "12.0" "11.0" "10.5" "10.3" "10.0" "9.0"))
  (while (and versoes (null app))
    (setq r (i3d:catch 'vlax-invoke-method
              (list (vlax-get-acad-object) 'GetInterfaceObject
                    (strcat "AeccXUiLand.AeccApplication." (car versoes)))))
    (if (i3d:ok? r) (setq app r))
    (setq versoes (cdr versoes)))
  app)

(defun i3d:tin-data (/ versoes r o)
  (setq versoes '("13.8" "13.7" "13.6" "13.5" "13.4" "13.3" "13.2" "13.1"
                  "13.0" "12.0" "11.0" "10.5" "10.3" "10.0" "9.0"))
  (while (and versoes (null o))
    (setq r (i3d:catch 'vlax-create-object
              (list (strcat "AeccXLandLib.AeccTinCreationData." (car versoes)))))
    (if (i3d:ok? r) (setq o r))
    (setq versoes (cdr versoes)))
  (if (null o)
    (progn
      (setq r (i3d:catch 'vlax-create-object
                (list "AeccXLandLib.AeccTinCreationData")))
      (if (i3d:ok? r) (setq o r))))
  o)

; nome do primeiro estilo disponivel de uma colecao de estilos
(defun i3d:estilo0 (coll / r)
  (setq r (i3d:catch 'vlax-invoke-method (list coll 'Item 0)))
  (if (i3d:ok? r)
    (setq r (i3d:catch 'vlax-get-property (list r 'Name))))
  (if (i3d:ok? r) r nil))

; cria superficie TIN; devolve o objeto ou nil
(defun i3d:cria-superficie (civdoc nome / surfs tin estilo r)
  (setq surfs  (vlax-get-property civdoc 'Surfaces)
        tin    (i3d:tin-data)
        estilo (i3d:estilo0 (vlax-get-property civdoc 'SurfaceStyles)))
  (if tin
    (progn
      (i3d:catch 'vlax-put-property (list tin 'Name nome))
      (if estilo (i3d:catch 'vlax-put-property (list tin 'Style estilo)))
      (i3d:catch 'vlax-put-property (list tin 'Layer "0"))
      (i3d:catch 'vlax-put-property (list tin 'BaseLayer "0"))
      (setq r (i3d:catch 'vlax-invoke-method (list surfs 'AddTinSurface tin)))
      (if (i3d:ok? r) r nil))))

; adiciona breaklines (lista de vla-objects) a superficie; devolve qtd. ok
(defun i3d:add-breaklines (surf objs / brks arr i r n a1)
  (setq brks (i3d:catch 'vlax-get-property (list surf 'Breaklines))
        n    0)
  (if (i3d:ok? brks)
    (progn
      (setq arr (vlax-make-safearray vlax-vbObject (cons 0 (1- (length objs))))
            i   0)
      (foreach o objs
        (vlax-safearray-put-element arr i o)
        (setq i (1+ i)))
      (setq r (i3d:catch 'vlax-invoke-method
                (list brks 'AddStandardBreakline arr "INTERS3D-FL" 1.0)))
      (if (i3d:ok? r)
        (setq n (length objs))
        (foreach o objs                      ; tenta uma a uma
          (setq a1 (vlax-make-safearray vlax-vbObject '(0 . 0)))
          (vlax-safearray-put-element a1 0 o)
          (setq r (i3d:catch 'vlax-invoke-method
                    (list brks 'AddStandardBreakline a1 "INTERS3D-FL" 1.0)))
          (if (i3d:ok? r) (setq n (1+ n)))))))
  (i3d:catch 'vlax-invoke-method (list surf 'Rebuild))
  n)

; cria alinhamento a partir de uma polilinha (apagar = T apaga a pline temp)
(defun i3d:cria-alinhamento (civdoc pl nome apagar / aligns estilo rotulo oid r)
  (setq aligns (i3d:catch 'vlax-get-property (list civdoc 'AlignmentsSiteless)))
  (if (i3d:ok? aligns)
    (progn
      (setq estilo (i3d:estilo0 (vlax-get-property civdoc 'AlignmentStyles))
            rotulo (i3d:estilo0 (vlax-get-property civdoc 'AlignmentLabelStyleSets))
            oid    (if (vlax-property-available-p pl 'ObjectID32)
                     (vla-get-ObjectID32 pl)
                     (vla-get-ObjectID pl)))
      (if (and estilo rotulo)
        (progn
          (setq r (i3d:catch 'vlax-invoke-method
                    (list aligns 'AddFromPolyline nome "0" oid estilo rotulo
                          :vlax-false
                          (if apagar :vlax-true :vlax-false))))
          (if (i3d:ok? r) r nil))))))

; cria alinhamento para um eixo (pto dir ename); usa a propria LWPOLYLINE
; do usuario se houver, senao cria uma pline temporaria de 2*comp metros
(defun i3d:alinha-eixo (civdoc eixo p0 comp nome lay / d en pa pb r)
  (setq d  (nth 1 eixo)
        en (nth 2 eixo))
  (if (= (cdr (assoc 0 (entget en))) "LWPOLYLINE")
    (i3d:cria-alinhamento civdoc (vlax-ename->vla-object en) nome :vlax-false)
    (progn
      (setq pa (i3d:v- p0 (i3d:v*s d comp))
            pb (i3d:v+ p0 (i3d:v*s d comp))
            en (entmakex (list '(0 . "LWPOLYLINE") '(100 . "AcDbEntity")
                               (cons 8 lay) '(100 . "AcDbPolyline")
                               (cons 90 2) (cons 70 0)
                               (list 10 (car pa) (cadr pa))
                               (list 10 (car pb) (cadr pb)))))
      (setq r (i3d:cria-alinhamento civdoc (vlax-ename->vla-object en) nome :vlax-true))
      (if (null r) en r))))    ; se falhar, devolve a pline (fica como eixo)

;--------------------------- tratamento de erro ----------------------------

(defun i3d:erro (msg)
  (if *i3d:oldcmd* (setvar "CMDECHO" *i3d:oldcmd*))
  (if *i3d:doc* (i3d:catch 'vla-EndUndoMark (list *i3d:doc*)))
  (if (and msg
           (/= msg "Function cancelled")
           (/= msg "quit / exit abort")
           (/= msg "bad argument type: consp nil"))
    (prompt (strcat "\nErro INTERS3D: " msg)))
  (princ))

;============================= COMANDO =====================================

(defun C:INTERS3D (/ *error* doc layfl laylim layrot layeix
                     ex1 ex2 p1 d1 p2 d2 p0 nm ns den
                     mp ms iguais raios cota comp sname sn-def suf
                     specs k im is raio pm ps pij cm cs cen ta tb
                     aa1 aa2 dd m1 m2 pa1 pa2 sweep amid ppos en
                     cantos cn1 cn2 cn3 cn4 flent vbs plbnd
                     q1 q2 w1 w2 htxt
                     criar-ali abrir-wiz app civdoc surf nbrk
                     ali1 ali2 msg-ali txt-raios)

  (setq *error*      i3d:erro
        *i3d:oldcmd* (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)
  (setq *i3d:doc* (vla-get-ActiveDocument (vlax-get-acad-object)))
  (vla-StartUndoMark *i3d:doc*)

  ; valores padrao lembrados entre execucoes
  (if (null *i3d:mp*)   (setq *i3d:mp*   3.50))
  (if (null *i3d:ms*)   (setq *i3d:ms*   3.50))
  (if (null *i3d:r*)    (setq *i3d:r*    8.00))
  (if (null *i3d:comp*) (setq *i3d:comp* 500.0))

  (prompt "\n=========== INTERS3D - Intersecao viaria ===========")
  (prompt "\nSelecione os eixos (LINE, LWPOLYLINE, XLINE ou RAY).")

  ;--- 1) eixos
  (setq ex1 (i3d:pega-eixo "\nEixo da via PRINCIPAL: ")
        p1  (nth 0 ex1) d1 (nth 1 ex1)
        ex2 (i3d:pega-eixo "\nEixo da via SECUNDARIA: ")
        p2  (nth 0 ex2) d2 (nth 1 ex2)
        p0  (i3d:inters2d p1 d1 p2 d2))
  (if (null p0)
    (progn (alert "Os eixos sao paralelos - nao ha cruzamento.") (exit)))
  (setq den (abs (- (* (car d1) (cadr d2)) (* (cadr d1) (car d2)))))
  (if (< den 0.087)
    (progn (alert "Angulo entre os eixos muito pequeno (< 5 graus).") (exit)))
  (setq nm (i3d:perp d1)
        ns (i3d:perp d2))

  ;--- 2) parametros
  (setq mp     (i3d:getreal "\nMeia-largura da pista PRINCIPAL (m)" *i3d:mp*)
        ms     (i3d:getreal "\nMeia-largura da pista SECUNDARIA (m)" *i3d:ms*)
        iguais (i3d:sn "\nRaios iguais nos 4 cantos?" "Sim"))
  (prompt "\n(Cantos numerados de 1 a 4 no anti-horario; rotulos serao desenhados.)")
  (if iguais
    (setq raios (list (i3d:getreal "\nRaio dos cantos (m)" *i3d:r*)
                      nil nil nil)
          raios (list (car raios) (car raios) (car raios) (car raios)))
    (setq raios (list (i3d:getreal "\nRaio do canto 1 (m)" *i3d:r*)
                      (i3d:getreal "\nRaio do canto 2 (m)" *i3d:r*)
                      (i3d:getreal "\nRaio do canto 3 (m)" *i3d:r*)
                      (i3d:getreal "\nRaio do canto 4 (m)" *i3d:r*))))

  (setq cota
    (if *i3d:cota*
      (progn
        (setq cota (getreal (strcat "\nCota (greide) da intersecao <"
                                    (rtos *i3d:cota* 2 3) ">: ")))
        (if cota cota *i3d:cota*))
      (getreal "\nCota (greide) da intersecao: ")))
  (if (null cota)
    (progn (prompt "\nCota e obrigatoria. Rotina encerrada.") (exit)))

  (setq comp (i3d:getreal "\nExtensao de cada alinhamento a partir do cruzamento (m)" *i3d:comp*))

  ; nome da superficie (sufixo de hora para nao duplicar)
  (setq suf    (menucmd "M=$(edtime,$(getvar,date),HHMMSS)")
        suf    (if (and suf (/= suf "")) suf "000000")
        sn-def (strcat "INTERS3D-" suf)
        sname  (getstring (strcat "\nNome da superficie <" sn-def ">: ")))
  (if (or (null sname) (= sname "")) (setq sname sn-def))

  (setq criar-ali (i3d:sn "\nCriar os alinhamentos das duas vias?" "Sim")
        abrir-wiz (i3d:sn "Abrir o assistente CreateIntersection ao final?" "Sim"))

  ; guarda defaults para a proxima execucao
  (setq *i3d:mp* mp *i3d:ms* ms *i3d:r* (car raios)
        *i3d:cota* cota *i3d:comp* comp)

  ;--- 3) camadas
  (setq layfl  (i3d:layer "INTERS3D-FL" 4)      ; linhas caracteristicas
        laylim (i3d:layer "INTERS3D-LIMITE" 3)  ; limite do patio
        layrot (i3d:layer "INTERS3D-ROTULOS" 2) ; rotulos dos raios
        layeix (i3d:layer "INTERS3D-EIXOS" 8))  ; eixos temporarios

  ;--- 4) geometria dos 4 cantos
  ; canto = (im is raio ta tb pa1 pa2 sweep pij)
  (setq htxt   2.5
        specs '((1 1) (-1 1) (-1 -1) (1 -1))
        cantos '()
        flent  '()
        k      0)
  (foreach sp specs
    (setq k    (1+ k)
          im   (car sp)
          is   (cadr sp)
          raio (nth (1- k) raios)
          pm   (i3d:v+ p0 (i3d:v*s nm (* im mp)))   ; borda da principal
          ps   (i3d:v+ p0 (i3d:v*s ns (* is ms)))   ; borda da secundaria
          pij  (i3d:inters2d pm d1 ps d2))          ; esquina (quina)
    ; centro do arco: bordas deslocadas de R para dentro (em direcao aos eixos)
    (setq cm  (i3d:v- pm (i3d:v*s nm (* im raio)))
          cs  (i3d:v- ps (i3d:v*s ns (* is raio)))
          cen (i3d:inters2d cm d1 cs d2)
          ta  (i3d:foot cen pm d1)                  ; tangencia na borda princ.
          tb  (i3d:foot cen ps d2)                  ; tangencia na borda secun.
          aa1 (angle cen ta)
          aa2 (angle cen tb)
          dd  (rem (+ (- aa2 aa1) (* 2.0 pi)) (* 2.0 pi))
          m1  (polar cen (+ aa1 (/ dd 2.0)) raio)
          m2  (polar cen (+ aa1 (/ dd 2.0) pi) raio))
    ; escolhe o arco que passa mais perto da quina (canto externo)
    (if (<= (distance m1 pij) (distance m2 pij))
      (setq pa1 ta pa2 tb sweep dd)
      (setq pa1 tb pa2 ta sweep (- (* 2.0 pi) dd)))
    ; arco ja na cota da intersecao -> vira breakline
    (setq en (i3d:arco3d cen raio (angle cen pa1) (angle cen pa2) cota layfl))
    (setq flent (cons en flent))
    ; rotulo do raio
    (setq amid (polar cen (+ (angle cen pa1) (/ sweep 2.0)) raio)
          ppos (polar cen (angle cen amid) (+ raio (* 1.5 htxt))))
    (i3d:texto ppos htxt (strcat "R" (itoa k) "=" (rtos raio 2 2)) layrot)
    (setq cantos (append cantos (list (list im is raio ta tb pa1 pa2 sweep pij)))))

  (setq cn1 (nth 0 cantos)
        cn2 (nth 1 cantos)
        cn3 (nth 2 cantos)
        cn4 (nth 3 cantos))

  ;--- 5) trechos retos de guia entre os retornos (na cota)
  (foreach par (list (list (nth 3 cn1) (nth 3 cn4))   ; borda principal +
                     (list (nth 3 cn2) (nth 3 cn3))   ; borda principal -
                     (list (nth 4 cn1) (nth 4 cn2))   ; borda secundaria +
                     (list (nth 4 cn3) (nth 4 cn4)))  ; borda secundaria -
    (setq en (i3d:conecta (car par) (cadr par) cota layfl))
    (if en (setq flent (cons en flent))))

  ;--- 6) eixos dentro do patio (linhas de coroamento, na cota)
  (setq q1 (i3d:inters2d p0 d1 (i3d:v+ p0 (i3d:v*s ns ms)) d2)
        q2 (i3d:inters2d p0 d1 (i3d:v+ p0 (i3d:v*s ns (- ms))) d2)
        w1 (i3d:inters2d p0 d2 (i3d:v+ p0 (i3d:v*s nm mp)) d1)
        w2 (i3d:inters2d p0 d2 (i3d:v+ p0 (i3d:v*s nm (- mp))) d1))
  (setq en (i3d:linha3d q1 q2 cota layfl)) (setq flent (cons en flent))
  (setq en (i3d:linha3d w1 w2 cota layfl)) (setq flent (cons en flent))

  ;--- 7) limite fechado do patio
  (setq vbs (list (cons (nth 3 cn1) 0.0)
                  (cons (nth 3 cn4) (i3d:bulge cn4 (nth 3 cn4)))
                  (cons (nth 4 cn4) 0.0)
                  (cons (nth 4 cn3) (i3d:bulge cn3 (nth 4 cn3)))
                  (cons (nth 3 cn3) 0.0)
                  (cons (nth 3 cn2) (i3d:bulge cn2 (nth 3 cn2)))
                  (cons (nth 4 cn2) 0.0)
                  (cons (nth 4 cn1) (i3d:bulge cn1 (nth 4 cn1))))
        plbnd (i3d:pline-fechada vbs cota laylim))

  (prompt (strcat "\nGeometria criada: 4 retornos, guias, coroamentos e limite na cota "
                  (rtos cota 2 3) "."))

  ;--- 8) Civil 3D via COM: superficie + breaklines + alinhamentos
  (setq app   (i3d:aecc-app)
        surf  nil
        nbrk  0
        ali1  nil
        ali2  nil)
  (if app
    (progn
      (setq civdoc (vlax-get-property app 'ActiveDocument))
      ; superficie TIN
      (setq surf (i3d:cria-superficie civdoc sname))
      (if surf
        (progn
          (setq nbrk (i3d:add-breaklines surf (mapcar 'vlax-ename->vla-object flent)))
          (i3d:catch 'vlax-invoke-method
            (list (vlax-get-property surf 'Boundaries) 'Add
                  (vlax-ename->vla-object plbnd) "Limite INTERS3D" 1 :vlax-true 1.0))
          (prompt (strcat "\nSuperficie \"" sname "\" criada com "
                          (itoa nbrk) " breaklines.")))
        (prompt (strcat "\nNao foi possivel criar a superficie via COM. "
                        "Crie uma superficie TIN vazia e adicione as breaklines "
                        "da camada INTERS3D-FL manualmente.")))
      ; alinhamentos
      (if criar-ali
        (progn
          (setq ali1 (i3d:alinha-eixo civdoc ex1 p0 comp
                                      (strcat "INTERS3D-PRINCIPAL-" suf) layeix)
                ali2 (i3d:alinha-eixo civdoc ex2 p0 comp
                                      (strcat "INTERS3D-SECUNDARIA-" suf) layeix))))
      )
    (prompt "\nAPI COM do Civil 3D indisponivel - criada apenas a geometria."))

  (setq msg-ali
    (cond ((and ali1 ali2) "INTERS3D-PRINCIPAL / INTERS3D-SECUNDARIA criados")
          (criar-ali       "falha - crie manualmente (CreateAlignmentEntities)")
          (t               "nao solicitados")))

  ;--- 9) resumo
  (setq txt-raios
        (strcat "R1=" (rtos (nth 0 raios) 2 2)
                "  R2=" (rtos (nth 1 raios) 2 2)
                "  R3=" (rtos (nth 2 raios) 2 2)
                "  R4=" (rtos (nth 3 raios) 2 2)))
  (princ (strcat
    "\n\n================ INTERS3D - RESUMO ================="
    "\n Cota da intersecao : " (rtos cota 2 3)
    "\n Raios (cantos 1-4) : " txt-raios
    "\n Superficie TIN     : " (if surf (strcat sname " (" (itoa nbrk) " breaklines)")
                                     "nao criada")
    "\n Alinhamentos       : " msg-ali
    "\n===================================================="
    "\n PROXIMOS PASSOS (corredor + intersecao dinamica):"
    "\n  1. Crie os perfis de greide dos dois alinhamentos"
    "\n     (CreateProfileFromSurface sobre o terreno, ou perfil de"
    "\n      projeto passando pela cota " (rtos cota 2 2) " no cruzamento);"
    "\n  2. No assistente CreateIntersection:"
    "\n     - pagina Geral: ponto do cruzamento + via primaria;"
    "\n     - pagina Geometria: informe os raios R1..R4 por quadrante;"
    "\n     - pagina Corredor: marque 'criar corredor' e escolha a assembly;"
    "\n  3. Apos criado: botao direito no corredor ->"
    "\n     'Criar superficie do corredor' (Create surface from corridor)."
    "\n===================================================="))
  (princ)

  ;--- 10) assistente
  (if (and abrir-wiz app)
    (progn
      (prompt "\nAbrindo o assistente CreateIntersection...")
      (command "_.CreateIntersection")))

  (vla-EndUndoMark *i3d:doc*)
  (setvar "CMDECHO" *i3d:oldcmd*)
  (setq *i3d:doc* nil)
  (prompt "\nINTERS3D concluido.")
  (princ)
)

(prompt "\nInters3D.lsp carregado. Digite INTERS3D para iniciar.")
(princ)
