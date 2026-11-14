;This routine divides any size drawing at any scale into sixteen views
;and an overall view. If you have a version of ACAD earlier than 2.6
;then you should remove the " ' " from each of the "view" commands in the
;suggeted menu. It is not recommended to use this routine for small size
;sheets or large scale drawings. You need to place the accompanying "views.dwg"
;in your drawing directory, or along your set path.
;
;The inserted drawing shows the location of the views and is on layer "0".
;Remove this drawing or freeze layer "0" before plotting! I find that this is
;great for checking drawings over before plotting and for many drawing tasks
;if you have transparent commands available. 
;
;The purpose of this routine is to eliminate the need for zooming back to an 
;overall view every time you need to move around a drawing. You can zoom 
;within each view and pan over if need be, then go directly to another view 
;or to view "A" which is almost all of the drawing limits.
;
;I recommend the following menu be placed somewhere on your digitizer. I have
;a twelve button cursor and assign one button to this menu, transparent pan,
;zoom w, and zoom p on other buttons. If you have a 4 button cursor, I would
;recommend that you write a second button menu to switch to for these transpar-
;rent controls. Setup properly, this routine will save alot of time in drawing
;and editing.
;** VIEWSET 3
;[VIEW:]
;[1] VIEW R 1 $S=
;[2] VIEW R 2 $S=
;[3] VIEW R 3 $S=
;[4] VIEW R 4 $S=
;[5] VIEW R 5 $S=
;[6] VIEW R 6 $S=
;[7] VIEW R 7 $S=
;[8] VIEW R 8 $S=
;[9] VIEW R 9 $S=
;[10] VIEW R 10 $S=
;[11] VIEW R 11 $S=
;[12] VIEW R 12 $S=
;[13] VIEW R 13 $S=
;[14] VIEW R 14 $S=
;[15] VIEW R 15 $S=
;[16] VIEW R 16 $S=
;[A] VIEW R A $S=
[CANCEL]^C^C$S= 
;
;If anyone knows of a simple mathematical expression for the calculation of
;the view windows in lieu of what I have done, I would appreciate hearing
;from you. It's a matter of style, not of speed. Also, if you have any
;questions, please feel free to write. Craig Sharp, 73417,2320
(defun c:viewset (/ x a b c d e f g sm vs vsx vsy pt1 pt2)
   (SETVAR "CMDECHO" 0)
   (command "layer" "s" "0" "")
   (setq x (getvar "ltscale"))
   (setq sm (/ x 96)) ;scale multiplier
   (setq a (* sm 39)) ;limits and beginning points of views set
   (setq b (* sm 3334))
   (setq c (* sm 2151))
   (setq d (* sm 31.375))
   (setq e (* sm 19.5625))
   (setq f (* sm 3480))
   (setq g (* sm 2169.25))
   (setq vs (getvar "limmax")) ;scale for dwg insert.
   (setq vsx (/ (car vs) 3340.8)) 
   (setq vsy (/ (cadr vs) 2188.8)) 
   (command "view" "w" "A" (setq pt1 (list d e)) (setq pt2 (list f g)))
   (command "view" "w" "1" (setq pt1 (list a (+ (* 0.75 (- c a)) a)))
      (setq pt2 (list (+ (* 0.25 (- b a)) a) c)))
   (command "view" "w" "2" (setq pt1 (list (+ (* 0.25 (- b a)) a) 
      (+ (* 0.75 (- c a)) a))) (setq pt2 (list (+ (* 0.5 (- b a)) a) c)))
   (command "view" "w" "3" (setq pt1 (list (+ (* 0.5 (- b a)) a) 
      (+ (* 0.75 (- c a)) a))) (setq pt2 (list (+ (* 0.75 (- b a)) a) c)))
   (command "view" "w" "4" (setq pt1 (list (+ (* 0.75 (- b a)) a) 
      (+ (* 0.75 (- c a)) a))) (setq pt2 (list b c)))
   (command "view" "w" "5" (setq pt1 (list a (+ (* 0.5 (- c a)) a))) 
      (setq pt2 (list (+ (* 0.25 (- b a)) a) (+ (* 0.75 (- c a)) a))))
   (command "view" "w" "6" (setq pt1 (list (+ (* 0.25 (- b a)) a) 
      (+ (* 0.5 (- c a)) a))) (setq pt2 (list (+ (* 0.5 (- b a)) a) 
      (+ (* 0.75 (- c a)) a))))
   (command "view" "w" "7" (setq pt1 (list (+ (* 0.5 (- b a)) a) 
      (+ (* 0.5 (- c a)) a))) (setq pt2 (list (+ (* 0.75 (- b a)) a) 
      (+ (* 0.75 (- c a)) a))))
   (command "view" "w" "8" (setq pt1 (list (+ (* 0.75 (- b a)) a) 
      (+ (* 0.5 (- c a)) a))) (setq pt2 (list b (+ (* 0.75 (- c a)) a))))
   (command "view" "w" "9" (setq pt1 (list a (+ (* 0.25 (- c a)) a))) 
      (setq pt2 (list (+ (* 0.25 (- b a)) a) (+ (* 0.5 (- c a)) a))))
   (command "view" "w" "10" (setq pt1 (list (+ (* 0.25 (- b a)) a) 
      (+ (* 0.25 (- c a)) a))) (setq pt2 (list (+ (* 0.5 (- b a)) a) 
      (+ (* 0.5 (- c a)) a))))
   (command "view" "w" "11" (setq pt1 (list (+ (* 0.5 (- b a)) a) 
      (+ (* 0.25 (- c a)) a))) (setq pt2 (list (+ (* 0.75 (- b a)) a) 
      (+ (* 0.5 (- c a)) a))))
   (command "view" "w" "12" (setq pt1 (list (+ (* 0.75 (- b a)) a) 
      (+ (* 0.25 (- c a)) a))) (setq pt2 (list b (+ (* 0.5 (- c a)) a))))
   (command "view" "w" "13" (setq pt1 (list a a)) 
      (setq pt2 (list (+ (* 0.25 (- b a)) a) (+ (* 0.25 (- c a)) a))))
   (command "view" "w" "14" (setq pt1 (list (+ (* 0.25 (- b a)) a) a)) 
      (setq pt2 (list (+ (* 0.5 (- b a)) a) (+ (* 0.25 (- c a)) a))))
   (command "view" "w" "15" (setq pt1 (list (+ (* 0.5 (- b a)) a) a)) 
      (setq pt2 (list (+ (* 0.75 (- b a)) a) (+ (* 0.25 (- c a)) a))))
   (command "view" "w" "16" (setq pt1 (list (+ (* 0.75 (- b a)) a) a)) 
      (setq pt2 (list b (+ (* 0.25 (- c a)) a))))
   (command "insert" "view" (setq pt1 (list a a)) vsx vsy "")
   (SETVAR "CMDECHO" 1)
)