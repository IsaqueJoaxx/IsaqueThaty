; DOOR.LSP
; Generates DOOR symbol in wall. 
; For use with AutoCAD 2.6 and later.
; This routine is a part of the Walls 1.3 utilities.
; Read WALLS.DOC for instructions.
; Copyright 1988 Alacrity

(defun c:DOOR (/ temp side ang p1 p2 p3 p4 p5)
    (store)
    (setvar "CmdEcho" 0)
    (if (not ang2) (setq ang2 >90))
    (if (not thick) (setq thick 1.5))
    (brkwall "Width of door" nil nil)
    (while 
      (progn
        (setq temp (getorient (strcat "\nAngle of door <" 
        (angtos ang2 (getvar "aunits") (getvar "auprec")) ">: ")))
        (cond
            ((and (> temp 0) (< temp pi)) (progn (setq ang2 temp) nil))
            ((not temp) nil)
            (T (princ "Angle must be between 0 and 180 degrees."))
        )
      )
    )
    (setq p5 (polar p1 (side ang >90 ang2) size))
    (setq ang (angle p1 p5))
    (setq thick (width thick "Thickness of door"))
    (if (zerop thick)
       (command "LINE" p5 p1 "")
       (progn
        (command "PLINE" p5 p1 (polar p1 (- ang (side >90)) thick) 
                               (polar p5 (- ang (side >90)) thick) "")
        (princ "\r                              ")
       )
    )
    (command "ARC" "C" p1 p3 "A" (side (* ang2 57.295780)))
    (restore)
)
