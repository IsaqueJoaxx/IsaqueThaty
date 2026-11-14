;TIP895:  GET.LSP    Get Properties   (C)1993, David Adie

(DEFUN C:GC (/ SS E N)
   (PRINC "\n select entities for color change:")
   (SETQ SS (SSGET))
   (IF SS (PROGN
         (SETQ E (CAR(ENTSEL "\n select new color <not bylayer>: ")))
         (IF E (PROGN
               (SETQ E (ENTGET E))
               (SETQ N (CDR (ASSOC 62 E)))
               (COMMAND "CHANGE" SS "" "PROP" "C" N "")
         ))
   ))
)
;
(defun C:GT (/ SS E N1 N2 N3)  
   (princ "\n select text to alter: ")
   (SETQ ss (ssget))
   (IF ss (PROGN  
         (SETQ E (car (entsel "\n pick text with desired qualities: ")))
         (IF E (PROGN   
               (SETQ E (ENTGET E)) 
               (SETQ N1 (CDR (ASSOC 7 E)))  
               (SETQ N2 (CDR (ASSOC 40 E)))
         (SETQ N3 (CDR (ASSOC 8 E)))))  
         (PROGN
            (COMMAND "change" SS "" "" "" N1 N2 "" "" "")
         (COMMAND "CHANGE" SS "" "P" "LA" N3 ""))
)))
;
(DEFUN C:GL (/ SS E N)
   (PRINC "\n select entities for linetype change:")
   (SETQ SS (SSGET))
   (IF SS (PROGN
         (SETQ E (CAR(ENTSEL "\n select new linetype <not bylayer>:")))
         (IF E (PROGN
               (SETQ E (ENTGET E))
               (SETQ N (CDR (ASSOC 6 E)))
               (COMMAND "CHANGE" SS "" "PROP" "LT" N "")
         ))
   ))
)
;
(DEFUN C:GSTR (/ SS E N)
   (PRINC "\n select text string to change:")
   (SETQ SS (SSGET))
   (IF SS (PROGN
         (SETQ E (CAR(ENTSEL "\n select string with desired content: ")))
         (IF E (PROGN
               (SETQ E (ENTGET E)) 
               (SETQ N (CDR (ASSOC 1 E)))
               (COMMAND "CHANGE" SS "" "" "" "" "" "" N)
         ))
   ))
)
;
(DEFUN C:LL (/ E N)
   (SETQ E(CAR (ENTSEL "\n pick layer to lock: ")))
   (if e nil (princ "\n try again homer:"))
   (if e
   (princ "\n layer locked thrasher:"))
   (IF E (PROGN
         (SETQ E (ENTGET E))
         (SETQ N (CDR (ASSOC 8 E)))
(COMMAND "LAYER" "lock" N ""))))
;
(DEFUN C:LU (/ E N)
   (SETQ E(CAR (ENTSEL "\n pick layer to unlock: ")))
   (if e nil (princ "\n try again homer:"))
   (if e
   (princ "\n layer unlocked thrasher:"))
   (IF E (PROGN
         (SETQ E (ENTGET E))
         (SETQ N (CDR (ASSOC 8 E)))
(COMMAND "LAYER" "unlock" N ""))))

