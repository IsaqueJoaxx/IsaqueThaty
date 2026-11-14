;Sylads.lsp, (C)1993 R. Frank.
;These lisp utilities provide additional functionality to the Sylads ADS
;program.  Sylads.exe/exp should be available when you (load "sylads")
;The functions included in sylads.lsp are:
;
;	syldefall,	defines all blocks from within a library file.
;	syldwgout, 	wblocks all blocks from a library file to DWG files.
;
;	syll,		sets the library to use with syli and sylw.
;	c:syli,		emulates the AutoCAD Insert command.
;	c:sylw,		emulates the AutoCAD Wblock command.
;	sylr,		maps the Insert and Wblock commands to c:syli and c:sylw.
;
;Globals used by sylads lisp functions: syllib, sylmap, syllast.
;You do not need to modify these globals yourself.

;Load Sylads ADS functions if thay are not already loaded
(if (not sylver) (xload "sylads"))

;Function to define all blocks from a library file into the current drawing.
;Existing blocks will be redefined.
(defun syldefall (lib / lst name n m)
	(setq lst (syllist lib "BLOCK"))
	(setq n (length lst))
	(setq m 0)
	(while (< m n)
		(setq name (nth m lst))
		(print (strcat "Defining " name ", " (itoa (+ 1 m)) " of " (itoa n)))
		(syldef lib name)
		(setq m (1+ m))
	)
	m
)

