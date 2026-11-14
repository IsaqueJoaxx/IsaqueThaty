;From CADENCE Magazine November , 1991
;Page 155

(defun c:al ()
 (setq p1(getpoint"\nArrow location: "))
 (setq p2(getpoint"\nText location: "))
 (setq a1(angle p1 p2))
 (setq a2(+ a1 (* pi 0.5)))
 (setq a3(+ a1 (* pi 1.5)))
 (setq d1(/ (distance p1 p2) 5))
 (setq p3(polar (polar p1 a1 (* 4 d1)) a2 d1))
 (setq p4(polar (polar p1 a1 d1) a3 d1))
 (setq p5(polar (polar p1 a1 (* 4.5 d1)) a2 (* 0.25 d1)))
 (command "dim" "leader" p1 p3)
 (command)(command)
 (entdel(entlast))
 (command "pline" p1 p3 p4 p5 p2 "")
 (command "pedit" "L" "S" "X")
 (if(and(> (angle p5 p2)(* 0.5 pi))(< (angle p5 p2)(* 1.5 pi)))
   (command "Dtext" "J" "R"
       (polar p2 pi (*(getvar "dimtxt")(getvar"dimscale")))
       (*(getvar "dimtxt")(getvar"dimscale")) 0)
   (command "Dtext"
       (polar p2 0 (*(getvar "dimtxt")(getvar"dimscale")))
       (*(getvar "dimtxt")(getvar"dimscale")) 0)
)
)



