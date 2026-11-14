; SAVE2A.LSP

;THIS IS TO AUTOMATE THE SAVING OF THE DWG FILE TO THE FLOPPY DRIVE A:
;modified to accomate Release 11's popup dialogue boxes

(DEFUN C:SAVE2A (/ DN)
  (setvar "filedia" 0);omit if using Rel 10
  (SETQ DN (GETVAR "DWGNAME"))
  (COMMAND "SAVE" (STRCAT "A:" DN) "Y")
  (setvar "filedia" 1);omit if using Rel 10
  (PROMPT (strcat dn ".dwg  " "SAVED TO DRIVE A: " )) (TERPRI)
  (PRINC)
)

