;;		BEAMS
;;
;;		copyright 1987
;;		By Jeff Hester
;;
;;		Will draw a beam with a a six inch offset from the specified 
;;		endpoints.
;
;		DEFINE FUNCTION
;
(defun C:BEAMS ()
;
;		Suppress the screen echo and set certain screen variables
;
	(setq ECHO (getvar "cmdecho"))
    (setq MODE (getvar "osmode"))
	(setq ORTHO (getvar "orthomode"))
	(setvar "orthomode" 0)
	(setvar "osmode" 0)
	(command "GRAPHSCR")
;
;		Obtain start and endpoint of beam
;
	(setq STPT (getpoint "\nEnter start point:"))
    (setq EDPT (getpoint "\nEnter end point:"))
	(setq A1 (angle STPT EDPT))
    (setq A2 (angle EDPT STPT))
	(setq PT1 (polar STPT A1 6))
	(setq PT2 (polar EDPT A2 6))
;
;		Draw beam from point 6" offset from specified points
;
	(command "LINE" PT1 PT2 "")
)
