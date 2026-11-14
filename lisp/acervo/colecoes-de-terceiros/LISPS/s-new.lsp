(setq dwg (getstring "\nOpen File: "))
(if (/= "UNNAMED" (getvar "DWGNAME"))
 (command ".SAVE" "" ".NEW" dwg)
 (if (= (Getvar "dbmod") 0)
  (command ".new" dwg)
  (command ".new" "y" dwg)
 )
) 

