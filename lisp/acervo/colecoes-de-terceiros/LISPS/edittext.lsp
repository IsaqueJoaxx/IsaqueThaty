;
;******************************************************************
;
;               Text Editor For AutoCAD Vers. 2.5
;          (C)1986 ADSI all commercial Rights Reserved
;  This program is provided for informational and educational
;     purposes only and not for commercial use of any kind,
;             without prior permission from the me!
;
;                         R. C. Bradlee
;      C/O ADSI 190 East Fifth Avenue Naperville IL. 60540
;              (312) 355-4666   (Noon to 6pm CST)
;
;       This function allows the editing of a text string
;
(defun c:EditText ( / Key Insert Left Right Delete Backup REturn
                      INput PutAscii InsAscii Entity Alist
                      OldText NewText Row Col)
   (while (not Entity)
          (terpri) (setq Entity (entsel "Select Text to Edit ")))
   (setq Alist (entget (car Entity)))
   (terpri)
   (if (= (cdr (assoc 0 Alist)) "TEXT")
      (progn 
         (setq OldText (assoc 1 Alist)
               NewText (cdr OldText)
               Col 1 
               Row 25
               Ins nil
               Left   '(progn (if (> Col 1) (setq Col (1- Col))))
               Right  '(progn (if (< Col 79) 
                                  (setq Col (1+ Col))))
               Delete '(progn (setq Col (1+ Col)) (eval Backup))
               Backup '(progn (if (> Col 1) 
                              (progn (setq Col (1- Col)) 
                                  (prinxy Col Row 
                                     (substr (setq NewText (strcat 
                                        (if (> Col 1) 
                                           (substr NewText 1 (1- Col)) "") 
                                           (substr NewText (1+ Col))))
                                        Col)) (princ " "))))
             PutAscii '(progn (prinxy Col Row 
                                  (substr (setq NewText 
                                     (strcat (if (> Col 1)
                                        (substr NewText 1 (1- Col)) "")
                                     (chr INput) 
                                     (substr NewText (1+ Col)))) 
                                  Col 1))
                                  (setq Col (1+ Col)))
             InsAscii '(progn (prinxy Col Row 
                                 (substr (setq NewText 
                                     (strcat (if (> Col 1) 
                                        (substr NewText 1 (1- Col)) "") 
                                     (chr INput) 
                                     (substr NewText Col))) 
                                 Col)) 
                                 (setq Col (1+ Col)))
              InsMode '(progn (setq Insert (not Insert)))
                Return nil)
         (terpri) (princ "* String Editor (C)1986 ADSI *")
         (terpri) (princ "* F2 Ins * F3 <- * F4 -> * F5 Sol * Esc Quit *")
         (terpri) (princ NewText) (prinxy 0 Row "")
         (while (not Return)
            (while (/= (car (setq Key (grread))) 2))
            (setq INput (cadr Key))
            (cond  ((and (>= INput 32) (<= INput 129)) 
                         (If Insert (eval InsAscii)
                                    (eval PutAscii))) 
               ((= INput 27)  (setq REturn T NewString (cdr OldString)))
               ((= INput 13)  (setq REturn T))
               ((= INput 191) (setq Col 0))
               ((= INput 189) (eval Left))
               ((= INput 190) (eval Right))
               ((= INput 8)   (eval Backup))
               ((= INput 211) (eval Delete))
               ((= INput 188) (eval InsMode)))
            (prinxy Col row ""))
         (setq Alist (subst (cons 1 NewText) OldText Alist))
         (entmod Alist) (terpri))
       (progn (terpri) (prompt "\007Not a Valid Text Entity !")))
)

