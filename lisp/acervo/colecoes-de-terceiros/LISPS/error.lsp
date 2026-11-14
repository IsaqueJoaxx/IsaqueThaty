; Stan H. Bimson 73507,3475 - CAD/Engineering Services - 15 June 1987
; Rte 2 Box 293 Forest Harbor Drive  -  Hendersonville, TN 37075-9802
;
; Reset normal variables at the end of a program run.
; (GC) is an undocumented function.

(DeFun End ()                 ; Syntax:   (END)
  (COMMAND "OSnap" "None")
  (SetVar "Aperture" 8)
  (SetVar "Blipmode" 1)
  (GC)
  (Princ)
)

; To Quit a program at some place inside the program
; Syntax in a program:  (If (NULL Entered) (Cmdp))
; Quit is an undocumented AutoLISP function

(DeFun Cmdp () (Quit) )

; To stop screen display of LISP programs when program is
; aborted/cancelled/^C/or fails on its' own internally the
; AutoLISP function *ERROR* must be redefined. On some
; abnormal program ending AutoLISP will pass a string with
; the appropriate error message, this string can be handled
; as shown below. If I abort the program in some manner I
; don't what the LISP code or the error message but if it
; fails for another error I at least what the error message
; to help find the program error.

(DeFun *ERROR* (St)
  (Cond
    ((OR (= St "Function cancelled")
         (= St "console break")
         (= St "quit / exit abort")) )
    ((Princ St))
  )
  (End); clean things up
)

; Add these function to your ACAD.LSP file or type them into
; a file named ACAD.LSP and in your AutoCAD subdirectory.
