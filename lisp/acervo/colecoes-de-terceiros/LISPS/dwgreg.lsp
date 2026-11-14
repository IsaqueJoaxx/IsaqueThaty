;==============================================================================
;
;  DWGREG.LSP version 1.00 (c) Christopher Lovelock - 10 January 1994
;  SysOp of CADenza BBS +44(0) 533 596725 U.K.
;  PCGnet 9:526/725
;  FidoNet 2:2503/308
;  CompuServe 100111,2200
;
;  This routine uses the Norton Utilities FI.EXE, FileInfo, program to
;  attach and list information to/from drawing files.  FI.EXE must be
;  somewhere on the DOS path.  The routine also uses the DOS commands
;  SORT MORE and FIND.  Finally it also uses the SH command to Shell to
;  DOS.
;
;------------------------------------------------------------------------------

;----- strip DWGNAME of drive and path and set to stfn

(defun stfn (s)
   (if (wcmatch s "*\\*,*/*,*:*")
       (substr s (1+ (stfnx (strlen s))))
       s
   )
)

(defun stfnx (n)
   (cond
      (  (zerop n) 0)
      (  (member (substr s n 1) '("\\" "/" ":")) n)
      (t (stfnx (1- n)))
   )
)

(if (= $na nil)
   (setq $na (stfn "*"))
)

(if (= $se nil)
   (setq $se "DWG")
)

;----------

(defun getn ()
   (prompt "\nDrawing Name(s)/<")(princ $na)(princ ">: ")
   (setq na (getstring))
      (if (> (strlen na) 8)
          (progn
             (princ "\nName too long - using default name!")(print)
             (setq na $na)
          )
      )
   (if (= na "") (setq na $na))
   (setq $na na)
)

;----------

(defun getw ()
   (prompt "\nSearch string/<")(princ $se)(princ ">: ")
   (setq se (getstring))
   (if (= se "") (setq se $se))
   (setq $se se)
)

;----------

(defun edit ()
   (getn)
   (setq com (strcat "FI " na ".DWG /E"))
   (command "sh" com)
)

;----------

(defun hard ()
   (getn)
   (setq com (strcat "FI " na ".DWG /L /C | SORT > PRN"))
   (command "sh" com)
)

;----------

(defun dlis ()
   (getn)
;   (setq com (strcat "FI " na ".DWG /L /C |SORT|MORE"))
   (setq com (strcat "FI " na ".DWG /L /C |FIND /I " (chr 34) "DWG" (chr 34) "|SORT|MORE"))
   (command "sh" com)
)

;----------

(defun upda ()
   (setq com (strcat "FI " (stfn (getvar "DWGNAME")) ".DWG /E"))
   (command "sh" com)
)

;----------

(defun pack ()
   (command "sh" "FI /Pack")
)

;----------

(defun sear ()
   (getn)
   (getw)
   (setq com (strcat "FI " na ".DWG /L /C |FIND /I " (chr 34) se (chr 34) "|SORT|MORE"))
   (command "sh" com)
)

;----------

(command "UNDEFINE" "END")
(defun C:END ()
   (upda)
   (command ".END")
   (princ)
)

;----------

(command "UNDEFINE" "NEW")
(defun C:NEW ()
   (upda)
   (command ".NEW")
   (princ)
)

;----------

(command "UNDEFINE" "OPEN")
(defun C:OPEN ()
   (if (/= (GETVAR "DWGNAME") "UNNAMED")
      (upda)
   )
   (command ".OPEN")
   (princ)
)

;----------

(defun C:DWGREG ()
   (PRINC "\nGreetings from Christopher Lovelock!")
   (PRINC "\nCADenza BBS +44(0) 533 596725 U.K.") 
   (PRINC "\nPCGnet  9:526/725") 
   (PRINC "\nFidoNet 2:2503/308")
   (PRINC "\nCompuServe 100111,2200\n")
   (prompt "\nDrawing Register")
   (setq key nil)
   (initget "Edit Hard Pack Search ?  ")
   (setq key (getkword "\nEdit/Hardcopy/Pack/Search/<?>: "))
   (cond ((= key "Edit")   (edit))
         ((= key "Hard")   (hard))
         ((= key "Pack")   (pack))
         ((= key "Search") (sear))
         ((= key "?")      (dlis))
   )
   (if (= key nil)
      (dlis)
   )
   (princ)
)
