; This function will produce a BOX or Rectangle
; and is provided as an example of CAR and CDR
;
(defun box ( / pnt1 pnt2 pnt3 pnt4) 
     (graphscr)
     (prompt "Box Requires 2 Opposing Corner Points ") 
     (terpri)
     (setq pnt1 (getpoint "First Point ? ")) (terpri)
     (setq pnt3 (getpoint "Second Point ? ")) (terpri)
     (setq pnt2 (list (car pnt1)(cadr pnt3)))
     (setq pnt4 (list (car pnt3)(cadr pnt1)))
     (command "line" pnt1 pnt2 pnt3 pnt4 "c")
)
