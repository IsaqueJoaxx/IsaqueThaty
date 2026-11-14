;		ATTPROP.LSP
;		load: (load "c:/lisp/attprop)	[or whatever the pathname is]
;		syntax: (attprop "s")
;	["s" for style
;	 "h" for height
;	 "w" for width factor
;	 "a" for angle
;	 "c" for color
;	 "l" for layer]
;		Prompts for block name or default <Pick>
;		Then lists attributes contained in that block
;		 and prompts for attribute name
;
;created by Dru Smyth CIS 74037,2434
;(c)1994 VHB, Inc.
;
(defun newerr (er)
	(if (/= er "Function cancelled")
		(princ (strcat "\nError: " er "\n"))
	)
	(command)
	(command)
	(command)
	(command ".undo" "end")
	(moder)
	(setq	*error* olderr moder nil)
(prin1)
)

(defun moder ()
	; list any other variable resets here
	(setq	attprop nil)
	(setvar "cmdecho" cecho)
(prin1)
)

(defun attprop (DRUass / DRUblk DRUval DRUset DRUnxt DRUnxl DRUntl DRUnty 
DRUnm DRUi DRUct DRUent DRUlst DRUnew)
(setq	olderr *error* *error* newerr
	cecho (getvar "cmdecho")
)
(setvar "cmdecho" 0)
(command ".undo" "group")

(setq	DRUblk (strcase (getstring "\nAttribute's block name/<Pick>: "))
	DRUass (cond ((= DRUass "s") 7)
			((= DRUass "h") 40)
			((= DRUass "w") 41)
			((= DRUass "a") 50)
			((= DRUass "c") 62)
			((= DRUass "l") 8)
		)
)
(if (or (= DRUblk "P") (= DRUblk ""))
(progn
(setq	DRUblk (cdr (assoc 2 (entget (car (entsel "\rSelect the block...                                  
"))))))
(princ (strcat "\nBlock: " DRUblk))
);end progn or
);end if or
(if (not (tblsearch "block" DRUblk))
	(princ (strcat "\rNo blocks named " DRUblk " were found."))
	(progn
(if (/= (cdr (assoc 70 (tblsearch "block" DRUblk))) 66)
	(princ (strcat "\rThe block " DRUblk " contains no attributes."))
	(progn
(setq	DRUset (ssget "x" (list (cons 2 DRUblk)))
	DRUnxt (ssname DRUset 0)
	DRUnxl (entget (entnext DRUnxt))
	DRUntl (cons (cdr (assoc 2 DRUnxl)) DRUntl)
)
	(princ (strcat " Attributes: "(cdr (assoc 2 DRUnxl))))
(setq	DRUnxl (entget (entnext (cdr (assoc -1 DRUnxl))))
	DRUnty (cdr (assoc 0 DRUnxl))
)
(while (not (= DRUnty "SEQEND"))
	(princ ", ")
	(princ (cdr (assoc 2 DRUnxl)))
(setq	DRUntl (cons (cdr (assoc 2 DRUnxl)) DRUntl)
	DRUnxl (entget (entnext (cdr (assoc -1 DRUnxl))))
	DRUnty (cdr (assoc 0 DRUnxl))
)
);end while
(setq	DRUnm (strcase (getstring "\nAttribute: ")))
(while (not (member DRUnm DRUntl))
	(princ (strcat DRUnm " is not in the list: "))
	(princ (reverse DRUntl))
	(setq	DRUnm (strcase (getstring "\nAttribute: ")))
);end while
(setq	DRUval (cond ((= DRUass 7)
		(strcase (getstring "\nNew style: ")))
			((= DRUass 40)
		(getreal "\nNew height: "))
			((= DRUass 41)
		(getreal "\nNew width factor: "))
			((= DRUass 50)
		(dtr (getreal "\nNew angle: ")))
			((= DRUass 62)
		(strcase (getstring "\nNew color: ")))
			((= DRUass 8)
		(strcase (getstring "\nNew layer: ")))
		)
	DRUi 0
	DRUct 0
)

(if (= DRUass 62)
(setq	DRUval (cond ((= (type (read DRUval)) 'INT) (read DRUval))
			((= DRUval "BYBLOCK") 0)
			((= DRUval "RED") 1)
			((= DRUval "YELLOW") 2)
			((= DRUval "GREEN") 3)
			((= DRUval "CYAN") 4)
			((= DRUval "BLUE") 5)
			((= DRUval "MAGENTA") 6)
			((= DRUval "WHITE") 7)
			((= DRUval "BYLAYER") 256)
		)
)
);end if 62

(repeat (sslength DRUset)
(setq	DRUent (ssname DRUset DRUi))
(while (/= (cdr (assoc 0 (entget (entnext DRUent)))) "SEQEND")
(setq	DRUent (entnext DRUent)
	DRUlst (entget DRUent)
)
(if (= (cdr (assoc 2 DRUlst)) DRUnm)
(progn
(if (= DRUass 62)
(progn
(if (assoc 62 DRUlst)
	(if (/= (cdr (assoc 62 DRUlst)) DRUval)
		(setq	DRUnew (subst (cons 62 DRUval) (assoc 62 DRUlst) DRUlst)
			DRUct (1+ DRUct)
		)
		(setq	DRUnew DRUlst)
	);end if not =
	(setq	DRUnew (cons (cons 62 DRUval) (cdr DRUlst))
		DRUnew (cons (car DRUlst) DRUnew)
		DRUct (1+ DRUct)
	)
);end if assoc 62
(entmod DRUnew)
(entupd DRUent)
);end progn then 62

(progn
(if (/= (cdr (assoc DRUass DRUlst)) DRUval)
(progn
	(setq	DRUnew (subst (cons DRUass DRUval) (assoc DRUass DRUlst) DRUlst)
		DRUnew (cond ((= DRUass 7)
			(subst (cons 41 (cdr (assoc 41
				(tblsearch "style" DRUval))))
				(assoc 41 DRUnew) DRUnew)
				)
			(t DRUnew)
			)
		DRUnew (cond ((= DRUass 7)
			(subst (cons 51 (cdr (assoc 50
				(tblsearch "style" DRUval))))
				(assoc 51 DRUnew) DRUnew)
				)
			(t DRUnew)
			)
		DRUct (1+ DRUct)
	)
	(entmod DRUnew)
	(entupd DRUent)
);end progn /=
);end if /=
);end progn else 62
);end if 62
);end progn DRUnm
);end if DRUnm
);end while
(setq DRUi (1+ DRUi))
);end repeat
(princ "Changed ")
(princ DRUct)
(if (= DRUct 1)
	(princ " attribute")
	(princ " attributes")
)
(princ " in ")
(princ (cond ((= (sslength DRUset) 1) "one block ")
		(t (strcat (itoa (sslength DRUset)) " blocks "))
	  )
)
(princ "named ")
(princ DRUblk)
(princ ".")
	);end progn no attributes
);end if no attributes
	);end progn not block
);end if not block
(command ".undo" "end")
(moder)
(setq *error* olderr moder nil)
(prin1)
)
(prin1)

