; WALL2.LSP
; Routine to draw wall segments (parallel lines) of a given width.
; For use with AutoCAD 2.6 and later.
; Included as a part of the WALLS utilities from Alacrity.
; Read WALLS.DOC for instructions.
; Copyright 1988 Alacrity

(defun c:WALL2 (/ pt1 pt2 ss1 ss2 ang num draw) 
   (defun draw ()
     (setq ang (angle pt1 pt2))
     (command "LINE" (polar pt1 (- ang >90) (/ dist 2)) 
                     (polar pt2 (- ang >90) (/ dist 2)) "")
     (setq ss1 (ssadd (entlast) ss1))
     (command "LINE" (polar pt1 (+ ang >90) (/ dist 2)) 
                     (polar pt2 (+ ang >90) (/ dist 2)) "")
     (setq ss2 (ssadd (entlast) ss2))
     (command "FILLET" ss1)
     (command "FILLET" ss2)
     (ssdel (ssname ss1 0) ss1)
     (ssdel (ssname ss2 0) ss2)
   )
   (store)
   (setvar "FilletRad" 0)
   (setvar "BlipMode" 0)
   (setq dist (width dist "Width"))
   (initget 1)
   (setq pt1 (getpoint "\nFrom point: "))
   (initget 1)
   (setq pt2 (getpoint pt1 "\nTo point: "))
   (setq ang (angle pt1 pt2))
   (command "LINE" (polar pt1 (- ang >90) (/ dist 2))
                   (polar pt2 (- ang >90) (/ dist 2)) "")
   (setq temp (entlast)
         ss1 (ssadd temp)
         begin (list pt1 temp)
   )
   (command "LINE" (polar pt1 (+ ang >90) (/ dist 2))
                   (polar pt2 (+ ang >90) (/ dist 2)) "")
   (setq temp (entlast)
         ss2 (ssadd temp)
         begin (append begin (list temp))
         pt1 pt2 num 1)
   (while 
    (and
      (not (initget "Width Close"))
      (setq pt2 (getpoint pt1 "To point: "))
      (cond 
           ((equal pt2 "Width")
            (progn
             (setq dist (width dist "Width"))
             T
            )
           )
           ((equal pt2 "Close")
            (if (> num 1)
              (progn
               (setq pt2 (nth 0 begin))
               (draw)
               (ssadd (nth 1 begin) ss1)
               (ssadd (nth 2 begin) ss2)
               (command "FILLET" ss1)
               (command "FILLET" ss2)
              )
              (princ "\nCannot close until two or more segments drawn.")
            )
           )
           (T
            (progn
             (draw)
             (setq pt1 pt2 num (1+ num))
            )
           )
      )
    )
   )
   (restore)
)