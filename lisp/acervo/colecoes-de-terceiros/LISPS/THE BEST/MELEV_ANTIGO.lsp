(defun c:MELEV () 
(setq L1 (entsel "\nSelecione a POLYLINHA...:"))
(command "change" PAUSE "" "p" "E" (getstring "\nDigite a Elevação...:")""))
(princ "\n------- Digite \"MELEV para Iniciar Comando...\"")