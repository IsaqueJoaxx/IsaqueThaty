; HOTKEY.LSP
 (textpage)
; Where we redefine ALT-keys to our purpose
(defun C:HOTKEY  (/ OLDERR INPUT KEY N OLD OLDSTR STRING)
                 (setvar "CMDECHO"  0)
                 (setvar "MENUECHO" 2)
                 (textpage)
                 (setq OLDERR *error*
                       INPUT  '(2 1))
; First, define error trapping
                 (defun *error*  (S)
                                 (if S (princ S))
                                 (setq *error* OLDERR)
                                 (setvar "CMDECHO"  1)
                                 (setvar "MENUECHO" 0)
                                 (graphscr)
                                 (prin1))
; Next, undefine all ALT keys previously defined
                 (foreach N HOTKEY
                   (write-line (strcat (chr 27) "[0;" (car N) ";0;" (car N) "p")))
; Now, loop for entries
                 (while INPUT 
                   (prompt "\nStrike the key to redefine: ")
                   (setq INPUT (grread)           ; Get input
                         KEY   (cadr INPUT))
                   (if (= (car INPUT) 2)          ; if Keyboard?
                     (progn
                       (setq TXT (cond
                           ((= (cadr INPUT) 27)    ; ESCAPE: EXIT.
                              (setq INPUT ()))
                           ((< 127 KEY 132)       ; Numeral 9 and 0, characters '-' and '='
                              (setq KEY (+ KEY 128))
                              (strcat "ALT-" (substr "90-=" (- KEY 127) 1)))
                           ((< 143 KEY 154)       ; Ascii 'Q' to 'P'
                              (strcat "ALT-" (substr "QWERTYUIOP" (- KEY 143) 1)))
                           ((< 157 KEY 167)       ; Ascii 'A' to 'L'
                              (strcat "ALT-" (substr "ASDFGHJKL" (- KEY 157) 1)))
                           ((< 171 KEY 179)       ; Ascii 'Z' to 'M'
                              (strcat "ALT-" (substr "ZXCVBNM" (- KEY 171) 1)))
                           ((< 211 KEY 222)       ; Shitf-Function keys
                              (strcat "SHIFT-F" (itoa (- KEY 211))))
                           ((< 221 KEY 232)       ; Ctrl- Function keys
                              (strcat "CTRL-F"  (itoa (- KEY 221))))
                           ((< 231 KEY 242)       ; Alt-Function keys
                              (strcat "ALT-F"   (itoa (- KEY 231))))
                           ((< 247 KEY 256)       ; Numeral 1 to 8
                              (strcat "ALT-" (substr "12345678" (- KEY 247) 1)))
                           (t (prompt "\n                    Invalid key... Try again."))))
                       (if TXT                    ; If valid
                         (progn
                           (setq KEY    (itoa (- KEY 128))
                                 OLDSTR (if (setq OLD (assoc KEY HOTKEY)) (cdr OLD) "")
                                 STRING (getSTRING t
                                          (strcat "  " TXT
                                                  "\n         Old string: " (chr 34) OLDSTR (chr 34)
                                                  "\n   " (if (eq OLDSTR "") "      " "Flush/")
                                                             "New string: " (chr 34))))
                           (cond
                             ((member (strcase STRING) '("F" "FLUSH"))
                                (setq HOTKEY (subst (cons KEY ()) OLD HOTKEY)))
                             ((eq OLDSTR "")
                                (if (= STRING "")
                                  (prompt "\n                    No input... key not defined.")
                                  (setq HOTKEY (cons (cons KEY STRING) HOTKEY))))
                             (t (if (/= STRING "")
                                  (setq HOTKEY (subst (cons KEY STRING) OLD HOTKEY))))))))
                     (prompt "\n                    Invalid input... Use keyboard.")))
; Done! Now, define keys.
                 (setq oldkey HOTKEY
                       HOTKEY ())
                 (foreach N oldkey
                   (if (cdr N)          ; If the entry was not flushed
                     (progn             ;  validate
                       (write-line (strcat (chr 27) "[0;" (car N) ";"
                                           (chr 34) (cdr N) (chr 34) "p"))
                       (setq HOTKEY (cons N HOTKEY)))))
; And exit.
                 (textpage)
                 (prompt "\n  >>> End program. <<<")
                 (*error* ()))
;
; Installation routine
;  Here we check for the presence of 'HOTKEY.DTA'
 (setq HOTKEY ())
 (if (setq INFILE (findfile "HOTKEY.DTA"))
   (progn
     (setq IN (open INFILE "r"))
     (while (and (setq STRING (read-line IN))
                 (not (eq STRING "")))
; Parse input
       (setq CODE "")
       (if (eq (ascii STRING) 59) ; ';' = Comment
         (setq STRING "")
         (progn
           (while (not (eq (ascii STRING) 44))    ; separator is a comma
             (setq CODE   (strcat CODE (strcase (substr STRING 1 1)))
                   STRING (substr STRING 2)))
           (setq STRING (substr STRING 2))))
; Valid entry?
       (if (not (eq STRING ""))
         (progn
           (setq KEY
             (cond
               ((eq (substr CODE 1 7) "SHIFT-F")
                  (if (zerop (setq X (atoi (substr CODE 8))))
                    () (+ 83 X)))
               ((eq (substr CODE 1 6) "CTRL-F")
                  (if (zerop (setq X (atoi (substr CODE 7))))
                    () (+ 93 X)))
               ((eq (substr CODE 1 4) "ALT-")
                  (if (= (strlen CODE) 5)
                    (progn
                      (setq X (ascii (substr CODE 5)))
                      (cond
                        ((< 64 X 91)
                           (nth (- X 65) '(30 48 46 32 18 33
                               34 35 23 36 37 38 50 49 24 25
                               16 19 31 20 22 47 17 45 21 44)))
                        ((< 47 X 58)
                           (- X 48))
                        (t ())))
                    (if (and (eq (substr CODE 5 1) "F")
                             (> (setq X (atoi (substr CODE 6))) 0))
                      (+ X 103)
                      ())))
               (t ())))
; If valid, add to list
           (if KEY
             (setq HOTKEY (cons (cons (itoa KEY) STRING) HOTKEY)))
         ) ; progn
       ) ; if string /= ""
     ) ; while
; Done!  Close file,
     (setq IN (close IN))
; then set keys
     (prompt "\n Setting HotKeys...")
     (foreach N HOTKEY
       (write-line (strcat (chr 27) "[0;" (car N) ";"
                           (chr 34) (cdr N) (chr 34) "p")))
     (prompt "\n\n      Done!")
   ))
;
(prin1)
