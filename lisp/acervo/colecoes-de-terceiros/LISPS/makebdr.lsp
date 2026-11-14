; DrafTools   [Version 1.00a] 4/6/94      
;
; ***************************************
; ****  Author:  Owen Wengerd        ****
; ****                               ****
; ****  Manu-Soft Computer Services  ****
; ****  P.O. Box 84                  ****
; ****  Fredericksburg, OH  44627    ****
; ****  (216) 695-5903               ****
; ****  Compu-Serve ID:  71324,3252  ****
; ***************************************


(defun C:MAKEBDR (/ tmp1 tmp2 errexit makebdrx fpath getpath
                  dlg_retcode dcl_id xdos_file xdos_loaded)

; *** Local Functions ***

(defun errexit (s)
  (princ "\nError:  ")
  (princ s)
  (restore)
)

(defun makebdrx ()
  (if (tblsearch "VIEW" "TEMP")
    (command "_VIEW" "_R" "TEMP" "_VIEW" "_D" "TEMP" "_UCS" "_P")
  )
  (setvar "CMDECHO" oldvar)
  (or
    (not xdos_loaded)
    (= T xdos_loaded)
    (xunload xdos_file nil)
    (princ "\n**Cannot unload " xdos_file " from memory**\n ")
  )
  (setq *error* olderr)
  (princ)
)

(defun fpath (filename / path)
  (if 
    (and
      *DT_PATH 
      (setq path
        (findfile 
          (strcat 
            *DT_PATH 
            (if (= "\\" (substr *DT_PATH (strlen *DT_PATH) 1)) "" "\\")  
            filename
          )
        )
      )
    )
    path
    (findfile filename)
  )
)

(defun getpath (pname / cnt)
  (if (= 'STR (type pname))
    (progn
      (setq cnt (strlen pname))
      (while
        (and
          (> cnt 0)
          (/= ":" (substr pname cnt 1))
          (/= "\\" (substr pname cnt 1))
        )
        (setq cnt (1- cnt))
      )
      (substr pname 1 cnt)
    )
  )
)


;***********************************************
;***************  Main Program  ****************
;***********************************************

  (setq T (not nil))
  (setq olderr  *error*
        restore makebdrx
        *error* errexit
  )
  (setq oldvar (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)
  (terpri)
  (if 
    (and
      (or 
        (setq xdos_loaded (= 'EXSUBR (type dt_dosdir))) 
        (setq xdos_loaded
          (if
            (setq tmp1
              (fpath
                (setq xdos_file
                  (if (= (getvar "PLATFORM") "Microsoft Windows")
                    "xdosw_dt.exe"
                    "xdos_dt.exp"
                  )
                )
              ) 
            ) 
            (xload tmp1 nil)
          )
        )
      )
      (setq dcl_id (if (setq tmp1 (fpath "MAKEBDR.DCL")) (load_dialog tmp1)))
    )
    (progn
      (command "_VIEW" "_S" "TEMP" "_UCS" "")
      (setq dlg_retcode 6)
      (while (and (> dlg_retcode 1) (new_dialog "MAKEBDR" dcl_id))
        (setq dlg_retcode (start_dialog))
        (cond
          ( (= 2 dlg_retcode)
            (if (setq tmp1 (getpoint "\n \nPick Insertion Point: "))
              (command "_MOVE" "_ALL" "" tmp1 '(0 0 0) "_ZOOM" "_E")
            )
          )  
          ( (= 3 dlg_retcode)
            (if C:RE-ORDER
              (C:RE-ORDER)
              (if 
                (and
                  (setq tmp1 (fpath "RE-ORDER.LSP"))
                  (load tmp1)
                )
                (C:RE-ORDER)
                (alert 
                  (strcat "Cannot find file 'RE-ORDER.LSP'"
                          "\n   in current search path!"
                  )
                )
              )
            )
          )
          ( (= 1 dlg_retcode)
            (if
              (setq tmp1 
                (getfiled "Enter Border Name" (getvar "DWGNAME") "BDR" 3)
              )
              (progn
                (dt_dosdeletefile
                  (setq tmp2 (strcat (getpath tmp1) "TEMPBDR.DWG"))
                )
                (command "WBLOCK" tmp2 "*")
                (dt_dosdeletefile tmp1)
                (dt_dosrename tmp2 tmp1)
              )
            )
          )
        )
      )
    )
    (alert 
      (if xdos_loaded
        (strcat 
          "Dialog Box Definition File 'INSTITLE.DCL' not Found"
          "\n                Cannot Continue!"
        )
        (strcat
          "ADS Application '"
          xdos_file
          "' not Found\n          Cannot Continue!"
        )
      )
    )
  )
  (restore)
)
