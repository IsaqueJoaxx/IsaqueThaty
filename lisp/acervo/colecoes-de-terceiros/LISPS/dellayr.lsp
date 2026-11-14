
;       Delete all entities on specified layer

(defun c:dellayer ()
        (setq l (strcase (getstring "\nEnter layer to delete: ")))
        (setq e (entnext))
        (while e
           (if (= l (cdr (assoc 8 (entget e))))
              (entdel e)
           )
           (setq e (entnext e))
        )
)

