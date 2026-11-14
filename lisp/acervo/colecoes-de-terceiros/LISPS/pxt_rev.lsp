;; pxt.lsp (c) 1994 Christopher Crawford
;; 3d extrusion for polylines
;;
;; pxt takes two polylines and extrudes the first around the second, creating
;; a polyface mesh.
;;
;; Revision History
;; CC 01.27.94  Created routine.
;; CC 02.02.94  Shows arrow and left/right
;; CC 02.16.94  Pick starting point on cross-section.  Works with closed
;;              cross-sections.  Works with path plines not in WCS.
;; CC 02.17.94  Support for bulge-specific plines in xsdef.  Bugs introduced.
;; CC 02.17.94  Reworked interface.  Now, entity must be perpendicular to a
;;              reference segment on the path pline.  Only two selections
;;              are necessary to start the routine. Implemented via (nentselp)
;;              Release 11 users will need a different version (search through
;;              pline until you reach "SEQUEND" to get the parent entity). Lost
;;              ability to extrude plines not in WCS.
;; CC 02.17.94  Fixed for spline and fit xsdef plines.
;; CC 02.18.94  Fixed bugs in bulge-specific arcs.  Bug still exists for closed
;;              pline with bulge-specific info in last segment.
;; CC 02.19.94  Fixed bug for collinear vertices in xtpl.  Shuts down CMDECHO
;;              and added number of faces prompt.  Much faster!
;; CC 02.19.94  Bulge specific arcs can now be divided depending on angle
;;              subtended.  Surftab option is retained.
;; CC 02.19.94  Support for curves in path pline.
;; CC 02.20.94  Fixed bug for closed pline with bulge-specific info in last
;;              segment.
;; CC 02.20.94  Problem with rotating out of WCS is identified.  Have to adjust
;;              offv to coordinate with ECS of path pline.
;; CC 02.22.94  Fixed problem with coincident vertices.
;; CC 02.22.94  Fixed problem with plines not in WCS.
;;
;; vector is a vector generation routine.
;;
(defun vector (v1 v2)
   (list (- (car v1) (car v2)) (- (cadr v1) (cadr v2)) (- (caddr v1) (caddr v2)))
)
;;
;; addvec is a vector addition routine.
;;
(defun addvec (v1 v2)
   (list (+ (car v1) (car v2)) (+ (cadr v1) (cadr v2)) (+ (caddr v1) (caddr v2)))
)
;;
;; magvec returns the magnitude of an input vector
;;
(defun magvec (v1)
   (sqrt (+ (expt (car v1) 2) (expt (cadr v1) 2) (expt (caddr v1) 2)))
)
;;
;; unitvec accepts a vector and returns a unit vector in the same direction.
;;
(defun unitvec (v1 / mag)
   (if (> (setq mag (magvec v1)) 0)
      (list (/ (car v1) mag) (/ (cadr v1) mag) (/ (caddr v1) mag))
      nil
   )
)
;;
;; cross returns the vector cross product of two input vectors.
;;
(defun cross (v1 v2)
   (list (- (* (cadr v1) (caddr v2)) (* (cadr v2) (caddr v1)))
         (- (* (car v2) (caddr v1)) (* (car v1) (caddr v2)))
         (- (* (car v1) (cadr v2)) (* (car v2) (cadr v1)))
   )
)
;;
;; divarc divides a pline arc into vertices based on maxabb
;;
(defun divarc (p1 p2 blg maxabb / xtravrt ca a b mp r rarc frac)
   (setq ca (angle p1 p2)
        mp (polar p1 ca (setq r (/ (distance p1 p2) 2)))
        rarc r
   )
   (cond
      ((= 0 blg) (setq r 0 rarc 0 cen mp maxabb "1"))
      ((/= 1 (abs blg))
         (progn
            (setq r (/ (* r (cos (setq ia (* 2 (atan (abs blg)))))) (sin ia))
                  rarc (/ r (cos ia))
            )
            (if (minusp blg)
               (setq ca (- ca (/ pi 2)))
               (setq ca (+ ca (/ pi 2)))
            )
            (setq cen (polar mp ca r))
         )
      )
      (t (setq cen mp))
   )
   (setq a (angle cen p1)
         b (angle cen p2)
   )
   (cond
      ((and (< a b) (not (minusp blg))) (setq sub (- b a)))
      ((and (< a b) (minusp blg)) (setq sub (- b a (* 2 pi))))
      ((and (> a b) (not (minusp blg))) (setq sub (+ (* 2 pi) (- b a))))
      (t (setq sub (- b a)))
   )
   (cond
     ((= maxabb "ST1") (setq div (getvar "SURFTAB1")))
     ((= maxabb "ST2") (setq div (getvar "SURFTAB2")))
     ((= maxabb "1") (setq div 1))
     (t (setq div (fix (/ (abs sub) (* maxabb (/ PI 180))))))
   )
   (if (= div 0) (setq div 1))
   (setq frac (/ sub div))
   (setq i 1
         xtravrt (append xtravrt (list p1))
   )
   (repeat (1- div)
      (setq xtravrt (append xtravrt (list (polar cen (+ a (* i frac)) rarc)))
            i (1+ i)
      )
   )
   xtravrt
)
;;
;; process_vrt process a pline into a vertex list
;;
(defun process_vrt (pl closed maxabb / ename vrt lastpt nvrt p1 p2 blg xtravrt plvert lastblg)
   (setq ename (entnext pl))
   (while (and ename (= (cdr (assoc 0 (entget ename))) "VERTEX"))
      (setq vrt (entget ename)
            vrtp (cdr (assoc 10 vrt))
      )
      (if (equal vrtp lastpt) (setq plvert (reverse (cdr (reverse plvert)))))
      (setq lastpt vrtp)
      (if (= 0 (cdr (assoc 42 vrt)))
         (if (/= 16 (cdr (assoc 70 vrt))) ;; make sure it's not a spline ref.
             (setq plvert (append plvert (list vrtp)))
         )
         (if (and (setq nname (entnext ename))
                  (setq nvrt (entget nname))
                  (= (cdr (assoc 0 nvrt)) "VERTEX")
             )
             (progn
                (setq p1 vrtp
                      p2 (cdr (assoc 10 nvrt))
                      blg (cdr (assoc 42 vrt))
                      xtravrt (divarc p1 p2 blg maxabb)
                )
                (foreach a xtravrt (setq plvert (append plvert (list a))))
             )
             (setq lastblg (cdr (assoc 42 vrt)))
         )
      )
      (setq ename (entnext ename))
   )
;; if closed, add first vertex to end of list [need to fix this for closed arcs]
   (if closed
      (if (not lastblg)
         (setq plvert (append plvert (list (car plvert))))
         (progn
            (setq p1 vrtp
                  p2 (car plvert)
                  xtravrt (divarc p1 p2 lastblg maxabb)
            )
            (foreach a xtravrt (setq plvert (append plvert (list a))))
            (setq plvert (append plvert (list p2)))
         )
      )
   )
   plvert
)
;;
;; pxt is the program call
;;
(defun c:pxt ( / os ce bm xspl xtpl sseg xtclosed xsvert xtvert
                 xtvertlen alist alistlen pt1 pt2 pt3 pt4 xpt stpt xtclosed
                 maxabb maxin opt px py)
   (setq os (getvar "OSMODE")
         ce (getvar "CMDECHO")
         bm (getvar "BLIPMODE")
   )
   (setvar "OSMODE" 0)
   (setvar "CMDECHO" 1)
   (setvar "BLIPMODE" 1)
   (command "UCS" "W")
   (setq xspl nil xsclosed nil maxabb nil xtpl nil xtclosed nil #FUZZ 1e-10)
   (while (or (not xspl)
              (/= (cdr (assoc 0 (setq xsplent (entget xspl)))) "POLYLINE")
              (> (setq xsclosed (cdr (assoc 70 xsplent))) 5)
          )
      (setq xspl (car (entsel "\nSelect cross-section polyline: ")))
   )
   (setq xsclosed (= (logand xsclosed 1) 1))
   (redraw xspl 3)
   (while (or (not xtpl)
              (/= (cdr (assoc 0 (setq xtplent (entget xtpl)))) "POLYLINE")
              (> (setq xtclosed (cdr (assoc 70 xtplent))) 5)
          )
      (setq attachent (entsel "\nSelect reference segment of path polyline: ")
            xtpl (car attachent)
      )
   )
   (setq xtclosed (= (logand xtclosed 1) 1))
   (redraw xtpl 3)
   (setq maxin (getstring "\nEnter maximum angle (in degrees) before break in bulge-specific arc\nor 'S' to use Surftabs <15.0>: "))
   (cond
      ((numberp (read maxin)) (setq maxabb (read maxin)))
      ((and maxin (= (strcase maxin) "S")) (setq maxabb nil))
      (t (setq maxabb 15))
   )
;;
;; build cross-section vertex list
;;
   (grtext -1 "Processing cross-section vertex list...")
   (if maxabb
      (setq xsvert (process_vrt xspl xsclosed maxabb))
      (setq xsvert (process_vrt xspl xsclosed "ST1"))
   )
;;
;; build path pline vertex list
;;
   (grtext -1  "Processing path vertex list...")
   (if maxabb
      (setq xtvert (process_vrt xtpl xtclosed maxabb))
      (setq xtvert (process_vrt xtpl xtclosed "ST2"))
   )
   (setq xtvertlen (length xtvert))
;;
;; path pline finished
;;
;; find base point for offset vector list
;;
   (grtext -1 "Processing offset vector list...")
   (setq attachseg (car (nentselp (last attachent))) ;; set segment
         attachv1 (cdr (assoc 10 (entget attachseg))) ;; set point
         attachnum (- xtvertlen (length (member attachv1 xtvert)))
         attachv1 (nth attachnum xtvert)
         attachv2 (nth (1+ attachnum) xtvert)
   )
;;
;; make sure ECS of xspl is perpendicular to path
;;
   (setq xszvec (cdr (assoc 210 xsplent))
         xtzvec (cdr (assoc 210 xtplent))
         pathvec (trans (unitvec (vector attachv2 attachv1)) xtpl 0)
         outvec (cross pathvec xtzvec)
         opt (trans attachv1 xtpl 0)
         px (addvec opt outvec)
         py (addvec opt xtzvec)
   )
;;
;; build offset vector list for cross-section pline
;;
   (if (equal (magvec (cross xszvec pathvec)) 0 #FUZZ) ;; xs ECS perp to xtpl
      (progn
         (command "UCS" "3" opt px py)
         (setq offv nil
               i 0
         )
         (repeat (length xsvert)
            (setq offv (append offv (list (trans (nth i xsvert) xspl 1)))
                  i (1+ i)
            )
         )
         (command "UCS" "P")
         (setq offvlen (length offv))
;;
;; offset vector list finished for cross-section pline
;;
;; build mesh
;;
         (grtext -1 "Building polyface mesh.  Please be patient...")
         (setq alist nil
               i 0
         )
         (repeat (- xtvertlen 1)
            (setq pt1 (nth i xtvert)
                  pt2 (nth (1+ i) xtvert)
                  ang (angle pt1 pt2)
                  alist (append alist (list ang))
                  i (1+ i)
            )
         )
         (setq alistlen (length alist))
;;
;; path pline angles finished
;;
         (setq j 0 xlist nil)
         (if (equal xszvec xtzvec #FUZZ) (setq rang (/ pi 2)) (setq rang (- (/ pi 2))))
         (repeat offvlen
            (if xtclosed
               (setq pt1 (polar (nth 0 xtvert) (+ (nth 0 alist) rang) (car (nth j offv)))
                     pt2 (polar (nth 1 xtvert) (+ (nth 0 alist) rang) (car (nth j offv)))
                     pt3 (polar (nth (- xtvertlen 2) xtvert) (+ (nth (1- alistlen) alist) rang) (car (nth j offv)))
                     pt4 (polar (nth (- xtvertlen 1) xtvert) (+ (nth (1- alistlen) alist) rang) (car (nth j offv)))
                     stpt (inters pt1 pt2 pt3 pt4 nil)
                     tlist (list (list (car stpt) (cadr stpt) (+ (caddr stpt) (cadr (nth j offv)))))
               )
               (setq pt1 (polar (nth 0 xtvert) (+ (nth 0 alist) rang) (car (nth j offv)))
                     tlist (list (list (car pt1) (cadr pt1) (+ (caddr pt1) (cadr (nth j offv)))))
               )
            )
            (setq i 0)
            (repeat (- xtvertlen 2)
               (setq pt1 (polar (nth i xtvert) (+ (nth i alist) rang) (car (nth j offv)))
                     pt2 (polar (nth (1+ i) xtvert) (+ (nth i alist) rang) (car (nth j offv)))
                     pt3 (polar (nth (1+ i) xtvert) (+ (nth (1+ i) alist) rang) (car (nth j offv)))
                     pt4 (polar (nth (+ i 2) xtvert) (+ (nth (1+ i) alist) rang) (car (nth j offv)))
                     xpt (inters pt1 pt2 pt3 pt4 nil)
               )
               (if xpt
                   (setq xpt (list (car xpt) (cadr xpt) (+ (caddr xpt) (cadr (nth j offv)))))
                   (setq xpt (list (car pt2) (cadr pt2) (+ (caddr pt2) (cadr (nth j offv)))))
               )
               (setq tlist (cons xpt tlist)
                     i (1+ i)
               )
            )
            (if xtclosed
               (setq tlist (cons (list (car stpt) (cadr stpt) (+ (caddr stpt) (cadr (nth j offv)))) tlist))
               (setq pt1 (polar (nth (1+ i) xtvert) (+ (nth i alist) rang) (car (nth j offv)))
                     tlist (cons (list (car pt1) (cadr pt1) (+ (caddr pt1) (cadr (nth j offv)))) tlist)
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
         (setq i 0
               xlistlen (length xlist)
         )
         (command "PFACE")
         (repeat xlistlen
            (setq tlist (nth i xlist))
            (foreach a tlist (command (trans a xtpl 0)))
            (grtext -1 (strcat "Vertices left: " (itoa (- xlistlen i))))
            (setq i (1+ i))
         )
         (command "")
         (setq i 1 j xtvertlen faces (* (1- offvlen) (1- xtvertlen)))
         (repeat (1- (length offv))
            (repeat (1- (length xtvert))
               (command i)
               (command (1+ i))
               (command (+ i 1 j))
               (command (+ i j ))
               (command "")
               (grtext -1 (strcat "Faces left: " (itoa (- faces i (- offvlen) 2))))
               (setq i (1+ i))
            )
            (setq i (1+ i))
         )
         (command "")
         (redraw xtpl 3)
         (grtext -1 (strcat "Total faces in pxt mesh: " (itoa (- i offvlen))))
      )
      (alert "Cross-section is not perpendicular \nto path at reference segment.")
   )
   (command "UCS" "P")
   (setvar "OSMODE" os)
   (setvar "BLIPMODE" bm)
   (setvar "CMDECHO" ce)
   (princ)
);pxt
(princ "\npxt is loaded.  Type pxt to execute")
(princ)
