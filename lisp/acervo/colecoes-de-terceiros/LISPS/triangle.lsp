; This function will define a new command "TRIANGLE" 
; It will prompt the user for 3 points and connect
; them with lines to make a triangle.
; Programming AutoCAD Example # 2-53
;
(defun c:triangle ( / pnt1 pnt2 pnt3)
   (setq pnt1 (getpoint "First point "))
   (setq pnt2 (getpoint "Second point "))
   (setq pnt3 (getpoint "Third point "))
   (command "line" pnt1 pnt2 pnt3 "c"))
