; TIP853: DCHAMFER.LSP (c)1993, Andrew C. Clarey

;This command will dimension a "Chamfer", by a note if the chamfer angle
;is 45 degree's, or by a horizontal and vertical dimension if the angle is
;other than 45 degree's.

(defun C:DCHAMFER(/ CL CHAM DL PT1 PT2 ANG ANG1 DIST PT3 PT4 DIST1 T1 H1 V1)
   (setvar "CMDECHO" 0)
   (setq CL(getvar "CLAYER")
         CHAM(entget(car(entsel "\nSelect Chamfer to Dimension: ")))
         DL(getstring "\nEnter Layer for Chamfer Dimension: ")
         PT1(cdr(assoc 10 CHAM))
         PT2(cdr(assoc 11 CHAM))
         ANG(angle PT1 PT2)
         ANG1(angtos ANG 0 0)
         DIST(distance PT1 PT2))
   (cond 
     ((or(= ANG1 "45")(= ANG1 "135")(= ANG1 "225")(= ANG1 "315"))
         (setq PT3(polar PT1 ANG (/ DIST 2.0))
               PT4(getpoint "\nChoose Note Location: ")
               DIST1(rtos(* DIST(abs(cos ANG))) 2 2)
               T1(strcat "Chamfer" " 45"  "%%d" " * " DIST1))
         (command "layer" "s" DL "" "dim"  "leader" PT3 PT4 "" T1 "" "" nil)
     )
     ((or(and(> ANG1 "0")(< ANG1 "45"))
         (and(> ANG1 "45")(< ANG1 "135"))
         (and(> ANG1 "135")(< ANG1 "225"))
         (and(> ANG1 "225")(< ANG1 "315"))
         (and(> ANG1 "315")(< ANG1 "360")))    
         (setq H1(getpoint "\nHorizontal Dimension Line Location: ")
               V1(getpoint "\nVertical Dimension Line Location: "))
         (command "layer" "s" DL "" "dim" "horizontal" PT1 PT2 H1 "" "vertical" PT1 PT2 V1 "" nil)
     )         
   )
   (command "layer" "s" CL "")
)
