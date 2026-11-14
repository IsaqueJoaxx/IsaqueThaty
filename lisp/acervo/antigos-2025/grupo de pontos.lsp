(defun c:CriarGruposAutomatico (/ *error* acadObj aeccApp aeccDoc aeccGroups aeccPoints 
                                  ptCount i pt rawDesc firstWord groupName newGroup descList)

  (vl-load-com)

  ;; Tratamento de erro padrão para não travar o CAD
  (defun *error* (msg)
    (if (and msg (not (wcmatch (strcase msg) "*CANCEL*,*QUIT*,*BREAK*")))
      (princ (strcat "\nErro encontrado: " msg))
    )
    (princ)
  )

  (princ "\nIniciando automação de grupos...")

  (setq acadObj (vlax-get-acad-object))
  
  ;; --- CONEXÃO COM CIVIL 3D 2026 ---
  ;; Tenta conectar na versão 13.8 (2026). Se falhar, tenta anteriores.
  (setq aeccApp (vl-catch-all-apply 'vla-getinterfaceobject (list acadObj "AeccXUiLand.AeccApplication.13.8")))
  
  (if (vl-catch-all-error-p aeccApp)
    (setq aeccApp (vl-catch-all-apply 'vla-getinterfaceobject (list acadObj "AeccXUiLand.AeccApplication.13.7"))) ;; Fallback 2025
  )

  (if (vl-catch-all-error-p aeccApp)
    (alert "Erro Crítico: Não foi possível acessar a API do Civil 3D.\nVerifique se a versão é compativel.")
    
    ;; --- BLOCO PRINCIPAL ---
    (progn
      (setq aeccDoc (vlax-get-property aeccApp "ActiveDocument"))
      (setq aeccGroups (vlax-get-property aeccDoc "PointGroups"))
      (setq aeccPoints (vlax-get-property aeccDoc "Points"))
      
      (setq ptCount (vlax-get-property aeccPoints "Count"))
      (setq descList '()) ;; Lista para armazenar nomes únicos
      
      (princ (strcat "\nLendo " (itoa ptCount) " pontos. Aguarde..."))

      ;; 1. VARREDURA: Coleta apenas as descrições únicas (Primeira palavra)
      (setq i 0)
      (repeat ptCount
        (setq pt (vlax-invoke-method aeccPoints "Item" i))
        (setq rawDesc (vlax-get-property pt "RawDescription"))
        
        (if (and rawDesc (/= rawDesc ""))
          (progn
            ;; Pega a primeira palavra e joga para MAIÚSCULO
            (setq firstWord (strcase (car (CheckSplitString rawDesc " "))))
            
            ;; Se ainda não está na lista, adiciona
            (if (not (member firstWord descList))
              (setq descList (cons firstWord descList))
            )
          )
        )
        (setq i (1+ i))
      )

      (princ (strcat "\nCriando " (itoa (length descList)) " grupos de pontos..."))

      ;; 2. CRIAÇÃO E VINCULAÇÃO
      (foreach nome descList
        ;; Verifica se o grupo já existe para não dar erro
        (if (vl-catch-all-error-p (setq newGroup (vl-catch-all-apply 'vla-Item (list aeccGroups nome))))
          (progn
            ;; Se não existe, cria
            (setq newGroup (vla-add aeccGroups nome))
            (princ (strcat "\n+ Grupo criado: " nome))
          )
          (princ (strcat "\n. Grupo atualizado: " nome))
        )

        ;; --- O SEGREDO DO VÍNCULO ESTÁ AQUI ---
        ;; 1. Define a regra na aba "Include" (Raw Description Matching)
        ;;    O asterisco (*) garante que "imprimacao ps" entre em "IMPRIMACAO"
        (vlax-put-property newGroup "RawDescriptionMatching" (strcat nome "*"))
        
        ;; 2. Força o Civil 3D a aplicar a regra IMEDIATAMENTE
        (vlax-invoke-method newGroup "Update")
      )

      ;; Finalização
      (vla-regen (vla-get-activedocument acadObj) acAllViewports)
      (alert "Processo concluído!\n\nVerifique no Toolspace se os grupos estão cheios.")
    )
  )
  (princ)
)

;; Função auxiliar para separar texto (segura contra caracteres estranhos)
(defun CheckSplitString (str delim / pos)
  (if (setq pos (vl-string-search delim str))
    (cons (substr str 1 pos) (CheckSplitString (substr str (+ pos 2)) delim))
    (list str)
  )
)

(princ "\nComando carregado: Digite 'CriarGruposAutomatico' para rodar.")
(princ)