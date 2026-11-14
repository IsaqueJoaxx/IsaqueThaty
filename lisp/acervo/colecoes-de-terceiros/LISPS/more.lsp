;
;	ACAD.LSP
;
;		Written by Hugh S. Myers of:
;
;		Bazeghi-Myers
;		8414 Fairview Ave.
;		Boise, Idaho  83704
;
;		Or contact me through CompuServe,
;		User ID: 76314,3672
;		Not too surprisingly I hang out at !go adesk...
;
;		Cautions and such...I'm giving this stuff away
;		as is...I don't plan on real heavy support(i.e. none!)
;		but would like to hear of bugs or problems, certainly
;		would like to hear if you can improve, sort of give back
;		at least as much as you get plan(does that make this
;		golden rule ware???!)  At any rate, this is not a how
;		too for Autolisp, and the comments are not as thick as
;		they might be, but persevere(or at least persist) and
;		all should be made clear.
;
;		So, here goes.....
;
;	set a few system variables of my own, useful for remembering
;	from one use of a routine to another...
;

(SETQ LAST_DIST 0.0)

(SETQ LAST_LINE (LIST 0 0))

;
;	make it a little easier to re-load this file for changes
;	and such.  note the full path name specification, so you
;	can use this from any sub-dir.
;

(DEFUN C:LISP()
	(LOAD "/ACAD/ACAD.LSP")
)

;
;	turn off command line echo etc...
;

(DEFUN ECHO_OFF()
	(SETQ ECHO (GETVAR "CMDECHO"))
	(SETVAR "CMDECHO" 0)
)

;
;	turn on command line echo etc...
;

(DEFUN  ECHO_ON()
	(SETVAR "CMDECHO" ECHO)
)

;
;	set 'LASTPOINT' to some arbitrary location
;

(DEFUN C:REL()
	(SETVAR "LASTPOINT"
		(GETPOINT "RELATIVE TO? ")
	)
)

;
;	boolean function test for 'is x a line?'
;

(DEFUN LINEP(X)
	(IF (OSNAP X "MID") T NIL)
)

;
;	get a line function using 'near' and prompted
;

(DEFUN GETLINE(/ P)
	(PROMPT "LINE? ")
	(COMMAND "OSNAP" "NEAR")
	(SETQ P (GETPOINT))
	(COMMAND "OSNAP" "OFF")
	(SETQ P P)
)

;
;	get an intersection using 'int' and prompted
;

(DEFUN GETINTER(/ P)
	(PROMPT "INTERSECTION? ")
	(COMMAND "OSNAP" "INT")
	(SETQ P (GETPOINT))
	(COMMAND "OSNAP" "OFF")
	(SETQ P P)
)

;
;	get the end of a line, using 'end' and prompted
;

(DEFUN GETEND(/ P)
	(PROMPT "END? ")
	(COMMAND "OSNAP" "END")
	(SETQ P (GETPOINT))
	(COMMAND "OSNAP" "OFF")
	(SETQ P P)
)

;
;	get a distance, prompted
;

(DEFUN GDIST()
	(GETDIST "DISTANCE? ")
)

;
;	get a point, prompted
;

(DEFUN GPOINT()
	(GETPOINT "POINT? ")
)

;
;	use break to erase a gap between two intersections
;

