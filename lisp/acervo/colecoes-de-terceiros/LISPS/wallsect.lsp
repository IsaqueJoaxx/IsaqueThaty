;This program will construct an exterior wall section.
;Brigitte Robertson - Student  2-1-94
;Billings Vocational Technical Center 
;3803 Central Avenue
;Billings, MT  59102
;406-656-4445
;
;contact Tim Urbaniak - (Instructor) for additional information
;        76264,2273
;
;Not bad for my first shot at LISP programs!!!
;
;This program was created for the DR238 Programming for CAD Applications
;class at BVTC during a session on the POLAR command.  This program
;is hereby placed into the public domain.  
;
;          *** Support Education in your Area!!! ***
;
(Defun c:wallsect ()
(Setvar "cmdecho" 0)
(Setq cl (Getvar "clayer"))
(Setq ww (Getdist "\nWall Width <0-6>: "))
 (if (= ww nil) (setq ww 6))
(Setq wh (Getdist "\nWall Height <8-0>: "))
 (if (= wh nil) (setq wh 96))
(Setq jw (Getdist "\nFloor Joist Depth <1-0>: "))
 (if (= jw nil) (setq jw 12))
(Setq oh (Getdist "\nEave Overhang on End <2-0>: "))
 (if (= oh nil) (setq oh 24))
(Setq ftw (Getdist "\nFooting Width <1-4>: "))
 (if (= ftw nil) (setq ftw 16))
(Setq fth (Getdist "\nFooting Height <0-10>: "))
 (if (= fth nil) (setq fth 10))
(Setq fdw (Getdist "\nFoundation Width <0-8>: "))
 (if (= fdw nil) (setq fdw 8))
(Setq fdh (Getdist "\nFoundation Height <8-0>: "))
 (if (= fdh nil) (setq fdh 96))
(Setq bf (Getdist "\nBasement Floor Thickness <0-4>: "))
 (if (= bf nil) (setq bf 4))
   (graphscr)
(Setq p1 (Getpoint "\nPick a Point: (Corner of Floor and Inside Wall): "))
;End of Getting Information

(Setq p2 (Polar p1 (dtr 90) wh))          ;Start of setting all those 
(Setq p3 (Polar p2 (dtr 180) 0.5))        ;miserable points.
(Setq p4 (Polar p3 (dtr 90) 0.625))
(Setq p5 (Polar p4 (dtr 180) ww))
(Setq p6 (Polar p5 (dtr 270) 2))
(Setq p7 (Polar p6 (dtr 270) 2))
(Setq p8 (Polar p7 (dtr 0) ww))
(Setq p9 (Polar p8 (dtr 90) 2))
(Setq p10 (Polar p3 (dtr 180) ww))
(Setq p11 (Polar p10 (dtr 180) 0.5))
(Setq p12 (Polar p11 (dtr 180) 0.5))
(Setq p13 (Polar p3 (dtr 270) wh))
(Command "layer" "m" "wood" "")           ;Due to confusion, I drew the
(Command "pline" p1 p2 p3 p13 "")         ;interior gypsum and bottom
(Command "pline" p8 p7 p9 p6 p8 "")       ;top plate here.
(Setq p14 (Polar p13 (dtr 180) ww))
(Setq p15 (Polar p14 (dtr 90) 2))
(Setq p16 (Polar p14 (dtr 270) 0.75))
(Setq p17 (Polar p16 (dtr 0) 2))
(Setq p18 (Polar p17 (dtr 0) 2))
(Setq p19 (Polar p18 (dtr 270) jw))
(Setq p20 (Polar p19 (dtr 180) 2))
(Setq p21 (Polar p20 (dtr 180) 2))
(Setq p22 (Polar p21 (dtr 270) 2))
(Setq p23 (Polar p22 (dtr 0) 6))
(Setq p24 (Polar p23 (dtr 90) 2))
(Setq p25 (Polar p22 (dtr 180) 0.5))
(Setq p26 (Polar p25 (dtr 270) 0.5))
(Setq p27 (Polar p26 (dtr 180) 0.5))
(Setq p28 (Polar p22 (dtr 0) fdw))
(Setq p29 (Polar p28 (dtr 270) fdh))
(Setq p30 (Polar p22 (dtr 270) fdh))
(Setq p31 (Polar p30 (dtr 180) (/ (- ftw fdw) 2)))
(Setq p32 (Polar p31 (dtr 270) fth))
(Setq p33 (Polar p32 (dtr 0) ftw))
(Setq p34 (Polar p33 (dtr 90) fth))
(Setq p35 (Polar p29 (dtr 180) (/ (- fdw 4.5) 2)))
(Setq p36 (Polar p35 (dtr 240) 1.5))
(Setq p37 (Polar p36 (dtr 180) 3))
(Setq p38 (Polar p37 (dtr 120) 1.5))
(Setq p39 (Polar p12 (dtr 180) oh))
(Setq p40 (Polar p39 (dtr 90) 0.625))
(Setq p41 (Polar p40 (dtr 90) 6))
(Setq p42 (Polar p41 (dtr 0) 2))
(Setq p43 (Polar p42 (dtr 270) 6))             ;Trusses are 2 x 6
(Setq p44 (Polar p41 (dtr 180) 0.5))
(Setq p79 (Polar p44 (dtr 120) 0.5))
(Setq p80 (Polar p79 (dtr 120) 0.5))
(Setq p45 (Polar p44 (dtr 270) 8))
(Setq p46 (Polar p45 (dtr 0) 0.5))
(Setq p48 (Polar p43 (dtr 30) 12))
(Setq p49 (Polar p2 (dtr 0) 48))
(Setq p50 (Polar p49 (dtr 90) 0.625))
(Setq p51 (Polar p50 (dtr 90) 6))
(Setq x (/ (- (+ 24 24 ww 1.5 OH) 2) 0.866))       ;The roof is set at a 30
(Setq x1 (/ (+ 24 24 ww 2 OH) 0.866))              ;degree inside angle.
(Setq x2 (/ (+ 24 24 ww 2.25 OH) 0.866))           ;Theese formulas figure
(Setq x3 (/ (+ 24 24 ww 2.5 OH) 0.866))            ;how long the roof line is.
(Setq p53 (Polar p80 (dtr 30) x3))
(Setq p54 (Polar p79 (dtr 30) x2))
(Setq p52 (Polar p43 (dtr 30) x))
(Setq p55 (Polar p44 (dtr 30) x1))
(Setq p56 (Polar p1 (dtr 0) 19))
(Setq p57 (Polar p56 (dtr 270) 0.75))
(Setq p58 (Polar p57 (dtr 270) jw))
(Setq p59 (Polar p29 (dtr 90) bf))
(Setq p60 (Polar p59 (dtr 0) 18))
(Setq p61 (Polar p60 (dtr 270) bf))
(Setq p62 (Polar p15 (dtr 0) ww))
(Setq p63 (Polar p60 (dtr 90) 1))
(Setq p64 (Polar p60 (dtr 335) 1.5))
(Setq p65 (Polar p64 (dtr 225) 2.5))
(Setq p66 (Polar p61 (dtr 270) 1))
(Setq p67 (Polar p56 (dtr 90) 2))
(Setq p68 (Polar p57 (dtr 270) 3))
(Setq p69 (Polar p68 (dtr 315) 2))
(Setq p70 (Polar p69 (dtr 210) 4))
(Setq p71 (Polar p70 (dtr 315) 4))
(Setq p72 (Polar p58 (dtr 270) 2))
(Setq p73 (Polar p54 (dtr 90) 2))
(Setq p74 (Polar p49 (dtr 270) 2))              ;Break line points
(Setq p75 (Polar p52 (dtr 270) 7))
(Setq p76 (Polar p75 (dtr 315) 2))
(Setq p77 (Polar p76 (dtr 225) 4))
(Setq p78 (Polar p77 (dtr 315) 2))

;Start of connecting the dots
(Command "line" p9 p4 p5 p6 p4 p50 p49 p2 "")
(Command "line" p24 p58 "")
(Command "line" p10 p22 p24 p23 "")
(Command "line" p23 p21 p24 p58 "")
(Command "line" p21 p17 p19 p18 p20 p16 p18 "")
(Command "line" p16 p20 "")
(Command "line" p14 p56 "")
(Command "line" p18 p57 "")
(Command "line" p13 p15 p62 p14 "")
(Command "line" p10 p39 p40 p5 p9 "")
(Command "line" p11 p25 p22 "")
(Command "line" p12 p27 p26 p25 "")
(Command "line" p40 p43 p42 p40 p41 p43 "")
(Command "line" p39 p46 p45 p44 p42 "")
(Command "line" p63 p60 "")
(Command "line" p61 p66 "")
(Command "line" p67 p68 p69 p70 p71 p58 p72 "")
(Command "line" p43 p52 "")
(Command "line" p80 p53 "")
(Command "line" p79 p54 "")
(Command "line" p80 p79 p44 "")
(Command "line" p44 p55 "")
(Command "line" p48 p51 "")
(Command "line" p73 p53 p54 p55 p52 p75 p76 p77 p78 p51 p50 p49 p74 "")
(Command "line" p17 p20 "")

;Start of Concrete and Hatching
(Command "Layer" "m" "conc"  "")
(Command "Pline" p30 p38 p37 p36 p35 p34 p33 p32 p31 "c")
  (setq e1 (entlast))
(Command "Hatch" "AR-CONC" "1" "" e1 "")
(Command "Pline" p29 p28 p22 p30 p38 p37 p36 p35 "c")
  (setq e2 (entlast))
(Command "Hatch" "AR-CONC" "1" "" e2 "")
(Command "Pline" p59 p60 P64 p65 p61 p29 "c")
  (setq e3 (entlast))
(Command "Hatch" "AR-CONC" "1" "" e3 "")

;Start of insulation Hatching on roof
(Setq A (Polar p43 (dtr 90) 3))              
(Setq B (Polar A (dtr 0) 0.5))    ;First pt. on 1st arc
(Setq C (Polar B (dtr 0) 4))      ;Second pt. on 1st, 1st pt. on 2nd arc
(Setq J (Polar C (dtr 0) 4))      ;Forgot it, Second pt. on 2nd arc
(Command "Arc" B "@3.2016<309" C)
 (setq a1 (entlast))
(Command "Arc" C "@3.2016<51" J)
 (setq a2 (entlast))
(Command "Array" a1 a2 "" "R" "1" "6" "8")
(Setq D (Polar p48 (dtr 180) 3))
(Setq E (Polar D (dtr 90) 3.5))
(Setq F (Polar E (dtr 0) 4))
(Setq K (Polar F (dtr 0) 4))
(Command "Arc" E "@3.2016<309" F)
 (setq a3 (entlast))
(Command "Arc" F "@3.2016<51" K)
 (setq a4 (entlast))
(Command "Array" a3 a4 "" "R" "1" "5" "8")

;Hatch for insulation in Wall
(Setq G (Polar p15 (dtr 90) 2))
(Setq AS (/ ww 2))                    ;Half Wall Width Distance
(Setq H (Polar G (dtr 0) AS))         ;Start point of arc
(Setq I (Polar H (dtr 90) 4))         ;End of first arc, start of 2nd arc
(Setq L (Polar I (dtr 90) 4))         ;End of 2nd arc
(Command "Arc" H "@3.1875<39" I)
 (setq a5 (entlast))
(Command "Arc" I "@3.1875<141" L)
 (setq a6 (entlast))
(Command "Array" a5 a6 "" "R" "11" "1" "8")
;(Command "Layer" "S" cl "")
)
(Defun dtr (A) (* pi (/ A 180.0)))
(Defun rtd (A) (/ (* A 180.0 pi)))

(Princ "\nType WALLSECT to run")
(Princ)

