;***** DELTABLE.LSP *****;
;
;++Last Update 05/28/93++
  
;{{/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\}}
;{{{{ Send Comments or Questions (or Donations!) }}}}
;{{{{                                            }}}}
;{{{{ To:                                        }}}}
;{{{{     Owen Wengerd                           }}}}
;{{{{     P.O. Box 84                            }}}}
;{{{{     Fredericksburg, OH  44627              }}}}
;{{{{     (216) 695-5903                         }}}}
;{{{{     71324,3252 (Compu-Serve ID)            }}}}
;{{\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/}}


(defun errexit (s)
  (princ "\nError:  ")
  (princ s)
  (restore)
)

(defun deltablex ()
  (setq  errexit   nil
         deltablex nil
         DELTABLE  nil
  )
  (setvar "CMDECHO" (car oldvar))
  (setvar "EXPERT" (cadr oldvar))
  (setq *error* olderr)
  (princ)
)

(defun DELTABLE (/ oldvar olderr restore table)
  (setq oldvar
    (list
      (getvar "CMDECHO")
      (getvar "EXPERT")
    )
  )
  (setq olderr  *error*
                restore deltablex
                *error* errexit
  )
  (setvar "CMDECHO" 0)
  (setvar "EXPERT" 0)
  (terpri)
  (if
    (setq table
      (getfiled
        "Select Table To Delete:"
        (strcat (if *LISPPATH *LISPPATH "") "*")
        "tbl"
        258
      )
    )
    (command "DEL" table)
  )
  (restore)
)

(DELTABLE)
