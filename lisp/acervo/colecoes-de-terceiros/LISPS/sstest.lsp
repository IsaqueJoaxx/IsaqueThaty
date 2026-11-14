; The following functions are examples of selection set minipulations
; These examples provide several functions not included in the (ssxxx)
; function set provided in AutoLISP.
;
;
; This function concatenates two selection sets <ss1> and <ss2>
; into <ss1> it returns an integer representing the number of Entities
; added to <ss1>
;
(defun sscat (ss1 ss2 / temp index )
   (setq index 0)
   (setq temp (ssname ss2 index))	  
   (while (/= (setq temp (ssname ss2 index)) nil) 
      (setq ss1 (ssadd temp ss1))
      (setq index (1+ index))	
   )
)
;
; This function will clear a selection set <ss1> of all entities, Thus
; freeing it for reuse by the (ssinput) function. It will return the 
; number of entities removed from the old set.
;
(defun ssclear (ss1 / temp index )
   (setq index 0)
   (while (/= (setq temp (ssname ss1 0)) nil) 
      (setq ss1 (ssdel temp ss1))
      (setq temp (1+ temp))
   )
)
;
; This function will allow the addition of entities to a 
; selection set <ss1>. 
;
(defun ssinput (ss1 / temp)
   (while (/= (setq temp (car (entsel))) nil) 
      (setq ss1 (ssadd temp ss1))
   )
)
;
; This function will list the primary <ename> for all entities in a 
; selection set <ss1>. 
;
(defun sslist (ss1 / temp)
   (setq index 0)
   (while (/= (setq temp (ssname ss1 index)) nil) 
      (print index) 
      (princ " = ")
      (princ temp)
      (setq index (1+ index))	
   )
   (terpri)
)
;
; This function will determin if an entity is in a given 
; selection set <ss1>. 
;
(defun sstest (ss1 / temp)
   (setq temp (car (entsel "Pick Entity ")))
   (if (/= (ssmemb temp ss1) nil) (print "YES") (print "NO")) 
   (terpri)
)
