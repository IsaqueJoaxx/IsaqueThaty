(apply '(lambda ( / name x names temp)
  (setvar "cmdecho" 0)
  (menucmd (strcat "s=" (cadr x)))             ;call up screen menu v2.5 only
  (setq name 1 names "")                       ;initialize values
  (prompt "\n \n \nPick ")
  (princ (caddr x))
  (princ " to ")
  (princ (car x))
  (while name
     (if (/= names "")(progn
       (princ "\nNames selected: ")
       (princ names)))
     (setq name (getstring "\n Pick from screen menu: <none> "))
     (if (or (= name "none")(= name ""))(setq name nil))
     (if name (progn
        (setq name (cond ((= (cadr x)"laymaj")(strcat name "*"))
         ((= (cadr x)"laymin")(strcat "????-" name "*"))
                 ((= (cadr x)"laybas")(strcat "????-????-" name))
  (t (quit))
            )                       ;end cond
 )                                  ;end setq
C:C