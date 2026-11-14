;;; PILHAS.LSP
;;; Cria pontos nas bordas dos 4 poligonos com cotas da superficie
;;; e depois cria superficies TIN + superficies de volume

(vl-load-com)

;;; Conecta ao documento Civil 3D
(defun c3d:doc (/ app)
  (setq app nil)
  (foreach id (quote ("AeccXUiLand.AeccApplication.14.0"
                "AeccXUiLand.AeccApplication.13.6"
                "AeccXUiLand.AeccApplication.13.5"
                "AeccXUiLand.AeccApplication.13.4"
                "AeccXUiLand.AeccApplication.13.3"))
    (if (null app)
      (vl-catch-all-apply
        (function (lambda ()
          (setq app (vlax-get-property
                      (vlax-get-or-create-object id) (quote ActiveDocument)))
        ))
      )
    )
  )
  app
)

;;; Busca superficie pelo nome
(defun c3d:surf (doc nm / r)
  (setq r nil)
  (vl-catch-all-apply
    (function (lambda ()
      (vlax-for s (vlax-get doc (quote Surfaces))
        (if (= nm (vlax-get s (quote Name))) (setq r s))
      )
    ))
  )
  r
)

;;; Samplea elevacao da superficie em X,Y
(defun surf:z (surf x y / r)
  (setq r (vl-catch-all-apply
    (function (lambda () (vlax-invoke surf (quote FindElevationAtXY) x y)))
  ))
  (if (vl-catch-all-error-p r) nil r)
)

;;; Extrai vertices de LWPOLYLINE
(defun poly:verts (ent / d v)
  (setq d (entget ent) v (quote ()))
  (foreach p d (if (= (car p) 10) (setq v (append v (list (cdr p))))))
  v
)

;;; Cria layer se nao existir
(defun mk:layer (nm col)
  (if (null (tblsearch "LAYER" nm))
    (entmake (list (quote (0 . "LAYER")) (cons 2 nm) (quote (70 . 0)) (cons 62 col)))
  )
)

;;; Cria ponto AutoCAD com Z correto
(defun mk:pt (x y z lyr)
  (entmake (list (quote (0 . "POINT")) (cons 8 lyr) (cons 10 (list x y z))))
)

;;; Adiciona ponto Civil 3D COGO
(defun add:cogo (doc x y z desc / pts pt)
  (setq pt nil)
  (vl-catch-all-apply
    (function (lambda ()
      (setq pts (vlax-get doc (quote CogoPoints)))
      (setq pt (vlax-invoke pts (quote Add)))
      (vlax-put pt (quote Northing) y)
      (vlax-put pt (quote Easting) x)
      (vlax-put pt (quote Elevation) z)
      (vlax-put pt (quote RawDescription) desc)
    ))
  )
  pt
)

;;; Cria grupo de pontos por descricao
(defun mk:ptgroup (doc nm desc / pgs pg fil)
  (setq pg nil)
  (vl-catch-all-apply
    (function (lambda ()
      (setq pgs (vlax-get doc (quote PointGroups)))
      (setq pg (vlax-invoke pgs (quote Add) nm))
      (vlax-put pg (quote Description) desc)
      (setq fil (vlax-get pg (quote IncludeFilter)))
      (vlax-put fil (quote RawDescriptionsMatchList) desc)
    ))
  )
  pg
)

;;; Cria superficie TIN vazia
(defun mk:surface (doc nm lyr / surfs s)
  (setq s nil)
  (vl-catch-all-apply
    (function (lambda ()
      (setq surfs (vlax-get doc (quote Surfaces)))
      (setq s (vlax-invoke surfs (quote AddTINSurface) nm))
    ))
  )
  s
)

;;; Cria superficie de volume TIN
(defun mk:volsurf (doc nm baseNm compNm / surfs vs)
  (setq vs nil)
  (vl-catch-all-apply
    (function (lambda ()
      (setq surfs (vlax-get doc (quote Surfaces)))
      (setq vs (vlax-invoke surfs (quote AddTINVolumeSurface) nm baseNm compNm))
    ))
  )
  vs
)

