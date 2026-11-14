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


; ============================
; ==  Set Global Variables  ==
; ============================

(setq *DT_PATH   "c:\\acad\\draftool\\"
      *TBSCALES  "TBSCALES"
      *TBLAYER   "TITLE"
      *DWGSCALE  1
)




; ===============================
; ==  Define Global Functions  ==
; ===============================

(defun dt_fpath (filename / path)
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

(defun dt_load (filename / path)
  (if
    (setq path
      (dt_fpath
        (setq filename
          (if (wcmatch filename "*`.*") filename (strcat filename ".lsp"))
        )
      )
    )
    (load path)
    (alert (strcat "     Cannot load " filename "!     "))
  )
)

(setq dt_helpdlg
  (if
    (and
      (= (getvar "PLATFORM") "Microsoft Windows")
      (dt_load "dt_help")
    )
    new_acad_helpdlg
    acad_helpdlg
  )
)

(defun C:EDTITLE  () (dt_load "EDTITLE")  (C:EDTITLE))
(defun C:INSTITLE () (dt_load "INSTITLE") (C:INSTITLE))
(defun C:MAKEBDR  () (dt_load "MAKEBDR")  (C:MAKEBDR))
(defun C:RE-ORDER () (dt_load "RE-ORDER") (C:RE-ORDER))

