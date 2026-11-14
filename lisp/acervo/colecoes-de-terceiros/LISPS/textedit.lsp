;PROGRAM NAME: TEXTEDIT

;PURPOSE:	Performs a global editing of text attributes.

; Copyright (C) 1991, CAD Consulting Services
;	Programmer: Steve McGuinness:
; Home Phone: (203) 757-7365

; Permission to use, copy, modify, and distribute this software and its
; documentation for any purpose and without fee is hereby granted.  

;HISTORY LOG:
;	8-4-90	SMM	Initial Coding
;	11-10-91 Steve McGuinness - Code reduction

;COMMANDS DEFINED:
;	TSIZE - Changes Height of Selected Text.
;	TSTYLE - Changes Font of Selected Text.
:	TVALUE - Changes Value of Selected Text.

;MODULES DEFINED:
;	TEXTEDIT - Performs text alteration based upon above criteria.
;	TSWAP		- Performs actual text entity modifications
;	GENERR	- Error Handling Routine

;PROGRAM CODE:

(defun C:TSIZE ()(globedit 40))
(defun C:TSTYLE ()(globedit 7))
(defun C:TVALUE ()(globedit 1))

(defun GENERR (s)
	(if (/= s "Function cancelled")
		(princ (strcat "\nError: " s))
	)
	(setq tset nil)	; Free selection set
	(setq *error* olderr)
	(princ)
)

(defun GLOBEDIT (type / cmde tset tcode tsize tstyle tvalue olderr)

	(setq
		olderr *error*
		*error* generr
		cmde (getvar "cmdecho")
		blip (getvar "blipmode")
	)
	(setvar "cmdecho" 0)
	(setvar "blipmode" 0)
	(menucmd "S=SELECT")
	(prompt "\nSelect Text To Modify <All>: ")
	(if (null (setq tset (ssget)))
		(setq tset (ssget "x" (list (cons 0 "TEXT"))))
	)
	(menucmd "S=")
	(cond
		((null tset) (prompt "\nNo Text Located in Drawing."))
		(t (terpri)
			(princ (itoa (sslength tset)))
			(prompt " Text Entities Selected.")
			(command "undo" "m")
		)
	)
	(cond
		((null tset) (prompt "\nNothing Modified."))
		((eq type 40)	; text height
			(initget 2)	; non zero
			(setq tsize (getreal "\nEnter New Text Size <Same>: "))
			(cond
				((null tsize)
					(prompt "\nNothing Modified.")
				)
				(t
					(tswap type tset tsize)
					(prompt "\nText Update Completed.")
				)
			)
		)
		((eq type 7)	; text font
			(setq tstyle (getstring "\nEnter New Text Font <Same>: "))
			(cond 					
				((eq tstyle "")
					(prompt "\nNothing Modified.")
				)
				((tblsearch "STYLE" tstyle)
					(tswap type tset tstyle)
					(prompt "\nText Update Completed.")
				)
				(t	(terpri)
					(princ (strcase tstyle))
					(prompt " is not a valid text style name.")
					(prompt "\nUse the STYLE command to list or create styles.")
				)
			)
		)
		((eq type 1)	; text value
			(setq tvalue (getstring "\nEnter New Text String <Same>: "))
			(cond 
				((eq tvalue "")
					(prompt "\nNothing Modified.")
				)
				(t
					(tswap type tset tvalue)
					(prompt "\nText Update Completed.")						
				)
			)
		)
	)
	(setvar "cmdecho" cmde)
	(setvar "blipmode" blip)
	(setq *error* olderr)
	(princ)
)

(defun TSWAP (index tset newval / sslen elist oldval oldpair newpair)
	(setq sslen (sslength tset))
	(setq count 0)
	(while (> sslen 0)
		(progn
			(setq elist (entget (ssname tset (- sslen 1))))
			(if (= "TEXT" (cdr (assoc 0 elist)))
				(entmod (subst
					(cons index newval)
					(assoc index elist)
					elist)
				)
			)
			(setq sslen (- sslen 1))
		)
	)
)
