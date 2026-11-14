; A SIMPLE BEEP ROUTINE THAT ALWAYS WORKS
; discovered by trial and error and accident
; by peter shreeve

(defun beep ()
  (setq f (open "con:" "w"))
  (write-char 7 f)
  (close f)
  (setq f nil)
)
