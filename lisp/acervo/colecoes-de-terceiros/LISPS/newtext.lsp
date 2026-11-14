;
; This function will allow you to space multi lines of
; text at a fixed line spaceing that is independant of 
; the text height. It is provided as an example of text processing
;
(DEFUN C:NEWTEXT ( / A B C D E)
   (SETVAR "BLIPMODE" 0)
   (SETQ A (GETPOINT "Enter STARTING POINT: ")
         B (GETDIST A "Enter TEXT HEIGHT  ")
         C (GETDIST A "Enter TEXT SPACING  ")
         D (GETSTRING "\nRight, Left or Center:Enter R,L,C  ")
         E "T")
   (WHILE E
      (SETQ E (GETSTRING T "\nEnter ! To Exit, TEXT:  "))
      (IF (= (ASCII E) 33) (SETQ E NILL) 
          (PROGN 
             (IF (or (= (ASCII D) 76) (= (ASCII D) 108)) 
                (COMMAND "TEXT" A B "0" E)
                (COMMAND "TEXT" D A B "0" E))
             (SETQ A (POLAR A (/ (* 3 PI) 2) C)))))
   (SETVAR "BLIPMODE" 0)
)

