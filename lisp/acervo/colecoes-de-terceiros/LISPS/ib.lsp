;From CADENCE Magazine November , 1991
;Page 156

(defun c:ib (/ flag e1 te textval tx1 tx2 tx3 tx4)
 (setvar "cmdecho" 0)
 (setq flag (entlast))
 (setq e1(car(entsel "\n Select Radial Dimension to convert: ")))
 (command "undo" "m")
 (command "explode"  e1)
 (setq te(entnext flag))
 (while (setq te (entnext te))
  (if(= "TEXT" (cdr(assoc 0 (entget te))))
    (setq textval (cdr(assoc 1 (entget te)))))
 )
 (command "undo" "b")
 (setq tx1(substr textval 1 1))
 (setq tx2(substr textval 1 3))
 (setq tx3(substr textval 2))
 (setq tx4(substr textval 4))
   (if(= tx1 "R")(setq textval(strcat tx3 " R.")))
   (if(= tx2 "%%c")(setq textval(strcat tx4 " DIA.")))
 (command "dim" "newtext" textval e1 "")
 (command)
 (command)

)

