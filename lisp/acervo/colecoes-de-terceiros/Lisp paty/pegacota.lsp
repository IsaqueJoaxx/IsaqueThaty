;--------------------Pega cota de String e atribui a um objeto -----------------------
(princ "\nDesenvolvido por Antonio J. Barbosa")
(princ "\nDigite pegacota")

;--------------------Troca virgula por ponto -----------------------

(defun trocavirg (/ ent entl pt)
   (setq ent(car (entsel "Selecione o texto com a cota ")))                ; Seleciona a entidade
   (setq entl(entget ent))            ; Retorna a lista de associação da entidade 
   (setq cta(cdr(assoc 1 entl)))      ; Pega o valor associado ao par "1" (texto)
   ;!cta
   (setq tamtotal (strlen cta))
   (setq tamint (strlen (itoa(atoi cta))))
   (setq dif (- tamtotal tamint))
   ;!dif
   (setq inteiro(substr cta 1 tamint))
   (setq pospt(+ tamint 1))           ; pega a posição do ponto separador
   (setq pt(substr cta pospt 1))      ; pega o conteudo da pospt
   (if (= pt ",")
     (progn
       (setq pt ".")
     )
   )
   (setq pospt(1+ pospt))
   (setq resto(substr cta pospt))      ; pega a parte decimal do texto
   (setq cota(strcat inteiro pt resto))     ; Junta tudo em cota   

)

(defun C:pegacota (/ cota g1 cota)
   (PROMPT "\nMuda elevacao de entidade(s).")
   (trocavirg)
   (PROMPT "\nSelecione a entidade.")
   (setq g1 (ssget))
   (prompt "\nElevacao: ")
   (command "change" g1 "" "p" "E" cota "")
   (command "change" cota "" "p" "C" "white" "")
   (command "change" g1 "" "p" "C" "magenta" "")
)
 