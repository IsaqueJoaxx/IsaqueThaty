; The Following function uses the Scale1 function that requires 
; 2 parameters <point1> a lists in (x y) point format and <scale>
; a real number to use as a scale factor. 
; It will return a Point that is Point1 scaled in both 
; the X and Y axis by the value Scale.           
; This is provided as an example of scaling and point minipulation.
; Programming AutoCAD Example # 2-12
;
(defun scale1 (Point1 Scale)
   (list (* (car Point1) Scale)
         (* (cadr Point1) Scale)))
)     
(defun C:PointScale ()
   (setq Scale (getreal "Enter Scale Factor "))
   (setq Point1 (getpoint "Enter Point To Be Scaled "))
   (setq point1 (scale1 point1 scale))
   (command "Point" point1))