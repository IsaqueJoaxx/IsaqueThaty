; TIP992.LSP: NEWSETUP.LSP   Setup for R12   (c)1994, Watson Kilbourne

;;   Modified Release 10 routine (SETUP.LSP) for use with Release 12
;;   Does not require use of BORDER.DWG
;;
(defun c:NEWSETUP (/ CE OE A B D CX CY XL YL P1 P2 P3 P4)
   (setq CE (getvar "CMDECHO"))
   (defun NE (NE)
      (setvar "CMDECHO" CE)
      (princ "Function cancelled ")
      (princ)
   )
   (setq OE *error* *error* NE)
   (setvar "CMDECHO" 0)
   (menucmd "S=UNITS0")
   (initget (+ 1 2 4))
   (setq A (getint "\nSelect the Units from the screen menu: "))
   (menucmd (strcat "S=U" (itoa A)))
   (cond
     (
       (= A 5)
       (setq A 2 D 5)
     )
   )
   (command "setvar" "lunits" A)
   (initget (+ 1 4))
   (setq B (getreal "\nSelect the Scale from the screen menu: "))
   (cond
     (
       (= B 0)
       (progn
         (initget (+ 1 2 4))
         (setq B (getreal "\nEnter the scale: "))
         (setq B (float B))
       )
     )
   )
   (cond
     (
       (= D 5)
       (menucmd "S=METRIC")
     )
   (T
     (menucmd "S=ENGLISH")
   )
   )
   (initget (+ 1 4))
   (setq CX (getdist "\nSelect the Paper size from the screen menu: "))
   (initget (+ 1 4))
   (setq CY (getdist))
   (cond
     (
     (= CX 0)
     (progn
       (initget (+ 1 2 4))
       (setq CX (getdist "\nEnter the Horizontal Dimension of the paper: "))
       (initget (+ 1 2 4))
       (setq CY (getdist "\nEnter the Vertical Dimension of the paper: "))
     )
     )
   )
   (setq XL (* B CX)
         YL (* B CY)
         P1 '(0.0 0.0)
         P2 (polar P1 (* pi 2) XL)
         P3 (polar P2 (/ pi 2) YL)
         P4 (polar P3 pi XL)
   )
   (command
      ".LIMITS" "0,0" (list XL YL)
      ".PLINE" P1 "W" "0.0" "0.0" P2 P3 P4 "C"
      ".ZOOM" "A"
   )
   (menucmd "S=S")
   (setvar "CMDECHO" CE)
   (setq *error* OE OE nil)
   (princ)
)

