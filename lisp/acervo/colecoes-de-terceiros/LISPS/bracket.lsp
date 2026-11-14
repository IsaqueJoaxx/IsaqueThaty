; Bracket.lsp            by R. Borzone           August 16, 1988
; Description: Draws bracket with fillets, chamfers, or straight lines
;   at user defined size, width, and rotation, on the current layer.
;--------------------------------------------------------------------------------------------------
(defun C:BRACKET (/ bm ab pt1 pt2 leng width choice lw)
 (setq ce (getvar "cmdecho"))                                  ; store current echo value
 (setvar "cmdecho" 0)                                          ; turn off command echo
 (initget 1)                                                   ; disallow null input
 (setq pt1 (getpoint "\nEnter starting point: "))              ; get starting point
 (initget 1)                                                   ; disallow null input
 (setq pt2 (getpoint "\nEnter ending point: "))                ; get ending point
 (command "pline" pt1 ^C)                                      ; to display current width
 (initget 4)                                                   ; disallow negatives
 (setq lw (getdist "\nEnter new width of line ? "))            ; get new pline width
 (if (null lw) (setq lw ""))                                   ; set width to default
 (initget "Chamfer Fillet Straight")                           ; acceptable parameters
 (setq choice (getkword "\n<Chamfer>/Fillet/Straight ? "))     ; get type of bracket
 (setq leng (/ (distance pt1 pt2) 2))                          ; get dist. to midpoint
 (cond ((= choice "Fillet")
  (setq width (getdist                                         ; get new fillet size
    (strcat "Fillet radius <" (rtos(getvar "filletrad")) "> ? ")))   
  (if (null width) (setq width (+ 0.01 (getvar "filletrad")))
                           (setq width (+ 0.01 width)))
            (setvar "filletrad" (- width 0.01))                ; set fillet variable
            (draw-bracket)                                     ; draw function
            (command "fillet" "p" "@")                         ; fillet bracket
            (command "fillet" "p" pt2))                        ; fillet bracket
       ((= choice "Straight")
            (initget 7)                                        ; disallow null,0,negatives
            (setq width (getdist "\nStraight distance ? "))    ; get straight distance
            (draw-bracket))                                    ; draw function
       (T   (setq width (getdist                               ; get new chmfer size 
             (strcat "Chamfer distance <" (rtos(getvar "chamfera")) "> ? ")))     
            (if (null width) (setq width (+ 0.01 (getvar "chamfera")))
                           (setq width (+ 0.01 width)))
            (setvar "chamfera" (- width 0.01))                 ; set chamfera variable
            (setvar "chamferb" (- width 0.01))                 ; set chamferb variable
            (draw-bracket)                                     ; draw function
            (command "chamfer" "p" "@")                        ; chamfer bracket
            (command "chamfer" "p" pt2)))                      ; chamfer bracket
 (setvar "blipmode" bm)                                        ; reset blipmode
 (setvar "cmdecho" ce)                                         ; reset command echo mode
 (princ))
(defun draw-bracket ()
  (setq bm (getvar "blipmode"))                                ; store current blipmode
  (setvar "blipmode" 0)                                        ; turn off blipmode
  (setq angle1 (angle pt2 pt1)                                 ; get bracket angle
        angle2 (+ 1.57079633 angle1))                          ; get perpendicular angle
  (command "pline" pt2 "w" lw lw (polar pt2 angle2 width)
       (polar (getvar "lastpoint") angle1 leng)                ; draw half of bracket
       (polar (getvar "lastpoint") angle2 width) \r)
  (setq angle2 (- angle2 pi))                                  ; change angle2 180
  (command "pline" "@" (polar (getvar "lastpoint")angle2 width)
       (polar (getvar "lastpoint") angle1 leng)                ; draw half of bracket
       (polar (getvar "lastpoint") angle2 width) \r)(prin1))(princ)

