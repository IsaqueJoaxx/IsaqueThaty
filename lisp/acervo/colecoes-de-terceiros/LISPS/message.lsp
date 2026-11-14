 (DEFUN MESSAGESTORE (BIT1)
  (SETQ STR1A 
   (STRCAT "$" (SUBSTR (RTOS (GETVAR "CDATE") 2 6) 14 2) DWGNAME2 STR1A BIT1))
  (COMMAND "BLOCK" STR1A "0,0" "" "INSERT" STR1A "0,0" "" "" ""))


 (DEFUN MESSAGEDEL1 ()
  (INITGET (+ 1 4))
  (SETQ MARKER (GETINT (STRCAT "\n\n\nLine number to delete or" 
                               "  [0] for none  [9999] for all: ")))
  (GRAPHSCR)
  (IF (AND (/= MARKER 0) (/= MARKER 9999) (< MARKER LINE1))
   (PROGN
    (PRINC "\nProcessing, please wait....")
    (MESSAGEFUNC 2)
    (PRINC (STRCAT "\nNote: In order to regain memory" 
                   " used up by the deleted messages," 
                   "\nyou must purge the blocks" " with names prefixed $$."))))
  (IF (= MARKER 9999)
   (PROGN
    (PRINC "\nProcessing, please wait....")
    (MESSAGEFUNC 3)
    (PRINC (STRCAT "\nNote: In order to regain memory" 
                   " used up by the deleted messages," 
                   "\nyou must purge the blocks" " with names prefixed $$."))))
  (IF (AND (>= MARKER LINE1) (/= MARKER 9999))
   (PRINC (STRCAT "\nNo message found at line " (ITOA MARKER)))))


 (DEFUN MESSAGEDEL2 ()
  (IF (SETQ ERASE1 
   (SSGET "X" (LIST (CONS 2 (SETQ BNAME1 (STRCASE (CDR (ASSOC 2 BLIST1))))))))
    (COMMAND "ERASE" ERASE1 ""))
  (SETQ BNAME2 (SUBSTR (STRCAT "$" BNAME1) 1 (STRLEN BNAME1)))
  (COMMAND "RENAME" "B" BNAME1 BNAME2))


 (DEFUN MESSAGEFUNC (BIT1)
  (SETQ LINEMAX 15 CHAR1 nil LINE1 1)
  (IF (= BIT1 1)
   (PROGN
    (SETQ CHAR1 (STRCAT (ITOA LINE1) " "))
    (REPEAT (- 4 (STRLEN (ITOA LINE1)))
     (SETQ CHAR1 (STRCAT "0" CHAR1)))
    (SETQ CHAR1 (STRCAT "\n" CHAR1))
    (PRINC CHAR1)))
  (DWGNAME)
  (SETQ COUNT2 1 BLIST1 (TBLNEXT "BLOCK" T))
  (WHILE BLIST1
   (IF (AND 
    (= (SUBSTR (SETQ STR1 (CDR (ASSOC 2 BLIST1))) 1 1) "$") 
    (= (STRCASE (SUBSTR STR1 4 8)) DWGNAME2))
     (PROGN
      (IF (AND (= LINE1 MARKER) (= BIT1 2))
       (MESSAGEDEL2))
      (IF (= BIT1 3)
       (MESSAGEDEL2))
      (SETQ COUNT1 12)
      (WHILE (/= (SETQ CHAR1 (ATOI (SUBSTR STR1 COUNT1 2))) 0)
       (IF (= CHAR1 99)
        (PROGN
         (SETQ COUNT2 1 LINE1 (1+ LINE1))
         (IF (AND (> LINE1 LINEMAX) (/= BIT1 2) (/= BIT1 3))
          (PROGN
           (PRINC (STRCAT "\n\n--Press RETURN" " for more --"))
           (GRREAD)
           (SETQ LINEMAX (+ LINEMAX 15))
           (TERPRI)
           (TERPRI)))
         (SETQ ASCIISTR1 (STRCAT (ITOA LINE1) " "))
         (REPEAT (- 4 (STRLEN (ITOA LINE1)))
          (SETQ ASCIISTR1 (STRCAT "0" ASCIISTR1)))
         (SETQ ASCIISTR1 (STRCAT "\n" ASCIISTR1)))
        (SETQ ASCIISTR1 (CHR (+ CHAR1 31))))
       (IF (= BIT1 1)
        (PROGN
         (IF (= COUNT2 70)
          (PRINC "\n     "))
         (PRINC ASCIISTR1)))
       (SETQ COUNT1 (+ COUNT1 2) COUNT2 (1+ COUNT2)))))
   (SETQ BLIST1 (TBLNEXT "BLOCK"))))


 (DEFUN DWGNAME ()
  (SETQ DWGNAME2 nil 
        DWGNAME1 (GETVAR "DWGNAME") 
        COUNT (STRLEN DWGNAME1) 
        CHAR1 (SUBSTR DWGNAME1 COUNT 1))
  (WHILE (AND (/= CHAR1 nil) (/= CHAR1 "/") (/= CHAR1 "\\") (> COUNT 0))
   (IF DWGNAME2
    (SETQ DWGNAME2 (STRCAT CHAR1 DWGNAME2))
    (SETQ DWGNAME2 CHAR1))
   (SETQ COUNT (1- COUNT) CHAR1 (SUBSTR DWGNAME1 COUNT 1)))
  (REPEAT (- 8 (STRLEN DWGNAME2))
   (SETQ DWGNAME2 (STRCASE (STRCAT "0" DWGNAME2)))))


 (DEFUN C:MESSAGE ()
  (TEXTSCR)
  (SETVAR "CMDECHO" 0)
  (SETQ STR4 nil)
  (INITGET "Store Retrieve")
  (SETQ STR1 (GETKWORD "\n[S]tore [R]etrieve\n"))
  (COND 
   ((= STR1 "Store")
    (SETVAR "EXPERT" 3)
    (PRINC (STRCAT "\n132 characters maximum, approx. one" 
                   " and three quarters line"))
    (IF (SETQ STR1 (GETSTRING T (STRCAT "\nStore message [*** CAUTION **>>" 
                                        " RETURN only when done]: \n\n")))
     (PROGN
      (SETQ NUMBER1 0)
      (GRAPHSCR)
      (PRINC "\nProcessing, please wait....")
      (DWGNAME)
      (SETQ COUNT1 1 COUNT2 0 STR1A nil)
      (WHILE (/= (SETQ CHAR1 (SUBSTR STR1 COUNT1 1)) "")
       (IF (< COUNT2 9)
        (PROGN
         (IF (< (SETQ ASCIISTR1 (- (ASCII CHAR1) 31)) 10)
          (SETQ ASCIISTR1 (STRCAT "0" (ITOA ASCIISTR1)))
          (SETQ ASCIISTR1 (ITOA ASCIISTR1)))
         (IF STR1A
          (SETQ STR1A (STRCAT STR1A ASCIISTR1))
          (SETQ STR1A ASCIISTR1))
         (IF (= (SUBSTR STR1 (1+ COUNT1) 1) "")
          (PROGN
           (MESSAGESTORE "99")
           (SETQ STR1A nil)))
         (SETQ COUNT2 (1+ COUNT2)))
        (PROGN
         (IF (< (SETQ ASCIISTR1 (- (ASCII CHAR1) 31)) 10)
          (SETQ ASCIISTR1 (STRCAT "0" (ITOA ASCIISTR1)))
          (SETQ ASCIISTR1 (ITOA ASCIISTR1)))
         (MESSAGESTORE ASCIISTR1)
         (IF (= (SUBSTR STR1 (1+ COUNT1) 1) "")
          (MESSAGESTORE "99"))
         (SETQ STR1A nil COUNT2 0)))
       (SETQ COUNT1 (1+ COUNT1)))
      (PRINC "\nProcess completed.")))
    (SETVAR "EXPERT" 0))
   ((= STR1 "Retrieve")
    (MESSAGEFUNC 1)
    (IF (> LINE1 1)
     (PROGN
      (INITGET "Yes No")
      (IF (= 
       (GETKWORD (STRCAT "\n\n\nDelete message" " [Y]es [N]o ?  ")) "Yes")
        (MESSAGEDEL1)))
     (PRINC "\nNo message found...."))))
  (SETVAR "CMDECHO" 1)
  (PRINC))
