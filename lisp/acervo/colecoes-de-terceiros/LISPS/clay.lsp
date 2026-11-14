
;---------------------------CLAY.LSP------------------------------------------
;                               by
;                                 BILL CARROLL
;-----------------------------------------------------------------------------

;-----------------------------------------------------------------------------
;       This routine changes entities to the current layer of a drawing.
;       It will also switch layers before selecting the entities.
;
;-----------------------------------------------------------------------------

(DEFUN C:CLAY ()
        (SETQ CMDECHO "0")
        (PRINT "THIS COMMAND WILL MOVE OBJECTS TO THE CURRENT LAYER.")
        (PRINT "PRESS <CTRL-C> TO QUIT")
        (TIMING 400)                                ;Pause
        ;
        (SETQ CHOICE (GETSTRING "\nCHANGE LAYERS? (Y/cr):"))
        (IF (OR (= 89 (ASCII CHOICE)) (= 121 (ASCII CHOICE)))
            (CHANGELAY) (CHANGEFUN))
)
        ;

(DEFUN CHANGELAY ()                                 ;Function to switch layers
        (SETQ OLDLAY (GETVAR "CLAYER"))
        (SETQ NEWLAY (GETSTRING "NEXT LAYER :"))
        (COMMAND "LAYER" "S" NEWLAY "" )
        (CHANGEFUN)
        (SETQ MESS "CHANGES DONE, ")
        (SETQ CHOICE (GETSTRING "\nRESTORE OLD LAYER? (Y/cr):"))
        (IF (OR (= 89 (ASCII CHOICE)) (= 121 (ASCII CHOICE)))
            (RESTORLAY) (SETQ DUMMY () ))
        (PRINC MESS)(PRINC "CURRENT LAYER IS : ")(SETQ MESS NEWLAY)
        )
        ;

(DEFUN RESTORLAY ()                                ;Function to restore layer
        (COMMAND "LAYER" "S" OLDLAY "" )
        (SETQ NEWLAY OLDLAY)
        )
        ;

(DEFUN CHANGEFUN ()              ;Function to select and change entities' layers
        (SETQ SSET (SSGET))                    ;Use Autocad selection set prompt
        (SETQ CURRLAY (GETVAR "CLAYER"))
        (COMMAND "CHANGE" SSET "" "P" "LA" CURRLAY "")  ;Autocad Change command
        (SETQ MESS "CHANGED")
        )
        ;

(DEFUN TIMING (numtimes)               ;Pause loop
        (REPEAT numtimes
        (1+ 5))
        )
        ;

