(DEFUN C:HEALARC ()
        (SETVAR "CMDECHO" 1)
        (SETQ BLIP (GETVAR "BLIPMODE"))
        (SETQ APER (GETVAR "APERTURE"))
        (SETVAR "APERTURE" 1)
        (SETVAR "BLIPMODE" 0)
        (SETQ P1
         (OSNAP
         (GETPOINT "1ST POINT CCW ONLY:")
         "ENDP"))
        (TERPRI)
        (SETQ P2
         (OSNAP
         (GETPOINT "2ND POINT:")"ENDP"))
        (SETQ C2 (OSNAP P2 "CENTER"))
        (SETQ C1 (OSNAP P1 "CENTER"))
        (SETQ R1 (DISTANCE P1 C1))
  (SETQ A1(* (/ 180.0 PI)(- (ANGLE C1 P1)
         (ANGLE C1 (OSNAP P1 "MIDP")))))
  (SETQ A2(* (/ 180.0 pi)(- (ANGLE C1 P2)
         (ANGLE C1 (OSNAP P2 "MIDP")))))
        (SETQ DELTA2 (- (ANGLE C1 
         (OSNAP P2 "MIDP")) (ANGLE C1 P2)))
        (SETQ P6 (POLAR C1 (+ (ANGLE C1
         (OSNAP P2 "MIDP")) DELTA2) R1))
        (SETQ DELTA (- (ANGLE C1
         (OSNAP P1 "MIDP")) (ANGLE C1 P1)))
        (SETQ P5 (POLAR C1 (+ (ANGLE C1
         (OSNAP P1 "MIDP")) DELTA) R1))
        (IF (> (ANGLE C1 P2) (ANGLE C1 P1))
         (PROGN
                (COMMAND "ERASE" P1 "")
                (COMMAND "ERASE" P2 "")
                (COMMAND "ARC" P5 "C" C1 P6))
        (PROMPT "*INVALID*"))
        (SETVAR "BLIPMODE" BLIP)
        (SETVAR "APERTURE" APER)
        (SETVAR "CMDECHO" 1)
)  