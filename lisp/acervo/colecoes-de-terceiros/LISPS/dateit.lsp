(defun c:dateit (/ x)
(setq x (getvar "cdate"))
(setq x (rtos x 2 0))
(setq date (strcat
(substr x 5 2)"/" ;month
(substr x 7 2)"/" ;day
(substr x 3 2)))  ;year
)
