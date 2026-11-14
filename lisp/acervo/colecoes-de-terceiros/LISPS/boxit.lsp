
;	DRAW A BOX AROUND A LETTER

(defun C:BTEXT (/ cntr ltr above below right left)
	(setq cntr (getpoint "\nCLICK A POINT AT THE CENTER"))
	(setq ltr (getstring "\nENTER ONE LETTER TO USE "))
	(setq above (+ (cadr cntr) (* 0.75 (getvar "TEXTSIZE"))))
	(setq below (- (cadr cntr) (* 0.75 (getvar "TEXTSIZE"))))
	(setq right (+ (car cntr) (* 0.75 (getvar "TEXTSIZE"))))
	(setq left (- (car cntr) (* 0.75 (getvar "TEXTSIZE"))))
	(command "LINE" (list left below)
			 (list right below)
			 (list right above)
			 (list left above)
			 "C"
	)
	(command "TEXT" "M" cntr (getvar "TEXTSIZE") "0" ltr
	)
)

;	DRAW A CIRCLE AROUND A LETTER

(defun C:CTEXT (/ cntr above)
	(setq cntr (getpoint "\nCLICK A POINT AT THE CENTER"))
	(setq ltr (getstring "\nENTER ONE LETTER TO USE "))
	(setq above (+ (cadr cntr) (* 0.75 (getvar "TEXTSIZE"))))
	(command "CIRCLE" cntr (list (car cntr) above)
	)
	(command "TEXT" "M" cntr (getvar "TEXTSIZE") "0" ltr
	)
)
;
