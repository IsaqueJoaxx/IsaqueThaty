; Surface Texture Symbols per ANSI Y14.36-1978  BASIC
(defun C:FINISH1 (/ P1 P2 P3 P4 Ins Sr ts txthgt ds Emark Ss1)
(setvar "cmdecho" 0)
(setq ds (getvar "dimscale"))
(graphscr)
(setq Ins (getpoint "\nInsertion point: ")) (rotat)
(setq Sr (getstring "\nSurface roughness: "))

; text height settings
(progn
(setq ts (cdr (assoc 40 (tblsearch "Style" (getvar "Textstyle")))))
(if (= ts 0.0)
(setq txthgt (* (getvar "dimtxt") ds)))
(if (= txthgt nil) (setq txthgt (* (getvar "dimtxt") ds))))

; draw basic symbol
(command "pline" (setq P1 (polar Ins 2.094395102 (* txthgt 1.732050807)))
             Ins (setq P2 (polar Ins (/ pi 3.0) (* txthgt 3.464101615))) "")
(setq Emark (entlast))

; add surface roughness text
(setq P3 (* txthgt 1.75))
(setq P4 (list (car Ins) (+ (cadr Ins) P3)))
(if (= ts 0.0) (command "Text" "R" P4 txthgt "0" Sr)
               (command "Text" "R" P4 "0" Sr))
;
    (setq Ss1 nil) (gc)
    (setq Ss1 (ssadd Emark))
    (while Emark
    (setq Ss1 (ssadd Emark SS1)
    Emark (entnext Emark)))
(command "Rotate" Ss1 "" Ins Rot)
(princ))

