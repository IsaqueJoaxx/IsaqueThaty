;;; ExportHatchesToKML_Advanced.lsp
;;; Versão avançada com suporte completo à API do Civil 3D 2022+
;;; Conversão de coordenadas precisa usando AeccCoordinateConversion
;;; COM SUPORTE A EXTRUSÃO 3D / ALTITUDE
;;;
;;; REQUISITOS:
;;; - AutoCAD Civil 3D 2022 ou superior
;;; - Drawing com sistema de coordenadas atribuído (Assign Coordinate System)
;;;
;;; COMANDOS:
;;;   EXPORTHATCHESKML   - Versão padrão (com extrusão 3D)
;;;   EXPORTHATCHESKML2  - Versão avançada (precisa, usa Civil 3D API + extrusão 3D)
;;;   TESTHATCHKML       - Testar extração de vértices

(vl-load-com)

;; ============================================================
;; VERSÃO AVANÇADA - USA CIVIL 3D API NATIVA + EXTRUSÃO 3D
;; ============================================================
(defun c:ExportHatchesKML2 (/
    *error*
    doc space
    ss hatch-list
    kml-file kml-path
    aecc-app aecc-doc
    coord-sys
    ;; Variáveis de altitude/extrusão
    altitude-mode extrusion-height height-source
    use-hatch-elevation fixed-height property-name
    ;; Loop vars
    vertices ll-vertices count i pt ll-pt
    hatch-props hatch-height base-z
    )

  (defun *error* (msg)
    (if (and kml-file (not (member msg '("Function cancelled" "quit / exit abort"))))
      (close kml-file)
    )
    (if aecc-app (vl-catch-all-apply 'vlax-release-object (list aecc-app)))
    (if (not (member msg '("Function cancelled" "quit / exit abort")))
      (princ (strcat "\nErro: " msg))
    )
    (princ)
  )

  ;; ============================================================
  ;; CONFIGURAÇÃO DE ALTITUDE/EXTRUSÃO
  ;; ============================================================
  (defun configure-altitude-settings (/ ans)
    (princ "\n=== CONFIGURAÇÃO DE ALTITUDE / EXTRUSÃO 3D ===")

    ;; Fonte da altura
    (princ "\nFonte da altura:")
    (princ "\n  [1] Valor fixo (mesma altura para todas)")
    (princ "\n  [2] Elevação da hachura (propriedade Elevation/Z) - RECOMENDADO")
    (princ "\n  [3] Property Set / Extended Data (Civil 3D)")
    (princ "\n  [4] Superfície/Terreno (Civil 3D Surface) - EXPERIMENTAL")
    (princ "\n  [5] Sem extrusão (2D no chão)")
    (setq ans (getint "\nEscolha <2>: "))
    (setq height-source (cond ((= ans 1) "FIXED") ((= ans 3) "PROPERTY") ((= ans 4) "SURFACE") ((= ans 5) "NONE") (T "ELEVATION")))

    (if (= height-source "FIXED")
      (progn
        (setq fixed-height (getreal "\nAltura de extrusão (metros) <10.0>: "))
        (if (not fixed-height) (setq fixed-height 10.0))
      )
    )

    (if (= height-source "PROPERTY")
      (progn
        (setq property-name (getstring T "\nNome do Property Set / Extended Data <Height>: "))
        (if (= property-name "") (setq property-name "Height"))
      )
    )

    (if (= height-source "SURFACE")
      (progn
        (princ "\n⚠ EXPERIMENTAL: Requer superfície ativa no drawing")
        (setq fixed-height (getreal "\nAltura adicional acima da superfície (m) <0>: "))
        (if (not fixed-height) (setq fixed-height 0.0))
      )
    )

    ;; Modo de altitude
    (if (not (= height-source "NONE"))
      (progn
        (princ "\nModo de altitude KML:")
        (princ "\n  [1] relativeToGround (altura acima do terreno) - RECOMENDADO")
        (princ "\n  [2] absolute (altitude absoluta WGS84)")
        (princ "\n  [3] clampToGround (no chão, sem volume)")
        (setq ans (getint "\nEscolha <1>: "))
        (setq altitude-mode (cond ((= ans 2) "absolute") ((= ans 3) "clampToGround") (T "relativeToGround")))

        ;; Extrusão (walls)
        (setq ans (strcase (getstring "\nExtrudar paredes laterais? (S/N) <S>: ")))
        (setq extrusion-height (if (= ans "N") 0 1))
      )
      (progn
        (setq altitude-mode "clampToGround")
        (setq extrusion-height 0)
      )
    )

    (princ (strcat "\n✅ Config: Fonte=" height-source
                   (if (= height-source "FIXED") (strcat " " (rtos fixed-height 2 2) "m") "")
                   (if (= height-source "PROPERTY") (strcat " Prop=[" property-name "]") "")
                   (if (= height-source "SURFACE") (strcat " Surface+ " (rtos fixed-height 2 2) "m") "")
                   " | Modo=" altitude-mode
                   " | Extrudar=" (if (= extrusion-height 1) "Sim" "Não")))
  )

  ;; ============================================================
  ;; INICIALIZAR CIVIL 3D API
  ;; ============================================================
  (defun init-civil3d-api (/ )
    (vl-catch-all-apply
      '(lambda ()
         ;; Tentar diferentes versões do Civil 3D
         (setq aecc-app
           (vl-catch-all-apply
             '(lambda ()
                (vla-getinterfaceobject
                  (vlax-get-acad-object)
                  "AeccXUiLand.AeccApplication.14.0")  ; Civil 3D 2024
             ))
         )
         (if (not aecc-app)
           (setq aecc-app
             (vl-catch-all-apply
               '(lambda ()
                  (vla-getinterfaceobject
                    (vlax-get-acad-object)
                    "AeccXUiLand.AeccApplication.13.0")  ; Civil 3D 2023
               ))
           )
         )
         (if (not aecc-app)
           (setq aecc-app
             (vl-catch-all-apply
               '(lambda ()
                  (vla-getinterfaceobject
                    (vlax-get-acad-object)
                    "AeccXUiLand.AeccApplication.12.0")  ; Civil 3D 2022
               ))
           )
         )
         (if aecc-app
           (progn
             (setq aecc-doc (vlax-get-property aecc-app 'ActiveDocument))
             (if aecc-doc
               (progn
                 (setq coord-sys (vlax-get-property aecc-doc 'CoordinateSystem))
                 (princ (strcat "\n✅ Civil 3D API conectado. CS: " coord-sys))
                 T
               )
               (progn (princ "\n❌ Documento Civil 3D não ativo") nil)
             )
           )
           (progn (princ "\n❌ Civil 3D API não disponível") nil)
         )
       )
    )
  )

  ;; ============================================================
  ;; CONVERTER PONTO WCS -> LAT/LONG (CIVIL 3D API)
  ;; ============================================================
  (defun wcs-to-latlong-c3d (pt / result)
    (if (and aecc-doc coord-sys (not (eq coord-sys "")))
      (vl-catch-all-apply
        '(lambda ()
           ;; Usar AeccCoordinateConversion para transformar
           (setq result
             (vlax-invoke-method
               aecc-doc
               'TransformPoint
               pt
               coord-sys
               "LL84"  ; WGS84 Lat/Long
             ))
         )
      )
      (progn
        (princ "\n⚠ Sem sistema de coordenadas - usando coordenadas brutas")
        (list (car pt) (cadr pt))
      )
    )
  )

  ;; ============================================================
  ;; OBTER VÉRTICES DA HACHURA (COM Z)
  ;; ============================================================
  (defun get-hatch-boundary-pts-3d (hatch-obj / vertices loop obj obj-data obj-type pts elev)
    (setq vertices '())
    (vlax-for loop (vla-get-loops hatch-obj)
      (setq obj (vlax-vla-object->ename loop))
      (setq obj-data (entget obj))
      (setq obj-type (cdr (assoc 0 obj-data)))
      (cond
        ;; LWPOLYLINE
        ((= obj-type "LWPOLYLINE")
         (setq elev (cdr (assoc 38 obj-data)))
         (if (not elev) (setq elev 0.0))
         (setq pts (get-lwpolyline-pts-3d obj elev))
         (setq vertices (append vertices pts))
        )
        ;; POLYLINE (old style)
        ((= obj-type "POLYLINE")
         (setq pts (get-polyline-pts-3d obj))
         (setq vertices (append vertices pts))
        )
        ;; REGION, CIRCLE, ELLIPSE, SPLINE - amostrar
        (T
          (setq elev (get-object-elevation obj))
          (setq pts (sample-curve-obj-3d obj 50 elev))
          (setq vertices (append vertices pts))
        )
      )
    )
    vertices
  )

  (defun get-lwpolyline-pts-3d (ename elev / pts data pt)
    (setq data (entget ename))
    (foreach item data
      (if (= (car item) 10)
        (progn
          (setq pt (cdr item))
          (setq pts (cons (list (car pt) (cadr pt) elev) pts))
        )
      )
    )
    (reverse pts)
  )

  (defun get-polyline-pts-3d (ename / pts vtx vtx-data pt)
    (setq vtx (entnext ename))
    (while vtx
      (setq vtx-data (entget vtx))
      (if (= (cdr (assoc 0 vtx-data)) "VERTEX")
        (progn
          (setq pt (cdr (assoc 10 vtx-data)))
          (setq pts (cons (list (car pt) (cadr pt) (caddr pt)) pts))
        )
      )
      (setq vtx (entnext vtx))
    )
    (reverse pts)
  )

  ;; Amostrar curva genérica com Z
  (defun sample-curve-obj-3d (ename num-seg elev / pts len i pt param)
    (setq len (vlax-curve-getdistatparam ename (vlax-curve-getendparam ename)))
    (setq i 0)
    (repeat (1+ num-seg)
      (setq param (* (/ i num-seg) (vlax-curve-getendparam ename)))
      (setq pt (vlax-curve-getpointatparam ename param))
      (setq pts (cons (list (car pt) (cadr pt) elev) pts))
      (setq i (1+ i))
    )
    (reverse pts)
  )

  ;; Obter elevação de um objeto
  (defun get-object-elevation (ename / obj-data elev)
    (setq obj-data (entget ename))
    (setq elev (cdr (assoc 38 obj-data)))
    (if (not elev) (setq elev 0.0))
    elev
  )

  ;; ============================================================
  ;; OBTER ALTURA DA HACHURA (MÚLTIPLAS FONTES)
  ;; ============================================================
  (defun get-hatch-height (hatch-obj / h val ename xdata)
    (cond
      ((= height-source "FIXED") fixed-height)
      ((= height-source "ELEVATION")
       ;; Usar propriedade Elevation da hachura
       (vl-catch-all-apply '(lambda () (setq val (vla-get-elevation hatch-obj))))
       (if (and val (numberp val)) val fixed-height)
      )
      ((= height-source "PROPERTY")
       ;; Tentar Property Set / XData
       (get-hatch-property-height hatch-obj property-name)
      )
      ((= height-source "SURFACE")
       ;; TODO: Implementar leitura de superfície Civil 3D
       ;; Por enquanto retorna 0 + fixed-height
       fixed-height
      )
      (T 0.0)
    )
  )

  ;; Buscar altura em Property Set / XData
  (defun get-hatch-property-height (hatch-obj prop-name / ename xdata val found)
    (setq ename (vlax-vla-object->ename hatch-obj))
    ;; Tentar XData (Extended Data)
    (setq xdata (entget ename (list "*")))
    (foreach item xdata
      (if (and (= (car item) -3) (cadr item))
        (progn
          (setq lst (cadr item))
          (setq i 0)
          (while (< i (length lst))
            (setq sub (nth i lst))
            (if (and (= (car sub) 1000) (wcmatch (strcase (cdr sub)) (strcat "*" (strcase prop-name) "*")))
              (progn
                (setq next (nth (1+ i) lst))
                (if (and next (= (car next) 40))
                  (setq val (cdr next) found T)
                )
              )
            )
            (setq i (1+ i))
          )
        )
      )
    )
    (if found val 0.0)
  )

  ;; ============================================================
  ;; OBTER PROPRIEDADES DA HACHURA
  ;; ============================================================
  (defun get-hatch-properties (hatch / props)
    (setq props '())
    (foreach prop '(PatternName PatternType PatternAngle PatternScale PatternSpace
                    PatternDouble HatchStyle AssociativeHatch Area Layer Linetype
                    Color TrueColor PlotStyleName Elevation Thickness)
      (vl-catch-all-apply
        '(lambda ()
           (setq val (vlax-get-property hatch prop))
           (setq props (cons (cons (vl-string-downcase (vl-string-left-trim "Pattern" prop)) val) props))
         )
      )
    )
    (reverse props)
  )

  ;; ============================================================
  ;; ESCRITA KML COM ALTITUDE/EXTRUSÃO
  ;; ============================================================
  (defun write-kml-header (file name desc / )
    (write-line "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" file)
    (write-line "<kml xmlns=\"http://www.opengis.net/kml/2.2\" xmlns:gx=\"http://www.google.com/kml/ext/2.2\">" file)
    (write-line "  <Document>" file)
    (write-line (strcat "    <name>" name "</name>") file)
    (write-line (strcat "    <description><![CDATA[" desc "]]></description>") file)

    ;; Estilo padrão
    (write-line "    <Style id=\"hatchStyle\">" file)
    (write-line "      <PolyStyle>" file)
    (write-line "        <color>7f00ff00</color>" file)
    (write-line "        <fill>1</fill>" file)
    (write-line "        <outline>1</outline>" file)
    (write-line "      </PolyStyle>" file)
    (write-line "      <LineStyle>" file)
    (write-line "        <color>ffff0000</color>" file)
    (write-line "        <width>2</width>" file)
    (write-line "      </LineStyle>" file)
    (write-line "    </Style>" file)

    ;; Estilo para extrusão
    (write-line "    <Style id=\"hatchStyleExtruded\">" file)
    (write-line "      <PolyStyle>" file)
    (write-line "        <color>7f00ff00</color>" file)
    (write-line "        <fill>1</fill>" file)
    (write-line "        <outline>1</outline>" file)
    (write-line "      </PolyStyle>" file)
    (write-line "      <LineStyle>" file)
    (write-line "        <color>ffff0000</color>" file)
    (write-line "        <width>2</width>" file)
    (write-line "      </LineStyle>" file)
    (write-line "    </Style>" file)

    ;; Estilo vermelho
    (write-line "    <Style id=\"hatchStyleRed\">" file)
    (write-line "      <PolyStyle><color>7fff0000</color><fill>1</fill><outline>1</outline></PolyStyle>" file)
    (write-line "      <LineStyle><color>ffff0000</color><width>2</width></LineStyle>" file)
    (write-line "    </Style>" file)

    ;; Estilo vermelho extrudado
    (write-line "    <Style id=\"hatchStyleRedExtruded\">" file)
    (write-line "      <PolyStyle><color>7fff0000</color><fill>1</fill><outline>1</outline></PolyStyle>" file)
    (write-line "      <LineStyle><color>ffff0000</color><width>2</width></LineStyle>" file)
    (write-line "    </Style>" file)
  )

  (defun write-kml-footer (file / )
    (write-line "  </Document>" file)
    (write-line "</kml>" file)
  )

  (defun write-kml-placemark-3d (file name desc coords height extrude mode style / )
    (write-line "    <Placemark>" file)
    (write-line (strcat "      <name>" name "</name>") file)
    (write-line (strcat "      <description><![CDATA[" desc "]]></description>") file)
    (write-line (strcat "      <styleUrl>#" style "</styleUrl>") file)
    (write-line "      <Polygon>" file)

    ;; Configurações de altitude/extrusão
    (if (not (= mode "clampToGround"))
      (progn
        (write-line (strcat "        <altitudeMode>" mode "</altitudeMode>") file)
        (if (= extrude 1)
          (write-line "        <extrude>1</extrude>" file)
        )
      )
    )

    (write-line "        <outerBoundaryIs>" file)
    (write-line "          <LinearRing>" file)
    (write-line "            <coordinates>" file)

    ;; Escrever coordenadas com altitude (lon,lat,alt)
    (foreach pt coords
      (write-line (strcat "              "
                          (rtos (car pt) 2 10) ","
                          (rtos (cadr pt) 2 10) ","
                          (rtos (caddr pt) 2 3)) file)
    )

    ;; Fechar anel
    (if coords
      (progn
        (setq pt (car coords))
        (write-line (strcat "              "
                            (rtos (car pt) 2 10) ","
                            (rtos (cadr pt) 2 10) ","
                            (rtos (caddr pt) 2 3)) file)
      )
    )

    (write-line "            </coordinates>" file)
    (write-line "          </LinearRing>" file)
    (write-line "        </outerBoundaryIs>" file)
    (write-line "      </Polygon>" file)
    (write-line "    </Placemark>" file)
  )

  ;; ============================================================
  ;; PROGRAMA PRINCIPAL
  ;; ============================================================

  (princ "\n=== EXPORTAÇÃO AVANÇADA HACHURAS → KML 3D (Civil 3D API) ===")

  ;; Configurar altitude
  (configure-altitude-settings)

  ;; Inicializar API
  (if (not (init-civil3d-api))
    (progn
      (princ "\n⚠ Continuando sem API do Civil 3D (precisão reduzida)...")
      (setq coord-sys nil)
    )
  )

  (setq doc (vla-get-activedocument (vlax-get-acad-object)))
  (setq space (vla-get-modelspace doc))

  ;; Seleção
  (princ "\nSelecione hachuras [Todas no ModelSpace]: ")
  (setq ss (ssget '((0 . "HATCH"))))

  (if ss
    (progn
      (setq i 0)
      (repeat (sslength ss)
        (setq hatch-list (cons (vlax-ename->vla-object (ssname ss i)) hatch-list))
        (setq i (1+ i))
      )
    )
    (progn
      (vlax-for obj space
        (if (= (vla-get-objectname obj) "AcDbHatch")
          (setq hatch-list (cons obj hatch-list))
        )
      )
    )
  )

  (if (not hatch-list)
    (progn (princ "\n❌ Nenhuma hachura encontrada!") (exit))
  )

  (princ (strcat "\n🔍 " (itoa (length hatch-list)) " hachura(s) selecionada(s)."))

  ;; Arquivo de saída
  (setq kml-path (getfiled "Salvar KML"
                           (strcat (getvar "DWGPREFIX")
                                   (vl-filename-base (getvar "DWGNAME"))
                                   "_hatches_3d.kml")
                           "kml" 1))

  (if (not kml-path) (progn (princ "\n❌ Cancelado.") (exit)))

  (setq kml-file (open kml-path "w"))
  (if (not kml-file) (progn (princ "\n❌ Erro ao criar arquivo!") (exit)))

  ;; Header
  (write-kml-header kml-file
                    (strcat "Hachuras 3D - " (vl-filename-base (getvar "DWGNAME")))
                    (strcat "Exportado de: " (getvar "DWGNAME")
                            "\nData: " (menucmd "M=$(edtime,$(getvar,date),DD/MM/YYYY HH:MM)")
                            "\nSistema de Coordenadas: " (if coord-sys coord-sys "N/A (WCS)")
                            "\nModo Altitude: " altitude-mode
                            (if (= height-source "FIXED") (strcat "\nAltura Fixa: " (rtos fixed-height 2 2) "m") "")
                            (if (= height-source "ELEVATION") "\nAltura: Elevação da hachura" "")
                            (if (= height-source "PROPERTY") (strcat "\nAltura: Property Set [" property-name "]") "")
                            (if (= height-source "SURFACE") (strcat "\nAltura: Superfície + " (rtos fixed-height 2 2) "m") "")))

  ;; Processar hachuras
  (setq count 0)
  (foreach hatch (reverse hatch-list)
    (setq count (1+ count))
    (princ (strcat "\n  Processando " (itoa count) "/" (itoa (length hatch-list)) "..."))

    ;; Vértices 3D do boundary
    (setq vertices (get-hatch-boundary-pts-3d hatch))

    (if vertices
      (progn
        ;; Obter altura para esta hachura
        (setq hatch-height (get-hatch-height hatch))

        ;; Elevação base da hachura
        (vl-catch-all-apply '(lambda () (setq base-z (vla-get-elevation hatch))))
        (if (not base-z) (setq base-z 0.0))

        ;; Converter para Lat/Long mantendo Z
        (setq ll-vertices '())
        (foreach vtx vertices
          (setq xy (list (car vtx) (cadr vtx)))
          (setq z (caddr vtx))
          (if coord-sys
            (setq ll-pt (wcs-to-latlong-c3d xy))
            (setq ll-pt xy)
          )
          (if (and ll-pt (listp ll-pt) (>= (length ll-pt) 2))
            ;; Z final = base + elevação do vértice + altura de extrusão
            (setq ll-vertices (cons (list (car ll-pt) (cadr ll-pt) (+ base-z z hatch-height)) ll-vertices))
          )
        )
        (setq ll-vertices (reverse ll-vertices))

        ;; Propriedades
        (setq props (get-hatch-properties hatch))
        (setq layer (cdr (assoc "layer" props)))
        (setq pattern (cdr (assoc "patternname" props)))
        (setq area (cdr (assoc "area" props)))
        (setq color (cdr (assoc "color" props)))
        (setq elev (cdr (assoc "elevation" props)))
        (setq thick (cdr (assoc "thickness" props)))

        (setq desc (strcat
                    "<b>Layer:</b> " (vl-princ-to-string layer) "<br/>"
                    "<b>Padrão:</b> " (vl-princ-to-string pattern) "<br/>"
                    "<b>Área:</b> " (rtos (vl-princ-to-string area) 2 3) " un²<br/>"
                    "<b>Cor:</b> " (vl-princ-to-string color) "<br/>"
                    "<b>Elevação Base:</b> " (rtos (vl-princ-to-string elev) 2 3) "m<br/>"
                    "<b>Altura Extrusão:</b> " (rtos hatch-height 2 3) "m<br/>"
                    "<b>Vértices:</b> " (itoa (length ll-vertices))))

        ;; Estilo baseado na layer + extrusão
        (setq base-style (if (wcmatch (strcase layer) "*VERMELHO*") "hatchStyleRed" "hatchStyle"))
        (setq style (if (= extrusion-height 1) (strcat base-style "Extruded") base-style))

        (write-kml-placemark-3d kml-file
                                (strcat "Hachura_" (itoa count) "_" layer)
                                desc
                                ll-vertices
                                hatch-height
                                extrusion-height
                                altitude-mode
                                style)
      )
      (princ " ⚠ Sem boundary válido")
    )
  )

  ;; Finalizar
  (write-kml-footer kml-file)
  (close kml-file)
  (setq kml-file nil)

  (if aecc-app (vlax-release-object aecc-app))

  (princ (strcat "\n\n✅ SUCESSO! Arquivo: " kml-path))
  (princ (strcat "\n   " (itoa count) " hachura(s) exportada(s) com extrusão 3D."))
  (princ "\n   Abra no Google Earth Pro → Ative '3D Buildings' e 'Terrain'.")

  ;; Auto-abrir
  (if (= (strcase (getstring "\nAbrir no Google Earth? (S/N) <S>: ")) "S")
    (startapp "googleearthpro.exe" kml-path)
  )

  (princ)
)

;; ============================================================
;; FUNÇÃO DE TESTE RÁPIDO (COM Z)
;; ============================================================
(defun c:TestHatchKML (/ )
  (princ "\nTestando extração de vértices 3D...")
  (setq ss (ssget '((0 . "HATCH"))))
  (if ss
    (progn
      (setq hatch (vlax-ename->vla-object (ssname ss 0)))
      (setq verts (get-hatch-boundary-pts-3d hatch))
      (princ (strcat "\nVértices encontrados: " (itoa (length verts))))
      (foreach v verts (princ (strcat "\n  X:" (rtos (car v)) " Y:" (rtos (cadr v)) " Z:" (rtos (caddr v)))))
      (princ (strcat "\nElevação da hachura: " (rtos (vla-get-elevation hatch) 2 3)))
    )
    (princ "\nNenhuma hachura selecionada")
  )
  (princ)
)

;; ============================================================
;; COMANDO DE CONFIGURAÇÃO RÁPIDA
;; ============================================================
(defun c:ExportHatchesKML2_Config (/ )
  (c:ExportHatchesKML2)
)

(princ "\n===============================================")
(princ "\n  ExportHatchesToKML_Advanced.lsp v2.0 - 3D EXTRUSION")
(princ "\n  Comandos disponíveis:")
(princ "\n    EXPORTHATCHESKML   - Versão padrão (com 3D)")
(princ "\n    EXPORTHATCHESKML2  - Versão avançada Civil 3D API + 3D")
(princ "\n    EXPORTHATCHESKML2_CONFIG - Reconfigurar e exportar")
(princ "\n    TESTHATCHKML       - Testar extração de vértices 3D")
(princ "\n===============================================")
(princ)

(prin1)