(defun factr (x)
  (if (= x 1)
    (eval 1)
    (* x (factr (- x 1)))
    )
  )
