(defun C:ADDINFO ()
  (setq ename (car (entsel "\nSelect ROOM NO: ")))
  (setq xlist (list))
; make sure a text entity is selected
  (if (and ename 
           (= (cdr (assoc 0 (setq elist (entget ename))))
              "TEXT"))
    (progn
      (redraw ename 3) ; highlight the entity
      (setq addname (car (entsel "\nSelect FIXTURE: ")))
      (while addname
        (if (= (cdr (assoc 0 (setq addlist (entget
            addname)))) "INSERT")
          (progn
            (redraw addname 3)
            (setq schno (getstring "\nEnter SCHEDULE: "))
; establish strings for Xdata
            (setq hand (cdr (assoc 5 addlist))
                  name (cdr (assoc 2 addlist))
                  ptno (strcat (substr name 1 2) schno))
; establish dotted pairs
            (setq blhand (cons 1005 hand)
                  blname (cons 1000 name)
                  blptno (cons 1000 ptno))
; create Xdata list to add to entity
            (setq data (list blhand blname blptno))
            (setq xlist (append xlist data))
            (prompt (strcat "\n     FIXTURE: " name " SCHEDULE: " ptno))
            (redraw addname)
          ); progn for selection
          (prompt "\nFIXTURE not selected.")
        ); if for selecting fixture
        (setq addname (car (entsel "\nSelect FIXTURE / RETURN to exit: ")))
      ); while for selection of fixtures
; create Xdata list to use for entity modification
      (setq xlist (list (list -3 (cons "COSTLY_INVENTORY" xlist))))
      (setq elist (append elist xlist))
      (entmod elist)
    )
    (prompt "\nROOM NO. not selected.")
  ); if
  (prin1)
)
