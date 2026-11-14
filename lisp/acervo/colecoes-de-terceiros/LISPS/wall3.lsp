; WALL3.LSP
; Draws wall segments (parallel lines) given user defined line offsets.
; For use with AutoCAD 2.6 and later.
; Included as a part of the WALLS utilities from Alacrity.
; Read WALLS.DOC for instructions.
; Copyright 1988 Alacrity

(defun c:WALL3 (/ offset draw fillet pt1 pt2 ang dist temp num
                  entlst1 entlst2 begin side)
    (defun offset ()
      (setq num 1
            temp nil)
      (while 
        (setq dist (getdist (strcat "\nLine segment #" (itoa num) " offset: ")))
        (setq temp (append (list dist) temp)
              num (1+ num))
      )
      (if temp (setq distlst (reverse temp)))
    )
    (defun draw ()
      (mapcar
        '(lambda (lst)
          (command "LINE" (polar pt1 ang lst) (polar pt2 ang lst) "")
          (entlast)
         )
         distlst
      )
    )
    (defun fillet ()
      (mapcar 
        '(lambda (ent1 ent2)
          (setq ss (ssadd)) 
          (foreach ent (list ent1 ent2) (ssadd ent ss))
          (command "FILLET" ss)
         )
        entlst1
        entlst2
      )
      (setq entlst1 entlst2)
    )
    (store)
    (setvar "FilletRad" 0)
    (setvar "BlipMode" 0)
    (if distlst
      (progn
       (initget "Yes No")
       (setq temp (getkword "\nRedefine line offsets Yes/<No>: "))
       (if (equal temp "Yes")
          (offset)
       )
      )
      (offset)
    )
    (if distlst
      (progn
       (setvar "CmdEcho" 0)
       (setvar "FilletRad" 0)
       (setvar "Blipmode" 0)
       (initget 1)
       (setq pt1 (getpoint "\nFrom point: "))
       (initget 1)
       (setq pt2 (getpoint pt1 "\nTo point: "))
       (grdraw pt1 pt2 -1)
       (initget 1)
       (setq ang (angle pt1 pt2)
             temp (getpoint "\nWhich side? ")
             side (- (angle pt1 temp) ang)
       )
       (grdraw pt1 pt2 -1)
       (if (< side 0.0) (setq side (+ side pi pi)))
       (if (< side pi) (setq side +) (setq side -))
       (setq ang (side ang >90)
             entlst1 (draw) 
             begin (append (list pt1) entlst1)
             pt1 pt2 num 1
       )
       (while 
         (and
           (not (initget "Close"))
           (setq pt2 (getpoint pt1 "\nTo point: "))
           (cond
               ((equal pt2 "Close")
                (if (> num 1)
                 (progn
                  (setq pt2 (car begin)
                        ang (side (angle pt1 pt2) >90)
                        entlst2 (draw)
                  )
                  (fillet)
                  (setq entlst2 (cdr begin))
                  (fillet)
                  nil
                 )
                 (princ "\nCannot close until two or more segments drawn.")
                )
               )
               (T
                (progn
                 (setq ang (side (angle pt1 pt2) >90)
                       entlst2 (draw)
                 )
                 (fillet)
                 (setq pt1 pt2 num (1+ num))
                )
               )
           )
         )
       )
      )
      (princ "\nNo line offsets defined.")
    )
  (restore)
)
 