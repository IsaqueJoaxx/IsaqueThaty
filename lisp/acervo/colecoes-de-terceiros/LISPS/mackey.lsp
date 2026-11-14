;MacKey.lsp
;Alpha Release June 1994
;Copyright 1994 by Fitting Solutions
;
;
; NOTICE:
;
;         This is a shareware product, copyrighted by and sole property
;         of Fitting Solutions, 7924 Ashton Street, Alexandria, 22309;
;         CompuServe 73054,3053.
;
;         Usage of this product assumes compliance with the accompanying
;         document, MACKEY.DOC.
;
;         If you have not read this document, please do so now.
;
;         In no way may this code be altered or incorporated into other
;         software products without the express written permission of
;         Fitting Solutions.
;
;         This code is provided in non-encrypted form, so that you, as
;         a programmer, may participate in its development.  We would
;         appreciate any and all comments you may have in this regard.
;         However, this does not give license to either a) retain and
;         use it without registration, or b) utilize it in whole or in
;         part for other applications.
;
;
; DESCRIPTION:
;
; MacKey lets you define your keys for what YOU want to do.  You can set
; up macros for:
;
;         - Function keys (regular, SHFT+, CTRL, ALT+)
;         - Aplha keys    (CTRL+, ALT+)
;         - Numeric keypad keys (regular)
;
; The "macros" you can assign include:
;
;         - AutoCAD command sequences, i.e.,
;
;         - Strings of text which you will need to repeatedly type
;
;         - AutoLISP calls
;
; MacKey definitions override key def's which AutoCAD sets... for example,
; if you are sick of having F7 calling TABLET, because you don't have a
; tablet, you can now set it to the command or function you desire.
;
; The best way to see what you can do is to experiment... Set a key to
; your macro string, then Display the key listing.  If it looks OK, eXit
; and hit the key to see what happens.
;
;
; INSTALLATION:
;
;         - Place this file in a directory which is included in the
;           ACAD path.  If this makes no sense, place it in C:\ACAD\,
;           or wherever your ACAD.EXE resides.
;
;         - The file KEYLIST.TBL will be written to / read from C:\ACAD
;           unless you specify otherwise (see below).  Note: if you do
;           not have a C:\ACAD directory, you MUST change the file path
;           for KEYLIST.TBL (see below) to match some known directory on
;           your system.
;
;         - You can load MacKey either by typing, from the command line,
;           (LOAD "MACKEY"), or by adding this same line to your ACAD.LSP
;           file.  With the latter, MacKey will load automatically whenever
;           you enter a new drawing.
;
;         - To set keys to your def's, you can either type, from the command
;           prompt, READKEY.  Or, in your ACAD.LSP file, after the
;           (LOAD "MACKEY") call, you can add (C:READKEY).
;
;
; USAGE:
;
;         - After loading, type MACKEY to start the program.
;
;         - If you want the key setting function to automatically run, say,
;           from your ACAD.LSP start-up procedures, you can (after first
;           loading MACKEY.LSP) type in (C:READKEY).  You can also type
;           READKEY from the command prompt.
;
;         - If you want to add the key clearing function to a re-defined
;           END statement, you can add (C:CLRKEY).  You can also type
;           CLRKEY from the command prompt.
;
;         - You can display the current settings on the KEYLIST.TBL file by
;           typing, from the command prompt, DISPKEY.
;
;         - If you want the KEYLIST.TBL located somewhere other than C:\ACAD\,
;           you can set that below.
;
;         - You may pre-define keys in the KEYLIST.TBL file.  Just Set a key
;           or two from MacKey to get the format right, refer to the DOS book
;           (see the section on ASCII key codes, which may be buried under an
;           appendix for ANSI.SYS), and type away.
;
;
; KNOWN LIMITATIONS:
;
;         - This program does require ANSI.SYS (or its .COM equivalent) to
;           be loaded (from config.sys or DOS, respectively).
;
;         - When typing in the words to assign to a macro key, don't use
;           the ' (apostrophy) character, as this is the terminator for the
;           definition.  You may also want to use some caution with the "
;           (quote) character.  I have tested a few uses of it with no problem,
;           but when it comes to string handling, well....
;
;         - While you can type in an AutoLisp expression, the calling string
;           will be displayed (which isn't the prettiest thing in the world...)
;
;         - For obvious reasons, Shift and Regular alpha-numeric keys should
;           not be defined -- you'd never be able to type in commands!
;
;         - I decided to draw the line at regular numeric pad keys.  If you
;           really want them available as SHFT+, CTRL+ and ALT+, let me know.
;
;         - When you Display the key definitions, you are seeing those def's
;           in the KEYLIST.TBL file.  If you have Set any def's without
;           making them "permanent" (i.e., they are local to the session
;           only), you will NOT see them show up in this listing.
;
;
; REVISION HISTORY:
;
;
;
; THIS IS WHERE YOU CHANGE THE KEYLIST.TBL LOCATION...
; You can modify this file location or name if you wish:

(setq KEYTABLE "C:\\ACAD\\KEYLIST.TBL")

;
; Le Main Programme

(defun c:MacKey ( / mac-err mac-flg mac-key)
  (defun key-err (stg)
    (princ "\nMacKey halted\n")
    (setq *error* mac-err)
    (princ)
  )
  (setq mac-err *error*)
  (if (/= debug "Y")
    (setq *error* key-err)
  )
  (prompt "\nMacKey macro key setting program")
  (prompt "\n(c) Fitting Solutions 1994")
  (setq mac-flg nil)
  (while (/= mac-flg "X")
    (initget "S s C c R r D d X x")
    (prompt "\n \nChoose task:")
    (setq mac-key (getkword "\nSet/Clear/Read/Display/eXit <S>: "))
    (if mac-key
      (setq mac-key (strcase mac-key))
      (setq mac-key "S")
    )
    (if (= mac-key "S")
      (progn
        (setq keyflg nil)
        (while (/= keyflg "X")
          (c:setkey)
        )
      )
    )
    (if (= mac-key "X")
      (setq mac-flg "X")
    )
    (if (/= mac-flg "X")
      (if (findfile keytable)
        (cond
          ((= mac-key "C")(c:clrkey))
          ((= mac-key "R")(c:readkey))
          ((= mac-key "D")(c:dispkey))
          (t nil)
        )
        (prompt "\nKEYTABLE not yet defined.  Please set some keys first.")
      )
    )
  )
  (key-wrap)
  (setq *error* mac-err)
  (princ)
)

;
; DISPKEY:  Displays current key settings

(defun c:dispkey ( / key-fil disp-num key-dat disp-val)
  (textscr)
  (setq key-fil (open keytable "r")
        disp-num 1
  )
  (prompt "\nKey:     Definition:")
  (while (setq key-dat (read-line key-fil))
    (prompt (strcat
      "\n"
      (substr key-dat 1 9)
      (substr key-dat 22)
    ))
    (setq disp-num (+ 1 disp-num)
          disp-val (/ (atof (itoa disp-num)) 15)
    )
    (if (= disp-val (atoi (rtos disp-val)))
      (progn
        (getstring "\nPress <enter> to continue: ")
        (prompt "\nKey:     Definition:")
      )
    )
  )
  (princ)
)

;
; CLRKEY:  Clears key settings

(defun c:clrkey ( / key-fil key-dat clr-val clr-stg)
  (textscr)
  (setq key-fil (open keytable "r"))
  (while (setq key-dat (read-line key-fil))
    (setq clr-val (key-trim (substr key-dat 12 9))
          clr-stg (strcat "\e[" clr-val ";" clr-val "p")
    )
    (prompt clr-stg)
  )
  (prompt "\nKeys cleared.\n")
  (princ)
)

;
; READKEY: Enables key settings

(defun c:readkey ( / key-fil key-dat)
  (textscr)
  (setq key-fil (open keytable "r"))
  (prompt "\nKeys defined:\n")
  (while (setq key-dat (read-line key-fil))
    (prompt (strcat
      "\e["
      (key-trim (substr key-dat 12 9))
      ";'"
      (key-trim (substr key-dat 22))
      "';13p"
    ))
    (princ (substr key-dat 1 9))
  )
  (princ)
)

;
; SETKEY: Sets key values

(defun c:setkey ( / keytyp keyans keyset)
  (textscr)
  (initget "F f C c A a N n")
  (princ "\n \nChoose type of key to set, or <ENTER> to exit -- ")
  (setq keytyp (getkword "\nFunction/Control/Alternate/Numeric pad key <exit>: "))
  (if keytyp
    (setq keytyp (strcase keytyp))
  )
  (setq keyflg nil)
  (cond
    ((= keytyp "F")(setq keyset 1)(Fkeytable))
    ((= keytyp "C")(setq keyset 2)(Ckeytable))
    ((= keytyp "A")(setq keyset 1)(Akeytable))
    ((= keytyp "N")(setq keyset 1)(Nkeytable))
    (t (setq keyflg "X"))
  )
  (if keynam (setq keynam (key-trim keynam)))
  (if (/= keyflg "X")
    (progn
      (setq keycom (getstring T (strcat "\nEnter word(s) to assign to " keynam ": "))
            keynew (itoa keynew)
      )
      (if (= keyset 1)
        (setq keynew (strcat "0;" keynew))
      )
      (setq keystg (strcat "\e[" keynew ";'" keycom "';13p"))
      (prompt keystg)
      (initget "Y y N n")
      (setq keyans (getkword "\nWrite the definition to key file? <Y>: "))
      (if
        (and
          (/= keyans "N")
          (/= keyans "n")
        )
        (if
          (and
            (findfile keytable)
            (redef keynam)
          )
          (key-repl)
          (key-write)
        )
      )
    )
  )
  (princ)
)

;
; Subroutines

(defun redef (red-name / red-file red-list)
  (setq red-file (open keytable "r")
        red-list (list "")
  )
  (while (setq red-line (read-line red-file))
    (setq red-list (cons (key-trim (substr red-line 1 9)) red-list))
  )
  (close red-file)
  (if (member red-name red-list)
    T
  )
)

(defun key-repl ( / rep-file rep-list rep-line)
  (setq rep-file (open keytable "r")
        rep-list (list "")
  )
  (while (setq rep-line (read-line rep-file))
    (if (/= keynam (key-trim (substr rep-line 1 9)))
      (setq rep-list (cons rep-line rep-list))
      (princ (strcat "\nRemoved previous " keynam " from definition file."))
    )
  )
  (setq rep-list (cdr (reverse rep-list)))
  (close rep-file)
  (setq rep-file (open keytable "w"))
  (foreach rep-item rep-list
    (princ rep-item rep-file)
    (princ "\n" rep-file)
  )
  (close rep-file)
  (key-write)
)

(defun key-write ( / key-fil)
  ;;;listing format:
  ;;;  (col 1)  (col 12)  (col 22)
  ;;;  verbal   code      command
  ;;;  desc.:   string:   string:
  ;;;
  ;;;  SHFT+F12 0;84      Anodize per MIL-QQ-Umptyump
  ;;;  CTRL+P   16        PLINE
  (setq key-fil (open keytable "a"))
  (repeat (- 10 (strlen keynew))
    (setq keynew (strcat keynew " "))
  )
  (repeat (- 11 (strlen keynam))
    (setq keynam (strcat keynam " "))
  )
  (princ (strcat keynam keynew keycom) key-fil)
  (princ "\n" key-fil)
  (close key-fil)
)

(defun key-trim (stg)
  (while
    (= (substr stg (strlen stg) 1) " ")
    (setq stg (substr stg 1 (- (strlen stg) 1)))
  )
  (setq stg stg)
)

(defun key-wrap ()
  (setq keystg nil
        keyflg nil
        keycom nil
        keynew nil
        keynam nil
        keyset nil
  )
)

;
; Definition tables

(defun Nkeytable ( / keyval)
  (initget 1 "I i H h U u L l E e D d")
  (princ "\nEnter a letter for numeric pad (white) key assignment ")
  (princ "\n(do NOT press the numeric pad key itself)")
  (setq keyval (getkword
    "\n<I>nsert/<H>ome/Page <U>p/De<L>ete/<E>nd/Page <D>own: ")
        keyval (strcase keyval)
  )
  (cond
    ((= keyval "I")(setq keynew 82 keynam "Insert" ))
    ((= keyval "H")(setq keynew 71 keynam "Home"  ))
    ((= keyval "U")(setq keynew 73 keynam "PageUp" ))
    ((= keyval "L")(setq keynew 83 keynam "Delete" ))
    ((= keyval "E")(setq keynew 79 keynam "End"   ))
    ((= keyval "D")(setq keynew 81 keynam "PageDn" ))
  )
)

(defun Fkeytable ( / keyfun keyval)
  (initget "R r S s C c A a")
  (setq keyfun (getkword "\nRegular/Shift/Control/Alt <R>: "))
  (if keyfun
    (setq keyfun (strcase keyfun))
    (setq keyfun "R")
  )
  (initget 1 "2 3 4 5 6 7 8 9 10 11 12")
  (prompt "\nEnter function key number (2-12) ")
  (setq keyval (atoi (getkword "\n(do NOT hit the function key itself): ")))
  (cond
    ((= keyval 2 )(setq keylst (list 60  85  95  105)  keynam "F2" ))
    ((= keyval 3 )(setq keylst (list 61  86  96  106)  keynam "F3" ))
    ((= keyval 4 )(setq keylst (list 62  87  97  107)  keynam "F4" ))
    ((= keyval 5 )(setq keylst (list 63  88  98  108)  keynam "F5" ))
    ((= keyval 6 )(setq keylst (list 64  89  99  109)  keynam "F6" ))
    ((= keyval 7 )(setq keylst (list 65  90  100 110)  keynam "F7" ))
    ((= keyval 8 )(setq keylst (list 66  91  101 111)  keynam "F8" ))
    ((= keyval 9 )(setq keylst (list 67  92  102 112)  keynam "F9" ))
    ((= keyval 10)(setq keylst (list 68  93  103 113)  keynam "F10"))
    ((= keyval 11)(setq keylst (list 133 135 137 139)  keynam "F11"))
    ((= keyval 12)(setq keylst (list 134 136 138 140)  keynam "F12"))
  )
  (cond
    ((= keyfun "R")(setq keynew (nth 0 keylst)))
    ((= keyfun "S")(setq keynew (nth 1 keylst) keynam (strcat "SHFT+" keynam)))
    ((= keyfun "C")(setq keynew (nth 2 keylst) keynam (strcat "CTRL+" keynam)))
    ((= keyfun "A")(setq keynew (nth 3 keylst) keynam (strcat "ALT+" keynam)))
  )
)

(defun Akeytable ( / keyval)
  (initget 1 "A B C D E F G H I J K L M N O P Q R S T U V W X Y Z a b c d e f g h i j k l m n o p q r s t u v w x y z")
  (prompt "\nEnter letter for ALT+ assignment (A-Z)")
  (setq keyval (getkword "\n(do NOT press the ALT key): ")
        keyval (strcase keyval)
  )
  (cond
    ((= keyval "A")(setq keynew 30 keynam "ALT+A" ))
    ((= keyval "B")(setq keynew 48 keynam "ALT+B" ))
    ((= keyval "C")(setq keynew 46 keynam "ALT+C" ))
    ((= keyval "D")(setq keynew 32 keynam "ALT+D" ))
    ((= keyval "E")(setq keynew 18 keynam "ALT+E" ))
    ((= keyval "F")(setq keynew 33 keynam "ALT+F" ))
    ((= keyval "G")(setq keynew 34 keynam "ALT+G" ))
    ((= keyval "H")(setq keynew 35 keynam "ALT+H" ))
    ((= keyval "I")(setq keynew 23 keynam "ALT+I" ))
    ((= keyval "J")(setq keynew 36 keynam "ALT+J" ))
    ((= keyval "K")(setq keynew 37 keynam "ALT+K" ))
    ((= keyval "L")(setq keynew 38 keynam "ALT+L" ))
    ((= keyval "M")(setq keynew 50 keynam "ALT+M" ))
    ((= keyval "N")(setq keynew 49 keynam "ALT+N" ))
    ((= keyval "O")(setq keynew 24 keynam "ALT+O" ))
    ((= keyval "P")(setq keynew 25 keynam "ALT+P" ))
    ((= keyval "Q")(setq keynew 16 keynam "ALT+Q" ))
    ((= keyval "R")(setq keynew 19 keynam "ALT+R" ))
    ((= keyval "S")(setq keynew 31 keynam "ALT+S" ))
    ((= keyval "T")(setq keynew 20 keynam "ALT+T" ))
    ((= keyval "U")(setq keynew 22 keynam "ALT+U" ))
    ((= keyval "V")(setq keynew 47 keynam "ALT+V" ))
    ((= keyval "W")(setq keynew 17 keynam "ALT+W" ))
    ((= keyval "X")(setq keynew 45 keynam "ALT+X" ))
    ((= keyval "Y")(setq keynew 21 keynam "ALT+Y" ))
    ((= keyval "Z")(setq keynew 44 keynam "ALT+Z" ))
  )
)

(defun Ckeytable ( / keyval)
  (initget 1 "A B C D E F G H I J K L M N O P Q R S T U V W X Y Z a b c d e f g h i j k l m n o p q r s t u v w x y z")
  (prompt "\nEnter letter for CTRL assignment (A-Z)")
  (setq keyval (getkword "\n(do NOT press the CTRL key): ")
        keyval (strcase keyval)
  )
  (cond
    ((= keyval "A")(setq keynew 1  keynam "CTRL+A" ))
    ((= keyval "B")(setq keynew 2  keynam "CTRL+B" ))
    ((= keyval "C")(setq keynew 3  keynam "CTRL+C" ))
    ((= keyval "D")(setq keynew 4  keynam "CTRL+D" ))
    ((= keyval "E")(setq keynew 5  keynam "CTRL+E" ))
    ((= keyval "F")(setq keynew 6  keynam "CTRL+F" ))
    ((= keyval "G")(setq keynew 7  keynam "CTRL+G" ))
    ((= keyval "H")(setq keynew 8  keynam "CTRL+H" ))
    ((= keyval "I")(setq keynew 9  keynam "CTRL+I" ))
    ((= keyval "J")(setq keynew 10 keynam "CTRL+J" ))
    ((= keyval "K")(setq keynew 11 keynam "CTRL+K" ))
    ((= keyval "L")(setq keynew 12 keynam "CTRL+L" ))
    ((= keyval "M")(setq keynew 13 keynam "CTRL+M" ))
    ((= keyval "N")(setq keynew 14 keynam "CTRL+N" ))
    ((= keyval "O")(setq keynew 15 keynam "CTRL+O" ))
    ((= keyval "P")(setq keynew 16 keynam "CTRL+P" ))
    ((= keyval "Q")(setq keynew 17 keynam "CTRL+Q" ))
    ((= keyval "R")(setq keynew 18 keynam "CTRL+R" ))
    ((= keyval "S")(setq keynew 19 keynam "CTRL+S" ))
    ((= keyval "T")(setq keynew 20 keynam "CTRL+T" ))
    ((= keyval "U")(setq keynew 21 keynam "CTRL+U" ))
    ((= keyval "V")(setq keynew 22 keynam "CTRL+V" ))
    ((= keyval "W")(setq keynew 23 keynam "CTRL+W" ))
    ((= keyval "X")(setq keynew 24 keynam "CTRL+X" ))
    ((= keyval "Y")(setq keynew 25 keynam "CTRL+Y" ))
    ((= keyval "Z")(setq keynew 26 keynam "CTRL+Z" ))
  )
)

;
; Loading prompt

(if (findfile keytable)
  (c:readkey)
)

(repeat 30 (princ "\n"))
(princ "\nMacKey loaded.")
(princ "\nType MACKEY to start.\n")
(princ)
