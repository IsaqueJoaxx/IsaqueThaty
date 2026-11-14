;; Program For AutoLISP Dialog Box Menu
(defun C:PROGRAM (/ ai_defaults dcl_id old_cmd
                   old_error PRG TILE what_next)
  (defun PROGRAM_MAIN ()
    (if (not (new_dialog "PROGRAM" dcl_id)) (exit))
      (action_tile "accept"" (done_dialog)")
      (action_tile "cancel"" (done_dialog)")
      (action_tile "PLN1" "(setq PRG $key)")
      (action_tile "PLN2" "(setq PRG $key)")
      (action_tile "PLN3" "(setq PRG $key)")
      (action_tile "PLN4" "(setq PRG $key)")
      (action_tile "PLN5" "(setq PRG $key)")
      (action_tile "PLN6" "(setq PRG $key)")
      (action_tile "PLN7" "(setq PRG $key)")
      (action_tile "PLN8" "(setq PRG $key)")
      (action_tile "PLN9" "(setq PRG $key)")
      (action_tile "PLN10" "(setq PRG $key)")

  (setq what_next (start_dialog))
   (if (= 1 what_next)
    (progn
      (if (assoc "PROGRAM" ai_defults)
       (setq ai_defults (subst (list "PROGRAM" on_screen)
       (assoc "PROGRAM" ai_defaults)
               ai_defaults
  )))))
  (do_action PRG)
  );end defun main

;; Setup Error Function
(setq old_cmd (getvar "cmdecho")
     old_error  *error*
      *error* ai_error
)
(setvar "cmdecho" 0)
(cond
   ((not (ai_notrans)))
   ((not (ai_acadapp)))
   ((not (setq dcl_id (ai_dcl "PROGRAM"))))
   (t (PROGRAM_MAIN))
)
(setq *error* old_error)
(setvar "cmdecho" old_cmd)
(done_dialog dcl_id)
(princ)
)

(defun DO_ACTION (PRG)
  (cond
    ((= PRG "PLN1") (load "PLN1") (c:PLN1))
    ((= PRG "PLN2") (load "PLN2") (c:PLN2))
    ((= PRG "PLN3") (load "PLN3") (c:PLN3))
    ((= PRG "PLN4") (load "PLN4") (c:PLN4))
.   ((= PRG "PLN5") (load "PLN5") (c:PLN5))
    ((= PRG "PLN6") (load "PLN6") (c:PLN6))
    ((= PRG "PLN7") (load "PLN7") (c:PLN7))
    ((= PRG "PLN8") (load "PLN8") (c:PLN8))
    ((= PRG "PLN9") (load "PLN9") (c:PLN9))
    ((= PRG "PLN10") (load "PLN10") (c:PLN10))
))
;****************************
(defun C:PLN1 ()
  (repeat 3
    (prompt "\nYou selected Program Option 1!")
  )
)
;****************************
(defun C:PLN2 ()
  (repeat 3
    (prompt "\nYou selected Program Option 2!")
  )
)
