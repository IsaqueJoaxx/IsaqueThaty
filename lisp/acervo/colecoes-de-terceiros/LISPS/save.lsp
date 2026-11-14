
;       C:SAVE is the main SAVE Date/Time STAMP.
;       Created by Denis P. Beck, Sr. - 08-11-91 09:06am

(defun dxf (code elist)
  (cdr (assoc code elist))
)

(if SAVEINFO nil(setq SAVEINFO "Y"))

(defun C:SAVE (/ : cdate date hour da ti in en ed bn found count emax ss1 old new)
  (graphscr)
  (setvar "cmdecho" 0)
     (write-line "\nDateSave - SAVE Date/Time/Initial STAMP")
     (write-line "(C) 1990, 1991 DPBeck\n")

;       GET SYSTEM DATE

  (setq da (getvar "cdate"))
  (setq da (rtos da 2 0))
  (setq date (strcat
     (itoa(atoi(substr da 5 2))) "/"
     (itoa(atoi(substr da 7 2))) "/"
     (substr da 3 2)))

;       GET SYSTEM TIME

  (setq ti (getvar "cdate"))
  (setq ti (rtos ti 2 6))
  (setq hr1 (substr ti 10 2))
  (if (>= (atoi hr1) 12)
     (setq hour (itoa (- (atoi hr1) 12)) daynite "pm")
     (setq hour (itoa (atoi hr1)) daynite "am"))
  (setq minute (substr ti 12 2))
  (setq time (strcat hour ":" minute daynite))

;       GET PATH AND DWG NAME

  (setq path    (getvar "dwgprefix"))
  (setq dwg     (getvar "dwgname"))
  (setq pathdwg (strcat path dwg))
  (setq rel     (getvar "acadver"))

;       FIND ATTRIBUTE DATE

  (setq ss1 (ssget "X" '((0 . "INSERT")
                         (2 . "DATE")))
  );SAVE setq ssgetx
  (if ss1
     (progn
        (setq count 0
           emax (sslength ss1)
        )
        (while (< count emax)
           (setq en (ssname ss1 count)
                 ed (entget en)
                 bn (dxf 2 ed)
           )
           (if (= "DATE" (substr bn 1 4))
              (setq count emax found T)
              (setq count (1+ count))
           )
        )
        (if found
           (progn
              (setq en (entnext en) ed (entget en))
              (setq old (assoc 1 ed))
              (setq in (getstring "\nUsers INITIALS: "))
              (if (= SAVEINFO "Y")
                (setq new (cons 1 (strcat "Acad "rel" "pathdwg" "date" "time" "in)))
                (setq new (cons 1 (strcat date" "time" "in)))
              )
              (setq ed (subst new old ed))
              (grtext -2 (strcat "Saved at " time))
              (entmod ed)
              (command ".SAVE")
              (prin1)
           )
        )
     )
     (progn
        (setq scl (getvar "dimscale"))
        (setq pt(getpoint "\nInsertion point: "))
        (command "insert" "DATE" pt scl scl "0" "Time Stamp")
        (c:SAVE)
        (command ".SAVE")
        (prin1)
     )
  )
(setvar "cmdecho" 1)
)
