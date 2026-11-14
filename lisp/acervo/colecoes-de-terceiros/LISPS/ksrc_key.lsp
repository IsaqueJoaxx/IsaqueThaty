; KSRC_KEY.LSP
;
;    THIS ROUTINE PLACES THE CURRENT DRAWING'S SYSTEM KEY IN THE EXTENDED
;    DATA OF SELECTED ENTITES.  THE INFORMATION IS REGISTERED UNDER
;    THE APPLICATION NAME OF
;
;    KENT_44240_GIS_SOURCE
;
;    FOR SOURCE DRAWING SYSTEM KEY, AND
;
;    KENT_44240_GIS_INPUT
;
;    FOR INPUT DRAWING SYSTEM KEY.
;
;    THE INFORMATION RECORDED UNDER THESES APPLICATIONS IS INTENDED TO
;    PROVIDE TRACKING OF THE ENTITIES' SOURCE AND INPUT DRAWINGS
;    FOR GIS INPUT.
;
; ********** BEGIN XD_CHECK **********
(defun XD_CHECK ()
(IF (ASSOC -3 ENTDSC)        ; IF THERE IS EXTENDED DATA
    (PROGN
        (SETQ ENTDAT (ASSOC -3 ENTDSC))
        (SETQ APPLIST (CDR ENTDAT))    ; GET RID OF -3
        (IF  (SETQ APPCUR (ASSOC APPNAME APPLIST))
            ( XD_EDIT )
            ( XD_ADD )
        )
        ( XD_BUILD )
    )
    (PROGN            ; IF THERE IS NO EXTENDED DATA
        ( XD_MAKE )    ; CREATE APPLICATION XDATA 
    )
)
)    ; ********** END XD_CHECK **********
    ; ********** BEGIN XD_EDIT **********
(defun XD_EDIT ()
    (IF APPUPDT
        (PROGN
            (PRINC "\n")
            (PRINC (ASSOC 1000 (CDR APPCUR) ))
            (PRINC "\n CHANGED TO \n")
            (PRINC (ASSOC 1000 (CDR APPSET) ))
            (SETQ APPCUR APPSET)
            (SETQ APPCUR APPCUR)
            (Setq DIDIT (+ 1 DIDIT))
        )
        (PROGN
            (PRINC "\n")
            (PRINC (ASSOC 1000 (CDR APPCUR) ))
            (PRINC "\n NOT CHANGED \n")
        )
    )
)    ; ********** END XD_EDIT **********
    ; ********** BEGIN XD_ADD **********
(defun XD_ADD ()
(SETQ APPCUR APPSET)
(SETQ APPLIST (CONS APPCUR APPLIST))
(Setq DIDIT (+ 1 DIDIT))
)    ; ********** END XD_ADD **********
    ; ********** BEGIN XD_BUILD **********
(defun XD_BUILD ()
(SETQ APPLIST 
    (SUBST    APPCUR
        (ASSOC APPNAME APPLIST)
        APPLIST
    )
)
(SETQ ENTDAT (CONS -3 APPLIST))
(SETQ ENTDSC
    (SUBST    ENTDAT
        (ASSOC -3 ENTDSC)
        ENTDSC
    )
)
(IF (ENTMOD ENTDSC)
    (PRINC "\nDATA POSTED TO ENTITY")
    (PRINC "\nNO DATA POSTED TO ENTITY")
)
(SETQ APPLIST NIL)
)    ; ********** END XD_BUILD **********
    ; ********** BEGIN XD_MAKE **********
(defun XD_MAKE ()
    (SETQ APPCUR APPSET)
    (SETQ ENTDAT (LIST -3 APPCUR))
    (SETQ ENTDSC (CONS ENTDAT ENTDSC))
    (IF (ENTMOD ENTDSC)
        (PRINC "\nDATA POSTED TO ENTITY")
        (PRINC "\nNO DATA POSTED TO ENTITY")
    )
    (Setq DIDIT (+ 1 DIDIT))
)    ; ********** END XD_MAKE **********
    ; ********** BEGIN XD_MSG1 **********
(defun XD_MSG1 ()
    (PRINC "\n TRYING TO ADD EXTEDED DATA TO ")
    (PRINC ENTTYPE)
    (PRINC " ENTITY.")
    (SETQ EDANS2 
        (GETSTRING "\n O.K. TO PROCEED? (Y/N): ")
    )
    (IF (OR (= EDANS2 "Y") (= EDANS2 "y"))
        (XD_CHECK)
    )
)    ; ********** END XD_MSG1 **********
    ; ********** BEGIN C:SRC_KEY **********
(defun C:SRC_KEY ()
    (SETQ SRCFIL (strcat (getvar "dwgprefix") (getvar "dwgname") ) )
    (SETQ SRCDATE (GETVAR "CDATE") )
    (SETQ SRCDATE (RTOS SRCDATE 2 9) )
    (TEXTPAGE)
    (PRINC SRCFIL )
    (PRINC " - ")
    (PRINC SRCDATE)
    (PRINC "\n")
    (SETQ APPICK (GETSTRING T "\nREGISTER SOURCE KEY FOR THIS DRAWING (Y/N): "))
    (IF (OR (= APPICK "Y") (= APPICK "y"))
        (SETQ APPNAME "KENT_44240_GIS_SOURCE")
        (SETQ APPNAME "KENT_44240_GIS_INPUT")
    )
    (IF (NOT
         (SETQ SRCKEY (GETSTRING T "\nENTER SOURCE KEY FOR THIS DRAWING: "))
        )
        (SETQ SRCKEY "NONE")
    )
;        BUILD XDATA FOR APPLICATION
    (SETQ APPSET (LIST '(1002 . "}")))
    (SETQ APPCODE 1000)
    (SETQ APP_VAL SRCKEY)
    (SETQ APPSET (CONS (CONS APPCODE APP_VAL) APPSET))
    (SETQ APPSET (CONS '(1002 . "{") APPSET))
    (SETQ APPSET (CONS APPNAME APPSET))
;        PROCESS SELECTED ENTITIES
    (IF  (REGAPP APPNAME)
        (PROGN
            (PRINC "\nNEW APPLICATION REGISTERED: ")
            (PRINC APPNAME)
        )
        (PROGN
            (PRINC "\nEXISTING APPLICATION: ")
            (PRINC APPNAME)
            (SETQ EDANS1 
                (GETSTRING T "\nCHANGE EXISTING XDATA? (Y/N) ")
            )
            (IF (OR (= EDANS1 "Y") (= EDANS1 "y"))
                (SETQ APPUPDT 1)    ; CHANGE EXISTING XDATA
                (SETQ APPUPDT NIL)    ; DON'T CHANGE 
            )
        )
    )
    (GRAPHSCR)
    (Prompt "\nPick ENTITIES TO PROCESS")
    (Setq D 0)
    (Setq DIDIT 0)
    (Setq E (Ssget))
    (Setq F (Sslength E))
    (Repeat F
        (Setq G (Ssname E D))
        (IF (XDROOM G)
          (PROGN
            (Setq ENTDSC (Entget G (list "*")))
            ( XD_CHECK )
          )
        )
        (Setq D (+ 1 D))
    )
    (PRINC "\n")
    (PRINC D)
    (PRINC " ENTITIES PROCESSED.\n")
    (PRINC DIDIT)
    (PRINC " ENTITIES XDATA PROCESSED.")
    (PRINC )
)    ; ********** END C:SRC_KEY **********


