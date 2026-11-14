(defun c:CotaZero (/ ss)
  (setvar "cmdecho" 0)
  (princ "\nSelecionando todas as entidades do desenho...")
  
  ;; Seleciona todos os objetos no desenho
  (setq ss (ssget "_X"))
  
  (if ss
    (progn
      (princ "\nProcessando... Por favor, aguarde.")
      
      ;; Técnica de deslocamento para o infinito e volta para garantir 
      ;; que todos os tipos de entidades (incluindo 3D Polylines e blocos) fiquem em Z=0
      (command "_.move" ss "" "0,0,0" "0,0,1e99")
      (command "_.move" ss "" "0,0,0" "0,0,-1e99")
      
      (princ (strcat "\nSucesso! " (itoa (sslength ss)) " entidades movidas para a cota zero."))
    )
    (princ "\nNenhuma entidade encontrada no desenho.")
  )
  
  (setvar "cmdecho" 1)
  (princ)
)

;; Atalho alternativo mais curto para o comando
(defun c:CZ () (c:CotaZero))

(princ "\nRotina CotaZero carregada. Digite COTAZERO ou CZ para executar.")
(princ)