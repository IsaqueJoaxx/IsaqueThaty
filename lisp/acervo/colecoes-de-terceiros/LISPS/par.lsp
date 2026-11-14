(defun c:PAR1 ()
(setvar "CMDECHO" 0)
(setq p4 (getpoint "\npoint to line: "))
(setq p4 (osnap p4 "near"))
(setq x (osnap p4 "end"))
(setq y (osnap x "mid"))
(setq z (distance x y))
(setq a (angle x y))
(setq perpa1 a)
(setq perpa1 (angtos perpa1 0 5))
(setq perpSM (getvar "SNAPMODE"))
(setq perpSM (1+ perpSM))
(command "snap" "r" "0,0" (read perpa1))
(setq perpOM (getvar "ORTHOMODE"))
(setq perpOM (1+ perpOM))
(if (= perpOM 1) (setvar "ORTHOMODE" 1))
(setq p3 (getdist p4 "\nenter offset from center: "))
(command "snap" "r" "0,0" "0")
(if (= perpOM 1) (setvar "ORTHOMODE" 0))
(if (= perpSM 1) (setvar "SNAPMODE" 0))
(setq p1 (polar x (+ (/ pi 2) a) p3))
(setq p2 (polar p1 a (* z 2)))
(command "line" p1 p2 "")
(setq p1 (polar p1 (+ (/ (* 3 pi) 2) a) (* 2 p3)))
(setq p2 (polar p1 a (* z 2)))
(command "line" p1 p2 "")
)
;
;  C:PAR1 draws a set of lines parallel to a selected line.
;The parallel lines are drawn on each side of the selected
;line.  The function first prompts the user to select the
;desired line, then for the offset distance.  The parallel
;lines drawn are the same length as the selected line.
