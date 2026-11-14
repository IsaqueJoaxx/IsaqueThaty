;; PXT.LSP   (c)1994, Christopher Crawford

;; pxt.lsp
;; 3d extrusion for polylines
;;
;; pxt takes two polylines and extrudes the first 
;; around the second, creating
;; a polyface mesh.
;;
;; Revision History
;; CC 01.27.94  Created routine.
;; CC 02.02.94  Shows arrow and left/right
;; CC 02.16.94  Pick starting point on cross-section. 
;; Works with closed
;; cross-sections.  Works with extrusion plines not in WCS.
;; CC 02.17.94  
;; Support for bulge-specific plines in xsdef.  Bugs introduced.
;; CC 02.17.94  
;; Reworked interface.  Now, entity must be perpendicular to a
;; reference segment on the extrusion pline.  
;; Only two selections are necessary to start the routine. 
;; Implemented via (nentselp)
;; Release 11 users will need a different version (two selects).
;; CC 02.17.94  Fixed for spline and fit xsdef plines.
;; CC 02.18.94  Fixed bugs in bulge-specific arcs.
;;
;; vector is a vector generation routine.
;;

(defun vector (v1 v2 / out)
   (setq out (list (- (car v1) (car v2)) (- (cadr v1) 
     (cadr v2)) (- (caddr v1) (caddr v2))))
)

;;
;; magvec returns the magnitude of an input vector
;;

(defun magvec (v1 / out)
   (setq out (sqrt (+ (expt (car v1) 2) (expt (cadr v1) 2) 
      (expt (caddr v1) 2))))
)

;;
;; unitvec accepts a vector and returns a unit vector 
;; in the same direction.
;;

