(setq dwg (getstring "\nOpen File: "))
(if (= (Getvar "dbmod") 0)
 (command "open" dwg)
 (command "open" "y" dwg)
)

