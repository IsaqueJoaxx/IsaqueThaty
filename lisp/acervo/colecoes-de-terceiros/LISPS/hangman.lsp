(defun C:HANG (/ ERR LWID LCNT FN PATH NST BEGST ENDST
WCNT WORD LETCNT MISSES GESLET GESNO WRDLST TOTHITS
GUESSES XPOS YPOS AGAIN NUMWINS NUMPLAYS)
;
; hangman for AutoCAD
;
(defun *ERROR* (ERR)
(if(ssget "X" '((8 . "HANGMAN")))
(command "erase" "p" "")
)
(command "layer" "t" "*" "s" "0" "")
(setvar "BLIPMODE" 1)
(setvar "HIGHLIGHT" 1)
(setvar "CMDECHO" 1)
(setvar "EXPERT" 0)
(princ ERR)
(setq *ERROR* nil)
(terpri)
); end *ERROR*
;
(defun START (LETCNT / LEVL)
(if(ssget "X" '((8 . "HANGMAN")))(command "erase" "p" ""))
(command "layer" "m" "hangman" "f" "*" ""
"style" "HANGMAN" "txt" "0" "1" "0" "N" "N" "N"
"line" "0,0" "10,0" "10,10" "0,10" "c"
"line" "2,3.5" "2,9.5" "4,9.5" "4,8.5" ""
"line" "0.5,3.5" "3.5,3.5" ""
"zoom" "e")
(setq LWID(* 0.8(/ 9.0 LETCNT)))
(command "line" "0.5,2.0" (polar '(0.5 2.0) 0 LWID) "")
(if(> LETCNT 1)
(command "array" (entlast) "" "r" "1" LETCNT (* 1.25 LWID))
)
(command "text" "m" "5,1.5" "0.25" "0"
(strcat (itoa LETCNT) " LETTERS"))
(initget 1 "E D")
(setq LEVL(getkword(strcat
"\nPlay <E>asy (infinite tries) "
" or <D>ifficult (6 misses) ? ")))
(if(equal LEVL "E")(setq GESNO 36)(setq GESNO 5))
); end START
;
(defun GETFILE (/ FOUND DPATH DFN)
;
; routine to choose source-file for words
;
(setq DFN "ACAD.HDX" DPATH "C:/ACAD/")
(while(null FOUND)
(setq FN(getstring(strcat
"\nName of source file ? < " DFN " >")))
(if(equal FN "")(setq FN DFN))
(setq PATH(getstring(strcat
"\nPath for source ? < " DPATH " > ")))
(if(equal PATH "")(setq PATH DPATH))
(setq FOUND(findfile (strcat PATH FN)))
(if(null FOUND)(prompt(strcat"\n*ERROR* File " (chr 34)
PATH FN (chr 34) " not found!"))
(progn
(prompt(strcat"\nFile " (chr 34) PATH FN (chr 34) " found!"))
(setq DPATH PATH DFN FN)
)
)
)
); end GETFILE
;
(defun LCHAR (OST CH / CNT)                       
;
; function to remove leading character "CH"
; from a string "OST" and producs "NST", the new string
;
(setq CNT 1)                                      
(while(and(<= CNT(strlen OST))
(equal(substr OST CNT 1)CH))
(setq CNT(1+ CNT))                                
)                                                 
(setq NST(substr OST                              
CNT (1+(-(strlen OST)CNT))))
); end LCHAR
;
(defun TCHAR (OST CH / CNT)                       
;
; function to divide a string at the character "CH"
; and produce "BEGST", the string in front,
; and "ENDST", the string in back of the character
;
(setq CNT 1)
(while(and(<= CNT(strlen OST))
(not(equal(substr OST CNT 1)CH)))
(setq CNT(1+ CNT))                                
)                                                 
(setq BEGST(substr OST 1 (1- CNT)))               
(setq ENDST(substr OST (1+ CNT)                   
(-(strlen OST)CNT)))                              
); end TCHAR                                                 
;
(defun CNTFILE (/ WFIL)
;
; function to count # of words in file (counting only
; first word...as in hdx file
;
(prompt(strcat"\nSearching " (chr 34) PATH FN (chr 34)
" for Hangman word...Please Wait"))
(setq WFIL(open(strcat PATH FN) "r"))
(setq WCNT 0)
(while(setq ST(read-line WFIL))
(setq WCNT(1+ WCNT))
)
(close WFIL)
); end CNTFILE
;
(defun GETWORD (/ TIME RAND MAXI MINI WNUM ST)
;
; Function to find a "random" word in the file
;
(repeat 15
(setq TIME(rtos(getvar "cdate")2 15))
)
(setq RAND(atoi(substr TIME 19 3))
      MAXI(max RAND WCNT) MINI(min RAND WCNT)
      WNUM MAXI)
(while(> WNUM WCNT)
(setq WNUM(- WNUM MINI))
)
(setq WFIL(open (strcat PATH FN) "r"))
(repeat WNUM
(setq ST(read-line WFIL))
)
(prompt"\n* OKAY!* I'VE GOT MY WORD!")
(close WFIL)
(prompt(strcat"\n"
(chr 34) "CLEANING" (chr 34) " the word...Please Wait"))
(LCHAR ST " ")
(setq ST NST)
(LCHAR ST "'")
(setq ST NST)
(TCHAR ST " ")
(setq WORD(strcase BEGST)
      LETCNT(strlen WORD))
(prompt"\n* I'M READY TO PLAY! *")
); end GETWORD
;
(defun EXPAND (/ CNT )
;
; section  to create WRDLST, a list of the
; individual letters in the word
;
(setq CNT 1 WRDLST nil)
(while(<= CNT LETCNT)
(setq WRDLST(cons(substr WORD CNT 1)WRDLST)
      CNT(1+ CNT))
)
(setq WRDLST(reverse WRDLST))
); end EXPAND
;
(defun HEAD ()
;
; section to draw head
;
(princ"...Drawing Head")
(command "circle" "4,8" "0.5"
"line" "4.1875,8.125" "4.25,8.25"
"" "line" "4.25,8.125" "4.1875,8.25" ""
"line" "3.8125,8.125" "3.75,8.25" ""
"line" "3.75,8.125" "3.8125,8.25" ""
"line" "3.875,7.75" "4.125,7.75" ""
"line" "4,7.75" "4,7.625" ""
"arc" "3.875,7.75" "4,7.625" "4.125,7.75")
); end HEAD
;
(defun BODY ()
;
; section to draw body
;
(princ"...Drawing Body")
(command "line" "4,7.5" "4,5.75" ""
"line" "3.5,7.0" "4.5,7.0" ""
"line" "3.75,5.75" "4.25,5.75" ""
)
); end BODY
;
(defun LARM ()
;
; section to draw left arm
;
(princ"...Drawing Left Arm")
(command "line" "3.25,5.0" "3.5,7.0" "")
); end LARM
;
(defun RARM ()
;
; section to draw right arm
;
(princ"...Drawing Right Arm")
(command "line" "4.75,5.0" "4.5,7.0" "") 
); end RARM
;
(defun LLEG ()
;
; section to draw left leg
;
(princ"...Drawing Left Leg")
(command "line" "3.5,3.75" "3.75,5.75" "")
); end LLEG
;
(defun RLEG ()
;
; section to draw right leg
;
(princ"...Drawing Right Leg")
(command "line" "4.5,3.75" "4.25,5.75" "")
); end RLEG
;
(defun MISS ()
;
; function to prompt and tally "misses"
;
(setq MISSES(1+ MISSES))
(if(equal GESLET "GU")
(progn
(prompt(strcat"\n*** AWWWW! *** You give up too easily!"
"  My word was " (chr 34) WORD (chr 34)))
(PLAYAGN)
)
(progn
(prompt(strcat"\n* NO! * There are no " (chr 34)
GESLET "'s" (chr 34)" in the word...MISS #"(itoa MISSES)))
(cond
((equal MISSES 1)(HEAD))
((equal MISSES 2)(BODY))
((equal MISSES 3)(LARM))
((equal MISSES 4)(RARM))
((equal MISSES 5)(LLEG))
((equal MISSES 6)(RLEG))
(t (prompt"...Body is complete!"))
)
(LETPOST)
)
)
(if(> MISSES GESNO)
(progn
(prompt(strcat"\n*** AWWWW! *** You lost!  My word was " (chr 34)
 WORD (chr 34)))
(PLAYAGN)
)
)
); end MISS
;
(defun PLAYAGN ()
;
; Function to evaluate end or another round
;
(setq NUMPLAYS(1+ NUMPLAYS))
(prompt(strcat "\nYou've win " (itoa NUMWINS) " out of "
(itoa NUMPLAYS) " = " (rtos(* 100(/ (float NUMWINS) NUMPLAYS))2 1)
"%"))
(setq AGAIN nil)
(initget 1 "Y N")
(setq AGAIN(getkword"\nPlay Again <Y N> ? "))
); end PLAYAGN
;
(defun ENDPLAY ()
;
; Function to "end play" and clean up screen
; and variables
;
(if(ssget "X" '((8 . "HANGMAN")))
(command "erase" "p" "")
)
(command "layer" "t" "*" "s" "0" "")
(setvar "BLIPMODE" 1)
(setvar "HIGHLIGHT" 1)
(setvar "CMDECHO" 1)
(setvar "EXPERT" 0)
(terpri)
); end ENDPLAY
;
(defun HIT (/ CNT HITS POSLST  POS TPT)
;
; function to prompt and evaluate a "hit"
;
(prompt"\n* YES! * ")
(setq CNT 0 HITS 0)
(while(< CNT LETCNT)
(if(equal(nth CNT WRDLST)GESLET)
(setq HITS(1+ HITS)
   POSLST(cons (1+ CNT) POSLST))
) 
(setq CNT(1+ CNT))
)
(setq POSLST(reverse POSLST))
(prompt"There ")
(if(= HITS 1)
(prompt"is ")
(prompt"are ")
)
(prompt(strcat(itoa HITS) " " (chr 34) GESLET))
(if(> HITS 1)
(prompt "'s"))
(prompt(strcat (chr 34)" in the word at position"))
(if(> HITS 1)
(prompt "s: ")
(prompt ": "))
(setq CNT 0)
(while(< CNT(length POSLST))
(setq POS(nth CNT POSLST))
(princ(strcat (itoa POS) " "))
(setq TPT(polar(list 0.5 2.125)
 0 (+(/ LWID 2.0)(* 1.25 LWID (1- POS)))))
 (command "text" "c" TPT
"0.25" "0" (nth(1-(nth CNT POSLST))WRDLST))
(setq CNT(1+ CNT))
)
(setq TOTHITS(+ TOTHITS HITS))
); end HIT
;
(defun GUESS (/ TRY)
;
; section to take guess and evaluate
; a "HIT" or "MISS"
;
(initget 1 "A B C D E F G H I J K L M N O P Q R 
 S T U V W X Y Z 1 2 3 4 5 6 7 8 9 0 GU GE")
 (setq GESLET(strcase
 (getkword"\nGuess a letter or number 0-9, GE to guess or GU to give up: ")))
(if(equal GESLET "GE")
(progn
(setq TRY(strcase(getstring"\nWhat's your guess ? ")))
(if(equal TRY WORD)
(progn
(setq NUMWINS(1+ NUMWINS))
(prompt(strcat"\n*** NICE GUESS! *** You won!  My word was " (chr 34)
 WORD (chr 34)))
)
(prompt(strcat"\n*** AWWWW! *** You lost!  My word was " (chr 34)
 WORD (chr 34)))
); end if
(PLAYAGN)
); end progn
 (if(member GESLET GUESSES)
 (princ(strcat"\nYou have already guessed " (chr 34)
 GESLET (chr 34)))
(progn
(setq GUESSES(cons GESLET GUESSES))
(if(member GESLET WRDLST)
(HIT)
(MISS)
)
)
)
)
); end GUESS
;
(defun LETPOST ()
;
; function to post bad letters
;
(command "text" (list XPOS YPOS) "0.25" "0" GESLET)
(setq XPOS(+ XPOS 0.5))
(if(>= XPOS 9.5)
(setq XPOS 5.0 YPOS (- YPOS 0.5))
)
); end LETPOST
;
; Main Program
;
(defun MAIN ()
(setq WORD nil WORDLST nil GESLET nil
MISSES 0 GUESSES nil TOTHITS 0 XPOS 5.0 YPOS 8.5)
(setvar "BLIPMODE" 0)
(setvar "HIGHLIGHT" 0)
(setvar "CMDECHO" 0)
(setvar "EXPERT" 4)
(GETWORD)
(EXPAND)
(START LETCNT)
(while(and(not(equal GESLET "GE"))(not(equal GESLET "GU"))
(<= MISSES GESNO)(< TOTHITS LETCNT))
(GUESS)
)
(if(equal TOTHITS LETCNT)
(progn
(setq NUMWINS(1+ NUMWINS))
(prompt(strcat"\n*** YOU WON! *** My word was " (chr 34)
 WORD (chr 34)))
(PLAYAGN)
)
)
); end MAIN
;
; First start
;
(setq NUMWINS 0 NUMPLAYS 0)
(GETFILE)
(CNTFILE)
(setq AGAIN "Y")
(while(equal AGAIN "Y")
(MAIN)
)
(ENDPLAY)
)
