; WELCOME.LSP - a sample program for showing WELCOME.DCL
; copyright (c) 1993, Mark Middlebrook and CADalyst

(defun C:WELCOME ( / dcl_id resp)
 ;*Load .DCL file and select dialogue ---------
 (setq dcl_id (load_dialog "WELCOME.DCL"))
 (if (not (new_dialog "welcome" dcl_id)) (exit))
 ;*Establish action expressions ---------------
 (action_tile "response" "(setq resp $value)" )
 (action_tile "accept" "(done_dialog 1)" )
 ;*Display dialogue on screen------------------
 (start_dialog)
 ;*Unload .DCL file from memory----------------
 (unload_dialog dcl_id)
 (princ "\nYou responded with: ") (princ resp)
 (princ)
)
