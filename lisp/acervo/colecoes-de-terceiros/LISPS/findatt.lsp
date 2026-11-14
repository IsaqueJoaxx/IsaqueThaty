; FINDATT loops thru .dwg, calls BLDPROP when it finds a block with attributes
;
(defun FINDATT ()
  (setq PROPLIST () )
  (setq E (entnext))
  (while E
    (setq enttyp (cdr (assoc 0 (entget E))))
    (setq BLKNM (cdr (assoc 2 (entget E))))
    (if
      (and
        (equal enttyp "INSERT")
        (equal (cdr (assoc 66 (entget E))) 1)
      )
      (bldprop E)
    )
    (setq E (entnext E))
  )
)
