; Stan H. Bimson 73507,3475 - CAD/Engineering Services - 15 June 1987
; Rte 2 Box 293 Forest Harbor Drive  -  Hendersonville, TN 37075-9802
;
;                 Requires AutoCAD V2.5+ ADE3
; Object snap commands are great but they have some limitations,
; you can not snap to something that is not there!! Well we are
; going to modify two of the standard commands Intersection and
; Midpoint and add one called Extend.
;
; These are not really OSNAP commands inasmuch as their function
; names can be typed while in a command but they can be CALLED,
; AutoLISP can be engaged at almost anytime (except while IN an
; AutoLISP function). While in a LINE command just type (intx)
; from the keyboard or pick the command off the screen menu. You
; should have your menu where you can get to the OSNAP menu at
; anytime while in any command.
;
; INTX.LSP - snap to the intersection of any two lines but the
; two lines do not have to intersection themselves. First code
; written by Tony Tanzillo, A/E Automation Systems 70307,2556

(DeFun IntX (/ L0)            ; Syntax:   (INTX)
  (Inters
    (Car (SetQ L0 (Endpts (EntSel "\nIntersection of line") '(10 11))))
    (Cadr L0)
    (Car (SetQ L0 (Endpts (EntSel " and line ") '(10 11))))
    (Cadr L0)
    Nil)
); end intx.lsp

; MIDX.LSP - snap to the midpoint of any two points.

(DeFun MidX (/ P0 P1)         ; Syntax:   (MIDX)
  (SetQ P0 (GetPoint "\nLocate 1st point "))
  (SetQ P1 (GetPoint P0 "\nLocate 2st point "))
  (MapCar '(LamBDA (x1 x2) (/ (+ x1 x2) 2)) P0 P1)
); end midx.lsp

; EXTX.LSP - now this is a "second" or "to" function, some command
; has to have been stated and a "first" point done such as:
;             LINE 15.25,5.75 (extx)
; where we start a line from point 15.25,5.75 and we call EXTX to
; goto the second point. We are first asked to show which direction
; that we want to go, then we are asked for a line to extend to,
; unlike the EXTEND command, the line we pick does not have to really
; cross the new line we are drawing. At this time we also have the
; option of showing a line that we would want to extend to by entering
; any two points to define a line (which is not there). First code
; written by Tony Tanzillo, A/E Automation Systems 70307,2556

(DeFun ExtX (/ P0 P1 E0 P2 L0); Syntax:   (EXTX)
  (SetQ P0 (GetVar "lastpoint"))
  (SetQ P1 (GetPoint P0 "\nShow angle : "))
  (SetQ E0 (EntSel "\nLine to extend to, or ENTER for 2 points: "))
  (SetQ L0
    (If E0 (EndPts E0 '(10 11)) ; Then
           (ProgN               ; Else
             (List
               (SetQ P2 (GetPoint "\nFrom point : "))
               (GetPoint P2 " to point : ")) ); end else ProgN
    ); endIf
  ); end SetQ
  (If (And P0 P1 (Car L0) (Cdr L0))
    (Apply 'Inters (List P0 P1 (Car L0) (Cadr L0) Nil)) )
); end ext.lsp

; get the endpoints of a LINE entity, first code written
; by Tony Tanzillo, A/E Automation Systems 70307,2556

(DeFun EndPts(Ex Px)          ; Syntax:   (ENDPTS <entity name> <atom list>)
  (Cond
    ( (Atom Px)
      (Cdr (Assoc Px (EntGet (Cond ((Eq (Type Ex) 'ENAME) Ex) ((Car Ex)))))))
    ( T (MapCar '(LamBDA(x) (EndPts Ex x )) Px)) )
); end endpts

; I have used these functions while in AutoCAD commands LINE, MOVE, COPY,
; STRETCH, ROTATE. I can see using them in BREAK, CIRCLE, ARC, MIRROR,
; and ARRAY. These functions do not work in TRIM or EXTEND as they require
; entities not point values.
