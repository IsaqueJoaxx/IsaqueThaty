; CHGNAME updates the .dwg with new employee names for the roomtags
;
(defun CHGNAME ()
  (setq INLIST FILELIST)
  (while INLIST
    (setq LOOKLIST (car INLIST))
    (setq INLIST (cdr INLIST))
    (setq ROOMNUM (car LOOKLIST))
    (setq EMPNM (last LOOKLIST))
    (cond
      ((assoc ROOMNUM PROPLIST)
        (setq CHGATTLIST (assoc ROOMNUM PROPLIST))
        (setq CE (last (cadr CHGATTLIST)))
        (while (not (equal (cdr (assoc 0 (entget CE))) "SEQEND"))
          (setq ATTAG (cdr (assoc 2 (entget CE))))
          (cond
            ((equal ATTAG "EMPNAME")
              (entmod (subst (cons '1 EMPNM)(assoc 1 (entget CE)) (entget CE)))
              (entupd CE)
            )
          )
          (setq CE (entnext CE))
        )
      )
    )
  )
)

