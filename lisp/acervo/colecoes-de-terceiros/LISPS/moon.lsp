
;       Draw the Moon

;       By Kelvin R. Throop  --  November 10, 1985

;       MOON  --  Draw the moon at the current time

(defun c:moon ()
        (setq cdate (getvar "date"))
        (princ "\nCalculating.  Please stand by...")
        (setq ph (phase cdate))
        (setq aom (* ph (- ptime lptime)))
        (princ "  Age of moon is ")
        (princ (rtos (float (fix aom)) 2 0))
        (princ " days ")
        (princ (rtos (* 24 (- aom (fix aom))) 2 0))
        (princ " hours.")
        ; Form unique block name, BNAME, from time of execution
        (setq bname (rtos (* 100000.0 (- cdate (fix cdate))) 2 0))
        (if (or (= ph 0.0) (= ph 1.0))
           (progn
              (princ "\nMoon is new!\n")        ; New moon -- nothing to draw
              nil
           )
           (progn
              (setq cp (getpoint "\nCentre point: "))
              (setq size (getdist "\nSize: " cp))
              (setq cech (getvar "cmdecho"))
              (setq blpm (getvar "blipmode"))
              (setvar "cmdecho" 0)
              (setvar "blipmode" 0)
              (command "arc" "c" cp)
              (setq p1 (polar cp (* pi 0.5) size))
              (setq p2 (polar cp (* pi 1.5) size))
              (if (< ph 0.5)
                 (command p2 p1)
                 (command p1 p2)
              )
              (command "block" bname cp "l" "")
              (command "insert" bname cp 1 1 0)
              (setq xscale (cos (* 2.0 pi ph)))
              (if (< (abs xscale) 0.0000001)
                 (command "line" p1 p2 "")      ; Half moon -- draw line
                 (command "insert" bname cp xscale 1 0)
              )
              (setvar "cmdecho" cech)
              (setvar "blipmode" blpm)
           )
        )
)

;       PHASE  --  Return phase of the moon as a real value:

;             0.00 = New
;             0.25 = First quarter
;             0.50 = Full
;             0.75 = Last quarter

;       The argument is the time for which the phase is requested,
;       expressed as a Julian date and fraction.  Results are accurate
;       to about 2 minutes.

(defun phase (pdate)
        (setq y (car (jyear pdate)))
        (setq tzone 8)
        (setq r1 (/ pi 180))
        (setq u 0)
        (setq k0 (fix (* (- y 1900.0) 12.3685)))
        (setq t (/ (- y 1899.5) 100.0))
        (setq t3 (* (setq t2 (* t t)) t))
        (setq j0 (+ 2415020.0 (* 29 k0)))
        (setq f0 (- (* 0.0001178 t2) (* 1.55E-7 t3)))
        (setq f0 (+ f0 0.75933 (* 0.53058868 k0)))
        (setq f0 (- f0 (* 8.370001E-4 t) (* 0.000335 t2)))
        (setq m0 (* k0 0.08084821133))
        (setq m0 (+ (* 360.0 (- m0 (fix m0))) 359.2242))
        (setq m0 (- m0 (* 0.0000333 t2)))
        (setq m0 (- m0 (* 3.47E-6 t3)))
        (setq m1 (* k0 0.07171366128))
        (setq m1 (+ (* 360.0 (- m1 (fix m1))) 306.0253
           (* 0.0107306 t2) (* 1.236E-5 t3)))
        (setq b1 (* k0 0.08519585128))
        (setq b1 (+ (* 360.0 (- b1 (fix b1))) 21.2964))
        (setq b1 (- b1 (* 0.0016528 t2) (* 2.39E-6 t3)))
        (setq k9 0.0)
        (setq lptime 0.0)
        (while (< k9 29)
           (setq j (+ j0 (* 14.0 k9)))
           (setq f (+ f0 (* 0.765294 k9)))
           (setq k (/ k9 2.0))
           (setq m5 (* (+ m0 (* k 29.10535608)) r1))
           (setq m6 (* (+ m1 (* k 385.81691806)) r1))
           (setq b6 (* (+ b1 (* k 390.67050646)) r1))
           (setq f (+ f (* -0.4068 (sin m6))
                        (* (- 0.1734 (* 0.000393 t)) (sin m5))
                        (* 0.0161 (sin (* 2 m6)))
                        (* 0.0104 (sin (* 2 b6)))
                        (* -0.0074 (sin (- m5 m6)))
                        (* -0.0051 (sin (+ m5 m6)))
                        (* 0.0021 (sin (* 2 m5)))
                        (* 0.001 (sin (- (* 2 b6) m6)))
                   )
           )
           (setq j (+ j (fix f)))
           (setq f (- f (fix f)))
           (setq f (+ f (/ tzone 24.0)))
           (if (>= f 1) (progn
              (setq f (1- f))
              (setq j (1+ j)))
           )
           (if (< f 0) (progn
              (setq f (1+ f))
              (setq j (1- j)))
           )
           (setq lptime ptime)
           (setq ptime (+ j f))
           (if (and (>= cdate lptime) (< cdate ptime))
              (setq k9 1000)
           )
           (setq k9 (+ k9 2))
        )
        (setq ph (/ (- cdate lptime) (- ptime lptime)))
)

;       JYEAR  --  Convert Julian date to a list containing
;                  (year month day).

(defun jyear (td)
        (setq j (fix td))
        (setq j (- j 1721119.0))
        (setq y (fix (/ (1- (* 4 j)) 146097.0)))
        (setq j (- (* j 4.0) 1.0 (* 146097.0 y)))
        (setq d (fix (/ j 4.0)))
        (setq j (fix (/ (+ (* 4.0 d) 3.0) 1461.0)))
        (setq d (- (+ (* 4.0 d) 3.0) (* 1461.0 j)))
        (setq d (fix (/ (+ d 4.0) 4.0)))
        (setq m (fix (/ (- (* 5.0 d) 3) 153.0)))
        (setq d (- (* 5.0 d) 3.0 (* 153.0 m)))
        (setq d (fix (/ (+ d 5.0) 5.0)))
        (setq y (+ (* 100.0 y) j))
        (if (< m 10.0)
           (setq m (+ m 3))
           (progn
              (setq m (- m 9))
              (setq y (1+ y))
           )
        )
        (list y m d)
)
