; CarlB     19 March 90
;
;--- kanji1 ------------------------------------------------
; print a bigfont character pre+char
; at <pt>, text angle 0-degrees 
; needs a defined text style
;
(defun kanji1 (pt pre char)
(command "text" pt 0 
   (strcat (chr pre) (chr char))
);command
)
;
;--- kanji2 ------------------------------------------------
; generate a series of charcaters
; from <start> to <end> for escape char <pre>
;
(defun kanji2 (pre start end pt ds)
(while (<= start end)
   (kanji1 pt pre start)               ; put text
   (setq pt (polar pt 0 ds)            ; right ds
      start (1+ start)                 ; next char
   )
);while
)
;
;--- kanji -------------------------------------------------
; write rows bigfont characters 
; for excape characters from <start> to <end>
; make text item, left to right, bottom to top
; Ex: (kanji 136 138 '(0 0) 1.0)
;
(defun kanji (start end pt ds)
(setvar "cmdecho" 0)
(while (<= start end)                  ; escape characters
   (kanji2 start 64 255 pt ds)         ; make a row
   (setq pt (polar pt (/ pi 2.0) ds)   ; up <ds>
      start (1+ start)                 ; next escape char
   )
);while
)
;
;
;--- c:kanji -----------------------------------------------
; make a string of kanji characters left to right
; use 6 for a space   ->
; use 4 for backspace <-
;
(defun c:kanji (/ a b c txt)
(setvar "cmdecho" 0)
(redraw (setq txt (car (entsel "\nSelect text to edit: "))) 3)
(initget "Replace Append")
(if (= "Append"
        (getkword "\nAppend/<Replace>: ")
    )
    (setq c (list (cdr (assoc 1 (entget txt)))))
);if
(command "undo" "group")
(while 
   (setq a (getint "\nRow: "))             ; while input               
   (cond 
   ((< 135 a 153 )
      (initget 1)
      (setq b (getint "Col: ")
            c (cons (strcat (chr a)(chr b)) c)
      );setq
   );not space
   ((= a 6) (setq c (cons " " c)))         ; add a space 
   ((= a 4) (setq c (cdr c)))              ; delete last character
   (T (setq a nil))
   );cond
   (if a 
      (entmod 
         (subst (cons 1 (apply 'strcat (reverse c)))
                (assoc 1 (entget txt))
                (entget txt)
         );subst
      );entmod
      (prompt " invalid")
   );if
);while
(command "undo" "end")
(princ)
);c:kanji

