;;
;; Submitted: 2-26-94
;;   Command: DST.LSP
;;        By: Wayne Petts
;;
;; DST is a replacement for the AutoCAD DIST command. Pick two points
;; and the distance is printed on the screen menu. Pick two more points 
;; and that distance is printed on the screen menu below the first distance.
;; This will continue until the user presses return instead of picking
;; a new first point. Then the total of all of the previous distances
;; is displayed on the screen menu after the previous distances. These 
;; numbers will remain on the screen menu for referance until another 
;; menu is displayed. The numbers are also displayed in place of the 
;; coordinates until another point is picked. This is because when using
;; achitectural units the screen menu is not wide enough to see all of the
;; number. This is especially true if there are fractions.  
;; You may set a default Osnap mode by removing the ";" comment
;; colon from in front of the appropriate (setvar "OSMODE" ...) statement
;; below.
;;
(defun C:DST (/ PT1 DIST X TOTAL OLDVARS)
   (setvar "cmdecho" 0)
   (setq OLDVARS (list (getvar "OSMODE")(getvar "ORTHOMODE")(getvar "COORDS")))
   (setvar "coords" 0)
   (setq TOTAL 0.0)
   (setq PT1 "")
   (setq x 2)
   (repeat 25
     (grtext x "        ")
     (grtext x "        " 1)
     (setq x (+ 1 x))
   )
   (setq X 5)
   (command "ortho" "off")
;   (setvar "osmode" 1536) ;NEARest        /Remove the ";" from in front of
;   (setvar "osmode" 1152) ;PERPendicular | the osmode you want to have as a
;   (setvar "osmode" 1025) ;ENDPoint      | default when picking points to
;   (setvar "osmode" 1056) ;INTersection  | measure the distance between.
;   (setvar "osmode" 1026) ;MIDPoint     <| If you do not want a default
;   (setvar "osmode" 1086) ;INSertion     | osmode leave as is.
;   (setvar "osmode" 1028) ;CENter        | Remove only the ";" in front of
;   (setvar "osmode" 1040) ;QUADrant      | the (setvar...) command, not the
;   (setvar "osmode" 1032) ;NODE           \second one.
   (while (setq PT1 (getpoint "\nPick First Point >:"))
      (initget 1)
      (setq DIST (getdist PT1 "\nPick Second Point >:"))
      (setq TOTAL (+ DIST TOTAL))
      (grtext X (rtos DIST))(setq X (1+ X))
      (grtext (- X 1)(rtos DIST) 1)
      (grtext -2 (rtos DIST))
   )
   (grtext 3 (rtos TOTAL))
   (grtext 4 "--------")
   (grtext -2 (strcat "Total = " (rtos TOTAL)))
   (setvar "OSMODE" (nth 0 OLDVARS))
   (setvar "ORTHOMODE" (nth 1 OLDVARS))
   (setvar "COORDS" (nth 2 OLDVARS))
   (princ)
)
