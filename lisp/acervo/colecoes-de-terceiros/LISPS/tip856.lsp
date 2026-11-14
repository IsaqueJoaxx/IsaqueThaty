; TIP856: WALLTEE.LSP (c)1993, Jason E. Baker

(defun nearestpt(entlist selectpt / temppt nearpt)
 (setq ang (angle (cdr (assoc 10 entlist))(cdr (assoc 11 entlist))))
 (setq temppt (polar selectpt (+ ang 1.5707963) 1))
 (setq nearpt (inters temppt selectpt (cdr (assoc 10 entlist))(cdr (assoc 11 entlist)) nil))
)

(defun c:wtee(/ top tee1 tee2 tee1selpt tee2selpt tee2sp
               tee2sp topsp topep int1 int2 dt1spsel dt2spsel dt1spint dt2spint
               selint1 selint2 topsint topeint flg cla)
 (setvar "cmdecho" 0)
 (setvar "highlight" 0)
 (setq cla (getvar"clayer"))
 (setq topsel (nearestpt (setq top (entget (car (setq tmp (entsel "\nSelect wall to be teed to:  ")))))(cadr tmp))
       tee1sel (nearestpt (setq tee1 (entget (car (setq tmp (entsel "\nSelect first wall line:  ")))))(cadr tmp))
       tee2sel (nearestpt (setq tee2 (entget (car (setq tmp (entsel "\nSelect second wall line:  ")))))(cadr tmp))
       tee1sp (cdr (assoc 10 tee1))
       tee1ep (cdr (assoc 11 tee1))
       tee2sp (cdr (assoc 10 tee2))
       tee2ep (cdr (assoc 11 tee2))
       topsp (cdr (assoc 10 top))
       topep (cdr (assoc 11 top))
       int1 (inters tee1sp tee1ep topep topsp nil)
       int2 (inters tee2sp tee2ep topep topsp nil)
       dt1spsel (distance tee1sp tee1sel)
       dt2spsel (distance tee2sp tee2sel)
       dt1epsel (distance tee1ep tee1sel)
       dt1spint (distance tee1sp int1)
       dt2spint (distance tee2sp int2)
       selint1 (distance tee1sel int1)
       selint2 (distance tee2sel int2)
       topsint (distance topsp int1)
       topeint (distance topsp int2)
 )                                 
 (command "erase" (cdar top)(cdar tee1)(cdar tee2) ""
          "layer" "s" (cdr (assoc 8 top)) "")
 (if (if (< topsint topeint)
         (command "line" topsp int1 "")
         (progn
           (command "line" topsp int2 "")
           (setq flg T)
         )
     );if
     (command "line" topep int1 "")
     (command "line" topep int2 "")
 );if

 (if (= (+ dt1spsel selint1) dt1spint)
     (command "line" int1 tee1sp "")
     (command "line" int1 tee1ep "")
 )
 (if (= (+ dt2spsel selint2) dt2spint)
     (command "line" int2 tee2sp "")
     (command "line" int2 tee2ep "")
 )
 (command "layer" "s" cla "")
 (setvar "cmdecho" 1)
 (setvar "highlight" 1)
)




