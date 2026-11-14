;;; ==========================================================================
;;;  KMLCOTAS.lsp  -  Exporta o projeto do Civil 3D para o Google Earth (KML)
;;;                   obedecendo as regras de cota gravadas no proprio DWG
;;;
;;;  Point Top Topografia  -  v1.0  -  31/10/2026
;;;
;;;  COMANDOS
;;;    KMLCOTAS     exporta. Na primeira vez em cada desenho abre a
;;;                 configuracao; depois so pede os objetos e o arquivo.
;;;    KMLCOTASCFG  altera as regras de cota do desenho.
;;;
;;;  REGRAS DE COTA (ficam salvas dentro do DWG - salve o desenho)
;;;    Absoluto  altitude no Google Earth = cota do projeto + correcao.
;;;              Preserva cotas e rampas exatamente. A correcao pode ser
;;;              digitada ou calibrada comparando um ponto seu com a
;;;              altitude que o Google Earth mostra no mesmo lugar.
;;;    Relativo  altura sobre o terreno = cota do projeto - cota do TN
;;;              (superficie do Civil 3D). O projeto assenta no chao do
;;;              Google Earth mesmo que o relevo dele esteja errado.
;;;              Trechos em aterro ficam na altura certa; trechos em corte
;;;              (abaixo do TN) sao desenhados no chao, mais transparentes,
;;;              com o valor do corte na descricao e nos rotulos.
;;;    Chao      so planimetria, colada no terreno do Google Earth.
;;;
;;;  OBJETOS
;;;    LINE, ARC, CIRCLE, LWPOLYLINE, POLYLINE 2D/3D, SPLINE, POINT,
;;;    feature line, figura de levantamento e ponto COGO.
;;;    Alinhamento nao tem cota: gere a feature line ou polilinha 3D a partir
;;;    do alinhamento + greide (ou extraia do corredor) antes de exportar.
;;;
;;;  COORDENADAS
;;;    UTM em metros, elipsoide GRS80 (SIRGAS 2000). Fuso e hemisferio
;;;    vem do sistema atribuido ao desenho, ou sao informados na config.
;;;
;;;  SAIDA
;;;    Uma pasta por layer, cor do objeto/layer, descricao com cota inicial,
;;;    final, minima, maxima e rampa media; rotulos de cota opcionais.
;;; ==========================================================================

(vl-load-com)

;;; ------------------------------------------------------------- config ----

(defun kc:get (k def / v)
  (setq v (vlax-ldata-get "KMLCOTAS" k))
  (if (null v) def v)
)

(defun kc:put (k v) (vlax-ldata-put "KMLCOTAS" k v))

