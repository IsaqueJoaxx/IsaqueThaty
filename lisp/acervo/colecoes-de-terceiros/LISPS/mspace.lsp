(defun C:MPSPACE ()
   (if (= (getvar "tilemode") 0)
      (if (eq 1 (getvar "cvport"))
         (command "_.mspace")
         (command "_.pspace"))
      (prompt
         (strcat
            "\nSet TILEMODE to 0"
            " to enter paper space!")))
   (princ)
)
