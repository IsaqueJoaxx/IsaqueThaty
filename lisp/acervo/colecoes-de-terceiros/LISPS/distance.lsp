; The following function performs the same function as 
; the "DISTANCE" command in AutoCAD. It is provided as 
; an example of the distance function and function nesting.
; Programming AutoCAD Example # 2-54
;
(defun c:getdist ()
   (prompt "distance = ")
   (distance (getpoint "First point ") (getpoint "Second point "))
)
