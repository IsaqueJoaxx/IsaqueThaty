;SPHERE.LSP v1.0
;
;There are no restrictions on the variables.
;However "Resolution" should be kept to whole
;integers. Suggested Resolution is 10.
;
;Because there is a extensive use of lines the HIDE command
;will not produce the desired results. Later versions will
;correct for this.
;
;For the latest version send  $5.00(US) to:
;
;           Kevin Reardon
;           536 41 St. #27
;           Oakland, CA   94609
;
;
(DEFUN C:SPHERE ()
   (SETQ CEN (GETPOINT "Center: "))
   (SETQ RAD (GETDIST CEN "Radius: "))
   (SETQ RES (GETREAL "Resolution: "))
   (SETQ EE (GETVAR "ELEVATION"))
   (SETQ C 0)
   (WHILE (< C (1+ RES))
      (SETQ T (* (/ C RES) 1.570796))
      (SETQ X (* RAD (COS T)))
      (SETQ ELE (* RAD (SIN T)))
      (SETVAR "ELEVATION" (+ EE ELE))
      (COMMAND "CIRCLE" CEN X)
      (COMMAND)
      (SETVAR "ELEVATION" (- EE ELE))
      (COMMAND "CIRCLE" CEN X)
      (COMMAND)
      (SETQ C (1+ C))
         )
      (SETVAR "ELEVATION" EE)
      (COMMAND "REDRAW")
 )
;<EOF>CEN X)
      (COMMAND)
      (SETQ C (1+ C))
         )
      (SETVAR "ELEVATION" EE)
      (COMMAND "REDRA