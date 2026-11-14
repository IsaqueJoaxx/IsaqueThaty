;***** SHIELD.LSP *****
;
;  By Owen Wengerd
;  CompuServe ID:  71324,3252
;


(defun C:SHIELD (/ mspace oldvar restore errexit shieldx)


;Define Local Functions

(defun errexit (s)   ;Local Error Handler
  (princ "\nError:  ")
  (princ s)
  (restore)
)

(defun shieldx ()   ;Restore System Settings
  (setvar "CMDECHO" (car oldvar))
  (setq *error* olderr)
  (princ)
)

(defun make_block (ents / index bname ename elist)   ;Create Anonymous Block of 'ents'
  (if ents   ;only if 'ents' isn't nil
    (progn
      (entmake '((0 . "BLOCK") (2 . "*U") (70 . 1) (10 0 0 0)))  ;start block definition
      (setq index (sslength ents))
      (while   ;step through entities in 'ents' selection set
        (and
          (<= 0 (setq index (1- index)))
          (entmake (setq elist (entget (setq ename (ssname ents index)))))   ;write it
        )
        (if (= 1 (cdr (assoc 66 elist)))   ;sub-entities?
          (while   ;write them too!
            (/= "SEQEND" (cdr (assoc 0 (entmake (entget (setq ename (entnext ename)))))))
          )
        )
      )
      (setq bname (entmake '((0 . "ENDBLK"))))   ;get the anonymous block's name
      (command "_ERASE" ents "")   ;erase the originals
      (entmake (list   ;insert the anonymous block
        '(0 . "INSERT") 
        (cons 2 bname)
        '(8 . "0") 
        '(10 0 0 0)
        '(43 . 1.000000001)   ;Z scale is not quite 1, which makes the block 'unexplodable'
      ))
    )
  )
)


;MAIN PROGRAM
  
  (setq olderr  *error*
        restore shieldx
        *error* errexit
  )
  (setq oldvar (list (getvar "CMDECHO")))   ;save system variables
  (setvar "CMDECHO" 0)   ;turn off command echo
  (if (= 0 (getvar "TILEMODE"))   ;paper space?
    (progn
      (princ "\nShielding Paper Space Entities...")
      (if (setq mspace (/= 1 (getvar "CVPORT"))) (command "_PSPACE"))   ;PSPACE if necessary
      (make_block   ;create a block of all non-viewport paper space entities
        (ssget "X" 
          '((-4 . "<AND")
             (-4 . "=") (67 . 1)
             (-4 . "<NOT") (0 . "VIEWPORT") (-4 . "NOT>")
           (-4 . "AND>"))
        )
      )
      (command "_MSPACE")    ;then do the model space entities
      (princ "\nShielding Model Space Entities...")
      (make_block (ssget "X" '((67 . 0))))
      (if (not mspace) (command "_PSPACE"))   ;switch back if that's where it was
    )
    (progn
      (princ "\nShielding Model Space Entities...")
      (make_block (ssget "X" '((67 . 0))))    ;block all model space entities
    )
  )
  (princ "\n** Drawing Shielded **\n")
  
  (restore)   ;restore system settings
)




