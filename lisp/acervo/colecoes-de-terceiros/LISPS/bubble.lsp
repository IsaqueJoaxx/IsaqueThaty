(defun c:bubble ( / ds p1 p2 b c)
   (graphscr)
   (setvar "cmdecho" 0)
   (command "osnap" "none")
   (setq ds (getvar "dimscale"))
   (setq p1 (getpoint "\nStart of line: "))
   (command "snap" (* 0.25 ds) on)
   (setq p2 (getpoint p1 "\nCenter of bubble: "))
   (setq b (* 0.5 ds))
   (command "line" p1 p2 ^c ^c)
   (command "circle" p2 (/ (abs b) 2))
   (command "trim" "l" "" p2 "")
   (command "snap" "off")
   (setq c (getstring "\nEnter item number:  "))
   (command "text" "m" p2 (* 0.20 ds) "0" c)
   (princ)
)



















































