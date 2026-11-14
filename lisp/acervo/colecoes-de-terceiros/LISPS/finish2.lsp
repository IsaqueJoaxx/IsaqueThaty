; Surface Texture Symbols per ANSI Y14.36-1978  MATERIAL REMOVAL REQ'D
(defun C:FINISH2 (/ ds ts txthgt Ins Sr Mr p1 p2 p3 p4 p5 p6 p7 Emark Ss1)
(setvar "cmdecho" 0)
(setq ds (getvar "dimscale"))
(graphscr)
(setq Ins (getpoint "\nInsertion point: ")) (rotat)
(setq Sr (getstring "\nSurface roughness: "))
(setq Mr (getstring "\nMaterial Removal: "))

; text height settings
(progn
(setq ts (cdr (assoc 40 (tblsearch "Style" (getvar "Textstyle")))))
(if (= ts 0.0)
(setq txthgt (* (getvar "dimtxt") ds)))
(if (= txthgt nil) (setq txthgt (* (getvar "dimtxt") ds))))

; draw basic symbol
(setq p1 (polar Ins 2.094395102 (* txthgt 1.732050807)))
(setq p2 (polar Ins (/ pi 3.0) (* txthgt 3.464101615)))
(setq p3 (polar p1 0.0 (* 1.732050807 txthgt)))
(command "pline" p2 Ins p1 p3 "")
(setq Emark (entlast))

; add surface roughness text
(setq p4 (* txthgt 1.75))
(setq p5 (list (car Ins) (+ (cadr Ins) p4)))
(if (= ts 0.0) (command "Text" "R" p5 txthgt "0" Sr)
               (command "Text" "R" p5 "0" Sr))

; add material removal text
;(setq p6 (* txthgt 1.5))
(setq p7 (list (- (car Ins) txthgt) (cadr Ins)))
(if (= ts 0.0) (command "Text" "R" p7 txthgt "0" Mr)
               (command "Text" "R" p7 "0" Mr))
;
    (setq Ss1 nil) (gc)
    (setq Ss1 (ssadd Emark))
    (while Emark
    (setq Ss1 (ssadd Emark SS1)
    Emark (entnext Emark)))
(command "Rotate" Ss1 "" Ins Rot)
(princ))

