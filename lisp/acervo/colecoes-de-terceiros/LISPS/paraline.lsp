; PARALINE.LSP  

(DeFun Paraline (Closed Offset / First)
   ; Routine to continously draw parallel lines, given
   ; distance between lines (Offset) and closure tolerance (Closed).
   ; Scratchpad variable First used to remember special case.

   (Startup)                            ; get ready
   (While (< Closed (Distance Start End))   ; control main loop
      (MainLoop))                       ; repeat until done
   (Endup "Closure"))                   ; the end when polygon closes

(DeFun Startup ()
   (SgnOn)                              ; identify program
   (SetQ ApSav (GetVar "aperture"))     ; save environment variables
   (SetQ BlipSave (GetVar "blipmode"))
   (SetQ FilletSav (GetVar "filletrad"))
   (SetQ CmdSave (GetVar "cmdecho"))
   (SetQ HalfPi (/ pi 2.0))             ; need this constant later
   (SetVar "aperture" 1)                ; shrink aperture to minimum
   (SetVar "blipmode" 0)                ; turn off blips
   (SetVar "filletrad" 0.0)             ; to join lines
   (SetVar "cmdecho" 0)                 ; run fast when debugged
   (GraphScr)                           ; flip to graphics screen
   (SetQ Start (GetPoint "From Point? "))
   (TerPri)                             ; skip to new line
   (SetQ Next Start)
   (SetQ First T)                       ; this is first line
   (SetQ End (Polar Start 0.0 (* Closed 2.0))))

(DeFun SgnOn ()
   (PrintEm (List "\n\nParallel line routine (C) 1986 M. Slinn\n"
         "Inside of parallel lines are drawn when working clockwise\n")))

(DeFun MainLoop ()                      ;
   (SetQ Mid2 (Drawlines Next Offset Closed))
   (If First
      (ProgN                            ; first line, don't fillet..
         (SetQ Start2 Pt3)              ; .. but store start points
         (SetQ Mid0 Mid2))
      (Command "fillet" Mid1 Mid2))     ; not first line, so fillet
   (SetQ Mid1 Mid2)                     ; get ready for next segment..
   (SetQ Next End)                      ; .. by advancing end point
   (SetQ First ()))                     ; not first any more

(DeFun Drawlines (Pt1 Offset Closed / ) ; Heart of Paraline routine
   (SetQ End (GetPoint "To Point? "))   ; Invalid point here, eg. space,
                                        ; Ctrl-C, return, invokes *Error*
   (TerPri)                             ; New line after coord entered
   (If (And (Not First)                 ; Test: within closure yet?
            (>= Closed (Distance Start End)))
      (SetQ End Start))                 ; yes, reset End
   (Command "line" Pt1 End "")          ; draw inside line
   (SetQ Len (Distance Pt1 End))        ; get length
   (SetQ Ang (Angle Pt1 End))           ; get angle
   (SetQ Pt3 (Polar Pt1 (+ HalfPi Ang) Offset)) 
   (SetQ Pt4 (Polar Pt3 Ang Len))
   (Command "line" Pt3 Pt4 "")          ; draw parallel line...
   (MidPt Pt3 Pt4))                     ; ... and return its midpoint

(DeFun EndUp (Result)                   ; clean up and exit routine
   (Command)                            ; generate Control C
   (If (Eq Result "Closure")
      (Command "fillet" Mid0 Mid2))     ; closure of outside parallel
   (If (Not (Eq Start Next))            ; does polygon close?
      (ProgN                            ;   if not...
         (If (AskYN "Do you wish to cap the start" T)
            (Command "line" Start Start2 ""))
         (If (AskYN "Do you wish to cap the end" T)
            (Command "line" Next Pt4 ""))))       ; no Else clause needed
   (PrintEm (List "\nEnd point is:  "))
   (Command "id" Next)                  ; can use @ next time to continue
   (SetVar "aperture" ApSav)            ; restore environment
   (SetVar "blipmode" BlipSave)
   (SetVar "filletrad" FilletSav)
   (Princ "\n\nAll done...\n")          ; termination message
   (SetVar "cmdecho" CmdSave) ())

