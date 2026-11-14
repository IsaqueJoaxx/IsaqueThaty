;;;===============================================================
;;; CRIAR GRUPOS DE PONTOS POR DESCRIÇÃO — AutoCAD Civil 3D
;;; Comando: C:GRPDESC
;;;
;;; Esta rotina:
;;; 1. Coleta todos os pontos COGO do desenho
;;; 2. Identifica todas as descrições únicas
;;; 3. Cria um grupo de pontos para cada descrição
;;; 4. Configura o filtro de descrição bruta no grupo
;;;
;;; Autor: Adaptado para Civil 3D
;;;===============================================================

(vl-load-com)

;;;---------------------------------------------------------------
;;; Função: OP:C3DDOC
;;; Retorna o objeto do documento ativo do Civil 3D
;;;---------------------------------------------------------------
(defun OP:c3ddoc (/ prod verstr c3dver)
  (defun c3dver (/ c3d *acad*)
    (setq C3D	(strcat	"HKEY_LOCAL_MACHINE\\"
			(if vlax-user-product-key
			  (vlax-user-product-key)
			  (vlax-product-key)
			)
		)
	  C3D	(vl-registry-read C3D "Release")
	  c3d	(substr
		  C3D
		  1
		  (vl-string-search
		    "."
		    C3D
		    (+ (vl-string-search "." C3D) 1)
		  )
		)
    )
    c3d
  )
  (if	(not _C3DDoc)
    (setq
      _C3DDoc	(vla-get-activedocument
		  (vla-getinterfaceobject
		    (vlax-get-acad-object)
		    (strcat "AeccXUiLand.AeccApplication." (c3dver))
		  )
		)
    )
  )
  _C3DDoc
)

