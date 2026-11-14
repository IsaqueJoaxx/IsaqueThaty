;bcoinart.lsp 
;Main Routine Names => c:bcoin
;					    
;Description	 => Erases all coincidental lines on 
;					    the same layer.
(princ "\nLoading Routine...\n")
(princ "\nTo erase Coincidental lines type ==> bcoin\n")
(defun c:bcoin ( / lay_p echo olderr lineass count totalt long element mcounter lineas ln1 paso line1 ln1 ln1lay ln1spt ln1ept line2 ln2 ln2lay ln2spt ln2ept lay_p lay_list)
(setq echo (getvar "CMDECHO"))
(setvar "CMDECHO" 0)
(command "UNDO" "mark" )
(if *error*
		(progn
			(setq olderr *error*)
			(setq *error* new_err)
		);progn
		(progn
			(setq *error* new_err)
		);progn
);if
(if (setq lineass (ssget "X" (list (cons 0 "line")))) (setq long (sslength lineass))
		(setq long 0)
);if
(setq totalt long)
(while (setq lay_data (tblnext "LAYER" (not lay_data))) (setq lay_data (cdr (assoc 2 lay_data)))
(setq lay_list (append lay_list (list lay_data)))
	)
(foreach lay_p lay_list
		(setq mcounter 0)
		(setq element 1)
		(if (setq lineas (ssget "X" (list (cons 0 "line")(cons 8 lay_p))))
				(setq long (sslength lineas))
				(setq long 0)
		);if
		(setq long (- long 1))
		(setq pass 0)
		(princ "\n    ")
		(princ (strcat "\nProcessing layer " lay_p "\n"))
(while  (and (> long mcounter)(/= long 0)(>= long element))
			(princ ".")
			(setq ln1 nil)
			(setq paso nil)
(setq line1 (ssname lineas mcounter)) (setq ln1 line1)
(setq line1 (entget line1))
			(setq ln1lay (cdr (assoc 8 line1)) )
			(setq ln1spt (cdr (assoc 10 line1)) )
			(setq ln1ept (cdr (assoc 11 line1)) )
(setq line2 (ssname lineas element)) (setq ln2 line2)
(setq line2 (entget line2))
			(setq ln2lay (cdr (assoc 8 line2))) 
(setq ln2spt (cdr (assoc 10 line2))
)
(setq ln2ept (cdr (assoc 11 line2))
)
(if (and (/= ln1lay nil)(= ln1lay ln2lay)) (progn
									(acode)
									(bcode)
							);progn
							(progn
									(setq element (+ element 1))
(if (> element long)
											(progn
(setq mcounter (+ mcounter 1)) (setq element (+ mcounter 1))
											);progn
									);if
							);progn
				);if
				(if (> element long)
							(progn
(setq mcounter (+ mcounter 1)) (setq element (+ mcounter 1))
							);progn
				);if
	);while
);foreach
(command "regen")
(if (setq tnow (ssget "X" (list (cons 0 "line"))))
			(setq tnow (sslength tnow))
			(setq tnow 0)
);if
(princ "\nTotal Number of Lines to Start: ")(princ totalt) (princ "\nTotal Number of Lines Left    : ")(princ tnow) (setq *error* olderror)
(princ)
);defun
(princ)
(defun acode ( / ang1 lnt1 oldspt oldept newspt newept ang2 ) (setq ang1 (rtd (angle ln1spt ln1ept)))
(if (and (> ang1 90.0)(<= ang1 270.0)) (progn
							(setq lnt1 nil)
(setq oldspt (cons 10 ln1spt)) (setq oldept (cons 11 ln1ept)) (setq lnt1 ln1spt)
(setq ln1spt ln1ept)
							(setq ln1ept lnt1)
(setq newspt (cons 10 ln1spt)) (setq newept (cons 11 ln1ept)) 
(setq line1 (subst newspt oldspt line1)) (setq line1 (subst newept oldept line1)) (entmod line1)
(setq ln1spt (cdr (assoc 10 line1))) (setq ln1ept (cdr (assoc 11 line1)))
					);progn
			);if
			(setq ang2 (rtd (angle ln2spt ln2ept)))
(if (and (> ang2 90.0)(<= ang2 270.0)) (progn
(setq lnt2 nil)
(setq oldspt (cons 10 ln2spt)) (setq oldept (cons 11 ln2ept)) (setq lnt2 ln2spt)
(setq ln2spt ln2ept) (setq ln2ept lnt2) 
(setq newspt (cons 10 ln2spt)) (setq newept (cons 11 ln2ept)) 
(setq line2 (subst newspt oldspt line2)) (setq line2 (subst newept oldept line2)) (entmod line2)
(setq ln2spt (cdr (assoc 10 line2))) (setq ln2ept (cdr (assoc 11 line2)))
							);progn
				);if
);defun
(defun bcode (/ ang1 ang2 ang12s ang12e paso ln1dist ln2dist)
(setq ang1 (rtd (angle ln1spt ln1ept)))
(setq ang2 (rtd (angle ln2spt ln2ept))) (setq ang12s (rtd (angle ln1spt ln2spt))) (setq ang12e (rtd (angle ln2ept ln1ept))) (setq paso nil)
(if (= ang1 ang2)
		(progn
			(if (and (equal ln1spt ln2spt)(equal ln1ept ln2ept))
						(progn
								(eraseln1)
								(setq paso 10)
						);progn
			);if
(if (and (or (equal ln1spt ln2spt)(equal ln1ept ln2ept))(= paso nil)) (progn
								(setq ln1dist (distance ln1spt ln1ept))
(setq ln2dist (distance ln2spt ln2ept)) (if (> ln1dist ln2dist)
											(progn
(eraseln2)
(setq paso 15)
											);progn
											(progn
(eraseln1)
(setq paso 100)
											);progn
								);if
						);progn
				);if
(if (and (= ang12s ang12e)(= paso nil))
							(progn
(setq ln1dist (distance ln1spt ln1ept)) (setq ln2dist (distance ln2spt ln2ept)) (if (> ln1dist ln2dist)
												(progn
(eraseln2)
														(setq paso 15)
												);progn
												(progn
(eraseln1)
														(setq paso 100)
												);progn
										);if
							);progn
					);if							  
	);progn
);if
(if (= paso nil)
	(setq element (+ element 1))
);if
);defun
(defun eraseln1 ()
	(command "erase" ln1 "")
(setq lineas (ssget "X" (list (cons 0 "LINE")))) (setq long (sslength lineas))
	(setq long (- long 1))
	(setq mcounter 0)
	(setq element 1)
	(princ "*")
);defun
(defun eraseln2 ()
	(command "erase" ln2 "")
	(setq element (+ element 1))
	(princ "*")
);defun
(defun new_err (err_msg)
(princ "The following error has ocurred: ") (princ err_msg)
		(princ)
);defun
(defun rtd (a)
			(/ (* a 180.0) PI)
)
(princ)
