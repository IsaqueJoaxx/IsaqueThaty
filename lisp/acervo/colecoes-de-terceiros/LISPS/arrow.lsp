; Function ARROW
; Author       : Russ Graham
;                302 E. Schreiber
;                Garland, Tx. 75040
;                (214) 495-5331
;
; Purpose      : Draw a line with a directed arrowhead, functioning basically
;                like the standard 'line' command.
; Requirements : Existance of block 'arrow', which is, relatively speaking,
;                a 45 degree arrowhead (line from (0,0) to (1,0.5) to (0,1))
;                and is scaled upon insertion.
; How to use   : Invoke just like an AutoCAD command
; Notes        : There exists a function (arrowsiz) which is used to set the
;                length and height of arrowheads. It is invoked as a command.
;                Arrowhead size is defaulted.
;
(defun c:arrow()
  (setvar "CMDECHO" 0)
  (if (null arrow_l) (setq arrow_l 0.25)) ; default length
  (if (null arrow_h) (setq arrow_h 0.25)) ; default height
  (setq p1 (getpoint "\nFrom point: ")
        p2 (getpoint "\nTo point: ")
  )
  (command "line" p1 p2 "")
  (command "insert" "arrow" p2 arrow_l arrow_h
    (atof (angtos (angle p1 p2) 0 4)))
  (while (not (null p2))
    (setq p1 p2 p2 (getpoint "\nTo point: "))
    (if (not (null p2))
      (progn
        (command "line" p1 p2 "")
        (command "insert" "arrow" p2 arrow_l arrow_h
          (atof (angtos (angle p1 p2) 0 4)))
      )
    )
  )
)
