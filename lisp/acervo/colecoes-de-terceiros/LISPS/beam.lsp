
;;;BEAM1.LSP
;;;==============================================================
;;;=                   VERSION 2.0                              =
;;;=                                                            =
;;;=                  BY D.E.CARLILE                            =
;;;=================RELEASED==NOV.11==1991=======================
;;;
;;;      THIS PROGRAM TOOK MANY HOURS TO COMPLETE SO PLEASE REGISTER!
;;;
;;;  IF YOU USE THIS AND LIKE IT 
;;;   PLEASE REGISTER BY SENDING $5 TO :
;;;                                    2110 NEWPORT LANDING
;;;                                    ALPHARETTA, GA 30201
;;;   THIS MONEY WILL HELP SUPPORT ON GONING LISP WRITINGS
;;;   AND WILL ALSO INSURE YOU OF FUTURE UPDATES!
;;;______________________________________________________________
;;;  
;;;  FOR FURTHER INFORMATION ON UPDATES PLEASE WRITE TO ABOVE ADDRESS
;;;______________________________________________________________
;;;______________________________________________________________
;;;               TO START TYPE BEAM OR BM
;;;
;;;    THIS PROGM WILL DRAW A STEEL BEAM BASED ON SIMALAR
;;;    DIMENSIONS AS FOUND IN THE A.I.S.C. STEEL HANDBOOK
;;;
;;;    FOR BEST RESULTS FOR CUSTOMIZING MENUS PUT THIS IN YOUR .MNU
;;;    FILE:
;;;        [BEAM]^C^C^P(IF (NOT C:BEAM) (IF (NOT (OR (SETQ AS +
;;;        (FINDFILE "SUBDIR/BEAM.LSP")) (SETQ AS (FINDFILR "BEAM.LSP"))))+
;;;        (DEFUN C:BEAM () (PRINC "CANNOT FIND BEAM.LSP ") (TERPRI);+
;;;        (PRINC "IT IS IN D:SUPPORT DISK.  ")(SETQ C:BEAM NIL)(PRINC))+
;;;        (PROGN(TERPRI)(PRINC (STRCAT "LOADING " AS " -- PLEASE WAIT...  "))+
;;;        (LOAD AS))))(PRINC) BEAM ^P
;;;---------------------------------------------------------------------------
(defun myerror (s)
(if (/= s "function cancelled ")(princ (strcat "\nError:  " s)))(setvar "cmdecho" ocmd)
(SETVAR "PICKBOX" PICK)(setvar "orthomode" orth)(setq *error* olderr)(princ))

(DEFUN angles ()
(setq pt1 (getpoint (STRCAT "\AnglePoint of this angle :  ")))
(SETQ H (GETDIST "\nPlease ENTER the <hieght> of the Angle  :  "))
(SETQ W (GETDIST "\nPlease ENTER the <WIDTH> of the Angle :  "))
(SETQ TH (GETDIST "\nPlease ENTER the <THICKNESS> of the Angle :  "))
(setq pt2 (list (car pt1) (+ (cadr pt1) H) 0.0))(setq pt3 (list (+ (car pt2) TH) (cadr pt2) 0.0))
(setq pt4 (list (car pt3) (+ (cadr pt1) TH) 0.0))(setq pt5 (list (+ (- w th) (car pt4)) (cadr pt4) 0.0))
(setq pt6 (list (car pt5) (cadr pt1) 0.0))(COMMAND "pLINE" PT2 PT1 PT6 "")
(command "line" pt2 PT3 PT4 "")(COMMAND "FILLET" "R" (/ th 3))(COMMAND "FILLET" PT2 pt4)
(command "line" pt6 PT5 PT4 "")(COMMAND "FILLET" "R" (/ th 3))(COMMAND "FILLET" PT6 pt4)
(SETQ PT7 (LIST (car pt4) (+ (cadr pt4) (/ h 8)) 0.0))(setq pt8 (list (+ (car pt4) (/ h 8)) (cadr pt4) 0.0))
(command "fillet" "r" (/ th 2))(command "fillet" pt8 pt7)(SETQ L NIL))

