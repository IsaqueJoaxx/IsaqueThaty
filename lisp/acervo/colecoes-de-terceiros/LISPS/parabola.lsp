;==========================================================
; PARABOLA.LSP Copyright 1992 by Looking Glass Microproducts
;==========================================================
(if (null PARABOLA_SEGS)
   (setq PARABOLA_SEGS 12)  ; default number of parabola segments
)
(defun C:PARABOLA (/ ERROR PUSHVARS POPVARS NOTRANS PARABOLA SQR
                   MIDPOINT 2D RTOD SYSVARS)
   ;==========================================================
   ; Error Handler
   (defun ERROR (S)
      (if (not
             (member
                S
                '("Function cancelled" "console break")
             )
          )
         (princ S)
      )
      (command "_undo" "end")
      (command "_undo" "1")
      (POPVARS)
   )
   ;==========================================================
   ; Set and Save System Variables
   (defun PUSHVARS (VLIST)
      (foreach PAIR VLIST
         (setq
            SYSVARS (cons
                       (cons
                          (strcase (car PAIR))
                          (getvar
                             (car PAIR)
                          )
                       )
                       SYSVARS
                    )
         )
         (if (cdr PAIR) (setvar (car PAIR) (cdr PAIR)))
      )
      t
   )
   ;==========================================================
   ; Restore System Variables
   (defun POPVARS ()
      (foreach PAIR SYSVARS (setvar (car PAIR) (cdr PAIR)))
      (setq
         *error* OLD-ERROR
      )
      (setq SYSVARS nil)
      (princ)
   )
   ;==========================================================
   ; Restore a single system variable from stack
   (defun RESTORE (VARNAME / OLD-VALUE)
      (if (setq
             OLD-VALUE (cdr (assoc (strcase VARNAME) SYSVARS))
          )
         (setvar VARNAME OLD-VALUE)
      )
   )
   ;==========================================================
   ; Disallow transparent invocation of routine.
   (defun NOTRANS ()
      (cond
         ((zerop (logand (getvar "cmdactive") (+ 1 2 4 8))))
         ((alert
             "This command may not be invoked transparently."
          )
         )
      )
   )
   ;===========================================================
   ; Square function
   (defun SQR (X) (* X X))
   ;===========================================================
   ; Midpoint between p1 and p2
   (defun MIDPOINT (P1 P2)
      (mapcar '(lambda (X1 X2) (* 0.5 (+ X1 X2))) P1 P2)
   )
   ;===========================================================
   ; Set z to zero
   (defun 2D (P) (mapcar '* P '(1 1)))
   ;==========================================================
   ; Radians to degrees
   (defun RTOD (X) (/ (* 180.0 X) pi))
   ;===========================================================
   ; extended getint
   (defun XGETINT (PRMPT DEFAULT)
      (cond
         ((getint (strcat PRMPT " <" (itoa DEFAULT) ">: ")))
         (DEFAULT
         )
      )
   )
   ;============================================================
   ; List of points on parabola with vertex at p0 through p1
   (defun PARAB (P0 P1 / X0 X1 Y0 Y1 A DX POINTS X Y)
      (setq X0 (car P0) Y0 (cadr P0) X1 (car P1) Y1 (cadr P1))
      (if (/= X0 X1)
         (progn
            (setq
               A      (/ (- Y1 Y0) (SQR (- X1 X0)))
               DX     (/ (* 2 (- X1 X0)) PARABOLA_SEGS)
               POINTS (list (list X1 Y1))
               X      X1
            )
            (repeat
               PARABOLA_SEGS
               (setq
                  X      (- X DX)
                  Y      (+ (* A (SQR (- X X0))) Y0)
                  POINTS (cons (list X Y) POINTS)
               )
            )
            POINTS
         )
      )
   )
   ;==========================================================
   ; Parabola main routine
   (defun PARABOLA (/ P0 P1 P1W P2 P3 P3W POINTS ANG)
      (graphscr)
      (initget 1 "Segments")
      (setq
         P0 (getpoint "\nSegments/<start point>: ")
      )
      (if (= "Segments" P0)
         (progn
            (initget 6) ; disallow zero, negative
            (setq
               PARABOLA_SEGS (*
                                (/
                                   (1+
                                      (XGETINT
                                         "\nNumber of segments"
                                         PARABOLA_SEGS
                                      )
                                   )
                                   2
                                )
                                2
                             )
            )
            (initget 1) ; disallow nil input
            (setq P0 (getpoint "\nStart point: "))
         )
      )
      (setq P0 (2D P0))
      ;
      (initget 1)
      (setq P1 (2D (getpoint P0 "\nEnd point: ")))
      (while (equal P0 P1)
         (prompt
            "\nPoints must be distinct."
         )
         (initget 1)
         (setq
            P1 (2D (getpoint P0 "\nTry again: "))
         )
      )
      ;
      (setq P2 (MIDPOINT P0 P1) ANG (angle P0 P1))
      (setvar
         "blipmode" 0
      )
      (command "_snap" "rotate" P2 (RTOD ANG))
      (RESTORE
         "snapmode"
      )
      (RESTORE "blipmode")
      (setvar "orthomode" 1)
      ;
      (grdraw P0 P1 -1)
      (initget 1) ; disallow nil zero inputs
      (setq P3 (getpoint P2 "\nVertex: "))
      (grdraw P0 P1 -1)
      ;
      (command "_undo" "1")
      ;
      (setq P1W (trans P1 1 0) P3W (trans P3 1 0))
      (setvar
         "blipmode" 0
      )
      (command
         "_ucs" "3p" P2 P1
         (polar P2 (+ ANG (* 0.5 pi)) 1)
      )
      (setq P1 (trans P1W 0 1) P3 (trans P3W 0 1))
      (setq
         P3 (mapcar '* P3 '(0 1))
      )
      (setq POINTS (PARAB P3 P1))
      (setvar "osmode" 0)
      (command "_pline")
      (apply 'command POINTS)
      (command "")
      (command "_pedit" (entlast) "f" "")
      (command "_ucs" "p")
   )

   ;==========================================================
   ; Body of PARABOLA Command 
   (if (NOTRANS)
      (progn
         (setq OLD-ERROR *error* *error* ERROR)
         (PUSHVARS
            '(("cmdecho" . 0)
               ("plinewid" . 0)
               ("plinegen" . 1)
               ("orthomode")
               ("blipmode")
               ("osmode")
               ("snapmode")
            )
         )
         (command "_undo" "group")
         (PARABOLA)
         (command "_undo" "end")
         (POPVARS)
      )
      (princ)
   )
)
(princ
   "  PARABOLA.LSP (Copyright 1992 by Looking Glass Microproducts) loaded."
)
(princ)
