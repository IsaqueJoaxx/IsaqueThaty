
;HIDELP.LSP
;Program to remove hidden lines from an ellipse. 
;
(defun makelp ()
     (setq cpoint (getpoint "PICK POINT IN A CLEAR AREA:  "))
     (command "circle" cpoint 0.5)
     (command "block" "PENNelp" cpoint "l" "")
     (command "arc" (setq start (polar cpoint (/ pi 2) 0.5)) "c" 
	cpoint "a" "180")
     (command "block" "hlfPNelp" cpoint "l" "")
)



(defun C:SGLELP ()
     (setq varaperture (getvar "aperture"))
     (setvar "aperture" 5)
     (setq c1 (getpoint "PICK CENTER OF ELLIPSE: "))
     (setvar "osmode" 512)
     (setq a1 (getpoint "PICK ALIGNING POINT ON ISO AXIS:  "))
     (setq a (atan (/ 1 (sqrt 2))))
     (setq ang (angle c1 a1))
     (setq z (getreal "RAD:  "))
     (command "insert" "PENNelp" c1 (setq f (* z (sin a))) z (* 
	ang (/ 180 pi)))
     (setvar "osmode" 0)
     (setvar "aperture" varaperture)
)


(defun C:HALFELP ()
     (setq varaperture (getvar "aperture"))
     (setvar "aperture" 5)
     (setq c1 (getpoint "PICK CENTER OF HALF ELLIPSE:  "))
     (setvar "osmode" 512)
     (setq a1 (getpoint "PICK ALIGNING POINT ON ISO AXIS:  "))
     (setq a (atan (/ 1 (sqrt 2))))
     (setq ang (angle c1 a1))
     (setq z (getreal "RAD:  "))
     (command "insert" "hlfPNelp" c1 (setq f (* z (sin a))) z (* 
	ang (/ 180 pi)))
     (setvar "osmode" 0)
     (setvar "aperture" varaperture)
)

(defun C:NEWDWG ()
     (setq dname (getvar "dwgname"))
     (setq varfile (strcat dname ".var"))
     (setq ofile (open varfile "w"))
     (write-line "0" ofile)
     (close ofile)
     (makelp)
)


(defun C:HIDNELPS ()
     (setq dname (getvar "dwgname"))
     (setq varfile (strcat dname ".var"))
     (setq ofile (open varfile "r"))
     (setq PENNcntr (+ 1 (atoi (read-line ofile))))
     (close ofile)
     (setq ofile (open varfile "w"))
     (write-line (rtos PENNcntr 2 0) ofile)
     (close ofile)
     (SETQ VARAPERTURE (GETVAR "APERTURE"))
     (SETVAR "APERTURE" 5)
     (SETQ A (ATAN (/ 1 (SQRT 2))))
     (SETVAR "OSMODE" 16)
     (SETQ Q1 (GETPOINT "PICK ELLIPSE TO HIDE:  "))
     (SETQ Q2 (GETPOINT "PICK OTHER ELLIPSE:  "))
     (SETVAR "OSMODE" 0)
     (SETVAR "APERTURE" 0)
     (SETQ C1 (OSNAP Q1 "CEN")) (SETQ C2 (OSNAP Q2 "CEN"))
     (SETQ ANG (ANGLE C1 C2)) (SETQ ANG (* ANG (/ 180 PI)))
     (SETQ D (DISTANCE C1 C2))
     (SETQ R1 (DISTANCE Q1 C1))
     (SETQ R2 (DISTANCE Q2 C2))
     (SETQ E (/ D (SIN A))) (SETQ S (/ (+ R1 R2 E) 2))
     (SETQ B (* 2 (ATAN (SQRT (/ (* (- S E) (- S R1)) (* S (- S 
	R2)))))))
     (COMMAND "ZOOM" "C" Q1 0.00000001)
     (COMMAND "ERASE" Q1 "")
     (COMMAND "ZOOM" "P")
     (COMMAND "ARC" "C" C1 (POLAR C1 (- 0 B) R1) (POLAR C1 B R1))
     (COMMAND "BLOCK" (STRCAT "PENNELP" (RTOS PENNcntr 2 0)) C1 
      "L" "")
     (COMMAND "INSERT" (STRCAT "PENNELP" (RTOS PENNcntr 2 0)) C1 
(SIN A) "1" ANG)
     (COMMAND "REDRAW")
     (SETVAR "APERTURE" VARAPERTURE)
)


center
near
quad
endpoint
.PA