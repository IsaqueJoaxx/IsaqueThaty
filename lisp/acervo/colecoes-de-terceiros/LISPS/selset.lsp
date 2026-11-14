; SELSET.LSP

; Written by Sri Vaidyanathan
; Bechtel Corp.
; 50 Beale St., PO Box 3965,
; San Francisco, CA 94119-3965, USA
; 415-768-1234
; 11/18/91

; This program is an improvement on Autodesk's SSX.LSP program
; supplied on bonus disk.  This is an easy-to-use way of selecting entities by 
; their properties -- such as color, layer, thickness, entity-type, etc.
; Once you've selected the entities with Selset, you can act on them using any
; AutoCAD command.
;                                                     
; To reate a selection set using Selset, either type "(Selset)" at the 
; "Command:" prompt to create a "previous" selection set or type "(Selset)" 
; in response to any "Select objects:" prompt.  More than one filter criteria
; can be used at a time.

; The following features were added to the original SSX program:
; - Build the selection set from specified entities (using Window, Crossing,
;   etc.) or use all entities in the dwg .
; - Combine selection criteria using logical AND/OR operators; default is AND.
; - Specify entity type without having to keyin E first.
; - Specify color by name or number.

; s1,s2 - sel set
; n1,n2 - number of sel
; f1,f2 - filter
; e,el - entity list
; c - choice

;--------------------------- INTERNAL ERROR HANDLER --------------------------|

(defun sels-err (s)                    ; If an error (such as CTRL-C) occurs
                                      ; while this command is active...
(if (/= s "Function cancelled") (progn
    (princ (strcat "\nError: " s))
    (setq *error* olderr)             ; Restore old *error* handler
))
(princ)
)

;-------------------------------- MAIN PROGRAM -------------------------------|

