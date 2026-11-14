;; CriarPolilinhaAoRedor.lsp
;; Rotina AutoLISP para criar uma polilinha ao redor de um objeto selecionado
;;
;; Como usar:
;; 1. Carregue o arquivo com APPLOAD
;; 2. Digite CPAR no comando
;; 3. Clique no corredor/objeto desejado
;; 4. Informe a distância de offset (margem ao redor)
;;

(defun c:CPAR ()
  (setq oldOS (getvar "OSMODE"))
  (setvar "OSMODE" 0)
  
  (princ "\n=== CRIAR POLILINHA AO REDOR DO CORREDOR ===")
  
  ;; Selecionar o objeto
  (setq ent (car (entsel "\nSelecione o corredor (ou objeto): ")))
  
  (if ent
    (progn
      ;; Obter os pontos mínimo e máximo do objeto (bounding box)
      (setq ptList (acet-ent-geomextents ent))
      
      (if ptList
        (progn
          (setq ptMin (car ptList))
          (setq ptMax (cadr ptList))
          
          ;; Solicitar distância de offset
          (setq offsetDist (getdist ptMin "\nInforme a distância de margem ao redor: "))
          
          (if (not offsetDist)
            (setq offsetDist 0.0)
          )
          
          ;; Calcular os 4 cantos do retângulo com offset
          (setq xMin (- (car ptMin) offsetDist))
          (setq yMin (- (cadr ptMin) offsetDist))
          (setq xMax (+ (car ptMax) offsetDist))
          (setq yMax (+ (cadr ptMax) offsetDist))
          
          ;; Criar a lista de pontos da polilinha retangular
          (setq pt1 (list xMin yMin))
          (setq pt2 (list xMax yMin))
          (setq pt3 (list xMax yMax))
          (setq pt4 (list xMin yMax))
          
          ;; Criar a polilinha (fechada)
          (command "_.PLINE" pt1 pt2 pt3 pt4 "C")
          
          (princ (strcat "\nPolilinha criada ao redor do objeto com margem de " 
                         (rtos offsetDist 2 3) " unidades."))
        )
        (princ "\nErro: Não foi possível obter os limites geométricos do objeto.")
      )
    )
    (princ "\nNenhum objeto selecionado.")
  )
  
  (setvar "OSMODE" oldOS)
  (princ)
)

;; =====================================================================
;; VERSÃO ALTERNATIVA: Usando OFFSET em uma polilinha existente
;; Se o corredor já for uma polilinha, esta versão faz offset dela mesma
;; =====================================================================

(defun c:CPAR2 ()
  (setq oldOS (getvar "OSMODE"))
  (setvar "OSMODE" 0)
  
  (princ "\n=== CRIAR POLILINHA POR OFFSET ===")
  
  ;; Selecionar o objeto
  (setq ent (entsel "\nSelecione a polilinha do corredor: "))
  
  (if ent
    (progn
      (setq entObj (vlax-ename->vla-object (car ent)))
      (setq entType (vla-get-ObjectName entObj))
      
      ;; Verificar se é polilinha
      (if (or (= entType "AcDbPolyline")
              (= entType "AcDb2dPolyline")
              (= entType "AcDb3dPolyline"))
        (progn
          ;; Solicitar distância de offset
          (setq offsetDist (getdist "\nInforme a distância de offset: "))
          
          (if offsetDist
            (progn
              ;; Fazer offset da polilinha
              (setq offsetResult (vla-Offset entObj offsetDist))
              
              (if offsetResult
                (progn
                  (setq newEnt (vlax-vla-object->ename (vlax-safearray->list (vlax-variant-value offsetResult))))
                  (princ "\nPolilinha criada com sucesso!")
                )
                (princ "\nErro: Não foi possível criar o offset.")
              )
            )
            (princ "\nOffset cancelado.")
          )
        )
        (princ "\nO objeto selecionado não é uma polilinha. Use CPAR para objetos genéricos.")
      )
    )
    (princ "\nNenhum objeto selecionado.")
  )
  
  (setvar "OSMODE" oldOS)
  (princ)
)

;; =====================================================================
;; VERSÃO 3: Para CORREDORES do CIVIL 3D (Corridor)
;; Extrai a polilinha dos limites do corredor
;; =====================================================================

(defun c:CPAR3D ()
  (vl-load-com)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq modelSpace (vla-get-ModelSpace doc))
  
  (princ "\n=== POLILINHA AO REDOR DE CORREDOR CIVIL 3D ===")
  
  ;; Selecionar o corredor
  (setq ent (entsel "\nSelecione o Corredor (Civil 3D): "))
  
  (if ent
    (progn
      (setq entObj (vlax-ename->vla-object (car ent)))
      
      ;; Tentar obter os limites do corredor
      (setq bounds (acet-ent-geomextents (car ent)))
      
      (if bounds
        (progn
          (setq ptMin (car bounds))
          (setq ptMax (cadr bounds))
          
          ;; Solicitar margem
          (setq margin (getdist ptMin "\nMargem ao redor do corredor: "))
          (if (not margin) (setq margin 0.0))
          
          ;; Criar polilinha retangular
          (setq coords (vlax-make-safearray vlax-vbDouble '(0 . 7)))
          (vlax-safearray-fill coords (list
            (- (car ptMin) margin) (- (cadr ptMin) margin)
            (+ (car ptMax) margin) (- (cadr ptMin) margin)
            (+ (car ptMax) margin) (+ (cadr ptMax) margin)
            (- (car ptMin) margin) (+ (cadr ptMax) margin)
          ))
          
          (setq pline (vla-AddLightweightPolyline modelSpace coords))
          (vla-put-Closed pline :vlax-true)
          
          (princ "\nPolilinha criada ao redor do corredor Civil 3D!")
        )
        (princ "\nErro ao obter limites do corredor.")
      )
    )
    (princ "\nNenhum corredor selecionado.")
  )
  
  (princ)
)

;; Mensagem de carregamento
(princ "\nRotinas carregadas: CPAR | CPAR2 | CPAR3D")
(princ "\n- CPAR   : Polilinha retangular ao redor de qualquer objeto")
(princ "\n- CPAR2  : Offset de polilinha existente")
(princ "\n- CPAR3D : Polilinha ao redor de Corredor Civil 3D")
(princ "\n")
(princ)
