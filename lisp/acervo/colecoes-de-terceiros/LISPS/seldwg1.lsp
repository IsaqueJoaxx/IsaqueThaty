;SELDWG
;
; This program reads the file SELDWG.TMP and file ???.DAT
; SELDWG.TMP has only one line of code. The file name of the ???.DAT file.
;
; ???.DAT contains the data base information created when SELDWG.EXE or
; NEWDWG.EXE programs are used to start a new drawing with title block
; information.
;
; Ln? reads line number ? of the DAT file.
;
; (Setq L?2 (SubStr Ln? 19)) reads from the ninteenth character of line ? to
; end of line.
;
; You should comment out any lines you do not require and add any read-line
; for lines you do.
;
; Also near the end of this program you will find the command to "INSERT"
; you should change the areas starting with "Your Company.....
; to your company name and address, etc.  If you do not require this
; information then replace "Your Company..." with "".
;
(Defun C:SELDWG (/ a a1 InF f Ln1 Ln2 Ln3 Ln4 Ln5 Ln6 Ln7 Ln8 Ln9 Ln10 Ln11
Ln12 Ln13 Ln14 Ln15 Ln16 Ln16a Ln16b Ln17 Ln18 Ln19 Ln20 Ln21 Ln22 Ln23)
(Setq a (Open "SELDWG.TMP" "r"))
(Setq a1 (Read-Line a))
(Close a)
(Setq Dname (GetVar "DWGNAME"))
;(Setq InF (StrCat Dname ".DAT"))
(Setq InF (StrCat a1))
(Setq f (Open InF "r"))
(Setq Ln1 (Read-Line f))
(Setq Ln2 (Read-Line f))
(Setq Ln3 (Read-Line f))
(Setq Ln4 (Read-Line f))
(Setq Ln5 (Read-Line f))
(Setq Ln6 (Read-Line f))
(Setq Ln7 (Read-Line f))
(Setq Ln8 (Read-Line f))
(Setq Ln9 (Read-Line f))
(Setq Ln10 (Read-Line f))
(Setq Ln11 (Read-Line f))
(Setq Ln12 (Read-Line f))
(Setq Ln13 (Read-Line f))
(Setq Ln14 (Read-Line f))
(Setq Ln15 (Read-Line f))
(Setq Ln16 (Read-Line f))
(Setq Ln17 (Read-Line f))
(Setq Ln18 (Read-Line f))
(Setq Ln19 (Read-Line f))
(Setq Ln20 (Read-Line f))
(Setq Ln21 (Read-Line f))
(Setq Ln22 (Read-Line f))
(Setq Ln23 (Read-Line f))
(Close f)
(Setq Ln1 (SubStr Ln1 19))
(Setq Ln2 (SubStr Ln2 19))
(Setq Ln3 (SubStr Ln3 19))
(Setq Ln4 (SubStr Ln4 19))
(Setq Ln5 (SubStr Ln5 19))
(Setq Ln6 (SubStr Ln6 19))
(Setq Ln7 (SubStr Ln7 19))
(Setq Ln8 (SubStr Ln8 19))
(Setq Ln9 (SubStr Ln9 19))
(Setq Ln10 (SubStr Ln10 19))
(Setq Ln11 (SubStr Ln11 19))
(Setq Ln12 (SubStr Ln12 19))
(Setq Ln13 (SubStr Ln13 19))
(Setq Ln14 (SubStr Ln14 19))
(Setq Ln15 (SubStr Ln15 19))
(Setq Ln16a (SubStr Ln16 19 3))
(Setq Ln16b (SubStr Ln16 23))
(Setq Ln17 (SubStr Ln17 19))
(Setq Ln18 (SubStr Ln18 19))
(Setq Ln19 (SubStr Ln19 19))
(Setq Ln20 (SubStr Ln20 19))
(Setq Ln21 (SubStr Ln21 19))
(Setq Ln22 (SubStr Ln22 19))
(Setq Ln23 (SubStr Ln23 19))
(Command "SETVAR" "LUNITS" Ln13)
(Command "SETVAR" "DIMSCALE" Ln15)
(Command "LIMITS" Ln16a Ln16b)
(Command "INSERT" Ln11 "0,0" Ln15 Ln15 "0" "YOUR COMPANY NAME" "Your Company Address"
"Your Company City state zip phone" Ln1 Ln2 Ln3 Ln18 Ln19 Ln20 Ln4 Ln8 Ln9
Ln12 Ln21 Ln5 "" Ln17 "")
(Command "ZOOM" "E")
)


