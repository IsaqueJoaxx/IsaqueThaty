;
; This function allows the editing of an attribute value
;
(defun C:ChangeAtt ( / A Tag TemP)
   (setq A (car (entsel "Select Block ")))
   (setq Tag (getstring "Enter TAG to Change "))
   (setq TemP (getatt A Tag))
   (setq TemP (getname T TemP "Enter New Value "))
   (setatt A Tag TemP T)
)
;
; This function Reads a Atribute and returns its Value from the database
;
(defun GetAtt (Section tag / Temp1 Alist Old)
   (setq Temp1 Section 
         Alist (entget Temp1))
   (while (not (= (cdr (assoc 2 Alist)) Tag))
          (setq Temp1 (entnext Temp1) 
                Alist (entget Temp1)))
   (setq Old (cdr (assoc 1 Alist)))
)
;
; This function Updates a Atribute Value
;
(defun SetAtt (Section tag Value Redraw / Temp1 Alist Old)
   (setq Temp1 Section Alist (entget Temp1))
   (while (not (= (cdr (assoc 2 Alist)) Tag))
          (setq Temp1 (entnext Temp1) Alist (entget Temp1)))
   (setq Old (assoc 1 Alist) Alist (subst (cons 1 value) Old Alist))
   (if Redraw (entupd (cdr (car (entmod Alist))))
              (entupd (cdr (car Alist))))
)
