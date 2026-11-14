;THIS FILE WILL CHANGE A DIM'S TEXT WIDTH
(defun c:DMWTH ()
  (command)
  (prompt "\nSelect objects for the loop, ")
  (setq ssloop (ssget))
  (setq i 0)
  (while (setq obj (ssname ssloop (- (setq i (1+ i)) 1)))
    (setq inspt (cdr (assoc '10 (entget obj))))
    (command)
    (setq DS (getreal "\nDIM. SCALE?"))
    (command "STYLE" "" "" "" DS "" "" "" "" "DIM1" "UPDATE" OBJ "" "STYLE" "" "" "" ".85" "" "" "" "")
  )ÿ
  (prin1)
)
