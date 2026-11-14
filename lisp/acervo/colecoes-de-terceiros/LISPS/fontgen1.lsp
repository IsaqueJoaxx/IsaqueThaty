;
;                   FONTGEN.LSP
;   
;              AUTOCAD FONT GENERATOR
;
;        copyright  1991,1992 - Keith P. Whitaker
;
;     
(setvar "cmdecho" 0)
(command "vslide" "fnt1")
(defun c:fontgen ()
;-- INITIALIZATION --
(setvar "cmdecho" 0)
(setq ans "O")
(princ "\n")
(princ "\n            Font Generator - Version 1.0")
(princ "\n")
;-- INPUT FILE DATA SECTION --
(setq fn (getstring "\nOutput File Name (no extention): "))
(setq fnn (strcat fn ".shp"))
;CHECK FOR EXISTING FILE
(setq f (findfile fnn))
(if f (setq ans (getstring "\nFont File Exists! (O)verwrite/(A)ppend/(E)dit/(Q)uit : ")))
(if (= ans "A")(setq f1 (open fnn "a")))
(if (= ans "O")(setq f1 (open fnN "w")))
(if (= ans "E")
   (progn
      (setq ans "A")
      (setq scn (getstring "\nCharacter Number to Replace: "))
      (SETQ F1 (OPEN FNN "r"))
      (setq f2 (open (strcat fn ".tmp") "w"))
      (setq lt (read-line f1))
      (setq count 0)
      (while lt
         (if (= (substr lt 1 1) "*")
            (progn
               (setq tr 3 CN "")
               (setq ct (substr lt 2 1))
               (while (/= "," ct)
                  (setq cn (strcat cn ct))
                  (setq ct (substr lt tr 1))
                  (setq tr (+ 1 tr))
               )
               (if (= cn scn)
                  (progn
                     (setq lt (read-line f1)
                           count (- count 1)
                     )
                     (while (/= (substr lt (strlen lt) 1) "0")
                        (setq lt (read-line f1))
                     )
                  )
                  (write-line lt f2)
               )
            )
            (write-line lt f2)
         )
         (setq count (+ count 1))
         (setq lt (read-line f1))
      )
      (close f1)
      (close f2)
      (setq f1 (open (strcat fn ".tmp") "r"))
      (setq f2 (open fnn "w"))
      (repeat COUNT
         (write-line (READ-LINE F1) F2)
      )
      (close f1)
      (setq f1 F2)
   )
)
(if (= ans "Q")()
(progn
;
;-- append section
;
(if (= ans "A")()
;--overwrite or new --- 
   (progn
      (princ (strcat "*0,4," fn) f1)     ;font file header
      (princ "\n100,50,2,0" f1)         
      (princ "\n*10,5,cr" f1)            ;carrage return
      (princ "\n2,8,0,-120,0" f1)
      (princ "\n*32,5,sp" f1)            ;space
      (princ "\n2,8,75,0,0\n" f1)
   )
)
;-- INPUT CHARACTERS --
(prompt "\nSelect first letter: ")
(setq lset (ssget))
(while lset
   (setq ip2 (getpoint "\nInsertion Point: "))
   (SETQ EP (GETPOINT "\nEnding Point: "))
   (setq n2$ (STRCASE (getstring "\nName of Letter: ") T))
   (IF CN$ (SETQ OCN$ CN$)(SETQ OCN$ "32"))
   (SETQ OCN$ (RTOS (+ (READ OCN$) 1) 2 0))
   (SETQ CN$ (GETSTRING (STRCAT "\nCharacter Number <" OCN$ ">: ")))
   (IF (= CN$ "")(SETQ CN$ OCN$))
   (setq l1 "2")
   (SETQ OP1 IP2)
   (setq r 0 nb 2)
   (while (< r (sslength lset))
      (setq en1 (entget (ssname lset r)))
;
;   ------ polyline segments ---
;
      (if (= (cdr(assoc 0 en1)) "POLYLINE")
          (progn
             (setq lp2 nil)
             (setq pen2 (entnext (ssname lset r)))
             (setq pent2 (entget pen2))             
             (while (= (cdr(assoc 0 pent2)) "VERTEX")
                (if lp2 
                   (progn
                      (setq cp1 (cdr(assoc 10 pent2))
                            blg (* 127 (cdr(assoc 42 pent2)))
                            dx1 (- (car cp1) (car lP2))
                            dy1 (- (cadr cp1) (cadr lP2))
                      )
                      (if (or (> dx1 127)(> dy1 127))(prompt "Line Length or Displacement Exceeds 127 units... Skipping Invalid Entry...")
                          (setq l1 (strcat l1 ",1,0C," (rtos dx1 2 0)","(rtos dy1 2 0)","(rtos blg 2 0)",2")
                                nb (+ nb 6))
                      )
                   )
                   (setq cp1 (cdr(assoc 10 pent2))
                         dx1 (- (car cp1) (car op1))
                         dy1 (- (cadr cp1) (cadr op1))
                         l1 (strcat l1 ",8," (rtos dx1 2 0)","(rtos dy1 2 0))
                         nb (+ nb 3)
                   )     
                )
                (setq lp2 cp1)
                (setq pen2 (entnext pen2))
                (setq pent2 (entget pen2))
             )
             (SETQ OP1 lP2)
          )
       )
;
;   ----- arc segments -----
;
      (if (= (cdr(assoc 0 en1)) "ARC")
          (progn
             (setq cp1 (cdr(assoc 10 en1))
                   rd (cdr(assoc 40 en1))
                   a1 (cdr(assoc 50 en1))
                   a2 (cdr(assoc 51 en1))
                   p1 (polar cp1 a1 rd)
                   p2 (polar cp1 a2 rd)
                   dx1 (- (car p1) (car OP1))
                   dy1 (- (cadr p1) (cadr OP1))
                   dx2 (- (car p2) (car p1))
                   dy2 (- (cadr p2) (cadr p1))
                   blg (* (1- (abs(car(polar (list 0 0) (abs (/ (- a2 a1) 2)) 1)))) -127)
            )
            (if (or (> dx1 127)(> dy1 127)(> dx2 127)(> dy2 127))(prompt "Line Length or Displacement Exceeds 127 units... Skipping Invalid Entry...")
                (setq l1 (strcat l1 ",8," (rtos dx1 2 0)","(rtos dy1 2 0) ",1,0C," (rtos dx2 2 0)","(rtos dy2 2 0)","(rtos blg 2 0)",2")
                      nb (+ nb 9))
            )
            (SETQ OP1 P2)
          )
     )
;
;   ----- line segments ----
;
      (if (= (cdr(assoc 0 en1)) "LINE")
          (progn
             (setq p1 (cdr(assoc 10 en1))
                   p2 (cdr(assoc 11 en1))
                   dx1 (- (car p1) (car OP1))
                   dy1 (- (cadr p1) (cadr OP1))
                   dx2 (- (car p2) (car p1))
                   dy2 (- (cadr p2) (cadr p1))
            )
            (if (or (> dx1 127)(> dy1 127)(> dx2 127)(> dy2 127))(prompt "Line Length or Displacement Exceeds 127 units... Skipping Invalid Entry...")
                (setq l1 (strcat l1 ",8," (rtos dx1 2 0)","(rtos dy1 2 0) ",1,8," (rtos dx2 2 0)","(rtos dy2 2 0)",2")
                      nb (+ nb 8))
            )
            (SETQ OP1 P2)
          )
     )
;
     (setq r (+ 1 r))
   )
   (SETQ DX (- (CAR EP) (CAR OP1))
         DY (- (CADR EP) (CADR OP1))
   )
   (SETQ L1 (STRCAT L1 ",8," (RTOS DX 2 0) "," (RTOS DY 2 0)))
   (SETQ NB (+ NB 3))
   (setq l1 (strcat l1 ",0"))
;-- PRINT CHARACTER TO FILE --   
   (princ (strcat "*" cn$ "," (rtos nb 2 0) "," n2$) f1)
   (setq r 1)
   (setq sl (strlen l1))
   (while (< r sl)
      (setq nc (- sl r))
      (if (> nc 30)
          (progn
             (setq nc 30)
             (setq tc (substr l1 (+ r (- nc 1)) 1))
             (while (/= tc ",")
                 (setq nc (- nc 1))
                 (setq tc (substr l1 (+ r (- nc 1)) 1))
             )
          )
          (SETQ NC (+ 1 NC))
      )
      (setq lt (substr l1 r nc))
      (princ (strcat "\n" lt) f1)
      (setq r (+ nc r))
   )
   (PRINC "\n" F1)
   (prompt "\nSelect Next Letter: ")
   (setq lset (ssget))
)
;-- END FILE --
(close f1)
;-- script file to create font --
(setq f2 (open "fontgen.scr" "w"))
(princ "END" f2)
(princ "\n7" f2)
(princ (strcat "\n" fn) f2)
(princ "\n" F2)
(princ "\n2" f2)
(princ "\n" f2)
(princ "\n" f2)
(close f2)
;-- run optional script file --
(prompt "\nShape File Creation Complete........")
(setq ans (getstring "\nCompile Text Font (<Y>/N): "))
(if (= ans "N")()(command "script" "fontgen"))
))
)
(getstring "\nPress <RET> to continue.....")
(command "redraw")
