;TIP894:  CLG.LSP  Center Line of Gravity   (C)1993, Richard Scott Woodhall
;        Usage : clg
;        This functions calculates the accumulated
;        centroid of mass of one or many plines se-
;        lected by the user.

(defun c:clg (/ i _Mx _My m ss ename value p a mi my mx x y oldpdmode oldpdsize)

   ; Initialize variables.

   (setq i 0
         _Mx 0
         _My 0
         m 0
   )

   ; Select objects.

   (princ "\nSelect all masses ...\n")
   (setq ss (ssget))

   ; Loop invariant : repeat for all objects in
   ; selection set.

   (repeat (sslength ss)
      (setq ename (ssname ss i))

      (redraw ename 3)    ; Highlight object.

      (setq value (centroid ename)    ; Calculate centroid of object.
            p (getreal "\nEnter density of highlighted mass <ie. thickness>: ")
            a (caddr value)
            mi (* p a)
            my (* mi (car value))
            mx (* mi (cadr value))
            _Mx (+ _Mx mx)    ; Sum moments about the x-axis.
            _My (+ _My my)    ; Sum moments about the y-axis.
            m (+ m mi)    ; Sum masses.
            i (1+ i)    ; Increment i.
      )

      (redraw ename 4)    ; De-highlight object.
   )

   ; Calculate accumulated centroid.

   (setq x (/ _My m)    
         y (/ _Mx m)
   )

   ; Save current system variables.

   (setq oldpdmode (getvar "PDMODE")
         oldpdsize (getvar "PDSIZE")
   )

   ; Set system variables.

   (setvar "CMDECHO" 0)
   (setvar "PDMODE" 35)
   (setvar "PDSIZE" (* (getvar "LTSCALE") 3))

   ; Draw a point at centroid and print x,y value.

   (command "POINT" (list x y))
   (princ "\nCentroid of mass is located at coordinate ")
   (princ x)
   (princ ",")
   (princ y)
   (princ ".\n")

   ; Reset system variables to original values.

   (setvar "PDMODE" oldpdmode)
   (setvar "PDSIZE" oldpdsize)

   (princ)

)

;        Usage : (fline x x1 y1 x2 y2)
;        This function returns the y value of a
;        line for every x value passed it.

(defun fline (ui x1 y1 x2 y2)
   (+ (* (/ (- y1 y2) (- x1 x2)) (- ui x2)) y2)
)

;        Usage : (centroid ename)
;        This function calculates the centroid of
;        mass of a pline object.

(defun centroid (ename / n e xbuf ybuf m _Mx _My _Dx ui xi yi mi x y x1 y1 x2 y2)

   ; Initialize variables.

   (setq n 100    ; Set number of iterations for summation.
         e (entget (entnext (cdr (assoc -1 (entget ename)))))
         x1 (cadr (assoc 10 e))
         y1 (caddr (assoc 10 e))
         xbuf x1
         ybuf y1
         e (entget (entnext (cdr (assoc -1 e))))
         x2 (cadr (assoc 10 e))
         y2 (caddr (assoc 10 e))
         m 0
         _Mx 0
         _My 0
   )

   ; Loop invariant : while not the end of the pline sequence.

   (while (/= "SEQEND" (cdr (assoc 0 e)))
      (setq _Dx (/ (- x2 x1) n)
            ui x1
      )

      (if (/= x1 x2)    ; Check that pline segment is a true function.

         ; Loop invariant : repeat n number of times.

         (repeat n
            (setq xi (+ ui (* 0.5 _Dx))
                  yi (fline xi x1 y1 x2 y2)
                  mi (* _Dx yi)
                  m (+ m mi)    ; Sum masses of rectangles.
                  _Mx (+ _Mx (* mi yi 0.5))    ; Sum moments about the x-axis.
                  _My (+ _My (* mi xi))    ; Sum moments about the y-axis.
                  ui (+ _Dx ui)    ; Increment ui.
             )
          )    ; End repeat.

      )    ; End if.

      (setq x1 x2
            y1 y2
            e (entget (entnext (cdr (assoc  -1 e))))    ; Increment e to next
                                                        ; vertex.
            x2 (cadr (assoc 10 e))
            y2 (caddr (assoc 10 e))
      )

   )    ; End while.

   (setq x2 xbuf
         y2 ybuf
         _Dx (/ (- x2 x1) n)
         ui x1
   )

   (if (/= x1 x2)    ; Check that pline segment is a true function.

      ; Loop invariant : repeat n number of times.

      (repeat n
         (setq xi (+ ui (* 0.5 _Dx))
               yi (fline xi x1 y1 x2 y2)
               mi (* _Dx yi)
               m (+ m mi)    ; Sum masses of rectangles.
               _Mx (+ _Mx (* mi yi 0.5))    ; Sum moments about the x-axis.
               _My (+ _My (* mi xi))    ; Sum moments about the y-axis.
               ui (+ _Dx ui)    ; Increment ui.
         )
      )    ; End repeat.

   )    ; End if.

; Calculate accumulated centroid.

   (setq x (/ _My m)
         y (/ _Mx m)
   )

   ; Return a list to calling function.

   (list x y (abs m))

)