;
;	note for error checking these and similar functions
;	should take the following form:
;
;	(DEFUN C:FUNCTION()
;		(ECHO_OFF)
;		(SETQ LINE (GETLINE))
;		(IF (LINEP LINE)
;			(PROGN
;				function a
;				function b
;				function ...
;		NIL)
;		(ECHO_ON)
;	)
;
;
;	And of course, if you want to take things to an extreme,
;	you could also construct similar testing logic for any
;	entity you need(i.e. (IF (INT_P P!)...)  For my part, 
;	I use these without error testing at all.  Bad me!
;
(DEFUN C:GAP(/ LINE P1 P2)
	(ECHO_OFF)
	(SETQ LINE (GETLINE))
	(SETQ P1 (GETINTER))
	(SETQ P2 (GETINTER))
	(COMMAND "BREAK" LINE "F" P1 P2)
	(ECHO_ON)
)

;
;	use break to cut a line at an intersection
;

(DEFUN C:CUT(/ LINE P1 P2)
	(ECHO_OFF)
	(SETQ LINE (GETLINE))
	(SETQ P1 (GETINTER))
	(COMMAND "BREAK" LINE "F" P1 P1)
	(ECHO_ON)
)

;
;	use break to trim a line to an intersection from an end
;

(DEFUN C:TRIM(/ LINE P1 P2)
	(ECHO_OFF)
	(SETQ LINE (GETLINE))
	(SETQ P1 (GETINTER))
	(SETQ P2 (GETEND))
	(COMMAND "BREAK" LINE "F" P1 P2)
	(ECHO_ON)
)

;
;	change n from radians to degrees
;

(DEFUN RTD(N)
	(* N (/ 180.0 PI))
)

;
;	set 'a' to the angle of a line
;

(DEFUN C:GANG(/ LINE)
	(ECHO_OFF)
	(SETQ LINE (GETLINE))
	(SETQ A (RTD (ANGLE LINE (OSNAP LINE "MID"))))
	(PRINT A)
	(ECHO_ON)
)

;
;	move 'LASTPOINT' a distance along an arbitrary line
;

(DEFUN C:MOVREL(/ L A P D)
	(ECHO_OFF)
	(SETQ L (GETLINE))
	(IF (NOT L) (SETQ L LAST_LINE) (SETQ LAST_LINE L))
	(SETQ A (ANGLE L (OSNAP L "END")))
	(SETQ P (GETINTER))
	(SETQ D (GDIST))
	(IF (NOT D) (SETQ D LAST_DIST) (SETQ LAST_DIST D))
	(SETVAR "LASTPOINT" (POLAR P A D))
	(ECHO_ON)
)

;
;	now a quick and dirty insert at '@' with last block
;	at an angle of !A
;

(DEFUN C:INS@()
	(ECHO_OFF)
	(COMMAND "INSERT" "" "@" "" "" A)
	(ECHO_ON)
)

;
;	set 'LASTPOINT' to a distance perpendicular to an arbitrary line
;

(DEFUN C:MOVPERP(/ A P D)
	(ECHO_OFF)
	(SETQ LINE (GETLINE))
	(SETQ A (ANGLE LINE (OSNAP LINE "MID")))
	(SETQ A (+ A (/ PI 2)))
	(SETQ P (GPOINT))
	(SETQ D (GDIST))
	(SETVAR "LASTPOINT" (POLAR P A D))
	(ECHO_ON)
)

;
;	set 'offset' and 'half_off' to a distance, using a default
;	value of either 4.75 or last value
;

(DEFUN C:SET_OFF(/ TEMP)
	(IF (NOT D_OFF) (SETQ D_OFF 4.75) T)
	(SETQ TEMP (GETDIST (STRCAT "DISTANCE? <" 
		(RTOS D_OFF 4 8) "," (RTOS (/ D_OFF 2) 4 8) "> ")))
	(IF (NUMBERP TEMP) (SETQ D_OFF TEMP) T)
	(SETQ OFFSET (RTOS D_OFF 4 8))
	(SETQ HALF_OFF (RTOS (/ D_OFF 2.0) 4 8))
)

;
;	use copy to create a wall perpendicular to an arbitrary line
;	at a distance of 'offset'
;

(DEFUN C:WALL(/ LINE A P LINE_STR)
	(ECHO_OFF)
	(WHILE (NOT (NOT (SETQ LINE (GETLINE))))
		(SETQ P (GETPOINT "SIDE? "))
		(SETQ A (RTD (ANGLE P (OSNAP LINE "PERP"))))
		(SETQ A (+ A 180.0))
		(SETQ LINE_STR (STRCAT "@" OFFSET "<" (RTOS A 2 8)))
		(COMMAND "COPY" LINE "" LINE LINE_STR)
	)
	(ECHO_ON)
)

;
;	single shot routine to parallel a line at a distance
;	on a side.
;

(DEFUN C:PCOPY(/ LINE A P D LINE_STR)
	(ECHO_OFF)
	(SETQ LINE (GETLINE))
	(SETQ D (GDIST))
	(SETQ P (GETPOINT "SIDE? "))
	(SETQ A (RTD (ANGLE P (OSNAP LINE "PERP"))))
	(SETQ A (+ A 180.0))
	(SETQ LINE_STR (STRCAT "@" (RTOS D 4 8) "<" (RTOS A 2 8)))
	(COMMAND "COPY" LINE "" LINE LINE_STR)
	(ECHO_ON)
)

;
;	use copy to create two walls parallel to a center line
;	at a distance of 'half_off'
;

(DEFUN C:CWALL(/ LINE P1 A1 A2 LINE_A1 LINE_A2)
	(ECHO_OFF)
	(WHILE (NOT (NOT (SETQ LINE (GETLINE))))
		(SETQ P1 (ANGLE LINE (OSNAP LINE "MID")))
		(SETQ A1 (ANGTOS (+ P1 (/ PI 2)) 0 8))
		(SETQ A2 (ANGTOS (- P1 (/ PI 2)) 0 8))
		(SETQ LINE_A1 (STRCAT "@" HALF_OFF "<" A1))
		(SETQ LINE_A2 (STRCAT "@" HALF_OFF "<" A2))
		(COMMAND "COPY" LINE "" LINE LINE_A1)
		(COMMAND "COPY" LINE "" LINE LINE_A2)
	)
	(ECHO_ON)
)

;
;	set of routines to do bearings and distance's
;

;
;	first set up for north east...
;

(DEFUN C:BNE(/ A DS P1 P2)
	(ECHO_OFF)
	(WHILE (NOT (NOT (SETQ A (GETANGLE "BEARING? "))))
		(SETQ A (- (/ PI 2) A))
		(SETQ DS (* 12.0 (GDIST)))
		(SETQ P2 (POLAR (SETQ P1 (GETVAR "LASTPOINT")) A DS))
		(COMMAND "LINE" P1 P2 "")
	)
	(ECHO_ON)
)
;
;	then set up for north west...
;

(DEFUN C:BNW(/ A DS P1 P2)
	(ECHO_OFF)
	(WHILE (NOT (NOT (SETQ A (GETANGLE "BEARING? "))))
		(SETQ A (+ (/ PI 2) A))
		(SETQ DS (* 12.0 (GDIST)))
		(SETQ P2 (POLAR (SETQ P1 (GETVAR "LASTPOINT")) A DS))
		(COMMAND "LINE" P1 P2 "")
	)
	(ECHO_ON)
)
;
;	then for south east...
;

(DEFUN C:BSE(/ A DS P1 P2)
	(ECHO_OFF)
	(WHILE (NOT (NOT (SETQ A (GETANGLE "BEARING? "))))
		(SETQ A (+ (/ (* 3 PI) 2) A))
		(SETQ DS (* 12.0 (GDIST)))
		(SETQ P2 (POLAR (SETQ P1 (GETVAR "LASTPOINT")) A DS))
		(COMMAND "LINE" P1 P2 "")
	)
	(ECHO_ON)
)
;
;	and last for south west...
;

(DEFUN C:BSW(/ A DS P1 P2)
	(ECHO_OFF)
	(WHILE (NOT (NOT (SETQ A (GETANGLE "BEARING? "))))
		(SETQ A (- (/ (* 3 PI) 2) A))
		(SETQ DS (* 12.0 (GDIST)))
		(SETQ P2 (POLAR (SETQ P1 (GETVAR "LASTPOINT")) A DS))
		(COMMAND "LINE" P1 P2 "")
	)
	(ECHO_ON)
)
