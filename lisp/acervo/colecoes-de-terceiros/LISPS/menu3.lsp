;;; -*-  Mode: LISP -*- Syntax: AutoLISP (C) Benjamin Olasov 1988, 1989

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; File: MENU.LSP Copyright (C) Benjamin Olasov    Graphic Systems, Inc.   ;;;
;;; Inquiries:                                                              ;;;
;;;                                                                         ;;;
;;;     Benjamin Olasov                                                     ;;;
;;;     Graphic Systems, Inc.:                                              ;;;
;;;                                                                         ;;;
;;;                    New York, NY:   PH (212) 725-4617                    ;;;
;;;                    MCI-Mail:       GSI-NY   344-4003                    ;;;
;;;                    Arpanet:        olasov@cs.columbia.edu               ;;;
;;;                                                                         ;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;; This program is provided 'as is' without warranty of any kind, either 
;; expressed or implied, including, but not limited to the implied warranties of
;; merchantability and fitness for a particular purpose.  The entire risk as to
;; the quality and performance of the program is with the user.  Should the 
;; program prove defective, the user assumes the entire cost of all necessary 
;; servicing, repair or correction. 
;; AutoLisp and AutoCad are registered trademarks of AutoDesk, Inc.


;; This function creates menus in text screen mode for AutoLISP.
;; It assumes an 80 column textscreen monitor and ANSI.SYS graphics device
;; MENU-OPERATION looks for and returns an integer.

;; In this version, the header, prompt and individual items in the item-list
;; MUST all be strings, that is, surrounded by double quotes. ex.: "STRING"
;; The syntax is: 
;;
;; (menu-operation "header" '("item-1" "item-2" ... "item-n") "prompt")

(TEXTSCR)

(VMON)

(GC)

(princ "\nLoading- please wait... \\") 

(DEFUN MENU-OPERATION (HEADER ITEM-LIST PRMPT COLOR / HGT WDT I L-COL)
       (MENU_INIT COLOR)
       (PAINT_BKGRND TOP_MARG L_COL HGT WDT COLOR)
       (PAINT_FRAME TOP_MARG L_COL HGT WDT)
       (PRINT_HEADER TOP_MARG L_COL WDT)
       (PRINT_ITEMS ITEM-LIST TOP_MARG L_COL COLOR)
       (PRINT_PRMPT PRMPT TOP_MARG L_COL HGT)
       (USR_VAL))

(princ "\rLoading- please wait... \|") 

(DEFUN MENU_INIT (COLOR)
       (TEXTSCR)
       (CLS)
       (NORMAL)
       (PRINC (STRCAT "\e[" (ITOA COLOR) "m"))
       (IF (/= (REM (STRLEN HEADER) 2) 0) (SETQ HEADER (STRCAT HEADER " ")))
       (SETQ HGT (+ 5 (LENGTH ITEM-LIST))
             WDT (+ 10 (MAX (LONGEST ITEM-LIST) (STRLEN HEADER))))
       (IF (/= (REM HGT 2) 0) (SETQ HGT (1+ HGT)))
       (IF (/= (REM WDT 2) 0) (SETQ WDT (1+ WDT)))
       (SETQ L_COL (- 40 (/ WDT 2))
             i 0
             TOP_MARG (- 12 (/ HGT 2))))

(princ "\rLoading- please wait... \/") 

(DEFUN PAINT_BKGRND (RW CL HT WD COLOR)
       (IF (> COLOR 40)  ;;don't try to paint invisible backgrounds
           (PROGN (GOTO (1+ RW) (1+ CL))
                  (REPEAT (- HT 1)
                          (REPEAT (- WD 2) (PRINC " " ))
                                  (NEXTROW (- WD 2))))))

(princ "\rLoading- please wait... \-") 

(DEFUN PAINT_FRAME (RW CL HT WD)
       (GOTO RW CL)           ;; position cursor at top left corner of frame 
       (PRINC (CHR 201))      ;; paint top left corner of frame
       (REPEAT (- WD 2)       ;; paint top of frame
               (PRINC (CHR 205)))
       (PRINC (CHR 187))      ;; paint top right corner of frame
       (REPEAT 3
               (NEXTROW WD)
               (PRINC (CHR 186))      ;; print side-of-frame char
               (MOVE (- WD 2) "C")    ;; move to right side of frame
               (PRINC (CHR 186)))     ;; print side-of-frame char
       (NEXTROW WD)
       (PRINC (CHR 204))                     ;;paint middle bar
       (REPEAT (- WDT 2) (PRINC (CHR 205)))
       (PRINC (CHR 185))
       (REPEAT (- HT 5)
               (NEXTROW WD)
               (PRINC (CHR 186))      ;; print side-of-frame char
               (MOVE (- WD 2) "C")    ;; move to right side of frame
               (PRINC (CHR 186)))     ;; print side-of-frame char
       (NEXTROW WD)
       (PRINC (CHR 200))
       (REPEAT (- WDT 2) (PRINC (CHR 205)))
       (PRINC (CHR 188)))

(princ "\rLoading- please wait... \\") 

(DEFUN PRINT_HEADER (RW CL WD)
       (GOTO (+ RW 3)
             (+ CL (- (/ WD 2) (/ (STRLEN HEADER) 2))))
       (BOLD)
       (PRINC HEADER)
       (NORMAL))
 
(DEFUN PRINT_HEADER (RW CL WD)
       (GOTO (+ RW 2)
             (+ CL (- (/ WD 2) (/ (STRLEN HEADER) 2))))
       (BOLD)
       (PRINC HEADER))
 
(princ "\rLoading- please wait... \|") 

(DEFUN PRINT_ITEMS (ITM_LST RW CL COLOR)
       (PRINC (STRCAT "\e[0m\e[" (ITOA COLOR) "m")) ;;restore normal screen
       (SETQ I 0)                                   ;;& then init user color
       (FOREACH ITEM ITM_LST
                (SETQ I (1+ I))
                (GOTO (+ RW 4)
                      (+ CL 2))
                (MOVE I "B")     ;; move I spaces down
                (PRINC (STRCAT " "
                               (IF (< I 10) " " "")
                               (RTOS (FLOAT I) 2 0) "] " ITEM))))

(princ "\rLoading- please wait... \/") 

(DEFUN PRINT_PRMPT (PRMPT RW CL HT)
       (NORMAL)
       (GOTO (+ RW HT 3) 0)
       (PRINC PRMPT)
       (GC))

(princ "\rLoading- please wait... \-") 

(DEFUN USR_VAL ()
       (NORMAL)
       (SETQ CHOICE (GETINT))
       (WHILE (OR (< CHOICE 1) (> CHOICE (LENGTH ITEM-LIST)))
              (SETQ CHOICE (GETINT "Choice is out of range, try again: ")))
       (CLS) CHOICE)

;;length of longest string in a list of strings
(princ "\rLoading- please wait... \\") 

(DEFUN LONGEST (LST)
       (APPLY 'MAX (MAPCAR '(LAMBDA (ITM) (STRLEN ITM)) LST)))

