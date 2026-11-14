; TIP960.LSP: DATUM.LSP   Datum Dimensioning   (c)1994, Kevin Kronschnabl

;; The DATUM command enters the coordinate dimensioning mode.  From there you
;; can use the DATUM dimensioning subcommands to setup and create a dimension.
;; Each subcommand can be abbreviated to the capital letters shown.  A space 
;; or RETURN will prompt for the start and placement points of the dimension.  
;; 
;;       Origin       -  Sets the origin to be dimensioned from
;;       Dimension    -  Dimensions a point from the origin
;;       Reset origin -  Resets the origin back to the world coordinate system
;;       ?            -  This help Screen
;; IMPORTANT:
;; When finished dimensioning your drawing, use reset origin to reestablish
;; the drawings original 0,0,0 point.
;;------------------------------------------------------------------------
; Internal error handler defined locally

(defun *error* (msg)
   (princ "error: ")
   (princ msg)
   (terpri)
   (princ)
)
(defun err (s)                   ; If an error (such as CTRL-C) occurs
   ; while this command is active...   
   (if (/= s "Function canceled")
      (if (= s "quit / exit abort")
         (princ)
         (princ (strcat "\nError: " s))
      )
   )
   (RVAR)
   (princ)
);defun
;;------------------------------------------------------------------------
;get variables
(defun GVAR()
   (setq FDIA (getvar "filedia")) ;get system settings
   (setq CMDE (getvar "cmdecho"))
   (setq MNUE (getvar "menuecho"))
   (setq EXPT (getvar "expert"))
   (setq CCLR (getvar "cecolor"))
   (setq CLTP (getvar "celtype"))
   (setq UCSO (getvar "ucsorg"))
);defun
;;------------------------------------------------------------------------
;set variables
(defun SVAR()
   (setvar "cmdecho" 1)         ;turn command echo off
   (setvar "menuecho" 1)        ;turn menu echo off
);defun
;;------------------------------------------------------------------------
;reset variables
(defun RVAR()
   (setvar "cmdecho" CMDE)
   (setvar "cmdecho" MNUE)
);defun
;;------------------------------------------------------------------------
;read help
(defun DHLP(/ FILE LINE)
   (setq FILE (open (findfile "datum.lsp") "r")
      LINE (read-line FILE)
   );setq
   (repeat 24
      (setq LINE (substr LINE 4 75))
      (write-line LINE)
      (setq LINE (read-line FILE))
   );repeat
   (close FILE)
   (princ)
);defun

;;------------------------------------------------------------------------
;main body
(defun C:DATUM(/ QST ORG)
   (setvar "cmdecho" 0)
   (initget "ORIGIN DIM DIMENSION RESET O D R ?")
   (setq QST (getkword "\nSelect option - Origin/Dimension/Reset origin/?/<Dimension>: "))
   (if (= QST "?")
      (progn
         ;      (command "type" (strcat (findfile "datum.hlp") "|more"))
         (DHLP)
         (initget "ORIGIN DIM DIMENSION RESET O D R")
         (setq QST (getkword "\nSelect option - Origin/Dimension/Reset origin/<Dimension>: "))
      );progn
   );if
   (if (or (= QST nil) (= QST "D") (= QST "DIM") (= QST "DIMENSION"))
      (progn
         (initget 1)
         (setq SPT (getpoint "\nPick starting point: "))
         (initget 1)
         (setq EPT (getpoint "\nPick ending point: " SPT))
         (command "dim" "ordinate" SPT EPT "")
         (command "exit")
      );progn
   );if
   (if (or (= QST "O") (= QST "ORIGIN"))
      (progn
         (initget 1)
         (setq ORG (getpoint "\nPick the Origin Point: "))
         (command "ucs" "origin" ORG)
      );progn
   );if
   (if (or (= QST "R") (= QST "RESET"))
      (progn
         (command "ucs" "world")
         (princ "\nThe origin has been reset...")
      );progn
   );if
   (princ)
); end datum.lsp
