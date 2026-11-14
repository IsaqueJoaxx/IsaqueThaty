;  MODE.LSP
;  12/19/1986
;  by Tony Tanzillo, A/E Automation Systems

;  MODE.LSP is a function which replaces the commonly used
;  user-defined (echo_off) and (echo_on) functions,
;  permitting better management of AutoCAD system variables
;  during execution of user-defined commands.

;  The old functions (echo_off) and (echo_on) which (mode)
;  replaces are;

;  (defun echo_off ()
;    (setq echo (getvar "CMDECHO"))    ; store value of "cmdecho" to echo
;    (setvar "CMDECHO" 0)              ; turn cmdecho off
;  )

;  (defun  echo_on()
;    (setvar "CMDECHO" echo)           ; restore value in echo to cmdecho
;  )


;  The main purpose of (mode) is to SAVE any number or types of
;  system variables, set new values to zero or other values for
;  the duration of the user defined command, then restore theese
;  same system variables to thier original values once the
;  user-defined AutoLISP command is completed.

;  System variables included in this function will be saved,
;  and can be changed any number of times to any value by
;  a lisp function or even by the user (i.e. toggle snap on
;  and off with F9) after (mode 0) has been issued, and they will be
;  restored to thier original values when (mode 1) is issued.

;  If (mode 0) is issued more than once without issuing
;  (mode 1), it will have no effect other than to set all
;  system variables in the list to zero (they will not be
;  saved again until they have been restored first, which
;  prevents the saved values from being overwritten).
;  This can happen when a function calls (mode 0) and then
;  an error condition occurs causing premature termination
;  of the function before (mode 1) is evaluated.

;  When (mode 1) is issued, the system variables in the list
;  VLST are restored to the values which were current prior
;  to issuing (mode 0).

;  You can add system variables to the list that is saved
;  by inserting them into the list (within quotes),
;  but they must be integer data types.

;  If you add mode.lsp to your ACAD.lsp file, and you currently
;  use (echo_off) and (echo_on) to toggle the system variable
;  "cmdecho" on and off, you can replace those two functions with
;  (mode x) as follows;

;  replace all occurances of (echo_off) with (mode 0)

;  replace all occurances of (echo_on) with (mode 1)

;  When (mode 0) is issued, the current values of all system
;  variables in the list bound to VLST are saved in a seperate
;  list so they can be restored later when (mode 1) is issued.
;  (usually when a user-defined lisp command has terminated).
;  Then, these same system variables are all set to 0 resulting
;  in the following conditions;

;  Object snap: OFF (or 0)
;  Highlight:   OFF   "
;  Blipmode:    OFF   "
;  Snap:        OFF   "
;  Cmdecho:     OFF   "
;  Filletrad    0.0  (a real)

;  For most user-defined commands written in AutoLISP, the above
;  conditions are neccessary for correct operation of the command
;  and to prevent AutoCAD prompts within a (command) function from
;  from being displayed. Some may be a matter of personal preference,
;  but the above variables are the ones that I save and turn off
;  when a user-defined Autolisp command is invoked.

;  Blipmode is optional, I prefer it to be OFF for user-defined
;  commands. If you prefer it to be on, just remove it from the
;  list of variables in the function below.

;  Object snap is not desireable in most cases since it will have
;  an unpredictable effect on point selection, and entity drawing.

;  Highlight is not needed for most operations except for (ssget).
;  If you wanted highlight off for most things, but want it on when
;  (ssget) is issued, then just issue a (setvar "highlight" 1) prior
;  to (ssget) and (setvar "highlight" 0) after (ssget) It will not
;  change the state of highlight after the lisp function is completed
;  since it's original value will be restored when (mode 1) is issued.

;  I keep highlighting on at all times, but prefer it to be off when an
;  Autolisp function is underway UNLESS it uses (ssget). So (mode 0)
;  turns it OFF for that, and since it is usually on when (mode 0)
;  is issued, it is restored to the same state when (mode 1) is issued.

;  The data types of system variables that can be "pushed" and "poped"
;  with this function are not limited to integers, but the lisp expression;
;  (foreach var vlst (setvar var 0)) assumes this, so other system
;  variables with data types other than INTEGER can be set individually
;  to whatever value deemed appropriate in the function below just after
;  the (foreach...  ) expression individually by placing a
;  (setvar "variable name" value) for each non-integer system variable
;  which is included in the list.

;  As an example, I have included the system variable "filletrad"
;  (fillet radius) which is a REAL in the list of variables which are
;  pushed and popped, but it can't be set by the (foreach ....)
;  expression since it expects an INTEGER datatype, so a seperate
;  (setvar...) expression is included to set it to the value it should be
;  set to (0 radius) when a user-defined command is invoked.
;  If the fillet radius were for example 2.5 units prior to invoking a user
;  defined command which used (mode 0) when it is invoked, the fillet
;  radius will be set to 0 drawing units for the duration of the command,
;  then prior to terminating, the fillet radus will be restored to 2" units
;  when (mode 1) is evaluated (usually the last expression to be evaluated
;  in the function).

;  other system variables of non-integer data types can be included
;  and set in the same manner as "filletrad". The variable name should
;  also be added to the list of system variables in the function below,
;  in order for it to be saved and restored.

;  This function was written originally for use with version 2.18,
;  but it can be used with just about any later version of AutoCAD.


