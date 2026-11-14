PROGRAM 1

(defun C:REDEF (/ S PATH1 PATH2 FIL1 FIL2 DWGNAME TEMPNAME LETR)
;
; Multi-drawing redefinition macro...
;
; April, 1988 by Ken Segal
; Space Design Incorporated
; One Penn Square West, Suite 600
; 30 South 15th Street,
; Philadelphia, PA 19102
; (215) 981-1492
;
; Error trapping routine...closes files and prompts error in case of crash
;
(defun *ERROR* (s)
(setvar "CMDECHO" 1)
(close FIL1)
(close FIL2)
(princ s)
(terpri)
)
;
(textscr)
(setvar "CMDECHO" 0)
(princ"\nDRAWING REDEFINITION MACRO...")
(princ"\n\nTHIS ROUTINE LOADS EACH DRAWING IN A PARTICULAR")
(princ"\nSUBDIRECTORY, AND SAVES IT, WHICH HAS THE EFFECT")
(princ"\nOF AUTOMATICALLY UPDATING IT TO COMPATIBILITY")
(princ"\nWITH THE CURRENT VERSION OF AUTOCAD.")
(setq PATH1(getstring(strcat"\n\nWHAT IS THE PATH IN WHICH THE DRAWINGS"
  " ARE CURRENTLY SAVED?"
  "\nUSE FORM: C:" (chr 92) "ACAD" (chr 92) "SYMS" (chr 92) "\n")))
(setq PATH2(getstring(strcat"\n\nWHAT IS THE PATH IN WHICH THE DRAWINGS"
  " ARE TO BE SAVED?"
  "\nUSE FORM: C:" (chr 92) "ACAD" (chr 92) "NEWSYMS" (chr 92) "\n")))
(setq DELETE(getstring"\n\nDO YOU WISH TO DELETE THE ORIGINAL DRAWINGS?"))
(princ"\n\nCREATING LIST OF ALL ORIGINAL DRAWINGS...PLEASE WAIT...\n")
(command "SHELL" (strcat "DIR " PATH1 "*.DWG > DWG.DIR"))
;
; Directory of drawings is saved in text file "DWG.DIR"
;
(princ"\n\nCREATING SCRIPT FILE WHICH WILL GENERATE NEW VERSION DRAWINGS...")
(setq FIL1(open "DWG.DIR" "r"); open file "DWG.DIR" for reading in...
      FIL2(open "REDEF.SCR" "w")); open file "REDEF.SCR" for writing...
(while(setq DWGNAME(read-line FIL1)); read DWGNAME until end of file...
   (if(and(> (strlen DWGNAME) 0); if first character exists (not carriage ret)
         (not(equal(substr DWGNAME 1 1)" ")); if first character isn't blank,
      ); End and...
      (progn ; strip off name of drawing
      (setq TEMPNAME "" COUNT 1); start a new word with no letters
      (while(not(equal(setq LETR(substr DWGNAME COUNT 1))" ")); add letter
      (setq TEMPNAME(strcat TEMPNAME LETR); from DWGNAME untless space 
            COUNT(1+ COUNT)); move to next letter      
      ); End while...      
      (setq DWGNAME TEMPNAME)
;
;Write appropriate entry to script file
;
   (princ(strcat "\nADDING DRAWING " PATH1 DWGNAME ".DWG TO SCRIPT..."))
   (write-line(strcat "QUIT Y 2 " PATH1 DWGNAME) FIL2)
   (if(equal(strcase(substr DELETE 1 1))"Y")
      (write-line(strcat "SHELL DEL " PATH1 DWGNAME ".DWG") FIL2)
   ); If requested, write erasure instruction to script file
   (write-line(strcat "SAVE " PATH2 DWGNAME " Y") FIL2)
   ); End progn...took care of this DWGNAME
); End if...evaluated if this is a standard DWGNAME
); End while...finished entire list of DWGNAMEs...
(write-line "SHELL DEL REDEF.SCR" FIL2); write self-destruction command
(write-line "(princ(chr 7))" FIL2)
(write-line(strcat "(princ " (chr 34) "FINISHED!" (chr 34) ")") FIL2)
; write "finished" message to script file for prompting user...
(close FIL2)
(close FIL1)
(princ"\n")
(command "SHELL" "DEL DWG.DIR"); erase drawing directory file
(princ"\nABOUT TO EXECUTE REDEFINITION SCRIPT...\n")
(setvar "CMDECHO" 1)
(command "SCRIPT" "REDEF")
); End of routine...

