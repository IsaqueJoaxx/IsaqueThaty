;
; Trig Calculations (c)1994, Gerry R. Bruno
; GB Engineering Co. 72466,1210
;
; Triangle Solutions - This program can be used to find the area, the
; dimensions of the sides (S1, S2, S3) and the angles (A1, A2, A3) of
; a triangle, and Area.  
;
;          /\		SSS - Three Sides		S1, S2, S3
;        / A1 \		ASS - A Angle then Side & Side	A2, S3, S1
;   S1 /        \ S2	SAS - Side an Angle and a Side	S2, A2, S3
;    /            \	SSA - Side, Side and Angle	S1, S2, A2
;  / A3          A2 \	AAS - Angle, Angle and a Side	A1, A2, S3
;  - - - - - - - - - -	ASA - Angle then Side and Angle	A1, S2, A2
;          S3		SAA - A Side and two Angles	S1, A1, A2
;			AAA - All Angles, assumes side = 1
;
; Note that the triangle described dose not conform to the standard
; triangle notation, and variable entry may be clockwise or counter-
; clockwise.

; Returns ASin of 'X'
(Defun ASin (X) (ATan (/ X (Sqrt(+ (* X (* -1 X)) 1)))))

; Returns ACos of 'X'
(Defun ACos (X) (+ (* -1.0 (ATan (/ X (Sqrt(+ (* X (* -1 X)) 1))))) (/ Pi 2)))

; Returns the Cos of the Angle in Radians between a and b (as CA)
;	c^2 - a^2 - b^2 / -2ab
(Defun abc (a b c) (/ (- (* c c) (* a a) (* b b)) (* -1 (* 2 a b))))

; Returns the length of the 'c' side
;	a^2 + b^2 - 2ab(CosC) 
(Defun aCAb (a CA b) (Sqrt (- (+ (* a a) (* b b)) (* 2 a b (Cos CA)))))

; Internal error handler defined locally
(Defun *error* (msg)
   (princ "error: ")
   (princ msg)
   (SetVar "AUPrec" AUP)
   (SetVar "LUPrec" LUP)
   (SetVar "CmdEcho" CMD)
   (Exit)
   (terpri)
   (princ))

;Main trig program
(Defun C:Trig ( / CMD AUP LUP Func S1 S2 S3 A1 A2 A3 AREA)
  (Setq CMD (GetVar "CmdEcho"))
  (SetVar "CmdEcho" 0)		;Kill the Echo
  (Setq AUP (GetVar "AUPrec"))
  (SetVar "AUPrec" 4)		;Make 4 decimal
  (Setq LUP (GetVar "LUPrec"))
  (SetVar "LUPrec" 4)		;Make 4 decimal

  (Setq Func (StrCase (GetString "\nFunction: SSS ASS SAS SSA AAS ASA SAA AAA ")))

   (If (or (= Func "SSS") (= Func "SSA") (= Func "SAA") (= Func "SAS"))
     (Setq S1 (GetReal "Enter Side 1 = ")))
 
   (If (or (= Func "SAA") (= Func "SAS") (= Func "ASS") (= Func "AAS") (= Func "ASA") (= Func "AAA"))
     (Setq A1 (GetAngle "Enter Angle 1 = ")))
 
   (If (or (= Func "SSS") (= Func "SSA") (= Func "SAS") (= Func "ASS") (= Func "ASA"))
     (Setq S2 (GetReal "Enter Side 2 = ")))
 
   (If (or (= Func "SSA") (= Func "SAA") (= Func "AAS") (= Func "ASA") (= Func "AAA"))
     (Setq A2 (GetAngle "Enter Angle 2 = ")))
 
   (If (or (= Func "SSS") (= Func "ASS") (= Func "AAS"))
     (Setq S3 (GetReal "Enter Side 3 = ")))
 
   (If (= Func "AAA")
     (Setq A3 (GetAngle "Enter Angle 3 = ")))
   
; AAA ------------------------------------------------------------------------
   (If (= Func "AAA")
     (Progn
       (Setq S1 1.0)
       (If (/= Pi (+ A1 A2 A3)) (*Error* "Sum of the Angles do not add up - "))
      )
    )

; SAA ------------------------------------------------------------------------
   (If (= Func "SAA") 
       (Setq A3 (- Pi A1 A2)))
   (If (or (= Func "SAA") (= Func "AAA")) 
     (Progn
       (Setq S2 (/ (* S1 (Sin A3)) (Sin A2)))
       (Setq S3 (/ (* S1 (Sin A1)) (Sin A2)))
      )
    )

; ASA ------------------------------------------------------------------------
   (If (= Func "ASA") 
     (Progn
       (Setq A3 (- Pi A1 A2))
       (Setq S1 (/ (* S2 (Sin A2)) (Sin A3)))
       (Setq S3 (/ (* S1 (Sin A1)) (Sin A2)))
      )
    )

; AAS ------------------------------------------------------------------------
   (If (= Func "AAS") 
     (Progn
       (Setq A3 (- Pi A1 A2))
       (Setq S1 (/ (* S3 (Sin A2)) (Sin A1)))
       (Setq S2 (/ (* S3 (Sin A3)) (Sin A1)))
      )
    )

; ASS ------------------------------------------------------------------------
   (If (= Func "ASS") 
     (Progn
       (Setq A3 (ASin (/ (* S2 (Sin A1)) S3)))
       (Setq A2 (- Pi A1 A3))
       (Setq S1 (/ (* S3 (Sin A2)) (Sin A1)))
      )
    )

; SSA ------------------------------------------------------------------------
   (If (= Func "SSA") 
     (Progn
       (Setq A3 (ASin (/ (* S2 (Sin A2)) S1)))
       (Setq A1 (- Pi A2 A3))
       (Setq S3 (/ (* S1 (Sin A1)) (Sin A2)))
      )
    )

; SAS ------------------------------------------------------------------------
   (If (= Func "SAS")
     (Setq S3 (aCAb S1 A1 S2)))

; SSS ------------------------------------------------------------------------
   (If (or (> S1 (+ S2 S3)) (> S2 (+ S1 S3)) (> S3 (+ S1 S2)))
     (*Error* "The sides of the Triangle do not match - "))

   (If (= Func "SSS")
     (Setq A1 (ACos (abc S1 S2 S3))))

   (If (or (= Func "SSS") (= Func "SAS"))
     (Setq A2 (ACos (abc S2 S3 S1))))

   (If (or (= Func "SSS") (= Func "SAS"))
     (Setq A3 (ACos (abc S3 S1 S2))))

   (Setq Area (* 0.5 S1 S2 (Sin A1)))

   (Princ "\n ")
   (Princ (StrCat " S1= " (RtoS S1) " A1= " (AngtoS A1) " S2= " (RtoS S2) " A2= " (AngtoS A2) " S3= " (RtoS S3) " A3= " (AngtoS A3) " Area= " (RtoS Area)))
   (Princ "\n ")

   (SetVar "AUPrec" AUP)
   (SetVar "LUPrec" LUP)
   (SetVar "CmdEcho" CMD)
   (Princ)
   )

