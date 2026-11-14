(DEFUN EVALROT ();Math model simulator for the four-bar link
	(IF (= ANGSEQ PI) (SETQ RT (- (- PI ANG) REFANG))
		(SETQ RT (- ANG REFANG)))
	(SETQ L5  (SQRT (- (+  (EXPT L1  2) (EXPT L4  2))
	(* 2 L1  L4  (COS RT )))))
;L5 is the diagonal across input link and stationary link.
;L5 is evaluated applying the Cosine rule.
	(IF (/= ANG NIL)
	(SETQ CANG1 (/ (- (+ (EXPT L5 2) (EXPT L2 2)) (EXPT L3 2))
		(* 2 L5 L2)))
	)
	(IF (> (ABS CANG1) 1.0) (ERROR))
	(SETQ SANG1 (SQRT (-  1 (EXPT CANG1 2))))
	(SETQ ANG1 (ATAN (/ (ABS SANG1) (ABS CANG1))))
		(IF (< CANG1 0.) (SETQ ANG1 (- 3.14 ANG1)))
	(IF (/= ANG NIL)
	(SETQ CANG2 (/ (- (+ (EXPT L5 2) (EXPT L3 2)) (EXPT L2 2))
		(* 2 L5 L3 )))
	)
	(IF (> (ABS CANG2) 1.0) (ERROR))
	(SETQ SANG2 (SQRT (- 1 (EXPT CANG2 2))))
	(SETQ ANG2 (ATAN (/ (ABS SANG2) (ABS CANG2))))
		(IF (< CANG2 0.) (SETQ ANG2 (- 3.14 ANG2)))
	(SETQ ANG2 (* ANG2 ICON))
	(IF (/= ANG NIL)
	(SETQ CANG3 (/ (- (+ (EXPT L4 2) (EXPT L5 2)) (EXPT L1 2))
		(* 2 L4 L5 )))
	)
	(IF (> (ABS CANG3) 1.0) (ERROR))
	(SETQ SANG3 (SQRT (- 1 (EXPT CANG3 2))))
	(SETQ ANG3 (ATAN (/ (ABS SANG3) (ABS CANG3))))
		(IF (< CANG3 0.) (SETQ ANG3 (- 3.14 ANG3)))
		(SETQ TST L12)
		(SETQ TPT L01)
		(TEST)
	(IF (< S 0.0) (SETQ ANG3 (* ANG3 -1.0)))
)

(DEFUN TEST ();To determine configuration of link 3, based on input
;configuration. Mathematically there are 2 possible positions for
;link 3. It is assumed that Kinematic constraint makes the other
;position unattainable.
	(SETQ TSTANG (ANGLE TPT L30))
	(IF (AND (AND (/= TSTANG 0.0) (/= TSTANG PI))
	(AND (/= TSTANG (/ PI 2)) (/= TSTANG (* 3 (/ PI 2)))))
		(TEST1) (TEST2))
	(IF (AND (= ANGSEQ 0.0) (> TSTANG PI))
		(SETQ CASE1 1.00)  (SETQ CASE1  -1.0 ))
	(IF (AND (= ANGSEQ PI) (> TSTANG PI)) (SETQ CASE1  1.0)) 
	(SETQ S (* CASE1 S))
)

(DEFUN TEST1 ();Sub function for TEST
	(SETQ S
          (- (/ (- (CADR TST) (CADR TPT))
		(- (CADR TPT) (CADR L30)))
  (/ (- (CAR TST) (CAR TPT)) (- (CAR TPT) (CAR L30))))
	)
)

(DEFUN TEST2 ();Sub function for TEST
	(IF (OR (= TSTANG 0.0) (= TSTANG PI)) (SETSX) (SETSY))
)

(DEFUN SETSX ();Sets flag after TEST
	(IF (> (CADR TST) (CADR L01)) (SETQ S -1.0) (SETQ S 1.0))
)

(DEFUN SETSY ();Sets flag after TEST
	(IF (>= (CAR TST) (CAR L01)) (SETQ S 1.00) (SETQ S -1.0))
)

(DEFUN ERROR ();Function to initiate error message for all modes,
;but branches for AUTOPATH mode.
	(IF (= OPTFLG 5) (BEGINPTH) (ERRMSG))
)

(DEFUN ERRMSG ();Error message for constrained position of link.
;Criterion for constraint: Sum of two sides of a triangle is
;greater than the third side.
	(PROMPT "\nLINKS CONSTRAINED IN THIS POSITION")
	(SETQ ANG LSTANG)
	(SETQ L12 LSTL12)
	(IF (= OPTFLG 1) (ANIMATE))
	(IF (= OPTFLG 2) (DISCRETE))
	(IF (= OPTFLG 3) (KEYBOD))
	(IF (= OPTFLG 4) (KEYBOD))
	(FOOLPATH)
)

(DEFUN BEGINPTH ();Associated with AUTOPATH. Sets flag to indicate
;that a limit of constraint has been reached and coupler curve
;generation can begin.
	(SETQ PSTRTFLG (+ PSTRTFLG 1))
	(IF (= PSTRTFLG 1) (GENPATH) (FOOLPATH))
)

(DEFUN FOOLPATH ();Function to facilitate smooth exit from one mode
;in order to enter another mode.
	(SETQ CANG1 LCANG1)
	(SETQ CANG2 LCANG2)
	(SETQ CANG3 LCANG3)
)
