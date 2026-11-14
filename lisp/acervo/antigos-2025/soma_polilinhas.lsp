;; ========================================================================
;; SOMA_POLILINHAS.LSP
;; Rotina para somar comprimentos de múltiplas polilinhas no AutoCAD Civil
;; 
;; Desenvolvido por: Manus AI
;; Data: 28/07/2025
;; Versão: 1.0
;; 
;; Funcionalidades:
;; - Seleciona múltiplas polilinhas
;; - Calcula comprimento individual de cada polilinha
;; - Soma o comprimento total
;; - Exibe resultados na linha de comando
;; - Opção de criar texto com o resultado no desenho
;; ========================================================================

(defun C:SOMAPOLI (/ ss i ent obj comp-individual comp-total lista-comp
                     pt-texto resposta altura-texto)
  
  ;; Inicialização de variáveis
  (setq comp-total 0.0
        lista-comp '()
        i 0)
  
  ;; Cabeçalho da rotina
  (princ "\n========================================")
  (princ "\n    SOMA COMPRIMENTOS DE POLILINHAS    ")
  (princ "\n========================================")
  
  ;; Seleção das polilinhas
  (princ "\nSelecione as polilinhas para somar:")
  (setq ss (ssget '((0 . "LWPOLYLINE,POLYLINE"))))
  
  (if ss
    (progn
      ;; Processa cada polilinha selecionada
      (princ (strcat "\n\nProcessando " (itoa (sslength ss)) " polilinha(s)...\n"))
      (princ "\n--- COMPRIMENTOS INDIVIDUAIS ---")
      
      (while (< i (sslength ss))
        (setq ent (ssname ss i))
        (setq obj (vlax-ename->vla-object ent))
        
        ;; Obtém o comprimento da polilinha
        (setq comp-individual (vlax-get-property obj 'Length))
        
        ;; Adiciona à lista e ao total
        (setq lista-comp (append lista-comp (list comp-individual)))
        (setq comp-total (+ comp-total comp-individual))
        
        ;; Exibe comprimento individual
        (princ (strcat "\nPolilinha " (itoa (+ i 1)) ": " 
                      (rtos comp-individual 2 3) " m"))
        
        (setq i (+ i 1))
      )
      
      ;; Exibe resultado final
      (princ "\n\n--- RESULTADO FINAL ---")
      (princ (strcat "\nNúmero de polilinhas: " (itoa (sslength ss))))
      (princ (strcat "\nComprimento total: " (rtos comp-total 2 3) " m"))
      (princ (strcat "\nComprimento total: " (rtos (/ comp-total 1000) 2 3) " km"))
      
      ;; Pergunta se deseja inserir texto no desenho
      (princ "\n\nDeseja inserir o resultado como texto no desenho? [S/N]: ")
      (setq resposta (getstring))
      
      (if (or (= (strcase resposta) "S") (= (strcase resposta) "SIM"))
        (progn
          ;; Solicita ponto de inserção
          (setq pt-texto (getpoint "\nClique no ponto para inserir o texto: "))
          
          (if pt-texto
            (progn
              ;; Solicita altura do texto
              (setq altura-texto (getreal "\nAltura do texto <2.5>: "))
              (if (not altura-texto) (setq altura-texto 2.5))
              
              ;; Cria o texto com o resultado
              (criar-texto-resultado pt-texto altura-texto comp-total (sslength ss))
            )
          )
        )
      )
      
      (princ "\n========================================")
      (princ "\nRotina concluída com sucesso!")
    )
    (princ "\nNenhuma polilinha foi selecionada.")
  )
  
  (princ)
)

;; ========================================================================
;; Função auxiliar para criar texto com o resultado
;; ========================================================================
(defun criar-texto-resultado (ponto altura total-comp num-poli / texto-conteudo)
  
  (setq texto-conteudo 
    (strcat 
      "SOMA DE POLILINHAS\n"
      "Quantidade: " (itoa num-poli) " un\n"
      "Comprimento: " (rtos total-comp 2 3) " m\n"
      "Comprimento: " (rtos (/ total-comp 1000) 2 3) " km"
    )
  )
  
  (command "_.MTEXT" 
           ponto 
           "J" "TL"  ; Justificação Top-Left
           "H" altura
           "W" 0     ; Largura automática
           texto-conteudo
           ""
  )
  
  (princ "\nTexto inserido no desenho.")
)

;; ========================================================================
;; Comando alternativo mais simples (apenas soma total)
;; ========================================================================
(defun C:SPOLI (/ ss i ent obj comp-total)
  
  (setq comp-total 0.0
        i 0)
  
  (princ "\nSELECIONE AS POLILINHAS PARA SOMAR:")
  (setq ss (ssget '((0 . "LWPOLYLINE,POLYLINE"))))
  
  (if ss
    (progn
      (while (< i (sslength ss))
        (setq ent (ssname ss i))
        (setq obj (vlax-ename->vla-object ent))
        (setq comp-total (+ comp-total (vlax-get-property obj 'Length)))
        (setq i (+ i 1))
      )
      
      (princ (strcat "\n>>> TOTAL: " (rtos comp-total 2 3) " m (" 
                    (rtos (/ comp-total 1000) 2 3) " km) <<<"))
    )
    (princ "\nNenhuma polilinha selecionada.")
  )
  
  (princ)
)

;; ========================================================================
;; Comando para somar por layer específico
;; ========================================================================
(defun C:SOMALAYER (/ layer-nome ss i ent obj comp-total)
  
  (setq comp-total 0.0
        i 0)
  
  ;; Solicita nome do layer
  (setq layer-nome (getstring T "\nDigite o nome do layer: "))
  
  (if (and layer-nome (/= layer-nome ""))
    (progn
      ;; Seleciona todas as polilinhas do layer especificado
      (setq ss (ssget "X" (list '(0 . "LWPOLYLINE,POLYLINE") 
                                (cons 8 layer-nome))))
      
      (if ss
        (progn
          (princ (strcat "\nEncontradas " (itoa (sslength ss)) 
                        " polilinha(s) no layer '" layer-nome "'"))
          
          (while (< i (sslength ss))
            (setq ent (ssname ss i))
            (setq obj (vlax-ename->vla-object ent))
            (setq comp-total (+ comp-total (vlax-get-property obj 'Length)))
            (setq i (+ i 1))
          )
          
          (princ (strcat "\n>>> TOTAL LAYER '" (strcase layer-nome) "': " 
                        (rtos comp-total 2 3) " m (" 
                        (rtos (/ comp-total 1000) 2 3) " km) <<<"))
        )
        (princ (strcat "\nNenhuma polilinha encontrada no layer '" layer-nome "'"))
      )
    )
    (princ "\nNome do layer não informado.")
  )
  
  (princ)
)

;; ========================================================================
;; Comando para criar relatório detalhado
;; ========================================================================
(defun C:RELATORIOPOLI (/ ss i ent obj comp-individual comp-total lista-dados
                          arquivo-nome arquivo layer-nome)
  
  (setq comp-total 0.0
        lista-dados '()
        i 0)
  
  (princ "\n=== RELATÓRIO DETALHADO DE POLILINHAS ===")
  (princ "\nSelecione as polilinhas para o relatório:")
  (setq ss (ssget '((0 . "LWPOLYLINE,POLYLINE"))))
  
  (if ss
    (progn
      ;; Coleta dados de cada polilinha
      (while (< i (sslength ss))
        (setq ent (ssname ss i))
        (setq obj (vlax-ename->vla-object ent))
        (setq comp-individual (vlax-get-property obj 'Length))
        (setq layer-nome (vlax-get-property obj 'Layer))
        
        ;; Adiciona dados à lista
        (setq lista-dados 
          (append lista-dados 
            (list (list (+ i 1) layer-nome comp-individual))))
        
        (setq comp-total (+ comp-total comp-individual))
        (setq i (+ i 1))
      )
      
      ;; Exibe relatório na tela
      (princ "\n\n--- RELATÓRIO DETALHADO ---")
      (princ "\nItem | Layer | Comprimento (m)")
      (princ "\n-----|-------|---------------")
      
      (foreach item lista-dados
        (princ (strcat "\n" 
                      (rtos (car item) 2 0) " | "
                      (cadr item) " | "
                      (rtos (caddr item) 2 3)))
      )
      
      (princ (strcat "\n\nTOTAL: " (rtos comp-total 2 3) " m"))
      
      ;; Opção de salvar em arquivo
      (princ "\n\nDeseja salvar relatório em arquivo TXT? [S/N]: ")
      (if (= (strcase (getstring)) "S")
        (progn
          (setq arquivo-nome (getfiled "Salvar relatório como:" "" "txt" 1))
          (if arquivo-nome
            (salvar-relatorio-arquivo arquivo-nome lista-dados comp-total)
          )
        )
      )
    )
    (princ "\nNenhuma polilinha selecionada.")
  )
  
  (princ)
)

;; ========================================================================
;; Função auxiliar para salvar relatório em arquivo
;; ========================================================================
(defun salvar-relatorio-arquivo (nome-arquivo dados total / arquivo)
  
  (setq arquivo (open nome-arquivo "w"))
  
  (if arquivo
    (progn
      (write-line "========================================" arquivo)
      (write-line "    RELATÓRIO DE POLILINHAS" arquivo)
      (write-line "========================================" arquivo)
      (write-line "" arquivo)
      (write-line (strcat "Data: " (menucmd "M=$(edtime,$(getvar,date),DD/MM/YYYY)")) arquivo)
      (write-line (strcat "Desenho: " (getvar "DWGNAME")) arquivo)
      (write-line "" arquivo)
      (write-line "Item | Layer | Comprimento (m)" arquivo)
      (write-line "-----|-------|---------------" arquivo)
      
      (foreach item dados
        (write-line (strcat (rtos (car item) 2 0) " | "
                           (cadr item) " | "
                           (rtos (caddr item) 2 3)) arquivo)
      )
      
      (write-line "" arquivo)
      (write-line (strcat "TOTAL: " (rtos total 2 3) " m") arquivo)
      (write-line (strcat "TOTAL: " (rtos (/ total 1000) 2 3) " km") arquivo)
      (write-line "========================================" arquivo)
      
      (close arquivo)
      (princ (strcat "\nRelatório salvo em: " nome-arquivo))
    )
    (princ "\nErro ao criar arquivo.")
  )
)

;; ========================================================================
;; Mensagens de ajuda
;; ========================================================================
(defun C:AJUDAPOLI ()
  (princ "\n========================================")
  (princ "\n    COMANDOS DISPONÍVEIS")
  (princ "\n========================================")
  (princ "\nSOMAP0LI    - Soma polilinhas com detalhes")
  (princ "\nSPOLI       - Soma rápida de polilinhas")
  (princ "\nSOMALAYER   - Soma polilinhas por layer")
  (princ "\nRELATORIOPOLI - Relatório detalhado")
  (princ "\nAJUDAPOLI   - Esta ajuda")
  (princ "\n========================================")
  (princ)
)

;; ========================================================================
;; Mensagem de carregamento
;; ========================================================================
(princ "\n========================================")
(princ "\n  ROTINAS DE SOMA DE POLILINHAS")
(princ "\n========================================")
(princ "\nComandos carregados:")
(princ "\n- SOMAPOLI    (soma detalhada)")
(princ "\n- SPOLI       (soma rápida)")
(princ "\n- SOMALAYER   (soma por layer)")
(princ "\n- RELATORIOPOLI (relatório)")
(princ "\n- AJUDAPOLI   (ajuda)")
(princ "\n========================================")
(princ "\nDigite AJUDAPOLI para mais informações.")
(princ)

