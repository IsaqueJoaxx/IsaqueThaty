;PROGRAM NAME: TEXTSRCH.LSP

;PURPOSE: Performs a global text character search and replace.

; Copyright (C) 1991, CAD Consulting Services
;	Programmer: Steve McGuinness:
; Home Phone: (203) 757-7365

; Permission to use, copy, modify, and distribute this software and its
; documentation for any purpose and without fee is hereby granted.  

;HISTORY LOG:
;	7-15-88  AutoDesk:	Initial Coding	(CHGTEXT.LSP)
;	8-4-90	SMM:	Renamed to CHGTXT.LSP &
;				All text automatically selected (new option)
;	11-10-91 Steve McGuinness - Code reduction

;COMMANDS DEFINED:
;	TEXTSRCH	- Formerly called CHGTEXT

;MODULES DEFINED:
;	GENERR	- Error Handling Routine

;PROGRAM CODE:

(defun GENERR (s)
	(if (/= s "Function cancelled")
		(princ (strcat "\nError: " s))
	)
	(setq tset nil)	; Free selection set
	(setq *error* olderr)
	(princ)
)

(defun C:TEXTSRCH (/ tset l n e os as ns st s nsl osl sl si chf chm olderr)

	(setq
		olderr *error*
		*error* generr
		chm 0
	)
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
			(command "undo" "mark")
		)
	)
	(cond
		((null tset) (prompt "\nNothing Modified."))
		(t
			(while (= 0 (setq osl (strlen 
				(setq os (getstring t "\nOld string: ")))))
				(princ "Null input invalid")
			)
			(setq nsl (strlen (setq ns (getstring t "\nNew string: "))))
			(setq l 0 n (sslength tset))
			(while (< l n)		; For each selected object...
				(if (= "TEXT" (cdr (assoc 0 (setq e (entget (ssname tset l))))))
					(progn
						(setq
							chf nil
							si 1
							s (cdr (setq as (assoc 1 e)))
						)
						(while (= osl (setq sl (strlen
							(setq st (substr s si osl)))))
								(if (= st os)
								(progn
									(setq s (strcat (substr s 1 (1- si)) ns
										(substr s (+ si osl)
									)))
									(setq chf t) ; Found old string
									(setq si (+ si nsl))
								)
								(setq si (1+ si))
							)
						)
						(if chf
							(progn	; Substitute new string for old
								(setq e (subst (cons 1 s) as e))
								(entmod e)
								(setq chm (1+ chm))
							)
						)
					)
				)
				(setq l (1+ l))
			)
			(princ "Changed ")					 ; Print total lines changed
			(princ chm)
			(prompt " text lines.")
		)
	)
	(setq *error* olderr)
	(princ)
)
