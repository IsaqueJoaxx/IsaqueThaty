(defun pt2X-sort (x)  ;Sort a list of numbers
  (setq cmp pt2X-cmp)
  (sort x)
  )

(defun pt2X-cmp (a b) ; Compare numbers or strings in a list
  (cond 
    ((< (caar a) (caar b)) -1)
    ((> (caar a) (caar b)) 1)
    ((= (caar a) (caar b)) 0)
    )
)