(DEFUN WFBEAM ()
(setq pt1 (getpoint (STRCAT "\MidPoint of WFbeam:  ")))
(SETQ D (GETDIST "\nPlease ENTER the <D> DEPTH of the WFbeam  :  "))
(setq Tw (getdist "\nPlease ENTER THE <TW> THICKNESS of the WFbeam :  "))
(SETQ BF (GETDIST "\nPlease ENTER the <BF> WIDTH of the WFbeam :  "))
(SETQ Tf (GETDIST "\nPlease ENTER the <Tf> THICKNESS of the WFbeam :  "))
(SETQ TW2 (/ TW 2))(SETQ PA (+ TW2 (CAR PT1)))(SETQ PT2 (LIST PA (CADR PT1) 0.0))
(SETQ D2 (/ D 2))(SETQ PB (+ D2 (CADR PT1)))(SETQ PT3 (LIST (CAR PT1) PB 0.0))
(SETQ BF2 (/ BF 2))(SETQ PC (+ BF2 (CAR PT3)))(SETQ PT4 (LIST PC (CADR PT3) 0.0))
(SETQ PD (- (CADR PT4) TF))(SETQ PT5 (LIST (CAR PT4) PD 0.0))(SETQ PT6 (LIST (CAR PT2) (CADR PT5) 0.0))(SETQ RR2 (LIST (+ (CAR PT6) TW) (CADR PT6) 0.0))
(COMMAND "pLINE" PT3 PT4 PT5 "")(command "pline" pt5 PT6 PT2 "")
(COMMAND "FILLET" "R" TW2)(COMMAND "FILLET" "p" PT2)(COMMAND "MIRROR" pt2 PT3 "" PT3 PT1 "")(SETQ PT7 (LIST (- (CAR PT4) BF) (CADR PT4) 0.0))
(setq pt8 (list (- (car pt2) tw) (cadr pt2) 0.0))(COMMAND "MIRROR" "P" PT7 PT8 "" PT1 PT2 "")
(SETQ L NIL))
(DEFUN channels ()
(setq pt1 (getpoint (STRCAT "\MidPoint of the Channel:  ")))
(SETQ D (GETDIST "\nPlease ENTER the <D> DEPTH of the Channel  :  "))
(setq Tw (getdist "\nPlease ENTER THE <TW> THICKNESS of the Channel :  "))
(SETQ BF (GETDIST "\nPlease ENTER the <BF> WIDTH of the Channel :  "))
(SETQ Tf (GETDIST "\nPlease ENTER the <Tf> THICKNESS of the Channel :  "))
(SETQ TW2 (/ TW 2))(SETQ PA (+ TW2 (CAR PT1)))(SETQ PT2 (LIST PA (CADR PT1) 0.0))
(SETQ D2 (/ D 2))(SETQ PB (+ D2 (CADR PT1)))(SETQ PT3 (LIST (CAR PT1) PB 0.0))
(SETQ BF2 (/ BF 2))(SETQ PC (+ BF2 (CAR PT3)))(SETQ PT4 (LIST PC (CADR PT3) 0.0))
(SETQ PD (- (CADR PT4) TF))(SETQ PT5 (LIST (CAR PT4) PD 0.0))(SETQ PT6 (LIST (CAR PT2) (- (cadr pt5) (/ (+ tw tw) 4)) 0.0))
(SETQ RR2 (LIST (+ (CAR PT6) TW) (CADR PT6) 0.0))(COMMAND "pLINE" pt1 PT3 PT4 PT5 "")
(command "pline" pt5 PT6 PT2 "")(COMMAND "FILLET" "R" TW2)(COMMAND "FILLET" "p" PT2)
(COMMAND "MIRROR" pt2 PT3 "" PT2 PT1 "")(SETQ L NIL))
(DEFUN TEES ()
(setq pt1 (getpoint (STRCAT "\PICK the Bottom MidPoint of Tee:  ")))
(SETQ D (GETDIST "\nPlease ENTER the <D> DEPTH of the Tee :  "))
(setq Tw (getdist "\nPlease ENTER THE <TW> THICKNESS of the Tee :  "))
(SETQ BF (GETDIST "\nPlease ENTER the <BF> WIDTH of the Tee :  "))
(SETQ Tf (GETDIST "\nPlease ENTER the <Tf> THICKNESS of the Tee :  "))
(SETQ TW2 (/ TW 2))(SETQ PA (+ TW2 (CAR PT1)))(SETQ PT2 (LIST PA (CADR PT1) 0.0))
;(SETQ D2 (/ D 2))(SETQ PB (+ D2 (CADR PT1)))(setq pb (+ D (cadr pt1)))
(SETQ PT3 (LIST (CAR PT1) PB 0.0))(SETQ BF2 (/ BF 2))(SETQ PC (+ BF2 (CAR PT3)))
(SETQ PT4 (LIST PC (CADR PT3) 0.0))(SETQ PD (- (CADR PT4) TF))
(SETQ PT5 (LIST (CAR PT4) PD 0.0))(SETQ PT6 (LIST (CAR PT2) (CADR PT5) 0.0))
(SETQ RR2 (LIST (+ (CAR PT6) TW) (CADR PT6) 0.0))
(COMMAND "pLINE" PT3 PT4 PT5 "")(command "pline" pt5 PT6 PT2 "")
(command "pline" pt1 pt2 "")(COMMAND "FILLET" "R" TW2)(COMMAND "FILLET" "p" RR2)
(COMMAND "MIRROR" RR2 PT3 pt1 "" PT3 PT1 "")(SETQ L NIL))
(DEFUN C:BEAM ()
(setq olderr *error*)(setq *error* myerror)(setq ocmd (getvar "cmdecho"))
(SETQ PICK (GETVAR "PICKBOX"))(setq orth (getvar "orthomode"))(setvar "cmdecho" 0)
(SETVAR "PICKBOX" 1)(setvar "orthomode" 1)(SETQ L T)
(WHILE L
(PRINC "\nPLEASE SELECT WHICH TYPE OF STEEL YOU WILL BE DRAWING... ")
(SETQ TEST (STRCASE (GETSTRING "\n  <A>ngles wf<B>eams or <T>ees <C>hannels  ")))
(IF (EQUAL TEST "B") (WFBEAM))(IF (EQUAL TEST "T") (TEES))
(IF (EQUAL TEST "C") (channels))(IF (EQUAL TEST "A") (ANGLES))
(SETVAR "CMDECHO" OCMD)(SETVAR "PICKBOX" PICK)(SETVAR "ORTHOMODE" ORTH)
(setq *error* olderr))
(princ "\n ........................**WARNING**............................ ")
(PRINC "\n ......PLEASE REGISTER THIS VERSION OF BEAM WITHIN 30 DAYS.......")
(PRINC))
(defun C:BM() (C:BEAM))
(princ "\nSTEEL BEAM VERSION 2.0 by D.E.Carlile 1991....is loaded! ")
(princ "\nTo Start Type....BM    or...BEAM  ")
(princ)