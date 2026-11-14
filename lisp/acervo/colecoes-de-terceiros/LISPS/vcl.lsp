;Rob Johnson
;CadCon Consulting
;Greer,sc
;802-297-0115
;Place a vertical construction line thru a point.
(DEFUN c:vcl
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
  (* a 0.48))
(SETQ d   
  (* a 0.96))
(SETVAR "BLIPMODE" 0)
(SETQ pt2
  (LIST      
     (CAR pt1)      
     (-         
        (CADR b) c)))
(SETQ pt3   
  (LIST     
     (CAR pt1)      
     (+ d         
        (CADR pt2))))
(COMMAND "LINE" pt2 pt3 "")
(TERPRI)
(SETVAR "BLIPMODE" 1)
(DISTANCE pt2 pt3)
(PRINC))

