(defun c:AutoGrupos2026 (/ acadObj aeccApp aeccDoc aeccGroups points count i pt rawDesc groupName group)
  (vl-load-com)
  
  (setq acadObj (vlax-get-acad-object))
  ;; Conecta à API específica do Civil 3D 2026 (versão 13.8)
  (setq aeccApp (vl-catch-all-apply 'vla-getinterfaceobject 
                (list acadObj "AeccXUiLand.AeccApplication.13.8")))

  (if (vl-catch-all-error-p aeccApp)
    (princ "\nErro: API do Civil 3D 2026 não encontrada.")
    (progn
      (setq aeccDoc (vlax-get-property aeccApp "ActiveDocument"))
      (setq aeccGroups (vlax-get-property aeccDoc "PointGroups"))
      (setq points (vlax-get-property aeccDoc "Points"))
      (setq count (vlax-get-property points "Count"))
      (setq i 0)

      (princ (strcat "\nLendo nomenclaturas de " (itoa count) " pontos..."))

      (repeat count
        (setq pt (vlax-invoke-method points "Item" i))
        (setq rawDesc (vlax-get-property pt "RawDescription"))
        
        (if (and rawDesc (/= rawDesc ""))
          (progn
            ;; Define a nomenclatura base (ex: "imprimacao ps" vira "IMPRIMACAO")
            (setq groupName (strcase (car (split-string-nomenclatura rawDesc " "))))

            ;; Verifica se o grupo já existe. Se não existir, ele cria.
            (if (vl-catch-all-error-p (setq group (vl-catch-all-apply 'vla-Item (list aeccGroups groupName))))
              (progn
                (setq group (vla-add aeccGroups groupName))
                
                ;; --- ESTA É A PARTE QUE VOCÊ SOLICITOU ---
                ;; Define o critério de INCLUSÃO por nomenclatura (Raw Description)
                ;; O asterisco (*) garante que "passeio" e "passeio fim" entrem no mesmo grupo
                (vlax-put-property group "RawDescriptionMatching" (strcat groupName "*"))
                
                (princ (strcat "\nGrupo criado e configurado: " groupName))
              )
            )
          )
        )
        (setq i (1+ i))
      )
      
      ;; Atualiza todos os grupos para que os pontos sejam incluídos imediatamente
      (vlax-for pg aeccGroups (vl-catch-all-apply 'vla-Update (list pg)))
      
      (vla-regen (vla-get-activedocument acadObj) acAllViewports)
      (princ "\n--- Todos os grupos foram incluídos e atualizados conforme a nomenclatura! ---")
    )
  )
  (princ)
)

;; Função para separar a nomenclatura principal (trata espaços)
(defun split-string-nomenclatura (str delim / pos)
  (if (setq pos (vl-string-search delim str))
    (cons (substr str 1 pos) (split-string-nomenclatura (substr str (+ pos 2)) delim))
    (list str)
  )
)