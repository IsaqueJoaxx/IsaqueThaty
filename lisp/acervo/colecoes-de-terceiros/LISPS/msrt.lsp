(defun c:msrt ()
  (setq part (getstring"\nPart name  "))
  (setq pt1 (getpoint "\nInsertion point  "))
  (command "insert" part pt1 "" "" "")
  (command "copy" "l" "" "m" pt1)
)