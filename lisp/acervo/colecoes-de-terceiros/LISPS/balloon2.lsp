;ANDY DIMACALE 10/16/86
;
;This program creates balloons for tagging parts of identifying
;roughing connecitons.
;
;
(DEFUN C:BALLOON ()
(SETVAR "CMDECHO" 0)
(GRAPHSCR)
(IF(= RAD NIL) (SETQ RAD(/ (GETREAL "\nENTER DIAMETER OF BALLOON: ") 2)))
(IF(= TXTL NIL) (SETQ TXTL (GETSTRING "\nENTER TEXT LAYER: ")))
(IF(= TS NIL) (SETQ TS (GETREAL "\nENTER TEXT HEIGHT: ")))
(SETVAR "BLIPMODE" 1)
     (SETQ X      (GETPOINT "\nFIRST POINT: ")
           Y      (GETPOINT "\nCENTER POINT: ")
           TXT    (GETSTRING "\nENTER TEXT: ")
           DIST   (DISTANCE X Y)
           ANG    (ANGLE X Y)
           YY     (POLAR X ANG (- DIST RAD))
           LAY    (GETVAR "CLAYER")
     )
(SETVAR "BLIPMODE" 0)
(COMMAND "LINE" X YY "")
 (COMMAND "CIRCLE" Y RAD)
  (COMMAND "LAYER" "S" TXTL "")
   (COMMAND "TEXT" "M" Y TS "")
    (COMMAND TXT)
     (COMMAND "LAYER" "S" LAY "")
)
