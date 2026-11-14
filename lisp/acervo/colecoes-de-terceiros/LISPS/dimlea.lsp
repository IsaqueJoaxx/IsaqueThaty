;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; CADENCE    PROGRAMMER's TOOLBOX
;; MAY 1994   W.Kramer
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun C:DIMLEA ( / P1 P2 AA RR TXS TX)
  (setvar "CMDECHO" 0)
  ;;can set layer and textsize here if needed
  ;;
  (setq P1 
    (getpoint "\nLocate leader start: "))
  (if P1 (progn
     ;;
     ;;Operator input of leader line points
     ;;and generation of leader arrow plus lines
     (DIMLEA1)
     ;;
     ;;Operator input of text data
     ;;creates list of text plus sizing info
     (DIMLEA2)
     ;;
     ;;Reset location of box corner to avoid
     ;;other graphic elements in drawing.
     ;;P1 is upper left corner of box.
     (setq P1 (DIMLEA3))
     ;;
     ;;Draw Outline box for text as PLINE
     ;;using P1 and P2.
     (DIMLEA4)
     ;;
     ;;Place text inside the box.
     (DIMLEA5)
  )) ;;end (IF (PROGN
  (princ)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;DIMLEA1 draws leader line and arrow based on
;;operator input of a stream of points.
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DIMLEA1 ()
   ;;repeat while operator inputs points
   (while 
      (setq P2 (getpoint P1 " next point: "))
      ;;check if first time in, AA will be null
      (if (null AA) (progn
         ;;first time in, draw arrow and set
         ;;AA & RR globals.
         (setq AA (angle P1 P2)
               RR (/ ;;length of arrow side
                    (getvar "DIMASZ") 
                    0.986394) ;;cos(0.165149)
         )
         (command 
            "_SOLID" ;;draw arrow head
            P1       ;;1st point of arrow
            (polar   ;;2nd point of arrow
              P1 
              (+
                AA   ;;base angle
                0.165149) ;;roughly 9 degrees
              RR)    ;;length of side
            (polar   ;;3rd point of arrow
              P1 
              (- 
                AA   ;;base angle
                0.165149) ;;little over 9 deg
              RR)    ;;length of side
            "" ""    ;;two returns to exit SOLID
         ) ;;end (COMMAND)
      )) ;;end (IF (NULL AA) (PROGN
                     ;;
      (entmake       ;;draw line between points
        (list 
          '(0 . "LINE")
           (cons 10 P1)
           (cons 11 P2)))
      (setq P1 P2) ;;prepare for next iteration
   ) ;;end point selection (WHILE) loop
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;DIMLEA2  operator input of text strings.
;;
;;Creates list TXS (("string" Xsize Ysize)...)
;;of the input text strings.  Also sets P2 to
;;be the size of the surrounding rectangle.
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DIMLEA2 ()
  ;; Do input until empty line is entered.
  (while (/= "" 
             (setq TX (getstring 1 "\nTEXT: ")))
     (setq TXS (cons TX TXS))
  )
  (setq 
     TXS                  ;;create list TXS
         (mapcar          ;;from TXS input
           '(lambda (TX)
              (setq P2    ;;get text box size
                 (textbox (list (cons 1 TX)))) 
                          ;;
              (list       ;;create sub-list
                TX        ;;("string" X Y)
                (- (caadr P2)  ;;X sizes
                   (caar P2))
                (- (cadadr P2) ;;Y sizes
                   (cadar P2)))
            ) ;;end (LAMBDA)
           (reverse TXS)) ;;Reverse CONSed list
                          ;;
     P2                   ;;P2 is (X Y) size
                          ;;of rectangle.
         (list
          (+ 0.25         ;;add 0.25 to
             (apply       ;;largest [max]
               'max       ;;X size found
               (mapcar    ;;in TXS
                 'cadr 
                 TXS)))
          (+ 0.25         ;;add 0.25 to
             (apply       ;;the sum [+]
               '+         ;;of all Y sizes
               (mapcar    ;;in TXS and to
                 'caddr   ;;the result of
                 TXS))    ;;multiplying the
             (*           ;;the number of lines
               (length TXS) ;;by one half the
               (/           ;;the text size.
                 (getvar "TEXTSIZE") 
                 2.0))))
  ) ;;end (SETQ) of TXS & P2
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;DIMLEA3  Given the size P2, check for any
;;interferences in order to place the box.
;;The last point entered by the operator is
;;currently stored in P1.  Return a new P1 which
;;is the upper left corner of the box.
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DIMLEA3 ( / PT)
  (setq PT P1)
  (cond
    ((null (setq SS1
        (ssget "C" 
           (polar PT 0.0 0.1)
           (list 
              (+ (car PT) (car P2))
              (- (cadr PT) (cadr P2))
              0.0
           )))   )
     (setq PT PT))
    ((null (setq SS2
        (ssget "C"
           (polar PT 0.0 0.1)
           (list
              (+ (car PT) (car P2))
              (+ (cadr PT) (cadr P2))
              0.0)))   )
     (setq PT 
        (list 
          (car PT) 
          (+ (cadr PT) (cadr P2))))
    )
    ((null (setq SS3
       (ssget "C" 
         (polar PT PI 0.1)
         (list
           (- (car PT) (car P2))
           (+ (cadr P2)(cadr PT))
           0.0)))    )
     (setq PT 
       (list 
         (- (car PT) (car P2)) 
         (+ (cadr P2) (cadr PT)))))
    ((null (setq SS4
       (ssget "C" 
         (polar PT PI 0.1)
         (list
           (- (car PT) (car P2))
           (- (cadr PT) (cadr P2))
           0.0)))  )
     (setq PT 
       (list 
         (- (car PT) (car P2)) 
         (cadr PT))))
    (t
      (prompt "\nPlace text box: ")
      (command 
        "_PLINE" 
        PT 
        (polar PT 0.0 (car P2))
        (list (+ (car PT) (car P2))
              (- (cadr PT) (cadr P2)))
        (polar PT (* 1.5 PI) (cadr P2))
        "_C"
        "_MOVE" (entlast) ""
        (list 
          (+ (car PT) 
             (/ (car P2) 2.0))
          (- (cadr PT) 
             (/ (cadr P2) 2.0)))
        PAUSE
        "_ERASE" 
        (entlast) ""
      )
      (setq PT (getvar "LASTPOINT")
            PT 
             (list 
               (- (car PT) 
                  (/ (car P2) 2.0)) 
               (+ (cadr PT) 
                  (/ (cadr P2) 2.0)))
      )
    )
  )
  PT
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; DEALIM4  draw box around text region
;; P1 is upper left corner
;; P2 is X,Y size of rectangle
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DIMLEA4 ()     
  (command 
        "_PLINE" 
        P1 
        (list 
             (+ (car P1) (car P2)) 
             (cadr P1))
        (list 
             (+ (car P1) (car P2)) 
             (- (cadr P1) (cadr P2)))
        (list 
             (car P1) 
             (- (cadr P1) (cadr P2)))
        "_C"
  ) ;;end (COMMAND)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;DIMLEA5  Output text strings inside box.
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DIMLEA5 ()     
  (setq P1       ;;Compute the starting
     (list       ;;location of the text
       (+        ;;based on the corner
         (car P1) ;P1's value plus 0.125
         0.125   ;;per edge.
       ) 
       (-         ;;Y is corner less the
         (cadr P1) ;text size and 0.125 
         (getvar "TEXTSIZE") 
         0.125
       )
     )
  ) ;;end (SETQ)
  (command 
     "_TEXT"         ;;Output the first
     P1              ;;text string at P1
     ""              ;;Default Height
     0               ;;Rotation
     (caar TXS)      ;;String value
  ) ;;end (COMMAND)
  (foreach TX (cdr TXS)  ;;for each member
     (command            ;;of the remainder
        "_TEXT"          ;;of TXS, repeat the
        ""               ;;TEXT command with
        (car TX)         ;;the string.
     )
  ) 
)
