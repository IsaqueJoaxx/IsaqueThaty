(DEFUN C:LAYR ()
   ;
   ; Mike Pillers     22:41:27  2/24/1987
   ; Machine/ Mechanism Design Consultant
   ; San Jose, CA          (408) 280-7959
   ;
   ; Allows user to pick a new current layer from the screen menu.
   ; Program searches database for all existing layers and displays
   ; them on the screen. Needs AutoCAD version 2.6.
   ;
  (SETQ LAYS (CDR (ASSOC 2 (TBLNEXT "LAYER" T))))
  (SETQ LAYS (LIST (CDR (ASSOC 2 (TBLNEXT "LAYER"))) LAYS))
  (WHILE (SETQ TMP (TBLNEXT "LAYER")) 
         (SETQ LAYS (CONS (CDR (ASSOC 2 TMP)) LAYS))
  )
  (SETQ I 0)
  (FOREACH N LAYS (PROGN (GRTEXT I N) (SETQ I (1+ I))))
  (COMMAND "LAYER" "S" (NTH (NTH 1 (GRREAD)) LAYS) "")
  (GRTEXT)
)
