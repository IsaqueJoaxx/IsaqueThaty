; Surface Texture Symbols per ANSI Y14.36-1978  SURFACE TEXTURE SYMBOL
(defun C:FINISH4 (/ Ins Sr Rs Ms Mw ts txthgt ds A B Emark Ss1
                    p1 p2 p3 p4 p5 p6 p7 p8 p9) 
(setvar "cmdecho" 0)
(setq ds (getvar "dimscale"))
(graphscr)
(setq Ins (getpoint "\nInsertion point: ")) (Rotat)
(setq Sr (getstring "\nSurface roughness: "))
(setq Rs (getstring "\nRoughness sampling length: "))
(setq Ms (strcase (getstring T "\nLay symbol/Maximum roughness spacing: ")))
(setq Mw (getstring "\nMaximum waviness height: "))

; text height settings
(progn
(setq ts (cdr (assoc 40 (tblsearch "Style" (getvar "Textstyle")))))
(if (= ts 0.0)
(setq txthgt (* (getvar "dimtxt") ds)))
(if (= txthgt nil) (setq txthgt (* (getvar "dimtxt") ds))))

; draw basic symbol
(setq p1 (polar Ins 2.094395102 (* txthgt 1.732050807)))
(setq p2 (polar Ins (/ pi 3.0) (* txthgt 3.464101615)))
(setq A (strlen Mw))
(if (/= Mw "")
    (setq B (- (* A txthgt) txthgt))
    (setq B (* txthgt 3.0))
    )
(setq p3 (polar p2 0.0 B))
(command "pline" p1 Ins p2 p3 "")
(setq Emark (entlast))

; add surface roughness text
(setq p4 (* txthgt 1.75))
(setq p5 (list (car Ins) (+ (cadr Ins) p4)))
(if (= ts 0.0) (command "Text" "R" p5 txthgt "0" Sr)
               (command "Text" "R" p5 "0" Sr))

; add roughness sampling text
(setq p6 (+ (/ txthgt 4) txthgt))
(setq p7 (list (car p2) (- (cadr p2) p6)))
(if (= ts 0.0) (command "Text" p7 txthgt "0" Rs)
               (command "Text" p7 "0" Rs))

; add maximun roughness spacing text
(setq p8 (* txthgt 3.0))
(setq p9 (list (car p2) (- (cadr p2) p8)))
(if (= ts 0.0) (command "Text" p9 txthgt "0" Ms)
               (command "Text" p9 "0" Ms))

; add maximun waviness text
(setq p10 (/ txthgt 4))
(setq p11 (list (car p2) (+ (cadr p2) p10)))
(if (= ts 0.0) (command "Text" p11 txthgt "0" Mw)
               (command "Text" p11 "0" Mw))
;
    (setq Ss1 nil) (gc)
    (setq Ss1 (ssadd Emark))
    (while Emark
    (setq Ss1 (ssadd Emark SS1)
    Emark (entnext Emark)))
(command "Rotate" Ss1 "" Ins Rot)
(princ))

