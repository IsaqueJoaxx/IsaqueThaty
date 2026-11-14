; =================================================================
;    SPSINP  Spreadsheet Interface Macro.     by Bill Kramer  1986
;
;    Read a text file character by character, building a set of
;    columns from the data.  Numeric data is expected to be found
;    in the comma delimited format.
;
;    Variables Used
;
;       COLMS      List  (Real)      Location of decimal point in
;                                    column
;       YY         Real              Current row location
;       FH         File Pointer      Input file handle
;       FNAM       String            File name
;       CH         Integer           Character read in from file
;       TH         Real              Text height
;       TV         Real              Text vertical spacing distance
;       FLAG       Integer           Header/Trailer flip-flop
;       NN         Integer           Current column number
;       NX         Integer           Max column number
;       HD         String            Header (before decimal)
;       TR         String            Trailer (after decimal)
;       TX         Real              X coordinate for text output
; ====================================================================
;  Function SPSTXT, outputs text strings HD and TR.
;
(defun spstxt ()
   (setq flag 1) ; Reset Flip/Flop flag for definition of header/trailer.
   (setq mmm (* th 0.25)) ; Space between decimal and header number.
   (setq tx (- (nth nn colms) mmm)) 
   (command   ; Right justify text to just left of decimal location.
      "text" "r" (list tx yy) th 0.0
      hd
   )
   (setq tx (nth nn colms))
   (command
      "text" (list tx yy) th 0.0
      (strcat "." tr)
   )
   (setq hd "") (setq tr "")(setq nn (1+ nn))
)
; ---------------------------------------------
;  Function SPSINP (AutoCAD Macro)
; ---------------------------------------------
(defun c:spsinp ()
   (setvar "CMDECHO" 0)   ; Turn off command echos
   (setq colms (list 0.0)) ; Initialize list of column settings.
   (setq hd "") (setq tr "") ; Initialize character string variables.
   (setq nx (getint "\nNumber of Columns:"))
   (if (not (null nx))  ; Option: use BOUNDP 'NX
       (progn
          (prompt "\nShow the Column X locations:")
          (repeat nx
            (setq colms (cons (car (getpoint)) colms))
          )
          (setq colms (cdr (reverse colms)))
          (setq yy (cadr (getpoint "\nStarting Y Coordinate:")))
          (setq th (getreal "\nText Height:"))
          (setq tv (getreal "\nVertical Spacing:"))
          (setq fnam (getstring "\nName of File:"))
          (setq fh (open fnam "r"))
          (if (null fh)
              (prompt "\nFile not found")
              (progn   ; Else <file was found!>
                (setq ch 1)(setq nn 0)(setq flag 1)
                (while (not (null fh))   ; Option: use BOUNDP 'FH
                  (setq ch (read-char fh))
                  (cond
                    ((= ch 44)   ; Comma ","
                     (spstxt) ; End of column data, output text.
                     (if (= nn nx)
                          (progn
                             (setq nn 0)
                             (setq yy (- yy tv))
                          )
                     )
                    )
                    ((= ch 46) ; Period "."
                     (setq flag 2) ; Remaining characters in trailer.
                    )
                    ((= ch 10) ; Return, end of line.
                     (spstxt) ; End of column data, output text.
                     (setq yy (- yy tv)) ; Prepare for next line.
                     (setq nn 0)
                    )
                    ((= ch nil) ; End of file read?
                     (close fh)
                     (setq fh nil)
                    )
                    ((/= ch 32) ; Not a space, add to string.
                     (if (= flag 1) ; Add to header or trailer?
                         (setq hd (strcat hd (chr ch)))
                         (setq tr (strcat tr (chr ch)))
                     )
                    )
                  ) ; End of Conditional Test of CH value.
                ) ; End of While Loop
              ) ; End of PROGN <Else file was found>
          ) ; End of IF test for file not found.
       ) ; End of 1st PROGN
   ) ; End of IF test for column numbers
)
