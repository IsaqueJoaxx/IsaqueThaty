;****************************************************************
;Program Name: C:2LINE
;
;Description:
;This program is used to create double lines (i.e. Walls).
;The C:2LINE function defined below is the main program and it
;calls the APEXINT function below which is used to intersect
;two lines at their apex.  When the 2LINE program is activated,
;the user is prompted to enter the distance between the two lines
;and then is prompted to enter the from and to points for the
;double line.  If the last point entered is the same as the first
;point entered, the user is prompted if the he/she wants the
;lines closed.  If the user responds YES the corners of the
;starting and ending points are cleaned up and closed.
;
;Author: Craig Allen (415) 842-8350
;****************************************************************
;*** Intersects two lines at their apex. (Called from 2LINE function below) 
(DEFUN APEXINT (#A #B)
 (SETQ PT3 (INTERS
  (CDR (ASSOC 10 (ENTGET #A))) (CDR (ASSOC 11 (ENTGET #A)))
  (CDR (ASSOC 10 (ENTGET #B))) (CDR (ASSOC 11 (ENTGET #B))) nil))
 (COMMAND ".CHANGE" #A "" PT3 ".CHANGE" #B "" PT3) (PRINC))

;*** Creates double line with user specified distance between lines.
(DEFUN C:2LINE ()
   (SETVAR "CMDECHO" 0)
   (SETQ CNT 0)
   (IF (NOT D1) (SETQ D1 1.0))
;*** Get the distance desired between the lines.
   (SETQ D1A (GETDIST (STRCAT "\nEnter DISTANCE between lines <" (RTOS D1) ">: ")))
   (IF (NUMBERP D1A) (SETQ D1 D1A))
   (SETQ D2 (/ D1 2))
;Get the first from point and to point.
   (SETQ PT1 (GETPOINT "\nFrom point: ")
         PT2 (GETPOINT PT1 "\n  To point: ")
         STPT PT1
   )
   (SETVAR "HIGHLIGHT" 0)
   (WHILE (/= PT2 nil) (SETVAR "BLIPMODE" 0) (SETQ A1 (ANGLE PT1 PT2))
      (COMMAND ".LINE" (POLAR PT1 (+ A1 1.5707963) D2) (POLAR PT2 (+ A1 1.5707963) D2) "")
      (IF (= CNT 0)
          (SETQ L1A (ENTLAST) L1F L1A)
          (PROGN
            (SETQ L1B L1A L1A (ENTLAST))
            (APEXINT L1A L1B)
          ) ;End of progn for if (= cnt 0)
      ) ;End of if (= cnt 0)
      (COMMAND ".LINE" (POLAR PT1 (- A1 1.5707963) D2) (POLAR PT2 (- A1 1.5707963) D2) "")
      (IF (= CNT 0)
          (SETQ L2A (ENTLAST) L2F L2A)
          (PROGN
             (SETQ L2B L2A L2A (ENTLAST))
             (APEXINT L2A L2B)
          ) ;End of progn for if (= cnt 0)
      ) ;End of if (= cnt 0)
      (SETVAR "BLIPMODE" 1)
      (SETQ PT1 PT2 PT2 (GETPOINT PT1 "\n  To point: ") CNT (1+ CNT))
   ) ;End of while (/= pt2 nil...)
   (INITGET "Yes No")
   (IF (EQUAL STPT PT1)
       (PROGN
          (SETQ YN (GETKWORD "\nClose lines? (<Yes>/No): "))
          (IF (NOT YN) (SETQ YN "YES") (SETQ YN (STRCASE YN)))
          (IF (= YN "YES")
              (PROGN
                 (APEXINT L1A L1F)
                 (APEXINT L2A L2F)
              ) ;End of progn.
          );End of if.
       )
   )
   (SETVAR "HIGHLIGHT" 1)
   (PRINC)
)
