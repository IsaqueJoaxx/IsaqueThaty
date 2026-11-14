
(defun c:explodit ()
  (setq a (entsel))
  (setq e1 (entget (car a)))
  (setq part (cdr (assoc 2 e1)))
  (setq ipt (cdr (assoc 10 e1)))
  (command "erase" ipt "")
  (command "insert" part ipt "" "" "")
  (command "explode" ipt)
)



