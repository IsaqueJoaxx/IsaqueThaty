(VMON)
(DEFUN AUTOHATCH (PTA PTB PTC PTD E1 E2)
   (LAYR "HATCH") (SCLEE)
  (COMMAND "HATCH" "U" 45 (SCLEE) "Y"
  "W" (POLAR PTA (ANGLE PTD PTA) 1.0)
      (POLAR PTD (ANGLE PTA PTD) 1.0) ""
  )
  (IF (= E1 1) (COMMAND "ERASE" "W"
        (POLAR PTA (ANGLE PTD PTA) 1.0)
        (POLAR PTB (ANGLE PTA PTD) 1.0) "")
  )
  (IF (= E2 1) (COMMAND "ERASE" "W"
        (POLAR PTC (ANGLE PTB PTC) 1.0)
        (POLAR PTD (ANGLE PTC PTA) 1.0) "")
  )
)

(DEFUN C:FDN (/ E F G H DCM2 FCL2 FCM2 TW2 ATT
          HL HL2 HL3 HL4 CL CL2 CM CM2 GCL GCL2 GCM GCM2
          PT0 PT1 PT2 PT3 PT4 PT5 PT6 PT7 PT8 PT9 
          P0 P1 P2 P3 P4 P5 P6 P7 P8 P9 PHI PHI1 PHI2 PHI3)
  (SETVAR "CMDECHO" 0) (SETVAR "BLIPMODE" 0)
  (COMMAND "UNDO" "AUTO" "OFF") (SCREEN10A)
  (SETQ PT1 (GETPOINT "\nEnter column center of 1st footing to be drawn: ")
        PT2 (GETPOINT PT1 "\nEnter column center of previous footing: ")
        H 0.0
  )
  (SCREEN10)
  (SETVAR "ORTHOMODE" 1)
  (CRLYR "FDNWALL" 7 "CONTINUOUS")
  (CRLYR "FTG" 4 "CONTINUOUS")
  (CRLYR "HATCH" 3 "CONTINUOUS")
  (PROMPT "*** To end routine: set the next col CL = current col CL ***")
  (SETQ PT3 (GETPOINT PT1 "\nEnter location of next col CL along foundation wall:")
    ATT (- (ANGLE PT1 PTT) (ANGLE PT1 PT3))
  )
  (IF (OR (= (FIX (/ (+ (ABS ATT) 0.00001) PI)) 1)
          (= (FIX (* (ABS ATT) 10000)) 0)
      )
    (SETQ ATT (- (ANGLE PT1 PTT) (ANGLE PT2 PT1)))
  )

  (WHILE (> (DISTANCE PT1 PT3) 0)
    (SETQ D2 (/ (DISTANCE PT2 PT1) 2.0) H (1+ H)
          D3 (/ (DISTANCE PT1 PT3) 2.0)
          G "Enter footing size (square) <"
    )
    (IF (ANGTYP (ANGLE PT1 PT3) (ANGLE PT2 PT1))
     (PROGN
      (SETQ PHI (ANGLE PT1 PT3) PHI1 (ANGLE PT3 PT1)
           PHI2 (ANGLE PT1 PTT)
           PHI3 (ANGLE PTT PT1) PL PL3 PL2 PL4
      )
      (IF (> DCL PL)
        (PIERTWO)
        (PIERONE)
      )
      (SCREENF)
      (FOOTING)
     )
     
     (PROGN
      (TERPRI) (SCREENCNR)
      (SETQ PHI (ANGLE PT2 PT1) PHI1 (ANGLE PT1 PT2)
           PHI2 (ANGLE PT1 PT3)
           PHI3 (ANGLE PT3 PT1)
      )
         (PIERCNR)   ;OUTSIDE CORNER
         (SCREENF)
         (FOOTCNR)
     (SETQ PL PL3 PL2 PL4 TW TW2 DCL DCL2 DCM DCM2 FCL FCL2 FCM FCM2
         PTT (POLAR PT1 (+ PHI2 ATT) 24))
     )
    )
  (COMMAND "ID" PT3) (SCREENNXT)
  (SETQ PT2 PT1 PT1 PT3
        PT3 (GETPOINT PT1 "\nEnter location of next col CL along foundation wall: ")
        PTT (POLAR PTT (ANGLE PT2 PT1) (DISTANCE PT2 PT1)))
  )
(LAYR 0)
)


