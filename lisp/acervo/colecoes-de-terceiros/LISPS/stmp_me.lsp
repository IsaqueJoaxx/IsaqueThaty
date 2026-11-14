;STMP_ME by Mark Evinger 73162,1676
;at Cowhey Gudmundson Leder, Ltd. Itasca, Illinois.
; modified 08-13-1993 to work full 24 hrs per day (didn't used to work
; between noon and 1:00 p.m.)
; modified 08-27-1993 to cover for ATTDIA variable
;=========================================================================
;Fm: Richard Halle [AVE TM] 73417,340
;=========================================================================
(defun listxref ( / data work first)
     (setq first t)                              ;set rewind flag
     (while (setq data (tblnext "block" first))  ;rewind 1st time thru
          (setq first nil)                       ;don't want to rewind anymore
          (if (= 68 (logand 68 (cdr (assoc 70 data))))  ;XREFed >AND< ref'd Block?
               (setq work (cons (cdr (assoc 2 data)) work));If "Yup", add name to list
          )
     )
     work                                            ;returns the list
)
;===========================================================================
;Fm: Mark Evinger 73162,1676 -> code modified from July 93 Cadalyst tip #881
(defun datetime ()
     (setq DATST (rtos (getvar "cdate") 2 16))
     (setq HRS (atoi (substr DATST 10 2)))
     (cond
          ((= HRS 00)
               (setq NHRS (itoa (+ HRS 12)))
               (setq XTR "a.m.")
          )
          ((< HRS 12)
               (setq NHRS (itoa HRS))
               (setq XTR "a.m.")
          )
          ((> HRS 12)
               (setq NHRS (itoa (- HRS 12)))
               (setq XTR "p.m.")
          )
          ((= HRS 12)
               (princ "Hey! It's lunchtime, stop working! <g>")
               (setq NHRS (itoa HRS))
               (setq XTR "p.m.")
          )
     )
     (setq MO (substr DATST 5 2))
     (cond
          ((= MO "01")
               (setq MONTH "January")
          )
          ((= MO "02")
               (setq MONTH "February")
          )
          ((= MO "03")
               (setq MONTH "March")
          )
          ((= MO "04")
               (setq MONTH "April")
          )
          ((= MO "05")
               (setq MONTH "May")
          )
          ((= MO "06")
               (setq MONTH "June")
          )
          ((= MO "07")
               (setq MONTH "July")
          )
          ((= MO "08")
               (setq MONTH "August")
          )
          ((= MO "09")
               (setq MONTH "September")
          )
          ((= MO "10")
               (setq MONTH "October")
          )
          ((= MO "11")
               (setq MONTH "November")
          )
          ((= MO "12")
               (setq MONTH "December")
          )
     ); end MO cond
     (setq DATE-STRING
          (strcat
               MONTH
               " "
               (substr DATST 7 2)","   ; day
               " "
               (substr DATST 1 4)      ; year
               " "
               NHRS                    ; hours
               ":"
               (substr DATST 12 2)     ; minutes
               ":"
               (substr DATST 14 2)     ; seconds
               " "
               XTR                     ; am/pm
          ); end strcat
     ); end setq date-string
); end defun datetime
;=========================================================================
(defun C:STMP_ME
     (/
          ip ips scaf sca rot rotn
          dwg data myxref xlist first ssa tdata tname ddata dname gdata gname
          xdata xname n BLOCK_NUMBER
     )
;_________________________________________________________________________
;
; Donald Pirl 71174,1113 Yamabe & Horn Engineering, Clovis, CA
; with appologies and due credit to:
;   Richard Henley 73260,2346 Scanlon and Associates, Albuquerque NM
;   Jason Osgood 73417,1756 Alacrity BBS (206) 746-0680, Bellevue WA
; Lisp Routine to Place Current Time, Date, Drawing Name and operator's
; initials at selected location(s) and rotation (entered or picked).
; STMP.DWG accompanying this file must be in the ACAD path.
; Suggestions:  insert STMP.DWG in your prototype drawing,
;               include the command C:STMP in menu or keyboard save macros.
;       To include operator's initials, put the line
;           set intl=XXX
;           (where XXX is the operator's initials)
;       in the autoexec.bat or acad.bat file
; Text will be L60 (.06") when plotting scale = stamp block insertion
; scale, but the routine can be easily modified to suit.
;__________________________________________________________________________
;/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\
;Revisions by ME... I wasn't happy with the way the time & date were
;       displayed in the original lisp (stmp.lsp), so I modified all
;       the things I needed to such that the time, date and  drawing
;       name are now displayed like this
;
;                       July 08, 1993 7:56:32 p.m.
;             Drawing: C:\DATA\ME1177\1177_P10.DWG
;               Xrefs: 1117.DWG 2436.DWG 2436A.DWG
;
;       The stmp.dwg has been modified so that all the attributes are
;       right-justified, and the block insertion point  is  the  text
;       insertion point of the drawing name. (Xrefs are not  normally
;       used in our drawings). This setup  allows  us  to  insert the
;       block so that it is right-justified in the  lower  right-hand
;       corner of our various border sheets.  The stmp_me.dwg will be
;       inserted on the current layer. One last addendum is the ever-
;       present menu modification code, use or discard as ya see fit!
;
;       [STAMP_ME]^C^C^C(IF (not c:stmp_me);(LOAD "stmp_me"));stmp_me;
;
;       Good luck with this program, it was fun to edit it! ME
;/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\
;==========================================================================
; Modifications by Mark Evinger 73162,1676 - with help from Richard Halle
;==========================================================================
     (setq DIALOG_BOX (getvar "ATTDIA")) ; save the current dialog box setting
     (setvar "ATTDIA" 0)                 ; turn dialog boxes off while running
     (datetime)
     (setq dt DATE-STRING)
     (setq dt2 "")
     (setq dwg
          (strcat "Drawing: " (strcase (getvar "dwgname"))".DWG")
     )
     (if (getenv "intl")
          (progn
               (setq init (strcase (getenv "intl")))
               (setq dwg
                    (strcat "Drawing: " (strcase (getvar "dwgname"))".DWG ("init")" )
               )
          ); end progn
     ); end if
     (setq myxref (listxref))
     (setq xlist (strcat "Xrefs: "))
     (if (= myxref nil) (setq xlist (strcat " ")))
     (mapcar
          '(lambda (l)
               (setq xlist (strcat xlist l ".DWG "))
          )
          myxref
     ) ; end of xref lister routine
     (if
          (setq ssa (ssget "X" '((0 . "INSERT") (2 . "STMP_ME")))) ;does the block exist?
          (progn
               (setq n (sslength ssa))
               (setq BLOCK_NUMBER n)
               (while (> n 0)
                    (setq n (- n 1)
                         tname (entnext (ssname ssa n))
                         tdata (entget tname)
                         dname (entnext tname)
                         ddata (entget dname)
                         gname (entnext dname)
                         gdata (entget gname)
                         xname (entnext gname)
                         xdata (entget xname)
                         tdata (subst (cons 1 dt2) (assoc 1 tdata) tdata)
                         ddata (subst (cons 1 dt) (assoc 1 ddata) ddata)
                         gdata (subst (cons 1 dwg) (assoc 1 gdata) gdata)
                         xdata (subst (cons 1 xlist) (assoc 1 xdata) xdata)
                    )
                    (entmod tdata)
                    (entmod ddata)
                    (entmod gdata)
                    (entmod xdata)
                    (entupd gname)
               ); end if stmp_me blk exists
               (if (/= BLOCK_NUMBER 1)
                    (progn
                         (setq text (rtos BLOCK_NUMBER 2 0)) ;sets text to # of blocks found in dwg
                         (princ (strcat "\n"text" Stamp blocks \"STMP_ME\" updated.")) ;reports # of blocks updated
                    ); end progn
                    (princ "\nStamp block \"STMP_ME\" updated.") ; only if 1 block found
               );end if
          )
          (progn
               (princ "\nSTMP_ME block not found - Now inserting.\n")
               (setq ips (getpoint "\nInsertion point: <0.0,0.0,0.0> "))
               (if (null ips) (setq ip (list 0.0 0.0 0.0)) (setq ip ips))
               (setq scaf (getreal "\nScale factor: <1.0> "))
               (if (null scaf) (setq sca "1.0") (setq sca scaf))
               (setq rotn (getangle ip "\nEnter Text Rotation: <0.0> "))
               (if (null rotn) (setq rot "0.0") (setq rot (angtos rotn 0 6)))
               (command ".INSERT" "STMP_ME"
               ip sca sca rot dt2 dt dwg xlist)
          )
     )
     (setvar "ATTDIA" DIALOG_BOX) ;return dialog box setting to original value
     (princ)
); End Of File