.PA

PROGRAM 2

(defun C:EXP ()
;
;"NEW EXPLOSION ROUTINE" by KEN SEGAL
;THIS ROUTINE EXPLODES OBJECTS USING THE STANDARD "EXPLODE"
;COMMAND IF POSSIBLE. IN ADDITION, IT WILL SUCCESSFULLY
;"EXPLODE" A MIRRORED BLOCK
;
(defun *ERROR* (s)
(print s)
(setq *ERROR* nil)
(terpri)
)
(setq BLK(car(entsel "\nSELECT OBJECT TO EXPLODE\n")))
(if(not(equal(cdr(assoc 0(entget BLK)))"INSERT"))
(command "EXPLODE" BLK)
(progn(setq BLKNAME(cdr(assoc 2(entget BLK)))
      XPROP(cdr(assoc 41(entget BLK)))
      YPROP(cdr(assoc 42(entget BLK)))
      ZPROP(cdr(assoc 43(entget BLK)))
      INSPT(cdr(assoc 10(entget BLK)))
      ROTANG(atof(angtos(cdr(assoc 50(entget BLK)))
                 (getvar"aunits")(getvar"auprec"))))
         (if(null XPROP)(setq XPROP 1))
         (if(null YPROP)(setq YPROP 1))
         (if(null ZPROP)(setq ZPROP 1))
         (if(and(equal XPROP YPROP)(equal YPROP ZPROP)
                (equal XPROP ZPROP))
         (command "explode" BLK)
            (if(and(not(equal(substr BLKNAME 1 1) "*"))
               (equal (abs XPROP)(abs YPROP)))
         (progn
(princ"\nTHIS INSERTION IS A MIRRORED BLOCK! REDEFINING IT")
         (setvar "cmdecho" 0)
         (setq LAST(entlast))
         (command "erase" BLK ""
         "insert" (strcat "*" BLKNAME) INSPT (abs XPROP)"0")
         (setq INSGRP(ssadd)) 
         (while LAST
            (setq LAST(entnext LAST)) 
            (if LAST(ssadd LAST INSGRP))
         )
         (if (minusp XPROP)
         (command "mirror" INSGRP "" INSPT "@0,12" "Y"))
         (if (minusp YPROP)
         (command "mirror" INSGRP "" INSPT "@12,0" "Y"))
         (command "rotate" "p" ""INSPT ROTANG)
         (setvar "cmdecho" 1))))))
         (princ"\nDONE!")
         (print)
         )
