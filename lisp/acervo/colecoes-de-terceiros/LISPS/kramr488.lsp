;==================================================================
;
;   CADENCE 4/88          AutoLISP Concepts
;
;   Bill Kramer   Kramer Consulting, Inc.  (614)-792-3900
;
;   Example of using "English" like statements to drive AutoCAD.
;   Demonstrates parsing and pattern matching.
;
;========1=========2=========3=========4=========5=========6=========7
;
;   LISTING 1:   Pattern matching in lists.
;
;      PATTERN:   Input template pattern to match.
;      INP:       Incoming list to see if pattern matches.
;
;     Within PATTERN, the following entries are special:
;        $ONE$    Any single word
;        $MANY$   Any combination of words
;        $SAVE$   Builds return list with information to save
;
;========1=========2=========3=========4=========5=========6=========7
;
(defun Lmatch (pattern inp)
   (cond
     ((and (null pattern) (null inp))) 
     ((or (null pattern) (null inp)) nil)
     ((or (equal (car Pattern) "$ONE$")
          (equal (car pattern) (car inp)))
        (Lmatch (cdr pattern) (cdr inp)))
     ((equal (car pattern) "$MANY$")
      (cond ((Lmatch (cdr Pattern) (cdr inp)))
            ((Lmatch pattern (cdr inp)))))
     ((equal (car pattern) "$SAVE$")
        (cond ((Lmatch (cdr pattern) (cdr inp))
                  (setq Extracted (cons (car inp)
                          (if (null Extracted) nil (list Extracted)))))
              ((Lmatch pattern (cdr inp))
                  (setq Extracted
                     (cons (car inp) Extracted)))))))

;========1=========2=========3=========4=========5=========6=========7
;
;   LISTING 2:   Parse input string into a list of strings.
;
;========1=========2=========3=========4=========5=========6=========7
(defun sparse (S / LL TMP CNT)
   (setq TMP "" CNT 0)
   (while (< CNT (strlen S))
      (setq CNT (1+ CNT))
      (cond
        ((and (= (substr S CNT 1) " ") (/= TMP "")) 
          (setq LL (cons TMP LL) TMP "")) 
        ((= (substr S CNT 1) ".")) 
        ((= (substr S CNT 1) ",")) 
        (t
          (setq TMP (strcat TMP (substr S CNT 1)))))) 
   (reverse (cons TMP LL))
)
;========1=========2=========3=========4=========5=========6=========7
;
;   LISTING 3      Talker main program.
;
;========1=========2=========3=========4=========5=========6=========7
(defun C:TALKER ()
   (setq Exit nil)
   (setvar "CMDECHO" 0)
   (setvar "BLIPMODE" 0)
   (prompt "\nEnglish like AutoCAD example.")
   (prompt "\n\nCommands accepted are:")
   (prompt "\n\tDisplay the ...")
   (prompt "\n\tHide the ...")
   (prompt "\n\tDraw ...")
   (prompt "\n\t.. quit ...")
   (prompt "\n\nObjects recognized include:")
   (prompt "\n\tLINES, ARCS, and CIRCLES.")
   (setq Pat1 '("DISPLAY" "THE" "$SAVE$")
         Pat2 '("HIDE" "THE" "$SAVE$")
         Pat3 '("DRAW" "$SAVE$" "$SAVE$")
         Pat4 '("$MANY$" "QUIT" "$MANY$") 
         Pat5 '("$MANY$" "QUIT")) 
   (command "LAYER" "N" "LINES,ARCS,CIRCLES,TEMP" "") 
   (while (null Exit)
     (setq S (getstring 1 "\nType your command -->"))
     (setq S (mapcar 'strcase (sparse S))
           extracted nil)
     (cond
        ((setq ext (Lmatch Pat1 S)) 
           (command "LAYER" "S" "TEMP" "")
           (foreach LL (layers ext) (command "LAYER" "THAW" LL "")))
        ((setq ext (Lmatch Pat2 S))
           (command "LAYER" "S" "TEMP" "")
           (foreach LL (layers ext) (command "LAYER" "FREEZE" LL "")))
        ((setq ext (Lmatch Pat3 S))
           (setq Cnt (how_many (car ext))
                 ext (caadr ext)
                 ext (cond
                        ((= ext "LINES") "LINE")
                        ((= ext "ARCS") "ARC")
                        ((= ext "CIRCLES") "CIRCLE")
                        (t ext)))
           (if (null (member ext '("LINE" "ARC" "CIRCLE"))) (setq cnt 1))
           (repeat CNT
              (cond
                ((= ext "LINE") (setq P1 (getpoint "\nStarting point:")
                                      P2 (getpoint P1 "\nEnding point:"))
                          (command "LAYER" "T" "LINES" "s" "LINES" "" 
                                   "LINE" p1 p2 ""))
                ((= ext "ARC") (setq p1 (getpoint "\nCenter of arc:")
                                     p2 (getpoint p1 "\nStart point:")
                                     p3 (getpoint p2 "\nEnd point:"))
                          (command "LAYER" "T" "ARCS" "S" "ARCS" "" 
                                   "ARC" P2 "C" P1 P3))
                ((= ext "CIRCLE")
                       (setq p1 (getpoint "\nCenter of circle:")
                             p2 (getdist p1 "\nRadius:"))
                          (command "LAYER" "T" "CIRCLES" "S" "CIRCLES" ""
                                "CIRCLE" P1 p2))
                (t (prompt (strcat "\nThe entity type " ext
                      " is not recognized.")))
              )
        ))
        ((lmatch pat4 S) (setq Exit "All done"))
        ((lmatch pat5 S) (setq Exit "All done"))
        (t
          (prompt "\nSorry, do not understand what you are saying.")
          (prompt "\n\nCommands accepted are:")
          (prompt "\n\tDisplay the ...")
          (prompt "\n\tHide the ...")
          (prompt "\n\tDraw ...")
          (prompt "\n\t.. quit ...")
          (prompt "\n\nObjects recognized include:")
          (prompt "\n\tLINES, ARCS, and CIRCLES.")
        )
    )
  )
)

;========1=========2=========3=========4=========5=========6=========7
;   Layer relations
;
(defun layers (llist)
   (cond ((null llist) '("0"))
         ((= (substr (car llist) 1 4) "CIRC") '("CIRCLES"))
         ((= (substr (car llist) 1 3) "ARC") '("ARCS"))
         ((= (substr (car llist) 1 4) "LINE") '("LINES"))
         (t '("0"))
   ))
;
;========1=========2=========3=========4=========5=========6=========7
;   Given string with count value, return numeric value
;
(defun how_many (test)
  (cond
   ((or (= test "A") (= test "AN") 
        (= test "ONE") (= test "1")) 1)
   ((or (= test "TWO") (= test "2")) 2)
   ((or (= test "THREE") (= test "3")) 3)
   ((or (= test "FOUR") (= test "4")) 4)
   ((or (= test "FIVE") (= test "5")) 5)
   (t (prompt (strcat "\nSorry, " test
              " is not in my vocabulary.")) 0)))

