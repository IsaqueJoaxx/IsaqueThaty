; TIP965.LSP: REPLRD.LSP   Parallel Lines from Plines  (c)1994, Bill Bratt

(defun C:REPLRD ()
   (setvar "cmdecho" 0)
   (setq WIDTH 0.0
      ANG   nil
      IPS   (getreal "\n Road Width Edge to Edge: ")
      LYR   (strcase (getstring "\n NEW Road Edge Layer: "))
      LYR2   (strcase (getstring "\n EXISTING Road Centerline Layer: "))
   )

   ;   (command ".rename" "la" LYR LYR2)
   (command ".layer" "m" LYR2 "m" LYR "")
   (command)
   ;---
   (setq Ent (Entnext))
   (while Ent
      (setq ental (entget Ent))
      (if (eq LYR2 (CDR (Assoc 8 ental)))
         (progn
            (if (eq "POLYLINE" (cdr (assoc 0 ental)))
               (progn
                  ;               (SetQ ental (subst (cons 8 lyr) (assoc 8 ental) ental))
                  (SetQ ental (subst (cons 40 width) (assoc 40 ental) ental))
                  (SetQ ental (subst (cons 41 width) (assoc 41 ental) ental))
                  (entmod ental)
               )
            )
            (setq Ent (entnext Ent))
         ) ;end progn
         (setq Ent (entnext Ent))
      ) ;end if
   ) ;end while
   ;---
   (setq Ent (Entnext))
   (while Ent
      (setq ental (entget Ent))                     ;POLYLINE
      (if (and (eq LYR2 (CDR (Assoc 8 ental)))
            (eq "POLYLINE" (cdr (assoc 0 ental)))
         )
         (progn
            (setq ent   (entnext ent)               ;VERTEX1
               ental (entget ent)
               p1    (cdr (assoc 10 ental))
               pts2  nil
               pts3  nil
               ent   (entnext ent)               ;VERTEX2
               ental (entget ent)
               p2   (cdr (assoc 10 ental))
               ang  (angle p1 p2)
               ang1 (+ (/ pi 2.0) ang)
               ang2 (+ (* pi 1.5) ang)
               p3   (polar p1 ang1 (/ ips 2.0))
               p4   (polar p1 ang2 (/ ips 2.0))
               p5   (polar p2 ang1 (/ ips 2.0))
               p6   (polar p2 ang2 (/ ips 2.0))
               pts2 (cons p3 pts2)               ;1st list of points
               pts3 (cons p4 pts3)               ;2nd list of points
               p1   p2
               ent   (entnext ent)               ;VERTEX3
               ental (entget ent)
            )
            (while (eq "VERTEX" (cdr (assoc 0 ental)))
               (setq p2   (cdr (assoc 10 ental))
                  ang3 (angle p1 p2)
                  ang4 (+ (/ pi 2.0) ang3)
                  ang5 (+ (* pi 1.5) ang3)
                  p8   (polar p1 ang4 (/ ips 2.0))
                  p9   (polar p1 ang5 (/ ips 2.0))
                  p10  (polar p2 ang4 (/ ips 2.0))
                  p11  (polar p2 ang5 (/ ips 2.0))
                  p12  (inters p3 p5 p8 p10 nil)
                  p13  (inters p4 p6 p9 p11 nil)
               )
               (if (eq p12 nil) (setq p12 p5))
               (if (eq p13 nil) (setq p13 p6))
               (setq pts2 (cons p12 pts2)
                  pts3 (cons p13 pts3)
                  p1   p2
                  p3   p8
                  p4   p9
                  p5   p10
                  p6   p11
                  ent   (entnext ent)
                  ental (entget ent)
               )
            ) ;end while
            (setq pts2 (cons p5 pts2)
               pts3 (cons p6 pts3)
               Ent (entnext Ent)
            )
            (apply 'command (cons ".pline" (reverse pts2)))
            (command)
            (apply 'command (cons ".pline" (reverse pts3)))
            (command)
         ) ;end progn
         (setq Ent (entnext Ent))
      ) ;end if
   ) ;end while
   (princ)
   ;(command ".layer" "f" LYR2 "")
   ;(command ".resume")
) ;end replrd.lsp
