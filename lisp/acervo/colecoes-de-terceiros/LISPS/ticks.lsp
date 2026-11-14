;==========================================================
; TICKS.LSP Copyright 1994 by Looking Glass Microproducts
;==========================================================
; Select Short Lines, and Polylines in drawing 
;=============================================================
(setq MAX-TICK 0.125)
(defun C:TICKS (/ ERROR OLD-ERROR ITEM MAPSET FILTER SS1 SS2 
                ONE-TICK PERIMETER SEGLEN BITSET VBLIST TICKS 
                XGETDIST NOTRANS PUSHVARS POPVARS SYSVARS)
   ;==========================================================
   ; Lines, 2D Polylines, 3D Polylines
   (setq
      FILTER '((-4 . "<OR")
                (0 . "LINE")
                (-4 . "<AND")
                (0 . "POLYLINE")
                (-4 . "<NOT")
                (-4 . "&")
                (70 . 80)
                (-4 . "NOT>")
                (-4 . "AND>")
                (-4 . "OR>")
             )
   )
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
      (POPVARS)
      (princ)
   )
   ;==========================================================
   ; Set and Save System Variables
   ;==========================================================
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
   )
   ;==========================================================
   ; Restore System Variables
   ;==========================================================
   (defun POPVARS ()
      (foreach PAIR SYSVARS (setvar (car PAIR) (cdr PAIR)))
      (setq
         *error* OLD-ERROR
      )
      (setq SYSVARS nil)
   )
   ;==========================================================
   ; Disallow transparent invocation of routine.
   ;==========================================================
   (defun NOTRANS ()
      (cond
         ((zerop (logand (getvar "cmdactive") (+ 1 2 4 8))))
         ((alert
             "This command may not be invoked transparently."
          )
         )
      )
   )
   ;==========================================================
   ; Apply func to every entity in ss
   ;==========================================================
   (defun MAPSET (FUNC SS PRMPT / I L SUFFIX ENAME)
      (if SS
         (progn
            (setq
               FUNC   (eval FUNC)
               I      0
               L      (sslength SS)
               SUFFIX (strcat ":" (itoa L))
            )
            (repeat
               L
               (setq I (1+ I) ENAME (ssname SS (- L I)))
               (if (and PRMPT (zerop (rem I 10)))
                  (setvar
                     "modemacro" (strcat
                                    PRMPT
                                    " "
                                    (itoa I)
                                    SUFFIX
                                 )
                  )
               )
               (princ "")
               (FUNC ENAME)
            )
            SS
         )
      )
   )
   ;==========================================================
   ; Item from association list
   ;==========================================================
   (defun ITEM (A B) (cdr (assoc A B)))
   ;==========================================================
   ; Get Distance with Default
   ;==========================================================
   (defun XGETDIST (BASE PRMPT DEFAULT)
      (setq
         PRMPT (strcat
                  PRMPT
                  (if DEFAULT
                     (strcat " <" (rtos DEFAULT) ">")
                     ""
                  )
                  ": "
               )
      )
      (cond
         ((if BASE (getdist BASE PRMPT) (getdist PRMPT)))
         (DEFAULT
         )
      )
   )
   ;==========================================================
   ; Perimeter of a polyline
   ;==========================================================
   (defun PERIMETER (PLIST / PERIM)
      (setq PERIM 0.0)
      (while PLIST
         (setq
            PERIM (+ (SEGLEN PLIST) PERIM)
            PLIST (cdr PLIST)
         )
      )
      PERIM
   )
   ;==========================================================
   ; Length of a polyline segment
   ;==========================================================
   (defun SEGLEN (PLIST / P0 1 B ATANB D/2 R)
      (setq P0 (caar PLIST) B (cdar PLIST) P1 (caadr PLIST))
      (cond
         ((null P1) 0)
         ((zerop B) (distance P0 P1))
         (t
            (setq
               ATANB (atan B)
               D/2   (* 0.5 (distance P0 P1))
               R     (/ D/2 (sin (* 2.0 ATANB)))
            )
            (abs (* R 4.0 ATANB))
         )
      )
   )
   ;==========================================================
   ; Return T if any bit of A matches B 
   ;==========================================================
   (defun BITSET (A B) (/= 0 (logand A B)))
   ;==========================================================
   ; Return, in current wcs, a vertex/bulge list 
   ; for a polyline -- ((point . bulge)...) 
   ;==========================================================
   (defun VBLIST (PNAME / ENT ENAME CLOSED PLIST)
      (setq
         ENT    (entget PNAME)
         ENAME  PNAME
         CLOSED (BITSET 1 (ITEM 70 ENT))
      )
      (while (=
                "VERTEX"
                (ITEM
                   0
                   (setq
                      ENAME (entnext ENAME)
                      ENT   (entget ENAME)
                   )
                )
             )
         (if (not (BITSET 16 (ITEM 70 ENT))) ; not a spline control point
            (setq
               V     (cons (ITEM 10 ENT) (ITEM 42 ENT))
               PLIST (cons
                        V
                        (if (not
                               (equal
                                  (car V)
                                  (caar PLIST)
                                  1E-6
                               )
                            )
                           PLIST
                           (cdr PLIST) ; discard zero length segments
                        )
                     )
            )
         )
      )
      (if CLOSED (setq PLIST (cons (last PLIST) PLIST)))
      (reverse
         PLIST
      )
   )

   ;==========================================================
   ; Process one Tick
   ;==========================================================
   (defun ONE-TICK (ENAME / ENT TICKSIZE)
      (setq
         ENT      (entget ENAME)
         TICKSIZE (cond
                     ((= "LINE" (ITEM 0 ENT))
                        (distance
                           (ITEM 10 ENT)
                           (ITEM 11 ENT)
                        )
                     )
                     (t (PERIMETER (VBLIST ENAME)))
                  )
      )
      (if (<= TICKSIZE MAX-TICK) (ssadd ENAME SS2))
   )
   ;==========================================================
   ; Main routine
   ;==========================================================
   (defun TICKS ()
      (setq SS1 (ssget FILTER))
      (cond
         ((null SS1))
         (t
            (initget 6) ; disallow zero, negative
            (setq
               MAX-TICK (XGETDIST
                           nil
                           "\nMaximum tick size"
                           MAX-TICK
                        )
            )
            (setq SS2 (ssadd))
            (MAPSET
               'ONE-TICK
               SS1
               "Processing"
            )
            (if (zerop (sslength SS2))
               (progn
                  (setq SS2 nil)
                  (prompt "\n0 qualified")
               )
               (progn
                  (prompt
                     (strcat
                        "\n"
                        (itoa (sslength SS2))
                        " qualified in Previous Selection Set"
                     )
                  )
                  (setvar "highlight" 0)
                  (command
                     ".select" SS2 ""
                  )
               )
            )
         )
      )
      (setq SS2 nil)
   )
   ;==========================================================
   ; Body of c:ticks  
   ;==========================================================
   (if (NOTRANS)
      (progn
         (setq *error* ERROR OLD-ERROR ERROR)
         (PUSHVARS
            '(("cmdecho" . 0) ("modemacro") ("highlight"))
         )
         (TICKS)
         (POPVARS)
      )
   )
   (princ)
)
(princ
   (strcat
      "  TICKS.LSP v1.0 (Copyright 1993 by "
      "Looking Glass Microproducts) loaded."
   )
)
(princ)
