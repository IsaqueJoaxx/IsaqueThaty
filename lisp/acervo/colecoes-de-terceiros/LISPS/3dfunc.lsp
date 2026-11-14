;3DFUNC.LSP   copyright 1988  Thäo Armour

(defun C:3Dfunc (/ x1 x2 xx y1 y2 yy z1 z2 z3 z4)
(defun zcalc (xx yy)

;Add or remove ;'s to run each function
;Example 1
;(* 16 (sin (* 0.004 (- (* xx xx) (* yy yy)))))
;Example 2
;(*  3 (cos (* -0.01 (+ (* xx xx) (* yy yy)))))
;Example 3 (this one is set to run)
(+
(*  24 (exp (- (* -0.005 (- yy 16) (- yy 16))
       (* 0.015 (+ xx 20) (+ xx 20)))))
(* -16 (exp (- (* -0.008 (- yy 14) (- yy 14))
       (* 0.005 (- xx 18) (- xx 18)))))
(*  18 (exp (- (* -0.015 (+ yy 22) (+ yy 22))
       (* 0.003 (+ xx 18) (+ xx 18)))))
(* -20 (exp (- (* -0.025 (+ yy 12) (+ yy 12))
       (* 0.015  (- xx 20) (- xx 20))))))
;Example 4
;(/ (* xx yy yy) (+ (* xx xx) (* yy yy yy yy)))
; Before running: set x1 from -0.95 to 1,
; and y1 to -0.95 to 1 & incr 0.025
)

(setvar "CMDECHO" 0)
(setq y1 -30)
(while (< y1 30)
  (setq x1 -30
        y2 (1+ y1)
        z1 (zcalc x1 y1)
        z4 (zcalc x1 y2))
  (while (< x1 30)
    (setq x2 (1+ x1)
          z2 (zcalc x2 y1)
          z3 (zcalc x2 y2))
    (command "3Dface"
    (list x1 y1 z1)
    (list x2 y1 z2)
    (list x2 y2 z3)
; Add or remove ;'s to alter code to plot
; either square or triangular faces
;    (list x1 y2 z4)
;     "")
    "" "")
    (command "3Dface"
    (list x1 y1 z1)
    (list x2 y2 z3)
    (list x1 y2 z4)
    "" "")
    (setq x1 x2 z1 z2 z4 z3))
  (setq y1 y2))
(setvar "CMDECHO" 1))

