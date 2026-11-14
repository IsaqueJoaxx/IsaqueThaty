; A function that acts just like AutoCAD's LEADER command, except it allows
;     multiple lines of text.

; Sets up the text left-aligned and left-justified, no matter which side
;     of the leader line it's on.

; Draws everything in one plane, parallel to the XY plane of the
;     current UCS.  The height off the XY plane is set by the Z value
;     of the first point picked

; Draws text at an angle of 0 degrees from the UCS X axis.

; Leaves the default polyline width at zero, I don't know a good way to
;     restore any non-zero default polyline width. Otherwise, should restore
;     the system state completely under any circumstances.

; A typical menu line for using this might be:

;		^C^C^C(progn (if (not (boundp 'C:LEADR))(load "leadr"))(prin1));LEADR

; (assuming LEADR.LSP is stored in a directory that AutoCAD can find)


; Released into the public domain, with no warranty of mercantability or
;    fitness for a particular purpose.  Jon Fleming, 7/7/92

; Modified for AutoCAD 12 and correct handling of DIMSCALE=0 7/14/94



; If we're working with AutoCAD 11 or higher ...
(if (>= (atoi (substr (getvar "ACADVER") 2 2)) 11)
	; Remember that we're running under 11 or higher for later
	(setq	#mes11 T)
	(setq #mes11 NIL)
)

; If we're working with AutoCAD 12 or higher ...
(if (>= (atoi (getvar "ACADVER")) 12)
	; Remember that we're running under 11 or higher for later
	(setq	#mes12 T)
	(setq #mes12 NIL)
)

(defun C:LEADR	( /	p1 p2 p3 a_head a_size scale txtht s_set last_e our_z
										is_r_a count next_e done xmin dx sslen elist temp temp2
										cs_el cstyle cs_shx oldcec
								)
  ; Replace AutoCAD's standard error handler with ours (change
  ;     "T" to "NIL" for debugging)
  (if T
    (setq #mesoe *error*
          *error* meserh
    )
  )
  ; Save some system variables to be restored later (note that we will
  ;     restore ORTHOMODE to its state at entry)
	(if #mes12
  	(setq	#messl	(messsv	(list	"CMDECHO" "MENUECHO" "ORTHOMODE" "TEXTSIZE"
																"SNAPMODE" "OSMODE" "REGENMODE" "BLIPMODE"
																"PLINEWID"
																(list	"COMMAND" ".STYLE" 
																			(setq cstyle (getvar "TEXTSTYLE"))
																			"" "" "" "" "" "" ""
																			".COLOR" (setq oldcec (getvar "CECOLOR"))
																)
													)
									)
		)
  	(setq	#messl	(messsv	(list	"CMDECHO" "MENUECHO" "ORTHOMODE" "TEXTSIZE"
																"SNAPMODE" "OSMODE" "REGENMODE" "BLIPMODE"
																(list	"COMMAND" ".STYLE" 
																			(setq cstyle (getvar "TEXTSTYLE"))
																			"" "" "" "" "" "" ""
																			".COLOR" (setq oldcec (getvar "CECOLOR"))
																)
													)
									)
		)
	)
  ; Set system variables where we want them
  (mapcar 'setvar	'("CMDECHO" "MENUECHO" "SNAPMODE" "OSMODE" "REGENMODE" "PLINEWID")
									'(0 0 0 0 0 0.0)
	)
  ; Get the scale at which to draw things
  (setq scale (mes_scle)
        ; The height for the leader text
        txtht (* scale (getvar "DIMTXT"))
        ; Get the point for the arrowhead tip
        p1 (getpoint "\nPick arrow head point: ")
				; The Z value for everything
				our_z (caddr p1)
        ; Get the endpoint of the first leader segment
        p2 (planar (getpoint p1 "\nTo point: ") our_z)
        ; Get a point for the end of the arrowhead, saving the arrowhead
        ;     length for later
        p3 (polar p1 (angle p1 p2) (setq a_size (* scale (getvar "DIMASZ"))))
  )
  ; If the user has his/her own arrowhead block defined ...
  (if	(or	(and	(= (getvar "DIMSAH") 1)
								(/= "" (setq a_head (getvar "DIMBLK1")))
					)
					(/= "" (setq a_head (getvar "DIMBLK")))
			)
		(progn
			; Insert the arrowhead block (acting just like an AutoCAD 11
			;     dimension, the arrowhead block color doesn't depend on
			;     DIMCLRD)
	    (command	".INSERT" a_head p1 a_size ""
  				      (strcat "<<" (angtos (angle p3 p1) 0 8))
			)
			; If we're in AutoCAD 11+ and the user has a color defined for lines
			;     and arrows ...
			(if (and #mes11 (/= 0 (setq temp (getvar "DIMCLRD"))))
				; Draw the arrowhead and leader in the appropriate color
				(command ".COLOR" temp)
			)
			; Start drawing the leader
			(command ".PLINE" p1 "W" "0" "0")
    )
    ; Otherwise,
		(progn
			; If we're in AutoCAD 11+ and the user has a color defined for lines
			;     and arrows ...
			(if (and #mes11 (/= 0 (setq temp (getvar "DIMCLRD"))))
				; Draw the arrowhead and leader in the appropriate color
				(command ".COLOR" temp)
			)
	    ; Draw the arrowhead as a pline
  	  (command ".PLINE" p1 "W" "0" (* 0.3472 a_size) p3 "W" "0" "0")
		)
  )
  ; Draw the first line segment of the leader
  (command p2)
  ; Set p1 in case the user doesn't enter any more segments
  (setq p1 p2)
  ; As long as the user keeps entering endpoints ...
  (while (setq p2 (getpoint p2 "\nTo point (hit <Enter> to draw text): "))
    ; Draw the next line segment, saving the point for later
    (command (setq p1 (planar p2 our_z)))
  )
  ; Done with the leader, finish the PLINE command
  (command "")
	; And get rid of blips if they're on
	(setvar "BLIPMODE" 0)
	; If the current style is fixed height ...
	(if	(/= (cdr (assoc 40 (setq cs_el (tblsearch "STYLE" cstyle)))) 0.0)
		; If we've created our own style already ...
		(if (tblsearch "STYLE" "$MES_STYLE")
			; Make our style the current one
			(command ".STYLE" "$MES_STYLE" "" "" "" "" "" "" "")
			; Otherwise, create our own variable height style, using the same
			;    SHX file(s) as the current style
			(progn
				(if (= (setq cs_shx (cdr (assoc 4 cs_el))) "")
					(setq cs_shx (cdr (assoc 3 cs_el)))
					(setq cs_shx (strcat (cdr (assoc 3 cs_el)) "," cs_shx))
				)
				(command	".STYLE" "$MES_STYLE" cs_shx "0.0" "1.0" "0.0" "N" "N" "N")
			)
		)
	)
	; Save a pointer to the last entity in the database
	(setq last_e (entlast))
	; If we're in AutoCAD 11+ and the user has a color defined for dimension
	;    text ...
	(if (and #mes11 (/= 0 (setq temp (getvar "DIMCLRT"))))
		; Draw the text in the appropriate color
		(command ".COLOR" temp)
		; Otherwise, in case DIMCLRD was non-zero
		(command ".COLOR" oldcec)
	)
	; Prompt the user to enter the text
  (prompt "\nText: ")
	(command ".DTEXT")
  ; If the last segment was drawn left-to-right or vertical ...
  (if (>= (car p1) (car (osnap p1 "quick,midp")))
    ; Draw text left aligned
		; Use the appropriate responses for release 10 or release 11+ 
		(if #mes11
      (command  (mapcar '+ p1 (list (* scale (getvar "DIMGAP"))
																		(/ txtht -2.0)
																		0.0
															)
								)
                txtht "0"
      )
      (command  (mapcar '+ p1 (list (* scale (getvar "DIMEXO"))
																		(/ txtht -2.0)
																		0.0
															)
								)
                txtht "0"
      )
		)
		(progn
			; Otherwise, remember that we're drawing the text on the left side
			(setq is_r_a T)
	    ; Draw the text right-aligned
			; Use the appropriate responses for release 10 or release 11+ (not
			;     necessary but it makes me feel better)
			(if #mes11
				(command	"J" "R"
                  (mapcar '- p1 (list (* scale (getvar "DIMGAP"))
																			(/ txtht 2.0)
																			0.0
																)
									)
                  txtht "0"
        )
        (command  "R"
                  (mapcar '- p1 (list (* scale (getvar "DIMEXO"))
																			(/ txtht 2.0)
																			0.0
																)
									)
				)
			)
		)
  )
	; If we drew any right-aligned text ...
	(if	(and	(not (equal last_e (entlast)))
						is_r_a
			)
		(progn
			; Form a selection set of all the text entities drawn
			(setq	s_set (ssadd)
						next_e last_e
						count -1
			)
			(while (not done)
				; Skip the vertices of the polyline leader
				(if (= "TEXT" (cdr (assoc 0 (entget (setq next_e (entnext next_e))))))
					(setq s_set (ssadd next_e s_set))
				)
				(if (equal next_e (entlast))
					(setq done T)
				)
			)
			; Sweep through the text entities to find the minimum X coordinate
			;     of the right end of any text entity
			(setq	sslen (sslength s_set)
						xmin	(car	(trans	(cdr (assoc 10 (setq elist (entget next_e))))
																(cdr (assoc -1 elist))
																1
												)
									)
			)
			(while (/= (setq count (1+ count)) sslen)
				(setq	xmin	(min	xmin
													(car	(trans	(cdr (assoc 10 (setq elist (entget (ssname s_set count)))))
																				(cdr (assoc -1 elist))
																				1
																)
													)
										)
				)
			)
			; Sweep through the selection set again, moving each entity as
			;     necessary to make the X coordinates at the left end line up
			(setq count -1)
			(while (/= (setq count (1+ count)) sslen)
				; Get the current text entity
				(setq	elist (entget (ssname s_set count))
							; How far we have to move it
							dx	(-	xmin
											(car	(trans	(cdr (setq temp (assoc 10 elist)))
																		(cdr (assoc -1 elist))
																		1
														)
											)
									)
							; Fix the start point
							elist	(subst	(cons	10
																	(mapcar	'+
																					(trans	(list dx 0.0 0.0)
																									1
																									(cdr (assoc -1 elist))
																					)
																					(cdr temp)
																	)
														)
														temp elist
										)
							; Get the current end point
							temp2 (assoc 11 elist)
							; Fix the end point to be like left-aligned text
							elist (subst (list 11 0.0 0.0 (cadddr temp)) temp2 elist)
							; Set the flag for left-aligned
							elist (subst '(72 . 0) '(72 . 2) elist)
				)
				; Update the text
				(entmod elist)
				; If we're on the first text line and we've moved it ...
				(if	(and	(= count 0)
									(/= dx 0.0)
						)
					; We want to extend the leader line horizontally to make the line-
					;     to-text gap constant
					(progn
						(command ".LINE" p1 (mapcar '+ p1 (list dx 0.0 0.0)) "")
						(command ".PEDIT" last_e "JOIN" (entlast) "" "X")
					)
				)
			)
		)
	)
  ; Restore system variables
  (mesrsv #messl)
  ; Clean finish with no return value
  (prin1)
)

; A function to substute a value for the third element of a three-
;    element list.  Useful to force points chosen to a particular
;    Z value.

; Arguments:

;		p1 = a list of three reals (that is, a 3D point)

;   z_val = real number to substitute for the third element of p1

; Return value:

;		A list of three reals as described above

(defun planar (p1 z_val)
	(reverse (cons z_val (cdr (reverse p1))))
)

; MES Save System Variables, a function to save some set of system
;		variables.

; Argument:

;		s_list = a list in which each element should be either the name of
;							a system variable to be saved, or a list of strings
;							starting with "COMMAND" and followed by strings or quoted
;							items that will reset the desired item.  (The second
;							form is for resettting read-only stuff such as current
;							layer).

; Return value:

;		A list of either dotted pairs of a system variable name and its
; current value, or the commands to be fed to the "command" function
; to restore the state.

(defun messsv (s_list / tmp_l)
	; Set up an empty list for the return value
	(setq tmp_l '())
	; For each item in the list of variables to be saved ...
	(foreach next_v s_list
		; If the item is a list ...
		(if (listp next_v)
			; Then it must be a command, just store it
			(setq tmp_l (cons next_v tmp_l))
			; It's not a list, it must be a system variable name.  Put it into
			; a dotted pair list with its current value, and add that list to
			; the final list
			(setq tmp_l (cons (cons next_v (getvar next_v)) tmp_l))
		)
	)
)

; MES Restore System Variables, a function to restore a set of system
;		variables

; Argument:

;		tmp_l = a list previously set up by the messsv function

; Return value:

;		none

; Special note:

;		If the name of a previous error handler is bound to the variable
;			"#mesoe" (MES Old Error Handler), the *error* is restored to
;			whatever is bound to "#mesoe"

(defun mesrsv (tmp_l / cmdstr)
	; If there is indeed a list of values to restore ...
	(if tmp_l
		(progn
			; Restore the values
			(foreach next_v tmp_l
				; If it starts with the string "command" ...
				(if (= (strcase (car next_v)) "COMMAND")
					; Then it must be a set of strings to pass to the (command)
					; function
					(progn
						(setq cmdstr (cdr next_v))
						(foreach next_c cmdstr
							(command next_c)
						)
					)
					; Otherwise, it's a variable name and its value to reset to
					(setvar (car next_v) (cdr next_v))
				)
			)
			; NIL out the list
			(setq tmp_l NIL)
		)
	)
	(if #mesoe
		; Re-install the original error handler
		(setq *error* #mesoe)
	)
)

; MES ERror Handler, a function to clean up and exit if an error
;		occurs or the user cancels (Control-C or Control-Break)

;	Argument:

; msg = descriptive message string passed by AutoCAD

; Return value:

;		none

(defun meserh (msg / errno)
	(if #mes11
		(setq errno (itoa (getvar "ERRNO")))
	)
	;Cancel any pending command and end any UNDO group
	(command)
	(command)
	(command)
	(command "UNDO" "E")
	; Restore system variables
	(mesrsv #messl)
	; If the message is just a user cancelling ...
	(if	(or	(= (strcase msg) "FUNCTION CANCELLED")
					(= (strcase msg) "CONSOLE BREAK")
			)
		; Print a message without "Error"
		(prompt (strcat "\n" msg))
		; Otherwise, print an error indication
		(if #mes11
			(prompt (strcat "\nError " errno ": " msg))
			(prompt (strcat "\nError: " msg))
		)
	)
	; Clean finish with no return value
	(prin1)
)

; A function to determine a scale factor for "dimension-like" entities.

;  If DIMSCALE is non-zero, returns the value of DIMSCALE.

;  If DIMSCALE is 0.0, returns 1.0 if the current space is paper space
;                      returns an appropriate scale factor if crrent
;                           space is a model space viewport
;                      returns 1.0 if there are no model space viewports

(defun mes_scle ( / scale vport )
	(if (= (setq scale (getvar "DIMSCALE")) 0.0)
		(if (= (getvar "TILEMODE") 0)
			; We might be in paper space or model space
			(if (= (setq vport (getvar "CVPORT")) 1)
				; We're in paper space, use 1.0
				1.0
				; We're in model space, get the entity association list of the
				;  viewport we're in, including ACAD's extended entity data
				(setq	vp_el	(entget	(ssname	(ssget	"X"
																						(list	'(0 . "VIEWPORT")
																									(cons 69 vport)
																						)
																		)
																		0
														)
														'("ACAD")
										)
							; Get a scale by dividing the seventh number in AutoCAD's
							;  data (the second "1040" group) by the viewport height
							;  (paperspace units)
							scale (/	(cdr (nth 7 (cadr (assoc -3 vp_el))))
												(cdr (assoc 41 vp_el))
										)
				)
			)
			; DIMSCALE is zero and TILEMODE is 1, which makes no sense; use 1.0
			;   as the best of an infinite set of bad guesses.
			1.0
		)
		; DIMSCALE is non-zero, use it
		(eval scale)
	)
)

(prin1)
