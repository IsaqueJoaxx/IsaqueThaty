; ENT_SRC.LSP
;
;    THIS ROUTINE RETRIEVES THE SELECTED ENTITIES EXTENDED DATA
;    REGISTERED UNDER THE APPLICATION NAME OF
;
;            KENT_44240_GIS_SOURCE
;
;    THE INFORMATION RECORDED UNDER THIS APPLICATION IS INTENDED TO
;    PROVIDE TRACKING OF THE ENTITIES' SOURCE DRAWING FOR GIS INPUT.
;    
;    THE INFORMATION IS RETURNED FOR USE IN GEL/SQL TAGGING OPERATIONS.
;
(defun c:Ent_src ( )
    (setq appname "KENT_44240_GIS_SOURCE")
;
;    INCLUDE NEXT LINE TO SELECT ENTITY OUTSIDE OF GEO/SQL ROUTINES
    (setq tagent (entsel))
;
    (SETQ DSRC nil)
    (setq Ent_cur (entget (CAR tagent) (list "*")))

    (if (setq ent_dsc (assoc -3 ent_cur))
      (progn
        (if (setq ent_dat (assoc appname (cdr ent_dsc)))
          (progn
            (if (setq xd_data (assoc 1000 (cdr ent_dat)))
          (progn
                (SETQ DSRC (CDR XD_DATA))
          )
          (PROGN
                (setq DSRC "NONE")
          )
        )
          )
        )
      )
      (PROGN
;        (princ "\nNo xd_data\n")
        (setq DSRC "NONE")
      )
    )
    (if DSRC (setq x DSRC) (setq x "N/A"))
;
;    INCLUDE THESE LINES TO DISPLAY RESULTS OUTSIDE OF GEO/SQL ROUTINES

;    (PRINC DSRC)
;    (PRINC "\n")
;    (PRINC x)
;    (PRINC "\n")
)

