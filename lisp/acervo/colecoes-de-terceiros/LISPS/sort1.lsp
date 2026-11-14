(defun atom-sort (x)  ;Sort a list of numbers
  (setq cmp atom-cmp)
  (sort x)
  )

(defun assoc1-sort (x)  ;For association list, 1st element sort
  (setq cmp assoc1-cmp)
  (sort x)
  )

(defun assoc2-sort (x)  ;For association list, 2nd element sort
  (setq cmp assoc2-cmp)
  (sort x)
  )

(defun assoc12-sort (x)
  (setq cmp assoc12-cmp)
  (sort x)
  )

(defun sort (x)
  (cond ((null (cdr x)) x)
        (T (merge (sort (first-half x))
                  (sort (last-half x))))))

(defun merge (a b)
  (cond ((null a) b)
        ((null b) a)
        ((< (cmp (car a) (car b)) 0)
           (cons (car a) (merge (cdr a) b)))
        (t (cons (car b) (merge a (cdr b))))))

(defun first-half (l)
  (head l (1- (length l))))

(defun head (l n)
  (cond ((minusp n) nil)
        (t (cons (car l) (head (cdr l) (- n 2))))))

(defun last-half (l)
  (tail l (1- (length l))))

(defun tail (l n)
  (cond ((minusp n) l)
        (t (tail (cdr l) (- n 2)))))

;  Compare two lists.  Return 0 if they are equal, -1 if the
;  first string is less than the second in ASCII collating sequence,
;  and 1 if the second string is less than the first.

(defun atom-cmp (a b) ; Compare numbers or strings in a list
  (cond 
    ((< a b) -1)
    ((> a b) 1)
    ((= a b) 0)
    )
)

; Compare the first number (X) in sublist of an association list
(defun assoc1-cmp (a b) 
  (cond ((< (car a) (car b)) -1)
        ((> (car a) (car b))  1)
        ((= (car a) (car b)) 0)
        )
  )

; Compare the second number (Y) in sublist of an association list
(defun assoc2-cmp (a b)
  (cond ((< (cadr a) (cadr b)) -1)
        ((> (cadr a) (cadr b))  1)
        ((= (cadr a) (cadr b)) 0)
        )
  )

; Compare the first number (X) in sublist of an association list.  If the 
; first numbers are equal compare the second number (Y).
(defun assoc12-cmp (a b / result)
  (if (zerop (setq result (assoc1-cmp a b)))
    (assoc2-cmp a b)
    (eval result)
    )
  )

