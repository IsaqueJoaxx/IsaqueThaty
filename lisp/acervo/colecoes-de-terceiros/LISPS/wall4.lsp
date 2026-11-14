; WALL4.LSP
; Routine to draw wall segments (parallel lines) loaded with WALLTYPE.LSP
; For use with AutoCAD 2.6 and later.
; Included as part of the WALLS utilities from Alacrity
; Read WALLS.DOC for instructions.
; Copyright 1988 Alacrity

(defun c:WALL4 (/ draw fillet temp ang pt1 pt2 side
                  ent entlst1 entlst2 ss begin
                  ccolor cltype clayer color ltype layer) 
 (store)
 (setvar "FilletRad" 0)
 (setvar "BlipMode" 0)
 (if wallst
   (progn
    (setq clayer (getvar "CLayer")
          ccolor (getvar "CEColor")
          cltype (getvar "CELType")
          temp 1
          len (strlen ccolor)
    )
    (while 
        (and
         (< temp len)
         (not (equal (substr ccolor temp 1) " "))
        )
        (setq temp (1+ temp))
    ) 
    (setq ccolor (substr ccolor 1 (1- temp)))
    (defun draw ()
      (mapcar
        '(lambda (lst)
          (setq layer (car lst)
                ltype (cadr lst)
                color (caddr lst)
                dist  (last lst)
          )
          (if layer (command "LAYER" "S" layer ""))
          (if ltype (command "LINETYPE" "S" ltype ""))
          (if color (command "COLOR" color))
          (command "LINE" (polar pt1 ang dist) (polar pt2 ang dist) "")
          (if layer (command "LAYER" "S" clayer ""))
          (if ltype (command "LINETYPE" "S" cltype ""))
          (if color (command "COLOR" ccolor))
          (entlast)
         )
         wallst
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

    (setvar "CmdEcho" 0)
    (setvar "FilletRad" 0)
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
   (princ "\nNo current walltype defined.")
 )
 (restore)
)
