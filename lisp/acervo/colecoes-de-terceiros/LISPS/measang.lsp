; MEASANG.LSP

; TO MEASURE ANGLE BETWEEN 2 INTERSECTING LINES

(Defun C:MEASANG (/ os p1 p2 p3 a1 a2 d)
  (setq os (getvar "osmode"))
  (setvar "osmode" 512) ;nearest
  (setq p1 (getpoint "First Leg of Angle: ")) (terpri)
  (setvar "osmode" 32) ;intersection
  (setq p2 (getpoint p1 "Vertex of Angle: ")) (terpri)
  (setvar "osmode" 512) ;nearest
  (setq p3 (getpoint p2 "2nd Leg of Angle: ")) (terpri)
  (setvar "osmode" os)
  (setq a1 (angle p2 p1))
  (setq a2 (angle p2 p3))
  (setq d (abs (- a2 a1)))
  (prompt "Angle Measures: ") (prompt (angtos d)) (terpri)
  (princ) ;to exit quietly w/o "nil" message at end
)