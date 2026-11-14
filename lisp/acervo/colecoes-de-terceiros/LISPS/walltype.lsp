; WALLTYPE.LSP
; Routine to List, Create and Load the walltypes used by WALL4.LSP.
; For use with AutoCAD 2.6 and later.
; Included as a part of the WALLS utilities from Alacrity.
; Copyright 1988 Alacrity

(defun c:WALLTYPE (/ errmsg loadtype listtype crttype
                     color ltype layer temp dist
                     in out data );err)  
  (defun errmsg (a b c)
     (princ "\n   ")
     (princ a)
     (if (equal (type b) 'STR)
        (princ b)
        (prin1 b)
     )
     (princ c)
  )
  (defun loadtype ()
   (while (equal (setq waltype (strcase (getstring "\nWalltype to load: "))) "")
     (princ "\nInvalid input...")
   )
   (initget 1)
   (setq temp (getstring (strcat "\nFile to search <" file ">: "))
         wallst ""
   )
   (if (not (equal temp "")) (setq file temp))
   (if (setq in (open (strcat file ".WAL") "r"))   
       (progn
        (while
         (and
          (setq data (read-line in))
          (cond
              ((equal (strcase (substr data 2)) waltype)
               (progn
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
        (if (equal wallst "")
            (progn
             (princ "\nWalltype not found.")
             (setq waltype nil)
            )
            (progn
             (setq wallst (read wallst))
             (princ (strcat "\nWalltype " waltype " loaded."))
             (testtype)
            )
        )
       )
       (princ "\nFile not found.")
   )
   T
  )
  (defun testtype ()
   (mapcar
      '(lambda (lst)
        (if (/= (length lst) 4)
         (errmsg "Line segment " lst " is invalid.")
         (progn
          (setq layer (car lst)
                ltype (cadr lst)
                color (caddr lst)
                dist  (last lst)
          )
          (if layer
            (if (equal (type layer) 'STR)
              (if (setq temp (tblsearch "LAYER" layer))
                (if (minusp (cdr (assoc 62 temp)))
                   (errmsg "Layer " layer " is OFF or Frozen.")
                )
                (errmsg "Layer " layer " not found.")
              )
              (errmsg "Layer " layer " is invalid.")
            )
          )
          (if ltype
            (if (equal (type ltype) 'STR)
              (if (not (equal ltype "BYLAYER"))
                 (if (not (tblsearch "LTYPE" ltype))
                    (errmsg "LType " ltype " not loaded.")
                 )
              )
              (errmsg "LType " ltype " is invalid.")
            )
          )
          (if color 
            (if (not 
               (or 
                (numberp color)
                (numberp (atoi color))
                (member (strcase color) 
                   (list "RED" "WHITE" "BLUE" "GREEN" "YELLOW" "CYAN"
                         "MAGENTA" "BYLAYER"))
                ))
               (errmsg "Color " color " is invalid.")
            )
          )
          (if (not (numberp dist))
             (errmsg "Offset " dist " is invalid.")
          )
         )
        )
      )
      wallst
   )
  )
  (defun crttype ()
   (setq i 1 wallst nil)
   (if (setq waltype (strcase (getstring "\nName of walltype to create: ")))
     (progn
      (while 
        (progn
         (princ (strcat "\nLine segment #" (itoa i) "\n"))
         (if (equal (setq layer (strcase (getstring "  Layer: "))) "") 
             (setq layer nil)
         )
         (if (equal (setq ltype (strcase (getstring "  LType: "))) "")
             (setq ltype nil)
         )
         (if (equal (setq color (strcase (getstring "  Color: "))) "")
             (setq color nil)
         )
         (setq dist (getreal "  Offset: "))
        )
        (setq wallst (append (list (list layer ltype color dist)) wallst)
              i (1+ i))
      )
      (if wallst
        (progn
         (setq wallst (reverse wallst))
         (princ (strcat "\nWalltype " waltype " is now current."))
         (testtype)
         (initget "Yes No")
         (setq temp (getkword (strcat "\nSave " waltype " to file? No/<Yes>: ")))
         (if (or (not temp) (equal temp "Yes"))
            (progn
             (setq temp (getstring (strcat "\nFile to save walltype <" file ">: ")))  
             (if (not (equal temp "")) (setq file temp))
             (if (setq out (open (strcat file ".wal") "a"))
                (progn
                 (princ "\n" out)
                 (princ (strcat "*" waltype) out)
                 (princ "\n" out)
                 (prin1 wallst out)
                 (setq out (close out))
                 (princ "\nWalltype successfully saved.")
                )
             )
            )
         )
        )
      )
     )
   )
   T
  )        
  (defun listtype ()
     (initget 1)
     (setq temp (getstring (strcat "\nFile to list <" file ">: ")))
     (if (not (equal temp "")) (setq file temp))
     (if (setq in (open (strcat file ".wal") "r"))
         (progn
          (setq in (close in))
          (if waltype 
              (princ (strcat "\nCurrent walltype: " waltype "\n\n"))
          )
          (command "TYPE" (strcat file ".wal"))
         )
         (princ "\nFile not found.")
     )
     T
  )

  (if (not file) (setq file "ACAD"))
  (setq err *error*)
  (defun *error* (msg)
     (setq in (close in))
     (princ "\nerror: ")
     (princ msg)
     (setq *error* err)
     (princ)
  )
  (setvar "CmdEcho" 0)

  (while
    (and
     (not (initget "? Create Load"))
     (setq temp (getkword "\n?/Create/Load: "))
     (cond 
       ((equal temp "?")
        (listtype)
       )
       ((equal temp "Create")
        (crttype)
       )
       ((equal temp "Load")
        (loadtype)
       )
     )    
    )
  )
  (setq *error* err)
  (princ)
)
