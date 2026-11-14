;get orientation of 2D polyline segment for labeling
(defun pinpnt ( / ttldis reman acdist tandis nxtvtx lvtx nowvtx)

   (command "area" "entity" elun)
   (setq ttldis (getvar "perimeter"))
   (command "break" elun "F" (last elun) "@")
   (command "area" "entity" "last")
   (setq reman (getvar "perimeter"))
   (command "undo" "2")
   (setq dtopt (- ttldis reman))

   (setq nxtvtx (entget (entnext (cdr (assoc -1 (entget (cdr (assoc -1 olst)))))))
         lvtx (cdr (assoc 10 nxtvtx))
         nxtvtx (entget (entnext (cdr (assoc -1 nxtvtx)))) 
         nowvtx (cdr (assoc 10 nxtvtx))
         acdist (distance lvtx nowvtx)
   );end setq
           
   (while (< acdist dtopt)
      (setq lvtx nowvtx
            nxtvtx (entget (entnext (cdr (assoc -1 nxtvtx))))  
            nowvtx (cdr (assoc 10 nxtvtx))
            tandis (distance lvtx nowvtx)
            acdist (+ acdist tandis)
      );end setq
   );end while           

   ;if the point selected on the polyline is coincidental with a
   ;vertex of the polyline, the distance and bearing use the 
   ;coincidental coordinate as the ahead point when labeling

   (setq point1 lvtx
         point2 nowvtx
   );end setq   
    
);end function pinpnt

;this subroutine was scabbed from chgtext.lsp - Autodesk R11
(defun chgdha ()
           
  (setq e (entget (entlast)))
  (setq osl (strlen (setq os "d")))
  (setq nsl (strlen (setq ns "%%d")))

  (setq chf nil si 1)
  (setq s (cdr (setq as (assoc 1 e))))
            (while (= osl (setq sl (strlen
                             (setq st (substr s si osl)))))
                  (if (= st os)
                      (progn
                        (setq s (strcat (substr s 1 (1- si)) ns
                                        (substr s (+ si osl))))
                        (setq chf t) ; Found old string
                        (setq si (+ si nsl))
                      )
                      (setq si (1+ si))
                  )
               )
               (if chf (progn        ; Substitute new string for old
                  (setq e (subst (cons 1 s) as e))
                  (entmod e)         ; Modify the TEXT entity
               ))

);end function chgdha



(defun c:op ( / distnc point1 point2 pangl tar dolabl)

;no null input = 1
(initget 1)

(setq elun (entsel "\nSelect position and entity... ")
      untipo (cdr (assoc 0 (setq olst (entget (nth 0 elun)))))
);end setq

(cond
  ( (eq untipo "LINE")
    (setq point1 (cdr (assoc 10 olst))
          point2 (cdr (assoc 11 olst))
          doflag 1
    );end setq
  );end case
  ( (eq untipo "INSERT")
    (setq point1 (cdr (assoc 10 (entget (nth 0 elun)))))
    (setq point2 (getpoint point1 "\nSelect the second point: ")
          doflag 1
    );end setq
  );end case
  ( (eq untipo "POLYLINE")
    (if (eq (cdr (assoc 70 olst)) 8)
        (progn (princ "\n ...3D Polyline invalid... ") (setq doflag  0) )
        (progn (pinpnt) (setq doflag 1))
    );end if
  );end case
);end cond

(cond 
( (eq doflag 1)
;distance for the x,y of selected points at the current ucs elevation
(setq distnc (distance
                (list (car point1) (cadr point1) )
                (list (car point2) (cadr point2) )
             );end distance
      pangl  (angtos
                (setq tar    
                     (angle 
                       (list (car point1) (cadr point1) )
                       (list (car point2) (cadr point2) )
                     );end angle
                );end setq tar
               4 4);end angtos
);end setq
(terpri)
(prompt (strcat "\n___ Plane distance: " (rtos distnc 2 3) " ___ Bearing: " pangl))

(menucmd "P9=P9ANS")
(menucmd "P9=*")

(setq dolabl (strcase (getstring "\n...Label it ? <N> :") ))

(if (or (eq dolabl "") (eq dolabl "N"))
  (terpri)
  (progn
    (princ (strcat "\nText size is set to " (rtos (getvar "textsize") 2 2 )))
    (if (eq (setq nsz (getreal "\nEnter new text size ... [ENTER] to keep: ")) nil)
       (terpri)  
       (setvar "textsize" nsz)
    );end if
    (setq wput (getpoint "\nPick point to put DISTANCE Label: "))
    (setvar "lastpoint" point1)
    (command "text" "M" wput (getvar "textsize")  (angtos tar 0 4) (rtos distnc 2 2))

    (setq wput (getpoint "\nPick point to put BEARING Label: "))
    (setvar "lastpoint" point1)
    (command "text" "M" wput (getvar "textsize")  (angtos tar 0 4) pangl )

    (chgdha)
  );end progn
);end if 
);end case
);end cond

(prin1)

);end function
