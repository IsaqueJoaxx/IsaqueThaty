;The dtr function converts degrees to radians
;The rtd function converts radians to degrees
(defun dtr (a) (* pi (/ a 180.0)))
(defun rtd (a) (/ (x a 180.0) pi));

;   This lisp routine breaks lines where they intersect two
;   other lines and draws a hidden line between the other two
;   lines.

(defun c:hbrk (/ osn a b c d e f g h lne j )
   (setq osn (getvar "osmode"))
   (prompt "\n Written by Gary A. Moretz, Montverde, Fla ") (terpri)
   (setq c (getdist "Enter Offset  ")) (terpri)
   (setq j (getreal "Number of Hidden Lines  ")) (terpri)
   (setq h "t")
     (while h
   (setvar "osmode" 32)
   (setq lne (getpoint "pick line to hide  ")) (terpri)
   (setq a (getpoint "pick first intersection  ")) (terpri)
   (setq b (getpoint "pick second intersection  ")) (terpri)
   (setq d (angle a b))
   (setq e (polar a d c))
   (setq f (angle b a))
   (setq g (polar b f c))
   (command "break" lne "f" a b "")
   (command "line" e g "")
   (command "change" "l" "" "prop" "lt" "hidden" "")
   (setq j (- j 1))
   (if(= j 0)(setq h nil))
   (setvar "osmode" osn)
 )
)
