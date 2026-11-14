;*****************************************************************************
;*        LARC.LSP                              John Watson    11/26/91      *
;*   Allows user to pick a circle or arc, then inserts leader at proper      *
;*   angle toward center of arc. This allows for 2 part leader only.         *
;*****************************************************************************
;****************************
;*  Standard Error Handler  *
;****************************
(defun CLERR (s)
                                  ; If an error (such as CTRL-C) occurs
   (princ (strcat "\nErroR: " s)) ; while this command is active...
   (setvar "BLIPMODE" 1)
   (setvar "CMDECHO" 1)
   (setvar "Orthomode" os)
   (setq *error* olderr)             ; Restore old *error* handler
   (princ)
)

(defun c:larc(/ starta enda olderr axsiz aysiz en cpt rpt enpt
             pt1 pt2 pt3 pt4 ang ang2 flag)
 (setq olderr  *error*              ;set error handler to our 
       *error* clerr)               ;  custom one
 (setq os(getvar "Orthomode"))
 (setvar "CMDECHO" 0)
 (setq axsiz (* (getvar "DIMASZ")(getvar "DIMSCALE"))
       aysiz (/ axsiz 6)
       en(entget (car (entsel "\nPick circle or arc:"))))
 (while (and
          (/= (cdr(assoc 0 en)) "CIRCLE")
          (/= (cdr(assoc 0 en)) "ARC")
         )
  (setq en(entget (car (entsel "\nPick circle or arc:"))))
 )
 (setvar "orthomode" 0)
 (setvar "BLIPMODE" 0)
; (command "DIM1" "CENTER" en)
 (setq cpt (cdr(assoc 10 en)))
 (cond ((= (cdr(assoc 0 en)) "ARC")    ;If it's an arc, we must be certain that
         (setq starta(cdr(assoc 50 en))  ;we place the leader touching the arc.
               enda  (cdr(assoc 51 en))  ;The following accomplishes that.
               flag T)
         (while flag
          (initget 1)
          (setq endpt(getpoint cpt "\nSelect end point of leader: ")) 
          (setq ang(angle cpt endpt))
          (if (< enda starta) ;the arc passes thru 2ã radians beyond 0 again
           (progn
            (if (and(< ang starta)(> ang enda))
             (setq rpt(polar cpt(+ ang 3.14159)(cdr(assoc 40 en))))
             (setq rpt(polar cpt ang (cdr(assoc 40 en))))
            );end if
            (setq ang2(angle cpt rpt))
            (if (or(< ang2 enda)(> ang2 starta))
              (setq flag nil)
              (prompt "\nInvalid endpoint!")
            )
           );end progn
           (progn          ;the arc starts and ends before 2ã radians
            (if (or (< ang starta)(> ang enda))
             (setq rpt(polar cpt(+ ang 3.14159)(cdr(assoc 40 en))))
             (setq rpt(polar cpt ang (cdr(assoc 40 en))))
            );end if
            (setq ang2(angle cpt rpt))
            (if (and(< ang2 enda)(> ang2 starta))
              (setq flag nil)
              (prompt "\nInvalid endpoint!")
            )
           );end progn
          );end if < enda starta
         );end while
        );end ARC

        ((= (cdr(assoc 0 en)) "CIRCLE") ;if a circle place wherever user wants
          (initget 1)
          (setq endpt(getpoint cpt "\nSelect end point of leader: ")) 
          (setq ang(angle cpt endpt)
                rpt(polar cpt ang (cdr (assoc 40 en))))
        );end CIRCLE
        (T nil)
 );end cond

 (command "line" rpt endpt "") 
 (setq pt4 (polar rpt ang axsiz)
       pt2(polar pt4 (+ ang 1.57079) aysiz)
       pt3(polar pt4 (- ang 1.57079) aysiz))
 (command "SOLID" pt3 pt2 rpt "" "")
 (prompt "\nFinish the leader horizontally: ")
 (setvar "orthomode" 1)
 (command "LINE" endpt pause "")
 (command)
 (prompt "\nEnter text line(s): ")
 (setq enl(entget(entlast)))
 (setq startx(cadr(assoc 10 enl))
       endx(cadr(assoc 11 enl))
       ytxt(- (caddr(assoc 10 enl))(* 0.4 (getvar "dimtxt"))))
 (if (> startx endx) ;determine if we're toward the left or the right.
     (progn
      (setq tpt (list(- endx (* 1.0 (getvar "dimtxt"))) ytxt))
      (command "dtext" "R" tpt "" "0")
      (princ))
     (progn
      (setq tpt (list(+ endx (* 1.0 (getvar "dimtxt"))) ytxt))
      (command "dtext" tpt "" "0")
      (princ))
 )
 (setvar "BLIPMODE" 1)
 (setvar "CMDECHO" 1)
 (setvar "orthomode" os)
 (setq *error* olderr)
 (princ)
)
