; UNDSCR.LSP
; 3/12/1987
; Tony Tanzillo, A/E Automation Systems  (all rights reserved)
;
; UNDSCR.LSP is a user-defined AutoLISP command that will underscore
; the specified text entities in the drawing with ease.
; The functions in this file require most of the user-defined
; functions contained in file TEXTED.LSP located in the data libraries.
;
; -Tony Tanzillo, 70307,2556

(defun C:UNDERSCORE( / ent pr )
  (prompt "\nSelect text to underscore,")
  (ss-each nil 'ent
    '(cond
       ((setq pr (etype ent "text"))
          (entput ent 1 (strcat "%%u" (get 1 pr))))))
  (prin1))
