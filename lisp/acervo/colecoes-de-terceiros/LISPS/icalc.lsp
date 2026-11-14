;  "ICALC.LSP" -- written by: Bob Thomas,
;                              Thomas Enterprises, ltd. 
;                              628 Clement Street
;                              San Francisco, CA  94118
;                              (415) 751-3275
; 
;   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *
;   This LISP file requires version 2.6 or later.
;
;   "ICALC.LSP" creates a function called "calc" that interupts AutoCAD
;   commands to prompt for some simple math calculations. Then
;   it passes the result of the calculation to the AutoCAD command.
;
;   For example, the following sequence uses the "calc" function to rotate
;   the last drawn object a calculated amount (78.65 degrees minus 6.293):
;
;   Command: ROTATE
;   Select objects: L
;   1 found
;   Select objects:  [press Return]
;   Base point:  [pick point]
;   <Rotation angle>/Reference: (CALC)
;   Multiply/Divide/Add/SUbtract/SQRoot of/SQUare of/: SU
;   Subtract: 6.293
;   From: 78.65
;   Result is: 72.357000     (Object is rotated this number of degeres)
;   Command:
;
;
;   "ICALC.LSP can be modified to include any form of math calculation
;   desired. To do so, first include an appropriate keyword in the 
;   "initget" string, i.e.:
;    
;       (initget "Multiply Divide Add SUbtract SQRoot SQUare Yourmath")
;
;       Then, add your math sequence to the "cond" function, i.e.:
;
;                  ((= (strcase x) "YOURMATH")
;                      ( <your get- functions as required> )
;                      ( <your math functions as required> )
;                  ) ; close sublist within "cond" function
;
;  One odd thing:
;
;  The square root math presents some problems. It seems that AutoLISP
;  won't accurately convert a whole number to an integer when it is
;  the result of square root calculation -- I don't know why. 
;  I have put some extra code in here that will work in most cases, 
;  but not all. Thus, be careful when using square root calculations 
;  if the AutoCAD command is expecting an integer. (Most of the time this 
;  is going to be a moot point anyway.)
;
(defun calc()

    (Progn 

; Prompt for which operation (modify this to include your own math)

      (Initget "Multiply Divide Add SUbtract SQRoot SQUare")
      (setq x
         (getkword "\nMultiply/Divide/Add/SUbtract/SQRoot of/SQUare of/:")
      ) ; end setq x

; Based on above response, do one of the following:

        (cond

; If subtraction:

            ((= (strcase x) "SUBTRACT")
                (setq b (getreal "\nSubtract: "))
                (setq a (getreal "From: "))
                (setq res (- a b))
            )

; If squaring:

            ((= (strcase x) "SQUARE")
                (setq a (getreal "\nSquare of: "))
                (setq res (* a a))
            )

; If square root:

            ((= (strcase x) "SQROOT")
                (setq a (getreal "\nSquare Root of: "))
                (setq res (sqrt a))
            )

; If addition:

            ((= (strcase x) "ADD")
                (setq a (getreal "\nAdd: "))
                (setq b (getreal " To: "))
                (setq res (+ a b))
            )

; If division:

            ((= (strcase x) "DIVIDE")
                (setq a (getreal "\nDivide: "))
                (setq b (getreal " By: "))
                (setq res (/ a b))
            )
            
; If multiplication:

            ((= (strcase x) "MULTIPLY")
                (setq a (getreal "\nMultiply: "))
                (setq b (getreal " By: "))
                (setq res (* a b))
             )

; -- Your own math functions can be added here.

         ) ; End Cond

; Converts whole numbers to integers (uses different conversion for
; square-root math):

         (if (/= (strcase x) "SQROOT")
                 (if (= (- res (fix res)) 0)
                        (setq res (fix res))
                 )
                 (if (< 0.0000009 (- res (fix res)))
                        (setq res (1+ (fix res)))
                 )
         ) ; End if

; Displays the result of the math:

         (if (= (type res) 'REAL)
             (prompt (strcat "\nResult is: " (rtos res 2 6)))
             (prompt (strcat "\nResult is: " (itoa res)))
         )

; returns the result to AutoCAD:

         (eval res)

      ) ; End Progn

) ; End Calc()

; EOF CALC4F.LSP  144 lines.  Bob Thomas.
