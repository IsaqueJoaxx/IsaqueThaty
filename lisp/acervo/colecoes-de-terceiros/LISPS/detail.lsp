;DETAIL.LSP

; Purpose: To make a circular detail from part of a drawing and trim parts of
; entities outside detail.

; Before running the routine, draw a circle around the section to be detailed
; and a circle to put the detail into. The routine will prompt you to pick the
; source and destination circles, and does the rest for you. The two circles
; can be different sizes.

; You are given the option to copy text into the detail, to scale any text
; which does get copied and to scale pline widths. The finished detail can be
; wblocked to an external file. Attributes may be turned into text, in order
; to save their values and any attedit changes.

; Detail.lsp is able to trim every entity, except minserts, blocks with unequal
; scales and text or shapes. Plines with width will lose their linetype and
; become continuous. Some entities (ie. solids) get trimmed along the circle
; with line segment approximations. Their accuracy is controlled by the system
; variable SPLINESEGS. For my purposes, it is the # of line segments in a 45
; degree arc.

;   written by: Len Switzer
;		Upper Canada Software
;		222 Monteith Ave.
;		Stratford, Ont., Canada
;		N5A 2P6
;		(519) 271-1019

(prompt"\nLoading detail.lsp...")

(defun C:DETAIL( / CENTER CIR1 CIR2 ENT ENT2 ENTLST FH FLAGS PLINES RADIUS
		   SCALE SKIPSS SS STR SYSLST TEXT)

  (SYSVAR"CMDECHO"0)
  (SYSVAR"HIGHLIGHT"0)
  (SYSVAR"BLIPMODE"0)

  (while(or(null(setq CIR1(entsel"\nPick source circle: ")))
	  (/=(cdr(assoc 0(setq CIR1(entget(car CIR1)))))"CIRCLE")))

  (while(or(null(setq CIR2(entsel"\nPick destination circle: ")))
	  (/=(cdr(assoc 0(setq CIR2(entget(car CIR2)))))"CIRCLE")))


  (setq RADIUS(cdr(assoc 40 CIR2))
	CENTER(cdr(assoc 10 CIR2))

	;Find all entities in source circle.
	SS(ssget"C"(mapcar'(lambda(PT1)(- PT1(cdr(assoc 40 CIR1))))
		     (cdr(assoc 10 CIR1)))
		   (mapcar'(lambda(PT1)(+ PT1(cdr(assoc 40 CIR1))))
		     (cdr(assoc 10 CIR1))))

	ENTLST(entlast)
	ENT ENTLST)

  ;Remove source circle & copy entities to destination circle.
  (ssdel(cdr(assoc -1 CIR1))SS)
  (command".COPY"SS""(cdr(assoc 10 CIR1))CENTER)

  ;Make a selection set of new entities.
  (setq SS(ssadd))
  (while(setq ENT(entnext ENT))
    (ssadd ENT SS))

  ;Scale new entities to fit destination circle.
  (setq SCALE(/ RADIUS(cdr(assoc 40 CIR1))))
  (if(/= SCALE 1.0)
    (command".SCALE"SS""CENTER SCALE))

  ;Bit flags: Delete source circle/Wblock/Scale text.
  (setq FLAGS 0)

  (initget"Yes No")
  (if(eq(getkword"\nCopy text into detail? <No>: ")"Yes")
    (progn
      (prompt"\nSelect text to copy or <All>:")
      (if(null(setq TEXT(ssget)))
	(setq TEXT T))
      (initget"Yes No")
      (if(eq(getkword"\nScale text to fit detail? <No>: ")"Yes")
	(setq FLAGS(logior FLAGS 4)))))

  (initget"Yes No")
  (if(eq(getkword"\nScale pline width to fit detail? <No>: ")"Yes")
    (progn
      (prompt"\nSelect plines for width scaling or <All>:")
      (if(null(setq PLINES(ssget)))
	(setq PLINES T))))

  (initget"Yes No")
  (if(/=(getkword"\nDelete source circle? <Yes>: ")"No")
    (setq FLAGS(logior FLAGS 1)))

  (initget"Yes No")
  (if(eq(getkword"\nWblock detail? <No>: ")"Yes")
    (while
      (progn
	(setq FLAGS(logior FLAGS 2)
	      STR(strcase(getstring"\nEnter file name: ")))
	(if(setq FH(open(strcat STR".DWG")"r"))
	  (progn(setq FH(close FH))
	    (initget"Yes No")
	    (eq(getkword(strcat"\n"STR".DWG exists. Overwrite? <Yes>: "))"No"))
	  (if(setq FH(open(strcat STR".DWG")"a"))
	    (setq FH(close FH))
	    (null(prompt(strcat"\nCan't open "STR".DWG for write."))))))))

  (if(eq PLINES T)
    (setq PLINES SS))
  (if(eq TEXT T)
    (setq TEXT SS))

  ;delete source circle?
  (if(eq(logand FLAGS 1)1)
    (entdel(cdr(assoc -1 CIR1))))

  (setq ENT(entget ENTLST)SS nil SKIPSS(ssadd))

    ;Start off trim command.
  (command".TRIM"(cdr(assoc -1 CIR2))"")


  ;Main loop.

  ;Process each new entity, unless it's in SKIPSS. The trim command is used
  ;for lines, circles and arcs, and is maintained as the current command
  ;throughout the routine. It's very important to not go nuts trimming things
  ;at any old point. Everything must only be trimmed outside the detail. Lines
  ;and arcs may need to be trimmed once or twice, circles may need one. Any
  ;entities entirely outside the detail are deleted.

  ;Blocks, dimensions, 3d-meshes and zero width plines are exploded into their
  ;component parts. Attributes may be turned into text, in order to save their
  ;value and any attedit changes.

  ;The Lisp War Rules were very clear that the routine should handle solids
  ;and pline widths. These two features are much more difficult than the rest
  ;of the program put together!! But, after some work, I came up with nice
  ;algorithms to implement these features. The solid routine will trim solids,
  ;traces and 3d-faces. It is very robust, and will handle any solid which
  ;gets copied to the detail. It does these entities in one pass, and generates
  ;as few extra entities as possible. These entities may need to be broken into
  ;several pieces, and they may need to have a wedge (bounded by an edge and an
  ;arc) filled with smaller solids or 3d-faces. Plines with width are turned
  ;into multiple solids.

  ;An even harder challenge is to trim pline widths. This is very, very hard.
  ;I decided to tackle it...by cheating!! I break up plines with width into
  ;multiple solids, (not trivial, mind you) and then pass them through the
  ;solid algorithm. Problem solved. Beauty!!

  (while
    ;Grab next entity to process, missing the ones in SKIPSS.
    (progn
      (while(and(setq ENT(entnext(cdr(assoc -1 ENT))))
	      (ssmemb ENT SKIPSS)
	      (setq ENT(entget ENT))))
      ENT)

    (redraw ENT 3)
    (setq ENT(entget ENT))

    ;*Every* entity type will get processed.

    (eval
      (cdr(assoc(cdr(assoc 0 ENT))
	    (list
	      (cons"LINE"'(D_LINE))

	      (cons"3DLINE"'(D_LINE))

	      (cons"CIRCLE"'(D_CIRCLE))

	      (cons"ARC"'(D_ARC))

	      (cons"POINT"
	        '((lambda()
		  (if(>(distance(cdr(assoc 10 ENT))CENTER)RADIUS);Point outside circle?
		    (entdel(cdr(assoc -1 ENT)))))))

	      (cons"DIMENSION"
	        '((lambda()
		  (setq ENT2(if(eq(type TEXT)'PICKSET)
			     (entget(entlast))))
		  (EXPLODE_ENT)

		  ;Save any text from being processed.
		  (if ENT2
		    (while(and(setq ENT2(entnext(cdr(assoc -1 ENT2))))
			    (setq ENT2(entget ENT2))
			    (/=(cdr(assoc 0 ENT2))"TEXT"))))
		  (if ENT2
		    (ssadd(cdr(assoc -1 ENT2))SKIPSS)))))

	      (cons"INSERT"'(D_INSERT))

	      (cons"ATTDEF"
	        '((lambda()
		  (if(zerop(logand FLAGS 4))
		    (entmod(subst(cons 40(/(cdr(assoc 40 ENT))SCALE))
		      (assoc 40 ENT)ENT))))))

	      (cons"TEXT"'(D_TEXT))

	      (cons"SHAPE"'(D_TEXT))

	      (cons"3DFACE"'(TEST_SOLID T))

	      (cons"SOLID"'(TEST_SOLID nil))

	      (cons"TRACE"'(TEST_SOLID nil))

	      (cons"POLYLINE"'(D_PLINE))))))

    (if ENT
      (redraw(cdr(assoc -1 ENT))4)))

  (command)

  (if(eq(logand FLAGS 2)2)
    (progn
      (prompt(strcat"\nWblocking detail to "STR"...."))
      (command".WBLOCK" STR)
      (if(setq FH(open(strcat STR".DWG")"r"))
	(progn(setq FH(close FH))
	  (command"Yes")))
      (command "" CENTER)
      (while(setq ENTLST(entnext ENTLST))
	(command ENTLST))
      (command"")))

  (SYSVAR nil nil)
  (redraw(cdr(assoc -1 CIR2))4)

  (prompt"\nDone.")
  (princ))

(prompt".")

;Functions used by C:DETAIL.

;May need to trim one or both ends of a line.
(defun D_LINE()
  ;One end outside detail?
  (if(>=(distance(cdr(assoc 10 ENT))CENTER)RADIUS)
    ;Both ends outside detail?
    (if(>=(distance(cdr(assoc 11 ENT))CENTER)RADIUS)
      ;Entire line outside detail?
      (if(CLOSEST(cdr(assoc 10 ENT))(cdr(assoc 11 ENT)))
	;Trim both ends.
	(command(list(cdr(assoc -1 ENT))(cdr(assoc 10 ENT)))
	  (list(cdr(assoc -1 ENT))(cdr(assoc 11 ENT))))
	;Delete line.
	(entdel(cdr(assoc -1 ENT))))
      ;Trim one end.
      (command(list(cdr(assoc -1 ENT))(cdr(assoc 10 ENT)))))
    ;Other end outside detail?
    (if(>=(distance(cdr(assoc 11 ENT))CENTER)RADIUS)
      ;Trim other end.
      (command(list(cdr(assoc -1 ENT))(cdr(assoc 11 ENT)))))))

;May need to trim circle once or delete it.
(defun D_CIRCLE( / PT1)
  ;Is the point on the circle closest to the detail center, outside?
  (if(>=(distance
	  (polar(cdr(assoc 10 ENT))
	    (angle(cdr(assoc 10 ENT))CENTER)
	    (cdr(assoc 40 ENT)))
	  CENTER)
       RADIUS)
    ;Delete circle.
    (entdel(cdr(assoc -1 ENT)))
    ;Is the point on the circle farthest from the detail center, outside?
    (if(>=(distance
	   (setq PT1(polar(cdr(assoc 10 ENT))
		      (angle CENTER(cdr(assoc 10 ENT)))
		      (cdr(assoc 40 ENT))))
	   CENTER)
	 RADIUS)
      ;Trim circle.
      (command(list(cdr(assoc -1 ENT))PT1)))))

(prompt".")

;May need to trim one or both ends of arc.
(defun D_ARC( / PT1 PT2)
  ;Endpoints of arc.
  (setq PT1(polar(cdr(assoc 10 ENT))(cdr(assoc 50 ENT))(cdr(assoc 40 ENT)))
	PT2(polar(cdr(assoc 10 ENT))(cdr(assoc 51 ENT))(cdr(assoc 40 ENT))))
  ;One endpoint outside detail?
  (if(>=(distance PT1 CENTER)RADIUS)
    ;Other end outside detail?
    (if(>=(distance PT2 CENTER)RADIUS)
      (progn
	;Trim both ends of arc.
	(command(list(cdr(assoc -1 ENT))PT1)(list(cdr(assoc -1 ENT))PT2))
	;If arc is unchanged, delete it.
	(if(equal ENT(entget(cdr(assoc -1 ENT))))
	  (entdel(cdr(assoc -1 ENT)))))
      ;Trim one end.
      (command(list(cdr(assoc -1 ENT))PT1)))
    ;Other end outside detail?
    (if(>=(distance PT2 CENTER)RADIUS)
      ;Trim other end.
      (command(list(cdr(assoc -1 ENT))PT2))
      ;Both ends are inside detail.
      ;Trim point on arc farthest from detail center, just in case.
      (command(list(cdr(assoc -1 ENT))
		(polar(cdr(assoc 10 ENT))
		  (angle CENTER(cdr(assoc 10 ENT)))
		  RADIUS))))))

(defun D_TEXT()
  (if(or(null TEXT)
       (not(ssmemb(cdr(assoc -1 ENT))TEXT)))
    (entdel(cdr(assoc -1 ENT)))
    (if(zerop(logand FLAGS 4))
      (entmod(subst(cons 40(/(cdr(assoc 40 ENT))SCALE))
	(assoc 40 ENT)ENT)))))

(prompt".")

;Explode block. If there are attributes, offer to save their values as text.
(defun D_INSERT( / MODE TEMP)
  (cond
    ((not(=(cdr(assoc 41 ENT))(cdr(assoc 42 ENT))(cdr(assoc 43 ENT))))
      (prompt"\nCan't explode block with unequal scales."))
    ((and(assoc 66 ENT)
       (eq(logand(cdr(assoc 66 ENT))1)1)
       (progn
	 (redraw(cdr(assoc -1 ENT))3)
	 (initget"Yes No")
	 (command"")
	 (/=(getkword"\nBlock has attributes. Save attribute values as text? <Yes>: ")
	   "No"))
	(setq MODE T)
	(ATT2TEXT)
	nil))
    ((setq TEMP(entlast))
      (EXPLODE_ENT)
      (if(and(assoc 66 ENT)
	   (eq(logand(cdr(assoc 66 ENT))1)1)
	   (null MODE)
	   (null(initget"Yes No"))
	   (eq(getkword"\nDelete attribute definitions? <Yes>: ")"No"))
	(setq TEMP(entlast)))
      (while(setq TEMP(entnext TEMP))
	(if(eq(cdr(assoc 0(entget TEMP)))"ATTDEF")
	  (entdel TEMP)))
      (REDRAW_SS))))

(defun EXPLODE_ENT()
  (command)
  (command".EXPLODE"(cdr(assoc -1 ENT))".TRIM"(cdr(assoc -1 CIR2))""))

;Turn a block's attributes into text.
(defun ATT2TEXT()
  (if(/=(type SS)'PICKSET)
    (setq SS(ssadd)))
  (while(/=(cdr(assoc 0(setq ENT(entget(entnext(cdr(assoc -1 ENT)))))))
	  "SEQEND")
    (command".TEXT"(getvar"VIEWCTR")"" "" "%%u")
    (ssadd(entlast)SS)
    (ssadd(entlast)SKIPSS)
    (if(zerop(logand FLAGS 4))
      (setq ENT(subst(cons 40(/(cdr(assoc 40 ENT))SCALE))
		  (assoc 40 ENT)ENT)))
    (COPY_PROPS
      (LIST_PROPS'(1 6 7 8 10 11 38 39 40 41 50 51 62 71 72 230))
      (entget(entlast))))
  (setq ENT(entget(cdr(assoc -2 ENT)))))

(prompt".")

;Find intersection between circle and line.
;PT1 is outside circle, PT2 is inside.
(defun CIRCLE_LINE(PT1 PT2 / TEMP TEMP2)
  (setq TEMP(inters PT1 PT2
	      CENTER(polar CENTER(+(angle PT1 PT2)(* pi 0.5))1.0)
	      nil)
	TEMP2(distance CENTER TEMP))
  (polar TEMP
    (angle PT2 PT1)
    (sqrt(-(* RADIUS RADIUS)(* TEMP2 TEMP2)))))

;Return point closest to detail center, or nil if it's outside detail.
(defun CLOSEST(PT1 PT2)
  (inters PT1 PT2
    (polar CENTER(+(angle PT1 PT2)(* pi 0.5))RADIUS)
    (polar CENTER(-(angle PT1 PT2)(* pi 0.5))RADIUS)))

(defun REDRAW_SS( / ENT)
  (if(eq(type SS)'PICKSET)
    (while(setq ENT(ssname SS 0))
      (redraw ENT 1)
      (ssdel ENT SS)))
  (setq SS nil))

;Return list of property values from ENT for each GROUP in LST1.
(defun LIST_PROPS(LST1)
  (mapcar'(lambda(GROUP)(assoc GROUP ENT))
    LST1))

;Copy properties passed in LST1, into ENT.
(defun COPY_PROPS(LST1 ENT / TEMP)
  (foreach GROUP LST1
    (if GROUP
      (setq ENT
	(if(setq TEMP(assoc(car GROUP)ENT))
	  (subst GROUP TEMP ENT)
	  (cons GROUP ENT)))))
  (entmod ENT))

(prompt".")

;My standard system variable routine.
;Uses SYSLST to store old settings.
(defun SYSVAR(SYM MODE)
  (cond
    (MODE
      (if(assoc SYM SYSLST)
	(if(null(cdr(assoc SYM SYSLST)))
	  (setq SYSLST(subst(cons SYM(getvar SYM))(cons SYM nil)SYSLST)))
	(setq SYSLST(cons(cons SYM(getvar SYM))SYSLST)))
      (setvar SYM MODE))
    (SYM
      (if(and(setq MODE(assoc SYM SYSLST))(cdr MODE))
	(progn(setvar SYM(cdr MODE))
	  (setq SYSLST(subst(cons SYM nil)MODE SYSLST)))))
    (T(foreach MODE SYSLST
	(if(cdr MODE)(setvar(car MODE)(cdr MODE))))
      (setq SYSLST nil))))


;The rest of the routine is used for the solid and wide pline features.

;Trim a solid, trace or 3d-face.
;MODE is T for a 3d-face, nil for a solid or trace.
;It's needed because the meaning of groups 12 & 13 are switched.
(defun TEST_SOLID(MODE / LST3)

  ;Build LST3 with outside/inside flag for each corner.
  (foreach PT1(if MODE '(13 12 11 10) '(12 13 11 10))
    (setq LST3(cons(>(distance(cdr(assoc PT1 ENT))CENTER)
		     RADIUS)
		LST3)))

  ;Is solid not entirely inside detail?
  (if(not(equal LST3'(nil nil nil nil)))
    (TEST_SOLID2)))

(defun TEST_SOLID2( / CNT LST1 LST2 TEMP)
  (setq LST2(if MODE '(10 11 12 13 10 11) '(10 11 13 12 10 11))
	LST3(append LST3(list(car LST3)))
	CNT 0)

  ;Examine each edge. Build LST1 with every corner needed to define the new
  ;polygon. The corners could be endpoints of ENT, or intersections between
  ;an edge and the circle. As many new corners as neccessary are added to LST1.
  ;If an edge passes outside the detail, a wedge is needed. The value nil is
  ;added to flag the need for a wedge fill between two corners.
  (repeat 4
    (if(null(nth CNT LST3));Is corner inside detail?
      (progn
	(setq LST1(cons(cdr(assoc(nth CNT LST2)ENT))LST1));Add corner to LST1.
	(if(nth(1+ CNT)LST3);Is next corner outside detail?
	  ;Add single intersection between edge and circle.
	  (setq LST1(cons nil
		      (cons(CIRCLE_LINE
			     (cdr(assoc(nth(1+ CNT)LST2)ENT))
			     (cdr(assoc(nth CNT LST2)ENT)))
			LST1)))))
      (if(nth(1+ CNT)LST3);Is next corner also outside circle?
	;Does edge intersect with circle?
	(if(setq TEMP(CLOSEST(cdr(assoc(nth CNT LST2)ENT))
		       (cdr(assoc(nth(1+ CNT)LST2)ENT))))
	  ;Add two intersections to LST1.
	  (setq LST1(append(list nil
			     (CIRCLE_LINE
			       (cdr(assoc(nth(1+ CNT)LST2)ENT))
			       TEMP)
			     (CIRCLE_LINE
			       (cdr(assoc(nth CNT LST2)ENT))
			       TEMP))LST1)))
	;Add single intersection to LST1.
	(setq LST1(cons(CIRCLE_LINE
			 (cdr(assoc(nth CNT LST2)ENT))
			 (cdr(assoc(nth(1+ CNT)LST2)ENT)))
		    LST1))))
    (setq CNT(1+ CNT)))

  ;Is solid not entirely outside detail?
  (if LST1
    (TRIM_SOLID)
    (entdel(cdr(assoc -1 ENT)))))

(prompt".")

(defun TRIM_SOLID( / PT1 SS2)
  (setq LST1(append(list(last LST1))LST1)
	LST2 nil LST3 nil CNT -1)

  ;Build LST3 with ordered coords for a single solid.
  ;Build LST2 with complete sets of coords for each solid.
  (repeat(1-(length LST1))
    (if(setq PT1(nth(setq CNT(1+ CNT))LST1));Corner?
      (if(eq(length(setq LST3(cons PT1 LST3)))4);Is LST3 complete?
	(setq LST2(cons LST3 LST2);Add LST3 to LST2.
	      ;Initialize LST3 for next solid. Neighbouring solids share an edge.
	      LST3(list PT1(last LST3))))

      ;Fill a wedge between last & next corners.
      (WEDGE(nth(1- CNT)LST1)(nth(1+ CNT)LST1))))

  (setq SS2 SS SS nil)
  (if(eq(length LST3)3)
    (setq LST2(cons(cons(car LST3)LST3)LST2)))
  (COPY_SOLID(length LST2));Generate needed # of solids.
  (setq LST3(if MODE '(10 11 12 13) '(10 11 13 12))
	CNT -1)

  ;Copy new corners into new entities.
  (foreach LST1 LST2
    (setq ENT2(entget(ssname SS(setq CNT(1+ CNT)))))
    (ssadd(cdr(assoc -1 ENT2))SKIPSS)
    (COPY_PROPS
      (append(mapcar 'cons LST3 LST1)
	(list(assoc 6 ENT)(assoc 8 ENT)(assoc 38 ENT)
	  (assoc 39 ENT)(assoc 62 ENT)(assoc 210 ENT)))
      ENT2))

  (REDRAW_SS)
  (setq SS SS2 SS2 nil)
  (REDRAW_SS))

(defun D_PLINE( / ENT2 ENTLST LST1 MODE)
  (cond

    ;3d-mesh?
    ((eq(logand(cdr(assoc 70 ENT))16)16)
      (EXPLODE_ENT))

    ;Are there no widths?
    ((progn
       (while(and(/=(cdr(assoc 0(setq ENT(entget(entnext(cdr(assoc -1 ENT)))))))
		   "SEQEND")
	       (=(cdr(assoc 40 ENT))(cdr(assoc 41 ENT))0.0)))
       (eq(cdr(assoc 0 ENT))"SEQEND"))
      (setq ENT(entget(cdr(assoc -2 ENT))))
      (EXPLODE_ENT))

    ;Pline has widths, explode into solids.
    (T
      (command)

      ;Reset ENT to pline ename.
      (while(/=(cdr(assoc 0(setq ENT(entget(entnext(cdr(assoc -1 ENT)))))))
	      "SEQEND"))

      ;Scale widths, if needed.
      (if(or(null PLINES)
	   (not(ssmemb(cdr(assoc -2 ENT))PLINES)))
	(progn
	  (setq ENT(entget(cdr(assoc -2 ENT))))
	  (while(/=(cdr(assoc 0(setq ENT(entget(entnext(cdr(assoc -1 ENT)))))))
		  "SEQEND")
	    (entmod(subst(cons 40(/(cdr(assoc 40 ENT))SCALE))
		     (assoc 40 ENT)
	      (subst(cons 41(/(cdr(assoc 41 ENT))SCALE))
		(assoc 41 ENT)ENT))))
	  (entupd(cdr(assoc -2 ENT)))))

      (setq ENT(entget(cdr(assoc -2 ENT)))
	    ENTLST(entlast))

      ;Loop for each vertex.
      ;LST1 is used to hold the beveled coords of the 2nd last vertex,
      ;and the unbeveled coords of the last vertex.
      (while(/=(cdr(assoc 0(setq ENT(entget(entnext(cdr(assoc -1 ENT)))))))
	      "SEQEND")
	(if(or(null(assoc 42 ENT))(zerop(cdr(assoc 42 ENT))));no bulge?
	  (D_WP_LINE)
	  (D_WP_ARC)))
      (entdel(cdr(assoc -2 ENT)))
      (while(setq ENTLST(entnext ENTLST))
	(redraw ENTLST 1))))

  (command)
  (command".TRIM"(cdr(assoc -1 CIR2))""))

(prompt".")

;Make solids from wide pline segment.
(defun D_WP_LINE( / ANG LST2 PT1 PT2)
  (if(and(setq PT1(cdr(assoc 10 ENT))
	       ENT2(entget(entnext(cdr(assoc -1 ENT)))))
       (/=(cdr(assoc 0 ENT2))"SEQEND"))
    (setq PT2(cdr(assoc 10 ENT2))
	  ANG(list(angle PT1 PT2)(* pi 0.5))

	  ;Build LST2 with coords of unbeveled box for segment.
	  LST2(mapcar
		'(lambda(PT1 SYM GROUP)
		   (polar PT1
		     (apply SYM ANG)
		     (*(cdr(assoc GROUP ENT))0.5)))
		(list PT1 PT1 PT2 PT2)
		'(+ - + -)
		'(40 40 41 41)))
    (setq LST2 nil))

  ;Use LST2 to modify LST1.
  (if LST2
    (if LST1
      (if(BEVEL_TEST
	   (D_MIDPT(car LST1)(cadr LST1))
	   PT1
	   PT2)

	;Bevel.
	(setq LST1(list
		    (inters(car LST1)(caddr LST1)
		      (car LST2)(caddr LST2)nil)
		    (inters(cadr LST1)(cadddr LST1)
		      (cadr LST2)(cadddr LST2)nil)
		    (caddr LST2)
		    (cadddr LST2)))

	;No bevel.
	(progn
	  (command(caddr LST1)(cadddr LST1)""".SOLID")
	  (setq LST1 LST2)))

      (progn
	(setq ENT2 ENT
	      LST1(if(and MODE	;Was an arc the last segment?
		       (BEVEL_TEST
			 (polar PT1 MODE 1.0)
			 PT1
			 PT2))

		    ;Bevel.
		    (list(inters PT1(polar PT1 MODE 1.0)
			   (car LST2)(caddr LST2)nil)
		      (inters PT1(polar PT1 MODE 1.0)
			(cadr LST2)(cadddr LST2)nil)
		      (caddr LST2)
		      (cadddr LST2))

		    ;No bevel.
		    LST2)
	      MODE nil)
	(command".SOLID")))
    (setq LST1(cddr LST1)))

  (command(car LST1)(cadr LST1))

  (if(/=(cdr(assoc -1 ENT))(cdr(assoc -1 ENT2)))
    (progn
      (COPY_PROPS
	(list(assoc 6 ENT2)(assoc 8 ENT2)(assoc 38 ENT2)
	  (assoc 39 ENT2)(assoc 62 ENT2)(assoc 210 ENT2))
	(entget(entlast)))
      (setq ENT2 ENT))))

(prompt".")

;Approximate Pline arc with width using solids.
(defun D_WP_ARC( / ANG BULGE CNT PT1 PT2 PT3 RADIUS TEMP TEMP2 WIDTH WIDTH2)

  (setq PT1(cdr(assoc 10 ENT))
	PT2(cdr(assoc 10(entget(entnext(cdr(assoc -1 ENT))))))
	BULGE(cdr(assoc 42 ENT))
	PT3(polar PT1	;Center of arc.
	     (apply(if(minusp BULGE)'-'+)
	       (list(angle PT1 PT2)
		 (-(* pi 0.5)(*(atan(abs BULGE))2))))
	     (/(distance PT1 PT2)
	       (sin(*(atan(abs BULGE))2))2))
	RADIUS(distance PT1 PT3)
	BULGE(* 4(atan BULGE))
	CNT(fix(abs(/(* BULGE 4(getvar"SPLINESEGS"))pi)))
	TEMP(/ BULGE CNT);Angle increment
	WIDTH(*(cdr(assoc 40 ENT))0.5)
	WIDTH2(*(cdr(assoc 41 ENT))0.5))
  (if(> WIDTH RADIUS)
    (setq WIDTH RADIUS))
  (if(> WIDTH2 RADIUS)
    (setq WIDTH2 RADIUS))
  (setq TEMP2(/(- WIDTH2 WIDTH)CNT);Width increment.
	ANG(angle PT3 PT1))

  ;May need to finish off last line segment before making arc.
  (if LST1
    (progn
      (if(BEVEL_TEST
	   (D_MIDPT(car LST1)(caddr LST1))
	   PT1
	   (polar PT1 ANG 1.0))
	(command
	  (inters(car LST1)(caddr LST1)
	    PT3(polar PT3 ANG 1.0)nil)
	  (inters(cadr LST1)(cadddr LST1)
	    PT3(polar PT3 ANG 1.0)nil))
	(command(caddr LST1)(cadddr LST1)))
      (if ENT2
	(COPY_PROPS
	  (list(assoc 6 ENT2)(assoc 8 ENT2)(assoc 38 ENT2)
	    (assoc 39 ENT2)(assoc 62 ENT2)(assoc 210 ENT2))
	  (entget(entlast))))
      (setq LST1 nil)))

  (command)
  (command ".SOLID")

  (if(> CNT 1)
    (progn
      (setq LST1(LIST_PROPS'(6 8 38 39 62 210)))
      (command(polar PT3(angle PT3 PT1)(+ RADIUS WIDTH))
	(polar PT3(angle PT3 PT1)(- RADIUS WIDTH)))
      (repeat CNT
	(setq ANG(+ ANG TEMP)
	      WIDTH(+ WIDTH TEMP2))
	(command(polar PT3 ANG (+ RADIUS WIDTH))
	  (polar PT3 ANG (- RADIUS WIDTH)))
	(COPY_PROPS LST1 (entget(entlast))))
      (setq LST1 nil)))

  (command"")


  (setq MODE(angle PT3 PT2)))

(prompt".")

;Fill a wedge bounded by an arc and a line with several solids or 3d-faces.
(defun WEDGE(PT1 PT2 / ANG CNT ENT2 LST1 LST2 PT3 TEMP)

  ;Figure # of segments needed to fill the wedge. I use the coord length & trig
  ;to avoid quadrant messes when dealing with angles.
  (setq CNT(fix(abs(/(*(ASIN(/(distance PT1 PT2)2 RADIUS))
		       8
		       (getvar"SPLINESEGS"))
		     pi))))
  (if(> CNT 1)
    (progn
      (setq ENT2(entlast)
	    TEMP(/(*(ASIN(/(distance PT1 PT2)RADIUS 2))2)CNT)
	    ;Figure starting angle. Again, use trig to avoid quadrant hassles.
	    ANG(-(angle CENTER(D_MIDPT PT1 PT2))
		 (ASIN(/(distance PT1 PT2)2 RADIUS))))

      (command)
      (command(if MODE ".3DFACE" ".SOLID"))

      ;LST1 & LST2 will be alternated when supplying points to the command.
      ;This lets me account for the different point orders of solids & 3dfaces.
      (setq LST1'((inters CENTER(setq PT3(polar CENTER ANG RADIUS))
		    PT1 PT2 nil)
		    PT3)
	    LST2(if MODE
		  '((setq PT3(polar CENTER ANG RADIUS))
		     (inters CENTER PT3 PT1 PT2 nil))
		  '((inters CENTER(setq PT3(polar CENTER ANG RADIUS))
		      PT1 PT2 nil)
		      PT3)))

      ;Make wedge.
      (repeat(1+ CNT)
	(apply'command(mapcar'eval(if(minusp CNT)LST2 LST1)))
	(setq ANG(+ ANG TEMP)CNT(* CNT -1)))
      (command)
      (if(/=(type SS)'PICKSET)
	(setq SS(ssadd)))
      (setq TEMP(LIST_PROPS'(6 8 38 39 62 210))
	    CNT 1)

      ;Copy ENT's properties to wedge.
      (while(setq ENT2(entnext ENT2))
	(ssadd ENT2 SKIPSS)
	(ssadd ENT2 SS)
	(COPY_PROPS
	  (if MODE
	    (if(minusp CNT)
	      (cons'(70 . 7)TEMP)
	      (cons'(70 . 13)TEMP))
	    TEMP)
	  (entget ENT2))
	(setq CNT(* CNT -1)))
      (command".TRIM"(cdr(assoc -1 CIR2))""))))

;Create new solids or 3d-faces, to be modified later.
(defun COPY_SOLID(CNT / TEMP)
  (setq TEMP(getvar"VIEWCTR"))
  (if(/=(type SS)'PICKSET)
    (setq SS(ssadd)))
  (command)
  (command(if(eq(cdr(assoc 0 ENT))"3DFACE")".3DFACE"".SOLID")TEMP TEMP)
  (repeat CNT
    (command TEMP TEMP)
    (ssadd(entlast)SS))
  (command""".TRIM"(cdr(assoc -1 CIR2))"")
  (entdel(cdr(assoc -1 ENT))))

(defun ASIN(ANG)
  (atan(/ ANG(sqrt(abs(- 1(* ANG ANG)))))))

;Test plines with width to see if a bevel is needed.
(defun BEVEL_TEST(PT1 PT2 PT3)
  (>(/(*(ASIN(/(distance PT1(polar PT2(angle PT2 PT3)(distance PT1 PT2)))
	       (*(distance PT1 PT2)2)))
	360)pi)
    29.955))

(defun D_MIDPT(PT1 PT2)
  (mapcar'(lambda(PT1 PT2)(/(+ PT1 PT2)2))
    PT1 PT2))

(prompt"Done.")
(princ)
