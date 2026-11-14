; TIP909.LSP: CLIPBRD.LSP   Cut, Paste, and Copy   (c)1993,  Kevin Kronschnabl
;;  Date:     8-06-93
;;  COPYRIGHT (C) 1993, KMK CAD
;;  ===========================
;;
;;  The CLIPBRD is a temporary storage area for cut or copied drawing
;;  entities that you want to move or copy from one location to another.
;;  You can paste the contents of the CLIPBRD into any AutoCAD drawing.
;;  The CLIPBRD holds the information until you cut or copy other drawing
;;  entities.  You can use several subcommands to control the CLIPBRD.
;;  Each subcommand can be abbreviated to the capital letter shown.
;; 
;;       CUT    - Writes the selected objects to the CLIPBRD file
;;       Copy   - Copies the selected objects to the CLIPBRD file
;;       Paste  - Pastes the information from the CLIPBRD to the drawing
;;       ?      - HELP
;;
;;  To copy or move drawing entities:
;;
;;       1   Do one of the following:
;;           To copy drawing entities, choose Copy.
;;           To move drawing entities, choose Cut.
;;       2   Select the drawing entities you want to copy or move.
;;       3   Open the other drawing (if necessary).
;;       4   Choose Paste.
;;       5   Position the insertion point where you want to insert
;;           the drawing entities.
;;
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
   (if (/= s "Function cancelled")
      (if (= s "quit / exit abort")
         (princ)
         (princ (strcat "\nError: " s))
      )
   )
   (RVAR)
   (princ)
);defun
;;------------------------------------------------------------------------
;get system variables
(defun GVAR()
   (setq CMDE (getvar "cmdecho"))
   (setq MNUE (getvar "menuecho"))
);defun
;;------------------------------------------------------------------------
;set system variables
(defun SVAR()
   (setvar "cmdecho" 0)
   (setvar "menuecho" 0)
);defun
;;------------------------------------------------------------------------
;reset system variables
(defun RVAR()
   (setvar "cmdecho" CMDE)
   (setvar "menuecho" MNUE)
);defun
;;------------------------------------------------------------------------
;read help
(defun HELP(/ FILE LINE)
   (setq FILE (open (findfile "clipbrd.lsp") "r")
      LINE (read-line FILE)
   );setq
   (repeat 30
      (setq LINE (substr LINE 5 75))
      (write-line LINE)
      (setq LINE (read-line FILE))
   );repeat
   (close FILE)
   (princ)
);defun
;;------------------------------------------------------------------------
;main body
(defun C:CLIPBRD(/ QST ORG IPT OBJ)
   (GVAR)
   (SVAR)
   (setq fname "c:/acad12/clipbrd")   ;; revise this to suit
   (initget "CUT COPY PASTE C P ?")
   (setq QST (getkword "\nSelect option - CUT/Copy/Paste/?/<Copy>: "))
   (if (= QST "?")
      (progn
         (HELP)
         (initget "CUT COPY PASTE C P")
         (setq QST (getkword "\nSelect option - CUT/Copy/Paste/<Copy>: "))
      );progn
   );if
   (if (or (= QST nil) (= QST "C") (= QST "COPY"))
      (if (= (findfile "clipbrd.dwg") nil)
         (progn
            (initget 1)
            (setq IPT (getpoint "\nPick insertion point: "))
            (setq OBJ (ssget))
            (command "wblock" fname "" IPT OBJ "")
            (command "oops")
         );progn
         (progn
            (initget 1)
            (setq IPT (getpoint "\nPick insertion point: "))
            (setq OBJ (ssget))
            (command "wblock" fname "y" "" IPT OBJ "")
            (command "oops")
         );progn
      );if
   );if
   (if (= QST "CUT")
      (if (= (findfile "clipbrd.dwg") nil)
         (progn
            (initget 1)
            (setq IPT (getpoint "\nPick insertion point: "))
            (setq OBJ (ssget))
            (command "wblock" fname "" IPT OBJ "")
         );progn
         (progn
            (initget 1)
            (setq IPT (getpoint "\nPick insertion point: "))
            (setq OBJ (ssget))
            (command "wblock" fname "y" "" IPT OBJ "")
         );progn
      );if
   );if
   (if (or (= QST "P") (= QST "PASTE"))
      (if (= (findfile "clipbrd.dwg") nil)
         (princ "\nClipboard is empty...")
         (progn
            (initget 1)
            (setq IPT2 (getpoint "\nPick insertion point: "))
            (command "insert" (strcat "*"(findfile "clipbrd.dwg")) IPT2 "" "")
         );progn
      );if
   );if
   (RVAR)
   (princ)
); end clipbrd.lsp
