;;; ================================================================
;;; ExportHatchesToKML.lsp  -  v3.0
;;; Exporta hachuras do Civil 3D para KML (Google Earth), com altura e
;;; extrusao 3D opcionais. Substitui as versoes v2.0 (padrao e Advanced).
;;;
;;; Comandos:
;;;   EXPORTHATCHESKML  - exporta as hachuras selecionadas (Enter = todas do Model)
;;;   TESTHATCHKML      - diagnostico de UMA hachura (contornos, vertices, lat/long)
;;;
;;; Requisitos:
;;;   - Civil 3D (usa as funcoes ade_proj* do Map 3D, que ja vem no Civil 3D)
;;;   - Desenho com sistema de coordenadas atribuido (MAPCSASSIGN),
;;;     ou informe o codigo quando o comando pedir.
;;;   - Rodar na aba Model.
;;;
;;; Como funciona:
;;;   1. Recria o contorno de cada hachura como polilinha temporaria
;;;      (-HATCHEDIT > recreate Boundary), le os vertices e apaga a temporaria.
;;;      Arcos, circulos, elipses e splines sao subdivididos.
;;;   2. Separa contorno externo e furos (ilhas) pela area e por ponto-no-poligono.
;;;   3. Converte X,Y do desenho para Longitude/Latitude WGS84 (LL84).
;;;   4. Grava o KML em UTF-8, com altitude e extrusao conforme escolhido.
;;; ================================================================

(vl-load-com)

;;; ---------------- utilitarios ----------------

(defun hk:replace-all (s old new / pos)
  (setq pos 0)
  (while (setq pos (vl-string-search old s pos))
    (setq s   (strcat (substr s 1 pos) new (substr s (+ pos 1 (strlen old))))
          pos (+ pos (strlen new))
    )
  )
  s
)

;; Escapa texto para dentro de tags XML (fora de CDATA)
(defun hk:xml (s)
  (setq s (vl-princ-to-string s))
  (setq s (hk:replace-all s "&" "&amp;"))
  (setq s (hk:replace-all s "<" "&lt;"))
  (hk:replace-all s ">" "&gt;")
)

(defun hk:w (f s) (write-line s f))

(defun hk:2d (p) (list (car p) (cadr p)))

