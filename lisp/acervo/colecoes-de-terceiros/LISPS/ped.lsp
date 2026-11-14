;THIS FILE WILL GIVE WIDTH TO A LINE
(defun c:PED ()
  (command)
  (setq W (getreal "\nNEW WIDTH: "))
  (command)
  (prompt "\nSelect objects for the loop, ")
  (setq ssloop (ssget))
  (setq i 0)
  (while (setq obj (ssname ssloop (- (setq i (1+ i)) 1)))
    (setq inspt (cdr (assoc '10 (entget obj))))
    (command "PEDIT" OBJ "" "W" W "")
  )ÿ
  (prin1)
)
