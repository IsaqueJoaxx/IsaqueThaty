;==========================================================
; GORE.LSP Copyright 1993 by Looking Glass Microproducts
;==========================================================
; Draws a gore 
;=============================================================
(defun ASIN (X) (atan X (sqrt (- 1.0 (* X X )))))

(defun C:GORE (/ ERROR PUSHVARS POPVARS SYSVARS OLD-ERROR NOTRANS
               XGETINT XGETKWORD GETPARMS <-90 <90 <180)
   (setq <-90 (* -0.5 pi) <90 (* 0.5 pi) <180 pi)
   ;==========================================================
   ; Error Handler
   (defun ERROR (S)
      (if (not
             (member
                S
                '("Function cancelled" "console break")
             )
          )
         (alert S)
      )
      (command ".undo" "end")
      (command ".undo" "1")
      (POPVARS)
      (princ)
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
   )
   ;==========================================================
   ; Restore System Variables
   (defun POPVARS ()
      (foreach PAIR SYSVARS (setvar (car PAIR) (cdr PAIR)))
      (setq
         *error* OLD-ERROR
      )
      (setq SYSVARS nil)
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
   ;==========================================================
   ; Get Integer with default
   ;==========================================================
   (defun XGETINT (PRMPT DEFAULT)
      (cond
         ((getint (strcat PRMPT " <" (itoa DEFAULT) ">: ")))
         (DEFAULT
         )
      )
   )

   ;==========================================================
   ; GetKword with Default
   ;==========================================================
   (defun XGETKWORD (PRMPT DEFAULT)
      (cond
         ((getkword (strcat PRMPT " <" DEFAULT ">: ")))
         (DEFAULT
         )
      )
   )

   ;==========================================================
   ; Main routine
   ;==========================================================
   (defun GETPARMS (/ N-MIN N-DEF)
      (initget 1)
      (setq P0 (getpoint "\nCenter of gore: "))
      (initget "S H")
      (setq
         MODE (XGETKWORD
                 "\nSpherical or Hemispherical gore (S/H)"
                 "S"
              )
      )
      (initget 1 "Diameter")
      (setq
         R (getdist "\nDiameter/<radius>: ")
      )
      (if (= "Diameter" R)
         (progn
            (initget 1)
            (setq
               R (* 0.5 (getdist "\nDiameter: "))
            )
         )
      )
      (while (progn
                (initget 6) ; disallow negative, zero       
                (setq
                   M (XGETINT
                        "\nNumber of longitudinal segments"
                        16
                     )
                )
                (< M 3)
             )
         (prompt
            "\nNumber of segments must be at least 3."
         )
      )
      (if (= MODE "S")
         (setq N-MIN 2 N-DEF 16)
         (setq N-MIN 1 N-DEF 8)
      )
      (while (progn
                (initget 6) ; disallow negative, zero       
                (setq
                   N (XGETINT
                        "\nNumber of latitudinal segments"
                        N-DEF
                     )
                )
                (< N N-MIN)
             )
         (prompt
            (strcat
               "\nNumber of segments must be at least "
               (itoa
                  N-MIN
               )
               "."
            )
         )
      )
   )
   ;==========================================================
   ; Main routine
   ;==========================================================
   (defun GORE (/ P0 MODE R M N SINB A DA TOP BOT)
      (GETPARMS)
      (setq SINB (sin (/ <180 M)))
      (if (= MODE "S")
         (setq A <-90 DA (/ <180 N))
         (setq A 0.0 DA (/ <90 N))
      )
      (repeat
         (1+ N)
         (setq
            X   (* R A)
            Y   (* R (ASIN (* (cos A) SINB)))
            TOP (cons (list X Y) TOP)
            BOT (cons (list X (- Y)) BOT)
            A   (+ A DA)
         )
      )
      (setq
         POINTS (append
                   TOP
                   (if (= MODE "S")
                      (cdr (reverse (cdr BOT)))
                      (reverse
                         (cdr BOT)
                      )
                   )
                   (list "c")
                )
      )
      (setvar "blipmode" 0)
      (command ".ucs" "or" P0)
      (command ".pline")
      (apply 'command POINTS)
      (command ".ucs" "p")
      (setvar "lastpoint" P0)
   )
   ;==========================================================
   ; Body of c:gore  
   ;==========================================================
   (if (NOTRANS)
      (progn
         (setq OLD-ERROR *error* *error* ERROR)
         (setvar
            "cmdecho" 0
         )
         (command ".undo" "group")
         (PUSHVARS
            '(("osmode" . 0) ("plinewid" . 0) ("blipmode"))
         )
         (GORE)
         (command ".undo" "end")
         (POPVARS)
      )
   )
   (princ)
)
(princ
   (strcat
      "  GORE.LSP (Copyright 1993 by"
      " Looking Glass Microproducts) loaded."
   )
)
(princ)

