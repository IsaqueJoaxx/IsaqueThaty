(defun c:BKC ()
  (if (null brkscale)
    (setq brkscale (getint "\nEnter scale of your drawing <1>: ")))
  (if (null brkscale) (setq brkscale 1))
  (setq p1 (getpoint "\nSelect circle: "))
  (setq p1 (osnap p1 "cen"))
  (setq p2 (osnap p1 "end"))
  (if (= (read(rtos(distance p1 p2) 2 4)) 0.0)
   (setq p2 (osnap p1 "mid")))
  (setq a1 (angle p1 p2))
  (setq p3 (polar p1 a1 (* brkscale 0.07813)))
  (setq p4 (polar p1 a1 (* brkscale 0.1719)))
  (command "break" p4 p3)
  (setq p3 (polar p1 (+ a1 pi) (* brkscale 0.07813)))
  (setq p4 (polar p1 (+ a1 pi) (* brkscale 0.1719)))
  (command "break" p4 p3)
  (setq p3 (polar p1 (+ (/ pi 2) a1) (* brkscale 0.07813)))
  (setq p4 (polar p1 (+ (/ pi 2) a1) (* brkscale 0.1719)))
  (command "break" p4 p3)
  (setq p3 (polar p1 (+ (/ (* pi 3) 2) a1) (* brkscale 0.07813)))
  (setq p4 (polar p1 (+ (/ (* pi 3) 2) a1)(* brkscale 0.1719)))
  (command "break" p4 p3)
)
;
;
;  C:BKC creates a center mark or "x" in a circle where
;the centerlines are already drawn through the circle.  
;BKC breaks the intersecting lines at appropriate intervals, to
;create the center mark.  BKC automatically scales the dashes
;to the user specified scale.  Use of BKC instead of using the
;"center" command in the "dimension menu" allows the centerlines
;for circles and arcs to be drawn in continuous lines,
;possibly using "osnap center" to locate the circle center
;then drawing the line.
;
;  Use of BKC along with the above BK command allows the
;draftsman to control the center marks exactly.  BKC works
;best when zommed in so that circles being selected fill
;at least 1/16 of the screen size.  Reserve using BKC untill
;the end of drawing operations so that the extra drawing
;elements created do not constantly lengthen zoom
;and regen times.