(DEFUN PIERONE () ;TYPICAL PIER
  (SETQ PT4 (POLAR PT2 PHI  D2)
        PT4 (POLAR PT4 PHI2 DCL)
        PT5 (POLAR PT4 PHI  (- D2 (/ PW 2.0)))
        PT6 (POLAR PT5 PHI2 (- PL DCL))
        PT7 (POLAR PT6 PHI PW)
        PT8 (POLAR PT7 PHI3 (- PL DCL))
        PT9 (POLAR PT8 PHI (- D3 (/ PW 2.0)))
         P4 (POLAR PT4 PHI3 TW)
         P5 (POLAR P4 PHI (- D2 (/ PW 2.0)))
         P6 (POLAR PT6 PHI3 (+ PL PL2))
         P7 (POLAR PT7 PHI3 (+ PL PL2))
         P8 (POLAR P7 PHI2 (- PL2 DCM))
         P9 (POLAR P8 PHI (- D3 (/ PW 2.0)))
  )
  (LAYR "FDNWALL")
  (IF (> PL DCL)
    (COMMAND "LINE" PT4 PT5 PT6 PT7 PT8 PT9 "")
    (COMMAND "LINE" PT4 PT6 PT7 PT9 "")
  )
  (IF (> PL2 DCM)
    (COMMAND "LINE" P4 P5 P6 P7 P8 P9 "")
    (IF (= PL2 DCM)
      (COMMAND "LINE" P4 P6 P7 P9 "")
      (COMMAND "LINE" P4 P9 "")
    )
  )
  (IF (= MAS "Y")
     (PROGN
       (COMMAND "LINE" PT4 P4 "" "LINE" PT9 P9 "")
       (IF (> DCM PL2)
        (PROGN
         (COMMAND "LINE" PT5 P6 P7 PT8 "")
         (AUTOHATCH PT4 P4 PT9 P9 1 1)
        )
        (PROGN
         (COMMAND "LINE" PT5 P5 "" "LINE" PT8 P8 "")
         (AUTOHATCH PT4 P4 PT5 P5 1 0)
         (AUTOHATCH PT8 P8 PT9 P9 0 1)
        )
       )
     )
  )
  
)


(DEFUN PIERTWO () ;FOR DCL>PL ONLY
  (SETQ PT4 (POLAR PT2 PHI  D2)
        PT4 (POLAR PT4 PHI2 DCL)
        PT9 (POLAR PT4 PHI  (+ D2 D3))
         P4 (POLAR PT4 PHI3 TW)
         P5 (POLAR P4 PHI (- D2 (/ PW 2.0)))
         P6 (POLAR P5 PHI2 (+ PL DCM))
         P7 (POLAR P6 PHI PW)
         P8 (POLAR P5 PHI PW)
         P9 (POLAR P8 PHI (- D3 (/ PW 2.0)))
        PT5 (POLAR P6 PHI3 (+ PL PL2))
        PT6 (POLAR P7 PHI3 (+ PL PL2))
  )
  (LAYR "FDNWALL")
  (COMMAND "LINE" PT4 PT9 "")
  (IF (> DCM PL)
    (COMMAND "LINE" P6 PT5 PT6 P7 "")
    (COMMAND "LINE" P5 PT5 PT6 P8 "")
  )
  (COMMAND "LINE" P4 P5 P6 P7 P8 P9 "")
   (IF (= MAS "Y")
    (PROGN
       (COMMAND "LINE" PT4 P4 "" "LINE" PT9 P9 "")
       (AUTOHATCH PT4 P4 PT9 (POLAR P9 PHI2 (MIN (+ PL DCM) 0)) 1 1)
    )
   )
   
  )


(DEFUN FOOTING ( / X Y YM) ;MIDWALL FTG
  (SETQ PT4 (POLAR PT2 PHI  D2)
        PT4 (POLAR PT4 PHI2 (+ DCL FCL))
       X (/ FTGSIZE 2.0) Y (MIN FCL FCM) YM (MAX FTGSIZE (+ PW Y Y))
         P3 (POLAR PT2 PHI (- D2 (- D2) (MAX X (+ (/ PW 2.0) Y))))
         P4 (POLAR PT4 PHI3 (+ TW FCL FCM))
        PT5 (POLAR P3 PHI2 (+ DCL FCL))
        PT6 (POLAR P3 PHI2 (MAX X (+ PL Y)))
         P5 (POLAR P3 PHI3 (+ DCM FCM))
         P6 (POLAR P3 PHI3 (MAX X (+ PL2 Y)))
        PT7 (POLAR PT6 PHI YM)
        PT8 (POLAR PT5 PHI YM)
         P7 (POLAR P6 PHI YM)
         P8 (POLAR P5 PHI YM)
        PT9 (POLAR PT8 PHI (- D3 (MAX X (+ (/ PW 2.0) Y))))
         P9 (POLAR PT9 PHI3 (+ TW FCL FCM))
  )
  (LAYR "FTG")
  (IF (>= (DISTANCE P3 PT5) (DISTANCE P3 PT6))
    (COMMAND "LINE" PT4 PT9 "")
    (COMMAND "LINE" PT4 PT5 PT6 PT7 PT8 PT9 "")
  )
  (IF (>= (DISTANCE P3 P5) (DISTANCE P3 P6))
    (COMMAND "LINE" P4 P9 "")
    (COMMAND "LINE" P4 P5 P6 P7 P8 P9 "")
  )
   
)