(defun C:RBLOCK ()
;
;BLOCK REDEFINITION ROUTINE... REDEFINES SELECTED
;SUBENTITY INFO WITHIN ALL INSERTIONS OF A GIVEN BLOCK
;
(defun *ERROR* (s)
(print s)
(setq *ERROR* nil)
(terpri)
)

(setq BLK(car(entsel "\nSELECT BLOCK FOR REDEFINITION:\n")))
(if(not(equal(cdr(assoc 0(entget BLK)))"INSERT"))
(progn
   (princ "\nSORRY, THIS IS NOT A BLOCK")
   (terpri)
)
(progn
   (princ(strcat "\nYOU HAVE SELECTED BLOCK <" 
      (setq BLKNAME(cdr(assoc 2(entget BLK)))) ">"))
   (setq COLR(getstring (strcat "WHAT COLOR (NAME OR NUMBER)"
              " WOULD YOU LIKE FOR "
              "\nALL SUBENTITIES IN NEW BLOCK?")))
   (setvar "cmdecho" 0)
   (setq LAST(entlast))
   (command "insert" (strcat "*" BLKNAME) "0,0" "1" "0")
         (setq INSGRP(ssadd))
         (while LAST 
            (if LAST(command "change" LAST "" "P" "COLOR" COLR ""))
            (if LAST(ssadd LAST INSGRP)) 
         )
         (command "block" BLKNAME "y" "0,0" INSGRP "")
         (setvar "cmdecho" 1)
)
)         

.PA


PROGRAM 3

(defun C:3DCURV (/ nparse cmdmode amin amax ainc
bmin bmax binc ac
ount pt1 pt2 pt3 pt4 bcount)
;
; Function to create 3D curves from AutoLISP expressions
;
; This version is a modification to GENCURV.LSP,
; first published in CADENCE magazine, Feb. 1988
;
; Copyright 1988 Ken Segal
; All Rights Reserved
; 125 South 21st Street, #2F
; Philadelphia, PA 19103
; (215) LOG-NINE
;
; ERROR TRAPPING ROUTINE
;
(defun *ERROR* (s)
(princ s)
(setq *ERROR* nil)
(terpri)
)
;
; If an equation isn't stored yet, set it to blank string
;
(if(null parse)(setq parse ""))
;
; Prompt user for their equation...print the previous
; equation if it exists. If they press carriage return
; without entering a new equation, re-use the previous
; one...
;
(textscr)
(if(/=(setq nparse(getstring(strcat "\n\nLAST CURVE: Z = "
   parse "\nNEW CURVE: Z = "))) "")
      (setq parse nparse)
)
;
; If the current expression isn't blank, process the 3D
; curve...
;
(if(not(equal parse ""))
(progn
;
; Expression parser...solves the expression "parse" for the
; values of x and y
;
(defun equate (x y)
   (eval (read parse))
)
;
; Input x and y values and "stepping" increment for the
; 3d model
;
(setq CMDMODE(getvar "cmdecho")
AMIN(getreal "\nWhat is lower limit for X?")
AMAX(getreal "\nWhat is upper limit for X?")
AINC(getreal "\nWhat is increment for X?")
BMIN(getreal "\nWhat is lower limit for Y?")
BMAX(getreal "\nWhat is upper limit for Y?")
BINC(getreal "\nWhat is increment for Y?")
ACOUNT AMIN)
(setvar "cmdecho" 0)
(princ"\n\nDRAWING CURVE...PLEASE WAIT...")
;
; Choose an initial viewpoint so user can see the curve
; generate
;
(command "vpoint" "1,1,1" "3dline" 
(list AMIN BMIN 0.0) (list AMAX BMAX 0.0) ""
"zoom" "e" "erase" "l" "")
;
; This is the critical part of the routine!
;
; Loop through x and y values, placing each in the user's
; equation, evaluating for nearby endpoints and creating
; 3DFACES for curve
;
(while(<= ACOUNT AMAX)
(setq BCOUNT BMIN)
(while(<= BCOUNT BMAX)
(setq PT1(list ACOUNT BCOUNT
   (equate ACOUNT BCOUNT))
PT2(list (+ ACOUNT AINC) BCOUNT 
   (equate (+ ACOUNT AINC) BCOUNT))
PT3(list (+ ACOUNT AINC) (+ BCOUNT BINC)
   (equate (+ ACOUNT AINC) (+ BCOUNT BINC)))
PT4(list ACOUNT (+ BCOUNT BINC)
   (equate ACOUNT (+ BCOUNT BINC))))
(command "3DFACE"PT1 PT2 PT3 PT4 "")
(setq BCOUNT (+ BCOUNT BINC))
)
(setq ACOUNT (+ ACOUNT AINC))
)
;
; Curve is finished...zoom out and reset command echoing
;
(command "zoom" "e")
(setvar "cmdecho" CMDMODE )
); End progn
(princ"\nSORRY...EQUATION MUST BE DEFINED TO BE MAPPED!\n")
); End if...equation exists
(princ"\n\n\7FINISHED!")
(terpri)
)



)
                                                                                                                             