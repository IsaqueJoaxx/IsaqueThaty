;LOCKLYR.LSP
;This lisp has been extracted from [GET.LSP] written by David Adie,
;as published in CADalyst Magazine (Sept.'93), with minor moifications.
;It's purpose is to provide a utility to lock and unlock layers
;in AutoCad 12. You lock and unlock an entity by selecting it on screen.

(DEFUN C:LL (/ E N)
     (SETQ E (CAR (ENTSEL "\n pick layer to lock: ")))
     (if e nil  (princ "\n try again, no entity was selected:"))
     (if e
     (princ " \n layer selected is locked:"))
     (IF E  (PROGN
               (SETQ E (ENTGET E))
               (SETQ N (CDR (ASSOC 8 E)))
     (COMMAND "LAYER" "lock" N "")))
     (princ)
)
(DEFUN C:LU (/ E N)
     (SETQ E(CAR  (ENTSEL "\n pick layer to unlock: ")))
     (if e nil (princ "\n try again, no entity was selected:" ))
     (if e
     (princ "\n layer selected is unlocked:"))
     (IF E (PROGN
               (SETQ E (ENTGET E))
               (SETQ N (CDR (ASSOC 8 E)))
     (COMMAND "LAYER" "unlock" N "")))
     (princ)
) 
;end locklyr.lsp