(defun selset ()
(setvar "highlight" 1)
(setq olderr *error* *error* sels-err
        el (list "POINT" "LINE" "ARC" "CIRCLE" "POLYLINE" "TEXT" "ATTDEF")
        n1 0 n2 0 
        f1 nil f2 nil
        s2 (ssadd)      ;create a null sel set
        s1 (ssget)      ;prompt sel set
)

(if s1 (progn
       (setq t1 T n1 1)
       (initget "I C")
       (setq c (getkword (strcat "\nIgnore/<Choose> objects by criteria? ")))
       (if (= c "I") (setq s2 (ssget "P")))
       ) 
       (setq t1 nil s1 s2)
)

(while t1
  (setq t3 "Entity Color LAyer LType Style THickness Or ")
  (setq t3 (strcat t3 "Block Text Attdef Point Line ARc CIrcle POLyline"))
  (initget t3)
  (setq t1 (getkword (strcat "\n>" (itoa n1) 
     ">Entity/Color/LAyer/LType/Style/THickness/Or: ")))
  (setq t2
     (cond
        ((eq t1 "Block")      2)   ((eq t1 "Color")     62)
        ((eq t1 "Entity")    -1)   ((eq t1 "LAyer")      8)
        ((eq t1 "LType")      6)   ((eq t1 "Style")      7)
        ((eq t1 "THickness") 39)   ((eq t1 "Or")        99)
        ((eq t1 "Text")       0)   ((eq t1 "Attdef")     0)
        ((eq t1 "Point")      0)   ((eq t1 "Line")       0)
        ((eq t1 "ARc")        0)   ((eq t1 "CIrcle")     0)
        ((eq t1 "POLyline")   0)   (T nil)
  ))

  (if (= t2 -1) (progn
  (initget 1 "Block Text Attdef Point Line ARc CIrcle POLyline")
  (setq t1 (getkword "\n>>Entity type: "))
  (setq t2
     (cond
        ((eq t1 "Block")      2)
        ((eq t1 "Text")       0)   ((eq t1 "Attdef")     0)
        ((eq t1 "Point")      0)   ((eq t1 "Line")       0)
        ((eq t1 "ARc")        0)   ((eq t1 "CIrcle")     0)
        ((eq t1 "POLyline")   0)   (T nil)
  ))
  ))

  (initget 1 "?")
  (cond
     ((= t2  0) (setq t3 t1))
     ((= t2  2)  (while 
                    (or (= (setq t3 (getstring "\n>>Block name/<?>: ")) "?")
                        (= t3 ""))
                    (command "block" "?" (command))
                    (initget "?")
                 ))
     ((= t2 62)  (setq t3 nil)
                 (while (null t3)
                    (initget 5 "? BYBlock Red Yellow Green Cyan Blue Magenta White BYLayer")
                    (setq t3 (getint "\n>>Color / <?>: "))
                    (if (or (equal t3 "?")
                        (and (equal (type t3) 'INT) (> t3 256))
                        )
                        (progn
                        (setq t3 nil)
                        (textscr)
       (princ "\n                                                     ")
       (princ "\n                 Color number   |   Standard meaning ")
       (princ "\n                ________________|____________________")
       (princ "\n                                |                    ")
       (princ "\n                       0        |      <BYBLOCK>     ")
       (princ "\n                       1        |      Red           ")
       (princ "\n                       2        |      Yellow        ")
       (princ "\n                       3        |      Green         ")
       (princ "\n                       4        |      Cyan          ")
       (princ "\n                       5        |      Blue          ")
       (princ "\n                       6        |      Magenta       ")
       (princ "\n                       7        |      White         ")
       (princ "\n                    8...255     |      -Varies-      ")
       (princ "\n                      256       |      <BYLAYER>     ")
       (princ "\n                                               \n\n\n")
                    ))
                 ) ;end while
                (setq t3
                  (cond
                    ((eq t3 "BYBlock") 0)   ((eq t3 "Cyan")    4)
                    ((eq t3 "Red")     1)   ((eq t3 "BYLayer") 256)
                    ((eq t3 "Yellow")  2)   ((eq t3 "Blue")    5)
                    ((eq t3 "Green")   3)   ((eq t3 "Magenta") 6)
                    ((eq t3 "White")   7)   (T t3)
                )))

     ((= t2  8)  (while 
                    (or (= (setq t3 (getstring "\n>>Layer name/<?>: ")) "?")
                        (= t3 ""))
                    (command "layer" "?" "" "")
                    (initget "?")
                 ))
     ((= t2  6)  (while 
                    (or (= (setq t3 (getstring "\n>>Linetype name/<?>: ")) "?")
                        (= t3 ""))
                    (command "linetype" "?" "" "")
                    (initget "?")
                 ))
     ((= t2  7)  (while 
                    (or (= (setq t3 (getstring "\n>>Text style name/<?>: ")) "?")
                        (= t3 ""))
                    (command "style" "?" (command))
                    (initget "?")
                 ))
     ((= t2 39)  (setq t3 (getreal   "\n>>Thickness: ")))

     ((= t2 99)  (if f1 (sels-do))  ;call func
                 (setq f1 nil n1 (+ n1 1) t3 nil t1 T))

     (T (setq t3 nil))
  ) ;end cond

  (if t3 (progn 
         (if (= (type t3) 'STR) (setq t3 (strcase t3)))
         (setq f1 (cons (cons t2 t3) f1))       ;filter list
  ))

) ;end while

(if f1 (sels-do))  ;call func

(if (null f2) (setq s2 s1))  ;no criteria specified
(command "select" s2 "")     ;make sel set prev
(setq n2 (sslength s2) s1 nil s2 nil)
(setq *error* olderr)
(if (> n2 0) (progn
 (prompt "\nClick or press any key to continue")
 (setq t3 (getvar "cmdecho"))
 (setvar "cmdecho" 0)
 (command "list" "p" (setq c (grread)) (command))       ;hilite
 (setvar "cmdecho" t3)
 (if (= (car c) 6) (grread)) ;ignore Button menu click
))
(princ)
) ;end func


; make sel set 

(defun sels-do ()
(setq t2 0 f2 T)  ;flag criteria processing

(while (< t2 (sslength s1))  ;go thru sel set
       (setq e (entget(ssname s1 t2)) t3 0)

       (while (< t3 (length f1))  ;go thru filter list
              (setq t1 (nth t3 f1))
              (if (or (member t1 e)  ;match filter & elem lists
                      (and (equal t1 (cons 6 "BYLAYER")) (null (assoc 6 e)))
                      (and (equal t1 (cons 62 256)) (null (assoc 62 e)))
                  )
                  (setq t3 (+ t3 1))
                  (setq e nil t3 (length f1))
              ) ;end if mem
       ) ;end wh t3

       (setq t3 (cdr(assoc -1 e)))
       (if t3
        (if (= c "I")
         (if (ssmemb t3 s2) (progn  ;if exist in sel set remove
            (ssdel t3 s2)
            (setq n2 (+ n2 1))
         ))
         (if (not (ssmemb t3 s2)) (progn  ;if not exist in sel set add
            (ssadd t3 s2)
            (setq n2 (+ n2 1))
         ))
       )) ;end if t3        

       (setq t2 (+ t2 1))
) ;end while t2

(setq t3 nil)
) ;end func

;-------------------------- ADD AND REMOVE FUNCTIONS -------------------------|

;(defun r() (command "r") (ssx))
;(defun a() (command "a") (ssx))