;; objeto COM sem derrubar a rotina se a COM nao estiver disponivel
(defun kc:obj (e / o)
  (setq o (vl-catch-all-apply 'vlax-ename->vla-object (list e)))
  (if (vl-catch-all-error-p o) nil o)
)

(defun kc:cscode (/ c)
  (setq c (vl-catch-all-apply 'getvar (list "CGEOCS")))
  (if (or (vl-catch-all-error-p c) (/= (type c) 'STR) (= c ""))
    (progn
      (setq c (if ade_projgetwscode (vl-catch-all-apply 'ade_projgetwscode nil)))
      (if (or (null c) (vl-catch-all-error-p c) (/= (type c) 'STR) (= c ""))
        (setq c nil)
      )
    )
  )
  c
)

;; "SIRGAS2000.UTM-23S" -> (23 "Sul") ; "UTM84-23S" -> (23 "Sul") ; "SA-SIR-23S" -> (23 "Sul")
(defun kc:parse-utm (code / s p i ch num ult apos indig z)
  (if code
    (setq s (strcase code)
          p (vl-string-search "UTM" s)
          s (cond (p (substr s (+ p 4)))
                  ((wcmatch s "*#S,*#N") s)
                  (T nil)))
  )
  (if s
    (progn
      (setq i 1 num "" ult "" apos "")
      (while (<= i (strlen s))
        (setq ch (substr s i 1))
        (if (wcmatch ch "#")
          (progn (if (not indig) (setq num "")) (setq num (strcat num ch) indig T))
          (progn (if indig (setq ult num apos ch)) (setq indig nil))
        )
        (setq i (1+ i))
      )
      (if indig (setq ult num apos ""))
      (setq z (atoi ult))
      (if (and (> z 0) (<= z 60))
        (list z (cond ((= apos "S") "Sul") ((= apos "N") "Norte") (T nil)))
      )
    )
  )
)

(defun kc:surf-ename (h / e)
  (if (and h (setq e (handent h)) (entget e)
           (member (cdr (assoc 0 (entget e))) '("AECC_TIN_SURFACE" "AECC_GRID_SURFACE")))
    e
  )
)

(defun kc:surfname (e / o n)
  (if (setq o (kc:obj e)) (setq n (vl-catch-all-apply 'vlax-get (list o 'Name))))
  (if (= (type n) 'STR) n "superficie")
)

(defun kc:pick-surface (/ cur e r ok)
  (setq cur (kc:surf-ename (kc:get "SUPERFICIE" nil)))
  (while (not ok)
    (setvar "ERRNO" 0)
    (setq r (entsel (strcat "\nSelecione a superficie do TERRENO NATURAL"
                            (if cur (strcat " <" (kc:surfname cur) ">") "") ": ")))
    (cond
      (r
       (setq e (car r))
       (if (member (cdr (assoc 0 (entget e))) '("AECC_TIN_SURFACE" "AECC_GRID_SURFACE"))
         (setq cur e ok T)
         (princ "\nIsso nao e uma superficie TIN/Grid do Civil 3D.")
       )
      )
      ((= (getvar "ERRNO") 52)
       (if cur (setq ok T) (princ "\nE preciso selecionar uma superficie."))
      )
      (T (princ "\nNada selecionado, tente de novo."))
    )
  )
  (kc:put "SUPERFICIE" (cdr (assoc 5 (entget cur))))
  (princ (strcat "\nTerreno de referencia: " (kc:surfname cur)))
  cur
)

(defun kc:calibrar (off / p zr zg)
  (princ "\nCalibracao: escolha um ponto de chao aberto e plano (rua, patio), longe de predios e arvores.")
  (princ "\nNo Google Earth, pare o mouse nesse lugar e leia a altitude na barra inferior.")
  (if (setq p (getpoint "\nClique o ponto de referencia no desenho: "))
    (progn
      (setq zr (getreal (strcat "\nSua cota real nesse ponto <" (rtos (caddr p) 2 3) ">: ")))
      (if (null zr) (setq zr (caddr p)))
      (initget 1)
      (setq zg (getreal "\nAltitude que o Google Earth mostra nesse ponto (m): "))
      (princ (strcat "\nCorrecao calculada: " (rtos (- zg zr) 2 3) " m"))
      (- zg zr)
    )
    off
  )
)

(defun kc:resumo (/ m)
  (setq m (kc:get "MODO" "Absoluto"))
  (princ "\nRegras de cota deste desenho:")
  (princ (strcat "\n  Modo: " m
                 (cond
                   ((= m "Absoluto")
                    (strcat " (altitude = cota "
                            (if (equal (kc:get "OFFSET" 0.0) 0.0 1e-9)
                              "sem correcao)"
                              (strcat "+ " (rtos (kc:get "OFFSET" 0.0) 2 3) " m)")
                            )
                    )
                   )
                   ((= m "Relativo")
                    (strcat " (altura sobre o TN: "
                            (if (kc:surf-ename (kc:get "SUPERFICIE" nil))
                              (kc:surfname (kc:surf-ename (kc:get "SUPERFICIE" nil)))
                              "superficie nao encontrada"
                            )
                            ")"
                    )
                   )
                   (T " (colado no terreno do Google Earth)")
                 )
         )
  )
  (princ (strcat "\n  UTM fuso " (itoa (kc:get "FUSO" 23)) " " (kc:get "HEMISFERIO" "Sul")
                 " | Densificar: " (rtos (kc:get "PASSO" 5.0) 2 2) " m"
                 " | Cortina: " (kc:get "CORTINA" "Nao")
                 " | Rotulos: " (kc:get "ROTULOS" "Nao")))
  (princ)
)

(defun kc:config (/ code fz fuso hemi modo off r passo cort rot abr)
  (princ "\n=== KMLCOTAS - regras de cota deste desenho ===")
  (setq code (kc:cscode) fz (kc:parse-utm code))
  (if code (princ (strcat "\nSistema de coordenadas do desenho: " code)))
  ;; fuso / hemisferio
  (setq fuso (kc:get "FUSO" (if fz (car fz) 23)))
  (initget 6)
  (if (setq r (getint (strcat "\nFuso UTM <" (itoa fuso) ">: "))) (setq fuso r))
  (setq hemi (kc:get "HEMISFERIO" (if (and fz (cadr fz)) (cadr fz) "Sul")))
  (initget "Norte Sul")
  (if (setq r (getkword (strcat "\nHemisferio [Norte/Sul] <" hemi ">: "))) (setq hemi r))
  ;; modo
  (princ "\n\nModos de altitude:")
  (princ "\n  Absoluto - altitude = cota do projeto (+ correcao). Cotas e rampas exatas.")
  (princ "\n  Relativo - altura sobre o terreno natural (projeto - TN). Assenta no chao do Google Earth.")
  (princ "\n  Chao     - so planimetria, colada no terreno do Google Earth.")
  (setq modo (kc:get "MODO" "Absoluto"))
  (initget "Absoluto Relativo Chao")
  (if (setq r (getkword (strcat "\nModo de altitude [Absoluto/Relativo/Chao] <" modo ">: ")))
    (setq modo r)
  )
  (setq off (kc:get "OFFSET" 0.0))
  (cond
    ((= modo "Absoluto")
     (princ "\nCotas ortometricas (IBGE) ja ficam proximas da referencia do Google Earth (EGM96).")
     (princ "\nUse a correcao para casar com o terreno do Google Earth, ou [Calibrar] por um ponto.")
     (initget "Calibrar")
     (setq r (getreal (strcat "\nCorrecao vertical somada as cotas, em m [Calibrar] <" (rtos off 2 3) ">: ")))
     (cond
       ((= r "Calibrar") (setq off (kc:calibrar off)))
       ((numberp r) (setq off r))
     )
    )
    ((= modo "Relativo") (kc:pick-surface))
  )
  ;; densificacao
  (setq passo (kc:get "PASSO" 5.0))
  (if (/= modo "Chao")
    (progn
      (initget 4)
      (if (setq r (getdist (strcat "\nDensificar as linhas a cada quantos metros (0 = so vertices) <"
                                   (rtos passo 2 2) ">: ")))
        (setq passo r)
      )
    )
  )
  ;; cortina
  (setq cort (kc:get "CORTINA" "Nao"))
  (if (/= modo "Chao")
    (progn
      (initget "Sim Nao")
      (if (setq r (getkword (strcat "\nDesenhar cortina vertical ate o chao [Sim/Nao] <" cort ">: ")))
        (setq cort r)
      )
    )
  )
  ;; rotulos
  (setq rot (kc:get "ROTULOS" "Nao"))
  (initget "Sim Nao")
  (if (setq r (getkword (strcat "\nRotular a cota de cada vertice [Sim/Nao] <" rot ">: ")))
    (setq rot r)
  )
  ;; abrir
  (setq abr (kc:get "ABRIR" "Sim"))
  (initget "Sim Nao")
  (if (setq r (getkword (strcat "\nAbrir no Google Earth ao terminar [Sim/Nao] <" abr ">: ")))
    (setq abr r)
  )
  (mapcar 'kc:put
          '("FUSO" "HEMISFERIO" "MODO" "OFFSET" "PASSO" "CORTINA" "ROTULOS" "ABRIR")
          (list fuso hemi modo (float off) (float passo) cort rot abr)
  )
  (princ "\nRegras gravadas no desenho (salve o DWG para mante-las).")
  (kc:resumo)
  (princ)
)

(defun kc:load-cfg (/ e)
  (setq *kc-modo*   (kc:get "MODO" "Absoluto")
        *kc-off*    (float (kc:get "OFFSET" 0.0))
        *kc-passo*  (float (kc:get "PASSO" 5.0))
        *kc-fuso*   (kc:get "FUSO" 23)
        *kc-sul*    (= (kc:get "HEMISFERIO" "Sul") "Sul")
        *kc-cort*   (= (kc:get "CORTINA" "Nao") "Sim")
        *kc-rot*    (= (kc:get "ROTULOS" "Nao") "Sim")
        *kc-abrir*  (= (kc:get "ABRIR" "Sim") "Sim")
        *kc-arco*   1.0
        *kc-surf*   nil
        *kc-surfnome* ""
  )
  (if (= *kc-modo* "Chao") (setq *kc-cort* nil *kc-passo* 0.0))
  (if (= *kc-modo* "Relativo")
    (if (setq e (kc:surf-ename (kc:get "SUPERFICIE" nil)))
      (setq *kc-surf* (kc:obj e) *kc-surfnome* (kc:surfname e))
    )
  )
  (or (/= *kc-modo* "Relativo") *kc-surf*)
)

;;; --------------------------------------------- UTM -> geograficas -------
;;; Serie de Kruger de 6a ordem, GRS80 (SIRGAS 2000). Erro < 1 mm.

(defun kc:sinh (x) (/ (- (exp x) (exp (- x))) 2.0))
(defun kc:cosh (x) (/ (+ (exp x) (exp (- x))) 2.0))
(defun kc:asin (x) (atan x (sqrt (max 0.0 (- 1.0 (* x x))))))

(defun kc:utm-init (/ a f n n2 n3 n4 n5 n6)
  (setq a 6378137.0
        f (/ 1.0 298.257222101)
        n (/ f (- 2.0 f))
        n2 (* n n) n3 (* n2 n) n4 (* n3 n) n5 (* n4 n) n6 (* n5 n)
  )
  (setq *kc-A* (* (/ a (+ 1.0 n)) (+ 1.0 (/ n2 4.0) (/ n4 64.0) (/ n6 256.0))))
  (setq *kc-beta*
         (list
           (+ (/ n 2.0) (- (/ (* 2.0 n2) 3.0)) (/ (* 37.0 n3) 96.0) (- (/ n4 360.0))
              (- (/ (* 81.0 n5) 512.0)) (/ (* 96199.0 n6) 604800.0))
           (+ (/ n2 48.0) (/ n3 15.0) (- (/ (* 437.0 n4) 1440.0)) (/ (* 46.0 n5) 105.0)
              (- (/ (* 1118711.0 n6) 3870720.0)))
           (+ (/ (* 17.0 n3) 480.0) (- (/ (* 37.0 n4) 840.0)) (- (/ (* 209.0 n5) 4480.0))
              (/ (* 5569.0 n6) 90720.0))
           (+ (/ (* 4397.0 n4) 161280.0) (- (/ (* 11.0 n5) 504.0)) (- (/ (* 830251.0 n6) 7257600.0)))
           (+ (/ (* 4583.0 n5) 161280.0) (- (/ (* 108847.0 n6) 3991680.0)))
           (/ (* 20648693.0 n6) 638668800.0)
         )
  )
  (setq *kc-delta*
         (list
           (+ (* 2.0 n) (- (/ (* 2.0 n2) 3.0)) (* -2.0 n3) (/ (* 116.0 n4) 45.0)
              (/ (* 26.0 n5) 45.0) (- (/ (* 2854.0 n6) 675.0)))
           (+ (/ (* 7.0 n2) 3.0) (- (/ (* 8.0 n3) 5.0)) (- (/ (* 227.0 n4) 45.0))
              (/ (* 2704.0 n5) 315.0) (/ (* 2323.0 n6) 945.0))
           (+ (/ (* 56.0 n3) 15.0) (- (/ (* 136.0 n4) 35.0)) (- (/ (* 1262.0 n5) 105.0))
              (/ (* 73814.0 n6) 2835.0))
           (+ (/ (* 4279.0 n4) 630.0) (- (/ (* 332.0 n5) 35.0)) (- (/ (* 399572.0 n6) 14175.0)))
           (+ (/ (* 4174.0 n5) 315.0) (- (/ (* 144838.0 n6) 6237.0)))
           (/ (* 601676.0 n6) 22275.0)
         )
  )
  (princ)
)

;; retorna (longitude latitude) em graus decimais
(defun kc:utm->ll (ee nn / k0 xi eta xp ep j chi phi lam0 lam)
  (setq k0 0.9996)
  (if *kc-sul* (setq nn (- nn 10000000.0)))
  (setq xi  (/ nn (* k0 *kc-A*))
        eta (/ (- ee 500000.0) (* k0 *kc-A*))
        xp  xi
        ep  eta
        j   1
  )
  (foreach b *kc-beta*
    (setq xp (- xp (* b (sin (* 2.0 j xi)) (kc:cosh (* 2.0 j eta))))
          ep (- ep (* b (cos (* 2.0 j xi)) (kc:sinh (* 2.0 j eta))))
          j  (1+ j)
    )
  )
  (setq chi (kc:asin (/ (sin xp) (kc:cosh ep)))
        phi chi
        j   1
  )
  (foreach d *kc-delta*
    (setq phi (+ phi (* d (sin (* 2.0 j chi))))
          j   (1+ j)
    )
  )
  (setq lam0 (* (- (* *kc-fuso* 6.0) 183.0) (/ pi 180.0))
        lam  (+ lam0 (atan (kc:sinh ep) (cos xp)))
  )
  (list (* lam (/ 180.0 pi)) (* phi (/ 180.0 pi)))
)

(defun kc:utm-ok (p)
  (and p (numberp (car p)) (numberp (cadr p))
       (> (car p) 100000.0) (< (car p) 900000.0)
       (> (cadr p) 0.0) (< (cadr p) 10000000.0))
)

(defun kc:chk (p) (if (not (kc:utm-ok p)) (setq *kc-nbad* (1+ *kc-nbad*))))

;;; ------------------------------------------------------------ texto -----

(defun kc:w (f s) (write-line s f))

;; barra de status e data, sem falhar fora da interface grafica
(defun kc:status (s) (vl-catch-all-apply 'grtext (if s (list -1 s) nil)))
(defun kc:agora (/ r)
  (setq r (vl-catch-all-apply 'menucmd (list "M=$(edtime,$(getvar,date),DD/MO/YYYY HH:MM)")))
  (if (= (type r) 'STR) r "")
)

;; escapa XML e converte acentos em entidades numericas (arquivo 100% ASCII)
(defun kc:esc (s / out)
  (setq out "")
  (if (/= (type s) 'STR) (setq s (vl-princ-to-string s)))
  (foreach c (vl-string->list s)
    (setq out
           (strcat out
                   (cond
                     ((= c 38) "&amp;")
                     ((= c 60) "&lt;")
                     ((= c 62) "&gt;")
                     ((= c 34) "&quot;")
                     ((> c 127) (strcat "&#" (itoa c) ";"))
                     ((< c 32) " ")
                     (T (chr c))
                   )
           )
    )
  )
  out
)

;; numero com virgula decimal (para leitura)
(defun kc:fmt (x d) (vl-string-translate "." "," (rtos x 2 d)))

(defun kc:hex2 (i / h)
  (setq h "0123456789abcdef" i (max 0 (min 255 (fix i))))
  (strcat (substr h (1+ (/ i 16)) 1) (substr h (1+ (rem i 16)) 1))
)

;;; ------------------------------------------------------------ cores -----

;; HSV -> RGB (h em graus, s 0-1, v 0-255)
(defun kc:hsv (h s v / i fr p q u)
  (setq h  (/ h 60.0)
        i  (fix h)
        fr (- h i)
        p  (* v (- 1.0 s))
        q  (* v (- 1.0 (* s fr)))
        u  (* v (- 1.0 (* s (- 1.0 fr))))
  )
  (mapcar 'fix
          (nth (rem i 6)
               (list (list v u p) (list q v p) (list p v u) (list p q v) (list u p v) (list v p q))))
)

;; cor ACI -> RGB (aproximacao da paleta do AutoCAD; so usada sem COM)
(defun kc:aci->rgb (c)
  (cond
    ((null c) '(255 255 255))
    ((= c 1) '(255 0 0))
    ((= c 2) '(255 255 0))
    ((= c 3) '(0 255 0))
    ((= c 4) '(0 255 255))
    ((= c 5) '(0 0 255))
    ((= c 6) '(255 0 255))
    ((= c 8) '(128 128 128))
    ((= c 9) '(192 192 192))
    ((and (>= c 10) (<= c 249))
     (kc:hsv (* 15.0 (1- (/ c 10)))
             (if (= 1 (rem c 2)) 0.5 1.0)
             (nth (/ (rem c 10) 2) '(255.0 204.0 153.0 127.0 76.0)))
    )
    ((and (>= c 250) (<= c 255))
     (setq c (nth (- c 250) '(51 91 132 173 214 255)))
     (list c c c)
    )
    (T '(255 255 255))
  )
)

;; cor pelo DXF (entidade ou layer): true color exata, ACI aproximada
(defun kc:dxf-rgb (e / ed c tc ld)
  (setq ed (entget e)
        c  (cdr (assoc 62 ed))
        tc (cdr (assoc 420 ed))
  )
  (if (or (null c) (= c 256) (= c 0))
    (setq ld (entget (tblobjname "LAYER" (cdr (assoc 8 ed))))
          c  (abs (cdr (assoc 62 ld)))
          tc (cdr (assoc 420 ld))
    )
  )
  (if tc
    (list (logand (lsh tc -16) 255) (logand (lsh tc -8) 255) (logand tc 255))
    (kc:aci->rgb c)
  )
)

(defun kc:rgb (e / o tc rgb)
  (if (setq o (kc:obj e))
    (setq tc (vl-catch-all-apply 'vla-get-TrueColor (list o)))
  )
  (if (or (null tc)
          (vl-catch-all-error-p tc)
          (member (vl-catch-all-apply 'vla-get-ColorMethod (list tc)) '(192 193)))
    (setq tc (vl-catch-all-apply
               '(lambda ()
                  (vla-get-TrueColor
                    (vla-item (vla-get-Layers (vla-get-ActiveDocument (vlax-get-acad-object)))
                              (cdr (assoc 8 (entget e))))
                  )
                )
               nil
             )
    )
  )
  (if (and tc (not (vl-catch-all-error-p tc)))
    (setq rgb (mapcar '(lambda (p) (vl-catch-all-apply 'vlax-get (list tc p))) '(Red Green Blue)))
  )
  (if (and rgb (vl-every 'numberp rgb)) rgb (kc:dxf-rgb e))
)

(defun kc:sid (e / c)
  (setq c (kc:rgb e))
  (strcat "c_" (kc:hex2 (car c)) (kc:hex2 (cadr c)) (kc:hex2 (caddr c)))
)

;; sid = "c_rrggbb" ; cor KML = aabbggrr
(defun kc:style (sid / bgr ic)
  (setq bgr (strcat (substr sid 7 2) (substr sid 5 2) (substr sid 3 2))
        ic  "<Icon><href>http://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon>"
  )
  (strcat
    "<Style id=\"" sid "\"><LineStyle><color>ff" bgr "</color><width>3</width></LineStyle>"
    "<PolyStyle><color>55" bgr "</color></PolyStyle>"
    "<IconStyle><color>ff" bgr "</color><scale>0.7</scale>" ic "</IconStyle>"
    "<LabelStyle><scale>0.8</scale></LabelStyle></Style>\n"
    "<Style id=\"" sid "_corte\"><LineStyle><color>80" bgr "</color><width>2</width></LineStyle></Style>\n"
    "<Style id=\"" sid "_rot\"><IconStyle><color>ff" bgr "</color><scale>0.35</scale>" ic
    "</IconStyle><LabelStyle><scale>0.7</scale></LabelStyle></Style>"
  )
)

;;; -------------------------------------------------------- geometria -----

(defun kc:tipo (e typ)
  (cond
    ((= typ "LINE") "Linha")
    ((= typ "ARC") "Arco")
    ((= typ "CIRCLE") "Circulo")
    ((= typ "LWPOLYLINE") "Polilinha 2D")
    ((= typ "POLYLINE")
     (if (= 8 (logand 8 (cdr (assoc 70 (entget e))))) "Polilinha 3D" "Polilinha 2D")
    )
    ((= typ "SPLINE") "Spline")
    ((= typ "AECC_FEATURE_LINE") "Feature line")
    ((= typ "AECC_SURVEY_FIGURE") "Figura")
    ((= typ "POINT") "Ponto")
    ((= typ "AECC_COGO_POINT") "Ponto COGO")
    (T typ)
  )
)

(defun kc:nome (e typ / o n)
  (if (and (member typ '("AECC_FEATURE_LINE" "AECC_SURVEY_FIGURE")) (setq o (kc:obj e)))
    (setq n (vl-catch-all-apply 'vlax-get (list o 'Name)))
  )
  (if (and (= (type n) 'STR) (/= n ""))
    n
    (strcat (kc:tipo e typ) " " (cdr (assoc 5 (entget e))))
  )
)

(defun kc:group3 (l / r)
  (while (and l (cddr l))
    (setq r (cons (list (car l) (cadr l) (caddr l)) r)
          l (cdddr l)
    )
  )
  (reverse r)
)

;; vertices de feature line / figura pela API COM do Civil 3D
(defun kc:fl-points (e / o r pts)
  (setq o (kc:obj e))
  (foreach ty '(3 1)
    (if (and o (null pts))
      (progn
        (setq r (vl-catch-all-apply 'vlax-invoke (list o 'GetPoints ty)))
        (if (and (not (vl-catch-all-error-p r)) (listp r) (>= (length r) 6))
          (setq pts (kc:group3 r))
        )
      )
    )
  )
  pts
)

(defun kc:pad (e d tot / p)
  (cond
    ((<= d 1e-9) (vlax-curve-getStartPoint e))
    ((>= d (- tot 1e-9)) (vlax-curve-getEndPoint e))
    ((setq p (vlax-curve-getPointAtDist e d)) p)
    (T (vlax-curve-getEndPoint e))
  )
)

(defun kc:clean-dists (ds tot / r)
  (setq ds (vl-sort (mapcar '(lambda (d) (min tot (max 0.0 d))) (append (list 0.0 tot) ds)) '<))
  (foreach d ds
    (if (or (null r) (> (- d (car r)) 1e-6)) (setq r (cons d r)))
  )
  (reverse r)
)

;; amostra a curva: vertices exatos + arcos a cada 1 m + densificacao
;; retorna lista de ((x y z) vertice?)
(defun kc:densify (e ds tot / res d0 d1 p0 p1 pm cm sub d k)
  (setq ds  (kc:clean-dists ds tot)
        res (list (list (kc:pad e (car ds) tot) T))
  )
  (while (cdr ds)
    (setq d0 (car ds)
          d1 (cadr ds)
          ds (cdr ds)
          p0 (kc:pad e d0 tot)
          p1 (kc:pad e d1 tot)
          pm (kc:pad e (/ (+ d0 d1) 2.0) tot)
          cm (mapcar '(lambda (a b) (/ (+ a b) 2.0)) p0 p1)
    )
    (setq sub (cond
                ((> (distance pm cm) 0.001)
                 (if (and (> *kc-passo* 0.0) (< *kc-passo* *kc-arco*)) *kc-passo* *kc-arco*)
                )
                ((> *kc-passo* 0.0) *kc-passo*)
              )
    )
    (if sub
      (progn
        (setq k 1 d (+ d0 sub))
        (while (< d (- d1 (* 0.2 sub)))
          (setq res (cons (list (kc:pad e d tot) nil) res)
                k   (1+ k)
                d   (+ d0 (* k sub))
          )
        )
      )
    )
    (setq res (cons (list p1 T) res))
  )
  (reverse res)
)

(defun kc:samples-curve (e typ / ed sp ep tot ds i pts)
  (setq ed  (entget e)
        sp  (vlax-curve-getStartParam e)
        ep  (vlax-curve-getEndParam e)
        tot (vlax-curve-getDistAtParam e ep)
  )
  (cond
    ((and (member typ '("LWPOLYLINE" "POLYLINE"))
          (= 0 (logand (cdr (assoc 70 ed)) 6)))
     (setq i (fix (+ sp 0.5)))
     (while (<= i (+ ep 1e-8))
       (setq ds (cons (vlax-curve-getDistAtParam e i) ds)
             i  (1+ i)
       )
     )
    )
    ((and (member typ '("AECC_FEATURE_LINE" "AECC_SURVEY_FIGURE"))
          (setq pts (kc:fl-points e)))
     (setq ds (mapcar '(lambda (p) (vlax-curve-getDistAtPoint e (vlax-curve-getClosestPointTo e p))) pts))
    )
  )
  (kc:densify e ds tot)
)

;; plano B: vertices lidos pela COM, segmentos retos
(defun kc:samples-pts (pts / res p0 p1 L k fr)
  (if (cdr pts)
    (progn
      (setq res (list (list (car pts) T)))
      (while (cdr pts)
        (setq p0  (car pts)
              p1  (cadr pts)
              pts (cdr pts)
              L   (distance (list (car p0) (cadr p0)) (list (car p1) (cadr p1)))
              k   1
        )
        (if (> *kc-passo* 0.0)
          (while (< (* k *kc-passo*) (- L (* 0.2 *kc-passo*)))
            (setq fr  (/ (* k *kc-passo*) L)
                  res (cons (list (mapcar '(lambda (a b) (+ a (* fr (- b a)))) p0 p1) nil) res)
                  k   (1+ k)
            )
          )
        )
        (setq res (cons (list p1 T) res))
      )
      (reverse res)
    )
  )
)

(defun kc:samples (e typ / r)
  (setq r (vl-catch-all-apply 'kc:samples-curve (list e typ)))
  (if (or (vl-catch-all-error-p r) (< (length r) 2))
    (if (member typ '("AECC_FEATURE_LINE" "AECC_SURVEY_FIGURE"))
      (setq r (kc:samples-pts (kc:fl-points e)))
      (setq r nil)
    )
  )
  r
)

(defun kc:repr-pt (e typ / r)
  (cond
    ((= typ "AECC_COGO_POINT") (car (kc:cogo e)))
    ((= typ "POINT") (cdr (assoc 10 (entget e))))
    (T
     (setq r (vl-catch-all-apply 'vlax-curve-getStartPoint (list e)))
     (if (or (vl-catch-all-error-p r) (null r)) (car (kc:fl-points e)) r)
    )
  )
)

;;; ---------------------------------------------------------- pontos ------

(defun kc:prop (e o p / r)
  (setq r (vl-catch-all-apply 'vlax-get (list o p)))
  (if (vl-catch-all-error-p r)
    (setq r (vl-catch-all-apply 'getpropertyvalue (list e (vl-symbol-name p))))
  )
  (if (vl-catch-all-error-p r) nil r)
)

;; retorna ((x y z|nil) numero descricao)
(defun kc:cogo (e / o x y z)
  (setq o (kc:obj e)
        x (kc:prop e o 'Easting)
        y (kc:prop e o 'Northing)
        z (kc:prop e o 'Elevation)
  )
  (if (or (not (numberp z)) (< z -1.0e10)) (setq z nil))
  (list (if (and (numberp x) (numberp y)) (list x y z))
        (kc:prop e o 'Number)
        (kc:prop e o 'RawDescription)
  )
)

;;; ---------------------------------------------------------- altitude ----

(defun kc:zterr (x y / r)
  (setq r (vl-catch-all-apply 'vlax-invoke (list *kc-surf* 'FindElevationAtXY x y)))
  (if (= (type r) 'VARIANT) (setq r (vlax-variant-value r)))
  (cond
    ((vl-catch-all-error-p r) nil)
    ((not (numberp r)) nil)
    ((< r -9999.0) nil)
    (T r)
  )
)

;; retorna (altitude dz) conforme a regra do desenho
(defun kc:alt (p / zt)
  (cond
    ((= *kc-modo* "Absoluto") (list (+ (caddr p) *kc-off*) nil))
    ((= *kc-modo* "Relativo")
     (if (setq zt (kc:zterr (car p) (cadr p)))
       (list (- (caddr p) zt) (- (caddr p) zt))
       (progn (setq *kc-nfora* (1+ *kc-nfora*)) (list 0.0 nil))
     )
    )
    (T (list 0.0 nil))
  )
)

;;; ---------------------------------------------------------- KML ---------

(defun kc:pm-line (f nome desc sid am runs extr)
  (kc:w f "<Placemark>")
  (kc:w f (strcat "<name>" (kc:esc nome) "</name>"))
  (if desc (kc:w f (strcat "<description><![CDATA[" desc "]]></description>")))
  (kc:w f (strcat "<styleUrl>#" sid "</styleUrl>"))
  (kc:w f "<MultiGeometry>")
  (foreach run runs
    (kc:w f "<LineString>")
    (if extr (kc:w f "<extrude>1</extrude>"))
    (if (= am "clampToGround") (kc:w f "<tessellate>1</tessellate>"))
    (kc:w f (strcat "<altitudeMode>" am "</altitudeMode>"))
    (princ "<coordinates>" f)
    (foreach c run
      (princ (strcat (rtos (car c) 2 9) "," (rtos (cadr c) 2 9) "," (rtos (caddr c) 2 3) " ") f)
    )
    (kc:w f "</coordinates>")
    (kc:w f "</LineString>")
  )
  (kc:w f "</MultiGeometry>")
  (kc:w f "</Placemark>")
)

(defun kc:pm-pt (nome desc sid am lon lat alt)
  (strcat "<Placemark><name>" (kc:esc nome) "</name>"
          (if desc (strcat "<description><![CDATA[" desc "]]></description>") "")
          "<styleUrl>#" sid "</styleUrl><Point><altitudeMode>" am "</altitudeMode><coordinates>"
          (rtos lon 2 9) "," (rtos lat 2 9) "," (rtos alt 2 3)
          "</coordinates></Point></Placemark>"
  )
)

(defun kc:c-abs (recs) (mapcar '(lambda (r) (list (car r) (cadr r) (nth 2 r))) recs))
(defun kc:c-rel (recs) (mapcar '(lambda (r) (list (car r) (cadr r) (max 0.0 (nth 2 r)))) recs))
(defun kc:c-chao (recs) (mapcar '(lambda (r) (list (car r) (cadr r) 0.0)) recs))

(defun kc:dzv (r) (if (nth 4 r) (nth 4 r) 0.0))

;; separa trechos em aterro ("A") e corte ("C")
(defun kc:runs (recs / prev cur cls c runs)
  (setq prev (car recs))
  (foreach r (cdr recs)
    (setq c (if (< (+ (kc:dzv prev) (kc:dzv r)) 0.0) "C" "A"))
    (cond
      ((null cls) (setq cls c cur (list r prev)))
      ((= c cls) (setq cur (cons r cur)))
      (T (setq runs (cons (cons cls (reverse cur)) runs)
               cls  c
               cur  (list r prev)
         )
      )
    )
    (setq prev r)
  )
  (if cls (setq runs (cons (cons cls (reverse cur)) runs)))
  (reverse runs)
)

(defun kc:desc-line (e typ lay l2d zi zf zmn zmx dzmx dzmn)
  (strcat
    "<b>Layer:</b> " (kc:esc lay)
    "<br><b>Objeto:</b> " (kc:tipo e typ) " (handle " (cdr (assoc 5 (entget e))) ")"
    "<br><b>Comprimento 2D:</b> " (kc:fmt l2d 3) " m"
    "<br><b>Cota inicial:</b> " (kc:fmt zi 3) " m"
    "<br><b>Cota final:</b> " (kc:fmt zf 3) " m"
    "<br><b>Cota m&#237;nima:</b> " (kc:fmt zmn 3) " m"
    "<br><b>Cota m&#225;xima:</b> " (kc:fmt zmx 3) " m"
    "<br><b>Rampa m&#233;dia:</b> "
    (if (> l2d 0.001) (strcat (kc:fmt (/ (* 100.0 (- zf zi)) l2d) 3) " %") "-")
    (cond
      ((= *kc-modo* "Relativo")
       (strcat "<br><b>Refer&#234;ncia:</b> superf&#237;cie " (kc:esc *kc-surfnome*)
               "<br><b>Aterro m&#225;ximo:</b> "
               (if (and dzmx (> dzmx 0.0)) (strcat (kc:fmt dzmx 3) " m") "-")
               "<br><b>Corte m&#225;ximo:</b> "
               (if (and dzmn (< dzmn 0.0)) (strcat (kc:fmt (abs dzmn) 3) " m") "-")
       )
      )
      ((= *kc-modo* "Absoluto")
       (strcat "<br><b>Altitude no Google Earth:</b> cota "
               (if (equal *kc-off* 0.0 1e-9)
                 "sem corre&#231;&#227;o"
                 (strcat (if (< *kc-off* 0.0) "- " "+ ") (kc:fmt (abs *kc-off*) 3) " m")
               )
       )
      )
      (T "<br><b>Altitude:</b> colado no terreno do Google Earth")
    )
  )
)

(defun kc:lbl (r sid / txt dz am alt)
  (setq txt (kc:fmt (nth 3 r) 3)
        dz  (nth 4 r)
  )
  (if (and (= *kc-modo* "Relativo") dz)
    (setq txt (strcat txt " | " (if (< dz 0.0) "C " "A ") (kc:fmt (abs dz) 2)))
  )
  (cond
    ((= *kc-modo* "Absoluto") (setq am "absolute" alt (nth 2 r)))
    ((and (= *kc-modo* "Relativo") dz (>= dz 0.0)) (setq am "relativeToGround" alt dz))
    (T (setq am "clampToGround" alt 0.0))
  )
  (kc:pm-pt txt nil (strcat sid "_rot") am (car r) (cadr r) alt)
)

(defun kc:proc-curve (f e lay typ sid / smp recs p ll a zi zf zmn zmx l2d prev dzmx dzmn
                                        runs aruns cruns nome desc dz ultr)
  (setq smp (kc:samples e typ))
  (if (< (length smp) 2)
    (setq *kc-nfalha* (1+ *kc-nfalha*))
    (progn
      ;; rec = (lon lat altitude cota dz vertice?)
      (foreach s smp
        (setq p  (car s)
              ll (kc:utm->ll (car p) (cadr p))
              a  (kc:alt p)
        )
        (kc:chk p)
        (setq recs (cons (list (car ll) (cadr ll) (car a) (caddr p) (cadr a) (cadr s)) recs))
      )
      (setq recs (reverse recs) ultr (last recs))
      ;; estatisticas com as coordenadas originais
      (setq zi  (caddr (car (car smp)))
            zf  (caddr (car (last smp)))
            zmn zi
            zmx zi
            l2d 0.0
      )
      (foreach s smp
        (setq p (car s))
        (if prev
          (setq l2d (+ l2d (distance (list (car prev) (cadr prev)) (list (car p) (cadr p)))))
        )
        (setq zmn  (min zmn (caddr p))
              zmx  (max zmx (caddr p))
              prev p
        )
      )
      (foreach r recs
        (if (setq dz (nth 4 r))
          (progn
            (if (or (null dzmx) (> dz dzmx)) (setq dzmx dz))
            (if (or (null dzmn) (< dz dzmn)) (setq dzmn dz))
          )
        )
      )
      (setq nome (kc:nome e typ)
            desc (kc:desc-line e typ lay l2d zi zf zmn zmx dzmx dzmn)
      )
      (cond
        ((= *kc-modo* "Relativo")
         (foreach r (kc:runs recs)
           (if (= (car r) "C")
             (setq cruns (cons (cdr r) cruns))
             (setq aruns (cons (cdr r) aruns))
           )
         )
         (if aruns
           (kc:pm-line f nome desc sid "relativeToGround"
                       (mapcar 'kc:c-rel (reverse aruns)) *kc-cort*)
         )
         (if cruns
           (kc:pm-line f (strcat nome " (corte)") desc (strcat sid "_corte") "clampToGround"
                       (mapcar 'kc:c-chao (reverse cruns)) nil)
         )
        )
        ((= *kc-modo* "Absoluto")
         (kc:pm-line f nome desc sid "absolute" (list (kc:c-abs recs)) *kc-cort*)
        )
        (T (kc:pm-line f nome desc sid "clampToGround" (list (kc:c-chao recs)) nil))
      )
      (if *kc-rot*
        (foreach r recs
          (if (and (nth 5 r) (not (and (eq r ultr) (equal (list (car r) (cadr r)) (list (caar recs) (cadar recs)) 1e-10)))) (setq *kc-lbls* (cons (kc:lbl r sid) *kc-lbls*)))
        )
      )
      (setq *kc-nobj* (1+ *kc-nobj*))
    )
  )
)

(defun kc:proc-pt (f e lay typ sid / info p z num desc ll a dz am alt nome d)
  (if (= typ "AECC_COGO_POINT")
    (setq info (kc:cogo e))
    (setq info (list (cdr (assoc 10 (entget e))) nil nil))
  )
  (setq p    (car info)
        num  (cadr info)
        desc (caddr info)
  )
  (if (null p)
    (setq *kc-nfalha* (1+ *kc-nfalha*))
    (progn
      (setq z  (caddr p)
            ll (kc:utm->ll (car p) (cadr p))
      )
      (kc:chk p)
      (cond
        ((null z) (setq am "clampToGround" alt 0.0))
        ((= *kc-modo* "Absoluto") (setq am "absolute" alt (+ z *kc-off*)))
        ((= *kc-modo* "Relativo")
         (setq a  (kc:alt p)
               dz (cadr a)
         )
         (if (and dz (>= dz 0.0))
           (setq am "relativeToGround" alt dz)
           (setq am "clampToGround" alt 0.0)
         )
        )
        (T (setq am "clampToGround" alt 0.0))
      )
      (setq nome (if num
                   (strcat (vl-princ-to-string num)
                           (if (and (= (type desc) 'STR) (/= desc "")) (strcat " - " desc) ""))
                   (strcat (kc:tipo e typ) " " (cdr (assoc 5 (entget e))))
                 )
      )
      (if *kc-rot* (setq nome (strcat nome " | " (if z (kc:fmt z 3) "sem cota"))))
      (setq d (strcat "<b>Layer:</b> " (kc:esc lay)
                      "<br><b>E:</b> " (kc:fmt (car p) 3)
                      "<br><b>N:</b> " (kc:fmt (cadr p) 3)
                      "<br><b>Cota:</b> " (if z (strcat (kc:fmt z 3) " m") "sem cota")
                      (if dz
                        (strcat "<br><b>" (if (< dz 0.0) "Abaixo" "Acima")
                                " do terreno:</b> " (kc:fmt (abs dz) 3) " m")
                        ""
                      )
              )
      )
      (kc:w f (kc:pm-pt nome d sid am (car ll) (cadr ll) alt))
      (setq *kc-npt* (1+ *kc-npt*))
    )
  )
)

(defun kc:proc (f e lay typ sid)
  (if (member typ '("AECC_COGO_POINT" "POINT"))
    (kc:proc-pt f e lay typ sid)
    (kc:proc-curve f e lay typ sid)
  )
)

(defun kc:close-layer (f)
  (if *kc-lbls*
    (progn
      (kc:w f "<Folder><name>Cotas dos v&#233;rtices</name>")
      (foreach s (reverse *kc-lbls*) (kc:w f s))
      (kc:w f "</Folder>")
    )
  )
  (setq *kc-lbls* nil)
  (kc:w f "</Folder>")
)

(defun kc:uniq (l / r)
  (foreach x l (if (not (member x r)) (setq r (cons x r))))
  (reverse r)
)

;;; ---------------------------------------------------------- exportacao --

;; lista ordenada por layer: (layer ename tipo estilo)
(defun kc:lista (ss / i e ed typ lst)
  (setq i 0)
  (repeat (sslength ss)
    (setq e   (ssname ss i)
          i   (1+ i)
          ed  (entget e)
          typ (cdr (assoc 0 ed))
    )
    (if (not (and (= typ "POLYLINE") (/= 0 (logand (cdr (assoc 70 ed)) 80))))
      (setq lst (cons (list (cdr (assoc 8 ed)) e typ (kc:sid e)) lst))
    )
  )
  (vl-sort lst '(lambda (a b) (< (strcase (car a)) (strcase (car b)))))
)

;; grava o KML. Requer kc:load-cfg e kc:utm-init. Retorna T se gravou.
(defun kc:exportar (lst path / p f curlay sids n tot modo-txt data)
  (setq p (kc:repr-pt (cadr (car lst)) (caddr (car lst))))
  (cond
    ((and p (not (kc:utm-ok p)))
     (princ "\nAs coordenadas nao parecem UTM em metros (E entre 100.000 e 900.000, N entre 0 e 10.000.000).")
     (princ "\nO desenho esta em sistema local, transladado ou em outra unidade? Exportacao cancelada.")
     nil
    )
    ((null (setq *kc-f* (open path "w")))
     (princ "\nNao foi possivel gravar o arquivo (esta aberto em outro programa?).")
     nil
    )
    (T
     (setq *kc-dimzin* (getvar "DIMZIN"))
     (setvar "DIMZIN" 0)
     (setq f           *kc-f*
           sids        (kc:uniq (mapcar 'cadddr lst))
           *kc-lbls*   nil
           *kc-nobj*   0
           *kc-npt*    0
           *kc-nfora*  0
           *kc-nfalha* 0
           *kc-nbad*   0
           tot         (length lst)
           n           0
           data        (kc:agora)
           modo-txt    (cond
                         ((= *kc-modo* "Absoluto")
                          (strcat "absoluto (altitude = cota"
                                  (if (equal *kc-off* 0.0 1e-9)
                                    ")"
                                    (strcat (if (< *kc-off* 0.0) " - " " + ") (kc:fmt (abs *kc-off*) 3) " m)")
                                  )
                          )
                         )
                         ((= *kc-modo* "Relativo")
                          (strcat "relativo ao terreno natural (superf&#237;cie " (kc:esc *kc-surfnome*) ")")
                         )
                         (T "colado no terreno do Google Earth")
                       )
     )
     (kc:w f "<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
     (kc:w f "<kml xmlns=\"http://www.opengis.net/kml/2.2\">")
     (kc:w f "<Document>")
     (kc:w f (strcat "<name>" (kc:esc (vl-filename-base (getvar "DWGNAME"))) " - cotas</name>"))
     (kc:w f "<open>1</open>")
     (kc:w f (strcat "<description><![CDATA[Gerado pelo KMLCOTAS em " data
                     "<br>Desenho: " (kc:esc (getvar "DWGNAME"))
                     "<br>Coordenadas: UTM fuso " (itoa *kc-fuso*) (if *kc-sul* " Sul" " Norte") " (SIRGAS 2000)"
                     "<br>Altitude: " modo-txt "]]></description>"))
     (foreach s sids (kc:w f (kc:style s)))
     (foreach it lst
       (setq n (1+ n))
       (if (= 0 (rem n 25)) (kc:status (strcat "KMLCOTAS: " (itoa n) " de " (itoa tot))))
       (if (/= (car it) curlay)
         (progn
           (if curlay (kc:close-layer f))
           (setq curlay (car it))
           (kc:w f (strcat "<Folder><name>" (kc:esc curlay) "</name>"))
         )
       )
       (kc:proc f (cadr it) (car it) (caddr it) (cadddr it))
     )
     (if curlay (kc:close-layer f))
     (kc:w f "</Document>")
     (kc:w f "</kml>")
     (close f)
     (setq *kc-f* nil)
     (setvar "DIMZIN" *kc-dimzin*)
     (setq *kc-dimzin* nil)
     (kc:status nil)
     (princ (strcat "\nKML gravado: " path))
     (princ (strcat "\n  Linhas: " (itoa *kc-nobj*) "  |  Pontos: " (itoa *kc-npt*)))
     (if (> *kc-nfora* 0)
       (princ (strcat "\n  Aviso: " (itoa *kc-nfora*)
                      " amostras fora da superficie de terreno (desenhadas no chao).")))
     (if (> *kc-nfalha* 0)
       (princ (strcat "\n  Aviso: " (itoa *kc-nfalha*)
                      " objetos nao puderam ser lidos (converta em polilinha 3D).")))
     (if (> *kc-nbad* 0)
       (princ (strcat "\n  Aviso: " (itoa *kc-nbad*)
                      " vertices com coordenadas fora da faixa UTM - confira o desenho.")))
     T
    )
  )
)

;;; ---------------------------------------------------------- comandos ----

(defun c:KMLCOTASCFG () (kc:config) (princ))

(defun c:KMLCOTAS (/ *error* ss lst path)
  (defun *error* (msg)
    (if *kc-f* (progn (close *kc-f*) (setq *kc-f* nil)))
    (if *kc-dimzin* (progn (setvar "DIMZIN" *kc-dimzin*) (setq *kc-dimzin* nil)))
    (kc:status nil)
    (if (and msg (not (wcmatch (strcase msg) "*CANCEL*,*QUIT*,*EXIT*,*BREAK*")))
      (princ (strcat "\nKMLCOTAS - erro: " msg))
    )
    (princ)
  )
  (princ "\nKMLCOTAS v1.0 - Google Earth respeitando as cotas do projeto")
  ;; regras do desenho
  (if (null (kc:get "MODO" nil))
    (progn (princ "\nPrimeiro uso neste desenho: defina as regras de cota.") (kc:config))
    (progn
      (kc:resumo)
      (initget "Exportar Configurar")
      (if (= (getkword "\nOpcao [Exportar/Configurar] <Exportar>: ") "Configurar") (kc:config))
    )
  )
  (if (not (kc:load-cfg))
    (progn
      (princ "\nA superficie de terreno configurada nao foi encontrada neste desenho.")
      (kc:pick-surface)
      (if (not (kc:load-cfg)) (progn (princ "\nNao foi possivel ler a superficie pela API do Civil 3D. Use o modo Absoluto (KMLCOTASCFG).") (exit)))
    )
  )
  (kc:utm-init)
  ;; objetos
  (princ "\nSelecione os objetos do projeto (linhas, polilinhas 2D/3D, feature lines, arcos, splines, pontos, pontos COGO)")
  (cond
    ((null (setq ss (ssget '((0 . "LINE,ARC,CIRCLE,LWPOLYLINE,POLYLINE,SPLINE,POINT,AECC_FEATURE_LINE,AECC_SURVEY_FIGURE,AECC_COGO_POINT")))))
     (princ "\nNada selecionado.")
    )
    ((null (setq lst (kc:lista ss)))
     (princ "\nNenhum objeto exportavel.")
    )
    (T
     ;; arquivo
     (setq path (kc:get "ARQUIVO" nil))
     (if (or (null path) (not (vl-file-directory-p (vl-filename-directory path))))
       (setq path (strcat (getvar "DWGPREFIX") (vl-filename-base (getvar "DWGNAME")) "_cotas.kml"))
     )
     (if (setq path (getfiled "Salvar KML para o Google Earth" path "kml" 1))
       (progn
         (kc:put "ARQUIVO" path)
         (if (and (kc:exportar lst path) *kc-abrir*)
           (startapp "explorer" (strcat "\"" path "\""))
         )
       )
     )
    )
  )
  (princ)
)

(princ "\nKMLCOTAS carregado. Comandos: KMLCOTAS (exportar) e KMLCOTASCFG (regras de cota).")
(princ)
