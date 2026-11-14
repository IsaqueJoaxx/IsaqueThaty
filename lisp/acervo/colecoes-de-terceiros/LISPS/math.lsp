;
;
;
; *****************************************************************************
;                         *   AutoLisp MATH Program   *
;                         *          Developed        *
;                         *             By            *
;                         *      Greg MacGeorge       *
;                         *      Louisville, Ky.      *
; *****************************************************************************
;
; This program performs math functions as determined by the user. The program
; acts as an on line calculator while in AutoCad. The math functions used are:
; ADD, SUBTRACT, MULTIPLY, DIVIDE, SQUARE and SQR.ROOT. The input numbers and
; result are stored in memory and may be accessed at any time by typing an
; exclamation point (!) and the variable name. For instance, !N1 is the first
; input number variable, !N2 is the second number and !R is the resultant of
; the function.
;
;
;
; *****************************************************************************
;
;
;
;
; ************************  Print Data Sub-Routine  ***************************
;
;
(DEFUN M2 ()
     (PRINT)
     (PRINC N1)
     (PRINC FUN)
     (PRINC N2)
     (PRINC " = ")
     (PRINC R)
     (PRINT)
)
;
;
; ************************  Print Data Sub-Routine  ***************************
;
;
(DEFUN M3 ()
     (PRINT)
     (PRINC N1)
     (PRINC FUN)
     (PRINC " = ")
     (PRINC R)
     (PRINT)
)
;
;
; ************************  Print Data Sub-Routine  ***************************
;
;
(DEFUN M4 ()
     (IF (= F "ADD") (M2))
     (IF (= F "SUB") (M2))
     (IF (= F "MUL") (M2))
     (IF (= F "DIV") (M2))
     (IF (= F "SQR") (M3))
     (IF (= F "ROT") (M3))
)
;
;
; ****************************  Function Set-Up  ******************************
;
(DEFUN M1 ()
     (PRINT)
     (SETQ F (GETSTRING "SELECT A FUNCTION: "))
     (IF (= F "ADD") (ADD) "TRY AGAIN")
     (IF (= F "SUB") (SUB) "TRY AGAIN")
     (IF (= F "MUL") (MUL) "TRY AGAIN")
     (IF (= F "DIV") (DIV) "TRY AGAIN")
     (IF (= F "SQR") (SQR) "TRY AGAIN")
     (IF (= F "ROT") (ROT) "TRY AGAIN")
)
;
;
; ****************************  Add Sub-Routine  ******************************
;
;
(DEFUN ADD ()
     (PRINT)
     (PRINT)
     (SETQ FUN " + ")
     (SETQ N1 (GETREAL "ENTER THE FIRST NUMBER: "))
     (PRINT)
     (SETQ N2 (GETREAL "ENTER THE SECOND NUMBER: "))
     (PRINT)
     (SETQ R (+ N1 N2))
)
;
;
; ***************************  Subtract Sub-Routine  **************************
;
;
(DEFUN SUB ()
     (PRINT)
     (PRINT)
     (SETQ FUN " - ")
     (SETQ N1 (GETREAL "ENTER THE FIRST NUMBER: "))
     (PRINT)
     (SETQ N2 (GETREAL "ENTER THE SECOND NUMBER: "))
     (PRINT)
     (SETQ R (- N1 N2))
)
;
;

; **************************  Multiply Sub-Routine  ***************************
;
;
(DEFUN MUL ()
     (PRINT)
     (PRINT)
     (SETQ FUN " x ")
     (SETQ N1 (GETREAL "ENTER THE FIRST NUMBER: "))
     (PRINT)
     (SETQ N2 (GETREAL "ENTER THE SECOND NUMBER: "))
     (PRINT)
     (SETQ R (* N1 N2))
)
;
;
; ***************************  Divide Sub-Routine  ****************************
;
;
(DEFUN DIV ()
     (PRINT)
     (PRINT)
     (SETQ FUN " / ")
     (SETQ N1 (GETREAL "ENTER THE FIRST NUMBER: "))
     (PRINT)
     (SETQ N2 (GETREAL "ENTER THE SECOND NUMBER: "))
     (PRINT)
     (SETQ R (/ N1 N2))
)
;
;
; ****************************  Square Sub-Routine  ***************************
;
;
(DEFUN SQR ()
     (PRINT)
     (PRINT)
     (SETQ FUN " ^2 ")
     (SETQ N1 (GETREAL "ENTER THE NUMBER: "))
     (SETQ N2 0)
     (PRINT)
     (SETQ R (* N1 N1))
)
;
;
; **************************  Square Root Sub-Routine  ************************
;
;
(DEFUN ROT ()
     (PRINT)
     (PRINT)
     (SETQ FUN " ^1/2 ")
     (SETQ N1 (GETREAL "ENTER THE NUMBER: "))
     (SETQ N2 0)
     (PRINT)
     (SETQ R (SQRT N1))
)
;
;
;
; *****************************************************************************
;
;
(DEFUN M5 ()
     (M1)
     (M4)
     (SETQ ANSWER (GETSTRING "PERFORM ANOTHER ? (Y/N) "))
     (IF (= ANSWER "Y") (M5))
)
;
;
;************************  Main Program Execution  ****************************
;
;
(DEFUN C:MATH ()
     (COMMAND "MENU"
        (WRITE-LINE "MATH")
     )
     (M1)
     (M4)
     (SETQ ANSWER (GETSTRING "PERFORM ANOTHER ? (Y/N) "))
     (IF (= ANSWER "Y") (M5))
     (COMMAND "MENU"
        (WRITE-LINE "ACAD")
     )
)
;
;
