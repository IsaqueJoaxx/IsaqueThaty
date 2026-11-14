; OPENING.LSP
; Breaks open a wall 
; For use with AutoCAD 2.6 and later.
; Included with the Walls 1.3 utilities.
; Read WALLS.DOC for instructions.
; Copyright 1988 Alacrity

(defun c:OPENING (/ p1 p2 p3 p4 side ang)
    (store)
    (setvar "CmdEcho" 0)
    (brkwall "Width of opening" nil nil)
    (restore)
)
