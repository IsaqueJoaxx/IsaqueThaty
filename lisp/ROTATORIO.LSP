;;; ============================================================
;;; ROTATORIO.LSP - Criação Automática de Rotatória
;;; Civil 3D 2026 | AutoCAD 2026
;;; Versão 2.0 - com módulo de Vias de Acesso
;;; Comandos: ROTATORIO | ROT-VIAS | ROT-FORMA | ROT-AJUDA
;;; ============================================================

(vl-load-com)

;;; ============================================================
;;; UTILITÁRIOS GERAIS
;;; ============================================================

(defun rot:msg (txt)
  (princ (strcat "\n" txt)))

(defun rot:make-layer (layname color ltype / )
  (if (not (tblsearch "LAYER" layname))
    (entmake (list
      '(0 . "LAYER")
      '(100 . "AcDbSymbolTableRecord")
      '(100 . "AcDbLayerTableRecord")
      (cons 2 layname)
      '(70 . 0)
      (cons 62 color)
      (cons 6 (if ltype ltype "Continuous"))))))

(defun rot:dist2d (p1 p2)
  (sqrt (+ (expt (- (car p2) (car p1)) 2)
           (expt (- (cadr p2) (cadr p1)) 2))))

(defun rot:getreal-default (prompt default / val)
  (setq val (getreal (strcat prompt " <" (rtos default 2 2) ">: ")))
  (if val val default))

(defun rot:getint-default (prompt default / val)
  (setq val (getint (strcat prompt " <" (itoa default) ">: ")))
  (if val val default))

(defun rot:getkw-default (prompt kws default / val)
  (initget kws)
  (setq val (getkword (strcat prompt " <" default ">: ")))
  (if val val default))

;;; Normaliza ângulo para [-pi, pi]
(defun rot:norm-ang (a)
  (while (> a pi)  (setq a (- a (* 2 pi))))
  (while (< a (- pi)) (setq a (+ a (* 2 pi))))
  a)

;;; Ponto 2D a distância d na direção ang a partir de p
(defun rot:pt-polar (p ang d)
  (list (+ (car p) (* d (cos ang)))
        (+ (cadr p) (* d (sin ang)))
        (if (caddr p) (caddr p) 0.0)))

;;; Perpendicular à esquerda do vetor ang (sentido horário = direita)
(defun rot:ang-esq (ang) (+ ang (/ pi 2.0)))
(defun rot:ang-dir (ang) (- ang (/ pi 2.0)))

;;; Interseção de dois segmentos infinitos
(defun rot:intersect-lines (p1 p2 p3 p4 /
  x1 y1 x2 y2 x3 y3 x4 y4 denom t-num tt)
  (setq x1 (car p1) y1 (cadr p1)
        x2 (car p2) y2 (cadr p2)
        x3 (car p3) y3 (cadr p3)
        x4 (car p4) y4 (cadr p4))
  (setq denom (- (* (- x1 x2) (- y3 y4)) (* (- y1 y2) (- x3 x4))))
  (if (not (equal denom 0.0 1e-8))
    (progn
      (setq t-num (- (* (- x1 x3) (- y3 y4)) (* (- y1 y3) (- x3 x4))))
      (setq tt (/ t-num denom))
      (list (+ x1 (* tt (- x2 x1)))
            (+ y1 (* tt (- y2 y1)))
            0.0))
    nil))

;;; ============================================================
;;; CAMADAS
;;; ============================================================

(defun rot:init-layers ()
  ;; Rotatória
  (rot:make-layer "ROT-EIXO"          1  "Continuous")
  (rot:make-layer "ROT-BORDAO-EXT"    3  "Continuous")
  (rot:make-layer "ROT-BORDAO-ILH"    3  "Continuous")
  (rot:make-layer "ROT-CALCADA"       2  "Continuous")
  (rot:make-layer "ROT-ILHA"          5  "Continuous")
  (rot:make-layer "ROT-BORDA-2D"      8  "Continuous")
  (rot:make-layer "ROT-AUX"           8  "DASHED")
  (rot:make-layer "ROT-SURFACE-PTS"   4  "Continuous")
  (rot:make-layer "ROT-PERFIL-AUX"    6  "Continuous")
  ;; Vias de acesso
  (rot:make-layer "VIA-EIXO"          1  "CENTER")
  (rot:make-layer "VIA-BORDO-ESQ"     3  "Continuous")
  (rot:make-layer "VIA-BORDO-DIR"     3  "Continuous")
  (rot:make-layer "VIA-FAIXA"         8  "DASHED")
  (rot:make-layer "VIA-CALCADA-ESQ"   2  "Continuous")
  (rot:make-layer "VIA-CALCADA-DIR"   2  "Continuous")
  (rot:make-layer "VIA-ILHA-CANAL"    5  "Continuous")
  (rot:make-layer "VIA-RAIO-ENT"      6  "Continuous")
  (rot:make-layer "VIA-RAIO-SAI"      6  "Continuous")
  (rot:make-layer "VIA-SURFACE-PTS"   4  "Continuous"))

;;; ============================================================
;;; PRIMITIVAS DE DESENHO
;;; ============================================================

(defun rot:draw-ring-3d (cx cy cz raio nseg layname /
  step ang x1 y1 x2 y2 i)
  (setq nseg (max nseg 36))
  (setq step (/ (* 2.0 pi) nseg))
  (setq ang 0.0 i 0)
  (while (< i nseg)
    (setq x1 (+ cx (* raio (cos ang)))
          y1 (+ cy (* raio (sin ang)))
          x2 (+ cx (* raio (cos (+ ang step))))
          y2 (+ cy (* raio (sin (+ ang step)))))
    (entmake (list '(0 . "LINE")
                   (cons 8 layname)
                   (cons 10 (list x1 y1 cz))
                   (cons 11 (list x2 y2 cz))))
    (setq ang (+ ang step) i (1+ i))))

(defun rot:draw-circle-2d (cx cy raio layname)
  (entmake (list '(0 . "CIRCLE")
                 (cons 8 layname)
                 (cons 10 (list cx cy 0.0))
                 (cons 40 raio))))

(defun rot:draw-point-3d (x y z layname)
  (entmake (list '(0 . "POINT")
                 (cons 8 layname)
                 (cons 10 (list x y z)))))

(defun rot:ring-points (cx cy cz raio nseg layname /
  step ang i x y)
  (setq step (/ (* 2.0 pi) nseg))
  (setq ang 0.0 i 0)
  (while (< i nseg)
    (setq x (+ cx (* raio (cos ang)))
          y (+ cy (* raio (sin ang))))
    (rot:draw-point-3d x y cz layname)
    (setq ang (+ ang step) i (1+ i))))

