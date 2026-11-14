;    This routine will rotate the crosshairs to match the angle of a selected
; line with the option of designating an offset from this line.
;
;                             Mark Vodhanel    2-6-87
;                             CIS 72456,463
;
(defun c:picang (/ snapmode-sv unts entnam type pt1 pt2 ang)
     (setq snapmode-sv (getvar "SNAPMODE"))
     (setvar "SNAPMODE" 0)
     (setq unts (getvar "AUNITS"))
     (setq entnam (entsel "\nSelect a line:"))
     (if (null entnam) (princ "\nNothing found") (progn ; Nothing  - exit
         (setq entlst (entget (car entnam)))
         (setq type (cdr (assoc 0 entlst)))
         (if (/= type "LINE") (princ "\nNot a line") (progn ; No line - exit
             (setq pt1 (cdr (assoc 10 entlst)))
             (setq pt2 (cdr (assoc 11 entlst)))
             (setq ang (angle pt1 pt2))
             (setvar "cmdecho" 0)
             (command "snap" "r" "" (angtos ang unts 6))
             (setvar "SNAPMODE" 0)
             (setq ans (getangle "\nOffset? <0>: "))
             (if (null ans) () (progn                ; No offset - exit
                 (setq ang (+ ans ang))
                 (setq angstr (angtos ang 0 6))
                 (command "snap" "r" "" (angtos ang unts 6))))))))
     (setvar "SNAPMODE" snapmode-sv)
     (princ)
)
;
;
;

     (if (= ap-text " ") () (progn
         (setq distrng (strcat  distrng