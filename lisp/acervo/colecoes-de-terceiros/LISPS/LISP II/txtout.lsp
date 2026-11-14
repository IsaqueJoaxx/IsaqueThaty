(defun C:TXTOUT (/ va vb vc vd ve vf vg);V1.0
;By Scott Hull, 11-20-86
;SAH Mechanical Design (415) 343-4015
;Exports ASCII text to file.

 (defun *error* (st) (prompt (strcat "error: " st "\007\n")))

 (setq va (getstring "Name of ASCII file to create: ") vb (open va "r"))
 (if (/= vb nil) (progn (close vb) (setq vc (ascii (strcase (getstring
 "A file with this name already exists.\nDo you want to replace it? <N> ")))))
  (setq vc 89))
 (if (= vc 89) (progn
   (setq vb (open va "w") vd (ssget) ve (sslength vd) vf 0)
   (while (< vf ve)
    (if (= "TEXT" (cdr (assoc 0 (setq vg (entget (ssname vd vf))))))
     (write-line (cdr (assoc 1 vg)) vb))
    (setq vf (1+ vf)))
   (close vb)
   (eval "DONE"))))

t (ssname vd vf))))))
     (write-l