;;;------------------------------------------------------------------------------;;;
;;; Rotina: FB.lsp (Fundo Branco/Preto) - VERSÃO COM CONTROLE DE ESTADO        ;;;
;;; Autor: Manus                                                               ;;;
;;; Data: 14/11/2025                                                           ;;;
;;; Descrição: Alterna a cor de fundo do Model Space entre Preto e Branco com  ;;;
;;;            um único comando, usando uma variável de estado para garantir    ;;;
;;;            a alternância correta e confiável.                              ;;;
;;;------------------------------------------------------------------------------;;;

;; --- Variável de Controle Global ---
;; Esta variável irá guardar o estado atual do fundo (inicia como "preto").
;; O asterisco (*) é uma convenção em LISP para indicar variáveis globais.
(if (not *bg-state*)
    (setq *bg-state* "preto")
)

(defun c:fb (/ prefs disp)
  
  ;; --- Bloco de Tratamento de Erros ---
  (defun *error* (msg)
    (if (and msg (not (wcmatch (strcase msg) "*BREAK,*CANCEL*,*EXIT*")))
      (princ (strcat "\nErro na rotina FB: " msg))
    )
    (princ) ; Saída limpa
  )

  ;; 1. Garante que as funções VLISP estejam carregadas
  (vl-load-com)

  ;; 2. Acessa as preferências de Display (Tela)
  (setq prefs (vla-get-preferences (vlax-get-acad-object)))
  (setq disp (vla-get-display prefs))

  ;; 3. LÓGICA DE ALTERNÂNCIA BASEADA NO ESTADO
  (if (eq *bg-state* "preto")
    ;; SE o estado atual é "preto", ENTÃO mude para Branco.
    (progn
      ;; Define o fundo do Model para Branco (16777215)
      (vla-put-GraphicsWinModelBackgrndColor disp 16777215)
      ;; Define o cursor (crosshair) para Preto (0) para contraste
      (vla-put-ModelCrosshairColor disp 0)
      ;; ATUALIZA O ESTADO para "branco" para a próxima execução
      (setq *bg-state* "branco")
      (princ "\nFundo alterado para Branco.")
    )
    ;; SENÃO (se o estado for "branco")...
    (progn
      ;; ...mude para Preto.
      ;; Define o fundo do Model para Preto (0)
      (vla-put-GraphicsWinModelBackgrndColor disp 0)
      ;; Define o cursor (crosshair) para Branco (16777215) para contraste
      (vla-put-ModelCrosshairColor disp 16777215)
      ;; ATUALIZA O ESTADO de volta para "preto"
      (setq *bg-state* "preto")
      (princ "\nFundo alterado para Preto.")
    )
  )

  ;; Libera a memória dos objetos COM
  (vlax-release-object disp)
  (vlax-release-object prefs)
  
  (princ) ; Finaliza a função de forma limpa
)

;; Mensagem de carregamento
(princ "\nComando 'FB' (com Estado) carregado. Digite FB para alternar o fundo.")
(princ)
