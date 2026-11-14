;Change Color
;
;
(Defun c:cc(/ a c d)
(setq oer   *error*  *error*  err2)
(graphscr)
(prompt "\nSelect object to be changed...")
(setq a(ssget))
(if a(progn
(initget 1 "R B C G M Y W L r b c g m y w l")
(setq c(getkword "\nselect.(R)ed.(B)lue.(C)yan.(G)reen.(M)agenta.(Y)ellow.(W)hite.by(L)ayer"))
(if(or(= c "R")(= c "r"))(setq d '"red"))
(if(or(= c "B")(= c "b"))(setq d '"blue"))
(if(or(= c "C")(= c "c"))(setq d '"cyan"))
(if(or(= c "G")(= c "g"))(setq d '"green"))
(if(or(= c "M")(= c "m"))(setq d '"magenta"))
(if(or(= c "Y")(= c "y"))(setq d '"yellow"))
(if(or(= c "W")(= c "w"))(setq d '"white"))
(if(or(= c "L")(= c "l"))(setq d '"bylayer"))
(command "change" a "" "prop" "c" d "")))
(princ))
(princ "\nType CC to change color.")
