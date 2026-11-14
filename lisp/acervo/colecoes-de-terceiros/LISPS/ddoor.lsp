;	Tom A. Stoeckel 71521,450  His Sound, Fresno CA
;	This lisp routine will create double doors with a 2"x4" jamb on
;	each side.  All but the first selection point have osnaps set.
;	If the default of 3.0' doors is not appropriate for you, the change can
;	be made in line 3 and 4 to whatever you most commonly use.
;	It has only been used on Release 9.  
;	I would welcome comments and criticism.  This is FREEWARE.  Enjoy!

(defun C:DDOOR ( / W os P1 P2 P3 P4 P5 P6 P7 A1 A2 A3 J1 J2 p a b)
	(setvar "CMDECHO" 0)
	(setq W (getdist "\nEnter Width of Doors <3.0'>:  "))
		(if (= W nil) (setq W 36))
		(setq W (+ W 2))
	(menucmd "S=OSNAP")
	(menucmd "P1=*")
	(setq P1 (getpoint "\nEnter Center of Double Doors on Side of Opening: "))
		(setvar "lastpoint" P1)
	(setq os (getvar "OSMODE"))
	(setvar "OSMODE" 128)
	(setq P2 (getpoint "\nTouch Opposite Wall: "))
	(setvar "OSMODE" os)
	(setq A1 (angle P1 P2))
	(setq A2 (+ (* pi 1.5) A1))
	(setq P3 (polar P1 A2 W))
	(setq A3 (+ pi A2))
	(setq P4 (polar P1 A3 W))
	(setq P5 (polar P3 A1 (distance P1 P2)))
	(setq P6 (polar P4 A1 (distance P1 P2)))
	(setq J1 (polar P3 A3 2))
	(setq J2 (polar P4 A2 2))
	(setq P7 (polar J1 (+ pi A1) (- W 2)))
	(setq B1 (polar P1 A3 (/ W 2)))
	(setq B2 (polar P2 A3 (/ W 2)))
		
	(command "break" B1 "f" P3 P4)
	(command "break" B2 "f" P5 P6)

	(command "pline"
		(setq p P5)
		(setq p P3)
		(setq p (polar p A3 3.75))
		(setq a (polar p (+ pi A1) (distance J1 P7)))
		(setq p (polar a A2 1.75))
		(setq p (polar p A1 (+ W 2)))
		(setq p (polar p A2 2))
	"")

	(command "pline"
		(setq p P6)
		(setq p P4)
		(setq p (polar p A2 3.75))
		(setq b (polar p (+ pi A1) (distance J1 P7)))
		(setq p (polar b A3 1.75))
		(setq p (polar p A1(+ W 2)))
		(setq p (polar p A3 2))
	"")
	(command "arc" P1 "e" a "d" (polar P1 (+ pi A1) (- W 2)))
	(command "arc" P1 "e" b "d" (polar P1 (+ pi A1) (- W 2)))

	(prin1)
)