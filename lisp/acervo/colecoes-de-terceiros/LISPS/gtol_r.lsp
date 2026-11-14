; GTOL_R.LSP
;***********************************************************
; Corrections To GTOL.LSP from CADalyst Toolbox Nov. '93
; 컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴�
;  Rename the following symbols:

;    Old Name                  New Name
;컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴컴
;    FORM1.DWG                  FORM0.DWG
;    FORM2.DWG                  FORM1.DWG
;    FORM3.DWG                  FORM2.DWG
;    FORM4.DWG                  FORM3.DWG
;   LOCAT1.DWG                 LOCAT0.DWG
;   LOCAT2.DWG                 LOCAT1.DWG
;  ORIENT1.DWG                ORIENT0.DWG
;  ORIENT2.DWG                ORIENT1.DWG
;  ORIENT3.DWG                ORIENT2.DWG
; PROFILE1.DWG               PROFILE0.DWG
; PROFILE2.DWG               PROFILE1.DWG
;  RUNOUT1.DWG                RUNOUT0.DWG
;  RUNOUT2.DWG                RUNOUT1.DWG
;
  (setq SYM "FORM0" X1 "0") ;Add at the beginning of GTOL

  (action_tile "mtlc" "(setq X4 $value) (getmtl 5)")
; Replace the existing line in GTOL with this one.

; 컴컴컴�- Replace previous subroutines with these
  (defun DOCAT ()
     (cond
      ((= X1 "0") (dref 1) (dolist C0) (setq SYM "FORM0"))
      ((= X1 "1") (dref 0) (dolist C1) (setq SYM "ORIENT0"))
      ((= X1 "2") (dref 0) (dolist C2) (setq SYM "LOCAT0"))
      ((= X1 "3") (dref 0) (dolist C3) (setq SYM "RUNOUT0"))
      ((= X1 "4") (dref 0) (dolist C4) (setq SYM "PROFILE0"))
      )
  )

  (defun GETMTL (XXX)
    (cond
      ((= XXX 00)(setq TZS  (nth (atoi X3)  SMOD)))
      ((= XXX 5) (setq MTLC (nth (atoi X4)  MODS)))
      ((= XXX 1) (setq MAT1 (nth (atoi MT1) MODS)))
      ((= XXX 2) (setq MAT2 (nth (atoi MT2) MODS)))
      ((= XXX 3) (setq MAT3 (nth (atoi MT3) MODS)))
    )
  )
