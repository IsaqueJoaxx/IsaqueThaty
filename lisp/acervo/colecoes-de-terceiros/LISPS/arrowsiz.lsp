; Function ARROWSIZ
; Author       : Russ Graham
;                302 E. Schreiber
;                Garland, Tx. 75040
;                (214) 495-5331
;
; Purpose      : Set arrowhead Length and Height; for use in conjunction with
;                ARROW.LSP, a command to draw lines with directed arrows.
; How to use   : Invoke just like an AutoCAD command
;
(defun c:arrowsiz()
  ; Set defaults
  (if (null arrow_l) (setq arrow_l 0.25))
  (if (null arrow_h) (setq arrow_h 0.25))
  (setq l_dflt (strcat "<" (rtos arrow_l 2 4) ">:")
        h_dflt (strcat "<" (rtos arrow_h 2 4) ">:")
        o_arrow_l arrow_l o_arrow_h arrow_h
  )
  ; Prompt for values
  (setq arrow_l (getreal (strcat "\nEnter length of Arrowheads " l_dflt)))
  (setq arrow_h (getreal (strcat "\nEnter height of Arrowheads " h_dflt)))
  (if (null arrow_l) (setq arrow_l o_arrow_l))
  (if (null arrow_h) (setq arrow_h o_arrow_h))
)
