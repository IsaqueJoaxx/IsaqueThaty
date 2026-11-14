(defun C:TS ()
 (setq termcen (getpoint "\nCenter of first terminal: "))
 (setq om (getvar "ORTHOMODE"))
 (setq om (1+ om))
 (if (= om 1) (setvar "ORTHOMODE" 1))
 (setq terma (getangle termcen "\nDirection of terminal strip: "))
 (setq termn (getint "\nNumber of terminals: "))
 (setq termsn (getint "\nStarting number <1>: "))
 (if (null termsn) (setq termsn 1))
 (setq termp1 (polar termcen (+ terma (* 3 (/ pi 4))) 0.28284))
 (setq termp2 (polar termp1 (+ terma (* 3 (/ pi 2))) 0.4))
 (setq termp3 (polar termp2 terma (* 0.2 (1+ termn))))
 (setq termp4 (polar termp3 (+ terma (/ pi 2)) 0.4))
 (command "line" termp1 termp2 termp3 termp4 "c")
 (while (>= termn 1)
  (command "circle" termcen 0.1)
  (command "text" "c"
   (polar termcen (* 3 (/ pi 2)) 0.05) 0.1 0 termsn)
  (setq termsn (1+ termsn))
  (setq termcen (polar termcen terma 0.2))
  (setq termn (1- termn))
 )
 (if (= om 1) (setvar "ORTHOMODE" 0))
)
;
;  C:TS automatically draws terminal strips for electrical
;wiring type schematics.  The terminals are drawn as
;circles with a number inside.  The finished terminal
;strip is a series of connected terminals enclosed by a
;rectangular box.
;
;  The function asks for user input for; center of the
;first terminal or the starting point, the direction that
;the terminal strip will be drawn from the start point,
;the number of terminals to be drawn in the terminal strip,
;and the starting number for the first terminal.  The
;terminals will be numbered automatically starting with
;the number entered for the starting number.
