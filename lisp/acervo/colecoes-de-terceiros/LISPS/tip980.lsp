; TIP980.LSP: CAM.LSP   Cams or Slotted Holes   (c) 1994, Robert Trombley

(defun C:CAM ()
 (setq C1 (getpoint "\nPick center of first radius: ")
       R1 (getdist C1 "\nEnter first radius: ")
       C2 (getpoint C1 "\nPick center of second radius ")
       R2 (getdist C2 "\nEnter second radius ")
       A (angle C1 C2) D (distance C1 C2) P (- R1 R2)
       L (sqrt (- (* D D) (* P P))) A1 (atan L P)
       PT1 (polar C1 (- A A1) R1) 
       PT2 (polar PT1 (+ (/ pi 2.0) (- A A1)) L)
       PT3 (polar C1 (+ A A1) R1) 
       PT4 (polar PT3 (- (+ A A1) (/ pi 2.0)) L))
 (command "ARC" "C" C1 PT3 PT1
          "ARC" "C" C2 PT2 PT4)
 (command "LINE" PT1 PT2 ""
          "LINE" PT3 PT4 "")
 (princ)
)
