
; LaunchCAD Autolisp program
; If you do not have an ACAD.LSP file then rename this file to ACAD.LSP
; or add to your present ACAD.LSP file.

; Note: You may edit this lisp file to rename the following functions
; to any name of your liking so long as it does not conflict with an
; AutoCAD internal command (unless you undefine it in S::STARTUP).

(defun lc_shell (mode dt / ce dir)		; call LaunchCAD in shell mode
  (setq ce (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)                          ; don't echo commands
  (if (= dt "s")
    (setq dir (getvar "ACADPREFIX"))
    (setq dir (getvar "DWGPREFIX"))
  )
  (if (null dir) (setq dir "."))
; (command "sh" "mode co80")                    ; decomment for dual screen
  (command "launchcad" (strcat dir " " mode))   ; invoke LaunchCAD
; (command "sh" "mode mono")                    ; decomment for dual screen
; (graphscr)					;    "           "
  (command "script" "lc")                       ; run the script
  (setvar "CMDECHO" ce)                         ; restore CMDECHO
  (princ)
)

; Execute LaunchCAD from within AutoCAD (in shell mode)
; you can rename lc to anything you like

(defun c:lc ()	(lc_shell "shell" "d"))

; Insert Mode
(defun c:ins ()  (lc_shell "insert" "d"))

; Lisp Mode
(defun c:lisp ()  (lc_shell "lisp" "s"))

; DXFIN Mode
(defun c:dxf ()  (lc_shell "dxfin" "s"))

; DXBIN Mode
(defun c:dxb ()  (lc_shell "dxbin" "s"))

; VSlide Mode
(defun c:vs ()	(lc_shell "vslide" "s"))

; Menu Mode
(defun c:mu ()	(lc_shell "menu" "s"))

; File Mode
(defun fi ()
  (command "launchcad" ". file" )        ; invoke LaunchCAD
  (princ)
)

; Redefine the QUIT command to run quit.scr in order to
; bypass the AutoCAD opening menu.

(defun c:quit ()
  (setvar "CMDECHO" 0)
  (if
    (= (strcase (getstring
	 "\nDo you really want to discard\nall changes to drawing (Y/N):")) "Y")
    (command "script" "quit")           
  ) ; end if
  (princ)
)

; Redefine the END command to run end.scr in order to
; bypass the AutoCAD opening menu.

(defun c:end ()
  (setvar "CMDECHO" 0)
  (command "script" "end")              ; run end.scr which bypasses menu
  (princ)
)

(defun c:wend ()
  (setvar "CMDECHO" 0)
  (command "script" "wend")              ; run wend.scr which saves drawing
  (princ)				 ; using wblock * and bypasses menu
)


; Undefine the AutoCAD drawing editor END and QUIT commands so that
; the end and quit functions defined above will work in place of the
; internal commands (.end and .quit will still work like normal)
; This function will execute automatically with release 10 only if
; you use earlier versions then rename s::startup to c:startup
; and execute it as the "Initial Command" from LaunchCAD
; specify "startup" as the initial command and saVe your configuration
; this command will remain as the default in LaunchCAD unless changed.

(defun s::startup ()
  (setvar "cmdecho" 0)
  (command "undefine" "quit")
  (command "undefine" "end")
  (princ "\nEND and QUIT commands redefined...")
  (princ)
)

(princ "\nLaunchCAD loaded...")
(princ)

