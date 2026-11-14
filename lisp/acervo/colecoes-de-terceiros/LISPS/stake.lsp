;Radial stake out program for surveyors
;You must first create a block drawing with attributes that
;will be inserted by this program.
;Name the block drawing "SN", place a point @ 0,0
;create the following attributes:
;     number
;     bearing
;     distance
;     angle
;begin at 1,1 with text height 1
;attributes bearing, distance and angle may be made invisible and
;extracted to a file or to the printer if desired.
;
;This program is being uploaded with the hope that the shareware
;concept is not totally dead. If you like it, and can put it to
;good use, send $15 to:
;    KBW Engineering, Inc.
;    P.O. Box 1444
;    Gig Harbor, WA 98335
;In return we will send a demo disk of other low cost civil and 
;survey AutoLISP programs we have developed.
(vmon)
(setvar "cmdecho" 0)
(defun input ()
(setq IP (getpoint "\nInstrument at what point?")
      BS (getpoint "\nBacksight what point?")
      t1 "Text Height: <default = "
      t2 " >: "
      t3 (getvar "textsize")
)
(terpri)
(setq HT (getreal (strcat t1 (rtos t3 2 2) t2)))
(if (= HT nil)
(setq HT t3)
)
(setvar "textsize" ht)
(setq N (getreal "\nBeginning point number:"))
)
(defun BSBRNG ()
(setq O (- (car IP) (car BS))
      T (- (cadr IP) (cadr BS))
      AZMTH (/ (* (atan (/ O T)) 180.0) pi)
      A (abs AZMTH)
      D (fix A)
      M (* 60 (- A D))
      S (* 60 (- M (fix M)))
      M (FIX M)
)
(if (= "60" (rtos S 2 0)) (setq M (+ 1 M)))
(if (= "60" (rtos S 2 0)) (setq S 0))
(if (= "60" (rtos M 2 0)) (setq D (+ 1 D)))
(if (= "60" (rtos M 2 0)) (setq M 0))
(if (< (cadr BS) (cadr IP)) (setq G "S") (setq G "N"))
(if (< (car IP) ( car BS)) (setq H "E") (setq H "W"))
(setq bsbng (strcat G " "
  (if (< D 10) (strcat "0" (rtos D 2 0)) (rtos D 2 0)) "%%D"
  (if (< M 10) (strcat "0" (rtos M 2 0)) (rtos M 2 0)) "'"
  (if (< S 10) (strcat "0" (rtos S 2 0)) (rtos S 2 0)) "''" " " H
))
(setq bsa A)
(if (and (= G "S") (= H "E")) (setq bsa (- 180.0 bsa)))
(if (and (= G "S") (= H "W")) (setq bsa (+ 180.0 bsa)))
(if (and (= G "N") (= H "W")) (setq bsa (- 360.0 bsa)))
)
(defun FSBRNG ()
(setq O (- (car IP) (car P1))
      T (- (cadr IP) (cadr P1))
      AZMTH (/ (* (atan (/ O T)) 180.0) pi)
      A (abs AZMTH)
      D (fix A)
      M (* 60 (- A D))
      S (* 60 (- M (fix M)))
      M (FIX M)
)
(if (= "60" (rtos S 2 0)) (setq M (+ 1 M)))
(if (= "60" (rtos S 2 0)) (setq S 0))
(if (= "60" (rtos M 2 0)) (setq D (+ 1 D)))
(if (= "60" (rtos M 2 0)) (setq M 0))
(if (< (cadr P1) (cadr IP)) (setq G "S") (setq G "N"))
(if (< (car IP) ( car P1)) (setq H "E") (setq H "W"))
(setq fsbng (strcat G " "
  (if (< D 10) (strcat "0" (rtos D 2 0)) (rtos D 2 0)) "%%D"
  (if (< M 10) (strcat "0" (rtos M 2 0)) (rtos M 2 0)) "'"
  (if (< S 10) (strcat "0" (rtos S 2 0)) (rtos S 2 0)) "''" " " H
))
(setq fsa A)
(if (and (= G "S") (= H "E")) (setq fsa (- 180.0 fsa)))
(if (and (= G "S") (= H "W")) (setq fsa (+ 180.0 fsa)))
(if (and (= G "N") (= H "W")) (setq fsa (- 360.0 fsa)))
)
(defun ANGRT ()
(setq A (- fsa bsa))
(if (< A 0) (setq A (+ 360 A)))
(setq D (fix A)
      M (* 60 (- A D))
      S (* 60 (- M (fix M)))
      M (FIX M)
)
(if (= "60" (rtos S 2 0)) (setq M (+ 1 M)))
(if (= "60" (rtos S 2 0)) (setq S 0))
(if (= "60" (rtos M 2 0)) (setq D (+ 1 D)))
(if (= "60" (rtos M 2 0)) (setq M 0))
(setq arbng (strcat "AR "
  (if (< D 10) (strcat "0" (rtos D 2 0)) (rtos D 2 0)) "%%D"
  (if (< M 10) (strcat "0" (rtos M 2 0)) (rtos M 2 0)) "'"
  (if (< S 10) (strcat "0" (rtos S 2 0)) (rtos S 2 0)) "''"
))
)
(defun C:STAKE ()
(input)
(setq another "Y")
(while (= another "Y")
(setq P1 (getpoint "\nOSNAP point:")
      DS (distance P1 IP))
(BSBRNG)
(FSBRNG)
(ANGRT)
(command "insert" "sn" P1 ht "" "0" (rtos N 2 0)
  fsbng (rtos DS 2 2) arbng)
(setq N (+ 1 N))
(setq j1 "Next point <")
(setq j2 ">:")
(print (strcat j1 (rtos N 2 0) j2))
(setq another (getstring "\nContinue <Y>es, <N>o, default=<Y>:"))
(if (= another "")
(setq another "Y")
))
)
