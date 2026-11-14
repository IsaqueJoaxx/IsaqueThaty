; DrafTools   [Version 1.00a] 4/6/94       
;
; ***************************************
; ****  Author:  Owen Wengerd        ****
; ****                               ****
; ****  Manu-Soft Computer Services  ****
; ****  P.O. Box 84                  ****
; ****  Fredericksburg, OH  44627    ****
; ****  (216) 695-5903               ****
; ****  Compu-Serve ID:  71324,3252  ****
; ***************************************


(defun C:EDTITLE ( / 


;*** Local Variables ***

  as 
  il 
  p 
  ca 
  t1 
  oldvar 
  olderr 
  restore


;*** Local Functions ***

  edtitlex
  errexit
  fpath
  get_attrib
  )


; *****************  Function Definitions  *****************

(defun edtitlex ()
  (setvar "OSMODE" (nth 1 oldvar))
  (setvar "REGENMODE" (nth 2 oldvar))
  (setvar "EXPERT" (nth 3 oldvar))
  (setvar "CMDECHO" (car oldvar))
  (setq *error* olderr)
  (princ)
)

(defun errexit (s)
  (princ "\nError:  ")
  (princ s)
  (restore)
)

(defun fpath (filename / path)
  (if
    (and
      *DT_PATH
      (setq path
        (findfile
          (strcat
            *DT_PATH
            (if (= "\\" (substr *DT_PATH (strlen *DT_PATH) 1)) "" "\\")
            filename
          )
        )
      )
    )
    path
    (findfile filename)
  )
)

(defun get_attrib (ent tag / t1)
  (while (and ent (setq ent (entnext ent)))
    (setq t1 (entget ent))
    (if 
      (and (= "ATTRIB" (cdr (assoc '0 t1))) (= tag (cdr (assoc '2 t1))))
      (setq ent nil)
      (setq t1 nil)
    )
  )  
  t1
)


; ***********************************************
; ***************  Main Program  ****************
; ***********************************************

  (setq T (not nil))
  (setq oldvar
    (list
      (getvar "CMDECHO")
      (getvar "OSMODE")
      (getvar "REGENMODE")
      (getvar "EXPERT")
    )
  )
  (setq olderr  *error*
        restore edtitlex
        *error* errexit
  )
  (setvar "CMDECHO" 0)
  (setvar "OSMODE" 0)
  (setvar "REGENMODE" 1)
  (setvar "EXPERT" 0)
  (terpri)
  (graphscr)
  (if 
    (or
      (setq as (ssget "X" '((0 . "INSERT") (-3 ("TBLOCK")))))
      (setq as 
        (ssget "X" 
          (list 
            (cons 0 "INSERT") 
            (cons 8 (if *TBLAYER *TBLAYER "TITLE"))
          )
        )
      )
    )
    (progn
      (if (setq t1 (fpath "TBLOCK.LSP")) (load t1))
      (setq p 0)
      (if (< 1 (sslength as))
        (progn
          (setq t1 0)
          (setq il nil)
          (repeat (sslength as)
            (setq il
              (cons
                (cdr (assoc '10 (entget (ssname as t1))))
                il
              )
            )
            (setq t1 (1+ t1))
          )
          (setq il (reverse il))
          (if
            (not 
              (setq sp 
                (getpoint "Pick Insertion Point of Title Block To Edit:  ")
              )
            )
            (setq sp (getvar "LASTPOINT"))
          )
          (setq d (distance (car il) sp))
          (setq t1 1)
          (while (< t1 (length il))
            (if
              (> d (setq t2 (distance (nth t1 il) sp)))
              (progn
                (setq d t2)
                (setq p t1)
              )
            )
            (setq t1 (1+ t1))
          )
        )
      )                  
      (setq ca (ssname as p))
      (while
        (and 
          (setq ca (entnext ca)) 
          (/= "SEQEND" (cdr (assoc '0 (setq t1 (entget ca '("TBLOCK"))))))
        )
        (and
          (= "ATTRIB" (cdr (assoc '0 t1)))
          (setq t2
            (cdr (assoc '1000 (cdr (assoc "TBLOCK" (cdr (assoc '-3 t1))))))
          )
          (entmod
            (subst
              (cons '1 t2)
              (assoc '1 t1)
              t1
            )
          )
        )
      )
      (setq ca (ssname as p))
      (command "_DDATTE" ca)
      (setq *TBATTRIB ca)
      (if (/= 0 (cdr (assoc '66 (entget ca))))
        (progn
          (while
            (and (setq ca (entnext ca)) (setq t1 (entget ca)))
            (and
              (= "ATTRIB" (cdr (assoc '0 t1)))
              (= "==" (substr (cdr (assoc '1 t1)) 1 2))
              (entmod
                (subst
                  (cons '1 (eval (read (substr (cdr (assoc '1 t1)) 3))))
                  (assoc '1 t1)
                  t1
                )
              )
            )
          )
          (entupd (ssname as p))
        )
      )
    )
    (alert "*** You Must Insert a Title Block Before Attempting to Edit ***")
  )
  (restore)
)
