; WALLLOAD.LSP
; Routine to Load the walltypes used by WALL4.LSP.
; For use with AutoCAD 2.6 and later.
; Included as a part of the WALLS utilities from Alacrity.
; Copyright 1988 Alacrity

(defun WALLLOAD (name file / in data err found) 
  (setq err *error*)
  (defun *error* (msg)
     (setq in (close in))
     (princ "\nerror: ")
     (princ msg)
     (setq *error* err)
     (princ)
  )
  (setq name (strcase name))
  (if (not (equal name waltype))
   (if (setq in (open (strcat file ".WAL") "r"))   
      (progn
       (while
        (and
         (setq data (read-line in))
         (cond
             ((equal (strcase (substr data 2)) name)
              (progn
               (setq wallst "" found T)
               (while 
                (and
                 (setq data (read-line in))
                 (not (equal (substr data 1 1) "*"))
                )
                (setq wallst (strcat wallst " " data))
               )
               nil
              )
             )
             (T T)
         )
        )
       )
       (setq in (close in))
       (if (not found)
           (princ "\nWalltype not found.")
           (progn
            (setq wallst (read wallst)
                  waltype name
            )
            (princ (strcat "\nWalltype " name " loaded."))
           )
       )
      )
      (princ "\nFile not found.")
   )
   (princ "\nWalltype already loaded.")
  )
  (setq *error* err)
  (princ)
)