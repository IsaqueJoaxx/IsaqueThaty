



;Table.LSP         Program by Jim McMillan         June 1987.
;
;CIS (72067,2076) -- PCAN ID: PC1806
;
;This program offers a method of numbering sequential items
;such as those found in a bill of material.
;I found that when you have a B.O.M. with 100 items, you would do
;something like array the text, then change the values to suit.
;Hence, this program, will number for you, and comes in
;handy especially in the above mentioned application.
;
;Notes:
;If you reply to the "FIRST PART OF STRING IS ? :", prompt with a
;<CR>,the program will run starting at the number sequence you selected
;at the "START COUNTER AT? <1> :" prompt, and run to the integer value 
;you entered at the "HOW MANY TIMES TO DO? " prompt.
;I added this so you could reply with the likes of- ITEM#, or PART#.
;Pressing return to the values shown as < >, will select them as 
;your defaults.
;
;
(DEFUN C:TABLE (/ COUNTER N SP B SPACE TH TS A C X)
(SETVAR "CMDECHO" 0)
(SETQ TS (STRCAT "\nTHE CURRENT TEXTSTYLE IS - " 
(GETVAR "TEXTSTYLE")))
(PROMPT TS)
(SETQ COUNTER (GETINT "\nSTART COUNTER AT? <1> :"))
(IF (= COUNTER NIL ) (SETQ COUNTER 1))
(SETQ N (GETINT "\nHOW MANY TIMES TO DO? :"))
(SETQ SP (GETPOINT "\nSTART WHERE? "))
(SETQ B (GETSTRING  T "\nFIRST PART OF STRING IS ? "))
(SETQ SPACE (GETDIST "WHAT IS THE SPACING? <0.375> :"))
(IF (= SPACE NIL) (SETQ SPACE 0.375))
(SETQ TH (GETDIST "\nTEXT HEIGHT IS? <0.1> : "))
(IF (= TH NIL) (SETQ TH 0.1))
(SETVAR "TEXTSIZE" TH)
(REPEAT N
(SETQ A (ITOA COUNTER))
(SETQ C (STRCAT B A))
(COMMAND "TEXT"  SP TH "" C)
(SETQ X (- (CADR SP) SPACE))
(SETQ SP (LIST (CAR SP) X))
(SETQ COUNTER (1+ COUNTER))
)
(SETVAR "CMDECHO" 1)
)
;End of routine.
;
