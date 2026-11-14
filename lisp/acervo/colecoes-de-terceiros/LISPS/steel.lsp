; LOAD DIMENSION DATA FROM THE APPROPRIATE DATA FILE, (angles)
;
(defun LOAD-ANG ()
 (setq zz (open file "r"))
 (repeat index (read-line zz))
 (setq shapedat (read-line zz))
 (setq whatkind (substr shapedat 7 22))
 (setq vlg (atof (substr shapedat 46 6)))
 (setq thk (atof (substr shapedat 57 7)))
 (setq hlg (atof (substr shapedat 53 7)))
 (setq ak (atof (substr shapedat 64 7)))
 (setq weight (atof (substr shapedat 39 7)))
 (close zz)
 (print "The shape which you have selected is:" )
 (prin1 whatkind )
 (terpri)
 (prompt "Command:" ))
;
;
; LOAD DIMENSION DATA FROM THE APPROPRIATE DATA FILE, (I-beams & channels)
;
;
(defun LOAD-C&I ()
 (setq zz (open file "r"))
 (repeat index (read-line zz))
 (setq shapedat (read-line zz))
 (setq whatkind (substr shapedat 7 15))
 (setq wlg (atof (substr shapedat 22 6)))
 (setq wthk (atof (substr shapedat 28 6)))
 (setq flg (atof (substr shapedat 34 9)))
 (setq fthk (atof (substr shapedat 43 9)))
 (setq k (atof (substr shapedat 62 7)))
 (setq weight (atof (substr shapedat 69 7)))
 (close zz)
 (print "The shape which you have selected is:" )
 (prin1 whatkind )
 (terpri)
 (prompt "Command:" ))
