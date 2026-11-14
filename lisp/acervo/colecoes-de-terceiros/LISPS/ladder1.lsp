
; Draws ladder in elevation with ladder bucks
; extending 2'-6" above wall height
;
; Written by:  John Mauldin
;              P.O. Box 3641
;              Greenville, SC 29608
;
; I know there is a better way to write this routine but this is my first

(defun C:LAD 
  (/ wt2 rng vdst bwth p1 p2 p3 p4 p5 p6 p7 p8 p9 p10 p11 ht ht1)
  (setq wt2 9.0)         ; half width in inches full width = 18"
  (setq rng 0.75)        ; diameter of rungs 3/4"
  (setq vdst 10.0)       ; vertical distance between rungs 10"
  (setq bwth 0.625)      ; width of ladder bucks 5/8"
  (graphscr)
  (setq p1 
     (getpoint "\nCenterline of ladder at base: "))
  (setq p2 
     (getdist "\nEnter wall height or pick from screen: " p1))
  (setq ht 
     (+ p2 30.0))
  (setq ht1 
     (/ p2 12))
  (setq ht1 
     (fix ht1))
  (setq p3 
     (list 
        (- 
           (car p1) 
           (+ wt2 bwth))
        (cadr p1)))
  (setq p4 
     (list 
        (car p3)
        (+ 
           (cadr p3) vdst)))
  (setq p5 
     (list 
        (car p4)
        (+ 
           (cadr p4) rng)))
  (setq p6 
     (list 
        (+ 
           (car p1) 
           (+ wt2 bwth))
        (cadr p1)))
  (setq p7 
     (list 
        (car p6)
        (+ 
           (cadr p6) vdst)))
  (setq p8 
     (list 
        (car p7)
        (+ 
           (cadr p7) rng)))
  (setq p9 
     (list 
        (car p3)
        (+ 
           (cadr p3) ht)))
  (setq p10 
     (list 
        (- 
           (car p9) bwth)
        (cadr p9)))
  (setq p11 
     (list 
        (- 
           (car p3) bwth)
        (cadr p3)))
  (command "pline" p3 p9 p10 p11 "close" "")
  (command "copy" "l" "" p11 p6)
  (command "line" p5 p8 "")
  (command "array" "l" "" "r" ht1 "" 12)
  (command "line" p4 p7 "")
  (command "array" "l" "" "r" ht1 "" 12))