(defun mode(x)
  (if (not vlst)                     ; the first time (mode) is evaluated,
    (setq vlst                       ; create the list of system variables
     '(
         "highlight"                 ; insert other system variable names
         "blipmode"                  ; here if you want to save and restore
         "osmode"                    ; them along with these, or
         "snapmode"                  ; remove any of these if you don't
         "cmdecho"                   ; wish to save and restore them.
         "filletrad"                 ; any data type is permitted as well,
      )                              ; provided they are set individually as
    )                                ; illustrated below with "filletrad"
  )
  (if (zerop x)
    (progn                                    ; (mode 0) entering lisp mode
      (if (not varg)                          ; if not already saved,
        (setq varg (mapcar 'getvar vlst))     ; save sys. vars in varg
      )
      (foreach var vlst                       ; step thru list of variables
        (if (eq (type (getvar var)) 'INT)     ; if variable is an INTEGER,
          (setvar var 0)                      ; then set it to ZERO
        )                                     ; if not, leave it alone
      )
        ; you can set other non-integer system variables
        ; individually with (setvar "variable name" value)
        ; if they were added to the above list.
        ; these expressions should be placed at this point
        ; in the function. i.e.

      (setvar "filletrad" 0.0) ; sets fillet radius to zero

    )
    (progn                                    ; (mode 1) returning to command mode
      (if varg                                ; if already saved,
        (mapcar 'setvar vlst varg)            ; restore variables in vlst
      )
      (setq varg nil)                         ; throw away old values
    )
  )
)


;  Example usage of (mode)

;  The following is an example of a user-defined function which
;  uses (mode) in the typical manner for which it is intended
;  The function permits a continous chain of filleting lines.
;  The command is useful in itself because it permits you to
;  fillet every corner of a closed polygon with almost half of
;  the amount of pointing required by the AutoCAD fillet command.
;  To use it, draw several (at least 4) lines at different angles
;  and enter CFILLET at the AutoCAD command prompt. You will be prompted
;  for the fillet radius, then you will be prompted for the first 2 lines
;  to be filleted, once this is done, you will be prompted for another line
;  which will be filleted with the 2nd line entered. then you will
;  continously prompted for more lines, each one new one picked will be
;  filleted with the last line picked. To terminate the command, simpley
;  reply with space or CR to the "Next line: " prompt, or point to an
;  arbitrary point on the display that is not within the aperture range of
;  a line.
;
;  The command will demonstrate how the usage of (mode) the function will
;  save and restore any number of system variables to thier original values.

;  variables effected:  object snap mode (command "osnap"), fillet radius
;  (setvar "filletrad" value), snap, cmdecho, highlight, blipmode, etc.

;  Note the value of these system variables before invoking the CFILLET
;  command (You can use another user defined command function included
;  here called VTEST to view the variables). Once into the CFILLET command,
;  notice the change to the status line, with respect to the snap indicator
;  also notice that you will have an aperture box when the command is in
;  progress, (if you set osnap off first, you will notice it will be off
;  after the CFILLET command terminates also but will be on for the duration
;  of the command. Once the command is terminated by replying with space
;  or <cr> to the "Next line: " prompt, try using the VTEST command again
;  to see if the variables are the same as they were before issuing CFILLET
;  (if they aren't somethings wrong!)
;
;
;
;  Look over the function and notice the use of (mode 0) as opposed to
;  (echo_off) and (mode 1) as opposed to (echo_on) at the start and end
;  of the procedure.
;
;  Happy Holidays -Tony T.

(defun C:CFILLET()
  (MODE 0)
  (setq fr nil l1 nil l2 nil l nil c 1)
  (setq fr (distget NIL "Fillet radius" 4.0))
  (setvar "filletrad" fr)
  (command "osnap" "nea")
  (while (not (osnap l1 "nea")) (setq l1 (getpoint "\nFirst line to fillet: ")))
  (while (not (osnap l2 "nea")) (setq l2 (getpoint "\nSecond line to fillet: ")))
  (command "fillet" l1 (setq l1 l2))
  (if (and fr l1 l2)
    (progn
      (while
        (osnap
          (setq l
            (getpoint
              (strcat "\n" (itoa (setq c (1+ c))) " lines filleted, Next line: ")
            )
          )   "nea"
        )
        (if (osnap l "nea" ) (command "fillet" l2 (setq l2 l)) )
      )
    )
  )
  (MODE 1)        ; put everthing back the way it was before we started!
)



(defun C:VTEST()  ; a utility command to view system variables that were
  (textscr)       ; changed by the (mode) function
  (prompt "\nCurrent value of system variables under influence of (mode):\n")
  (terpri)
  (foreach var vlst
    (prompt
      (strcat
        var
        (space (- 12 (strlen var)))
      )
    )
    (prin1 (getvar var))
    (terpri)
  )
  (terpri)
)

(defun space(n / x s)
  (setq x 0 s (chr 32))
  (while (> n (setq x (1+ x))) (setq s (strcat s (chr 32))))
)


(defun distget( b p d / t)
  (if
    (null
      (setq t
        (getdist
          (if b b (list NIL NIL))
          (strcat "\n"
            (if p p "Distance" )
            " <"
            (rtos d
               (getvar "lunits")
               (getvar "luprec")
            )
            ">: "
          )
        )
      )
    )
    (float d)
    t
  )
)


;   -Tony Tanzillo, A/E Automation Systems  70307,2556

