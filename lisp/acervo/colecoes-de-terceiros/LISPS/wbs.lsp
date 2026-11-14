
(defun c:wbs (/ FP FN )
    (setq FP (getvar "dwgprefix"))
  (setq FN (getvar "dwgname"))
      (command "wblock" FN "y" "*" )
      (command "regen")
      (command "script" "wbs")
)