(DEFUN FOOTCNR ( / X Y Y2 P13 P17)
  (SETQ X (/ FTGSIZE 2.0) Y (MIN FCL FCM) Y2 (MIN FCL2 FCM2)
        PT4 (POLAR PT2 PHI D2)
        PT4 (POLAR PT4 PHI3 (+ DCL FCL))
   P7 (POLAR PT2 PHI (- D2 (- D2) (MAX X (+ DCM2 FCM2) (+ PL4 Y2))))
   P3 (POLAR PT3 PHI3 (- D3 (- D3) (MAX X (+ DCM FCM) (+ PL2 Y))))
   P17 (POLAR PT2 PHI (- D2 (- D2) (MAX X (+ PL4 Y2))))
   P13 (POLAR PT3 PHI3 (- D3 (- D3) (MAX X (+ PL2 Y))))
        PT5 (POLAR P17 PHI3 (+ DCL FCL))
        PT6 (POLAR P17 PHI3 (MAX X (+ PL Y)))
       P0 (POLAR PT2 PHI (+ D2 D2 (MAX X (+ DCL2 FCL2) (+ PL3 Y2))))
       PT0 (POLAR P0 PHI3 (MAX X (+ DCL FCL) (+ PL Y)))
         P5 (POLAR P7 PHI2 (+ DCM FCM))
         P6 (POLAR P7 PHI2 (MAX X (+ PL2 Y)))
         P8 (POLAR P3 PHI1 (+ DCM2 FCM2))
        PT7 (POLAR P13 PHI (MAX X (+ PL3 Y2)))
        PT8 (POLAR P13 PHI (+ DCL2 FCL2))
        PT9 (POLAR PT8 PHI2 (- D3 (MAX X (+ DCM FCM) (+ PL2 Y))))
         P4 (POLAR PT4 PHI2 (+ TW FCL FCM))
         P9 (POLAR PT9 PHI1 (+ TW2 FCL2 FCM2))
  )
  (LAYR "FTG")
  (IF (>= (DISTANCE P17 PT5) (DISTANCE P17 PT6))
    (COMMAND "LINE" PT4 PT0)
    (COMMAND "LINE" PT4 PT5 PT6 PT0)
  )

  (IF (>= (DISTANCE P13 PT8) (DISTANCE P13 PT7))
    (COMMAND PT9 "")
    (COMMAND PT7 PT8 PT9 "")
  )
  (SETQ P7 (POLAR P7 PHI3 (DISTANCE P7 P5))
        P3 (POLAR P3 PHI  (DISTANCE P3 P8))
  )

  (COMMAND "LINE" P4 P5 )
  (IF (< (DISTANCE P7 P5) (DISTANCE P7 P6))
    (IF (< (DISTANCE P3 P8) (DISTANCE P3 P6))
      (COMMAND P6 P8 P9 "")
      (COMMAND P6 P9 "")
    )
    (IF (> (DISTANCE P3 P5) (DISTANCE P3 P8))
      (COMMAND P8 P9 "")
      (COMMAND P9 "")
    )
  )
   
)
(DEFUN PIERCNR ()
  (IF (> DCM PL2)  ;TYPES 1, 2, 3, 4 AND 5
    (IF (= DCL (- PL2)) 
      (TYPE2)
      (IF (AND (< DCL PL) (< (- PL2) DCL))
        (TYPE3)
        (TYPENOT)
      )
    )
  )
  (IF (= DCM PL2)  ;TYPES 6, 7 AND 8
    (IF (< DCL PL)
      (TYPE6)
      (TYPENOT)
    )
  )
  (IF (AND (< DCM PL2) (< (- DCM) PL))  ;TYPES 9, 10 AND 11
    (IF (<= DCL PL)
      (TYPE910)
      (TYPE1112)
    )
  )
  (IF (= DCM (- PL)) (TYPE1112))
  (IF (> (- DCM) PL) (TYPENOT))
)


