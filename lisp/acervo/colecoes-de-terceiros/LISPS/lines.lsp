;		Lines.lsp
;
;		by		John H. Foss
;				Kent Co. Rd. Commission
;				1500 Scribner N.W.
;				Grand Rapids, Mi. 49503
;				
;				ID: 	76310,252
;
;	**************************************************************
;	*							     *
;	*	This program produces parallel lines that are        *
;	*	started at opposing ends of the designated window    *
;	*	for more efficient plotting. There is no provision   *
;    *	for alternative or unequal spacing.		     *
;	*							     *
;	*							     *
;	*	variables:					     *
;	*		pt1	=	lower left corner 	     *
;	*		pt2	=	upper right corner	     *
;	*		disp	=	spacing between lines	     *
;	*					(always equal)       *
; 	*							     *
;	**************************************************************
;
;
(defun input_var ()
	(setq pt1(getpoint "\nThe Lower Left Corner: "))
	(setq pt2 (getpoint "\nThe Upper Right Corner: "))
	(setq s(getstring "\nHorizontal or Vertical Lines (H/V): "))
	(setq s(strcase s))
	(setq disp(getreal "\nThe Spacing of the Lines: "))
	(setq minx (car pt1))
	(setq miny (car (cdr pt1)))
	(setq maxx (car pt2))
	(setq maxy (car (cdr pt2)))
)
(defun set_var_on ()
				(setq oney miny)
				(setq twoy maxy)
				(setq onex minx)
				(setq twox maxx)
)
(defun set_var_off ()
				(setq onex maxx) 
				(setq twox minx)
				(setq oney maxy)
				(setq twoy miny)
)
(defun hor_lines ()
		(setq sw1 0)
		(setq maxy(- maxy disp))
		(while (> maxy miny)
			(if (= sw1 0)
			(set_var_on)
			(set_var_off)
			)
			(command "line" 
				(setq pt (list onex maxy))
				(setq pt (list twox maxy))
			)
			(command "")
			(setq maxy(- maxy disp))
			(if (= sw1 0) (= sw1 1) (= sw1 0))
		)
)
(defun ver_lines ()
		(setq sw1 0)
		(setq minx(+ minx disp))
		(while (< minx maxx)
			(if (= sw1 0)
			(set_var_on)
			(set_var_off)
			)
			(command "line"
				(setq pt (list minx oney))
				(setq pt (list minx twoy))
			)
			(command "")
			(setq minx(+ minx disp))
			(if (= sw1 0) (= sw1 1) (= sw1 0))
		)
)
(defun c:lines()
	(input_var)
	(if (= s "H")
		(hor_lines)
	)
	(if (= s "V")
		(ver_lines)
	)
)