;;;---------------------------------------------------------------
;;; Função: AddOrGetItem
;;; Adiciona ou obtém item de uma coleção
;;;---------------------------------------------------------------
(defun AddOrGetItem (objCollection strName / objFromCollection)
  (or (= (type (setq objFromCollection
              (vl-catch-all-apply
            'vla-add
            (list objCollection strName)
              )
           )
     )
     'VLA-OBJECT
      )
      (= (type (setq objFromCollection
              (vl-catch-all-apply
            'vla-item
            (list objCollection strName)
              )
           )
     )
     'VLA-OBJECT
      )
  )
  (if (= (type objFromCollection) 'VL-CATCH-ALL-APPLY-ERROR)
    (setq objFromCollection nil)
    objFromCollection
  )
)

;;;---------------------------------------------------------------
;;; Função: AddPointGroup
;;; Cria ou obtém um grupo de pontos pelo nome
;;;---------------------------------------------------------------
(defun AddPointGroup (strName / objGroup objGroups)
  (if (and (setq objGroups (vlax-get-property (OP:c3ddoc) 'PointGroups))
	   (setq objGroup (AddOrGetItem objGroups strName))
      )
    objGroup
  )
)

;;;---------------------------------------------------------------
;;; Função: GetPointGroup
;;; Obtém um grupo de pontos existente pelo nome
;;;---------------------------------------------------------------
(defun GetPointGroup (strName / objGroup objGroups)
  (if (and (setq objGroups (vlax-get-property (OP:c3ddoc) 'PointGroups))
	   (= 'VLA-OBJECT (type (setq objGroup (vl-catch-all-apply 'vla-item (list objGroups strName)))))
      )
    objGroup
  )
)

;;;---------------------------------------------------------------
;;; Função: GetQueryBuilder
;;; Obtém o QueryBuilder do grupo de pontos
;;;---------------------------------------------------------------
(defun GetQueryBuilder (objGroup)
  (if (and (= (type objGroup) 'VLA-OBJECT)
           (= (vla-get-ObjectName objGroup) "AeccDbPG")
      )
    (vlax-get-property objGroup 'QueryBuilder)
  )
)

;;;---------------------------------------------------------------
;;; Função: SetPointGroupQueryByDescription
;;; Configura o filtro de descrição bruta no grupo de pontos
;;;---------------------------------------------------------------
(defun SetPointGroupQueryByDescription (objGroup strDescription / objQB)
  (if (and objGroup strDescription)
    (progn
      (setq objQB (GetQueryBuilder objGroup))
      (if objQB
        (progn
          ;; Define o filtro de descrição bruta
          (vlax-put-property objQB 'IncludeRawDescriptions strDescription)
          ;; Define o operador de consulta
          (vlax-put-property objQB 'QueryStatement 
            (strcat "Point.RawDescription = '" strDescription "'"))
          T
        )
      )
    )
  )
)

;;;---------------------------------------------------------------
;;; Função: GetAllCogoPoints
;;; Coleta todos os pontos COGO do desenho e suas descrições
;;; Retorna: lista de pares (numero . descricao)
;;;---------------------------------------------------------------
(defun GetAllCogoPoints (/ ss i ename obj desc num ptList)
  (setq ptList nil)
  (if (setq ss (ssget "X" '((0 . "AECC_COGO_POINT"))))
    (progn
      (setq i 0)
      (repeat (sslength ss)
        (setq ename (ssname ss i))
        (setq obj (vlax-ename->vla-object ename))
        (setq num (vlax-get obj 'Number))
        (setq desc (vlax-get obj 'Description))
        (setq ptList (cons (cons num desc) ptList))
        (setq i (1+ i))
      )
    )
  )
  ptList
)

;;;---------------------------------------------------------------
;;; Função: GetUniqueDescriptions
;;; Extrai descrições únicas de uma lista de pontos
;;; Retorna: lista de descrições únicas (strings)
;;;---------------------------------------------------------------
(defun GetUniqueDescriptions (ptList / descList desc uniqueList)
  (setq uniqueList nil)
  (foreach pt ptList
    (setq desc (cdr pt))
    (if (and desc (> (strlen desc) 0))
      (if (not (member desc uniqueList))
        (setq uniqueList (cons desc uniqueList))
      )
    )
  )
  uniqueList
)

;;;---------------------------------------------------------------
;;; Função: CleanDescription
;;; Limpa a descrição para uso como nome de grupo
;;; Remove caracteres inválidos e trunca se necessário
;;;---------------------------------------------------------------
(defun CleanDescription (strDesc / cleanStr)
  (setq cleanStr strDesc)
  ;; Remove caracteres inválidos para nome de grupo
  (setq cleanStr (vl-string-subst "_" "\\" cleanStr))
  (setq cleanStr (vl-string-subst "_" "/" cleanStr))
  (setq cleanStr (vl-string-subst "_" ":" cleanStr))
  (setq cleanStr (vl-string-subst "_" "*" cleanStr))
  (setq cleanStr (vl-string-subst "_" "?" cleanStr))
  (setq cleanStr (vl-string-subst "_" "<" cleanStr))
  (setq cleanStr (vl-string-subst "_" ">" cleanStr))
  (setq cleanStr (vl-string-subst "_" "|" cleanStr))
  (setq cleanStr (vl-string-subst "_" "\"" cleanStr))
  ;; Trunca para 64 caracteres (limite do Civil 3D)
  (if (> (strlen cleanStr) 64)
    (setq cleanStr (substr cleanStr 1 64))
  )
  cleanStr
)

;;;---------------------------------------------------------------
;;; Função: CreatePointGroupByDescription
;;; Cria um grupo de pontos para uma descrição específica
;;;---------------------------------------------------------------
(defun CreatePointGroupByDescription (strDescription / groupName objGroup objQB)
  (setq groupName (CleanDescription strDescription))
  ;; Cria ou obtém o grupo
  (setq objGroup (AddPointGroup groupName))
  (if objGroup
    (progn
      ;; Configura a descrição do grupo
      (vl-catch-all-apply 'vlax-put-property (list objGroup 'Description (strcat "Grupo: " strDescription)))
      ;; Configura o QueryBuilder para filtrar pela descrição bruta
      (setq objQB (GetQueryBuilder objGroup))
      (if objQB
        (progn
          ;; Define o filtro de descrição bruta com wildcard
          (vlax-put-property objQB 'IncludeRawDescriptions strDescription)
          ;; Atualiza o grupo
          (vl-catch-all-apply 'vlax-invoke-method (list objGroup 'Update))
          (princ (strcat "\n  Grupo criado: \"" groupName "\" -> Desc: \"" strDescription "\""))
          T
        )
        (progn
          (princ (strcat "\n  Erro: QueryBuilder não acessível para grupo \"" groupName "\""))
          nil
        )
      )
    )
    (progn
      (princ (strcat "\n  Erro: Não foi possível criar grupo \"" groupName "\""))
      nil
    )
  )
)

;;;===============================================================
;;; COMANDO PRINCIPAL: GRPDESC
;;; Cria grupos de pontos separados por descrição
;;;===============================================================
(defun c:GRPDESC (/ *error* ptList uniqueDescs desc count created failed)

  (defun *error* (msg)
    (cond ((not msg))
          ((member msg '("Function cancelled" "quit / exit abort")))
          ((princ (strcat "\n** Erro: " msg " **")))
    )
    (princ)
  )

  (princ "\n=== CRIAR GRUPOS DE PONTOS POR DESCRIÇÃO ===")
  
  ;; Passo 1: Coletar todos os pontos COGO
  (princ "\nColetando pontos COGO do desenho...")
  (setq ptList (GetAllCogoPoints))
  
  (if (null ptList)
    (progn
      (princ "\nNenhum ponto COGO encontrado no desenho.")
      (*error* nil)
      (exit)
    )
  )
  
  (princ (strcat "\nTotal de pontos encontrados: " (itoa (length ptList))))
  
  ;; Passo 2: Extrair descrições únicas
  (princ "\nIdentificando descrições únicas...")
  (setq uniqueDescs (GetUniqueDescriptions ptList))
  (setq count (length uniqueDescs))
  
  (princ (strcat "\nTotal de descrições únicas: " (itoa count)))
  
  ;; Passo 3: Perguntar confirmação
  (princ (strcat "\n\nSerão criados " (itoa count) " grupos de pontos."))
  (princ "\nDeseja continuar? [Sim/Não] <Sim>: ")
  (if (and (setq ans (getstring)) 
           (or (= (strcase ans) "N") (= (strcase ans) "NAO") (= (strcase ans) "NÃO")))
    (progn
      (princ "\nOperação cancelada pelo usuário.")
      (*error* nil)
      (exit)
    )
  )
  
  ;; Passo 4: Criar grupos para cada descrição
  (princ "\n\nCriando grupos de pontos...")
  (setq created 0)
  (setq failed 0)
  
  (foreach desc uniqueDescs
    (if (CreatePointGroupByDescription desc)
      (setq created (1+ created))
      (setq failed (1+ failed))
    )
  )
  
  ;; Resumo
  (princ (strcat "\n\n=== RESUMO ==="))
  (princ (strcat "\nGrupos criados com sucesso: " (itoa created)))
  (if (> failed 0)
    (princ (strcat "\nGrupos com falha: " (itoa failed)))
  )
  (princ "\nOperação concluída!")
  
  ;; Atualizar todos os grupos
  (princ "\nAtualizando grupos...")
  (vl-catch-all-apply
    '(lambda ()
       (vlax-for grp (vlax-get-property (OP:c3ddoc) 'PointGroups)
         (vl-catch-all-apply 'vlax-invoke-method (list grp 'Update))
       )
     )
  )
  
  (princ "\n")
  (*error* nil)
)

;;;===============================================================
;;; COMANDO ALTERNATIVO: GRPDESCSEL
;;; Cria grupos apenas para pontos selecionados
;;;===============================================================
(defun c:GRPDESCSEL (/ *error* ss ptList uniqueDescs desc count created failed)

  (defun *error* (msg)
    (cond ((not msg))
          ((member msg '("Function cancelled" "quit / exit abort")))
          ((princ (strcat "\n** Erro: " msg " **")))
    )
    (princ)
  )

  (princ "\n=== CRIAR GRUPOS DE PONTOS POR DESCRIÇÃO (SELEÇÃO) ===")
  (princ "\nSelecione os pontos COGO desejados...")
  
  ;; Selecionar pontos
  (if (null (setq ss (ssget '((0 . "AECC_COGO_POINT")))))
    (progn
      (princ "\nNenhum ponto selecionado.")
      (*error* nil)
      (exit)
    )
  )
  
  ;; Coletar pontos da seleção
  (setq ptList nil)
  (setq i 0)
  (repeat (sslength ss)
    (setq ename (ssname ss i))
    (setq obj (vlax-ename->vla-object ename))
    (setq num (vlax-get obj 'Number))
    (setq desc (vlax-get obj 'Description))
    (setq ptList (cons (cons num desc) ptList))
    (setq i (1+ i))
  )
  
  (princ (strcat "\nTotal de pontos selecionados: " (itoa (length ptList))))
  
  ;; Extrair descrições únicas
  (setq uniqueDescs (GetUniqueDescriptions ptList))
  (setq count (length uniqueDescs))
  
  (princ (strcat "\nTotal de descrições únicas: " (itoa count)))
  
  ;; Criar grupos
  (setq created 0)
  (setq failed 0)
  
  (foreach desc uniqueDescs
    (if (CreatePointGroupByDescription desc)
      (setq created (1+ created))
      (setq failed (1+ failed))
    )
  )
  
  ;; Resumo
  (princ (strcat "\n\n=== RESUMO ==="))
  (princ (strcat "\nGrupos criados com sucesso: " (itoa created)))
  (if (> failed 0)
    (princ (strcat "\nGrupos com falha: " (itoa failed)))
  )
  (princ "\nOperação concluída!")
  
  (princ "\n")
  (*error* nil)
)

;;;===============================================================
;;; COMANDO: LSTDESC
;;; Lista todas as descrições únicas dos pontos no desenho
;;;===============================================================
(defun c:LSTDESC (/ ptList uniqueDescs desc)

  (princ "\n=== LISTA DE DESCRIÇÕES ÚNICAS ===")
  
  (setq ptList (GetAllCogoPoints))
  
  (if (null ptList)
    (progn
      (princ "\nNenhum ponto COGO encontrado.")
      (exit)
    )
  )
  
  (setq uniqueDescs (GetUniqueDescriptions ptList))
  
  (princ (strcat "\nTotal de descrições: " (itoa (length uniqueDescs))))
  (princ "\n")
  
  (foreach desc (vl-sort uniqueDescs '<)
    (princ (strcat "\n  - " desc))
  )
  
  (princ "\n")
)

;;;===============================================================
;;; Carregar mensagem
;;;===============================================================
(princ "\n")
(princ "\n=== GRPDESC - Grupos de Pontos por Descrição ===")
(princ "\nComandos disponíveis:")
(princ "\n  GRPDESC    - Cria grupos para TODOS os pontos do desenho")
(princ "\n  GRPDESCSEL - Cria grupos apenas para pontos SELECIONADOS")
(princ "\n  LSTDESC    - Lista todas as descrições únicas dos pontos")
(princ "\nCarregado com sucesso!")
(princ "\n")