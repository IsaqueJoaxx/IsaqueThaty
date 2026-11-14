;THIS FILE ALLOWS YOU TO PICK MANY LINES TO EDIT
(defun c:TED ()
  (command)
  (prompt "\nSelect objects for the loop, ")
  (setq ssloop (ssget))
  (setq i 0)
  (while (setq obj (ssname ssloop (- (setq i (1+ i)) 1)))
    (setq inspt (cdr (assoc '10 (entget obj))))
    (command "DDEDIT" OBJ "")
  )ÿ
  (prin1)
)
