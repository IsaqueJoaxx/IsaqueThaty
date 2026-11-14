(defun fact (x) (cond ((= x 0) 1) (t (* x (fact (- x 1))))))
