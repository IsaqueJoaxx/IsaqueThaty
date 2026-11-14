;THIS FILE WILL PERFORM SSX FIRST THEN CHANGE THE ITEMS
(defun c:CLSS ()
  (command)
  (SSX)
  (command)
  (setq LA (cdr (assoc '8 (entget (car (entsel "\nExtract info from what object: "))))))
  (command "CHANGE" "P" "" "P" "LA" LA "")ÿ
  (prin1)
)
