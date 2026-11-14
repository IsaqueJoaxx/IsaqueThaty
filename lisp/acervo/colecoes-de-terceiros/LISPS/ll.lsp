;;; Lists on/thawed layers
;;;
;;; LLD, list layers to drawing as text
;;; LLF, list layers to file
;;;
;;; Guido Haarmans. Neuchâtel, Switzerland. January 1994
;;;
;;; Freeware
;;; I wrote this after seeing some requests on Compuserve.
;;; Contact me via Compuserve account 73324,1133 if you experience problems

; ------------------------------------------------------------------------------ 

(defun c:llf ()
;; List on/thawed layers to a file
	
    (sysvars T)
	
    (setq fname (getstring "\nFile name for listing >: "))
    (setq fp (open fname "w"))                   ; no check if file exists

    (write-line (strcat "List of on + thawed layers in drawing " 
      (getvar "dwgname") "\n") fp)

    (setq first T)                              ; rewind table at first entry

    (while (setq lay (tblnext "layer" first))
  	(if first (setq first nil))             ; don't rewind next loop
        (if (and 
              (= (rem (cdr (assoc 70 lay)) 2) 0); flag bit 1 not set
              (> (cdr (assoc 62 lay)) 0)        ; color > 0
            )
            (write-line (cdr (assoc 2 lay)) fp)
        )
    )

    (close fp)
    (sysvars nil)
    (princ)
)
	
	
; ------------------------------------------------------------------------------ 
(defun c:lld ()
;; List of on/thawed layer to drawing as text entities

    (sysvars T)

    (setq pt (getpoint "\nEnter start point for text >:"))
    (setq ht (getdist pt "\nEnter text height >:"))

    (setq first T)                              ; rewind table at first entry
    (while (setq lay (tblnext "layer" first))
	(if first (setq first nil))                 ; don't rewind next loop
        (if (and 
              (= (rem (cdr (assoc 70 lay)) 2) 0); flag bit 1 not set
              (> (cdr (assoc 62 lay)) 0)        ; color > 0
            )

            (progn                              ; layer is thawed and on
              (setq name (cdr (assoc 2 lay)))   ; get layer name  
              (command "text" pt ht "0.0" name) ; draw text
              (setq pt (list (car pt) (- (cadr pt) (* ht 1.4)) (caddr pt)))
           )
        )
    )

    (sysvars nil)

    (princ)
)

; ------------------------------------------------------------------------------
(defun *error* ( / msg)
    (princ msg)
    (sysvars nil)
)
; ------------------------------------------------------------------------------
(defun sysvars ( what / )

    (if what
	(progn                                    ; called on entering program
          (setq ll_echo (getvar "cmdecho"))   ; store old echo
          (setvar "cmdecho" 0)                ; set echo to 0
          (setq ll_err *error*)               ; end *error*
	)
	(progn                                    ; called at leaving or by *error*
          (setvar "cmdecho" ll_echo)          ; restore echo
          (setq *error* ll_err ll_err nil)    ; restore *error*
	)
    )
)
; ------------------------------------------------------------------------------
(prompt "\n\nType LLF to list to file or LLD to list to drawing.") (princ)