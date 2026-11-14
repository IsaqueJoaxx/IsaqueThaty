; STAMP.LSP
; Places current date/time into STAMP block.
; Meant to be for keeping track of dwg plots.
; Please read STAMP.DOC.  For use with AutoCAD Rel 9 and later.
; Copyright 1988 Alacrity
; This is Public Domain software, please use freely.

(defun C:STAMP (/ cdate ss edata ename)
  (setq cdate (rtos (getvar "cdate") 2 6)
        cdate 
    (strcat
       (cdr (assoc (substr cdate 5 2) 
          '(("01" . "Jan")
            ("02" . "Feb")
            ("03" . "Mar")
            ("04" . "Apr")
            ("05" . "May")
            ("06" . "Jun")
            ("07" . "Jul")
            ("08" . "Aug")
            ("09" . "Sept")
            ("10" . "Oct")
            ("11" . "Nov")
            ("12" . "Dec"))))
       " "
       (substr cdate 7 2)
       ", "
       (substr cdate 1 4)
       " - "
       (substr cdate 10 2)
       ":"
       (substr cdate 12 2)
       ":"
       (substr cdate 14 2)
     )
  )
  (if 
    (setq ss (ssget "X" '((0 . "INSERT") (2 . "STAMP"))))
    (progn
      (setq ename (entnext (ssname ss 0))
            edata (entget ename)
            edata (subst (cons 1 cdate) (assoc 1 edata) edata)
      )
      (entmod edata)
      (entupd ename)
    )
    (progn
      (princ "\nSTAMP block not found - Now inserting.\n")
      (command "INSERT" "STAMP" "Scale" (getvar "LtScale") pause pause cdate)
    )
  )
  (princ)
)

; End Of File