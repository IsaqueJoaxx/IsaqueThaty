;  IMPLODE.LSP  Implode   (c)1996, Dennis Kiracofe
; changes ^v^ CAD Studio

(defun C:IMP (/ SLCT CDATE BNAME)
   (setvar "cmdecho" 0)
   (princ "\nSelect items to IMPLODE: ")
   (setq SLCT (ssget))
   (setq
      CDATEX (rtos (getvar "cdate") 2 9)
      BNAME  (strcat (substr CDATEX 10 8))
   )
   (command "_block" BNAME "0,0" SLCT "")
   (command
      "_insert" BNAME "0,0" "" "" ""
   )
)
