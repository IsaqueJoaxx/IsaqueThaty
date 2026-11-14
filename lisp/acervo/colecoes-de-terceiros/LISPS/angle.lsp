;THIS FILE WILL DRAW AN ANGLE OF ANY SIZE AND GIVE YOU AN OPTION
;OF FILLETING THE CORNERS
(defun c:ANGLE ()
  (command)
  (setq AH (getint "\nANGLE HEIGHT? "))
  (command)
  (setq AL (getint "\nANGLE LENGTH? "))
  (command)
  (setq ATH (getreal "\nANGLE THICKNESS? "))
  (command)
  (setq ER (getreal "\nEDGE RADIUS? "))
  (command)
  (setq A1 (getangle "\nTYPE:90 "))
  (command)
  (setq A2 (getangle "\nTYPE:180 "))
  (command)
  (setq A3 (getangle "\nTYPE:270 "))
  (command)
  (setq P1 (getpoint "\nPICK LOCATION OF ANGLE: "))
  (command)
  (setq P2 (polar P1 A1 AH))
  (command)
  (setq P3 (polar P2 A2 ATH))
  (command)
  (setq P5 (polar P1 A2 AL))
  (command)
  (setq P4 (polar P5 A1 ATH))
  (command)
  (setq IAH (- AH ATH))
  (command)
  (setq P6 (polar P3 A3 IAH))
  (command)
  (command "PLINE" P1 P2 P3 P6 P4 P5 "C")ÿ
  (command)
  (setq RT (getstring "\nENTER TO PICK CORNERS FOR FILLET? "))
  (command)
  (command "fillet" "r" ER)ÿ
  (command "fillet" PAUSE PAUSE)ÿ
  (command "fillet" PAUSE PAUSE)ÿ
  (command "fillet" PAUSE PAUSE)ÿ
  (prin1)
)
