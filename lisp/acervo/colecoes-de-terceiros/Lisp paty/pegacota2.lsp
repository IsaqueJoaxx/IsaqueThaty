;--------------------Pega cota de uma LWPOLILINE e atribui a um objeto -----------------------
(princ "\nDesenvolvido por Antonio J. Barbosa")
(princ "\nDigite pegacota2")


(defun C:pegacota2 (/ s1 s2 cota echo)
   (setq echo (getvar "CMDECHO"))
   (setvar "CMDECHO" 0)
   (princ "\nSelecione todos os objetos que irão mudar de Elevação: ")
   (setq s1 (ssget))
   (setq ent (entsel "\nAgora selecione uma LWPOLILINE objeto para copiar a elevação: "))
   (if (NULL ent); Faça enquanto existirem objetos selecionados
      (while (NULL ent)
         (princ "\nTente novamente ou pressione  ^C para sair: ")
         (setq ent (entsel))
      ); While
   ): If
   (setq ent (entget (car ent)))
   (setq cota (cdr (assoc 38 ent)))  ; para lwpoliline
   ;(setq cota (caddr (assoc 30 ent)))  ; para line
(setq z(caddr p1))
   (COMMAND ".CHANGE" s1 "" "PROP" "E" cota "")
   (setvar "CMDECHO" echo)
   (princ)
); Defun pegacota2