(defun hk:save-vars (names)
  (mapcar '(lambda (n) (cons n (getvar n))) names)
)

(defun hk:restore (lst)
  (foreach p lst (vl-catch-all-apply 'setvar (list (car p) (cdr p))))
)

;;; ---------------- geometria ----------------

;; Remove pontos repetidos em sequencia e o ultimo se for igual ao primeiro
(defun hk:clean-ring (pts / out)
  (foreach p pts
    (if (or (null out) (> (distance (hk:2d p) (hk:2d (car out))) 1e-8))
      (setq out (cons p out))
    )
  )
  (setq out (reverse out))
  (if (and (> (length out) 1)
           (< (distance (hk:2d (car out)) (hk:2d (last out))) 1e-8))
    (setq out (reverse (cdr (reverse out))))
  )
  out
)

;; Pontos ao longo de uma curva fechada (WCS).
;; Polilinha: segmentos retos usam so o vertice; segmentos em arco sao subdivididos.
;; Circulo/elipse/spline: 72 pontos.
(defun hk:curve-pts (e / typ sp ep nseg i j d1 d2 pts n)
  (setq typ (cdr (assoc 0 (entget e)))
        sp  (vlax-curve-getStartParam e)
        ep  (vlax-curve-getEndParam e)
  )
  (if (member typ '("LWPOLYLINE" "POLYLINE"))
    (progn
      (setq nseg (fix (+ ep 1e-6))
            i    0
      )
      (while (< i nseg)
        (setq d1 (- (vlax-curve-getDistAtParam e (1+ i)) (vlax-curve-getDistAtParam e i))
              d2 (distance (vlax-curve-getPointAtParam e i) (vlax-curve-getPointAtParam e (1+ i)))
        )
        (if (> (- d1 d2) 1e-6)
          (progn
            (setq j 0)
            (repeat 16
              (setq pts (cons (vlax-curve-getPointAtParam e (+ i (/ j 16.0))) pts)
                    j   (1+ j)
              )
            )
          )
          (setq pts (cons (vlax-curve-getPointAtParam e i) pts))
        )
        (setq i (1+ i))
      )
    )
    (progn
      (setq n 72
            j 0
      )
      (repeat n
        (setq pts (cons (vlax-curve-getPointAtParam e (+ sp (* (- ep sp) (/ j (float n))))) pts)
              j   (1+ j)
        )
      )
    )
  )
  (hk:clean-ring (reverse pts))
)

;; Area 2D de um anel (formula do laco)
(defun hk:ring-area (r / a p)
  (setq a 0.0
        p (last r)
  )
  (foreach q r
    (setq a (+ a (- (* (car p) (cadr q)) (* (car q) (cadr p))))
          p q
    )
  )
  (abs (/ a 2.0))
)

;; Ponto dentro de anel (ray casting 2D)
(defun hk:pt-in-ring (pt r / x y inside a)
  (setq x      (car pt)
        y      (cadr pt)
        inside nil
        a      (last r)
  )
  (foreach b r
    (if (and (/= (> (cadr a) y) (> (cadr b) y))
             (< x (+ (car a) (/ (* (- (car b) (car a)) (- y (cadr a))) (- (cadr b) (cadr a))))))
      (setq inside (not inside))
    )
    (setq a b)
  )
  inside
)

;; Agrupa aneis em poligonos: cada item = (externo furo1 furo2 ...)
;; Aneis separados viram poligonos separados; anel dentro de um externo vira furo.
(defun hk:group-polys (loops / sorted polys placed)
  (setq sorted (mapcar '(lambda (i) (nth i loops))
                       (vl-sort-i (mapcar 'hk:ring-area loops) '>)))
  (foreach r sorted
    (setq placed nil)
    (setq polys
      (mapcar
        '(lambda (poly)
           (if (and (not placed)
                    (hk:pt-in-ring (car r) (car poly))
                    (not (vl-some '(lambda (h) (hk:pt-in-ring (car r) h)) (cdr poly))))
             (progn (setq placed T) (append poly (list r)))
             poly
           )
         )
        polys
      )
    )
    (if (not placed) (setq polys (append polys (list (list r)))))
  )
  polys
)

;;; ---------------- contorno da hachura ----------------

(setq hk:*temps* nil)

(defun hk:cleanup-temps ()
  (foreach e hk:*temps* (if (and e (entget e)) (entdel e)))
  (setq hk:*temps* nil)
)

;; Recria o contorno como polilinhas temporarias, le os aneis e apaga as temporarias.
;; Retorna lista de aneis (listas de pontos WCS).
(defun hk:get-loops (hent / mark e loops)
  (setq hk:*temps* nil
        mark       (entlast)
  )
  (command "_.-HATCHEDIT" hent "_B" "_P" "_N")
  (repeat 3 (if (= 1 (logand 1 (getvar "CMDACTIVE"))) (command)))
  (setq e (entnext mark))
  (while e
    (if (member (cdr (assoc 0 (entget e))) '("LWPOLYLINE" "POLYLINE" "CIRCLE" "ELLIPSE" "SPLINE"))
      (setq hk:*temps* (cons e hk:*temps*))
    )
    (setq e (entnext e))
  )
  (foreach e hk:*temps*
    (setq loops (cons (hk:curve-pts e) loops))
  )
  (hk:cleanup-temps)
  (vl-remove-if '(lambda (r) (< (length r) 3)) loops)
)

;;; ---------------- coordenadas ----------------

;; Configura conversao Desenho -> LL84 (WGS84). Retorna o codigo do sistema ou nil.
(defun hk:setup-cs (/ cs r)
  (if (not ade_projsetsrc)
    (progn
      (princ "\nERRO: funcoes de coordenadas do Map 3D (ade_proj*) indisponiveis. Rode no Civil 3D.")
      nil
    )
    (progn
      (setq cs (vl-catch-all-apply 'ade_projgetwscode))
      (if (or (vl-catch-all-error-p cs) (not cs)) (setq cs ""))
      (if (= cs "")
        (progn
          (princ "\nO desenho nao tem sistema de coordenadas atribuido (veja MAPCSASSIGN).")
          (setq cs (getstring "\nCodigo do sistema de coordenadas do desenho <SIRGAS2000.UTM-23S>: "))
          (if (= cs "") (setq cs "SIRGAS2000.UTM-23S"))
        )
      )
      (setq r (vl-catch-all-apply '(lambda () (and (ade_projsetsrc cs) (ade_projsetdest "LL84")))))
      (if (and r (not (vl-catch-all-error-p r)))
        (progn (princ (strcat "\nSistema de coordenadas: " cs " -> LL84 (WGS84)")) cs)
        (progn
          (princ (strcat "\nERRO: codigo de sistema de coordenadas invalido: " cs
                         "\nConfira o codigo em MAPCSASSIGN e rode de novo."))
          nil
        )
      )
    )
  )
)

;; Converte um anel para (lon lat z). Retorna nil se algum ponto falhar ou sair da faixa valida.
(defun hk:to-ll (r / out q ok)
  (setq ok T)
  (foreach p r
    (if ok
      (progn
        (setq q (vl-catch-all-apply 'ade_projptforward (list (list (car p) (cadr p) 0.0))))
        (if (and q
                 (not (vl-catch-all-error-p q))
                 (<= (abs (car q)) 180.0)
                 (<= (abs (cadr q)) 90.0))
          (setq out (cons q out))
          (setq ok nil)
        )
      )
    )
  )
  (if ok (reverse out))
)

;;; ---------------- altura ----------------

;; Primeiro valor numerico de uma lista de pares de XData
(defun hk:first-num (pairs / v)
  (while (and pairs (not v))
    (cond
      ((member (caar pairs) '(1040 1041 1042 1070 1071))
       (setq v (float (cdar pairs))))
      ((and (= (caar pairs) 1000) (distof (cdar pairs) 2))
       (setq v (distof (cdar pairs) 2)))
    )
    (setq pairs (cdr pairs))
  )
  v
)

;; Le um numero da XData: aplicativo com esse nome, ou texto (1000) com esse nome seguido de numero
(defun hk:xdata-number (ename key / xd pairs p val)
  (setq xd (assoc -3 (entget ename '("*"))))
  (foreach app (cdr xd)
    (if (not val)
      (if (wcmatch (strcase (car app)) (strcase key))
        (setq val (hk:first-num (cdr app)))
        (progn
          (setq pairs (cdr app))
          (while (and pairs (not val))
            (setq p     (car pairs)
                  pairs (cdr pairs)
            )
            (if (and (= (car p) 1000) (wcmatch (strcase (cdr p)) (strcat "*" (strcase key) "*")))
              (setq val (hk:first-num pairs))
            )
          )
        )
      )
    )
  )
  val
)

;; Menu de configuracao. Usa as variaveis locais do comando principal (escopo dinamico).
(defun hk:config (/ k def)
  (initget "Fixa Elevacao Xdata Nenhuma")
  (setq k (getkword "\nFonte da altura [Fixa/Elevacao/Xdata/Nenhuma] <Fixa>: "))
  (setq hk:src (cond (k) ("Fixa")))
  (cond
    ((= hk:src "Fixa")
     (initget 4)
     (setq hk:height (getreal "\nAltura de extrusao em metros <10>: "))
     (if (not hk:height) (setq hk:height 10.0))
    )
    ((= hk:src "Xdata")
     (setq hk:key (getstring "\nNome do aplicativo/campo XData com a altura <HEIGHT>: "))
     (if (= hk:key "") (setq hk:key "HEIGHT"))
    )
  )
  (if (= hk:src "Nenhuma")
    (setq hk:mode "clampToGround"
          hk:extr nil
    )
    (progn
      (setq def (if (= hk:src "Elevacao") "Absoluto" "Relativo"))
      (initget "Relativo Absoluto Chao")
      (setq k (getkword (strcat "\nModo de altitude [Relativo/Absoluto/Chao] <" def ">: ")))
      (if (not k) (setq k def))
      (setq hk:mode (cdr (assoc k '(("Relativo" . "relativeToGround")
                                    ("Absoluto" . "absolute")
                                    ("Chao" . "clampToGround")))))
      (if (= hk:mode "clampToGround")
        (setq hk:extr nil)
        (progn
          (initget "Sim Nao")
          (setq hk:extr (/= (getkword "\nExtrudar paredes ate o chao? [Sim/Nao] <Sim>: ") "Nao"))
        )
      )
    )
  )
  (princ (strcat "\nConfig: altura=" hk:src
                 (if (= hk:src "Fixa") (strcat " " (rtos hk:height 2 2) " m") "")
                 (if (= hk:src "Xdata") (strcat " [" hk:key "]") "")
                 " | modo=" hk:mode
                 " | extrudar=" (if hk:extr "Sim" "Nao")))
)

(defun hk:hatch-alt (obj ename / v)
  (cond
    ((= hk:src "Fixa") hk:height)
    ((= hk:src "Elevacao") (vla-get-elevation obj))
    ((= hk:src "Xdata")
     (setq v (hk:xdata-number ename hk:key))
     (if v v (progn (setq hk:noxd (1+ hk:noxd)) 0.0))
    )
    (T 0.0)
  )
)

;;; ---------------- escrita do KML ----------------

(defun hk:write-header (f enc title desc)
  (hk:w f (strcat "<?xml version=\"1.0\" encoding=\"" enc "\"?>"))
  (hk:w f "<kml xmlns=\"http://www.opengis.net/kml/2.2\">")
  (hk:w f "  <Document>")
  (hk:w f (strcat "    <name>" (hk:xml title) "</name>"))
  (hk:w f (strcat "    <description><![CDATA[" desc "]]></description>"))
  ;; Cores KML no formato aabbggrr (7f = 50% de opacidade)
  (hk:w f "    <Style id=\"hk\">")
  (hk:w f "      <LineStyle><color>ff00aa00</color><width>2</width></LineStyle>")
  (hk:w f "      <PolyStyle><color>7f00ff00</color><fill>1</fill><outline>1</outline></PolyStyle>")
  (hk:w f "    </Style>")
)

(defun hk:write-footer (f)
  (hk:w f "  </Document>")
  (hk:w f "</kml>")
)

(defun hk:coord (p alt)
  (strcat "            " (rtos (car p) 2 9) "," (rtos (cadr p) 2 9) "," (rtos alt 2 3))
)

(defun hk:write-ring (f tag r alt)
  (hk:w f (strcat "          <" tag "><LinearRing><coordinates>"))
  (foreach p r (hk:w f (hk:coord p alt)))
  (hk:w f (hk:coord (car r) alt))
  (hk:w f (strcat "          </coordinates></LinearRing></" tag ">"))
)

(defun hk:write-polygon (f poly alt mode extr)
  (hk:w f "        <Polygon>")
  (if extr (hk:w f "          <extrude>1</extrude>"))
  (if (= mode "clampToGround") (hk:w f "          <tessellate>1</tessellate>"))
  (hk:w f (strcat "          <altitudeMode>" mode "</altitudeMode>"))
  (hk:write-ring f "outerBoundaryIs" (car poly) alt)
  (foreach h (cdr poly) (hk:write-ring f "innerBoundaryIs" h alt))
  (hk:w f "        </Polygon>")
)

(defun hk:write-placemark (f name desc polys alt mode extr)
  (hk:w f "    <Placemark>")
  (hk:w f (strcat "      <name>" (hk:xml name) "</name>"))
  (hk:w f (strcat "      <description><![CDATA[" desc "]]></description>"))
  (hk:w f "      <styleUrl>#hk</styleUrl>")
  (if (> (length polys) 1) (hk:w f "      <MultiGeometry>"))
  (foreach poly polys (hk:write-polygon f poly alt mode extr))
  (if (> (length polys) 1) (hk:w f "      </MultiGeometry>"))
  (hk:w f "    </Placemark>")
)

;;; ---------------- comando principal ----------------

(defun c:EXPORTHATCHESKML (/ *error* hk:src hk:height hk:key hk:mode hk:extr hk:noxd
                              cs oldvars doc ss n i hent obj loops llpolys llpoly ok
                              alt layer pat area holes desc f enc path cnt skipped)

  (defun *error* (msg)
    (if f (close f))
    (hk:cleanup-temps)
    (hk:restore oldvars)
    (if doc (vla-endundomark doc))
    (if (not (wcmatch (strcase msg) "*CANCEL*,*QUIT*,*EXIT*"))
      (princ (strcat "\nErro: " msg))
    )
    (princ)
  )

  (princ "\n=== EXPORTAR HACHURAS -> KML (Google Earth) v3.0 ===")

  (if (/= (getvar "CTAB") "Model")
    (progn (princ "\nVa para a aba Model e rode o comando de novo.") (exit))
  )

  (if (not (setq cs (hk:setup-cs))) (exit))
  (hk:config)

  (princ "\nSelecione as hachuras <Enter = todas do Model>: ")
  (setq ss (ssget '((0 . "HATCH") (410 . "Model"))))
  (if (not ss) (setq ss (ssget "_X" '((0 . "HATCH") (410 . "Model")))))
  (if (not ss) (progn (princ "\nNenhuma hachura encontrada.") (exit)))
  (setq n (sslength ss))
  (princ (strcat "\n" (itoa n) " hachura(s) selecionada(s)."))

  (setq path (getfiled "Salvar KML"
                       (strcat (getvar "DWGPREFIX") (vl-filename-base (getvar "DWGNAME")) "_hachuras.kml")
                       "kml" 1))
  (if (not path) (exit))

  (setq oldvars (hk:save-vars '("CMDECHO" "PLINETYPE" "DIMZIN")))
  (setvar "CMDECHO" 0)
  (setvar "PLINETYPE" 2)
  (setvar "DIMZIN" 0)
  (setq doc (vla-get-activedocument (vlax-get-acad-object)))
  (vla-startundomark doc)

  ;; UTF-8 (AutoCAD 2021+); se nao aceitar, cai para ANSI declarado como ISO-8859-1
  (setq f (vl-catch-all-apply 'open (list path "w" "utf8")))
  (if (or (vl-catch-all-error-p f) (not f))
    (setq f   (open path "w")
          enc "ISO-8859-1")
    (setq enc "UTF-8")
  )
  (if (not f)
    (progn (princ "\nNao foi possivel criar o arquivo (esta aberto em outro programa?).") (exit))
  )

  (hk:write-header f enc
    (strcat "Hachuras - " (vl-filename-base (getvar "DWGNAME")))
    (strcat "Exportado de: " (getvar "DWGNAME")
            "<br/>Data: " (menucmd "M=$(edtime,$(getvar,date),DD/MO/YYYY HH:MM)")
            "<br/>Sistema de coordenadas: " cs
            "<br/>Altura: " hk:src
            "<br/>Modo de altitude: " hk:mode))

  (setq i 0 cnt 0 skipped 0 hk:noxd 0)
  (repeat n
    (setq hent (ssname ss i)
          obj  (vlax-ename->vla-object hent)
          i    (1+ i)
    )
    (if (= 0 (rem i 25)) (princ (strcat "\n  " (itoa i) " de " (itoa n) "...")))

    (setq loops   (hk:get-loops hent)
          llpolys nil
          ok      (and loops T)
    )
    (foreach poly (if loops (hk:group-polys loops))
      (setq llpoly (mapcar 'hk:to-ll poly))
      (if (member nil llpoly)
        (setq ok nil)
        (setq llpolys (cons llpoly llpolys))
      )
    )

    (if (and ok llpolys)
      (progn
        (setq llpolys (reverse llpolys)
              alt     (hk:hatch-alt obj hent)
              layer   (vla-get-layer obj)
              pat     (vla-get-patternname obj)
              area    (vl-catch-all-apply 'vla-get-area (list obj))
              holes   (- (length loops) (length llpolys))
              cnt     (1+ cnt)
        )
        (if (vl-catch-all-error-p area) (setq area nil))
        (if (= hk:mode "clampToGround") (setq alt 0.0))
        (setq desc (strcat "<b>Layer:</b> " layer
                           "<br/><b>Padrao:</b> " pat
                           (if area (strcat "<br/><b>Area:</b> " (rtos area 2 2) " m2") "")
                           "<br/><b>Altura:</b> " (rtos alt 2 2) " m"
                           (if (> holes 0) (strcat "<br/><b>Furos:</b> " (itoa holes)) "")))
        (hk:write-placemark f (strcat "Hachura " (itoa cnt) " - " layer)
                            desc llpolys alt hk:mode hk:extr)
      )
      (setq skipped (1+ skipped))
    )
  )

  (hk:write-footer f)
  (close f)
  (setq f nil)
  (vla-endundomark doc)
  (hk:restore oldvars)

  (princ (strcat "\n\nOK! " (itoa cnt) " hachura(s) exportada(s) para:\n  " path))
  (if (> skipped 0)
    (princ (strcat "\nATENCAO: " (itoa skipped)
                   " hachura(s) ignorada(s) (contorno invalido ou fora do sistema de coordenadas)."
                   " Use TESTHATCHKML nelas para ver o motivo.")))
  (if (> hk:noxd 0)
    (princ (strcat "\nATENCAO: " (itoa hk:noxd) " hachura(s) sem XData [" hk:key "], exportadas com altura 0.")))

  (if (> cnt 0)
    (progn
      (initget "Sim Nao")
      (if (/= (getkword "\nAbrir no Google Earth? [Sim/Nao] <Sim>: ") "Nao")
        (startapp "explorer.exe" (strcat "\"" path "\""))
      )
    )
  )
  (princ)
)

;;; ---------------- diagnostico ----------------

(defun c:TESTHATCHKML (/ ent oldvars loops polys p q)
  (setq ent (car (entsel "\nSelecione UMA hachura: ")))
  (if (and ent (= (cdr (assoc 0 (entget ent))) "HATCH"))
    (progn
      (setq oldvars (hk:save-vars '("CMDECHO" "PLINETYPE")))
      (setvar "CMDECHO" 0)
      (setvar "PLINETYPE" 2)
      (setq loops (hk:get-loops ent))
      (hk:restore oldvars)
      (princ (strcat "\nContornos encontrados: " (itoa (length loops))))
      (if loops
        (progn
          (setq polys (hk:group-polys loops))
          (princ (strcat "\nPoligonos: " (itoa (length polys))
                         " | furos: " (itoa (- (length loops) (length polys)))))
          (foreach r loops
            (princ (strcat "\n  contorno: " (itoa (length r)) " vertices, area "
                           (rtos (hk:ring-area r) 2 2)))
          )
          (if (hk:setup-cs)
            (progn
              (setq p (car (car loops))
                    q (hk:to-ll (list p))
              )
              (princ (strcat "\nPrimeiro vertice: X=" (rtos (car p) 2 3) " Y=" (rtos (cadr p) 2 3)))
              (if q
                (princ (strcat "\n  -> Lon=" (rtos (car (car q)) 2 8) " Lat=" (rtos (cadr (car q)) 2 8)))
                (princ "\n  -> FALHOU a conversao para lat/long (confira o sistema de coordenadas).")
              )
            )
          )
        )
        (princ "\nNao consegui recriar o contorno. Tente HATCHEDIT > Recreate Boundary manualmente.")
      )
    )
    (princ "\nIsso nao e uma hachura.")
  )
  (princ)
)

(princ "\nExportHatchesToKML v3.0 carregado. Comandos: EXPORTHATCHESKML | TESTHATCHKML")
(princ)
