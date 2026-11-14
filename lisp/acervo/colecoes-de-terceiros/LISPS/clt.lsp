;THIS FILE WILL CHANGE THE LINETYPE OF SELECTED OBJECTS
(defun c:CLT ()
  (command)
  (setq LT (cdr (assoc '6 (entget (car (entsel "\nExtract info from what object: "))))))
  (command)
  (prompt "\nSelect objects for the loop, ")
  (setq ssloop (ssget))
  (setq i 0)
  (while (setq obj (ssname ssloop (- (setq i (1+ i)) 1)))
    (setq inspt (cdr (assoc '10 (entget obj))))
    (command "CHANGE" OBJ "" "P" "LT" LT "")
  )ÿ
  (prin1)
)
