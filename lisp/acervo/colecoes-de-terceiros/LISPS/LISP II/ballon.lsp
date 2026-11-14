;; (c)1987                R&J Computer Service
;                            RR #3  Box 183
;                          Albion, IN  46701
;  Phone:                Voice  (219) 636-2460
;                         Data  (219) 636-3153
;                   24hrs 2400, 1200, 300 Baud 8-N-1
;          Balloon.LSP adds Detail Number "Balloons" to drawings
;                        Written by John Kitt
; We are NOT responsable for the performance or accuracy of this LISP routine
;  You are encouraged to copy and distribute this LISP routine
;  provided this header section IS NOT REMOVED.  For continued
;  support and new LISP routines you are asked to mail a Registration
;  fee of $10.00 to the above address.  Thank You!!!
(defun *ERROR* (st) (princ (strcat "*" st)) ' *)
(defun C:BALLOON ()
(setq ECHO (getvar "cmdecho") 
      MODE (getvar "osmode") 
      ORTHO (getvar "orthomode"))
(setvar "orthomode" 0)
(setvar "osmode" 0)
(setvar  "cmdecho" 0)
(command "GRAPHSCR")
(setq RADV 57.29578
      RAD1 (/ 90.0 RADV)
      RAD2 (/ 180.0 RADV)
      RAD3 (/ 270.0 RADV)
      RAD4 0
      PT1 (getpoint "\nEnter arrow start point: ")
      CENPT (getpoint "\nEnter balloon center location: ")
      RAD (angle PT1 CENPT) 
      ARROW (getvar "dimasz") 
      BALRAD 0.1563
      PT2 (polar CENPT RAD (- 0 BALRAD)))
(command "LINE" PT1 PT2 "")
(setq END1 (+ RAD 0.2000)
      END2 (- RAD 0.2000)
      PT3 (polar PT1 END1 ARROW)
      PT4 (polar PT1 END2 ARROW))
(command "LINE" PT1 PT3 PT4 "C")
(command "SOLID" PT1 PT3 PT4 PT4 "")
(setq CENTPT (polar PT2 RAD BALRAD))
(command "CIRCLE" CENTPT BALRAD)
(setq VAR 0)
(while (= VAR 0)
(setq TMPNR (getreal "\nEnter the number of divisions 1 to 4 [1]: "))
(if (= TMPNR nil) (setq TXTDIV 1.0) (setq TXTDIV TMPNR))
(setq VAR TXTDIV)
(if (/= TXTDIV 1.0) (progn
(if (/= TXTDIV 2.0) (progn
(if (/= TXTDIV 3.0) (progn
(if (/= TXTDIV 4.0) (progn (setq VAR 0)
(princ "Invalid Entry....Out of Range"))))))))))
(if (= TXTDIV 1.0) (progn
(setq TXTHT 0.125
      TXTLOC (list (car CENTPT) (- (cadr CENTPT) (/ TXTHT 2.0)))
      VAR1 0)
(while (= VAR1 0)
(setq BALTXT (getstring "\nEnter balloon text (2 char. max.): "))
(if (> (strlen BALTXT) 2) (progn (setq VAR1 0)
(princ "Invalid Entry....Too Many Charactors"))
(setq VAR1 1)))
(command "TEXT" "C" TXTLOC TXTHT 0 BALTXT)))
(if (= TXTDIV 2.0) (progn
(setq TXTHT1 0.2
      TXTHT2 0.18
      TXTLOC1 (list (car CENTPT) (+ (cadr CENTPT) 0.04))
      TXTLOC2 (list (car CENTPT) (- (cadr CENTPT) (+ TXTHT2 0.04)))
      PT5 (polar CENTPT RAD2 BALRAD)
      PT6 (polar CENTPT RAD4 BALRAD))
(command "LINE" PT5 PT6 "")
(setq VAR2 0)
(while (= VAR2 0)
(setq UPTXT (getstring "\nEnter upper text (2 char. max.): "))
(if (> (strlen UPTXT) 2) (progn (setq VAR2 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR2 1)
(command "TEXT" "C" TXTLOC1 TXTHT1 0 UPTXT))))
(setq VAR2 0)
(while (= VAR2 0)
(setq LOWTXT (getstring "\nEnter lower text (2 char. max.): "))
(if (> (strlen LOWTXT) 2) (progn (setq VAR2 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR2 1)
(command "TEXT" "C" TXTLOC2 TXTHT2 0 LOWTXT))))))
(if (= TXTDIV 3.0) (progn
(setq TXTHT1 0.2
      TXTHT2 0.18
      TXTLOC1 (list (car CENTPT) (+ (cadr CENTPT) 0.04))
      TXTLOC2 (list (- (car CENTPT) TXTHT2) (- (cadr CENTPT) (+ TXTHT2 0.04)))
      TXTLOC3 (list (+ (car CENTPT) 0.050) (- (cadr CENTPT) (+ TXTHT2 0.04)))
      PT5 (polar CENTPT RAD2 BALRAD)
      PT6 (polar CENTPT RAD4 BALRAD)
      PT7 (polar CENTPT RAD3 BALRAD))
(command "LINE" PT5 PT6 "")
(command "LINE" CENTPT PT7 "")
(setq VAR3 0)
(while (= VAR3 0)
(setq UPTXT (getstring "\nEnter upper text (2 char. max.): "))
(if (> (strlen UPTXT) 2) (progn (setq VAR3 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR3 1)
(command "TEXT" "C" TXTLOC1 TXTHT1 0 UPTXT))))
(setq VAR3 0)
(while (= VAR3 0)
(setq LLTXT (getstring "\nEnter lower left text (1 char. max.): "))
(if (> (strlen LLTXT) 1) (progn (setq VAR3 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR3 1)
(command "TEXT" TXTLOC2 TXTHT2 0 LLTXT))))
(setq VAR3 0)
(while (= VAR3 0)
(setq LRTXT (getstring "\nEnter lower right text (1 char. max.): "))
(if (> (strlen LRTXT) 1) (progn (setq VAR3 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR3 1)
(command "TEXT" TXTLOC3 TXTHT2 0 LRTXT))))))
(if (= TXTDIV 4.0) (progn
(setq TXTHT1 0.19
      TXTHT2 0.17
      TXTLOC1 (list (- (car CENTPT) TXTHT1) (+ (cadr CENTPT) 0.045))
      TXTLOC2 (list (+ (car CENTPT) 0.05) (+ (cadr CENTPT) 0.045))
      TXTLOC3 (list (- (car CENTPT) TXTHT2) (- (cadr CENTPT) (+ TXTHT2 0.045)))
      TXTLOC4 (list (+ (car CENTPT) 0.055) (- (cadr CENTPT) (+ TXTHT2 0.045)))
      PT5 (polar CENTPT RAD2 BALRAD)
      PT6 (polar CENTPT RAD4 BALRAD)
      PT7 (polar CENTPT RAD1 BALRAD)
      PT8 (polar CENTPT RAD3 BALRAD))
(command "LINE" PT5 PT6 "")
(command "LINE" PT7 PT8 "")
(setq VAR4 0)
(while (= VAR4 0)
(setq ULTXT (getstring "\nEnter upper left text (1 char. max.): "))
(if (> (strlen ULTXT) 1) (progn (setq VAR4 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR4 1)
(command "TEXT" TXTLOC1 TXTHT1 0 ULTXT))))
(setq VAR4 0)
(while (= VAR4 0)
(setq URTXT (getstring "\nEnter upper right text (1 char. max.): "))
(if (> (strlen URTXT) 1) (progn (setq VAR4 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR4 1)
(command "TEXT" TXTLOC2 TXTHT1 0 URTXT))))
(setq VAR4 0)
(while (= VAR4 0)
(setq LLTXT (getstring "\nEnter lower left text (1 char. max.): "))
(if (> (strlen LLTXT) 1) (progn (setq VAR4 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR4 1)
(command "TEXT" TXTLOC3 TXTHT2 0 LLTXT))))
(setq VAR4 0)
(while (= VAR4 0)
(setq LRTXT (getstring "\nEnter lower right text (1 char. max.): "))
(if (> (strlen LRTXT) 1) (progn (setq VAR4 0)
(princ "Invalid Entry....Too Many Charactors")) (progn
(setq VAR4 1)
(command "TEXT" TXTLOC4 TXTHT2 0 LRTXT))))))
(setvar "osmode" MODE)
(setvar "cmdecho" ECHO)
(setvar "orthomode" ORTHO)
(command)
)
)
      RAD3 (/ 270.0 RADV)
      RAD4 0
      PT1 (getpoint "\nEnter arrow start poi