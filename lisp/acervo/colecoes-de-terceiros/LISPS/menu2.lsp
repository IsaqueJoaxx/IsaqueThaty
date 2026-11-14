;;; -*-  Mode: LISP -*- Syntax: AutoLISP (C) Benjamin Olasov 1988
;;;      text screen menu function for ANSI graphics standard

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; File: MENU2.LSP Copyright (C) Benjamin Olasov 1988 All Rights Reserved  ;;;
;;; Research/ commercial inquiries:                                         ;;;
;;;       Benjamin Olasov  310 Riverside Drive    New York, NY  10025       ;;;
;;;                        PH (212) 678-5473                                ;;;
;;;                        Network Mailstop: G.OLASOV@CS.COLUMBIA.EDU       ;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;; This function creates menus in text screen mode for AutoLISP.
;; It assumes an 80 column textscreen monitor and ANSI.SYS graphics device.
;; MENU-OPERATION looks for and returns an integer.

;; In this version, the header, prompt and individual items in the item-list
;; MUST all be strings, that is, surrounded by double quotes. ex.: "STRING"

(TEXTSCR)

(VMON)

(GC)

(EXPAND 3)

;;length of longest string in a list of strings
(DEFUN LONGEST (LST)
       (APPLY 'MAX (MAPCAR '(LAMBDA (ITM) (STRLEN ITM)) LST)))

(DEFUN BLANK (COL WIDTH)
       (REPEAT COL (PRINC " "))
       (PRINC (CHR 186))
       (REPEAT (- WIDTH 2) (PRINC " "))
       (PRINC (CHR 186))
       (TERPRI) (REPEAT L-COL (PRINC " ")))

(DEFUN MENU-OPERATION (HEADER ITEM-LIST PROMPT / HEIGHT WIDTH COUNTER L-COL)
       (TEXTSCR)
       (PRINC "\e[2J")
       (IF (/= (REM (STRLEN HEADER) 2) 0) (SETQ HEADER (STRCAT HEADER " ")))
       (SETQ HEIGHT (+ 9 (LENGTH ITEM-LIST))
             WIDTH (+ 10 (MAX (LONGEST ITEM-LIST) (STRLEN HEADER))))
       (IF (/= (REM HEIGHT 2) 0) (SETQ HEIGHT (1+ HEIGHT)))
       (IF (/= (REM WIDTH 2) 0) (SETQ WIDTH (1+ WIDTH)))
       (SETQ L-COL (- 40 (/ WIDTH 2))
             COUNTER 0)
       (REPEAT (- 12 (/ HEIGHT 2)) (TERPRI))
       (REPEAT L-COL (PRINC " "))
       (PRINC (CHR 201))
       (REPEAT (- WIDTH 2) (PRINC (CHR 205)))
       (PRINC (CHR 187)) (TERPRI)
       (BLANK L-COL WIDTH)
       (PRINC (CHR 186))
       (REPEAT (- (- (/ WIDTH 2) (/ (STRLEN HEADER) 2)) 1) (PRINC " "))
       (PRINC HEADER)
       (REPEAT (- (- (/ WIDTH 2) (/ (STRLEN HEADER) 2)) 1) (PRINC " "))
       (PRINC (CHR 186)) (TERPRI)
       (BLANK L-COL WIDTH)
       (PRINC (CHR 204))
       (REPEAT (- WIDTH 2) (PRINC (CHR 205)))
       (PRINC (CHR 185)) (TERPRI)
       (REPEAT L-COL (PRINC " "))
       (PRINC (CHR 186))
       (REPEAT (- WIDTH 2) (PRINC " "))
       (PRINC (CHR 186))
       (FOREACH ITEM ITEM-LIST
                (SETQ COUNTER (1+ COUNTER))
                (TERPRI)
                (REPEAT L-COL (PRINC " "))
                (PRINC (STRCAT (CHR 186) "  " (RTOS (FLOAT COUNTER) 2 0) "] " ITEM))
                (REPEAT (- WIDTH (+ 6 (STRLEN (RTOS (FLOAT COUNTER) 2 0))
                             (STRLEN ITEM)))
                             (PRINC " "))
                (PRINC (CHR 186)))
       (TERPRI)
       (BLANK L-COL WIDTH)
       (PRINC (CHR 200))
       (REPEAT (- WIDTH 2) (PRINC (CHR 205)))
       (PRINC (CHR 188))
       (PRINC (STRCAT "\n\n" PROMPT))
       (SETQ CHOICE (GETINT))
       (WHILE (OR (< CHOICE 1) (> CHOICE (LENGTH ITEM-LIST)))
              (SETQ CHOICE (GETINT "\nChoice is out of range, try again: ")))
       (PRINC "\e[2J") CHOICE)

(princ "\nMENU-OPERATION")

;; This an an example of using MENU-OPERATION to get a value from the user.
;; The first argument must be the header.
;; The second argument must be a list of things to be chosen from.
;; The third argument must be a prompt [question] to the user.
;; MENU-OPERATION looks for and returns an integer.

(defun c:test ()
       (setq woodtype 
          (menu-operation "WOOD MENU"
               '("Cedar, western red"
                 "Cedar, northern or southern white"
                 "Cypress, southern"
                 "Douglas fir, western"
                 "Douglas fir, Rocky mountain region"
                 "Fir, balsam"
                 "Fir, golden"
                 "Hemlock, eastern"
                 "Larch, western"
                 "Oak, commerical white or red"
                 "Pine, southern yellow"
                 "Pine, California, midwestern, or northern"
                 "Redwood"
                 "Spruce, Engelemann"
                 "Tamarack, eastern")
                 "Select number corresponding to type of wood to be used: ")))

;;                           ** NEWS BULLETIN **
;;    
;;    New CAD/AutoCAD section on New York BBS Service Bulletin Board
;;    
;;    Discussions, news and software relevant to CAD, especially AutoCAD,
;;    are supported.  The primary aim of this section is to be a resource 
;;    for AutoLISP (and LISP) applications developers generally, with some
;;    emphasis on applying knowledge representation techniques in AutoCAD's
;;    LISP environment.  However, other forms of innovation, such as new 
;;    pattern files, text fonts- clever and useful applications generally- 
;;    are strongly encouraged.  Also, we very much need drawing libraries 
;;    such as steel sections, etc. 
;;
;;    The dial-up number is (212) 980-0770.
;;
;;    At this stage, our library of CAD applications is still far too light.
;;    We need more uploads from callers, especially LISP routines.
;;    
;;    Direct technical questions/ comments to one of the following addresses:
;;    
;;    MCI-MAIL ID 344-4003
;;    CompuServe ID 71450,3313

(princ "\e[2J")
(princ "\nThis menu system is written for the ANSI graphics standard.")
(princ "\nType TEST to try a sample menu.")
(princ)

