; TIP988A.LSP: BLL.LSP    Add a Bubble   (c)1994, Gary A. Otte

;;Add bubble to left of existing bubble
;;Pick existing bubble

(defun C:BLL ( / SCALE RADIUS DIA PE PM TXT)
 (setvar "CMDECHO" 0)
 (command "OSNAP" "CEN")
 (setq SCALE (getvar "DIMSCALE")
  RADIUS (* 0.3125 SCALE)
  DIA (* -0.625 SCALE)
  PE (getpoint "\nPick Circle: ")
  PM (polar PE 0.0 DIA))
 (command "circle" PM RADIUS)
 (setq TXT
    (getstring "\nEnter Part Number: "))
 (command "text" "M" PM
    (* SCALE (getvar "DIMTXT")) 0 TXT)
  (command "OSNAP" "NONE")
 (princ)
); end BLL

