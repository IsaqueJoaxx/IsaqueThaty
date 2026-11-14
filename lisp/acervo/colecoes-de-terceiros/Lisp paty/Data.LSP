(defun ctoj (yr m d hh mm ss / y a b)
    (setq y yr)
 
    (if (<= m 2)
        (setq y (1- y)
              m (+ m 12)
        )
    )
 
    (if (or (< yr 1582)
            (and (= yr 1582) (or (< m 10) (and (= m 10) (< d 5)))))
        (setq b 0)                    ;  calendar
        (setq a (fix (/ y 100))       ;  calendar
              b (+ (- 2 a) (fix (/ a 4)))
        )
    )
 
    (+ (fix (+ (* 365.25 (+ y 4716)) (fix (* 30.6001 (+ m 1)))))
         d b -1524.0 (/ (+ (* (+ (* hh 60) mm) 60) ss) (* 24.0 60 60)))
)
;;;
;;;
(defun dtoj (cdate / c f yr ys m d)
    (setq ys (if (< cdate 0) -1 1)    ; Sign on year
          c (fix (abs cdate))         ; Date in unsigned digits
          yr (* (/ c 10000) ys)       ; Get year
          m (rem (/ c 100) 100)       ; Get month
          d (rem c 100)               ; Get day
          f (rem (abs cdate) 1)       ; Fraction of day
    )
    (ctoj yr m d (fix (+ (* f 100) 0.1))
                  (rem (fix (+ (* f 10000) 0.1)) 100)
                  (+ (rem (fix (+ (* f 1000000) 0.1)) 100)
                     (/ (rem (fix (+ (* f 1000000000) 0.1)) 1000) 1000.0)))
)
;;;
(defun jtoc (td / time a b c d e alpha z m hh mm)
    (setq time (* 86400.0 (- td (setq z (fix td)))))
    (if (< z 2299161)
        (setq a z)                                         ;  calendar
        (setq alpha (fix (/ (- z 1867216.25) 36524.25))    ;  calendar
              a (- (+ z 1 alpha) (fix (/ alpha 4)))
        )
    )
 
    (setq b (+ a 1524)
          c (fix (/ (- b 122.1) 365.25))
          d (fix (* 365.25 c))
          e (fix (/ (- b d) 30.6001))
    )
 
    (setq m (fix (if (< e 14) (1- e) (- e 13))))
 
    ; Determine the clock time from the fraction of a day
 
    (setq hh (fix (/ time 3600.0))
          time (- time (* hh 3600.0))
          mm (fix (/ time 60.0))
    )
 
    ; Return calendar date as list
 
    (list (fix (- c (if (> m 2) 4716 4715))) m (fix (- b d (fix (* 30.6001 e))))
          hh mm
          (- time (* mm 60))
    )
)

;;;----------------------------------------------------------------------------

(defun jtod (td / j)
    (setq j (jtoc td))
 
    ; Return calendar date in form YYYYMMDD.HHMMSSmsec
 
    (* (+ (* (abs (car j)) 10000)     ; year
          (* (cadr j) 100)            ; month
          (caddr j)                   ; day
          (/ (cadddr j) 100.0)        ; hour
          (/ (nth 4 j) 10000.0)       ; minute
          (/ (nth 5 j) 1000000.0)     ; seconds, milliseconds
       )
       (if (< (car j) 0) -1 1)        ; apply sign to year
    )
)
;;;

(defun jtow (j)
    (fix (rem (1+ j) 7))
)
;;;
;;; (C:DATE)  --  Implement DATE command to display date/time
;;;
(defun c:data (/ j c cdate m d y hh mm ss msec)
   (setq cdate (jtod (setq j (getvar "date")))
         c (fix cdate)
         y (/ c 10000)              ; Get year
         m (rem (/ c 100) 100)      ; Ger month
         d (rem c 100)              ; Get day
         c (- cdate (fix cdate))    ; Strip date from date/time
         c (fix (* c 1000000000))   ; Scale time to get HHMMSSmmm integer
         hh (/ c 10000000)          ; Get hours
         mm (rem (/ c 100000) 100)  ; Get minutes
         ss (rem (/ c 1000) 100)    ; Get seconds
         msec (rem c 1000)          ; Get milliseconds
   )
 
   ; Print the day of the week
    (setq a (strcat (princ (nth (jtow j) '("DOM"
                          "SEG"
                          "TER"
                          "QUA"
                          "QUI"
                          "SEX"
                          "SAB")))
   (princ " ")
 
   ; Print the date.  YYYY/M/D
 
   (princ (strcat (itoa D) "/" (itoa m) "/" (itoa Y)))
 
   ; Print the time.  HH:MM:SS.msec
 
   (princ (strcat " " (if (> hh 9) "" "0") (itoa hh)))
   (princ (strcat ":" (if (> mm 9) "" "0") (itoa mm)))
   (princ (strcat ":" (if (> ss 9) "" "0") (itoa ss)))
   (princ (cond ((> msec 99) "."  )
                ((> msec 9)  ".0" )
                (T           ".00")
          )
   )))
   ;;;(princ msec)
  (command "text" (getpoint) "2.5" "0" a "" "")
   (princ)
)
(princ "\nDATA")