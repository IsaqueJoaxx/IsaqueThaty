;
;<<< Listing 1 - Program Header Format >>>
;
; 
;*******************************************************
;
;    < 1 >               (C) 1987 Kramer Consulting, 
Inc.
;    Author:  < 2 >
;
;    Macro    Description
;    -------- ----------------------------------------
;    < 3 >    < 4 >
;
;    Global Variables
;    -------------------------------------------------
;    < 5 >   < 6 >
;
;    File Information
;    < 7 >       < 8 >
;                < 9 >
;                < 10 >
; 
*******************************************************
;
;<< Listing 2 - Sub-Header Format >>>
;
; 
;-------------------------------------------------------
;
;   < 1 >                 (C)1987 Kramer Consulting, Inc.
;   Author: < 2 >
;
;   Function:
;     < 3 >
;
;   Local Variables:
;     < 4 >    < 5 >
;
;   Algorithm
;     < 6 >
;
;   Notes
;     < 7 >
;
;--------1---------2---------3---------4---------5---------6---------7
;
;    AutoLISP Concepts     November 1987 
;
;    CDNCB-87.LSP               (C) 1987  Kramer Consulting, Inc.
;    Author:  Bill Kramer
;
;    Function
;      C:HED     LISP file header generation tool.
;
(defun C:HED ()
   ;   Get the file name from the operator.
   (setq FN (strcase (getstring "\nAutoLISP Source file name <LSP>:"))) 
   ;   If HED file exists, erase it.
   (if (setq FH (open (strcat FN ".HED") "r"))
     (progn
        (close FH) (command "SH" (strcat "DEL " FN ".HED"))))
   ; Open up the files.
   (setq FL (open (strcat FN ".LSP") "r")
         FH (open (strcat FN ".HED") "w"))
   (if FL
    (progn
     ; Interrogate for main heading information.
     (write-line "; ***************************************************" FH)
     (write-line ";" FH)
     (write-line (strcat ";   " FN ".LSP     (c) 1987 Your company") FH)
     (setq S (getstring T "\nAuthors names: "))
     (write-line (strcat ";   Authors: " S) FH)
     (write-line ";" FH)
     (write-line ";    Function \tDescription" FH)
     (write-line ";    -------- \t-----------------------------" FH)
     (prompt "\nPlease wait.  Scanning for function names.")
     (setq FNC nil)
     (while (setq LN (read-line FL))
        (prompt ".")
       (if (= (strcase (substr LN 1 7)) "(DEFUN ")
           (setq S (strcase (car (instring (substr LN 8) "(")))
                 FNC (cons S FNC))) 
     )
     (setq FL (close FL) FNC (reverse FNC))
     (setq FL (open (strcat FN ".LSP") "r"))
     (foreach FF FNC
       (setq S (getstring T (strcat "\nDescribe function <" FF ">:")))
       (write-line (strcat ";    " FF " \t" S) FH)
     )
     ; While not the end of the LSP input file do
     (prompt "\nExtracting comments to HED file.\n")
     (write-line ";" FH)
     (write-line ";    << Main comments from source code >>" FH)
     (write-line ";" FH)
     (while (setq LN (read-line FL))
       (prompt ".")
       (setq C 1 L1 (strlen LN))
       (while (and (<= C L1) (= (substr LN C 1) " ")) (setq C (1+ C)))
       (if (= (substr LN C 1) ";")
         (write-line LN FH))
      )
      ; Close up the files.
      (setq FH (close FH) FL (close FL))
      (prompt "\nHED finished, no errors.    ")
    )
    (prompt "\nAutoLISP File not found.")
   )
)
;
(defun instring (S1 S2)
  (setq L1 (strlen S1) L2 (strlen S2) C 1)
  (while (<= C L1)
    (if (= (substr S1 C L2) S2) 
       (setq S1 (list (substr S1 1 (1- C))
                      (substr S1 (+ C L2)))
             C L1))
    (setq C (1+ C)))
 (if (listp S1) (setq S1 S1) (setq S1 nil)))
;
(defun *error* (S)
   (if (= S "File not found") (prompt "\nAutoLISP File not found")
       (prompt (strcat "HED Error: " S "      "))))
