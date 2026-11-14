; TIP1003.LSP: MR.LSP   Improved Measure Command   
;                       (c)1994, William J. Garmer (Willcad)

;;block Rtick required
(defun C:MR (/ MR_OBJ LEN SS NEWSCL SS_LEN)
   (command "undo" "mark")
   (initget 32)
   (setq MR_OBJ (entsel))
   (redraw (car MR_OBJ) 3)
   (setq LEN (getdist "\nDistance between blocks: "))
   (if (not (tblsearch "BLOCK" "RTICK"))
   (command "insert" "rtick" "0,0" "" "" "" "erase" (entlast) ""))
   (command "measure" MR_OBJ "b" "rtick" "" LEN)
   (setq SS (ssget "P"))
   (setq SS_LEN (rtos (sslength SS) 2 0))
   (setq NEWSCL (getvar "dimscale"))
   (sub_prop SS NEWSCL)
   (prompt (strcat "\nCommand created " SS_LEN " blocks references @ scale factor " (rtos NEWSCL 2 4) ". "))
);defun c:mr

(defun sub_prop (SS VAL / E COUNT)
   (terpri)
   (setq COUNT 0)
   (while (< COUNT (sslength SS))
      (if (= (cdr (assoc 0 (setq E (entget (ssname SS COUNT))))) "INSERT")
         (progn
            (setq E (subst (cons 41 VAL) (assoc 41 E) E))
            (setq E (subst (cons 42 VAL) (assoc 42 E) E))
            (setq E (subst (cons 43 VAL) (assoc 43 E) E))
            (entmod E)
         );progn
      );if
      (setq COUNT (+ COUNT 1))
   );while
)(princ "MR loaded")(princ);end mr.lsp

