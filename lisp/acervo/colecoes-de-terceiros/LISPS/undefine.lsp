;* UNDEFINE.LSP 
;* C:END replaces the AutoCAD END command with a customized version that
;* resets the enviroment, and optionally makes a slide of the drawing.

(command "UNDEFINE" "END")                   ;turns off AutoCAD's END command.

(defun C:END ()                              ;define our own.
  (if (= "Y" (ukword 1 "Y N" "Are you sure? (Y/N)" "Y"))
    (progn
      (command "SETVAR" "EXPERT" "3" "LAYER" "ON" "*" ""
        "FILL" "ON" "QTEXT" "OFF" "ATTDISP" "N" "ZOOM"
      );command
      (if (= "Y" (ukword 1 "Y N" "Make a record slide? (Y/N)" "N"))
        (command "ALL" "MSLIDE" "" ".END")   ;use AutoCAD's END
        (progn
          (prompt "\nDON'T wait for the regen! Hit  ^C  to continue END...\n")
          (command "ALL" ".END")
        );progn
      );if
    );progn
  );if
  (princ)             
);defun 
