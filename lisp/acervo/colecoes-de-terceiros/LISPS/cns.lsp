(setq sym1(ssget))
(setq insrtpt(getpoint "\nPick new insert point "))
(setq nname(getstring "\nEnter name of new symbol: "))
(setq nname1(strcat"c:/kesym1/" nname))
(command"wblock" nname1 "y" "" insrtpt sym1 "")
