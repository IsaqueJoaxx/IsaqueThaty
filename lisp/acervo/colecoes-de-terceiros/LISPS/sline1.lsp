(defun c:sline ()
   ;
   ; Mike Pillers, P.E.     July 12, 1987
   ; Machine/ Mechanism Design Consultant
   ; MJP Mechanical Engineering & CAD Service
   ; San Jose, CA          (408) 280-7959
   ;
   ; This command will generate a typical "Cross-section" line
   ; drawn as a phantom line, complete with arrow heads at
   ; at both ends.
   ;
   ;                    AutoCAD version 2.5 is required.
   ;
  (setvar "cmdecho" 0)
  (if (not asize) (setq asize 0.50))      ;initial default arrow size
  (if (not PThk)  (setq PThk 0.030))      ;   "       "    Pline width
                
  (defun GETR (val msg / tm)
    (setq tm (getreal (strcat msg " <" (rtos val 2 4) ">: ")))
    (cond ((= (type tm) 'REAL) (eval tm))
          ((= tm nil) (eval val))
          (t (princ "\007 *error* not a REAL") (eval val)) ) )

  (defun loop ()
    (cond ((setq p2 (getpoint p1 " To point: ")) (command p2) 
                                     (setq p0 p1) (setq p1 p2) (loop))
          ( t (command "u" (polar p1 (angle p1 p0) asize)
                       "w" (/ asize 3) 0.0 p1 ""))))

  (setq asize (getr asize " Arrow head size"))
  (setq PThk  (getr PThk " Pline width"))
  (setq p1 (getpoint " From point: "))
  (command "pline" p1 "w" 0.0 0.0)
  (setq p2 (getpoint p1 " To point: "))
  (command "w" 0.0 (/ asize 3) (polar p1 (angle p1 p2) asize) 
           "w" PThk PThk p2)
  (setq p1 p2)
  (loop)
  (command "change" "l" "" "p" "lt" "phantom" "")
  (eval "Done")
)
