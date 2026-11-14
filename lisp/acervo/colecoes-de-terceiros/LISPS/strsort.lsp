(defun atomd-sort (x)  ;Sort a list of numbers
  (setq cmp atomd-cmp)
  (sort x)
  )

(defun atomd-cmp (a b) ; Compare numbers or strings in a list
  (cond 
    ((< a b) 1)
    ((> a b) -1)
    ((= a b) 0)
    )
)


