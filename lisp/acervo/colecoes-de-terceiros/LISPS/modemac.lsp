;MODEMAC.LSP: CADalyst October customizing tutorial custom status line
; by Mark Middlebrook, 3 August 93

; Note: This .LSP file must be loaded each time AutoCAD initializes the
;       drawing editor.  In order to do this, use the s::startup function
;       in ACAD.LSP.  If you already have an ACAD.LSP file and it does not
;       yet contain a line that starts "(defun s::startup...", then add
;       these four lines:
;           (defun s::startup ()
;              (load "MODEMAC")
;              (princ)
;           )
;       If you don't yet have an ACAD.LSP file, then create one containing
;       the same four lines.  If you have an ACAD.LSP file and it does
;       already contains an s::startup function, then just add the line
;              (load "MODEMAC")
;       somewhere inside the function.

(setvar "MODEMACRO" (strcat
   "L>"
   "$(getvar,CLAYER)"
   "$(if,$(getvar,ORTHOMODE), O, )"
   "$(if,$(getvar,SNAPMODE), S>$(rtos,$(index,0,$(getvar,SNAPUNIT))), )"
   "$(if,$(and,$(=,$(getvar,TILEMODE),0),$(=,$(getvar,CVPORT),1)), P)"
   " ["
   "$(if,$(eq,$(getvar,DWGPREFIX),"
      "$(substr,$(getvar,DWGNAME),1,$(strlen,$(getvar,DWGPREFIX)))),"
      "$(substr,$(getvar,DWGNAME),$(+,1,$(strlen,$(getvar,DWGPREFIX)))),"
      "$(getvar,DWGNAME)"
   ")"
   "]"
))
