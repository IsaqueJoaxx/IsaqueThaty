(DEFUN C:BLK ()
   ;
   ; Mike Pillers     22:41:27  2/24/1987
   ; Machine/ Mechanism Design Consultant
   ; San Jose, CA          (408) 280-7959
   ;
   ; Allows user to pick a pre-defined block from the screen menu.
   ; Program searches database for all existing blocks and displays
   ; them on the screen. Requires AutoCAD version 2.6.
   ;
  (SETQ BLKS (CDR (ASSOC 2 (TBLNEXT "BLOCK" T))))
  (SETQ BLKS (LIST (CDR (ASSOC 2 (TBLNEXT "BLOCK"))) BLKS))
  (WHILE (SETQ TMP (TBLNEXT "BLOCK")) 
         (SETQ BLKS (CONS (CDR (ASSOC 2 TMP)) BLKS))
  )
  (SETQ I 0)
  (FOREACH N BLKS (PROGN (GRTEXT I N) (SETQ I (1+ I))))
  (COMMAND "INSERT" (NTH (NTH 1 (GRREAD)) BLKS) )
  (GRTEXT)
)
                                                              