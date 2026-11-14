;----------------------------------------------------------------------
;                               C A L C
;
; Written by: Leif Eriksen
;             Discovery Systems
;             PO Box 7058
;             Riverside, CA. 92513
;             (909) 353-2864
;
; Calc is a function that uses AutoLISP to calculate numeric values
; from inputs.  The result is then returned back to AutoCAD.
;
; To add additional functions to calc you must added the desired
; operator name to the initget and add a set of code to the cond
; function.

(defun calc ( / ce accum tmpaccum op num)
  (setq ce (getvar "cmdecho"))
  (setvar "cmdecho" 0)
  (setq accum 0.0)
  (setq op "+")
  (while op
    (initget "+ - * / ^ Set")
    (setq op (getkword (strcat "\n+, -, *, /, ^, Set, <"
                                (rtos accum 2) ">: ")))

    (cond

; if Set
      (( = op "Set")
        (setq tmpaccum (getdist (strcat "Accum value <"
                                  (rtos accum 2) ">: ")))
        (if tmpaccum
          (setq accum tmpaccum)
          )
        )

; if +
      ((= op "+")
        (terpri)
        (princ (rtos accum 2))
        (setq num (getdist " + "))
        (setq accum (+ accum num))
        (princ " = ")
        (princ accum)
        )

; if -
      ((= op "-")
        (terpri)
        (princ (rtos accum 2))
        (setq num (getdist " - "))
        (setq accum (- accum num))
        (princ " = ")
        (princ accum)
        )

; if *
      ((= op "*")
        (terpri)
        (princ (rtos accum 2))
        (setq num (getdist " * "))
        (setq accum (* accum num))
        (princ " = ")
        (princ accum)
        )

; if /
      ((= op "/")
        (terpri)
        (princ (rtos accum 2))
        (initget 1) ; Disallow zero input
        (setq num (getdist " / "))
        (setq accum (/ accum num))
        (princ " = ")
        (princ accum)
        )

; if ^
      ((= op "^")
        (terpri)
        (princ (rtos accum 2))
        (setq num (getdist " ^ "))
        (setq accum (expt accum num))
        (princ " = ")
        (princ accum)
        )

; Additional functions may be added at this point

      )
    )
  (setq cmdecho ce)
  (eval accum)
)
