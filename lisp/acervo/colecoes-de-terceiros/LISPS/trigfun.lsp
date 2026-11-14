; The following trig functions supplement the three provided
; in AutoLISP.  To use these functions, either add them to your
; LISP routine, or add them to your ACAD.LSP file. The following
; example shows how to use them in your routines:
;  (setq A (ASIN 0.5))
; This will set variable A 0.523598 which is in Radians.
;  (setq B (RTD A))
; This, in turn, will set variable B to 30.000000 which is in
; Degrees.
; As you can see, these functions are used just like AutoLISP's
; SIN, COS, and ATAN functions.
;
;
;
(defun ACOS (X) ;Inverse cosine
  (- (/ pi 2.0) (atan (/ X (sqrt (- 1.0 (* X X))))))
)
(defun ASIN (X) ; Inverse sine
  (atan (/ X (sqrt (- 1.0 (* X X)))))
)
(defun ACSC (X) ; Inverse cosecant
  (+ (atan (/ 1.0 (sqrt (- (* X X) 1.0)))) (* (/ pi 2.0)
    (- (if (< X 0) (- 1.0) (+ 1.0)) 1.0)))
)
(defun ASEC (X) ; Inverse secant
  (+ (atan (sqrt (- (* X X) 1.0))) (* (/ pi 2.0)
    (- (if (< X 0) (- 1.0) (+ 1.0)) 1.0)))
)
(defun ACOT (X) ; Inverse cotangent
  (- (/ pi 2.0) (atan X))
)
(defun SEC (X) ; Secant
  (/ 1.0 (cos X))
)
(defun CSC (X) ; Cosecant
  (/ 1.0 (sin X))
)
(defun DTR (X) ; Degree to Radian conversion
  (/ (* X pi) 180.0)
)
(defun RTD (X) ; Radian to Degree conversion
  (/ (* X 180.0) pi)
)
(defun TAN (X) ; Tangent
  (/ (sin X) (cos X))
)
(defun COT (X) ; Cotangent
  (/ (cos X) (sin X))
)
