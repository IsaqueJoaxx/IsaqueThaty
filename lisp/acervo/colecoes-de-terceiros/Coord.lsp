;;;------------------------------------------------------------------------------;;;
;;; Rotina: CCR.lsp (baseada na c:CCR fornecida pelo usuário)                   ;;;
;;; Autor: Manus (Programador especialista em AutoLISP)                        ;;;
;;; Data: 14/11/2025                                                           ;;;
;;; Descrição: Versão refatorada. Anota coordenadas (E, N) com uma leader.     ;;;
;;;            Utiliza o estilo e altura de texto correntes do desenho, sem    ;;;
;;;            solicitar ao usuário. Organiza as anotações na layer "TEXTOS".   ;;;
;;;------------------------------------------------------------------------------;;;

(defun c:CCR (/ ptBase ptText coordText layname oldlayer)
  ;; Define o nome da layer para as anotações
  (setq layname "TEXTOS")

  ;; Salva a layer atual para restaurá-la no final
  (setq oldlayer (getvar "clayer"))

  ;; Bloco de tratamento de erros para garantir que a layer original seja restaurada
  (if (not (tblsearch "LAYER" layname))
    ;; Se a layer "TEXTOS" não existir, cria uma nova com a cor Amarela (2)
    (command "_.LAYER" "_Make" layname "_Color" "2" layname "")
  )
  ;; Define a layer "TEXTOS" como a layer ativa
  (setvar "clayer" layname)

  ;; Inicia o loop principal. O comando continuará executando até o usuário pressionar Enter.
  (while 
    (setq ptBase (getpoint "\nClique no ponto para obter a coordenada (ou Enter para sair): "))
    
    ;; Se ptBase for válido (usuário clicou em um ponto)
    (if ptBase
      (progn
        ;; Solicita o ponto de destino para o texto da anotação
        (setq ptText (getpoint ptBase "\nClique no local para posicionar o texto: "))
        
        (if ptText
          (progn
            ;; Formata as coordenadas usando RTOS para garantir 4 casas decimais
            ;; e concatena em uma única string com quebra de linha (código {\P})
            (setq coordText (strcat
                              "E=" (rtos (car ptBase) 2 4)
                              "{\\P}" ; Quebra de linha para MTEXT
                              "N=" (rtos (cadr ptBase) 2 4)
                            ))

            ;; Executa o comando LEADER para criar a linha de chamada e o texto
            ;; O comando usará as configurações do DIMSTYLE e TEXTSTYLE correntes
            (command "_.LEADER" ptBase ptText "" coordText "")
          )
        )
      )
    )
  )

  ;; Restaura a layer que estava ativa antes do comando iniciar
  (setvar "clayer" oldlayer)

  ;; Mensagem final e saída limpa
  (princ "\nComando CCR finalizado.")
  (princ)
)

;; Mensagem de carregamento
(princ "\nComando 'CCR' carregado. Digite CCR para anotar coordenadas.")
(princ)
