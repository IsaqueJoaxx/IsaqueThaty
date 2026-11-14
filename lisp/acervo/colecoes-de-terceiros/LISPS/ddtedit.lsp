;==========================================================
; DDTEDIT.LSP Copyright 1993 by Looking Glass Microproducts
;==========================================================
; Edits Text, Attdefs, and Attributes. 
;=============================================================
(defun C:DDTEDIT (/ ERROR PUSHVARS POPVARS SYSVARS OLD-ERROR 
                  NOTRANS DDTEDIT ITEM)

   ;==========================================================
   ; Error Handler
   (defun ERROR (S)
      (if (not
             (member
                S
                '("Function cancelled" "console break")
             )
          )
         (alert S)
      )
      (command ".undo" "end")
      (POPVARS)
      (princ)
   )
   ;==========================================================
   ; Set and Save System Variables
   (defun PUSHVARS (VLIST)
      (foreach PAIR VLIST
         (setq
            SYSVARS (cons
                       (cons
                          (strcase (car PAIR))
                          (getvar
                             (car PAIR)
                          )
                       )
                       SYSVARS
                    )
         )
         (if (cdr PAIR) (setvar (car PAIR) (cdr PAIR)))
      )
   )
   ;==========================================================
   ; Restore System Variables
   (defun POPVARS ()
      (foreach PAIR SYSVARS (setvar (car PAIR) (cdr PAIR)))
      (setq
         *error* OLD-ERROR
      )
      (setq SYSVARS nil)
   )
   ;==========================================================
   ; Restore a Single System Variable
   (defun RESTORE (VARNAME / OLD-VALUE)
      (if (setq
             OLD-VALUE (cdr (assoc (strcase VARNAME) SYSVARS))
          )
         (setvar VARNAME OLD-VALUE)
      )
   )
   ;==========================================================
   ; Disallow transparent invocation of routine.
   (defun NOTRANS ()
      (cond
         ((zerop (logand (getvar "cmdactive") (+ 1 2 4 8))))
         ((alert
             "This command may not be invoked transparently."
          )
         )
      )
   )
   ;==========================================================
   ; Item from Association List
   ;==========================================================
   (defun ITEM (A B) (cdr (assoc A B)))
   ;==========================================================
   ; Main routine
   ;==========================================================
   (defun DDTEDIT (/ ESEL ENAME PRMPT EDITED ENT ETYPE NEW_ENT
                   NEW_ENAME)
      (setq
         PRMPT (strcat
                  "\n<Select a TEXT, ATTDEF,"
                  " or ATTRIBUTE object>/Undo: "
               )
      )
      (while (progn
                (initget "Undo")
                (setq ESEL (nentsel PRMPT))
             )
         (cond
            ((and (= "Undo" ESEL) (null EDITED))
               (prompt
                  "\nNothing to undo."
               )
            )
            ((= "Undo" ESEL)
               (command ".undo" "back")
               (setq
                  EDITED (cdr EDITED)
               )
            )
            ((progn
                (setq
                   ENAME (car ESEL)
                   ENT   (entget ENAME)
                   ETYPE (ITEM 0 ENT)
                )
                (not
                   (member ETYPE '("TEXT" "ATTDEF" "ATTRIB"))
                )
             )
               (prompt
                  "\nNot a Text, Attdef, or Attribute object."
               )
            )
            ((> (length ESEL) 2)
               (prompt
                  "\nCannot edit nested entities."
               )
            )
            ((member ETYPE '("TEXT" "ATTDEF"))
               (setq
                  EDITED (cons ESEL EDITED)
               )
               (command ".undo" "mark")
               (command
                  ".ddedit" ESEL ""
               )
            )
            ((= ETYPE "ATTRIB")
               (setq
                  EDITED (cons ESEL EDITED)
               )
               (command ".undo" "mark")
               ;=============================
               ; Copy the attribute as text
               ;=============================
               (setq
                  NEW_ENT (list
                             '(0 . "TEXT")
                             (assoc 1 ENT)
                             (assoc 10 ENT)
                             '(40 . 1E-6)
                          )
               )
               (entmake NEW_ENT)
               (setq NEW_ENAME (entlast))
               ;=============================
               ; Edit the text
               ;=============================
               (command
                  ".ddedit"
                  (cons NEW_ENAME (cdr ESEL)) ""
               )
               (setq NEW_ENT (entget NEW_ENAME))
               ;=============================
               ; Put the edited text into the
               ; Attribute
               ;=============================
               (setq
                  NEW_ENT (subst
                             (assoc 1 NEW_ENT)
                             (assoc 1 ENT)
                             ENT
                          )
               )
               (entdel NEW_ENAME)
               (if (not (equal NEW_ENT ENT))
                  (progn
                     (entmod NEW_ENT)
                     ;===========================
                     ; find parent of attribute
                     ;===========================
                     (while (progn
                               (setq
                                  ENAME (entnext ENAME)
                                  ENT   (entget ENAME)
                                  ETYPE (ITEM 0 ENT)
                               )
                               (/= ETYPE "SEQEND")
                            )
                     )
                     (setq ENAME (ITEM -2 ENT))
                     ;===========================
                     ; Regenerate the block
                     ;===========================
                     (setvar "blipmode" 0)
                     (command
                        ".scale" ENAME "" '(0 0) 1
                     )
                     (RESTORE "blipmode")
                  )
               )
            )
         )
      )
   )
   ;==========================================================
   ; Body of c:ddtedit  
   ;==========================================================
   (if (NOTRANS)
      (progn
         (setq OLD-ERROR *error* *error* ERROR)
         (setvar
            "cmdecho" 0
         )
         (command ".undo" "group")
         (PUSHVARS '(("highlight" . 0) ("blipmode")))
         (DDTEDIT)
         (command ".undo" "end")
         (POPVARS)
      )
   )
   (princ)
)
(princ
   (strcat
      "  DDTEDIT.LSP (Copyright 1993 by"
      " Looking Glass Microproducts) loaded."
   )
)
(princ)

