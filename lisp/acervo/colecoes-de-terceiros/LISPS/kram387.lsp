======================================================================================================
;
; Input/Output Expresion Examples.
;
; Listing 1: Long distance calculation
; Bill Kramer
;
(defun c:long-dist ()
   (setq Tot-dist 0.0 Cnt 0 Exit nil)                             ; Initialize
   (setq P1 (getpoint "\nStarting point:"))                       ; First point entry
   (while (null Exit)
     (setq P2 (getpoint "\nNext point:"))                         ; Next point entry
     (cond
       ((null P2)(setq Exit 1))                                   ; Nothing entered, set exit.
       (t (prompt "Distance: ")                                   ; Display distance during calc.
          (setq Tot-dist (+ Tot-dist (princ (distance P1 P2)))
                Cnt (1+ Cnt)                                      ; Increment leg counter.   	  
                P1 P2))))
    (prompt "\nTotal traversed distance: ")
    (princ Tot-dist)                                              ; Report total distance traveled.
    (prompt "  Ave: ")
    (if (> Cnt 0)(princ (/ Tot-dist Cnt))(prompt "0"))            ; Ave only if CNT > 0
    (prompt "\nLong distance measurement complete..."))
 

;
==============================================================================================================
;
; Listing 2. Data Typist
; Bill Kramer
;
(defun C:data-type ()
   (setvar "CMDECHO" 0)                                           ; Turn off AutoCAD command echo.
   (setq P1 (getpoint "\nStart point of text (left justify): "))
   (setq TH (getdist P1 "\nText height: "))
   (if (null TH)(setq TH (getvar "TEXTSIZE")))                    ; If nothing entered, use default
   (setq TR (getangle P1 "\nRotation angle: "))
   (if (null TR)(setq TR 0.0))                                    ; If nothing entered, use 0.0
   (setq VT (getdist P1 "\nVertical distance: ")) 
   (if (null VT)(setq Exit 1)(setq Exit nil))                     ; If nothing entered, exit
   (while (null Exit)
     (prompt "\nEnter: ")                                         ; Prompt for user entry of text line.
     (setq Txt (read-line))                                       ; Read line of text.
     (cond
       ((= Txt " ")(setq Exit 1))                                 ; Null text line, set exit condition
       (t
         (command "TEXT" P1 TH TR Txt)                            ; Output text information on display
         (setq P1 (polar P1 (* 1.5 PI) VT))))                     ; Calc next line of text position
     )
     (prompt "\nData entry ends.")                                ; Prompt end of program.
)        