;
;
; GENERATE A PLAN VIEW OF AN I BEAM
;
(defun BEAMPL ()
     (setq spt (getpoint "\nDigitize the lower left end:"))
     (setq ept (getpoint spt "\nEnter the lower right end:"))
     (setq ang (+ (angle spt ept) (/ pi 2)))
     (command "line" spt (polar spt ang flg) (polar ept ang flg)
        ept "close"
     )
     (command "copy" "l" ""
        (polar '(0.0 0.0) ang (- (/ flg 2) (/ wthk 2))) ""
     )
     (command "change" "l" ""  "layer" "steel-hdn")
     (command "copy" "l" "" (polar '(0.0 0.0) ang wthk) "")
)
;
; GENERATE A PLAN VIEW OF A CHANNEL
;
(defun CHANLPL ()
     (setq spt (getpoint "\nDigitize the lower left end:"))
     (setq ept (getpoint spt "\nEnter the lower right end:"))
     (setq ang (+ (angle spt ept) (/ pi 2)))
     (command "line" spt (polar spt ang flg) (polar ept ang flg) 
        ept "close"
     )
     (command "copy" "l" "" (polar '(0.0 0.0) ang (- flg wthk)) "")
     (command "change" "l" "" "layer" "steel-hdn")
)
;
; GENERATE A PLAN VIEW OF AN ANGLE
;
(defun ANGLPL ()
     (setq spt (getpoint "\nDigitize the lower left end:"))
     (setq ept (getpoint spt "\nEnter the lower right end:"))
     (setq ang (+ (angle spt ept) (/ pi 2)))
     (command "line" spt (polar spt ang hlg) (polar ept ang hlg)
       ept "close"
     )
     (command "copy" "l" "" (polar '(0.0 0.0) ang (- hlg thk)) "")
)
;
; GENERATE A PROFILE VIEW OF AN I BEAM
;
(defun BEAMPR ()
     (setq spt (getpoint "\nDigitize the lower left corner:"))
     (setq ept (getpoint spt "\nEnter the lower right corner:"))
     (setq ang (+ (angle spt ept) (/ pi 2)))
     (command "line" spt (polar spt ang wlg) (polar ept ang wlg)
        ept "close"
     )
     (command "copy" "l" ""
        (polar '(0.0 0.0) ang fthk) ""
     )
     (command "copy" "l" ""
        (polar '(0.0 0.0) ang (- wlg (* fthk 2))) ""
     )
)
;
; GENERATE A PROFILE VIEW OF A CHANNEL
;
(defun CHANLPR ()
     (setq spt (getpoint "\nDigitize the lower left corner:"))
     (setq ept (getpoint spt "\nEnter the lower right corner:"))
     (setq ang (+ (angle spt ept) (/ pi 2)))
     (command "line" spt (polar spt ang wlg)
        (polar ept ang wlg) ept "close"
     )
     (command "copy" "l" "" (polar '(0.0 0.0) ang fthk) "")
     (command "copy" "l" "" (polar '(0.0 0.0) ang 
		(- wlg (* fthk 2))) "")
)
;
; GENERATE A PROFILE VIEW OF AN ANGLE
;
(defun ANGLPR ()
     (setq spt (getpoint "\nDigitize the lower left corner:"))
     (setq ept (getpoint spt "\nEnter the lower right corner:"))
     (setq ang (+ (angle spt ept) (/ pi 2)))
     (command "line" spt (polar spt ang vlg) (polar ept ang vlg)
       ept "close"
     )
     (command "copy" "l" "" (polar '(0.0 0.0) ang thk) "")
)
;
; GENERATE A SECTION VIEW OF A W, M, OR HP SHAPE I-BEAM
;
(defun WBEAMS ()
     (setq spt (getpoint "\nDigitize the lower left corner:"))
     (setq xv (+ (/ flg 2) (/ wthk 2) (- k fthk)))
     (setq xv2 (- (/ flg 2) (/ wthk 2) (- k fthk)))
     (command "pline" spt (polar spt 0 flg)
     (polar spt (atan fthk flg) (sqrt (+ (* flg flg) (* fthk fthk))))
     (polar spt (atan fthk xv) (sqrt (+ (* xv xv) (* fthk fthk))))
     "a" "a" "-90" "r" (- k fthk) "135" "l" "l" (- wlg k k)
     "a" "a" "-90" "r" (- k fthk) 45 "l" "l" (- flg xv)
     (polar spt (atan wlg flg) (sqrt (+ (* wlg wlg) (* flg flg))))
     (polar spt 1.5708 wlg) (polar spt 1.5708 (- wlg fthk))
     (polar spt (atan (- wlg fthk) xv2)
   (sqrt (+ (expt (- wlg fthk) 2) (* xv2 xv2))))
     "a" "a" "-90" "r" (- k fthk) "315" "l" "l" (- wlg k k)
     "a" "a" "-90" "r" (- k fthk) "225" "l" "l"
     (polar spt (/ pi 2) fthk) "close"
     )
)

;
; GENERATE A SECTION VIEW OF AN S SHAPE I-BEAM
;
(defun SBEAMS ()
     (setq spt (getpoint "\nDigitize the lower left corner:"))
     (setq xv (- (/ flg 2) k (/ fthk 2)))
     (command "pline" spt (polar spt 0 flg) "a" "a" "90" "r"
     fthk "135" "l" "l" xv "a" "a" "-90" "r" (- k fthk) "135" "l"
     "l" (- wlg k k) "a" "a" "-90" "r" (- k fthk) "45" "l" "l" xv
     "a" "a" "90" "r" fthk "45" "l" (polar spt (/ pi 2) wlg)
     "a" "a" "90" "r" fthk "315" "l" "l" xv "a" "a" "-90" "r"
     (- k fthk) "315" "l" "l" (- wlg k k) "a" "a" "-90" "r"
     (- k fthk) "225" "l" "l" xv "a" "a" "90" "r" fthk "225" ""
     )
)
;
; GENERATE A SECTION VIEW OF A CHANNEL
;
(defun CHANLS ()
     (setq spt (getpoint "\nDigitize the lower left corner: "))
     (command "pline" spt (polar spt 0 flg) "a" "a" "90" "r" fthk
     "135" "l" "l" (- flg fthk k) "a" "a" "-90" "r" (- k fthk)
     "135" "l" "l" (- wlg k k) "a" "a" "-90" "r" (- k fthk)
     "45" "l" "l" (- flg fthk k) "a" "a" "90" "r" fthk "45" "l"
     (polar spt (/ pi 2) wlg) "close"
     )
)
;
; GENERATE A SECTION VIEW OF AN ANGLE
;
(defun ANGLS ()
     (setq spt (getpoint "\nDigitize the lower left corner:"))
     (command "pline" spt (polar spt 0 hlg) "a" "a" "90" "r" thk
     "135" "l" "l" (- hlg thk ak) "a" "a" "-90" "r" (- ak thk) "135"
     "l" "l" (- vlg thk ak) "a" "a" "90" "r" thk "135" "l" "close"
     )
)
;
;
;
; DRAW A PLAN VIEW OF A STRUCTURAL SHAPE
;
(defun C:PLAN ()
	(if (or (= IND "wbeam") (= IND "sbeam")) (beampl)
	(if (= IND "chanl") (chanlpl) (anglpl))
	)
)
;
; DRAW A PROFILE VIEW OF A STRUCTURAL SHAPE
;
(defun C:PROFILE ()
	(if (or (= IND "wbeam") (= IND "sbeam")) (beampr)
	(if (= IND "chanl") (chanlpr) (anglpr))
	)
)
;
; DRAW A SECTION VIEW OF A STRUCTURAL SHAPE
;
(defun C:SECTION ()
	(if (= IND "wbeam") (wbeams)
	(if (= IND "sbeam") (sbeams)
	(if (= IND "chanl") (chanls) (angls)))
	))














