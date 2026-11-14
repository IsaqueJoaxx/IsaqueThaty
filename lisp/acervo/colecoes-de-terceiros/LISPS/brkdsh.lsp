; BRKDSH.LSP

; By Jim Nakazawa
; (415) 768-1234

; This is to break a line between 2 intersecting lines and break that line
; into the specified number of dashes & breaks (better to enter an ODD number)
; This program was written for those occaisions when you need to show a 
; broken dashed line but defining a new linetype
; is too much trouble and changing the ltscale doesn't give the right results.

(defun C:BRKDSH (/ os p1 p2 p3 n a d p4 c)
  (setq os (getvar "osmode"))
  (setvar "osmode" 512);osnap set to nearest
  (setq p1 (getpoint "Pick Line to be Dashed: ")) (terpri)
  (setvar "osmode" 32);osnap set to intersection
  (setq p2 (getpoint "Pick First Intersection: ")) (terpri)
  (setq p3 (getpoint "Pick 2nd Intersection: ")) (terpri)
  (setq n (getint "Enter Number of Dashes Desired: "))
  (setq a (angle p2 p3))
  (setq d (distance p2 p3))
  (setq p4 (polar p2 a (/ d n)))
  (command "break" p1 "f" p2 p4)
  (setq c 2)
  (repeat (fix (/ (- n 1) 2)); (couldn't get to work using "(while"  )
        (setq p4 (polar p2 a (* c (/ d n))))
        (command "break" p4 (polar p4 a (/ d n)))
        (setq c (+ c 2))
  )
  (setvar "osmode" os);returns osnap to original setting
  (princ);to exit quietly
)

