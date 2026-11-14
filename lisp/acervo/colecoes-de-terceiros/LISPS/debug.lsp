;
; ßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßß
; A/E Automation Systems          ²² ²² ²²²²²  °°°°°°°°°°°°°°°°°°°°°°°°°
;                               ²²²  ²²
;                             ²²²    ²² ²²²       32 Gould Avenue
;                           ²²²   ²² ²²           Paterson, N.J.  07503
; °°°°°°°°°°°°°°°°°°°°°°  ²²²        ²² ²²²²²     (201) 523-7944
;
; ßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßß
; d e s i g n          a u t o m a t i o n         c o n s u l t a n t s
;
; ----------------------------------------------------------------------
; DEBUG.LSP                 Copyright 1989        A/E Automation Systems
;
;
; User-supported software.
;
; This product is not in the public domain, and is protected by federal
; copyright laws.  Any commercial use requires payment to the author.
; Contact the author at the above address for pricing information.
;
; Functions included:
;
; *BREAK*    Breakpoint interrupt program function.
; EVAL-STR   Auxillary function for *BREAK* program.
; VLIST      Variable assignment display function.
;
; The BREAK function.
;
; BREAK is an AutoLISP programming debugging tool that permits
; a program to be interrupted without termination, so that the
; programmer can examine the environment, change variable values,
; and perform other debugging tasks.  When the debugging task
; is complete, the programmer can then resume program execution
; and observe the effects of any changes made during the breakpoint
; event.
;
; To use *BREAK*, place the it in your program wherever you
; want a break to occur (do not be afraid to use it liberally,
; because you can EASILY turn *BREAK* ON and OFF off without
; having to remove the *break* expressions from your code).
;
; When you have finished debugging your code, you can use a
; text editor to do a global replace to remove all *break*
; expressions from your program, or you can simply leave them
; in and set *breakenable* off (this is useful if you want to
; diagnose a problem in an application that you have distributed
; to a customer, or if you are not onsite).
;
;    Example:
;
;     (defun C:myprog ( / a b c)
;
;       (setq a 1 b 2 c 3)
;
;       (*break*)    ; program will pause here if *breakenable* <> NIL.
;
;       <rest of your program>
;     )
;
; When a *break* expression is encountered, program execution will
; be temporarily suspended, and you will see the following prompt:
;
; BREAK >> _
;
; You can now enter ANY AutoLISP expression, and it will be evaluated,
; and it's result will be printed.  To examine the value of a variable,
; simply enter the name of the symbol it is bound to.  You can also use
; the SETQ function to change the value of any variable that is available
; from the environment in which the breakpoint interrupt originated.
;
; To resume program execution, simply hit RETURN at any BREAK >> prompt.
;
; Break will also check for and reject expressions containing unbalanced
; parenthesis and double quotes.
;
; You can turn breakpoint interruption on and off with the global
; variable "*break-enable*".  Breakpoint interruption will only occur
; if the value bound to this symbol is NON-NIL (i.e. T).
;
; So, in order to enable *break*, you must first enter the following
; expression at the command prompt (or include it in a program):
;
;    (setq *break-enable* T)
;
; To turn *BREAK* off:
;
;    (setq *break-enable* nil)
;
; Conditional breakpoint interruption.
;
;   Breakpoint interrupts can also be triggered conditionally by the
;   *break-if* function.  If defined, it will be evaluated every time
;   a *break* expression is encountered in the code.  If it evaluates to
;   NON-NIL, then a breakpoint interrupt will be triggered.  Otherwise,
;   program execution will continue uninterrupted.
;
;   For example, to trigger a conditional breakpoint only if the value
;   bound to the symbol `PT1' is both non-nil, and not a list;
;
;     (defun *break-if* ()
;        (and pt1 (not (listp pt1)))
;     )
;
;   If *BREAK-IF* is defined, *BREAK-ENABLE* must also be NON-NIL to
;   permit breakpoint interruption.
;
;
; Using BREAK in with the *ERROR* function.
;
;   You can also include a *break* expression in the *ERROR* function
;   which will generate a breakpoint interruption whenever an error
;   occurs.  This will permit you to examine the environment in which
;   the error occured, before it is lost, even if variables have been
;   declared local.
;
; Note: You should NOT use any symbols in your program that are used by
;       the following break functions.  If your program contains any of
;       these symbols, you will not be able to access thier values at
;       at a BREAK >> prompt (all symbols used by the break functions below
;       have a prefix of "BK:xxxxx").
;
; Listing variable values with VLIST.
;
;   VLIST is a utility function that permits you to examine the
;   values of all arguments and local variables (referred to as the
;   "local environment") for a given function, during evalaution.
;
;   To invoke VLIST, enter "(vlist)" at the BREAK >> prompt, during a
;   break interrupt that originated from within the function who's local
;   environment is to be listed.  You will then be prompted for the name
;   of a function.  Enter the name of the function without parenthesis,
;   and the names and values of all arguments and local variables will be
;   listed on the console.
;
;   This utility is primarily intended for use at the BREAK >> prompt, when
;   the break interrupt is originating from a *break* expression located
;   within the function to be listed.  VLIST must be called from within
;   the function who's environment is to be examined, or all results may
;   be bogus.
;
;   Good luck.
;   Tony Tanzillo,
;   A/E Automation Systems
;

(defun *break* ( / bk:expr)
  (cond (  (and *break-enable* (cond (*break-if* (*break-if*)) (t)))
           (prompt "\n<Breakpoint interrupt>")
           (while (not (eq "" (setq bk:expr (getstring "\nBREAK >> " t))))
                  (eval-str bk:expr)
           )
           (prompt "\n<Resuming program execution>")
        )
  )
)

(defun eval-str (bk:str / bk:c bk:q bk:p bk:i)
  (setq bk:q 0 bk:p 0 bk:i 0 bk:c "")
  (while (not (eq "" (setq bk:c (substr bk:str (setq bk:i (1+ bk:i)) 1))))
         (cond ( (eq bk:c "(") (setq bk:p (1+ bk:p)))
               ( (eq bk:c ")") (setq bk:p (1- bk:p)))
               ( (eq bk:c (chr 34)) (setq bk:q (1+ bk:q)))
         )
  )
  (cond (  (not (zerop (rem bk:q 2))) (prompt "<Error: unbalanced quotes>"))
        (  (not (zerop bk:p)) (prompt "<Error: unbalanced parenthesis>" ))
        (t (prompt "Result: ")
           (prin1 (eval (read bk:str)))
        )
  )
)

(defun vlist ( / func fname fn dsh)
  (setq func (getstring "\nFunction name: " t))
  (setq fname (read func))
  (setq fn (cond (  (listp fname) (car fname))
                 (t fname)
           )
  )
  (setq fn (eval fn))
  (cond ( (and (cond (fn) (t (prompt "\nFunction not defined.")))
               (cond (  (listp fn))
                     (t (prompt (strcat "\n" (strcase func)
                                        " is not a function definition."
                                )
                        )
                     )
               )
               (cond (  (car fn))
                     (t (prompt (strcat "\n" (strcase func)
                                        " has no arguments or local variables."
                                )
                        )
                     )
               )
          )
          (textscr)
          (setq dsh "\n----------------------------------------\n")
          (prompt (strcat "\nEnvironment: " (strcase func)))
          (cond ( (not (eq '/ (caar fn)))
                  (prompt (strcat "\nArguments: " dsh))
                )
          )
          (mapcar
            '(lambda (x)
               (cond (  (not (eq '/ x))
                        (prin1 x)
                        (princ ": ")
                        (prin1 (eval x))
                        (terpri)
                     )
                     (t (prompt (strcat "\nLocal variables: " dsh )))
               )
             )
             (car fn)
          )
        )
  )
  (prin1)
)

