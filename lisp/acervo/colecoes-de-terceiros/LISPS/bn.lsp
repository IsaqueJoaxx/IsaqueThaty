;Add block name by picking block
;
;
(defun C:bn()
(princ "\nAdd block name to drawing.")
(setq pt(cadr(entsel"\nSelect Block:")))
(setq e1(ssget pt)) 
(setq e2 (entget (ssname e1 0)))
(setq blname (cdr(assoc 2 e2)))
(setq pt1 (getpoint"\nSelect point for block title:"))
(command "text" "c" pt1 "" blname))
(princ "\nType BN to execute.)