(DEFUN TYPENOT ()
  (TERPRI)
(PRINT "Bad news friend, you have stumbled upon one of the few combinations")
(PRINT "that I can not draw.  (I am a moron, really.) Next I will try to draw")
(PRINT "the footing.  Strike the return key to continue.")
  (GTSTRNG ""  ""  " it now <")
)

(DEFUN SLCT (A B C D)
  (COMMAND "SELECT" "W" (LECT A B) (LECT B A)
                    "W" (LECT C D) (LECT D C) "")
)

(DEFUN SLCT3 (A B C D E F)
  (COMMAND "SELECT" "W" (LECT A B) (LECT B A)
                    "W" (LECT C D) (LECT D C) 
                    "W" (LECT E F) (LECT F E) "")
)

(DEFUN LECT (X Y)
  (POLAR X (ANGLE Y X) 1.0)
)

(DEFUN AUTOHATCHP ()
   (LAYR "HATCH") (SCLEE)
  (COMMAND "HATCH" "U" 45 (SCLEE) "Y" "P" ""
    "ERASE" "W" (LECT PT4 P9) (POLAR P4 (ANGLE PT4 P9) 1.0) ""
    "ERASE" "W" (LECT PT9 P4) (POLAR P9 (ANGLE PT9 P4) 1.0) ""
  )
)


(DEFUN PUNTS ( / PPL PPL3)
  (SETQ PT4 (POLAR PT2 PHI D2)
        PT4 (POLAR PT4 PHI3 DCL)
        PPL (+ PL PL2) PPL3 (+ PL3 PL4)
        P7 (POLAR PT4 PHI (- D2 PL4))
        PT5 (POLAR P7 PHI3 (- PL DCL))
        PT6 (POLAR PT5 PHI PPL3)
        PT7 (POLAR PT6 PHI2 PPL)
        P6  (POLAR PT7 PHI1 PPL3)
        P3 (POLAR PT7 PHI1 (- PL3 DCL2))
        P8 (POLAR P3 PHI1 TW2)
       PT8 (POLAR PT4 PHI (+ DCL2 D2))
       PT0 (POLAR PT4 PHI (+ D2 PL3))
       PT9 (POLAR P3 PHI2 (- D3 PL2))
       P4 (POLAR PT4 PHI2 TW)
       P9 (POLAR PT9 PHI1 TW2)
       P5 (POLAR P4 PHI (- D2 (MAX PL4 DCM2)))
       P0 (POLAR PT8 PHI2 (- DCL PL))
  )
)


(DEFUN DRAWINS ()
  (COMMAND "LINE" P4 P5)
  (IF (> DCM2 PL4)
    (COMMAND P9 "")  ;TYPE B & C
    (IF (= DCM2 PL4)
      (COMMAND P6 P9 "") ;TYPE D
      (IF (> (- DCL2 TW2) PL3)
        (COMMAND P6 PT7 (POLAR PT0 PHI2 TW) 
           (POLAR P9 PHI3 (+ D3 DCL (- TW))) P9 ""); TYPE I
        (COMMAND P6 P8 P9 "") ;TYPE E, H, J, K
      )
    )
  )
)


(DEFUN DRAW910 ()
  (LAYR "FDNWALL")
  (COMMAND "LINE" PT4 P7)
  (IF (> (DISTANCE P7 PT5) 0)
    (COMMAND PT5 PT6) ;TYPE 9
    (COMMAND PT6) ;TYPE 10
  )
  (IF (> DCL2 PL3)
    (IF (> (DISTANCE PT6 PT0) 0)
      (COMMAND PT0 PT8 PT9 "")  ;TYPE I, J, K
      (COMMAND PT8 PT9 "") ;TYPE 10 - I, J, K
    )
    (IF (> (DISTANCE PT7 P3) 0)
      (COMMAND PT7 P3 PT9 "")  ;TYPE B, C, D, E
      (COMMAND PT7 PT9 "")  ;TYPE H
    )
  )
)


(DEFUN TYPE910 ()
  (PUNTS)
  (DRAW910)
  (DRAWINS)
  (IF (= MAS "Y") (MAS910))
  
)


