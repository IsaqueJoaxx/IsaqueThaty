;***Beginning of FIT.LSP***

(defun c:fit ( / cmdecho
                 startpt
                 endpt
                 oldbase
                 oldang
                 spacing
                 number
                 objects
             )
 (graphscr)
 (setq cmdecho (getvar "cmdecho"))
 (setvar "cmdecho" 0)
 (setq startpt (getpoint "\nPick beginning of array to fit: "))
 (setq endpt (getpoint startpt "\nPick end: "))
 (setq oldbase (getvar "snapbase"))
 (setvar "snapbase" startpt)
 (setq oldang (getvar "snapang"))
 (setvar "snapang" (angle startpt endpt))
 (setq spacing (getdist "\nSpacing: "))
 (setq number (1+ (fix(/ (distance startpt endpt) spacing))))
 (setq objects (ssget))
 (command "array" objects "" "r" 1 number spacing)
 (setvar "snapbase" oldbase)
 (setvar "snapang" oldang)
 (setvar "cmdecho" cmdecho)
 (princ)
)

;***End of FIT.LSP***
