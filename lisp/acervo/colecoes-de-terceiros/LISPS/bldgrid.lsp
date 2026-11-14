; THIS IS BLDGRID.LSP - IT WILL GENERATE A BUILDING GRID
; By Robert Atwood                                   January 10, 1988
(DEFUN C:BLDGRID (/ H HL V N PNT C P1 P2 T1 PNTE PNTV)
 (SETQ SCALE (GETVAR "LTSCALE"))
 (COMMAND "ELEV" 0 0
          "LAYER" "M" "AR0-GRID" "C" "W" "" "L" "GRID" "" ""
          "STYLE" "SIMPLE" "SIMPLEX" (* SCALE 0.14025) "" "" "" "" ""
          "TEXT" "S" "SIMPLE" \e)
 (SETQ PNT (GETPOINT "\nStarting point (Upper Left Corner) :")
       H (GETDIST PNT "\nHorizontal Grid Spacings :")
       HL (GETDIST PNT "\nLength of Horizontal Lines :")
       N (GETINT "\nNumber of Horiz. lines :")
       V (GETDIST PNT "\nVertical Grid Spacings <Default=Horiz.> :")
       C 0 A 65
       B (- (CAR PNT) 60))
 (IF (< B (/ SCALE 2.66))(SETQ B (/ SCALE 2.66)))
 (IF (= V NIL)(SETQ V H))
 (SETVAR "BLIPMODE" 0)
 (REPEAT N
  (SETQ P1 (LIST (CAR PNT) (+ (CADR PNT) C))
        P2 (LIST (+ (CAR PNT) HL) (+ (CADR PNT) C))
        P1A (POLAR P1 PI B)
        P2A (POLAR P2 0 B)
        P1C (POLAR P1A PI (/ SCALE 4.0)))
  (COMMAND "LINE" P1A P2A ""
           "CIRCLE" P1C (/ SCALE 4.0)
           "TEXT" "M" P1C 0 (CHR A)
  )
  (SETQ C (- C H) A (1+ A))
 )
 (SETQ PNTV P1 PNTE P2 T1 1 C 0 A 0)
 (WHILE T1
  (SETQ P1 (LIST (+ (CAR PNT) C) (CADR PNT))
        P2 (LIST (+ (CAR PNTV) C) (CADR PNTV))
        P1B (POLAR P1 (/ PI 2.0) B)
        P2B (POLAR P2 (* PI 1.5) B)
        P2C (POLAR P1B (/ PI 2.0) (/ SCALE 4.0))
        C (+ C V) A (1+ A))
  (IF (>= (CAR P1) (+ (CAR PNTE) (/ H 2.0)))(SETQ T1 NIL)
   (COMMAND "LINE" P1B P2B ""
            "CIRCLE" P2C (/ SCALE 4.0)
            "TEXT" "M" P2C 0 A
  ))
 )
 (COMMAND "TEXT" "S" LTEXT \e)
 (PRINC)
)

; END OF FILE
