(DEFUN EVALLT () ;Evaluates length of individual links and checks if
;mechanism drawn qualifies as a four - bar link
	(SETQ L1 (DISTANCE L01 L12))
	(SETQ L2 (DISTANCE L12 L23))
	(SETQ L3 (DISTANCE L23 L30))
	(SETQ L4 (DISTANCE L01 L30))
	(IF (OR (= L1 0) (= L2 0) (= L3 0) (= L4 0)) (LTERROR))
)

(DEFUN LINKLT () ;Identifies and sets up based on initial 
;configuration
	(SETQ REFANG (ANGLE L01 L30)) ;Angular orientation of
;stationary link
	(IF (= ANGSEQ PI) (SETQ REFANG (- PI REFANG)))
	(IF (= (CAR L01) (CAR L30)) (SETQ ANGSEQ 0.0))
		(SETQ TPT L12)
		(SETQ TST L23)
		(TEST)
	(IF (OR (AND (<= (CAR L01) (CAR L30)) (<= (CAR TPT) (CAR L30)))
	        (AND (>  (CAR L01) (CAR L30)) (>= (CAR TPT) (CAR L30)))
	 	) (SETQ SCON 1.0) (SETQ SCON -1.0))
	(IF (< S 0.0) (SETQ ICON -1) (SETQ ICON 1))
	(SETQ ICON (* ICON SCON))
)

(DEFUN LTERROR () ;Error message if mechanism does not classify
;as four - bar link.
	(PROMPT "\nSYSTEM IS NOT A FOR-BAR LINKAGE")
	(COMMAND "U")
	(DRAWLINK)
)
