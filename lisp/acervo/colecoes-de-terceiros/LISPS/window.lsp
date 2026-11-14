; WINDOW.LSP
; Generates a window symbol in a wall 
; For use with AutoCAD 2.6 and later.
; This routine is part of the Walls 1.3 utilities.
; Read WALLS.DOC for instructions.
; Copyright 1988 Alacrity

(defun c:WINDOW (/ p1 p2 p3 p4 side dist ang temp)
    (store)
    (setvar "CmdEcho" 0)
    (brkwall "Width of window" T nil)
    (setq dist (/ (distance p1 p2) 2))
    (command "LINE" (polar p1 ang dist) (polar p3 ang dist) "")
    (setq ledge (width ledge "Width of ledge"))
    (command "PLINE" p2 (polar p2 ang ledge) (polar p4 ang ledge) p4 "")
    (princ "\r                              ")
    (restore)
)