(DEFUN MAS910 ()
  (COMMAND "LINE" PT4 P4 "" "LINE" PT9 P9 "")
  (IF (> DCM2 PL4)
    (PROGN
      (IF (> (DISTANCE P6 P3) 0)
        (COMMAND "LINE" P7 P6 P3 "")  ;TYPE C
        (COMMAND "LINE" P7 P3 "") ;TYPE B
      )
      (SLCT PT4 P3 PT8 P9)
    )
    (IF (> DCL2 PL3) ;TYPE I, J, K
      (PROGN
        (IF (> (- DCL2 TW2) PL)
      (COMMAND "LINE" P7 P5 "" "LINE" PT0 (POLAR PT0 PHI2 TW) ""); I
          (IF (= (- DCL2 TW2) PL)
            (COMMAND "LINE" P7 P5 "" "LINE" PT0 PT7 "")  ;J
            (COMMAND "LINE" P7 P5 "" "LINE" PT0 PT7 P8 "")  ;K
          )
        )
        (IF (> (- DCL2 TW2) PL)
          (SLCT3 PT4 P5 PT0 (POLAR PT8 PHI2 TW) PT8 P9) ;TYPE I
          (SLCT PT4 P5 PT8 P9)
        )
      )
      (PROGN
        (COMMAND "LINE" P7 P5 "" "LINE" P8 P3 "") ;TYPE D, E, H
        (SLCT PT4 P5 P8 PT9)
      )
    )
  )
  (AUTOHATCHP)
)


(DEFUN TYPE1112 ()
  (PUNTS)
  (LAYR "FDNWALL")
  (COMMAND "LINE" PT4 PT8)
  (IF (> DCL2 PL3)
    (COMMAND PT9 "") ;TYPE I,J&K
    (IF (= DCL2 PL3)
      (COMMAND P0 PT7 PT9 "") ;TYPE H
      (COMMAND P0 PT6 PT7 P3 PT9 "") ;TYPE B,C,D&E
    )
  )
  (DRAWINS)
  (IF (= MAS "Y") (MAS1112))
  
)

(DEFUN MAS1112 ()
  (COMMAND "LINE" PT4 P4 "" "LINE" PT9 P9 "")
  (IF (> DCM2 PL4)
    (PROGN
      (IF (> (DISTANCE P6 P3) 0)
        (COMMAND "LINE" P0 PT5 P6 P3 "") ;TYPE C
        (COMMAND "LINE" P0 P6 "")        ;TYPE B
      )
      (SLCT PT4 P3 P5 PT9)
    )
    (IF (> DCL2 PL3)
      (PROGN
        (IF (> (- DCL2 TW2) PL3)
          (COMMAND "LINE" P5 PT5 PT6 (POLAR PT0 PHI2 TW) "") ;I
          (IF (= (- DCL2 TW2) PL3)
            (COMMAND "LINE" P5 PT5 PT6 PT7 "") ;TYPE J
            (COMMAND "LINE" P5 PT5 PT6 PT7 P8 "") ;TYPE K
          )
        )
        (SLCT PT4 (POLAR PT8 PHI2 TW) PT8 P9) ;I,J&K
      )
      (PROGN
        (IF (< (- DCL TW) PL)
          (COMMAND "LINE" P0 PT5 P5 "" "LINE" P8 P3 "") ;11-D,E&H
          (COMMAND "LINE" P0 P5 "" "LINE" P8 P3 "") ;12-D,E&H
        )
        (SLCT PT8 P4 P8 PT9)
      )
    )
  )
  (AUTOHATCHP)
)


(DEFUN ANGTYP (ANG ANG2)
  (SETQ ANG (+ ANG 0.00001)
       ANG2 (+ ANG2 0.00001)
  )
  (IF (>= ANG (+ PI PI)) (SETQ ANG 0.0))
  (IF (>= ANG2 (+ PI PI)) (SETQ ANG2 0.0))
  (= (FIX (* ANG 10000)) (FIX (* ANG2 10000))) ;T=STRAIGHT
)

(DEFUN CRLYR (NME CLR LNTYP) ;CREATE A LAYER
  (COMMAND "LAYER" "NEW" NME "COLOR" CLR NME "LTYPE" LNTYP NME "")
)

(DEFUN LAYR (X) 
  (COMMAND "LAYER" "SET" X "")
)


(DEFUN SCLEE (/ X Y)
    (SETQ HATCHSPC 6.0)
)

(DEFUN *ERROR* ( G )
  (PRINC G) (PRINC " sorry for the inconvenience.") (TERPRI)
)

(SETQ G (PRINC "*** FOUNDATION GENERATOR LOADED ***") G (TERPRI))
