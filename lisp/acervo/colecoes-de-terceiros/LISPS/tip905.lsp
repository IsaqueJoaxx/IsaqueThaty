; TIP905.LSP   Changing Layers   NEWLAYER.LSP  (C)1993,  John E. Wilson

; Changes every entity on old layer to new layer
; The two while loops get the layer names, check through the AutoCAD symbol
; table to make sure that layers exist. If they don't exist the user is
; given a chance to either enter another name or quit.
; Once the layer names are entered, the program makes up a selection set of
; every entity in the old layer and then uses the CHPROP command to change
; them to the new layer.

(defun C:NEWLAYER ( / LOOP CMD TEST OLDLAY ANSWER NEWLAY SS)
   (setq LOOP T
   CMD (getvar "CMDECHO"))
   (setvar "CMDECHO" 0)
   (while loop
      (setq OLDLAY (strcase (getstring "\nName of Layer to be Changed: "))
      TEST (tblsearch "Layer" OLDLAY))
      (if TEST  (setq LOOP nil
         ANSWER "Y")
         ; else
         (progn
            (princ "Layer ") (princ OLDLAY)
            (princ " does not exist.  Enter another one?\n")
            (initget 1 "Y N")
            (setq ANSWER (strcase (getkword "Type in Y or N: ")))
            (if (equal ANSWER "N")  (setq LOOP nil))
         ) ; end progn
      ) ; end if
   ) ; end while

   (if (equal ANSWER "Y") (setq LOOP T))

   (while LOOP
      (setq NEWLAY (strcase (getstring "Name of New Layer: "))
      TEST (tblsearch "Layer" NEWLAY))
      (if TEST  (setq LOOP nil
         ANSWER "Y")
         ; else
         (progn
            (princ "Layer ") (princ newlay)
            (princ " does not exist.  Enter another one?\n")
            (initget 1 "Y N")
            (setq ANSWER (strcase (getkword "Type in Y or N: ")))
            (if (equal ANSWER "N")  (setq LOOP nil))
      )) ; end progn & if
   ) ; end while

   (if (equal ANSWER "Y")
      (progn
         (setq SS (ssget "X" (list (cons 8 OLDLAY))))
         (if SS
            (progn
               (command "CHPROP" SS "" "la" NEWLAY "")
               (princ (sslength SS)) (princ " entities changed.\n")
            )  ; else
            (progn
               (princ "No entities in layer ") (princ OLDLAY)
         )) ; end inside if
      ) ;  else
      (princ "Function cancelled.")
   ) ; end outside if
   (setvar "CMDECHO" CMD)
   (princ)
); end newlayer.lsp
