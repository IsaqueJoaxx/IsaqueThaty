 (DEFUN C:LLOCK ()
  (SETQ LAYER (GETSTRING "\ninput layer : ") ;Get name of layer to lock.
        F1 (LIST (CONS 8 LAYER))             ;Build dotted pair for SSGET "X" 
                                             ; filter list.
        LAYSET (SSGET "X" F1)                ;Build selection set of all 
                                             ; entities on the selected layer.
        PICK (SSGET)                         ;Select elements and store in 
                                             ; selection set PICK.
        ENTNUM 0                             ;Set counter       
        NEWSET (SSADD)                       ;Cretae new empty selection set.
        ENTTOT (SSLENGTH PICK))              ;Determine the number of elements in PICK.
  (WHILE (< ENTNUM ENTTOT)                   ;Add each element that is in PICK and LAYSET to NEWSET 
   (IF (SSMEMB (SSNAME PICK ENTNUM) LAYSET)  
    (SSADD (SSNAME PICK ENTNUM) NEWSET))
   (SETQ ENTNUM (1+ ENTNUM)))                ;Increment counter.
  (EVAL NEWSET))                             ;Evaluate to the selection set NEWSET.
;END OF FILE