;;; === FASE 1: Cria pontos nos vertices dos poligonos ===
(defun C:PILHAS (/ doc surf i sel ent pts v x y z lyr n desc)
  (vl-load-com)
  (setq doc (c3d:doc))
  (if (null doc) (progn (alert "Civil 3D nao conectado!") (exit)))
  (setq surf (c3d:surf doc "superficie 13 04 2026"))
  (if (null surf)
    (progn
      (setq surf (c3d:surf doc "superfície 13 04 2026"))
    )
  )
  (if (null surf)
    (progn (alert "Superficie nao encontrada! Verifique o nome.") (exit))
  )
  (prompt "\n=== PILHAS: CRIACAO DE PONTOS DE BORDA ===\n")
  (setq i 1)
  (repeat 4
    (setq sel (entsel (strcat "\nSelecione polilinha PILHA " (itoa i) ": ")))
    (if sel
      (progn
        (setq ent (car sel)
              pts  (poly:verts ent)
              lyr  (strcat "PILHA_" (itoa i))
              desc (strcat "PILHA_" (itoa i))
              n    0)
        (mk:layer lyr (+ i 1))
        (foreach v pts
          (setq x (car v) y (cadr v))
          (setq z (surf:z surf x y))
          (if (null z) (setq z 0.0))
          (add:cogo doc x y z desc)
          (mk:pt x y z lyr)
          (setq n (1+ n))
        )
        (mk:ptgroup doc lyr (strcat "PILHA_" (itoa i)))
        (prompt (strcat "\n  PILHA " (itoa i) ": " (itoa n) " pontos | Layer: " lyr))
      )
      (prompt (strcat "\n  PILHA " (itoa i) " ignorada."))
    )
    (setq i (1+ i))
  )
  (vl-catch-all-apply
    (function (lambda ()
      (vlax-invoke (vlax-get doc (quote PointGroups)) (quote Update))
    ))
  )
  (command "REGEN")
  (prompt "\nFase 1 OK! Execute C:CRIA_SURF para criar as superficies.")
  (princ)
)

;;; === FASE 2: Cria superficies TIN e de volume ===
(defun C:CRIA_SURF (/ doc primNm i sfNm sf pgNm vsNm vs)
  (vl-load-com)
  (setq doc (c3d:doc))
  (if (null doc) (progn (alert "Civil 3D nao conectado!") (exit)))
  (setq primNm "superficie 13 04 2026")
  (if (null (c3d:surf doc primNm))
    (setq primNm "superfície 13 04 2026")
  )
  (prompt "\n=== CRIANDO SUPERFICIES TIN E VOLUMES ===\n")
  (setq i 1)
  (repeat 4
    (setq pgNm (strcat "PILHA_" (itoa i))
          sfNm (strcat "PILHA_" (itoa i) "_SURF")
          vsNm (strcat "VOL_PILHA_" (itoa i)))
    (setq sf (mk:surface doc sfNm pgNm))
    (if sf
      (progn
        (vl-catch-all-apply
          (function (lambda ()
            (vlax-invoke
              (vlax-get (vlax-get sf (quote Definition)) (quote PointGroups))
              (quote Add) pgNm)
          ))
        )
        (vl-catch-all-apply
          (function (lambda () (vlax-invoke sf (quote Rebuild))))
        )
        (prompt (strcat "\n  Superficie '" sfNm "' criada."))
        (setq vs (mk:volsurf doc vsNm primNm sfNm))
        (if vs
          (progn
            (prompt (strcat "\n  Volume '" vsNm "' criado."))
            (vl-catch-all-apply
              (function (lambda ()
                (prompt (strcat "\n    Corte:  "
                  (rtos (vlax-get vs (quote CutVolume)) 2 2) " m3"))
                (prompt (strcat "\n    Aterro: "
                  (rtos (vlax-get vs (quote FillVolume)) 2 2) " m3"))
                (prompt (strcat "\n    Liquido:"
                  (rtos (vlax-get vs (quote NetVolume)) 2 2) " m3"))
              ))
            )
          )
          (prompt (strcat "\n  Volume '" vsNm "': crie manualmente no Civil 3D."))
        )
      )
      (prompt (strcat "\n  Superficie '" sfNm "': nao criada - verifique grupo " pgNm "."))
    )
    (setq i (1+ i))
  )
  (prompt "\n\nConcluido! Verifique o Prospector.")
  (princ)
)

(prompt "\nPILHAS.LSP carregado!")
(prompt "\n  C:PILHAS    -> Seleciona polilinhas, cria pontos com cota do primitivo")
(prompt "\n  C:CRIA_SURF -> Cria superficies TIN e volumes automaticamente")
(princ)