(DeFun *Error* (St)                     ; Trap for CTL-C, space or return
   (If (Or
          (Eq St "bad point argument")  ; poorly defined point
          (Eq St "console break")       ; CTL-C during function
          (Eq St "Function cancelled")  ; CTL-C during input
          (Eq St "bad point value"))    ; space or return during input
      (EndUp "Open")                    ; The end if not a closed polygon
      (PrintEm (List "\n" St ". Paraline aborts.\n"))))

; *** Routines ***
(DeFun AskReal (Prmpt V / X)            ; ask for real number
   (PrintEm (List "\n" Prmpt " <" V ">: ")) ; show prompt & default value
   (SetQ X (GetReal))                   ; Get number from user
   (If (Not (Null X))                   ; Space or Return?
      (SetQ V X)                        ; Value was typed so return it
      (SetQ V V)))                      ; Not typed so return default

(DeFun AskYN (Prmpt YN / Strng)         ; yes/no query routine
   (SetQ Strng "")
   (While
      (And
         (Not (Eq Strng "Y"))
         (Not (Eq Strng "N"))
         (Not (Eq Strng "D")))
      (PrintEm (List "\n" Prmpt "? <"))    ; present choice
      (If (Not (Null YN))
         (ProgN
            (Princ "Yes")
            (SetQ YN T))
         (ProgN
            (Princ "No")
            (SetQ YN ())))
      (PrintEm (List ">: "))
      (SetQ Strng "")
      (SetQ Strng (Substr (GetString) 1 1)) ; get 1st char of response
      (If (Eq "" Strng)                 ; if <CR> or space typed..
         (SetQ Strng "D")               ; .. go for default value
         (If (Or                        ; else if YES then..
                (Eq "Y" Strng)
                (Eq "y" Strng))
            (ProgN                      ; .. force yes result
               (SetQ Strng "Y")
               (SetQ YN T))
            (If (Or                     ; else if NO then
                  (Eq "N" Strng)
                  (Eq "n" Strng))
               (ProgN                   ; .. force no result
                  (SetQ Strng "N")
                  (SetQ YN ()))
               (PrintEm (List "\nYes, No, space or <CR> please.\n"))))))
   (SetQ YN YN))                        ; return response

(DeFun PrintEm (Args / A)               ; print any kind of list
   (ForEach A Args                      ; assign each item in Args to A
                                        ; in turn, and Princ it
      (Princ A)) ())                    ; Works with formatting chars

(DeFun MidPt (Pt1 Pt2)                  ; calculate midpoint
   (List (/ (+ (CAR Pt1) (CAR Pt2)) 2.0)
         (/ (+ (CADR Pt1) (CADR Pt2)) 2.0)))

; *** User Interface ***
(DeFun PL_Init ()
   (SgnOn)                              ; pls include to protect copyright
   (SetQ Offset 0.5)                    ; default distance between lines
   (SetQ Tol 50)                        ; default closure tolerance
   (TextScr)                            ; flip to text screen
   (PrintEm (List "\nType PO to change offset <" Offset ">\n"))
   (PrintEm (List "Type PT to reset closure tolerance <" Tol "% of offset> \n"))
   (PrintEm (List "Type PL to draw parallel lines\n")) ())

(DeFun C:PL ()                           ; draw double lines with defaults
   (Paraline (* Offset (/ Tol 100.0)) Offset))

(DeFun C:PO ()                           ; Set default line separation
   (SetQ Offset (AskReal "Offset? " Offset)) ())

(DeFun C:PT ()                           ; set closure tolerance
   (SetQ Tol (AskReal "Closure Tolerance (% of offset)" Tol)) ())

; *** The only code executed when loaded: ***
(PL_Init)       
