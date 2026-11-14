(defun c:PERP ()
(setvar "CMDECHO" 0)
(setq perp1 (getpoint "\nperp from: "))
(setq perpN (osnap perp1 "near"))
(setq perp2 (osnap perpN "end"))
(setq perpa1 (angle perpN perp2))
(setq perpa1 (angtos perpa1 0 5))
(setq perpSM (getvar "SNAPMODE"))
(setq perpSM (1+ perpSM))
(command "snap" "r" "0,0" (read perpa1))
(setq perpOM (getvar "ORTHOMODE"))
(setq perpOM (1+ perpOM))
(if (= perpOM 1) (setvar "ORTHOMODE" 1))
(command "line")
(command perpN)
(while (setq perp3 (getpoint))
(command perp3)
)
(command "")
(command "snap" "r" "0,0" "0")
(if (= perpOM 1) (setvar "ORTHOMODE" 0))
(if (= perpSM 1) (setvar "SNAPMODE" 0))
)
;
;  C:PERP draws perpendicular lines from a line to a point.
;This is opposite to the function of the "osnap 
;perpendicular" command.
;
;  The function prompts the user for the "from" point first.
;The point selected must be a line.  PERP then invokes the
;"snap rotated" command according to the angle of the line
;selected.  The "ortho mode" is switched on, then the "line"
;command.  This sequence of steps allows the user to rubberband
;the perpendicular line from the selected point.
;
;  Before PERP is finished, it resets the "snap" and "ortho"
;to whatever state they were before the function was used.
