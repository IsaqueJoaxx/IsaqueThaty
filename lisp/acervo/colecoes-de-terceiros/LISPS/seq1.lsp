;THIS FILE WILL INSERT NUMBERS IN SEQUENCE
(defun c:SEQ ()
  (command)
  (setq 1STNUM (getint "\nSTARTING NUMBER: "))
  (command)
  (setq TXTHT (getreal "\nENTER TEXT HEIGHT: "))
  (command)
  (setq i 0)
  (while 1
    (setq i (1+ i))
    (command)
    (setq TEMP (+ I 1STNUM))
    (command)
    (setq NUM (- TEMP 1))
    (command "TEXT" PAUSE TXTHT "" NUM)
  )ÿ
  (prin1)
)
