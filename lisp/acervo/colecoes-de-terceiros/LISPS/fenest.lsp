;FENEST.LSP 
;(c) 1988 by Looking Glass Microproducts
; ------------------- Degrees to Radians
(defun radians (degrees)
   (/ (* degrees pi) 180.0))
; ----------------- Defaults & Constants
(setq
  L      (radians 32) ;latitude
  AST    "0815"     ;Apparent Solar Time
  Date   "1021"     ;Date
  two_pi (* 2 pi))
; ----------------------- SHADOW Command
(defun c:shadow ()
   (fenest 'direct)
   (princ))
; ------------------------ LIGHT Command
(defun c:light ()
   (fenest 'reflect)
   (princ))
; ------------ Fenestration Calculations
(defun fenest (mode /  
; mode is 'direct or 'reflect
  n i p1 p2 p3 p4 ray s sp1 sp2 sp3 sp4
  ss sun z)
;
 (setvar "cmdecho" 0)
 (setq
   sun (getsun)
   ss  (ssget))
 (if ss
   (progn
   (setq
     n (sslength ss)
     i 0
     z (getvar "elevation"))
   (while (< i n)
     (setq ent (entget (ssname ss i)))
   (if (= (item 0 ent) "3DFACE")
     (progn
     (setq
       p1 (item 10 ent)
       p2 (item 11 ent)
       p3 (item 12 ent)
       p4 (item 13 ent)
       s  (surface p1 p2 p3) 
; normal to surface
       ray (if (eq mode 'reflect)
; reflected sun
             (vdif (sprod (* 2 (dotprod 
               s sun)) s) sun)
; direct sun
             (sprod -1 sun)))
     (if (minusp (caddr ray))
; the beam is going down
       (progn
       (setq
         sp1 (zpoint p1 ray z)
         sp2 (zpoint p2 ray z)
         sp3 (zpoint p3 ray z)
         sp4 (zpoint p4 ray z))
       (command "pline" sp1 sp2 sp3 sp4 
         "c")))))
   (setq i (1+ i))))))
; Compute intersection of direction 
; vector 'V' from point 'P' with a plane
; at elevation z
(defun zpoint (P V z / r)
  (setq r (/ (- z (caddr P)) (caddr V)))
  (vsum (sprod r V) P))
; Determine Solar Azimuth & Altitude 
; from Latitude, Date, and Time
(defun getsun ()
; --------------------------- Get Inputs
  (setq
    L (xgetangle 
      "\nLatitude (+=n, -=s)" L)
    Date (xgetstring 
      "\nDate (mmdd)" Date)
    AST (xgetstring 
     "\nApparent Solar Time (hhmm)" AST)
)
; ------------ Compute Solar Declination
   (setq del (* (radians 23.45) (sin (* 
     two_pi (/ (+ (dayofyear Date) 284) 
     365.0)))))
; ------------------- Compute Hour Angle
   (setq H (rem (+ (* (+ (* 60 (atoi 
     (substr AST 1 2)))(atoi (substr AST
     3 2))) (/ pi 720)) pi) two_pi))
; --- Compute Solar Azimuth and Altitude
   (setq 
      sin_beta (+ (* (cos L)(cos del)
                 (cos H)) (* (sin L) 
                 (sin del)))
      beta (arcsin sin_beta)
      cos_phi (/(- (* (sin beta) (sin 
                L)) (sin del))(* (cos 
                beta) (cos L)))
      phi (if (< H pi)
            (arccos cos_phi)
            (- (arccos cos_phi))))
   (princ (strcat 
     "\nSolar Azimuth = " (sangtos phi)
     "  Altitude = " (sangtos beta)
     "\n"))
   (azal phi beta))
; --------- Convert Azimuth and Altitude
;           to Direction Vector
(defun azal (phi beta)
   (list
     (* (cos beta) (- (sin phi)))
     (* (cos beta) (- (cos phi)))
     (sin beta)))
; ------------------ Compute Julian Date
(defun dayofyear (mmdd / month day)
  (setq
    Month   (atoi (substr mmdd 1 2))
    Day     (atoi (substr mmdd 3 2)))
  (+
    (nth Month '(0 0 31 59 90 120 151 
      181 212 243 273 304 334))
     Day))
; ------------------------------- Arcsin
(defun arcsin (x)
  (atan x (sqrt (- 1.0 (* x x)))))
; ------------------------------- Arccos
(defun arccos (x)
  (atan (sqrt (- 1.0 (* x x))) x))
; ------ Return nth assoc'd item in list
(defun item (n alist)
  (cdr (assoc n alist)))
; ------------------- Extended Get Angle
(defun xgetangle (prmpt default)
  (cond 
    ((getangle (strcat prmpt  
      " <" (sangtos default) ">: ")))
    (default)))
; --------------- Signed Angle to String
(defun sangtos (a)
  (if (< a 0)
    (setq a (+ a two_pi)))
  (if (<= a pi)
    (angtos a) 
    (strcat "-" (angtos (- two_pi a)))))
; ----------------- Extended Get Integer
(defun xgetint (prmpt default)
  (cond
    ((getint (strcat prmpt  
      " <" (itoa default) ">: ")))
    (default)))
; ------------------ Extended Get String
(defun xgetstring (prmpt default / temp)
  (setq temp (getstring (strcat prmpt  
    " <" default ">: ")))
  (if (/= "" temp) temp default))
; --------------------- Vector Magnitude
(defun mag (v)
   (sqrt (apply '+ (mapcar '* v v))))
; -------------------------- Unit Vector
(defun u (v / vmag)
   (setq vmag (mag v))
   (mapcar '(lambda (x) (/ x vmag)) v))
; --------------------------- Vector Sum
(defun vsum (v1 v2)
  (mapcar '+ v1 v2))
; -------------------- Vector Difference
(defun vdif (v1 v2)
  (mapcar '- v1 v2))
; ----------------------- Scalar Product
(defun sprod (s v)
  (mapcar '(lambda (x) (* s x)) v))
; -------------------------- Dot Product
(defun dotprod (v1 v2)
  (apply '+ (mapcar '* v1 v2)))
; ------------------------ Cross Product
(defun xprod (v1 v2 / a1 b1 c1 a2 b2 c2)
  (setq
     a1 (car   v1)
     b1 (cadr  v1)
     c1 (caddr v1)
     a2 (car   v2)
     b2 (cadr  v2)
     c2 (caddr v2))
  (list (- (* b1 c2) (* b2 c1))
        (- (* c1 a2) (* c2 a1))
        (- (* a1 b2) (* a2 b1))))
; --------------- Unit Normal to Surface
(defun surface (v1 v2 v3)
  (u (mapcar '(lambda (x y z) (+ x y z))
    (xprod v1 v2)
    (xprod v2 v3)
    (xprod v3 v1))))
