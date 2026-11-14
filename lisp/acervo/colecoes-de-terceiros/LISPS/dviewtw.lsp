; DVIEWTW.LSP

; Jim Nakazawa
; (415) 768-1234

; TO SET DVIEW TWIST ANGLE BY POINTING TO AN EXISTING LINE
; FIRST DRAW THE LINE FROM LEFT TO RIGHT THAT YOU WANT TO SET THE DVIEW
; TWIST ANGLE FROM

(defun C:DVIEWTW (/ rgm a b pt1 pt2 ang1 )
  (setq rgm (getvar "regenmode"))
  (command "setvar" "regenmode" 0)
  (setq a (entsel "Pick Line to Set DVIEW TWist Angle: "))
  (setq b (entget (car a)))
  (setq pt1 (cdr (assoc 10 b)))
  (setq pt2 (cdr (assoc 11 b)))
  (setq ang1 (angle pt1 pt2))
  (setq ang1 (/ (* ang1 180.0) pi))
  (setq ang1 (- ang1))
  (command "dview"  "" "tw" ang1 "")
  (command "setvar" "regenmode" rgm)
  (princ "Dview Twist Angle Set to ") (princ (+ 360.0 ang1)) (terpri)
;this should be modified to show angle in current angular units
  (princ)
) ;this routine seems to work
