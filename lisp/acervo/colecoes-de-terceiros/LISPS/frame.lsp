
; This function will produce a Solid Frame or Border
; using a pline with width, and is provided as an example 
; of (CAR), (CADR) and point processing.
;
(defun frame ( / pnt1 pnt2 pnt3 pnt4 width)
     (graphscr)
     (prompt "Frame Requires 2 Opposing Corner Points and a Width ") 
     (terpri)
     (setq pnt1 (getpoint "First Point ? ")) (terpri)
     (setq pnt3 (getpoint "Second Point ? ")) (terpri)
     (setq width (getdist pnt3 "Frame Width ? "))
     (setq pnt2 (list (car pnt1)(cadr pnt3)))
     (setq pnt4 (list (car pnt3)(cadr pnt1)))
     (command "pline" pnt1 "w" width "" pnt2 pnt3 pnt4 "c")
)Š