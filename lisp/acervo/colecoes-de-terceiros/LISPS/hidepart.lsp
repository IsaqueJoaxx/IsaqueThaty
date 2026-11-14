; HIDEPART.LSP
;(C) 1987 by Looking Glass Microproducts
; ENTITY PART CHANGE TO HIDDEN LAYER
; - - - - - - - - - - - - - - ERROR HALT
(defun *error* (s)
 (princ (strcat "\n" s))
 (setvar "blipmode" bm)
 (princ))
; - - - - - - - - - - - -  ITEM FUNCTION
(defun item (inum elist)
   (cdr (assoc inum elist)))
; - - - - - - - - - - - HIDEPART COMMAND
(defun c:hidepart ( /
  esel ename ent epoint etype elayer
  hlayer version two-pi p1 p2 cen rad
  a1 a2 ae tp p1a p1b)
 (setq
   esel (entsel "\nSelect object: ")
   bm   (getvar "blipmode"))
 (if esel
  (progn
   (setvar "cmdecho" 0)
   (setq
     ename  (car esel)
     ent    (entget ename)
     epoint (cadr esel)
     etype  (item 0 ent)
     elayer (item 8 ent)
     hlayer (strcat "HIDDEN" elayer)
     version (atof (substr (ver) 18))
     two-pi (+ pi pi))
   (if (member etype '("LINE" "CIRCLE"
      "ARC" "POLYLINE"))
    (progn
     (redraw ename 3)
     (if (setq p1 (getpoint 
               "\nEnter first point: "))
       (progn
; - - - - - - - - -  CREATE HIDDEN LAYER
        (if (or (< version 9.0)
            (null (tblsearch "layer"
            hlayer)))
         (command
             "layer" "new" hlayer ""))
; - - - - - - - - PROCESS BY ENTITY TYPE
        (if (= etype "CIRCLE")
         (progn
          (while
           (null (setq p2 (getpoint
             "\nEnter second point:"))))
          (setq
           cen (item 10 ent)
           rad (item 40 ent)
           a1  (angle cen p1)
           a2  (angle cen p2)
           ae  (angle cen epoint))
          (if (< a2 a1)
           (setq a2 (+ a2 two-pi)))
          (if (< ae a1)
           (setq ae (+ ae two-pi)))
          (if (not (< a1 ae a2))
; - - - - - - - - - - - - - -  SWAP ENDS
           (setq
            tp p1
            p1 p2
            p2 tp))
; - - - - - - - - - - - TAKE A PIECE OUT
          (setq
           p1a (polar cen (angle cen p1)
                rad)
           tan (+ (angle cen p1)
               (* 0.5 pi))
           p1b (polar p1a tan 1.0e-6))
          (command
           "break" epoint "f" p1a p1b
           "break" epoint "f" p2 p2)
; - - - - - - - - - - -  AND PUT IT BACK
          (setq   
           ename (ssname (ssget
                  epoint) 0)
           ent (entget ename)
           ent (subst (cons 50
               (angle cen p1a))
               (assoc 50 ent) ent)))
; - - - - - - - - - - - -IF NOT A CIRCLE
        (progn
         (if (setq p2 (getpoint
              "\nEnter second point: "))
          (command
            "break" epoint "f" p1 p1
            "break" epoint "f" p2 p2)
          (command
            "break" epoint "f" p1 p1))
; - - - - - - - - -  GET THE NEW SEGMENT
          (setq
           ename (ssname
                 (ssget epoint) 0)
           ent   (entget ename))))
; - - - - - - - - - HIDE THE NEW SEGMENT
        (setq
          ent (subst (cons 8 hlayer)
           (assoc 8 ent) ent))
        (entmod ent))
      (redraw ename 4)))
; - - - - - NO POINT PICKED, DEHIGHLIGHT
    (princ "\nNot a Line, Circle, Arc
     or Polyline."))))
 (princ))

(princ "\nHidePart.LSP ")
(princ "\n(C) 1987 by ")
(princ "Looking Glass Microproducts\n")
(c:hidepart)

