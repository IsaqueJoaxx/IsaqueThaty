; Surface Texture Symbols per ANSI Y14.36-1978  NO MATERIAL REMOVAL
(defun C:FINISH3 (/ ds ts txthgt Ins Rm p1 p2 p3 p4 p5 Emark Ss1)
(setvar "cmdecho" 0)
(setq ds (getvar "dimscale"))
(graphscr)
(setq Ins (getpoint "\nInsertion point: ")) (Rotat)
(setq Rm (getstring "\nRemoval of material prohibitation: "))

; text height settings
(progn
(setq ts (cdr (assoc 40 (tblsearch "Style" (getvar "Textstyle")))))
(if (= ts 0.0)
(setq txthgt (* (getvar "dimtxt") ds)))
(if (= txthgt nil) (setq txthgt (* (getvar "dimtxt") ds))))

; draw basic symbol
(setq p1 (polar Ins 2.094395102 (* txthgt 1.732050807)))
(setq p2 (polar Ins (/ pi 3.0) (* txthgt 3.464101615)))
(command "pline" p1 Ins p2 "")
(setq Emark (entlast))

; add the circle
(setq p3 (polar Ins (/ pi 2.0) txthgt))
(command "circle" p3 (* 0.5 txthgt))

; add material removal text
(setq p4 (* txthgt 1.75))
(setq p5 (list (car Ins) (+ (cadr Ins) p4)))
(if (= ts 0.0) (command "Text" "R" p5 txthgt "0" Rm)
               (command "Text" "R" p5 "0" Rm))
;
    (setq Ss1 nil) (gc)
    (setq Ss1 (ssadd Emark))
    (while Emark
    (setq Ss1 (ssadd Emark SS1)
    Emark (entnext Emark)))
(command "Rotate" Ss1 "" Ins Rot)
(princ))

