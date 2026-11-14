; *****************************************************************
;
;            L.LSP
;
;       Author: Paul Nance
;         Home: (919)-226-3648
;   Compuserve: [73627,1733]
;
;  Function Description
;  -------------------------------------------------------------
;     C:L  This routine loads a lisp file while in AutoCAD.
;          If loading file from outside of default drive
;          or path, drive and path are required, otherwise
;          filename alone is all that is required.
;       Note:
;          The "" marks or .lsp extention are not required.
;
(defun c:L ( / lo)
(setq lo (getstring "\n File to Load : "))
(load lo))
