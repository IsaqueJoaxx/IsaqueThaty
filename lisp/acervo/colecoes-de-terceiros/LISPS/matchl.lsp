; MATCHL.LSP

; SETS THE LAYER TO MATCH THE LAYER OF THE ENTITY
; YOU POINT TO

(defun C:MATCHL (/ x )
(setq x (Entsel "\nSelect object whose layer is to be matched: "))
(command "layer" "S" (cdr (assoc 8 (entget (car x)))) "" )
)