(princ "\rLoading- please wait... \|") 

(DEFUN BOLD ()
       (PRINC "\e[1m"))

(princ "\rLoading- please wait... \/") 

(DEFUN NORMAL ()
       (PRINC "\e[0m"))

;; This an an example of using MENU-OPERATION to get a value from the user.
;; The first argument must be the header.
;; The second argument must be a list of things to be chosen from.
;; The third argument must be a prompt [question] to the user.
;; MENU-OPERATION looks for and returns an integer.

(princ "\rLoading- please wait... \-") 

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
                 "Tamarack, eastern")
                 "Select number corresponding to type of wood to be used: " 
                  (ran_color) )))

(princ "\rLoading- please wait.. \\")

(DEFUN RVRS ()
       (PRINC "\e[7m"))

(princ "\rLoading- please wait.. \|")

(defun MOVE (NO DIR) ;;DIR ARG: A=UP   B=DOWN   C=RIGHT   D=LEFT
       (princ (strcat "\e[" (itoa NO) DIR)))

(princ "\rLoading- please wait... \/") 

(defun CLS () (textscr)
       (princ "\e[2J"))

(princ "\rLoading- please wait... \-") 

(defun goto (ROW COL)
       (princ (strcat "\e[" (itoa row) "\;" (itoa col) "H")))

(princ "\rLoading- please wait... \\") 

(defun nextrow (cols)
       (princ (strcat "\e[" (itoa cols) "D" "\e[1B")))

(princ "\rLoading- please wait... \|") 

(defun ran_color (/ *s)
       (setq s (if s (rem (+ (* s 15625.7) 0.21137152) 1)
                      0.3171943)
             s (* 50 s))
       (cond ((< s 31) (setq *s (fix (max 31 (/ (+ s 46) 2)))))
             ((> s 46) (setq *s (fix (min 46 (/ (+ s 31) 2)))))
             (T (setq *s (fix s)))))

(princ "\e[2J")
(princ "\nThis menu system is written for the ANSI graphics standard.")
(princ "\nIf your screen didn't just clear, you need to add the line:")
(princ "\n\nDEVICE=ANSI.SYS\n")
(princ "\nto your CONFIG.SYS file in order to use MENU-OPERATION.")
(princ "\n\nThe syntax is: ")
(princ "\n\n\(menu-operation \"header\" '(\"item-1\" \"item-2\" ... \"item-n\") \"prompt\"\)")
(princ "\n\nType TEST to try a sample menu.")
(princ)

