;
;       Display spiral
;
;       Designed and implemented by Kelvin R. Throop on 1985 January 85
;
;       (cspiral <# rotations> <base point> <growth per rotation>
;                <points per circle>)
;
(defun cspiral (ntimes bpoint cfac lppass / ang dist tp ainc dinc circle bs cs)
        (setq cs (getvar "cmdecho"))
        (setq bs (getvar "blipmode"))
        (setvar "blipmode" 0)
        (setvar "cmdecho" 0)
        (setq circle (* 3.141596235 2))
        (setq ainc (/ circle lppass))
        (setq dinc (/ cfac lppass))
        (setq ang 0.0)
        (setq dist 0.0)
        (command "pline" bpoint)
        (repeat ntimes
           (repeat lppass
              (setq tp (polar bpoint (setq ang (+ ang ainc))
                          (setq dist (+ dist dinc))))
              (command tp)
           )
        )
        (command)
        (setvar "blipmode" bs)
        (setvar "cmdecho" cs)
        nil
)
;
;       Interactive spiral generation
;
(defun C:SPIRAL ( / nt bp cf lp)
        (prompt "\nCentre point: ")
        (setq bp (getpoint))
        (prompt "\nNumber of rotations: ")
        (setq nt (getint))
        (prompt "\nGrowth per rotation: ")
        (setq cf (getdist bp))
        (prompt "\nPoints per rotation: ")
        (setq lp (getint))
        (cond ((null lp) (setq lp 30)))
        (cspiral nt bp cf lp)
)
