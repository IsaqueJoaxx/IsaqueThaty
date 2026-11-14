;           ARCLDR.LSP
;(C) 1987 by Looking Glass Microproducts
; ARC LEADER COMMAND
; - - - - - - - - - - - - - - ERROR HALT
(defun *error* (s)
 (princ (strcat "\n" s))
 (setvar "blipmode" bm)
 (princ))
; - - - - - - - - - - - - - - SAME POINT
(defun samepoint (p0 p1)
 (<= (distance p0 p1) 1.0E-6))
; - - - - - - - - - - - - - - - DELTA XY
(defun dxy (p0 dx dy)
 (list (+ (car p0)  dx)
       (+ (cadr p0) dy)))
; - - - - - - QUADRANT OF STARTING ANGLE
(defun quadrant (a)
 (fix (/ a (* 0.5 pi))))
; - - - - - - - - - - - - - - - - -  TAN
(defun tan (a)
 (/ (sin a) (cos a)))
; - - - - - - - - - - - - - -  MAIN BODY
(defun c:arcldr ( /
  bm version twopi halfpi asiz tsiz
  p0 p1 ename ent cen rad sa ea eam
  sp ep ccw len tp fp0 fp1 start
  lastp1 lastfp1 fr fd ho fe fc fc2
  left om langle langle0)
 (graphscr)
 (setq
  ; - - - - - - - - LINE ANGLE FOR BENDS
  bangle  (/ (* 65.0 pi) 180.0)
  bm      (getvar "blipmode")
  om      (getvar "orthomode")
  ; - - - - - - - - - - AUTOLISP VERSION
  version (atof (substr (ver) 18))
  twopi  (* 2.0 pi)
  halfpi (* 0.5 pi)
  lastp1 nil
  ; - - - - - - - - - - - - - ARROW SIZE
  asiz (if (>= version 2.6)
         (* (getvar "dimscale")
            (getvar "dimasz"))
         (* (getvar "ltscale") 0.18))
  ; - - - - - - - - - - - - -  TEXT SIZE
  tsiz (if (>= version 2.6)
         (* (getvar "dimscale")
            (getvar "dimtxt") )
         (* (getvar "ltscale") 0.18)))
 (setvar "cmdecho"  0)
 (setvar "blipmode" bm)
 (setq
   ; - - - - - - - START POINT OF LEADER
   start (getpoint "\nFrom point: "))
 (if start
 ; - - - - - -CONTINUE IF POINT SELECTED
  (progn
  (setq p0 start)
  (setvar "orthomode" 1) ;      ORTHO ON
  (while (null (setq langle
      (getangle p0 "\nDirection: "))))
  (setq langle
    (if (<= halfpi langle (* 1.5 pi))
         pi 0.0) langle0 langle)
  ; - - - - - - - - - - - - -  MAIN LOOP
  (while p0
   (setvar "blipmode" bm)
   (setvar "orthomode" 0)
   (cond ((>= version 9.0)
     (command "pline" p0 "width" 0 0
      "arc" "direction" (angtos langle))
     (prompt "\nTo point: ")
     (command pause "" ;    ENDPOINT
      "explode" "l"))
   (T (while (not (setq p1
         (getpoint "\nTo point: " p0))))
      (command "arc" p0 "e" p1 "d"
        (angtos langle))))
  (setvar "blipmode" 0)
; - - - - - - - - - - -  DISSECT THE ARC
  (setq
   ename (entlast)
   ent   (entget ename)
   cen   (cdr (assoc 10 ent))
   rad   (cdr (assoc 40 ent))
   sa    (cdr (assoc 50 ent))
   ea    (cdr (assoc 51 ent))
   eam   (if (< ea sa) (+ ea twopi) ea)
   sp    (polar cen sa rad)
   ep    (polar cen ea rad)
   ccw   (samepoint sp p0)
   len   (* rad (abs (- eam sa))))
  (if ccw
   (setq tp
    (polar cen (- ea (/ asiz rad)) rad)
     p1 ep a1 ea)
   (setq tp
    (polar cen (+ sa (/ asiz rad)) rad)
     p1 sp a1 sa))
; - - - - - - - - -  ADD ANOTHER SEGMENT
  (setvar "orthomode" 1)
  (setvar "blipmode" bm)
  (setq
   p0 nil
   ho (getdist p1 "\nOffset distance:"))
  (setvar "orthomode" 0)
  (setvar "blipmode" 0)
  (if ho
   (progn
    (setq
      quad (quadrant a1)
      fr   (/ ho (+ 1.0 (abs (sin a1))
           (/ 2.0 (cos bangle))))
      ls   (* 2.0 fr (tan bangle))
      fc   (polar p1 a1 fr))
    (cond ((= quad 0) (setq
          fe  (polar fc (- halfpi
              bangle) fr)
          fc2 (polar fc  halfpi (* fr
              (/ 2.0 (cos bangle))))))
     ((= quad 1) (setq
          fe  (polar fc (+ bangle
              halfpi) fr)
          fc2 (polar fc halfpi (* fr
              (/ 2.0 (cos bangle))))))
     ((= quad 2) (setq
          fe  (polar fc (- (+ halfpi
              bangle)) fr)
          fc2 (polar fc (- halfpi) (*
            fr (/ 2.0 (cos bangle))))))
     ((= quad 3) (setq
          fe  (polar fc (- bangle
              halfpi) fr)
          fc2 (polar fc (- halfpi) (*
            fr (/ 2.0 (cos bangle)))))))
   (while (null
     (setq langle (getangle (polar
          fc2 (angle fc fc2) fr)
          "\nDirection: "))))
     (setq p0 (polar fc2
          (if ccw (- langle halfpi)
                  (+ langle halfpi))
          fr))
    (command "arc" "" fe
             "line" "" ls ""
             "arc" "" p0))))
; - - - - - - - - - - DRAW THE ARROWHEAD
  (if (>= len (* 2 asiz))
   (if ccw
    (command "pline" tp
      "width" (/ asiz 3.0) 0 "arc"
      "radius" rad p1 "")
    (command "pline" p1
      "width" 0 (/ asiz 3.0) "arc"
      "radius" rad tp "width" 0 0 "")))
; - - - - - - - - -  ADD THE LEADER TEXT
  (prompt "\nText: ")
  (if (<= 1 (quadrant langle0) 2)
; - - - - - - -  LEADER TO LEFT OF START
    (command "dtext"
      (dxy start tsiz (* -0.5 tsiz))
      tsiz 0)
    (command "dtext" "right"
      (dxy start (- tsiz) (* -0.5 tsiz))
      tsiz 0))))
 (setvar "blipmode" bm)
 (setvar "orthomode" om)
 (princ)
)
(princ "\nArcLdr.LSP")
(princ "\n(C) 1987 ")
(princ "by Looking Glass Microproducts")
(c:arcldr)

