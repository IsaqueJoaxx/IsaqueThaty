; **********************************************************
; GTOL.LSP
; Copyright (c) Barry R. Bowen 1993
; ----------------------------------------------------------

(defun C:GTOL (/ C0 C1 C2 C3 C4 CAT DCL_ID DO_NXT DR1 DR2
                 DR3 MAT1 MAT2 MAT3 MODS MT1 MT2 MT3 MTLC
                 SMOD TV X1 X2)

 (setvar "cmdecho" 0)

(defun DREF (VAL)
  (foreach N '("dr1" "mat1" "dr2" "mat2" "dr3" "mat3")
   (mode_tile N VAL))
)

  (setq DCL_ID (load_dialog "gtol.dcl"))
  (if (not (new_dialog "gtol" DCL_ID)) (exit))
  (setq CAT (list "Form" "Orientation" "Location" "Runout"
                  "Profile"))
  (start_list "cat")
  (mapcar 'add_list CAT)
  (end_list)

  (defun DOCAT ()
      (cond
        ((= X1 "0") (dref 1) (dolist C0) (setq SYM "F0"))
        ((= X1 "1") (dref 0) (dolist C1) (setq SYM "O0"))
        ((= X1 "2") (dref 0) (dolist C2) (setq SYM "L0"))
        ((= X1 "3") (dref 0) (dolist C3) (setq SYM "R0"))
        ((= X1 "4") (dref 0) (dolist C4) (setq SYM "P0"))
      )
  )

  (defun DOLIST (LST)
    (start_list "char")
    (mapcar 'add_list LST)
    (end_list)
  )

  (setq C0 (list "Flatness" "Straightness" "Circularity"
                 "Cylindricity")
        C1 (list "Perpendicularity" "Angularity" "Parallelism")
        C2 (list "Position" "Concentricity")
        C3 (list "Circular Runout" "Total Runout")
        C4 (list "Line Profile" "Surface Profile")
  )

  (defun DOCHAR ()
    (cond
      ((= X1 "0") (setq SYM (strcat "FORM" X2)))
      ((= X1 "1") (setq SYM (strcat "ORIENT" X2)))
      ((= X1 "2") (setq SYM (strcat "LOCAT" X2)))
      ((= X1 "3") (setq SYM (strcat "RUNOUT" X2)))
      ((= X1 "4") (setq SYM (strcat "PROFILE" X2)))
    )
  )

  (defun GETMTL (XXX)
    (cond
      ((= XXX 00)(setq TZS  (nth (atoi X3)  SMOD)))
      ((= XXX 0) (setq MTLC (nth (atoi X4)  MODS)))
      ((= XXX 1) (setq MAT1 (nth (atoi MT1) MODS)))
      ((= XXX 2) (setq MAT2 (nth (atoi MT2) MODS)))
      ((= XXX 3) (setq MAT3 (nth (atoi MT3) MODS)))
    )
  )

  (setq SMOD (list "None" "DIA")
        MODS (list "None" "MMC" "LMC" "RFS" "PTZ"))

  (start_list "tzs")
  (mapcar 'add_list SMOD)
  (end_list)

  (foreach N '("mtlc" "mat1" "mat2" "mat3")
       (start_list N)
       (mapcar 'add_list MODS)
       (end_list)
  )

  (mode_tile "dr1" 1)
  (mode_tile "mat1" 1)
  (mode_tile "dr2" 1)
  (mode_tile "mat2" 1)
  (mode_tile "dr3" 1)
  (mode_tile "mat3" 1)

  (foreach N '(mtlc mat1 mat2 mat3) (setq N "None"))
  (setq TV "0.1")
  (set_tile "tv" TV)
  (DOLIST C0)

  (action_tile "cat"    "(setq X1 $value) (docat)")
  (action_tile "char"   "(setq X2 $value) (dochar)")
  (action_tile "tzs"    "(setq X3 $value) (getmtl 00)")
  (action_tile "tv"     "(setq TV $value)")
  (action_tile "mtlc"   "(setq X4 $value) (getmtl 0)")
  (action_tile "dr1"    "(setq DR1 $value)")
  (action_tile "mat1"   "(setq MT1 $value) (getmtl 1)")
  (action_tile "dr2"    "(setq DR2 $value)")
  (action_tile "mat2"   "(setq MT2 $value) (getmtl 2)")
  (action_tile "dr3"    "(setq DR3 $value)")
  (action_tile "mat3"   "(setq MT3 $value) (getmtl 3)")
  (action_tile "accept" "(done_dialog 4)")
  (action_tile "cancel" "(done_dialog)")

  (setq do_nxt (start_dialog))

; ************ THIS WILL BE COMPLETED NEXT MONTH ***********
  (if (= do_nxt 4) (ADDFRM))
; **********************************************************

  (unload_dialog dcl_id)
  (princ)
)

(prompt "\nGTOL Loaded!...Copyright Barry R. Bowen 1993")
(princ)
