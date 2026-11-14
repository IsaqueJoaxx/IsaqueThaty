; TIP903.LSP   Automatic Columns  INTS.LSP (C)1993, Todd Wiedemann

(defun C:INTS (/ ENAME ENT SS I J K L PRT1 PRT2 PRT3 PRT4 BL X)
   (princ "\nSelect the lines you wish to place columns on...")
   (setq SS (ssget))
   (if SS 
      (progn
         (princ "\nGathering all valid GRID lines...")
         (setq K 0)
         (while (< K (sslength SS)) 
            (setq ENAME (ssname SS K) 
            ENT (entget ENAME))
            (redraw ENAME 3) 
            (if (= "LINE" (cdr (assoc 0 ENT))) 
               (progn
                  (setq K (1+ K))
                  (princ ".")
               )
               (progn
                  (ssdel ENAME SS) 
                  (redraw ENAME 4)
               )
            )
         )
      ) 
      (command "select" SS "")
   ) 
   (setq I 0)
   (setq J 0)
   (setq L (sslength SS))
   (setq BL (getstring "\nInput block to place at intersections: "))
   (while (< I L) 
      (setq J (1+ I))
      (while (< J L) 

         (setq PRT1 (cdr (assoc 10 (entget (ssname SS I)))) 
         PRT2 (cdr (assoc 11 (entget (ssname SS I)))))

         (setq PRT3 (cdr (assoc 10 (entget (ssname SS J)))) 
         PRT4 (cdr (assoc 11 (entget (ssname SS J)))))

         (setq X (inters (list (car PRT1) (cadr PRT1))
               (list (car PRT2) (cadr PRT2)) 
               (list (car PRT3) (cadr PRT3)) 
               (list (car PRT4) (cadr PRT4))
         ))

         (if X (progn 
               (setq X (trans X 0 1))
               (command ".insert" BL X "" "" "")
            )
         ) 
         (setq J (1+ J))
      ) 
      (setq I (1+ I))
   ) 
   (princ) 
   (redraw)
); end ints.lsp
