;;; ExportHatchesToKML.lsp
;;; AutoLISP para AutoCAD Civil 3D - Exporta hachuras para KML (Google Earth)
;;; Versão com suporte a EXTRUSÃO 3D / ALTITUDE
;;; Autor: Claude Code
;;; Data: 2026-10-29
;;;
;;; Uso: Carregue com APPLOAD ou (load "ExportHatchesToKML.lsp")
;;; Comando: EXPORTHATCHESKML

(vl-load-com)

(defun c:ExportHatchesKML (/
    *error*
    doc space
    ss hatch-list
    kml-file kml-path
    coord-system
    ;; Variáveis de altitude/extrusão
    altitude-mode extrusion-height height-source
    use-hatch-elevation fixed-height property-name
    ;; Loop vars
    vertices ll-vertices count i pt ll-pt
    hatch-props hatch-elevation
    )

  ;; ============================================================
  ;; TRATAMENTO DE ERROS
  ;; ============================================================
  (defun *error* (msg)
    (if (and kml-file (not (member msg '("Function cancelled" "quit / exit abort"))))
      (close kml-file)
    )
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
    (princ "\n  [2] Elevação da hachura (propriedade Elevation/Z)")
    (princ "\n  [3] Property Set / Extended Data (Civil 3D)")
    (princ "\n  [4] Sem extrusão (2D no chão)")
    (setq ans (getint "\nEscolha <2>: "))
    (setq height-source (cond ((= ans 1) "FIXED") ((= ans 3) "PROPERTY") ((= ans 4) "NONE") (T "ELEVATION")))

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
                   " | Modo=" altitude-mode
                   " | Extrudar=" (if (= extrusion-height 1) "Sim" "Não")))
  )

  ;; ============================================================
  ;; FUNÇÕES AUXILIARES
  ;; ============================================================

  ;; Obter vértices de uma hachura (boundary) - COM Z
  (defun get-hatch-vertices-3d (hatch-obj / vertices loop obj pt z)
    (setq vertices '())
    (vlax-for loop (vla-get-loops hatch-obj)
      (setq obj (vlax-vla-object->ename loop))
      (setq obj-type (cdr (assoc 0 (entget obj))))
      (cond
        ((= obj-type "LWPOLYLINE")
         (setq vertices (append vertices (get-lwpolyline-pts-3d obj)))
        )
        ((= obj-type "POLYLINE")
         (setq vertices (append vertices (get-polyline-pts-3d obj)))
        )
        (T
         ;; Para arcs, circles, ellipses, splines - amostrar
         (setq vertices (append vertices (sample-curve-obj-3d obj 50)))
        )
      )
    )
    vertices
  )

  ;; LWPOLYLINE com Z (elevação constante ou vértice a vértice)
  (defun get-lwpolyline-pts-3d (ename / pts data pt z elev)
    (setq data (entget ename))
    ;; Elevação da polyline (código 38)
    (setq elev (cdr (assoc 38 data)))
    (if (not elev) (setq elev 0.0))
    (foreach item data
      (if (= (car item) 10)
        (progn
          (setq pt (cdr item))
          ;; LWPOLYLINE pode ter Z por vértice (código 10 é 2D, mas pode ter 38 global)
          (setq pts (cons (list (car pt) (cadr pt) elev) pts))
        )
      )
    )
    (reverse pts)
  )

  ;; POLYLINE antiga com Z
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
  (defun sample-curve-obj-3d (ename num-seg / pts len i pt param z)
    (setq z (get-object-elevation ename))
    (setq len (vlax-curve-getdistatparam ename (vlax-curve-getendparam ename)))
    (setq i 0)
    (repeat (1+ num-seg)
      (setq param (* (/ i num-seg) (vlax-curve-getendparam ename)))
      (setq pt (vlax-curve-getpointatparam ename param))
      (setq pts (cons (list (car pt) (cadr pt) z) pts))
      (setq i (1+ i))
    )
    (reverse pts)
  )

  ;; Obter elevação de um objeto (Z)
  (defun get-object-elevation (ename / obj-data elev)
    (setq obj-data (entget ename))
    (setq elev (cdr (assoc 38 obj-data))) ;; Elevação da entidade
    (if (not elev) (setq elev 0.0))
    elev
  )

  ;; Obter altura da hachura (múltiplas fontes)
  (defun get-hatch-height (hatch-obj / h val)
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
      (T 0.0)
    )
  )

  ;; Buscar altura em Property Set / XData
  (defun get-hatch-property-height (hatch-obj prop-name / ename xdata val)
    (setq ename (vlax-vla-object->ename hatch-obj))
    ;; Tentar XData (Extended Data)
    (setq xdata (entget ename (list "*")))
    (foreach item xdata
      (if (and (= (car item) -3) (cadr item))
        (progn
          (foreach sub (cadr item)
            (if (and (= (car sub) 1000) (wcmatch (strcase (cdr sub)) (strcat "*" (strcase prop-name) "*")))
              (setq val (atof (cdr (assoc 40 (member sub (cadr item)))))))
            )
          )
        )
      )
    )
    (if val val 0.0)
  )

  ;; Obter propriedades da hachura
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
  ;; CONVERSÃO DE COORDENADAS
  ;; ============================================================

  (defun wcs-to-utm (pt) pt)

  (defun get-drawing-coordsys (/ ade-obj coordsys)
    (vl-catch-all-apply
      '(lambda ()
         (setq ade-obj (vla-getinterfaceobject (vlax-get-acad-object) "ADE.Application.10"))
         (if ade-obj
           (progn (setq coordsys (vlax-get-property ade-obj 'ActiveCoordinateSystem))
                  (vlax-release-object ade-obj)
                  coordsys)
         )
       )
    )
  )

  (defun convert-point-cs (pt from-cs to-cs / ade-obj result)
    (vl-catch-all-apply
      '(lambda ()
         (setq ade-obj (vla-getinterfaceobject (vlax-get-acad-object) "ADE.Application.10"))
         (if ade-obj
           (progn (setq result (vlax-invoke-method ade-obj 'TransformPoint pt from-cs to-cs))
                  (vlax-release-object ade-obj)
                  result)
         )
       )
    )
  )

  ;; ============================================================
  ;; ESCRITA KML COM ALTITUDE
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
  )

  (defun write-kml-footer (file / )
    (write-line "  </Document>" file)
    (write-line "</kml>" file)
  )

  (defun write-kml-placemark-3d (file name desc coords height extrude mode style / coord-str)
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
      (setq coord-str (strcat
                       (rtos (car pt) 2 10) ","
                       (rtos (cadr pt) 2 10) ","
                       (rtos (caddr pt) 2 3)))
      (write-line (strcat "              " coord-str) file)
    )

    ;; Fechar anel
    (if coords
      (progn
        (setq pt (car coords))
        (setq coord-str (strcat
                         (rtos (car pt) 2 10) ","
                         (rtos (cadr pt) 2 10) ","
                         (rtos (caddr pt) 2 3)))
        (write-line (strcat "              " coord-str) file)
      )
    )

    (write-line "            </coordinates>" file)
    (write-line "          </LinearRing>" file)
    (write-line "        </outerBoundaryIs>" file)

    ;; Inner boundaries (holes) - se houver loops internos
    ;; TODO: implementar se necessário

    (write-line "      </Polygon>" file)
    (write-line "    </Placemark>" file)
  )

  ;; ============================================================
  ;; PROGRAMA PRINCIPAL
  ;; ============================================================

  (princ "\n=== EXPORTAÇÃO HACHURAS → KML (COM EXTRUSÃO 3D) ===")

  ;; Configurar altitude
  (configure-altitude-settings)

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

  ;; Coordinate system
  (setq coord-system (get-drawing-coordsys))

  ;; Header
  (write-kml-header kml-file
                    (strcat "Hachuras 3D - " (vl-filename-base (getvar "DWGNAME")))
                    (strcat "Exportado de: " (getvar "DWGNAME")
                            "\nData: " (menucmd "M=$(edtime,$(getvar,date),DD/MM/YYYY HH:MM)")
                            "\nSistema de Coordenadas: " (if coord-system coord-system "N/A (WCS)")
                            "\nModo Altitude: " altitude-mode
                            (if (= height-source "FIXED") (strcat "\nAltura Fixa: " (rtos fixed-height 2 2) "m") "")
                            (if (= height-source "ELEVATION") "\nAltura: Elevação da hachura" "")
                            (if (= height-source "PROPERTY") (strcat "\nAltura: Property Set [" property-name "]") "")))

  ;; Processar hachuras
  (setq count 0)
  (foreach hatch (reverse hatch-list)
    (setq count (1+ count))
    (princ (strcat "\n  Processando " (itoa count) "/" (itoa (length hatch-list)) "..."))

    ;; Vértices 3D do boundary
    (setq vertices (get-hatch-vertices-3d hatch))

    (if vertices
      (progn
        ;; Obter altura para esta hachura
        (setq hatch-height (get-hatch-height hatch))

        ;; Aplicar altura aos vértices (base no Z=0, topo no Z=height)
        ;; Para extrusão, o KML usa a altitude dos vértices + extrude=1
        (setq base-z 0.0)
        ;; Se a hachura já tem elevação, usar como base
        (vl-catch-all-apply '(lambda () (setq base-z (vla-get-elevation hatch))))

        ;; Converter para Lat/Long mantendo Z
        (setq ll-vertices '())
        (foreach vtx vertices
          (setq xy (list (car vtx) (cadr vtx)))
          (setq z (caddr vtx))
          (if coord-system
            (setq ll-pt (convert-point-cs xy coord-system "LL84"))
            (setq ll-pt xy)
          )
          (if ll-pt
            ;; Z final = base + altura de extrusão
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

        ;; Estilo
        (setq style (if (= extrusion-height 1) "hatchStyleExtruded" "hatchStyle"))

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

  (princ (strcat "\n\n✅ SUCESSO! Arquivo: " kml-path))
  (princ (strcat "\n   " (itoa count) " hachura(s) exportada(s) com extrusão 3D."))
  (princ "\n   Abra no Google Earth Pro → Ative '3D Buildings' ou 'Terrain'.")

  ;; Auto-abrir
  (if (= (strcase (getstring "\nAbrir no Google Earth? (S/N) <S>: ")) "S")
    (startapp "googleearthpro.exe" kml-path)
  )

  (princ)
)

;; ============================================================
;; COMANDO RÁPIDO: Reconfigurar e exportar
;; ============================================================
(defun c:ExportHatchesKML_Config (/ )
  (c:ExportHatchesKML)
)

(princ "\n===============================================")
(princ "\n  ExportHatchesToKML.lsp v2.0 - COM EXTRUSÃO 3D")
(princ "\n  Comando: EXPORTHATCHESKML")
(princ "\n  Comando: EXPORTHATCHESKML_CONFIG (reconfigurar)")
(princ "\n===============================================")
(princ)

(prin1)