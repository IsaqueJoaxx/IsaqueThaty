;THIS FILE WILL CHANGE ONE DIM'S STYLE TO ANOTHER
(defun c:DMSTYL ()
  (command "dim" "restore" "" PAUSE "update")
  (command (ssget) "" "e")ÿ
  (prin1)
)
