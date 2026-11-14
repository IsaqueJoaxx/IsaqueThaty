;Rob Johnson
;CadCon Consulting
;Greer,sc
;802-297-0115
;Place a horizontal construction line thru a point.
(DEFUN c:hcl   
  (/ a b c d pt1 pt2 pt3)
(DEFUN *error*   
  (st)
  (PROMPT      
     (STRCAT "ERROR: " st "\007\n")))
(SETVAR "CMDECHO" 0)
(SETQ pt1  
  (GETPOINT "\nEnter Construction Point: "))
(SETQ a  
  (GETVAR "VIEWSIZE"))
(SETQ b   
  (GETVAR "VIEWCTR"))
(SETQ c   
  (* a 1.339))
(SETQ d   
  (* c 0.5))
(SETVAR "BLIPMODE" 0)
(SETQ pt2   
  (LIST      
     (-         
        (CAR b) d)
     (CADR pt1)))
(SETQ pt3   
  (LIST      
     (+         
        (CAR pt2) c)      
     (CADR pt1)))
(COMMAND "LINE" pt2 pt3 "")
(SETVAR "BLIPMODE" 1)
(TERPRI)
(DISTANCE pt2 pt3)
(PRINC))

