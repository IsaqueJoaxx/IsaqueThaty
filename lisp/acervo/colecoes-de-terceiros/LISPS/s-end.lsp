(setq dwg (getstring "\nOpen File: "))
(if (/= "UNNAMED" (getvar "DWGNAME"))
 (command ".SAVE" "" ".open" dwg)
 (if (= (Getvar "dbmod") 0)
  (command "open" dwg)
  (command "open" "y" dwg)
 )
) 

