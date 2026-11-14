;
;  FIXDIM requires the DIMF.shx font be placed in your current
;  AutoCAD path, preferably with the rest of your fonts.  It
;  assumes that you normally use a style called "DIM" for dimensions.
;  If you normally use a different style, change lines "A" and "B" to
;  reflect your style name.  It also requires the UPDATE command of
;  Release 10.  Assuming Release 11 has this command, FIXDIM ought to
;  work with it as well.  See the Read.me file for more information.
;
;  Walter Warren
;  (73750,3535)
  
(defun c:fixdim (/ sty ss)
 (setvar "cmdecho" 0)
 (setq sty (getvar "textstyle"))
 (cond
  ((= sty "DIMF")
   (prompt "Select dimensions w/o fractions:")
   (setq ss (ssget))
   (command "dim" "style" "dim")                ;Line A
  )
  ((/= sty "DIMF")
   (prompt "Select dimensions w/ fractions:")
   (setq ss (ssget))
   (command "dim" "style" "dimf")
  )
 )
 (command "update" ss "")
 (command "style" sty "exit")
 (setvar "cmdecho" 1)
 (prin1)
)

(defun fixdim (/ sty)
 (setvar "cmdecho" 0)
 (setq sty (getvar "textstyle"))
 (cond
  ((= sty "DIMF")
   (command "style" "dim")                      ;Line B
  )
  ((/= sty "DIMF")
   (command "style" "dimf")
  )
 )
 (command "update" (entlast) "")
 (command "style" sty)
 (setvar "cmdecho" 1)
 (prin1)
)
