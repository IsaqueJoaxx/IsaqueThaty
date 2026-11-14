;This routine will ask you for an ENTITY type to scan for and a LAYER
;<either exitant or not> and will place ALL of those entities on that 
;specified layer. 
;This code is great for grabbing an entity (e.g.TEXT) and placing
;it on a layer. Be sure that the entity is correctly spelled (i.e. 
;polyline not pline)
;This is also a good example of how to process your database and modify the
;geometry. Main program by Creighton Hoke, modified by J.Sergneri and
;D. Kurland
;
;
(defun chnge (nsz nent / ent nnt ename)
 (prompt "\nScanning the entity database....")
 (setq ename (entnext))
 (while (and ename (setq ent (entget ename)))
   (if
     (= (cdr (assoc 0 ent)) nent)
     (progn
       (setq nnt (subst (cons 8 nsz) (assoc 8 ent) ent))
       (entmod nnt)
     )
   )
   (setq ename (entnext (cdar ent)))
 )
)
(defun c:clayer ()
 (setq nent (strcase (getstring "\n Enter the entity to change:")))
 (setq nsz (strcase (getstring "\n Enter LAYER for change: ")))
 (chnge nsz nent)
)
