;ARCL.LSP By Brian Carlson 7/19/88

(Defun C:ARCL (/ L S R D A C)
 (setvar "cmdecho" 0)
 (setq L (getreal "\nLength of arc: "))
 (setq S (getpoint "\nStart of arc: "))
 (setq R (getdist S "\nRadius: "))
 (setq D (getangle S "\nAngle to center: "))
 (setq A (/ (* (/ 180.0 pi) L) R))
 (setq C (polar S D R))
 (command "ARC" S "C" C "A" A)
 (setvar "cmdecho" 1)
 (princ)
)