; START.LSP
; Routines needed to run the various WALLS utilities.
; For use with AutoCAD 2.6 and later.
; Included with the Walls 1.3 utilities.
; Read WALLS.DOC for instructions.
; Copyright 1988 Alacrity

(princ "\nLoading...")

; 90 degrees in radians - used by all routines
(setq >90 (/ pi 2))

; Where to find ACAD.WAL - used by WALLTYPE
(setq file "\\ALACRITY\\ACAD")

; Stores enviroment variables - used by all routines
(defun store ()
    (setvar "CmdEcho" 0)
    (setq osm (getvar "OsMode"))
    (setq ortho (getvar "OrthoMode"))
    (setq apert (getvar "Aperture"))
    (setq pick (getvar "Pickbox"))
    (setq blip (getvar "Blipmode"))
)

; Store enviroment
(store)

; Restores enviroment variables - used by all routines
(defun restore ()
    (setvar "Highlight" 1)
    (setvar "OsMode" osm)
    (setvar "OrthoMode" ortho)
    (setvar "Aperture" apert)
    (setvar "Pickbox" pick)
    (setvar "Blipmode" blip)
    (setvar "CmdEcho" 1)
    (princ)
)

; Standard error routine - used by all routines when error occurs
(defun *error* (msg)
    (restore)
    (princ "error: ")
    (princ msg)
    (terpri)
    (restore)
)

; Get width - used by all routines
(defun width (dist msg / temp)
   (while
     (progn
      (initget 4)
      (cond
         ((setq temp (getdist (strcat "\n" msg (if dist (strcat " <"
           (rtos dist (getvar "lunits") (getvar "luprec")) ">") "") ": ")))
           (progn (setq dist temp) nil)
         )
         ((and (not temp) dist) nil)
         (T (princ "\nInvalid ..."))
      )
     )
   )
   dist
)

; Turn on virtual memory
(vmon)

; Generates break in wall - used by OPENING, DOOR, WINDOW and RELITE
(defun brkwall (msg a b / dist)
   (setvar "BlipMode" 1)
   (setvar "OsMode" 0)
   (while (not (setq p1 (osnap (cadr (entsel "\nPoint on wall: ")) "near")))
      (princ "\nInvalid ...")
   )
   (initget 6)
   (if (setq dist (getdist "\nDistance from endpoint: "))
      (setq p2 (osnap p1 "midp") 
            p1 (osnap p1 "endp")
            p1 (polar p1 (angle p1 p2) dist))
   )
   (setvar "OsMode" 128)
   (initget 1)
   (setq p2 (getpoint p1 "\nTouch opposite side of wall: "))
   (setvar "BlipMode" 0)
   (setvar "OsMode" 0)
   (setq size (width size msg))
   (initget 1)
   (setq side (getpoint "\nWhich side? "))
   (setq side (- (angle p1 side) (setq ang (angle p1 p2))))
   (if (< side 0) (setq side (+ side pi pi)))
   (if (< side pi) (setq side +) (setq side -))
   (setq p3 (polar p1 (side ang >90) size))
   (setq p4 (polar p2 (side ang >90) size))
   (if a (command "BREAK" p1 p1 "BREAK" p3 p3) (command "BREAK" p1 p3))
   (if b (command "BREAK" p2 p2 "BREAK" p4 p4) (command "BREAK" p2 p4))
   (command "LINE" p1 p2 "")
   (command "LINE" p3 p4 "")
)

(princ)