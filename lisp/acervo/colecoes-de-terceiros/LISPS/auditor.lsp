; =========================================================
;   AUDITOR.LSP              By:   Jeff Pilcher
;
;   Copywrite 1988 Jeff Pilcher, Production Systems
;                All rights reserved
;
;   This is an AutoLISP routine for use with the
;   AutoCAD drafting software.  
;
;
;   THIS ROUTINE WILL ONLY WORK WITH RELEASE 10.
;
;
;   I am providing this routine to other users of
;   AutoCAD under the SHAREWARE concept. I would
;   like to recover some of the cost of my time
;   spent developing this and other AutoLISP
;   routines for AutoCAD, by asking for voluntary
;   payments.
;
;   If you find this routine is useful to you in
;   doing paying work, please send $15 to:
;
;              Jeffrey S. Pilcher
;
;              Production Systems
;              43373 Mildred Ave.
;              Antioch, IL  60002
;
;              (312) 395-7071
;
;   Obviously $15 is not going to make me rich, or break
;   your bank account.  It will however, show me that
;   someone out there appreciates my efforts, and give
;   me the incentive to write more useful routines.
;
;   Feel free to pass this routine on to other AutoCAD
;   users, but PLEASE, do not remove these comments.
;
;   Good luck, have fun, and LONG LIVE AutoDESK !!!
;
; =========================================================
;
;   Auditor.lsp is a routine to keep track of your time
;   working on drawings in the AutoCAD drawing editor.
;
;   All you have to do to use the Auditor is create an
;   ACAD.LSP file with the following lines of code or add
;   the S::STARTUP function to your existing ACAD.LSP file.
;
;          (defun S::STARTUP ()
;            (load "AUDITOR")
;          )
;
;   If you already have a S::STARTUP function in your ACAD.LSP
;   file, then just add the line
;
;             (load "AUDITOR")
;
;   to your S::STARTUP function, similiar to the example above.
;
;   That's all there is to it!!
;
;   After you edit a drawing or two, look in your directory
;   for a file AUDITOR.LOG.  This file will contain information
;   about the time you have spent working on your drawings.
;
;   The file will look something like this:
;
;   _______________________________________________________________________
;
;   Date edited:     10/22/88               Drawing file name:  TEST
;   Session started: 21:43:37
;   Session ended:   21:53:13               Editing time:       0.16 hours.
;
;   Exited drawing using: END
;
;   Drawing was saved  4  times.
;  _______________________________________________________________________
;
;
; =========================================================

(setvar "cmdecho" 0)
(command "undefine" "quit"
         "undefine" "end"
         "undefine" "save"
)
(setq savcnt 0)
(defun c:END ()
  (setq cm "END")
  (LOG)
  (command ".END")
  (princ)
)
(defun c:QUIT ()
  (setq cm "QUIT")
  (LOG)
  (command ".QUIT")
  (princ)
)
(defun c:SAVE ()
  (setq sav "T")
  (setq savcnt (+ savcnt 1))
  (command ".SAVE")
  (princ)
)
(defun GETDATE ( / CD)
    (setq CD (rtos (getvar "CDATE") 2 6))
    (strcat (substr CD 5 2) "/" (substr CD 7 2) "/" (substr CD 3 2))
)
(defun GETTIME ( / CD)
    (setq CD (rtos (getvar "CDATE") 2 6))
    (strcat (substr CD 10 2) ":" (substr CD 12 2) ":" (substr CD 14 2))
)
(setq DWGSTART (getvar "DATE"))
(setq start (gettime))
(defun LOG ()
  (setq timein (rtos (/ (* (- (getvar "DATE") DWGSTART) 1440.0) 60.0) 2 2))
  (setq timefile (open "AUDITOR.LOG" "a"))
  (write-line "___________________________________________________________________________" timefile)
  (write-line "" timefile)
  (write-line (strcat "  Date edited:     " (getdate)
    "               Drawing file name:  " (getvar "dwgname")) timefile)
  (write-line (strcat "  Session started: " start) timefile)
  (write-line (strcat "  Session ended:   " (gettime)
    "               Editing time:       " timein " hours.   ") timefile)
  (write-line "" timefile)
  (write-line (strcat "  Exited drawing using: " (strcase cm)) timefile)
  (write-line "" timefile)
  (if sav 
    (write-line (strcat "  Drawing was saved  " (rtos savcnt 2 0) "  times.") timefile)
    (write-line "  Drawing was not saved during this editing session." timefile)
  )
  (close timefile)
  (princ)
)
(princ)