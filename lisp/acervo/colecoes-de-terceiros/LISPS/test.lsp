;lesson1 lisp routine to check answers to CAD lesson #1

(defun C:ANSWERS (/ set1 n na text user )
(setq set1 (ssget '(5.87 0.85)))
(if (= set1 nil)(command "insert" "bad" "8.08,6.5" "" "" "")(progn
(setq n (sslength set1))
(setq na (ssname set1 0))
(setq text (cddr (entget na)))
(setq user (strcase (cdr (assoc 1 text))))
  (if 
       (or (= user "USER1")
           (= user "USER2")
           (= user "USER3")
           (= user "USER4")
           (= user "USER5")
           (= user "USER6")
           (= user "USER7")
           (= user "USER8")
           (= user "USER9")
           (= user "USER0")
       )
           (command "insert" "ok" "8.08,6.5" "" "" "");then
           (command "insert" "bad" "8.08,6.5" "" "" ""))));else
(redraw)
(AN2)
)

(defun an2 ()
;first answer
(defun a1 (/ seta n na tp tp1 mtl1)
(setq seta (ssget '(1 6)))
(setq n (sslength seta))
(setq na (ssname seta 0))
(setq tp (car(cdr(entget na))));next three lines check for no answer
(setq tp1(cdr tp))
(if (= tp1 "LINE") 
(command "insert" "bad" "8.08,6" "" "" "");then
(progn(setq text (cddr (entget na)));else
      (setq mtl1 (list (strcase (cdr (assoc 1 text)))))
      (if (or(= (car mtl1) "ABSOLUTE")
             (= (car mtl1) "POLAR")
             (= (car mtl1) "RELATIVE")
            )
          (command "insert" "ok" "8.08,6" "" "" "")   
           (command "insert" "bad" "8.08,6" "" "" ""));else
      ))
)
;next answer
(defun a2 (/ setb n na tp tp1)
(setq setb (ssget '(1 5.8)))
(setq n (sslength setb))
(setq na (ssname setb 0))
(setq tp (car(cdr(entget na))));next three lines check for no answer
(setq tp1(cdr tp))
(if (= tp1 "LINE") 
(command "insert" "bad" "8.65,6" "" "" "");then
(progn(setq text (cddr (entget na)));else
      (setq mtl2 (list (strcase (cdr (assoc 1 text)))))
      (if (or(= (car mtl2) "ABSOLUTE")
             (= (car mtl2) "POLAR")
             (= (car mtl2) "RELATIVE")
            )
          (command "insert" "ok" "8.65,6" "" "" "")   
           (command "insert" "bad" "8.65,6" "" "" ""));else
      ))
)
; last answer
(defun a3 (/ setc n na tp tp1)
(setq setc (ssget '(1 5.6)))
(setq n (sslength setc))
(setq na (ssname setc 0))
(setq tp (car(cdr(entget na))));next three lines check for no answer
(setq tp1(cdr tp))
(if (= tp1 "LINE") 
(command "insert" "bad" "9.2,6" "" "" "");then
(progn(setq text (cddr (entget na)));else
      (setq mtl3 (list (strcase (cdr (assoc 1 text)))))
      (if (or(= (car mtl3) "ABSOLUTE")
             (= (car mtl3) "POLAR")
             (= (car mtl3) "RELATIVE")
            )
          (command "insert" "ok" "9.2,6" "" "" "")   
           (command "insert" "bad" "9.2,6" "" "" ""));else
      ))
)
(a1)
(a2)
(a3)
(REDRAW)
(Q3)
)             

(defun Q3 ()
(setq ans3 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #3 <T/F>")))
(if (= ans3 "T") (PROGN(command "text" "1,4.5" "" "T" )
          (command "insert" "ok" "8.08,5.5" "" "" ""))   
           (PROGN(command "text" "1,4.5" "" "F" )
           (command "insert" "bad" "8.08,5.5" "" "" "")))
(REDRAW)
(q4)
);end defun3

(defun q4 ()
(prompt "\nPLEASE READ QUESTION #4 BEFORE ANSWERING")
(setq ans4 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #4 <T/F>")))
(if (= ans4 "T") (PROGN(command "text" "1,4" "" "T" )
          (command "insert" "ok" "8.08,5" "" "" ""))   
           (PROGN(command "text" "1,4" "" "F" )
           (command "insert" "bad" "8.08,5" "" "" "")))
(REDRAW)
(q5)
);end defun4

(defun q5 ()
(prompt "\nPLEASE READ QUESTION #5 BEFORE ANSWERING")
(setq ans5 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #5" )))
(if (= ans5 "LAYER") (PROGN(command "text" "1,3.5" "" ans5 )
          (command "insert" "ok" "8.08,4.5" "" "" ""))   
           (PROGN(command "text" "1,3.5" "" ans5 )
           (command "insert" "bad" "8.08,4.5" "" "" "")))
(REDRAW)
(q6)
);end defun5

(defun q6 ()
(prompt "\nPLEASE READ QUESTION #6 BEFORE ANSWERING")
(setq ans6 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #6 <T/F>")))
(if (= ans6 "F") (PROGN(command "text" "4.8,6" "" "F" )
          (command "insert" "ok" "8.08,4" "" "" ""))   
           (PROGN(command "text" "4.8,6" "" "T" )
           (command "insert" "bad" "8.08,4" "" "" "")))
(REDRAW)
(q7)
);end defun6

(defun q7 ()
(prompt "\nPLEASE READ QUESTION #7 BEFORE ANSWERING")
(setq ans7 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #7 ")))
(if (= ans7 "C") (PROGN(command "text" "4.8,5.3" "" ans7 )
          (command "insert" "ok" "8.08,3.5" "" "" ""))   
           (PROGN(command "text" "4.8,5.3" "" ans7 )
           (command "insert" "bad" "8.08,3.5" "" "" "")))
(REDRAW)
(q8)
);end defun7

(defun q8 ()
(prompt "\nPLEASE READ QUESTION #8 BEFORE ANSWERING")
(setq ans8 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #8 <T/F>")))
(if (= ans8 "F") (PROGN(command "text" "4.8,4.5" "" "F" )
          (command "insert" "ok" "8.08,3" "" "" ""))   
           (PROGN(command "text" "4.8,4.5" "" "T" )
           (command "insert" "bad" "8.08,3" "" "" "")))
(REDRAW)
(q9)
);end defun8

(defun q9 ()
(prompt "\nPLEASE READ QUESTION #9 BEFORE ANSWERING")
(setq ans9 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #9" )))
(if (= ans9 "B") (PROGN(command "text" "4.8,3.9" "" ans9 )
          (command "insert" "ok" "8.08,2.5" "" "" ""))   
           (PROGN(command "text" "4.8,3.9" "" ans9 )
           (command "insert" "bad" "8.08,2.5" "" "" "")))
(REDRAW)
(q10)
);end defun9

(defun q10 ()
(prompt "\nPLEASE READ QUESTION #10 BEFORE ANSWERING")
(setq ans10 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #10 <T/F>")))
(if (= ans10 "F") (PROGN(command "text" "4.8,3.1" "" "F" )
          (command "insert" "ok" "8.08,2" "" "" ""))   
           (PROGN(command "text" "4.8,3.1" "" "T" )
           (command "insert" "bad" "8.08,2" "" "" "")))
(REDRAW)
(Q11)
);end defun10

(defun q11 ()
(prompt "\nPLEASE READ QUESTION #11 BEFORE ANSWERING")
(setq ans11 (strcase(getstring "\nENTER YOUR ANSWER TO QUESTION #11" )))
(if (OR (= ans11 "A")
        (= ans11 "B")
        (= ans11 "C")
    )    
(command "insert" "closing" "5.23,1.47" "" "" ""))   
(REDRAW)
(command "end" )
);end defun11

(defun c:ldl ()
(load"lesson1")
)
(defun c:el ()
(command "blue" "lesson1.lsp" )
)
