; ================================================================
;
;   FLEXD.LSP           Revised 2/87   Bill Kramer
;   Revisions for AutoCAD 2.5+
;   ----------------------------------------------------------
;   Changes applied:
;    1.  Use of GETPOINT with "anchor point" for rubber banding input.
;    2.  Added LINE terminators to COMMAND expressions using LINE.
;    3.  Replaced REDRAW command with expression (redraw).
;    4.  Renamed TURN so that both DUCT.LSP & FLEXD.LSP work in
;        "harmony".  New name is fxTURN for Flex Duct work.
;
; ================================================================
;FLEXD PROGRAM
(DEFUN STRGT ()
; (COMMAND "DIST" P1) (GETPOINT "\nENTER END POINT OF FLEX DUCT :")
; (COMMAND) 
; (SETQ P2 (GETVAR "LASTPOINT"))   <--- See notes in next function. 
 (setq p2 (getpoint p1 "\nEnter End Point of flex duct :"))
 (SETQ Z (+ (ANGLE P1 P2) 1.57079633))
 (SETQ PX (POLAR P1 (ANGLE P1 P2) 9))
 (SETQ P1A (POLAR PX (+ Z 3.14159265) (* W 0.5)))
 (SETQ P1B (POLAR P1A Z W))
 (SETQ P1D (POLAR P1 Z (+ W2 7)))
 (SETQ P1C (POLAR P1 (+ Z 3.1415926) (+ W2 7)))
 (SETQ P2B (POLAR P2 Z W2))
 (SETQ P2A (POLAR P2 (+ Z 3.1415926) W2))
 (COMMAND "LINE" P1D P1B P1A P1C "")
 (COMMAND "LINE" P1A P2A "")
 (COMMAND "LINE" P1B P2B "")
 (SETQ P1 P2)
 (SETQ P1A P2A)
 (SETQ P1B P2B))
(DEFUN fxTURN ()
;   This combination of expressions is converted to....
;
; (COMMAND "DIST" P1) (GETPOINT "\nENTER TURNING POINT OF DUCT: ")
; (COMMAND)
; (SETQ P2 (GETVAR "LASTPOINT"))
;
;   .... this single expression for AutoCAD 2.5+
;
 (setq P2 (getpoint P1 "\nEnter Turning point of Duct: "))
;
;   This combination of expression is converted to....
;
; (COMMAND "DIST" P2) (GETPOINT "\nENTER END OF FLEX DUCT: ")
; (COMMAND)
; (SETQ P3 (GETVAR "LASTPOINT"))
;
;   .... this single expression for AutoCAD 2.5+
;
 (setq P3 (getpoint P2 "\nEnter end of Flex Duct: "))
;
 (SETQ Z (+ (ANGLE P1 P2) 1.57079633))
 (SETQ PX (POLAR P1 (ANGLE P1 P2) 9))
 (SETQ P1A (POLAR PX (+ Z 3.14159265) (* W 0.5)))
 (SETQ P1B (POLAR P1A Z W))
 (SETQ P1D (POLAR P1 Z (+ W2 7)))
 (SETQ P1C (POLAR P1 (+ Z 3.1415926) (+ W2 7)))
 (SETQ Z2 (+ (ANGLE P2 P3) 1.57079633))
 (SETQ P2B (POLAR P2 Z W2))
 (SETQ P2A (POLAR P2 (+ Z 3.1415926) W2))
 (SETQ P2AA (POLAR P2 (+ Z2 3.1415926) W2))
 (SETQ P2BB (POLAR P2 Z2 W2))
 (SETQ P3B (POLAR P3 Z2 W2))
 (SETQ P3A (POLAR P3 (+ Z2 3.1415926) W2))
 (COMMAND "LINE" P1C P1A P1B P1D "")
 (COMMAND "LINE" P1A P2A "")
 (COMMAND "LINE" P1B P2B "")
 (COMMAND "LINE" P2AA P3A "")
 (COMMAND "LINE" P2BB P3B "")
 (SETQ D2 (DISTANCE P1 P2AA))
 (SETQ D1 (DISTANCE P1 P2BB))
 (IF (> D2 D1) (SETQ RB W) (SETQ RB (* 2 W)))
 (IF (> D2 D1) (SETQ RA (* W 2)) (SETQ RA W))
 (COMMAND "FILLET" "R" RB)
 (COMMAND "FILLET" P2B P2BB)
 (COMMAND "FILLET" "R" RA)
 (COMMAND "FILLET" P2A P2AA)
 (SETQ P1 P3)
 (SETQ P1A P3A)
 (SETQ P1B P3B)
)
;
(DEFUN C:FLEXD ()
 (SETVAR "CMDECHO" 0)
 (SETQ W (GETREAL "\nPICK DUCT WIDTH: "))
 (SETQ W2 (* W 0.5))
 (SETQ P1 (GETPOINT "\nENTER BEGINNING OF CENTERLINE: "))
 (SETQ L T)
 (WHILE L
  (setq Test
      (strcase
        (getstring "\nIs direction <S>traight or <T>urn or <Q>uit:")))
  (IF (EQUAL TEST "S") (STRGT))
  (IF (EQUAL TEST "T") (fxTURN))
  (IF (EQUAL TEST "Q") (SETQ L NIL)))
 (COMMAND "LINE" P1A P1B "")
(redraw) 
)
