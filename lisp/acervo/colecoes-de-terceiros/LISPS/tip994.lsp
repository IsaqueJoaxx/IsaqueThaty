; TIP994.LSP: OSL.LSP   Offset to a Layer   (c)1994, Chris Winsor

;;Offset Layer Program for Release 11-12 users.

;This function prompts the user for an object to offset
;while allowing the resultant entity's layer to be
;changed.  Note: The layer must already exist.  The
;layer and offset distance will be defaults the next time
;the routine is called.

(defun C:OSL (/ DEFAULT DIST OBJ PT)
 (setvar "cmdecho" 0)
 (prompt "\nOffset then change layer..." )
 (setq CURRENT (getvar "CLAYER"))
  (if (< (getvar "offsetdist") 0)    ;Checks for offset through
   (setvar "offsetdist" 1.0)         ;condition and changes it to
  )                                  ;offset distance (default 1 unit)
 (setq DEFAULT (getvar "offsetdist"))
 (prompt "\nOffset distance: <")
 (princ DEFAULT)
 (prompt "> ")
 (setq DIST (getdist))
  (if (= DIST nil)
   (setq DIST DEFAULT)
  )
  (while (= OBJ nil)
   (setq OBJ (entsel "\nSelect object to offset: "))
  )
 (setq PT (getpoint "\nSide to offset? "))
  (if (/= NEW2 nil)
   (progn
    (prompt "\nLayer to change to: <")
    (princ NEW2)
    (setq NEW3 NEW2)
    (prompt "> ")
    (setq NEW2 (getstring))
   )
  )
  (if (= NEW2 "")
   (setq NEW2 NEW3)
  ) 
  (if (= NEW2 nil)
   (progn
    (setq NEW1 CURRENT)
    (prompt "\nLayer to change to: <")
    (princ NEW1)
    (prompt "> ")
    (setq NEW2 (getstring))
   )
  )
  (if (= NEW2 "")
   (setq NEW2 NEW1)
  ) 
 (setq HIGH (getvar "highlight"))
 (setvar "highlight" 0)
 (command "offset" DIST OBJ PT "")
 (command "change" "last" "" "P" "la" NEW2 "")
 (setvar "highlight" HIGH)
 (princ)
) 

