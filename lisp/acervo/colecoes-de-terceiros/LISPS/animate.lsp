; --------------------------------------------------------
;
;   Setpath & Animate               Bill Kramer
;
;   Define a path (sequence of lines) for a block to
;   move along in "real time".
;
(defun c:setpath ()
   (prompt "Define a set of two or more points:")
   (setq path-list nil)
   (setq p1 (getpoint "\nStarting point:"))
   (setq path-list (list p1))
   (while (not (null (setq p1 (getpoint p1 "\nTo point:"))))
          (setq path-list (cons p1 path-list)))
   (setq path-list (reverse path-list))
   (setq bname (car (entsel "\nSelect block to move:")))
   (setq sdst (getdist "\nIncremental distance to move:")))
(defun c:animate ()
   (setq elist (entget bname))
   (foreach pnt path-list
        (setq elist (moveit elist pnt))))
(defun moveit (elist topnt)
   (setq dst (distance (cdr (assoc 10 elist)) topnt))
   (setq ang (angle (cdr (assoc 10 elist)) topnt))
   (setq dp (polar '(0 0) ang sdst))
   (setq pdst 0.0)
   (while (< (setq pdst (+ pdst sdst)) dst) 
      (setq elist (entmod
                     (subst
                       (cons 10
                         (mapcar '(lambda (a b) (+ a b))
                                  (cdr (assoc 10 elist)) dp))
                         (assoc 10 elist)
                         elist))))
    (entmod (subst (cons 10 topnt)
                   (assoc 10 elist) elist)))