;Similar to syldefall, except that it inserts all blocks into the drawing.
(defun sylinsall (lib / lst name n m aold)
	(setq aold (getvar "attreq"))
	(setvar "attreq" 0)
	(setq lst (syllist lib "BLOCK"))
	(setq n (length lst))
	(setq m 0)
	(command)
	(while (< m n)
		(setq name (nth m lst))
		(print (strcat "Defining " name ", " (itoa (+ 1 m)) " of " (itoa n)))
		(syldef lib name)
		(command ".insert" name '(0 0 0) 1 1 0)
		(setq m (1+ m))
	)
	(setvar "attreq" aold)
	m
)

;Function to WBLOCK all blocks from a library file to individual DWG files
(defun syldwgout (lib path / lst name exold cmold n m)
	(setq exold (getvar "expert"))
	(setq cmold (getvar "cmdecho"))
	(if (not path) (setq path ""))
	(setq lst (syllist lib "BLOCK"))
	(setvar "expert" 3)
	(setvar "cmdecho" 0)
	(setq n (length lst))
	(setq m 0)
	(while (< m n)
		(setq name (nth m lst))
		(princ (strcat "\nWriting " name ", " (itoa (+ 1 m)) " of " (itoa n)))
		(syldef lib name)
		(command "wblock" (strcat path name) name)
		(setq m (1+ m))
	)
	(setvar "expert" exold)
	(setvar "cmdecho" cmold)
)

(defun syll ( name / )
; This function sets the library to be used with the c:syli and c:sylw functions
	(setq syllib name)
	(sylfind syllib)
	syllib
)
(defun C:syll ( / s)
;	Command version of (syll)
	(setq s (getstring (strcat "\nPlease enter symbol library name <" syllib ">: ")))
	(if (= "" s) (setq s syllib))
	(syll s)
)
(if (not syllib) (setq syllib "acad"))

(defun C:syli (/ cmold ce bn block sblock i rdflag exflag dwflag flag)
; This function behaves similarly to the AutoCAD INSERT command.
; The library used is set using the syll function.
	(setq cmold (getvar "cmdecho"))
	(setq ce T);	To observe cmdecho use: (setq ce (not (= cmold 0)))
	(setvar "cmdecho" 0)
	(if ce
		(if sylmap	;only display default if command is remapped
			(progn
				(if (not (tblsearch "BLOCK" syllast)) (setq syllast ""))
				(if (/= "" syllast)
					(setq bn (getstring (strcat "Block name (or ?) <" syllast ">: ")))
					(setq bn (getstring "Block name (or ?): "))
				)
			)
			(setq bn (getstring "Block name (or ?): "))
		)
		(setq bn (getstring))
	)
	(cond
		((= 0 (strlen bn)) (command ".insert" (if sylmap syllast "")))
		((= "?" (substr bn 1 1)) (command ".insert" "?"))
		((= "=" (substr bn 1 1)) (command)) ;Catch user error
		((> (strlen bn) 0)
			(progn
				(setq block bn)	; name of block to be defined
				(if (= "*" (substr bn 1 1)) (setq block (substr bn 2) exflag T))
				(setq sblock block)
				(if (wcmatch bn "*=*")
					; "=" sign forces redefinition of the block.
					(progn
						(setq i 1 rdflag T)
				 		(while (/= "=" (substr bn i 1)) (setq i (1+ i)))
						(setq block (substr bn 1 (1- i)))
						(setq sblock (substr bn (1+ i)))
						(if (= "" sblock) (setq sblock block))
					)
				)
				(setq dwflag (findfile (strcat sblock ".dwg")))
				(if (or (wcmatch sblock "*\\*") (wcmatch sblock "*/*") (wcmatch sblock "*:*"))
					; path included for DWG filename.  This path will be ignored
					; if the library file is accessed.
					(progn
						;Locate last slash
						(setq i (strlen sblock))
						(while (and (/= "\\" (substr sblock i 1)) (/= "/" (substr sblock i 1)) (/= ":" (substr sblock i 1)))
							(setq i (1- i))
						)
						(setq sblock (substr sblock (1+ i)))
						(if (not rdflag)
							(setq block sblock)
						)
					)
				)
				; Variable BLOCK contains the block name to be inserted
				; SBLOCK is the name of the block in the library (or dwg)
				; rdflag is set when the block should be redefined
				; exflag indicates that INSERT * should be used
				; dwflag indicates that a DWG for the block is available
				;
				; Check the for the block in the following order:
				;    1.  Block table (already defined in drawing)
				;    2.  Library path for .DWG file
				;    3.  Symbol library set in the SYLL function.
				;
				; NOTE: Normally, an available DWG block will be inserted before the
				; SYL library is accessed.  If you would like to ignore existing
				; DWG files, set dwflag to nil here as follows:
				; (setq dwflag nil)
				;
				(if (or rdflag (not (tblsearch "BLOCK" sblock)))
					(if (not dwflag)
						; Define block from library file
						(if (not (syldef syllib block sblock)) (setq flag T))
					)
				)
				(cond
					(flag
						(progn
							(princ "\nCould not locate block \"")
							(princ (strcase sblock))
							(princ "\" within ")
							(princ (strcase syllib))
							(princ ".SYL or as ")
							(princ (strcase sblock))
							(princ ".DWG")
							(terpri)
						)
					)
					((and rdflag (not dwflag)) (command ".insert" block))
					(T (command ".insert" bn))
				)
				(if (not flag)
					(if exflag
						(setq syllast (strcat "*" block))
						(setq syllast block)
					)
					; Set the syllast default block value for future inserts, for
					; 	example "blk1" in "Block name (or ?) <blk1>:"
					; Note: The use of (sylins) or the command ".insert" will not
					;	change this value, causing the default to be
					;	different.  If this causes problems, set syllast to ""
					;	by removing the comment from the line below:
					;(setq syllast "")
				)
		  	)
	 	)
	)
 	(setvar "cmdecho" cmold)
 	(prin1)
)

(defun C:sylw ( / cmold user fn bn sb alflag ovr ce i sv sa)
; This function behaves similarly to the AutoCAD WBLOCK command.
; The library used is set using the syll function.
; This function is not defined in read-only versions of Sylads
;
	(if (= (substr (sylver) 5) "READ-ONLY") (setq user T))
	(setq cmold (getvar "cmdecho"))
	(setq ce T);	To observe cmdecho use: (setq ce (not (= cmold 0)))
	(setvar "cmdecho" 0)
	(if ce
		(setq fn (getstring "File name: "))
		(setq fn (getstring))
	)
	(if (not (sylfind syllib))
		(progn
			(princ "Creating library. ")
			(sylnewlib syllib)
			; The above line creates a new library if it cannot be located
		)
	)
	(if (or (wcmatch fn "*\\*") (wcmatch fn "*/*") (wcmatch fn "*:*"))
	; remove path name if used.
		(progn
			(setq i (strlen fn))
			(while (and (/= "\\" (substr fn i 1)) (/= "/" (substr fn i 1)) (/= ":" (substr fn i 1)))
				(setq i (1- i))
			)
			(setq sb (substr fn (1+ i)))
		)
		(setq sb fn)
	)
	(if (and (sylfind syllib sb) (< (getvar "expert") 3))
		; display warning message if block is in symbol library already
		(progn
			(setq ovr " " i nil)
			(while (and (/=  (strcase (substr ovr 1 1)) "Y") (/=  (strcase (substr ovr 1 1)) "N"))
				(if i (princ "Yes or No, please.\n"))
				(setq i T)
				(if ce
					(progn
						(princ "\nA symbol with this name already exists.")
						(princ "\nDo you wish to replace it? <N> ")
					)
				)
				(setq ovr (getstring))
				(if (= ovr "") (setq ovr "N"))
			)
			(if (/= (strcase (substr ovr 1 1)) "Y") (setq ovr nil))
		)
		(setq ovr T)
	)
	(if ce
		(setq bn (getstring "Block name: "))
		(setq bn (getstring))
	)
	(if (= "=" (substr bn 1 1))
		(setq bn sb)
	)
	(if (= "*" (substr bn 1 1))
		;save entire drawing
		(setq bn sb alflag T)
	)
	(if (= "" bn) (setq bn nil))
	; sb is 'filename' without path
	; bn is the name of the block to be written, nil if selection set
	; alflag is true if the entire drawing is to be written
	(if (and (findfile (strcat fn ".DWG")) (not user))
		(princ (strcat "\nWarning: " (findfile (strcat fn ".DWG")) " exists.  It is not overwritten."))
	)
	(cond
		(user (command ".wblock" fn bn)) ;Cannot write to library with read-only version
		((not ovr) nil) ;Not given permission to write
		((not (sylfind syllib))  ;write to DWG file
			(progn
				(princ "\nCannot locate ")
				(princ (strcase syllib))
				(princ ".SYL, creating DWG file.\n")
				(command ".wblock" fn bn)
			)
		)
		(alflag
 			(progn
				(setq sv (ssget "X" (list (cons 0 "VIEWPORT"))))
				(if sv
					;The first viewport is paperspace, and should be ignored.
					(if (> (sslength sv) 1)
						(princ "\nWarning: Viewports are not being written.")
					)
				)
				(setq sa (ssget "X" (list (cons 0 "~VIEWPORT"))))
				(if sa 
					(sylwblock syllib sb '(0 0 0) sa)
					(princ "\nDrawing is empty, no block written.")
				)
			)
		)
		((not bn)
 			(if (sylwblock syllib sb (getpoint "\nInsertion base point: ") (progn (princ "\n") (setq sa (ssget))))
				(command  "erase" sa "")
			)
		)
		((/= bn sb)
			; If the the file and block names are different, the filename will be
			; the name used in the library. This is to remain compatible with
			; the wblock command, where as in (command ".wblock" "fname" "bname"),
			; the block defined as bname loses it's identity and becomes fname.
			; This may be somewhat confusing because it may seem logical to ignore
			; the filename due to the fact that a DWG is never created.  Therefore,
			; the optional "rename" string as a third parameter is not documented
			; in the reference manual for (sylwblock) as used below.
			(sylwblock syllib bn sb)
		)
		(T (sylwblock syllib bn))
	)
	(setvar "cmdecho" cmold)
	nil
)

(defun sylr (flag / user cmold ce)
;Directs command: INSERT and WBLOCK to use the syli and sylw commands
	(setq cmold (getvar "cmdecho"))
	(setq ce T);	To observe cmdecho use: (setq ce (not (= cmold 0)))
	(setvar "cmdecho" 0)
	(if (= (substr (sylver) 5) "READ-ONLY") (setq user T))
	(if flag
		(progn
			(setq sylmap T)
			(setq syllast "") ;will be used to display the default block for syli
			(command "undefine" "insert")
			(defun C:insert () (C:syli))
			(if (not user)
				(progn
					;User versions cannot write to a library
					(command "undefine" "wblock")
					(defun C:wblock () (C:sylw))
					(if ce (princ "\nInsert and Wblock commands redefined for Sylads. "))
				)
				(if ce (princ "\nInsert command redefined for Sylads. "))
			)
		)
		(progn
			(setq sylmap nil)
			(setq syllast nil)
			(command "redefine" "insert")
			(command "redefine" "wblock")
			(if ce (princ "\nInsert and Wblock commands returned to normal. "))
		)
	)
 	(setvar "cmdecho" cmold)
	syllib
)

; The *error* function below may be used to keep sylads lisp from
; ralphing over the screen in the event of an error or cancel.

;(defun *error* (msg)
;	(princ "error: ")
;	(princ msg)
;	(terpri)
;)

(princ "\nSylads AutoLISP utilities loaded. ")
syllib
