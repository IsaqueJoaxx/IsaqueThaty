;--------------------------------------------------------------------
; 4/27/86
; Written by Lansing Pugh, Architect
;
; This is a handy little LISP routine that will use the current
; text size and style to put text from a straight text file into
; a drawing.  Not as powerful as commercial programs with lots of
; features like column alignment, etc.
; This function should be appended to the ACAD.LSP file.
; You are welcome to use this code in any way you see fit.  Just
; don`t blame me if it doesn't work.
;
;--------------------------------------------------------------------

(defun C:DRAWTEXT ()
   (drawgetname)
   (drawfirstline)
   (while (not (null text)) (drawreadthrough))
   (close fname)
)

(defun drawgetname ()
  (print "")
  (setq filename (getstring "Name the text file you want to draw: "))
  (setq fname (open filename "r"))
)

(defun drawfirstline ()
  (setq text (read-line fname))
  (setq point (getpoint "Enter text start: "))
  (command "text" point "" "" text)
)

(defun drawreadthrough ()
  (setq point (polar point (* 1.5 pi) (* 1.7143 (getvar "TEXTSIZE"))))
  (setq text (read-line fname))
  (command "TEXT" POINT "" "" text)
)
