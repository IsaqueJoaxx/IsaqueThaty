(defun C:LNAMES (/ FILE FILE_EXT LAY_NAME OPEN_FILE)
   (if *error*
     (setq olderror *error*)
   )
   (defun *error* (msg)
      (close OPEN_FILE)
      (princ msg)
      (if olderror
        (setq *error* olderror)
      )
   )
   (setvar "cmdecho" 0)
   (setq CONT "X")
   (if (not *FILE)(setq *FILE "FileName"))
   (princ (strcat"\nEnter Valid File Name <" *FILE))
   (setq FILE (getstring ">: "))
   (if (= FILE "")
       (setq FILE *FILE)
       (setq *FILE FILE)
   )
   (setq FILE (strcase FILE) *FILE (strcase *FILE))
   (setq X (tblnext "layer" T))
   (setq LAY_NAME (cdr (assoc 2 X)))
   (setq OPEN_FILE (open FILE "w"))
;   (write-line LAY_NAME OPEN_FILE)  ;<- THIS LINE WILL PRINT ONE LAYER NAME PER LINE
   (princ (strcat Lay_NAME " ") OPEN_FILE) ;<- THIS LINE WILL PRINT THE LAYER NAMES ON ONE LINE WITH A SPACE IN BETWEEN
   (princ "\nWorking.")
   (while CONT
     (if (not (setq X (tblnext "layer")))
         (progn
            (close OPEN_FILE)
            (princ (strcat "\nFile " FILE " Complete..."))
            (setq CONT nil)
         )
         (progn
             (setq LAY_NAME (cdr (assoc 2 X)))
;             (write-line LAY_NAME OPEN_FILE)  ;<- THIS LINE WILL PRINT ONE LAYER NAME PER LINE
             (princ (strcat Lay_NAME " ") OPEN_FILE) ;<- THIS LINE WILL PRINT THE LAYER NAMES ON ONE LINE WITH A SPACE IN BETWEEN
             (princ ".")
         )
     )
   )
   (if olderror
      (setq *error* olderror)
   )
   (princ)
)