(defun rot:line-3d (p1 p2 layname)
  (entmake (list '(0 . "LINE")
                 (cons 8 layname)
                 (cons 10 p1)
                 (cons 11 p2))))

;;; Desenha LWPOLYLINE 2D a partir de lista de pontos 2D
(defun rot:draw-lwpline (pts closed layname /
  nverts ent)
  (setq nverts (length pts))
  (entmake (list
    '(0 . "LWPOLYLINE")
    '(100 . "AcDbEntity")
    (cons 8 layname)
    '(100 . "AcDbPolyline")
    (cons 90 nverts)
    (cons 70 (if closed 1 0))))
  (foreach pt pts
    (entmake (list (cons 10 (list (car pt) (cadr pt))))))
  ;; Fecha entidade
  (entmake '((0 . "SEQEND"))))

;;; Arco 2D entre dois ângulos (ang1 -> ang2, sentido anti-horário)
(defun rot:draw-arc-2d (cx cy raio ang1 ang2 layname)
  (entmake (list '(0 . "ARC")
                 (cons 8 layname)
                 (cons 10 (list cx cy 0.0))
                 (cons 40 raio)
                 (cons 50 ang1)
                 (cons 51 ang2))))

;;; Sólido 2D preenchido (hatch simplificado como SOLID)
(defun rot:draw-solid (p1 p2 p3 p4 layname)
  (entmake (list '(0 . "SOLID")
                 (cons 8 layname)
                 (cons 10 p1)
                 (cons 11 p2)
                 (cons 12 p3)
                 (cons 13 p4))))

;;; ============================================================
;;; CÁLCULO DE COTAS DA SEÇÃO TRANSVERSAL
;;; ============================================================

(defun rot:calc-cotas (cota raio_rot raio_ilha larg_pista
                        alt_mf inc_pista /
  raio_eixo z_pe_mf_ext z_mf_ext z_pe_mf_ilha z_mf_ilha
  z_ilh_centro larg_mf inc)

  (setq larg_mf 0.15)
  (setq raio_eixo (/ (+ raio_ilha raio_rot) 2.0))
  (setq inc (/ inc_pista 100.0))

  (setq z_pe_mf_ext  (- cota (* inc (- raio_rot larg_mf raio_eixo))))
  (setq z_mf_ext     (+ z_pe_mf_ext alt_mf))
  (setq z_pe_mf_ilha (+ cota (* inc (- raio_eixo raio_ilha larg_mf))))
  (setq z_mf_ilha    (+ z_pe_mf_ilha alt_mf))
  (setq z_ilh_centro (+ z_mf_ilha 0.25))

  (list
    (cons 'raio_eixo     raio_eixo)
    (cons 'z_eixo        cota)
    (cons 'z_pe_mf_ext   z_pe_mf_ext)
    (cons 'z_mf_ext      z_mf_ext)
    (cons 'z_pe_mf_ilha  z_pe_mf_ilha)
    (cons 'z_mf_ilha     z_mf_ilha)
    (cons 'z_ilh_centro  z_ilh_centro)
    (cons 'larg_mf       larg_mf)))

(defun rot:cota (key cotas)
  (cdr (assoc key cotas)))

;;; ============================================================
;;; ROTATÓRIA CIRCULAR PADRÃO
;;; ============================================================

(defun rot:create-circular (cx cy cotas raio_rot raio_ilha nseg /
  raio_eixo larg_mf z_eixo z_pe_mf_ext z_mf_ext
  z_pe_mf_ilha z_mf_ilha z_ilh_centro)

  (setq raio_eixo    (rot:cota 'raio_eixo    cotas))
  (setq larg_mf      (rot:cota 'larg_mf      cotas))
  (setq z_eixo       (rot:cota 'z_eixo       cotas))
  (setq z_pe_mf_ext  (rot:cota 'z_pe_mf_ext  cotas))
  (setq z_mf_ext     (rot:cota 'z_mf_ext     cotas))
  (setq z_pe_mf_ilha (rot:cota 'z_pe_mf_ilha cotas))
  (setq z_mf_ilha    (rot:cota 'z_mf_ilha    cotas))
  (setq z_ilh_centro (rot:cota 'z_ilh_centro cotas))

  (rot:draw-ring-3d cx cy z_eixo       raio_eixo          nseg "ROT-EIXO")
  (rot:draw-ring-3d cx cy z_pe_mf_ext  (- raio_rot larg_mf) nseg "ROT-BORDAO-EXT")
  (rot:draw-ring-3d cx cy z_mf_ext     raio_rot           nseg "ROT-BORDAO-EXT")
  (rot:draw-ring-3d cx cy z_pe_mf_ilha (+ raio_ilha larg_mf) nseg "ROT-BORDAO-ILH")
  (rot:draw-ring-3d cx cy z_mf_ilha    raio_ilha          nseg "ROT-BORDAO-ILH")
  (rot:draw-point-3d cx cy z_ilh_centro "ROT-ILHA")
  (rot:draw-circle-2d cx cy raio_rot              "ROT-BORDA-2D")
  (rot:draw-circle-2d cx cy raio_eixo             "ROT-BORDA-2D")
  (rot:draw-circle-2d cx cy raio_ilha             "ROT-BORDA-2D")
  (rot:draw-circle-2d cx cy (- raio_rot larg_mf)  "ROT-BORDA-2D")
  (rot:draw-circle-2d cx cy (+ raio_ilha larg_mf) "ROT-BORDA-2D")

  (rot:msg "  Geometria circular criada.")
  (rot:msg (strcat "    Raio externo: " (rtos raio_rot  2 2) " m"))
  (rot:msg (strcat "    Raio eixo:    " (rtos raio_eixo 2 2) " m"))
  (rot:msg (strcat "    Raio ilha:    " (rtos raio_ilha 2 2) " m")))

;;; ============================================================
;;; ROTATÓRIA COM FORMA PERSONALIZADA
;;; ============================================================

(defun rot:elevate-copy-pline (ent_src z_target layname /
  dxf nverts)
  (setq dxf (entget ent_src))
  (if (= (cdr (assoc 0 dxf)) "LWPOLYLINE")
    (progn
      (setq nverts (cdr (assoc 90 dxf)))
      (entmake (list
        '(0 . "LWPOLYLINE") '(100 . "AcDbEntity")
        (cons 8 layname)
        '(100 . "AcDbPolyline")
        (cons 90 nverts)
        (cons 70 (cdr (assoc 70 dxf)))
        (cons 38 z_target)))
      (foreach item dxf
        (if (= (car item) 10)
          (entmake (list (cons 10 (list (cadr item) (caddr item)))))))))
  (rot:msg (strcat "  Copiado para z=" (rtos z_target 2 3))))

(defun rot:create-custom (cx cy cotas ent_borda_ext ent_ilha_base raio_ilha /
  z_mf_ext z_mf_ilha z_ilh_centro larg_mf)
  (setq z_mf_ext     (rot:cota 'z_mf_ext     cotas))
  (setq z_mf_ilha    (rot:cota 'z_mf_ilha    cotas))
  (setq z_ilh_centro (rot:cota 'z_ilh_centro cotas))
  (setq larg_mf      (rot:cota 'larg_mf      cotas))
  (rot:elevate-copy-pline ent_borda_ext z_mf_ext "ROT-BORDAO-EXT")
  (if ent_ilha_base
    (rot:elevate-copy-pline ent_ilha_base z_mf_ilha "ROT-BORDAO-ILH")
    (rot:draw-ring-3d cx cy z_mf_ilha raio_ilha 72 "ROT-BORDAO-ILH"))
  (rot:draw-point-3d cx cy z_ilh_centro "ROT-ILHA")
  (rot:msg "  Forma personalizada aplicada."))

;;; ============================================================
;;; SUPERFÍCIE TIN - PONTOS
;;; ============================================================

(defun rot:create-surface-points (cx cy cotas raio_rot raio_ilha larg_pista /
  raio_eixo larg_mf z_eixo z_pe_mf_ext z_mf_ext
  z_pe_mf_ilha z_mf_ilha z_ilh_centro nseg raio_mid z_mid)

  (setq nseg 48)
  (setq raio_eixo    (rot:cota 'raio_eixo    cotas))
  (setq larg_mf      (rot:cota 'larg_mf      cotas))
  (setq z_eixo       (rot:cota 'z_eixo       cotas))
  (setq z_pe_mf_ext  (rot:cota 'z_pe_mf_ext  cotas))
  (setq z_mf_ext     (rot:cota 'z_mf_ext     cotas))
  (setq z_pe_mf_ilha (rot:cota 'z_pe_mf_ilha cotas))
  (setq z_mf_ilha    (rot:cota 'z_mf_ilha    cotas))
  (setq z_ilh_centro (rot:cota 'z_ilh_centro cotas))

  (rot:draw-point-3d cx cy z_ilh_centro "ROT-SURFACE-PTS")
  (rot:ring-points cx cy z_mf_ilha     raio_ilha              nseg "ROT-SURFACE-PTS")
  (rot:ring-points cx cy z_pe_mf_ilha  (+ raio_ilha larg_mf)  nseg "ROT-SURFACE-PTS")
  (setq raio_mid (/ (+ raio_eixo (+ raio_ilha larg_mf)) 2.0))
  (setq z_mid    (/ (+ z_pe_mf_ilha z_eixo) 2.0))
  (rot:ring-points cx cy z_mid         raio_mid               nseg "ROT-SURFACE-PTS")
  (rot:ring-points cx cy z_eixo        raio_eixo              nseg "ROT-SURFACE-PTS")
  (setq raio_mid2 (/ (+ raio_eixo (- raio_rot larg_mf)) 2.0))
  (setq z_mid2    (/ (+ z_eixo z_pe_mf_ext) 2.0))
  (rot:ring-points cx cy z_mid2        raio_mid2              nseg "ROT-SURFACE-PTS")
  (rot:ring-points cx cy z_pe_mf_ext   (- raio_rot larg_mf)  nseg "ROT-SURFACE-PTS")
  (rot:ring-points cx cy z_mf_ext      raio_rot               nseg "ROT-SURFACE-PTS")
  (rot:msg (strcat "  " (itoa (+ 1 (* nseg 6))) " pontos em ROT-SURFACE-PTS")))

;;; ============================================================
;;; PERFIS DE GREIDE
;;; ============================================================

(defun rot:get-entity-angle (ent cpt / dxf tipo p1 p2)
  (setq dxf (entget ent))
  (setq tipo (cdr (assoc 0 dxf)))
  (cond
    ((= tipo "LINE")
     (setq p1 (cdr (assoc 10 dxf)))
     (setq p2 (cdr (assoc 11 dxf)))
     (atan (- (cadr p2) (cadr p1)) (- (car p2) (car p1))))
    (t
     (setq p1 (cdr (assoc 10 dxf)))
     (atan (- (cadr cpt) (cadr p1)) (- (car cpt) (car p1))))))

(defun rot:create-profile-via (cx cy cota raio_rot cotas ent_via idx /
  ang ang_op px_ent py_ent dist_trans p_ini p_rot_ent p_eixo
  z_eixo z_mf_ext raio_eixo)
  (setq z_eixo   (rot:cota 'z_eixo   cotas))
  (setq z_mf_ext (rot:cota 'z_mf_ext cotas))
  (setq raio_eixo (rot:cota 'raio_eixo cotas))
  (setq ang    (rot:get-entity-angle ent_via (list cx cy 0.0)))
  (setq ang_op (+ ang pi))
  (setq px_ent (+ cx (* raio_rot (cos ang_op)))
        py_ent (+ cy (* raio_rot (sin ang_op))))
  (setq dist_trans 20.0)
  (setq p_ini     (list (+ px_ent (* dist_trans (cos ang)))
                        (+ py_ent (* dist_trans (sin ang)))
                        cota))
  (setq p_rot_ent (list px_ent py_ent z_mf_ext))
  (setq p_eixo    (list (+ cx (* raio_eixo (cos ang_op)))
                        (+ cy (* raio_eixo (sin ang_op)))
                        z_eixo))
  (rot:draw-point-3d (car p_ini) (cadr p_ini) (caddr p_ini) "ROT-PERFIL-AUX")
  (rot:draw-point-3d px_ent py_ent z_mf_ext "ROT-PERFIL-AUX")
  (rot:draw-point-3d (car p_eixo) (cadr p_eixo) (caddr p_eixo) "ROT-PERFIL-AUX")
  (rot:line-3d p_ini p_rot_ent "ROT-PERFIL-AUX")
  (rot:line-3d p_rot_ent p_eixo "ROT-PERFIL-AUX")
  (entmake (list '(0 . "TEXT") (cons 8 "ROT-PERFIL-AUX")
                 (cons 10 (list (+ px_ent 1.0) (+ py_ent 1.0) z_mf_ext))
                 (cons 40 1.5)
                 (cons 1 (strcat "ENTRADA VIA " (itoa idx) " z=" (rtos z_mf_ext 2 3)))))
  (rot:msg (strcat "  Via " (itoa idx) ": borda z=" (rtos z_mf_ext 2 3)
                   " | eixo z=" (rtos z_eixo 2 3))))

(defun rot:create-profiles (cx cy cota raio_rot raio_ilha cotas ent1 ent2)
  (rot:msg "\nCriando referências de perfil...")
  (rot:create-profile-via cx cy cota raio_rot cotas ent1 1)
  (rot:create-profile-via cx cy cota raio_rot cotas ent2 2)
  (rot:msg "  >> Civil 3D: Profile > Create Profile by Layout"))

;;; ============================================================
;;; ============================================================
;;;   MÓDULO DE VIAS DE ACESSO
;;; ============================================================
;;; ============================================================

;;; ------------------------------------------------------------
;;; Parâmetros de uma via de acesso (struct como lista assoc)
;;; ------------------------------------------------------------
;;;
;;;  Seção transversal de uma via de acesso à rotatória:
;;;
;;;  [calçada esq]--[bordo esq]--[faixas esq]--[eixo]
;;;                              --[faixas dir]--[bordo dir]--[calçada dir]
;;;
;;;  Na aproximação da rotatória:
;;;  - a via ALARGA para criar faixas de entrada extra
;;;  - nariz da ilha canalizadora divide entrada/saída
;;;  - raio de concordância no bordo (entrada e saída)

;;; Cria a seção reta de uma via simples (longe da rotatória)
(defun via:draw-secao-reta (p_ini ang n_faixas_ent n_faixas_sai
                             larg_faixa larg_calcada alt_mf
                             comp_reta cota_ini decl_long layname_base /
  larg_pista_esq larg_pista_dir
  larg_tot_esq larg_tot_dir
  p_fim ang_perp
  pe_esq pe_dir pb_esq pb_dir pc_esq pc_dir
  pf_esq pf_dir pfb_esq pfb_dir pfc_esq pfc_dir
  z_ini z_fim z_bord_ini z_bord_fim i px_ini px_fim)

  (setq larg_pista_esq (* n_faixas_sai  larg_faixa))
  (setq larg_pista_dir (* n_faixas_ent  larg_faixa))
  (setq larg_tot_esq (+ larg_pista_esq larg_calcada))
  (setq larg_tot_dir (+ larg_pista_dir larg_calcada))

  (setq p_fim (rot:pt-polar p_ini ang comp_reta))
  (setq ang_perp (rot:ang-esq ang))
  (setq z_ini cota_ini)
  (setq z_fim (- cota_ini (* decl_long comp_reta)))
  (setq z_bord_ini (+ z_ini alt_mf))
  (setq z_bord_fim (+ z_fim alt_mf))

  ;; Pontos início (perpendicular ao eixo)
  (setq pe_esq (rot:pt-polar p_ini ang_perp larg_pista_esq))
  (setq pe_dir (rot:pt-polar p_ini (rot:ang-dir ang) larg_pista_dir))
  (setq pb_esq (rot:pt-polar p_ini ang_perp larg_tot_esq))
  (setq pb_dir (rot:pt-polar p_ini (rot:ang-dir ang) larg_tot_dir))

  ;; Pontos fim
  (setq pf_esq (rot:pt-polar p_fim ang_perp larg_pista_esq))
  (setq pf_dir (rot:pt-polar p_fim (rot:ang-dir ang) larg_pista_dir))
  (setq pfb_esq (rot:pt-polar p_fim ang_perp larg_tot_esq))
  (setq pfb_dir (rot:pt-polar p_fim (rot:ang-dir ang) larg_tot_dir))

  ;; --- Eixo da via ---
  (rot:line-3d (list (car p_ini)   (cadr p_ini)   z_ini)
               (list (car p_fim)   (cadr p_fim)   z_fim)
               "VIA-EIXO")

  ;; --- Bordos da pista ---
  (rot:line-3d (list (car pe_esq) (cadr pe_esq) z_ini)
               (list (car pf_esq) (cadr pf_esq) z_fim) "VIA-BORDO-ESQ")
  (rot:line-3d (list (car pe_dir) (cadr pe_dir) z_ini)
               (list (car pf_dir) (cadr pf_dir) z_fim) "VIA-BORDO-DIR")

  ;; --- Bordos da calçada (topo do meio-fio) ---
  (rot:line-3d (list (car pb_esq) (cadr pb_esq) z_bord_ini)
               (list (car pfb_esq) (cadr pfb_esq) z_bord_fim) "VIA-CALCADA-ESQ")
  (rot:line-3d (list (car pb_dir) (cadr pb_dir) z_bord_ini)
               (list (car pfb_dir) (cadr pfb_dir) z_bord_fim) "VIA-CALCADA-DIR")

  ;; --- Linhas de faixa (tracejadas) ---
  (setq i 1)
  (while (< i n_faixas_ent)
    (setq px_ini (rot:pt-polar p_ini (rot:ang-dir ang) (* i larg_faixa)))
    (setq px_fim (rot:pt-polar p_fim (rot:ang-dir ang) (* i larg_faixa)))
    (rot:line-3d (list (car px_ini) (cadr px_ini) z_ini)
                 (list (car px_fim) (cadr px_fim) z_fim) "VIA-FAIXA")
    (setq i (1+ i)))
  (setq i 1)
  (while (< i n_faixas_sai)
    (setq px_ini (rot:pt-polar p_ini ang_perp (* i larg_faixa)))
    (setq px_fim (rot:pt-polar p_fim ang_perp (* i larg_faixa)))
    (rot:line-3d (list (car px_ini) (cadr px_ini) z_ini)
                 (list (car px_fim) (cadr px_fim) z_fim) "VIA-FAIXA")
    (setq i (1+ i)))

  ;; Retorna pontos do início para uso na transição
  (list
    (cons 'p_ini     p_ini)
    (cons 'p_fim     p_fim)
    (cons 'pe_esq    pe_esq)
    (cons 'pe_dir    pe_dir)
    (cons 'pb_esq    pb_esq)
    (cons 'pb_dir    pb_dir)
    (cons 'pf_esq    pf_esq)
    (cons 'pf_dir    pf_dir)
    (cons 'pfb_esq   pfb_esq)
    (cons 'pfb_dir   pfb_dir)
    (cons 'ang       ang)
    (cons 'z_fim     z_fim)
    (cons 'z_bord_fim z_bord_fim)
    (cons 'larg_pista_esq larg_pista_esq)
    (cons 'larg_pista_dir larg_pista_dir)
    (cons 'larg_tot_esq larg_tot_esq)
    (cons 'larg_tot_dir larg_tot_dir)))

;;; ------------------------------------------------------------
;;; Seção de transição / alargamento antes da rotatória
;;; A faixa de entrada extra é criada pelo ALARGAMENTO à direita
;;; A ilha canalizadora divide entrada (dir) e saída (esq)
;;; ------------------------------------------------------------

(defun via:draw-transicao (params_reta n_faixas_ent_extra n_faixas_sai_extra
                            larg_faixa larg_calcada alt_mf
                            raio_nariz comp_trans comp_canal
                            cx cy raio_rot cota_rot /
  ang ang_op p_inicio
  p_eixo_rot p_bordo_rot_ent p_bordo_rot_sai
  larg_esq_ini larg_dir_ini larg_esq_fim larg_dir_fim
  z_ini z_fim ang_perp
  p_fai_esq_ini p_fai_esq_fim p_fai_dir_ini p_fai_dir_fim
  p_bordo_esq_ini p_bordo_esq_fim p_bordo_dir_ini p_bordo_dir_fim
  p_calc_esq_ini p_calc_esq_fim p_calc_dir_ini p_calc_dir_fim
  p_nariz ang_nariz
  p_ilha_esq p_ilha_dir p_ilha_tip
  ang_ent ang_sai)

  (setq ang          (cdr (assoc 'ang params_reta)))
  (setq ang_op       (+ ang pi))
  (setq p_inicio     (cdr (assoc 'p_fim params_reta)))
  (setq z_ini        (cdr (assoc 'z_fim params_reta)))
  (setq ang_perp     (rot:ang-esq ang))

  ;; Larguras no início da transição (= fim da seção reta)
  (setq larg_esq_ini (cdr (assoc 'larg_pista_esq params_reta)))
  (setq larg_dir_ini (cdr (assoc 'larg_pista_dir params_reta)))

  ;; Larguras no fim da transição (com faixas extras)
  (setq larg_esq_fim (+ larg_esq_ini (* n_faixas_sai_extra larg_faixa)))
  (setq larg_dir_fim (+ larg_dir_ini (* n_faixas_ent_extra larg_faixa)))

  ;; Cota no fim da transição (encontra a rotatória)
  (setq z_fim cota_rot)

  ;; Ponto de fim da transição = ponto no bordo externo da rotatória
  ;; O eixo da via encontra a borda da rotatória
  (setq p_bordo_rot (list (+ cx (* raio_rot (cos ang_op)))
                          (+ cy (* raio_rot (sin ang_op)))
                          0.0))

  ;; Pontos da transição (início)
  (setq p_fai_esq_ini  (rot:pt-polar p_inicio ang_perp larg_esq_ini))
  (setq p_fai_dir_ini  (rot:pt-polar p_inicio (rot:ang-dir ang) larg_dir_ini))
  (setq p_calc_esq_ini (rot:pt-polar p_inicio ang_perp
                         (+ larg_esq_ini larg_calcada)))
  (setq p_calc_dir_ini (rot:pt-polar p_inicio (rot:ang-dir ang)
                         (+ larg_dir_ini larg_calcada)))

  ;; Pontos da transição (fim - na borda da rotatória)
  (setq p_fai_esq_fim  (rot:pt-polar p_bordo_rot ang_perp larg_esq_fim))
  (setq p_fai_dir_fim  (rot:pt-polar p_bordo_rot (rot:ang-dir ang) larg_dir_fim))
  (setq p_calc_esq_fim (rot:pt-polar p_bordo_rot ang_perp
                         (+ larg_esq_fim larg_calcada)))
  (setq p_calc_dir_fim (rot:pt-polar p_bordo_rot (rot:ang-dir ang)
                         (+ larg_dir_fim larg_calcada)))

  ;; --- Eixo (continua até a rotatória) ---
  (rot:line-3d (list (car p_inicio)    (cadr p_inicio)    z_ini)
               (list (car p_bordo_rot) (cadr p_bordo_rot) z_fim) "VIA-EIXO")

  ;; --- Bordos da pista (com alargamento) ---
  (rot:line-3d (list (car p_fai_esq_ini) (cadr p_fai_esq_ini) z_ini)
               (list (car p_fai_esq_fim) (cadr p_fai_esq_fim) z_fim) "VIA-BORDO-ESQ")
  (rot:line-3d (list (car p_fai_dir_ini) (cadr p_fai_dir_ini) z_ini)
               (list (car p_fai_dir_fim) (cadr p_fai_dir_fim) z_fim) "VIA-BORDO-DIR")

  ;; --- Calçadas (com alargamento) ---
  (rot:line-3d (list (car p_calc_esq_ini) (cadr p_calc_esq_ini) (+ z_ini alt_mf))
               (list (car p_calc_esq_fim) (cadr p_calc_esq_fim) (+ z_fim alt_mf)) "VIA-CALCADA-ESQ")
  (rot:line-3d (list (car p_calc_dir_ini) (cadr p_calc_dir_ini) (+ z_ini alt_mf))
               (list (car p_calc_dir_fim) (cadr p_calc_dir_fim) (+ z_fim alt_mf)) "VIA-CALCADA-DIR")

  ;; --- Ilha canalizadora (nariz) ---
  ;; A ilha fica entre a faixa de saída (esq) e entrada (dir)
  ;; Posicionada ao longo do eixo, deslocada para o lado da saída
  (setq p_nariz_base p_bordo_rot)
  (setq larg_ilha_canal (* 0.5 larg_faixa)) ; largura da ilha canalizadora

  ;; Nariz da ilha (ponta que aponta para a via)
  (setq p_ilha_tip   (rot:pt-polar p_inicio ang (* 0.3 comp_trans)))
  ;; Bordas da ilha
  (setq p_ilha_esq   (rot:pt-polar p_nariz_base ang_perp larg_ilha_canal))
  (setq p_ilha_dir   p_nariz_base) ; borda direita = eixo da pista

  ;; Polilinha da ilha canalizadora (triângulo/gota)
  (rot:line-3d (list (car p_ilha_tip)  (cadr p_ilha_tip)  z_ini)
               (list (car p_ilha_esq)  (cadr p_ilha_esq)  z_fim)  "VIA-ILHA-CANAL")
  (rot:line-3d (list (car p_ilha_tip)  (cadr p_ilha_tip)  z_ini)
               (list (car p_ilha_dir)  (cadr p_ilha_dir)  z_fim)  "VIA-ILHA-CANAL")
  (rot:line-3d (list (car p_ilha_esq)  (cadr p_ilha_esq)  z_fim)
               (list (car p_ilha_dir)  (cadr p_ilha_dir)  z_fim)  "VIA-ILHA-CANAL")

  ;; --- Raio de concordância na entrada (curva no bordo dir) ---
  ;; Simplificado: arco 2D no ponto de entrada da pista
  (rot:draw-arc-2d
    (car (rot:pt-polar p_fai_dir_fim (rot:ang-dir ang) raio_nariz))
    (cadr (rot:pt-polar p_fai_dir_fim (rot:ang-dir ang) raio_nariz))
    raio_nariz
    ang_op
    (rot:ang-esq ang_op)
    "VIA-RAIO-ENT")

  ;; --- Raio de concordância na saída (curva no bordo esq) ---
  (rot:draw-arc-2d
    (car (rot:pt-polar p_fai_esq_fim ang_perp raio_nariz))
    (cadr (rot:pt-polar p_fai_esq_fim ang_perp raio_nariz))
    raio_nariz
    (rot:ang-dir ang_op)
    ang_op
    "VIA-RAIO-SAI")

  ;; --- Pontos para superfície TIN da via ---
  (via:ring-pts-transicao p_inicio p_bordo_rot ang
                           larg_esq_ini larg_dir_ini
                           larg_esq_fim larg_dir_fim
                           larg_calcada alt_mf z_ini z_fim 6))

;;; Pontos de seção transversal interpolados ao longo da transição
(defun via:ring-pts-transicao (p_ini p_fim ang
                                larg_esq_i larg_dir_i
                                larg_esq_f larg_dir_f
                                larg_calcada alt_mf z_ini z_fim
                                n_sec /
  step i t_param p_eixo ang_perp
  larg_esq larg_dir z_sec
  pt_esq pt_dir pt_calc_esq pt_calc_dir)
  (setq step (/ 1.0 (float (max n_sec 2))))
  (setq ang_perp (rot:ang-esq ang))
  (setq i 0)
  (while (<= i n_sec)
    (setq t_param (* i step))
    ;; Interpolação linear
    (setq p_eixo    (list (+ (car p_ini) (* t_param (- (car p_fim) (car p_ini))))
                          (+ (cadr p_ini) (* t_param (- (cadr p_fim) (cadr p_ini))))
                          0.0))
    (setq larg_esq  (+ larg_esq_i (* t_param (- larg_esq_f larg_esq_i))))
    (setq larg_dir  (+ larg_dir_i (* t_param (- larg_dir_f larg_dir_i))))
    (setq z_sec     (+ z_ini (* t_param (- z_fim z_ini))))
    ;; Pontos da seção
    (setq pt_esq      (rot:pt-polar p_eixo ang_perp larg_esq))
    (setq pt_dir      (rot:pt-polar p_eixo (rot:ang-dir ang) larg_dir))
    (setq pt_calc_esq (rot:pt-polar p_eixo ang_perp (+ larg_esq larg_calcada)))
    (setq pt_calc_dir (rot:pt-polar p_eixo (rot:ang-dir ang) (+ larg_dir larg_calcada)))
    ;; Plota pontos 3D
    (rot:draw-point-3d (car p_eixo)      (cadr p_eixo)      z_sec "VIA-SURFACE-PTS")
    (rot:draw-point-3d (car pt_esq)      (cadr pt_esq)      z_sec "VIA-SURFACE-PTS")
    (rot:draw-point-3d (car pt_dir)      (cadr pt_dir)      z_sec "VIA-SURFACE-PTS")
    (rot:draw-point-3d (car pt_calc_esq) (cadr pt_calc_esq) (+ z_sec alt_mf) "VIA-SURFACE-PTS")
    (rot:draw-point-3d (car pt_calc_dir) (cadr pt_calc_dir) (+ z_sec alt_mf) "VIA-SURFACE-PTS")
    (setq i (1+ i))))

;;; ------------------------------------------------------------
;;; Cria UMA via de acesso completa
;;; Chamado para cada via selecionada pelo usuário
;;; ------------------------------------------------------------

(defun via:create-one (ent_via cx cy raio_rot cota_rot
                        n_faixas_ent n_faixas_sai n_faixas_ent_extra
                        larg_faixa larg_calcada alt_mf
                        comp_reta comp_trans raio_nariz
                        decl_long idx /
  ang ang_op p_borda p_ini_reta cota_ini params_reta)

  (rot:msg (strcat "\n  --- Via " (itoa idx) " ---"))

  ;; Direção: do objeto para o centro
  (setq ang    (rot:get-entity-angle ent_via (list cx cy 0.0)))
  (setq ang_op (+ ang pi))

  ;; Ponto na borda da rotatória (onde a via termina)
  (setq p_borda (list (+ cx (* raio_rot (cos ang_op)))
                      (+ cy (* raio_rot (sin ang_op)))
                      0.0))

  ;; Início da seção reta (longe da rotatória)
  (setq p_ini_reta (rot:pt-polar p_borda ang (+ comp_reta comp_trans)))

  ;; Cota no início (sobe conforme declividade longitudinal)
  (setq cota_ini (+ cota_rot (* decl_long (+ comp_reta comp_trans))))

  ;; 1. Seção reta
  (setq params_reta
    (via:draw-secao-reta p_ini_reta ang
                          n_faixas_ent n_faixas_sai
                          larg_faixa larg_calcada alt_mf
                          comp_reta cota_ini decl_long
                          "VIA"))

  ;; 2. Transição / alargamento
  (via:draw-transicao params_reta
                       n_faixas_ent_extra 0
                       larg_faixa larg_calcada alt_mf
                       raio_nariz comp_trans comp_trans
                       cx cy raio_rot cota_rot)

  (rot:msg (strcat "    Início: (" (rtos (car p_ini_reta) 2 1)
                   ", " (rtos (cadr p_ini_reta) 2 1) ")"
                   " cota " (rtos cota_ini 2 3)))
  (rot:msg (strcat "    Borda:  (" (rtos (car p_borda) 2 1)
                   ", " (rtos (cadr p_borda) 2 1) ")"
                   " cota " (rtos cota_rot 2 3))))

;;; ------------------------------------------------------------
;;; COMANDO ROT-VIAS — cria vias de acesso para rotatória
;;; existente (pode ser chamado standalone ou pelo ROTATORIO)
;;; ------------------------------------------------------------

(defun c:ROT-VIAS (/
  n_vias i ent_via cx cy raio_rot cota_rot
  n_faixas_ent n_faixas_sai n_faixas_ent_extra
  larg_faixa larg_calcada alt_mf
  comp_reta comp_trans raio_nariz decl_long
  cpt resp)

  (rot:msg "\n============================================")
  (rot:msg " ROT-VIAS - Vias de Acesso à Rotatória")
  (rot:msg "============================================")

  (rot:init-layers)

  ;; --- Centro da rotatória ---
  (setq cpt (getpoint "\n  Clique no centro da rotatória: "))
  (if (not cpt) (progn (rot:msg "Cancelado.") (exit)))
  (setq cx (car cpt) cy (cadr cpt))

  ;; --- Parâmetros da rotatória ---
  (setq raio_rot  (rot:getreal-default "  Raio externo da rotatória (m)" 15.0))
  (setq cota_rot  (rot:getreal-default "  Cota do eixo da rotatória (m)" 100.0))

  ;; --- Parâmetros das vias ---
  (rot:msg "\n  Parâmetros das Vias de Acesso:")
  (setq n_faixas_ent       (rot:getint-default  "  Nº de faixas de ENTRADA por via" 1))
  (setq n_faixas_sai       (rot:getint-default  "  Nº de faixas de SAÍDA por via" 1))
  (setq n_faixas_ent_extra (rot:getint-default  "  Nº de faixas extras de entrada (alargamento)" 1))
  (setq larg_faixa         (rot:getreal-default "  Largura de cada faixa (m)" 3.5))
  (setq larg_calcada       (rot:getreal-default "  Largura da calçada (m)" 2.5))
  (setq alt_mf             (rot:getreal-default "  Altura do meio-fio (m)" 0.15))
  (setq comp_reta          (rot:getreal-default "  Comprimento da seção reta da via (m)" 80.0))
  (setq comp_trans         (rot:getreal-default "  Comprimento da transição/alargamento (m)" 30.0))
  (setq raio_nariz         (rot:getreal-default "  Raio de concordância de entrada/saída (m)" 8.0))
  (setq decl_long          (rot:getreal-default "  Declividade longitudinal (%)" 2.0))
  (setq decl_long          (/ decl_long 100.0))

  ;; --- Quantas vias? ---
  (setq n_vias (rot:getint-default "  Nº de vias de acesso a criar" 4))

  ;; --- Seleciona e cria cada via ---
  (setq i 1)
  (while (<= i n_vias)
    (rot:msg (strcat "\n  Selecione o eixo da Via " (itoa i)
                     " (polilinha/linha apontando para a rotatória):"))
    (setq ent_via (car (entsel)))
    (if ent_via
      (via:create-one ent_via cx cy raio_rot cota_rot
                       n_faixas_ent n_faixas_sai n_faixas_ent_extra
                       larg_faixa larg_calcada alt_mf
                       comp_reta comp_trans raio_nariz
                       decl_long i)
      (rot:msg (strcat "  Via " (itoa i) " ignorada (sem seleção).")))
    (setq i (1+ i)))

  (rot:msg "\nVias de acesso criadas.")
  (rot:msg "  Camadas: VIA-EIXO | VIA-BORDO-ESQ | VIA-BORDO-DIR")
  (rot:msg "           VIA-CALCADA-ESQ | VIA-CALCADA-DIR")
  (rot:msg "           VIA-ILHA-CANAL  | VIA-RAIO-ENT | VIA-RAIO-SAI")
  (rot:msg "           VIA-SURFACE-PTS (pontos para superfície)")
  (princ))

;;; ============================================================
;;; COMANDO PRINCIPAL: ROTATORIO (inclui opção de vias)
;;; ============================================================

(defun c:ROTATORIO (/
  ent1 ent2 cpt cx cy
  cota raio_rot raio_ilha larg_pista alt_mf inc_pista
  cotas modo_forma resp
  ent_borda_ext ent_ilha_base nseg)

  (rot:msg "\n============================================")
  (rot:msg " ROTATORIO v2.0 - Civil 3D 2026")
  (rot:msg " ROT-AJUDA para instruções completas")
  (rot:msg "============================================")

  (rot:init-layers)

  ;; --- SELEÇÃO DAS VIAS PRINCIPAIS ---
  (rot:msg "\nPasso 1: Eixos das vias principais")
  (setq ent1 (car (entsel "\n  Via 1 (polilinha/linha): ")))
  (if (not ent1) (progn (rot:msg "Cancelado.") (exit)))
  (setq ent2 (car (entsel "\n  Via 2 (polilinha/linha): ")))
  (if (not ent2) (progn (rot:msg "Cancelado.") (exit)))

  ;; --- CENTRO ---
  (rot:msg "\nPasso 2: Centro")
  (setq cpt (rot:find-intersection ent1 ent2))
  (if cpt
    (progn
      (rot:msg (strcat "  Interseção: (" (rtos (car cpt) 2 2) ", " (rtos (cadr cpt) 2 2) ")"))
      (initget "Sim Nao")
      (setq resp (getkword "  Usar este ponto? [Sim/Nao] <Sim>: "))
      (if (= resp "Nao") (setq cpt (getpoint "\n  Clique no centro: "))))
    (setq cpt (getpoint "\n  Clique no centro da rotatória: ")))
  (if (not cpt) (progn (rot:msg "Cancelado.") (exit)))
  (setq cx (car cpt) cy (cadr cpt))

  ;; --- PARÂMETROS DA ROTATÓRIA ---
  (rot:msg "\nPasso 3: Parâmetros")
  (setq cota      (rot:getreal-default "\n  Cota do eixo de rolamento (m)" 100.0))
  (setq raio_rot  (rot:getreal-default "  Raio externo - borda externa (m)" 15.0))
  (setq raio_ilha (rot:getreal-default "  Raio da ilha central (m)" 4.0))
  (setq larg_pista (- raio_rot raio_ilha))
  (setq alt_mf    (rot:getreal-default "  Altura do meio-fio (m)" 0.15))
  (setq inc_pista (rot:getreal-default "  Inclinação transversal da pista (%)" 3.0))

  (if (>= raio_ilha raio_rot)
    (progn (alert "ERRO: Raio da ilha deve ser menor que o raio externo!") (exit)))

  (setq cotas (rot:calc-cotas cota raio_rot raio_ilha larg_pista alt_mf inc_pista))

  ;; --- FORMA ---
  (rot:msg "\nPasso 4: Forma")
  (initget "Circular Personalizada")
  (setq modo_forma (rot:getkw-default "  Forma [Circular/Personalizada]" "Circular Personalizada" "Circular"))
  (setq nseg 72)

  (if (= modo_forma "Personalizada")
    (progn
      (rot:msg "\n  Borda EXTERNA:")
      (setq ent_borda_ext (car (entsel)))
      (rot:msg "  ILHA CENTRAL (ENTER = circular):")
      (setq ent_ilha_base (car (entsel "\n  Ilha (ENTER=circular): ")))
      (rot:create-custom cx cy cotas ent_borda_ext ent_ilha_base raio_ilha))
    (rot:create-circular cx cy cotas raio_rot raio_ilha nseg))

  ;; --- SUPERFÍCIE ---
  (initget "Sim Nao")
  (setq resp (rot:getkw-default "\nPontos para Superfície TIN? [Sim/Nao]" "Sim Nao" "Sim"))
  (if (= resp "Sim")
    (rot:create-surface-points cx cy cotas raio_rot raio_ilha larg_pista))

  ;; --- PERFIS ---
  (initget "Sim Nao")
  (setq resp (rot:getkw-default "Referências de Perfil? [Sim/Nao]" "Sim Nao" "Sim"))
  (if (= resp "Sim")
    (rot:create-profiles cx cy cota raio_rot raio_ilha cotas ent1 ent2))

  ;; --- VIAS DE ACESSO ---
  (initget "Sim Nao")
  (setq resp (rot:getkw-default "Criar Vias de Acesso agora? [Sim/Nao]" "Sim Nao" "Sim"))
  (if (= resp "Sim")
    (progn
      (rot:msg "\n--- VIAS DE ACESSO ---")
      (rot:msg "Parâmetros das vias (aplicados a todas):")
      ;; Reutiliza parâmetros da rotatória + pede específicos das vias
      (setq n_faixas_ent       (rot:getint-default  "  Faixas de entrada" 1))
      (setq n_faixas_sai       (rot:getint-default  "  Faixas de saída"   1))
      (setq n_faixas_ent_extra (rot:getint-default  "  Faixas extras de entrada (alargamento)" 1))
      (setq larg_faixa         (rot:getreal-default "  Largura da faixa (m)" 3.5))
      (setq larg_calcada       (rot:getreal-default "  Largura da calçada (m)" 2.5))
      (setq comp_reta          (rot:getreal-default "  Comprimento da seção reta (m)" 80.0))
      (setq comp_trans         (rot:getreal-default "  Comprimento da transição (m)" 30.0))
      (setq raio_nariz         (rot:getreal-default "  Raio de concordância entrada/saída (m)" 8.0))
      (setq decl_long          (/ (rot:getreal-default "  Declividade longitudinal (%)" 2.0) 100.0))
      (setq n_vias_ac (rot:getint-default "  Nº de vias de acesso" 4))
      (setq i 1)
      (while (<= i n_vias_ac)
        (rot:msg (strcat "\n  Selecione o eixo da Via " (itoa i) ":"))
        (setq ent_via_ac (car (entsel)))
        (if ent_via_ac
          (via:create-one ent_via_ac cx cy raio_rot cota
                           n_faixas_ent n_faixas_sai n_faixas_ent_extra
                           larg_faixa larg_calcada alt_mf
                           comp_reta comp_trans raio_nariz decl_long i))
        (setq i (1+ i)))))

  ;; Relatório final
  (rot:msg "\n=== RESUMO ===")
  (rot:msg (strcat "  Cota eixo:   " (rtos (rot:cota 'z_eixo   cotas) 2 3) " m"))
  (rot:msg (strcat "  Cota mf ext: " (rtos (rot:cota 'z_mf_ext cotas) 2 3) " m"))
  (rot:msg (strcat "  Cota ilha:   " (rtos (rot:cota 'z_mf_ilha cotas) 2 3) " m"))
  (rot:msg "  Camadas ROT-* e VIA-* criadas.")
  (rot:msg "\nConcluído. Execute ROT-AJUDA para próximos passos.")
  (princ))

;;; ============================================================
;;; COMANDO: ROT-FORMA
;;; ============================================================

(defun c:ROT-FORMA (/ ent_base larg_pista raio_ilha)
  (rot:msg "\n=== ROT-FORMA: Bordas por polilinha personalizada ===")
  (setq ent_base (car (entsel "\n  Polilinha guia do eixo da forma: ")))
  (if (not ent_base) (progn (rot:msg "Cancelado.") (exit)))
  (setq larg_pista (rot:getreal-default "  Largura da pista de rolamento (m)" 4.5))
  (setq raio_ilha  (rot:getreal-default "  Raio para ilha interna (m)" 3.0))
  (rot:msg "  Criando offset externo (borda):")
  (command "_OFFSET" larg_pista ent_base pause "")
  (rot:msg "  Criando offset interno (ilha):")
  (command "_OFFSET" raio_ilha ent_base pause "")
  (rot:msg "\nOffsets criados. Execute ROTATORIO > modo Personalizada.")
  (princ))

;;; ============================================================
;;; INTERSEÇÃO DE ENTIDADES (utilitário)
;;; ============================================================

(defun rot:find-intersection (ent1 ent2 / d1 d2 p1s p1e p2s p2e)
  (setq d1 (entget ent1) d2 (entget ent2))
  (setq p1s (cdr (assoc 10 d1)) p1e (cdr (assoc 11 d1)))
  (setq p2s (cdr (assoc 10 d2)) p2e (cdr (assoc 11 d2)))
  (if (and p1s p1e p2s p2e)
    (rot:intersect-lines p1s p1e p2s p2e)
    nil))

;;; ============================================================
;;; COMANDO: ROT-AJUDA
;;; ============================================================

(defun c:ROT-AJUDA ()
  (alert (strcat
"=== ROTATORIO.LSP v2.0 - AJUDA ===\n\n"
"COMANDOS:\n"
"  ROTATORIO   Rotatória completa (geometria + vias)\n"
"  ROT-VIAS    Cria apenas as vias de acesso\n"
"  ROT-FORMA   Gera bordas por offset de polilinha guia\n"
"  ROT-AJUDA   Esta tela\n\n"
"SEÇÃO TRANSVERSAL DA VIA (criada por ROT-VIAS):\n"
"  [calçada esq]-[meio-fio]-[faixas saída]-[eixo]-[faixas entrada]-[meio-fio]-[calçada dir]\n"
"  Na aproximação: via alarga com faixas extras de entrada\n"
"  Ilha canalizadora divide fluxos entrada/saída\n"
"  Raio de concordância no nariz (entrada e saída)\n\n"
"CAMADAS - ROTATÓRIA:\n"
"  ROT-EIXO          Eixo 3D da pista\n"
"  ROT-BORDAO-EXT    Meio-fio externo 3D\n"
"  ROT-BORDAO-ILH    Meio-fio da ilha 3D\n"
"  ROT-SURFACE-PTS   Pontos TIN da rotatória\n"
"  ROT-PERFIL-AUX    Referências de greide\n\n"
"CAMADAS - VIAS DE ACESSO:\n"
"  VIA-EIXO          Eixo da via (CENTER)\n"
"  VIA-BORDO-ESQ/DIR Bordo da pista (esq/dir)\n"
"  VIA-CALCADA-ESQ/DIR Topo do meio-fio\n"
"  VIA-FAIXA         Marcas de faixa (DASHED)\n"
"  VIA-ILHA-CANAL    Ilha canalizadora\n"
"  VIA-RAIO-ENT/SAI  Arcos de concordância\n"
"  VIA-SURFACE-PTS   Pontos TIN das vias\n\n"
"SUPERFÍCIE NO CIVIL 3D:\n"
"  Home > Surfaces > Create Surface > TIN Surface\n"
"  Add Point Group com ROT-SURFACE-PTS + VIA-SURFACE-PTS\n\n"
"PERFIS NO CIVIL 3D:\n"
"  Profile > Create Profile by Layout\n"
"  PVIs nos pontos de ROT-PERFIL-AUX\n"
))
  (princ))

;;; ============================================================
(rot:msg "\nROTATORIO.LSP v2.0 carregado.")
(rot:msg "Comandos: ROTATORIO | ROT-VIAS | ROT-FORMA | ROT-AJUDA")
(princ)