(defun unitvec (v1 / out)
   (setq out (list (/ (car v1) (magvec v1)) (/ (cadr v1) 
     (magvec v1))
              (/ (caddr v1) (magvec v1)))
   )
)
(defun arrow (p1 p2 / p3)
   (setq p3 (list (+ (car p2) (/ (- (car p1) (car p2)) 4))
                  (+ (cadr p2) (/ (- (cadr p1) (cadr p2)) 4))
                  (+ (caddr p2) (/ (- (caddr p1) (caddr p2)) 4))
            )
   )
   (grdraw p1 p2 3 1)
   (grdraw p3 p2 1)
)
(defun divarc (p1 p2 blg div / xtravrt)
   (setq a1 (angle p1 p2)
        mp (polar p1 a1 (setq r (/ (distance p1 p2) 2)))
        rarc r
   )
   (cond
      ((= 0 blg) (setq r 0 rarc 0 cen mp div 1))
      ((/= 1 (abs blg))
         (progn
            (setq r (/ (* r (cos (setq ia 
                (* 2 (atan (abs blg)))))) (sin ia))
                  rarc (/ r (cos ia))
            )
            (if (minusp blg)
               (setq a1 (- a1 (/ pi 2)))
               (setq a1 (+ a1 (/ pi 2)))
            )
            (setq cen (polar mp a1 r)
                  a (angle cen p1)
                  b (angle cen p2)
            )
            (cond
               ((and (< a b) (not (minusp blg))) 
                   (setq frac (/ (- b a) div)))
               ((and (< a b) (minusp blg)) 
                   (setq frac (/ (- b a (* 2 pi)) div)))
               ((and (> a b) (not (minusp blg))) 
                  (setq frac (/ (+ (* 2 pi) (- b a)) div)))
               (t (setq frac (/ (- b a) div)))
            )
         )
      )
      (t    (setq cen mp
                  a (angle cen p1)
                  frac (/ (* blg pi) div)
            )
      )
   )
   (setq i 1
         xtravrt (append xtravrt (list p1))
   )
   (repeat (1- div)
      (setq xtravrt (append xtravrt (list 
          (polar cen (+ a (* i frac)) rarc)))
            i (1+ i)
      )
   )
   xtravrt
)
(defun c:pxt ( / osmode xspl xtpl sseg xtclosed xsvert xtvert
                 xtvertlen pt1 pt2 pt3 pt4 xpt stpt xtclosed)
   (setq osmode (getvar "OSMODE"))
   (setvar "OSMODE" 0)
   (command "UCS" "W")
   (setq xspl nil xsclosed nil xtpl nil xtclosed nil 
         #FUZZ 1e-10 rang (- (/ pi 2)))
   (while (or (not xspl) (/= (cdr (assoc 0 (setq xsplent 
         (entget xspl)))) "POLYLINE"))
      (setq xspl (car (entsel 
           "\nSelect cross-section polyline: ")))
   )
   (redraw xspl 3)
   (while (or (not xtpl) (/= (cdr (assoc 0 (setq xtplent 
         (entget xtpl)))) "POLYLINE"))
      (setq attachent (entsel 
         "\nSelect reference segment of extrusion polyline: ")
            xtpl (car attachent)
      )
   )

;; build cross-section vertex list
   (setq xsvert nil
         ename (entnext xspl)
   )
   (while (and ename (= (cdr (assoc 0 (entget ename)))
         "VERTEX"))
      (setq vrt (entget ename))
      (if (= 0 (cdr (assoc 42 vrt)))
         (if (/= 16 (cdr (assoc 70 vrt)))
                          ;; make sure it’s not a spline ref.
             (setq xsvert (append xsvert (list 
                   (cdr (assoc 10 vrt)))))
         )
         (if (and (setq nname (entnext ename))
                  (setq nvrt (entget nname))
                  (= (cdr (assoc 0 nvrt)) "VERTEX")
             )
             (progn
                (setq p1 (cdr (assoc 10 vrt))
                      p2 (cdr (assoc 10 nvrt))
                      blg (cdr (assoc 42 vrt))
                      arcseg (getvar "SURFTAB1") 
                            ;; div's for arc in xs
                      xtravrt (divarc p1 p2 blg arcseg)
                )
                (foreach a xtravrt (setq xsvert 
                    (append xsvert (list a))))
             )
             (setq xsvert (append xsvert (list 
                    (cdr (assoc 10 vrt)))))
         )
      )
      (setq ename (entnext ename))
   )
;;if closed, add first vertex to end of list-This one's for Tony
   (if (= (cdr (assoc 70 xsplent)) 1)
      (setq xsvert (append xsvert (list (car xsvert))))
   )

;;
;; build extrusion pline vertex list
;;
   (setq xtvert nil
         ename (entnext xtpl)
   )
   (while (and ename (= (cdr (assoc 0 (entget ename))) "VERTEX"))
      (setq vrt (entget ename)
            xtvert (append xtvert (list (cdr (assoc 10 vrt))))
            ename (entnext ename)
      )
   )
   (if (= (cdr (assoc 70 xtplent)) 1)
      (setq xtvert (append xtvert (list (car xtvert)))
            xtclosed T
      )
   )
   (setq xtvertlen (length xtvert))

;;
;; extrusion pline finished
;;
;; find base point for offset vector list
;;
   (setq attachseg (car (nentselp (last attachent))) 
         attachv1 (cdr (assoc 10 (entget attachseg))) 
         attachnum (- (length xtvert) (length 
             (member attachv1 xtvert)))
         attachv1 (nth attachnum xtvert)
         attachv2 (nth (1+ attachnum) xtvert)
   )
   (arrow attachv1 attachv2)
;;
;; next few lines build offset vector list for 
;; cross-section pline
;;
   (setq base (trans attachv1 xtpl xspl)
         offv nil
         i 0
         xszvec (cdr (assoc 210 xsplent))
         xtzvec (unitvec (vector attachv2 attachv1))
   )
   (if (equal xszvec xtzvec #FUZZ) (setq rang (/ pi 2)))
   (repeat (length xsvert)
      (setq offv (append offv (list 
                (vector (nth i xsvert) base)))
            i (1+ i)
      )
   )
;;
;; offset vector list finished for cross-section pline
;;
   (setq alist nil
         i 0
   )
   (repeat (- (length xtvert) 1)
      (setq pt1 (nth i xtvert)
            pt2 (nth (1+ i) xtvert)
            ang (angle pt1 pt2)
            alist (append alist (list ang))
            i (1+ i)
      )
   )
   (setq alistlen (length alist))
;;
;; extrusion pline angles finished
;;
   (setq j 0 xlist nil)
   (repeat (length offv)
      (if xtclosed
         (setq pt1 (polar (nth 0 xtvert) (+ (nth 0 alist) rang)
                   (car (nth j offv)))
               pt2 (polar (nth 1 xtvert) (+ (nth 0 alist) rang)
                   (car (nth j offv)))
               pt3 (polar (nth (- xtvertlen 2) xtvert) 
      (+ (nth (1- alistlen) alist) rang) (car (nth j offv)))
               pt4 (polar (nth (- xtvertlen 1) xtvert) 
      (+ (nth (1- alistlen) alist) rang) (car (nth j offv)))
               stpt (inters pt1 pt2 pt3 pt4 nil)
               tlist (list (list (car stpt) (cadr stpt) 
      (+ (caddr stpt) (cadr (nth j offv)))))
         )
         (setq pt1 (polar (nth 0 xtvert) (+ (nth 0 alist) rang)
                   (car (nth j offv)))
               tlist (list (list (car pt1) (cadr pt1) 
                   (+ (caddr pt1) (cadr (nth j offv)))))
         )
      )
      (setq i 0)
      (repeat (- (length xtvert) 2)
         (setq pt1 (polar (nth i xtvert) (+ (nth i alist) rang)
                    (car (nth j offv)))
               pt2 (polar (nth (1+ i) xtvert) (+ (nth i alist)
                    rang) (car (nth j offv)))
               pt3 (polar (nth (1+ i) xtvert) (+ (nth (1+ i)
                   alist) rang) (car (nth j offv)))
               pt4 (polar (nth (+ i 2) xtvert) (+ (nth (1+ i)
                   alist) rang) (car (nth j offv)))
               xpt (inters pt1 pt2 pt3 pt4 nil)
               xpt (list (car xpt) (cadr xpt) (+ (caddr xpt)
                   (cadr (nth j offv))))
               tlist (cons xpt tlist)
               i (1+ i)
         )
      )
      (if xtclosed
         (setq tlist (cons (list (car stpt) (cadr stpt) 
            (+ (caddr stpt) (cadr (nth j offv)))) tlist))
         (setq pt1 (polar (nth (1+ i) xtvert) (+ (nth i alist)
            rang) (car (nth j offv)))
               tlist (cons (list (car pt1) (cadr pt1) 
                 (+ (caddr pt1) (cadr (nth j offv)))) tlist)
         )
      )
      (setq tlist (reverse tlist)
            xlist (append xlist (list tlist))
            j (1+ j)
      )
   )

;;
;; mitered vertex list finished, begin PFACE
;;
   (setq i 0)
   (command "PFACE")
   (repeat (length xlist)
      (setq tlist (nth i xlist))
      (foreach a tlist (command (trans a xtpl 0)))
      (setq i (1+ i))
   )
   (command "")
   (setq i 1 j (length xtvert))
   (repeat (1- (length offv))
      (repeat (1- (length xtvert))
         (command i)
         (command (1+ i))
         (command (+ i 1 j))
         (command (+ i j ))
         (command "")
         (setq i (1+ i))
      )
      (setq i (1+ i))
   )
   (command "")
   (redraw xtpl 3)
   (command "UCS" "P")
   (setvar "OSMODE" osmode)
);pxt
(princ "\npxt is loaded.  Type pxt to execute")
(princ)
;; end pxt.lsp


