;PCOM.LSP
(defun C:PCOM ()
 (setq e (car(entsel "Pick your command. "));Select object for command.
       e (entget e)                         ;Get association list for object.
       com (cdr(assoc 0 e)))                ;Retreive command name from associate 0.
 (if (= com "TEXT") (setq com "DTEXT"))     ;If entity is text use DTEXT command.
 (if (= com "POLYLINE") (setq com "PLINE")) ;If entity is a polyline use PLINE command.
 (command com)                              ;Start command sequence.
(prin1))                                    ;Finish routine clean.
;End of File

