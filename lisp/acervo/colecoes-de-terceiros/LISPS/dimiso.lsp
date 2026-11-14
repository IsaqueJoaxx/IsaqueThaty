;CADalyst  Vol.4 #5  September 1987
;Isometric Dimensioning   page 38
;by James Brittain
;*Note that two arrowhead blocks and text styles must be created
;for DIMISO.LSP 

 (Defun C:DimIso ()
  (Setvar "CmdEcho" 0)
  (SetQ A1 (/ Pi 180.0))
  (SetQ A2 (/ 180 Pi))
  (SetQ A (StrCase (SubStr (GetString "\n<L>eft or <R>ight: ") 1 1)))
  (SetQ P1 (GetPoint "\n1st ext'n line origin or RETURN to select: "))
  (Cond
   ((= nil P1)
   (SetQ P2 (Osnap (GetPoint "\nSelect line: ") "MID"))
  (SetQ P1 (Osnap P2 "END"))
  (SetQ P2 (Polar P1 (Angle P1 P2) (* 2.0 (Distance P1 P2))))
  )
  ((BoundP 'P1)
  (SetQ P2 (GetPoint "\nSecond extension line origin: "))
  )
 )
 (SetQ P3 (GetPoint "\nDimension line location: "))
 (Cond
  ((= A "L")
  (SetQ B 150 C 30 G "ISOAROW2" H "ISOTEXT2" I 30)
  (SetPt)
  (If
   (< (Cadr P6) (Cadr P7))
   (SetQ F -1)
   (SetQ F 1)
  )
  (DimIt)
  )
  ((= A "R")
  (SetQ B 30 C 150 G "ISOAROW1" H "ISOTEXT1" I 330)
  (SetPt)
  (If
   (< (Cadr P6) (Cadr P7))
   (SetQ F -1)
   (SetQ F 1)
  )
  (DimIt)
  )
 )
 )

 (DeFun SetPt ()
 (SetQ P4 (Inters P1 (Polar P1 (* A1 B) 1)
   P3 (Polar P3 (* A1 C) 1) Onseg))
 (SetQ P5 (Inters P2 (Polar P2 (* A1 B) 1)
   P3 (Polar P3 (* A1 C) 1) Onseg))
 (SetQ P6 (Polar P4 (Angle P4 P5) (/ (Distance P4 P5) 2)))
 (SetQ P7 (Polar P1 (Angle P1 P2) (/ (Distance P1 P2) 2)))
 )
 (DeFun DimIt ()
  (Command "Line" (Polar P1 (Angle P1 P4) (/ E 2))
    (Polar P4 (Angle P1 P4) (/ E 2)) ""
    "Line" (Polar P2 (Angle P2 P5) (/ E 2))
    (Polar P5 (Angle P2 P5) (/ E 2)) "" "Line" P4 P5 "")
 (SetQ P6 (Polar P6 (Angle P1 P4) (* F E)))
 (SetQ D1 (RtoS (Distance P4 P5) (GetVar "Lunits") (GetVar "Luprec")))
 (Command "Insert" G P4 E "" (* A2 (Angle P4 P5)))
 (Command "Insert" G P5 E "" (* A2 (Angle P5 P4)))
 (Prompt "\nDimension text <")
 (Prompt D1)
 (SetQ D2 (GetString ">: "))
 (If (= D2 "") (Setq D D1) (Setq D D2))
 (Command "Text" "S" H "C" P6 E I D)
 )

 (SetQ E (GetReal "\nText height for ISO DIMS: "))
