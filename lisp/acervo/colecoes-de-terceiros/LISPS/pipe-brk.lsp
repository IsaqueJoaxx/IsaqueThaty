; PIPE-BRK.LSP

;THIS IS TO DRAW A PIPE BREAK SYMBOL (LOOKS LIKE AN "8") AT THE END OF
;A PAIR OF PARALLEL LINES.  POINTS SHOULD BE PICKED IN COUNTERCLOCKWISE
;ORDER--IF AT THE RIGHT END OF PIPE, PICK THE BOTTOM END, THEN THE TOP END
;IF AT THE LEFT END OF PIPE, PICK THE TOP END FIRST, THEN THE BOTTOM END

(defun C:PIPE-BRK (/ pb pt pm)
  (graphscr)
  (setq os (getvar "osmode"))
  (setvar "osmode" 1);endpoint
  (setq pb (getpoint "Pick End of Pipe (In CCW Order): ")) (terpri)
  (setq pt (getpoint "Pick End of Pipe: ")) (terpri)
  (setvar "osmode" 0);none
  (setq pm (polar pb (angle pb pt) (/ (distance pb pt) 2)))
  (command "ellipse" pb pm "r" "70")
  (command "ellipse" pm pt "r" "70")
  (command "break" "l" pt pm)
  (command "mirror" "l" "" pb pt "y")
  (setvar "osmode" os)
  (princ)
)
