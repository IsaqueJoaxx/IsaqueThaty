;LALIST displays curve data for an arc, or line data for a line
; Terrible Software c 1990 or so - our motto: Do the hard part only once
; CompuServe message: This here's FREEWARE - warranty? you gotta be kidding,
; no way! Maybe I'll update this to work on selected polyline segments, using
; NENTSEL, then I'll upload a new version, and it'll still be FREE.
; Comments, hacks, etc. are welcome: W. Kiernan, CompuServe ID 71331,3510
(defun C:LALIST( / scr rad delta hdelta pt pc rp cname cdata ch coshd lalerr )
 (setq cname nil lalerr *error* scr (getvar "screenmode"))
 (defun *error*(msg)
  (setq *error* lalerr)
  (if (/= "Function cancelled" msg)
   (princ (strcat "\nERROR: " msg ". "))
  )
  (prin1)
 )
 (while (null cname)
  (setq cname (entsel "\nSelect curve or line: "))
  (if (= scr 1)
   (textscr)
  )
  (cond
   ((null cname)(princ "  Nothing selected. Try again. "))
   ((= "ARC" (cdr (assoc 0 (setq cdata (entget (setq cname (car cname)))))))
    (redraw cname 3)
    ($larc$ cdata)
    (getstring "   Press <CR> to continue...")
    (redraw cname 4)
   )
   ((= "LINE" (cdr (assoc 0 cdata)))
    (redraw cname 3)
    ($llin$ cdata)
    (getstring "   Press <CR> to continue...")
    (redraw cname 4)
   )
   (T (princ "  Neither arc nor line. Try again. ")(setq cname nil))
  )
 )
 (if (= scr 1)
  (graphscr)
 )
 (setq *error* lalerr)
 (prin1)
)
(defun $llin$(cdata)
 (princ
  (strcat
   "\Bearing: "
   (angtos
    (angle
     (setq pc (cdr (assoc 10 cdata)))
     (setq pt (cdr (assoc 11 cdata)))
    )
    4 6
   )
   "    Length: "
   (rtos (distance pc pt) 2 5)
  )
 )
 (princ
  (strcat
   "\n Beginning  N: "
   (rtos (cadr pc) 2 5)
   "   E: "
   (rtos (car pc) 2 5)
  )
 )
 (princ
  (strcat
   "\n End        N: "
   (rtos (cadr pt) 2 5)
   "   E: "
   (rtos (car pt) 2 5)
  )
 )
)
(defun $larc$(cdata)
 (princ
  (strcat
   "\nRadius: "
   (rtos (setq rad (cdr (assoc 40 cdata))) 2 5)
   "    Arc degree of curve: "
   (angtos (/ 100.0 rad) 1 6)
   "    Delta: "
   (angtos
    (if 
     (<
      (setq delta
       (- (setq pt (cdr (assoc 51 cdata)))(setq pc (cdr (assoc 50 cdata))))
      )
      0
     )
     (setq delta (+ delta pi pi))
     delta
    )
    1 6
   )
  )
 )
 (princ
  (strcat
   "\nArc: "
   (rtos (* rad delta) 2 5)
   "    Tangent: "
   (rtos
    (*
     rad 
     (/
      (sin (setq hdelta (/ delta 2.0)))
      (setq coshd (cos hdelta))
     )
    )
    2 5
   )
   "    Chord: "
   (angtos
    (angle
     (setq pc
      (polar (setq rp (cdr (assoc 10 cdata))) pc rad)
     )
     (setq pt (polar rp pt rad))
    )
    4 6
   )
   ", "
   (setq ch (rtos (distance pc pt) 2 5))
  )
 )
 (princ
  (strcat
   "\nMiddle ordinate: "
   (rtos (* (- 1.0 coshd) rad) 2 5)
   "  External: "
   (if (= 0.0 coshd)
    "-> infinity"
    (rtos (* rad (- (/ 1.0 coshd) 1.0)) 2 5)
   ) 
  )
 )    
 (princ
  (strcat
   "\n Radius point     N: "
   (rtos (cadr rp) 2 5)
   "   E: "
   (rtos (car rp) 2 5)
  )
 )
 (princ
  (strcat
   "\n Beginning (ccw)  N: "
   (rtos (cadr pc) 2 5)
   "   E: "
   (rtos (car pc) 2 5)
  )
 )
 (princ
  (strcat
   "\n End (ccw)        N: "
   (rtos (cadr pt) 2 5)
   "   E: "
   (rtos (car pt) 2 5)
  )
 )
)
