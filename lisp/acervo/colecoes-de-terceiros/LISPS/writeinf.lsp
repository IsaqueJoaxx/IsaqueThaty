(defun C:WRITEINFO ()
; filter all text entities that have "COSTLY_INVENTORY" 
; Xdata attached
  (setq xss (ssget "X" '((0 . "TEXT") (-3 ("COSTLY_INVENTORY")))))
; create ASCII file
  (setq f (open "XDATA.TXT" "w"))
; initialize counter and repeat through selection set
  (setq i 0)
  (repeat (sslength xss)
    (setq elist (entget (setq ename (ssname xss i)) '("COSTLY_INVENTORY")))
; prepare each record's fieldsÄroomno, blname, partno
    (setq roomno (cdr (assoc 1 elist))
          xlist (cadr (assoc -3 elist))
          ei 2)
    (while (< ei (length xlist))
      (setq blhand (cdr (nth (1- ei) xlist))
            blname (cdr (nth ei xlist))
            blptno (cdr (nth (1+ ei) xlist)))
      (write-line (strcat roomno "," blhand "," blname "," blptno) f)
      (setq ei (+ 3 ei))
    ); close while
    (setq i (1+ i))
  ); repeat
  (close f)
  (prin1)
)
