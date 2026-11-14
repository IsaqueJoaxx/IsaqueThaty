; TIP995.LSP: PG.LSP   Grid Points    (c)1994, Simon Whhite

;; PG - replace the grid with an equivalent pattern of point entities.
 ; Copyright 1993 by Rocket Software
 ; The statement "It takes all kinds" is not completely true - there are 
 ; undeniably a variety of types of people, but their place in the great
 ; scheme of things is more difficult to assess.  The same is true of software.

 (defun c:PG (/ LIMAX MAXX MAXY LIMIN MINX MINY GRUNIT GRIDX GRIDY NUMX NUMY)
  (setvar "cmdecho" 0)
  (setq LIMAX (getvar "limmax"))
  (setq MAXX (car LIMAX))
  (setq MAXY (cadr LIMAX))
  (setq LIMIN (getvar "limmin"))
  (setq MINX (car LIMIN))
  (setq MINY (cadr LIMIN))
  (setq GRUNIT (getvar "gridunit"))
  (if (equal GRUNIT (list 0.0 0.0))
      (setq GRUNIT (getvar "snapunit")))
  (setq GRIDX (car GRUNIT))
  (setq GRIDY (cadr GRUNIT))
  (setq NUMX (1+ (fix (/ (- MAXX MINX) GRIDX))))
  (setq NUMY (1+ (fix (/ (- MAXY MINY) GRIDY))))
  (command "point" LIMIN)
  (command "array" (entlast) "" "r" NUMY NUMX "0,0" GRUNIT)
  (if (= (getvar "gridmode") 1) (command "grid" "off"))
 (princ))

