(defun c:PDFSECOES ( / ss i ent obj minPt maxPt pt1 pt2 baseDir baseName fileName extPt1 extPt2 oldCmdecho oldFiledia count)
  (vl-load-com)
  
  ;; Salva as configurações atuais do usuário
  (setq oldCmdecho (getvar "CMDECHO"))
  (setq oldFiledia (getvar "FILEDIA"))
  (setvar "CMDECHO" 0)
  (setvar "FILEDIA" 0) 
  
  ;; Solicita a pasta e o prefixo do nome dos arquivos
  (setq baseDir (getstring "\nDigite o caminho da pasta para salvar os PDFs (ex: C:\\PDFs_SECOES): "))
  (setq baseName (getstring "\nDigite o prefixo do nome dos arquivos (ex: Secao_): "))
  
  (if (= baseDir "") (setq baseDir "C:\\"))
  (if (= baseName "") (setq baseName "Secao_"))
  
  ;; Solicita a seleção de objetos (qualquer objeto)
  (princ "\nSelecione as Seções Transversais (pode usar seleção por janela): ")
  (if (setq ss (ssget))
    (progn
      (setq i 0)
      (setq count 0) ;; Contador apenas para os PDFs gerados com sucesso
      
      (princ (strcat "\nTotal de objetos selecionados: " (itoa (sslength ss))))
      
      ;; Loop para percorrer todos os objetos selecionados
      (while (< i (sslength ss))
        (setq ent (ssname ss i))
        (setq obj (vlax-ename->vla-object ent))
        
        ;; Tenta pegar os limites (Bounding Box) do objeto com tratamento de erro
        (if (not (vl-catch-all-error-p (vl-catch-all-apply 'vla-getboundingbox (list obj 'minPt 'maxPt))))
          (progn
            (setq pt1 (vlax-safearray->list minPt))
            (setq pt2 (vlax-safearray->list maxPt))
            
            ;; Adiciona uma margem de 2 metros (ou unidades do desenho) ao redor
            (setq extPt1 (list (- (car pt1) 2.0) (- (cadr pt1) 2.0) 0.0))
            (setq extPt2 (list (+ (car pt2) 2.0) (+ (cadr pt2) 2.0) 0.0))
            
            ;; Define o nome do arquivo PDF
            (setq fileName (strcat baseDir "\\" baseName (itoa (1+ count)) ".pdf"))
            
            ;; Comando de Plotagem (-PLOT)
            (command "._-PLOT"
                     "_Y"                 ; Detalhado? Sim
                     ""                   ; Layout atual (Model)
                     "DWG To PDF.pc3"     ; Plotadora
                     "ISO A4 (297.00 x 210.00 MM)" ; Tamanho do papel A4 (Pode precisar de ajuste)
                     "_M"                 ; Milimetros
                     "_L"                 ; Paisagem (Landscape)
                     "_N"                 ; De cabeça para baixo? Não
                     "_W"                 ; Janela (Window)
                     extPt1 extPt2        ; Coordenadas da janela
                     "_F"                 ; Ajustar à folha (Fit)
                     "_C"                 ; Centralizar
                     "_Y"                 ; Usar estilos de plotagem? Sim
                     "monochrome.ctb"     ; Tabela de canetas (Preto e Branco)
                     "_Y"                 ; Espessuras de linha? Sim
                     "_N"                 ; Escalar espessuras? Não
                     "_N"                 ; Plotar em segundo plano? Não
                     fileName             ; Caminho e nome do arquivo
                     "_Y"                 ; Salvar alterações no layout? Sim
                     "_Y"                 ; Continuar a plotagem? Sim
            )
            
            (princ (strcat "\nGerado PDF: " fileName))
            (setq count (1+ count))
          )
          ;; Se não conseguir pegar o Bounding Box, apenas ignora e segue para o próximo
        )
        (setq i (1+ i))
      )
      (if (= count 0)
        (princ "\nNenhum objeto gráfico válido foi encontrado na seleção.")
      )
    )
    (princ "\nNenhum objeto selecionado.")
  )
  
  ;; Restaura as configurações do usuário
  (setvar "CMDECHO" oldCmdecho)
  (setvar "FILEDIA" oldFiledia)
  
  (princ "\nProcesso concluído! Verifique a pasta de destino.")
  (princ)
)
(princ "\nDigite PDFSECOES para rodar a rotina.")
(princ)