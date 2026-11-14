(defun facti (x / sum)
  (setq sum x)
  (repeat (1- x)
    (setq sum (* sum (setq x (1- x))))
    )
  )
