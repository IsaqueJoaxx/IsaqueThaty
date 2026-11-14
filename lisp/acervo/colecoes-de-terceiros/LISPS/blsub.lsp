(defun getblk (msg / name e d)
   (setq name
      (getstring (strcat msg "/<select>: ")))
   (cond
      (  (eq name "")
         (setq e (car (entsel
            "\nSelect insertion: " )))
         (setq d (entget e))
         (if (/= (cdr (assoc 0 d)) "INSERT")
             (princ "\nObject is not a block.")
             (cdr (assoc 2 d))))
      (  (not (tblsearch "block" name))
         (princ (strcat "\nBlock " name
             " not defined in this drawing.")))
      (t (strcase name)))
)

(defun C:SUBST ( / nb ob ss e i bg)
   (cond
      (  (not (setq ob (getblk
                          "\nBlock to replace"))))
      (  (not (setq ss
            (ssget "x" (list (cons 0 "INSERT")
                             (cons 2  ob)))))
         (princ (strcat
            "\nNo insertions of " ob " found.")))
      (  (not (setq nb (getblk
                         "\nReplacement block"))))
      (t (setvar "cmdecho" 0)
         (command "._undo" "_g")
         (setq bg (cons 2 nb))
         (repeat (setq i (sslength ss))
            (setq e (ssname ss (setq i (1- i))))
            (entmod (list (cons -1 e) bg))
         )
         (princ
            (strcat
               "\nModified " (itoa (sslength ss))
               " block insertions."))
         (command "._undo" "_e")))
   (princ)
)
