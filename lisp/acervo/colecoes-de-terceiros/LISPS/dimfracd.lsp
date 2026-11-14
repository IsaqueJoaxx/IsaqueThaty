(defun C:DIMFRACD ()
  (setq rgmd (getvar "REGENMODE"))
  (setvar "REGENMODE" 0)                 ;Prevent automatic drawing regens
  (setq styl (getvar "TEXTSTYLE"))
  (command ".style" "ROMANDFR" "ROMANDFR" 0 1 0 "n" "n" "n")
  (command ".style" styl "" "" "" "" "" "" "")
  (princ "\nSelect Dimensions to be converted to fractions : ")
  (setq ss (ssget))kE              ;Select Objects
  (setq plen (sslength ss))
  (setq n 0)                     ;Reset Index to 0
  (if (> plen 0)
    (while (< n plen)
      (setq e1 (ssname ss n))
      (setq en (entget e1))
      (setq et (cdr (assoc 0 en)))
      (setq en1 en)
      (if (= et "TEXT") (fract_text))
      (if (= et "DIMENSION")
        (progn
           (setq e0 (entlast))           ;Find last entity in drawing database
           (setq en1 (entnext e0))       ;  so that entities added from
           (while (not (null en1))       ;  explode can be distinguished
              (setq e0 en1)
              (setq en1 (entnext e0))
           )
           (command "explode" (getval -1 en))
           (setq s0 (ssadd))      ;Create an empty selection set
           (while (entnext e0) (ssadd (setq e0 (entnext e0)) s0))
           (command "chprop" s0 "" "c" "bylayer" "lt" "bylayer"
                                      "la" (getval 8 en) "")
           (setq plen1 (sslength s0))
           (setq n1 0)
           (if (> plen1 0)       ;Change Text String as needed
              (while (< n1 plen1)
                 (progn
                   (setq e11 (ssname s0 n1))
                   (setq en1 (entget e11))
                   (setq et1 (cdr (assoc 0 en1)))
                   (if (= et1 "TEXT") (fract_text))
                   (setq n1 (1+ n1))
                 )    ;progn
              )    ;while
           )       ;if
      ))
      (setq n (1+ n))
    )   ;while
  )     ;if plen
  (setvar "REGENMODE" rgmd)              ;Restore drawing regen mode
  (print "DIMFRAC Complete ...")
  (princ)
)

(defun parse_etxt ()
   (setq movdis 0)
   (setq tht (cdr (assoc 40 en1)))     ;Get text height
   (setq tloc (cdr (assoc 10 en1)))    ;Get text location
   (setq trot (cdr (assoc 50 en1)))    ;Get text rotation
   (setq txtlen (strlen etxt))
   (setq si 1 slloc 0)
   (while (<= si txtlen)
     (progn
       (if (= "/" (substr etxt si 1))
          (setq slloc si)
       )
       (setq si (1+ si))
   ))
   (if (> slloc 0)
      (progn
         (setq denom (substr etxt (1+ slloc) 1))
         (setq numer (substr etxt (1- slloc) 1))
         (setq numer1 (substr etxt (- slloc 2) 1))
         (setq ctest (ascii (substr etxt 1 1)))
         (if (and (>= ctest 48) (<= ctest 57)) (setq mask 0) (setq mask -1))
         (if (= denom "2") (setetx1 "i"))
         (if (= denom "4")
            (progn
               (if (= numer "1") (setetx1 "r"))
               (if (= numer "3") (setetx1 "s"))
            )
         )
         (if (= denom "8")
            (progn
               (if (= numer "1") (setetx1 "w"))
               (if (= numer "3") (setetx1 "y"))
               (if (= numer "5") (setetx1 "p"))
               (if (= numer "7") (setetx1 "f"))
            )
         )
         (if (= denom "1")    ;(1/16")
            (progn
               (if (= numer "7") (setetx2 "u"))
               (if (= numer "9") (setetx2 "o"))
               (if (and (= numer1 " ") (= numer "1")) (setetx2 "q"))
               (if (and (= numer1 " ") (= numer "3")) (setetx2 "e"))
               (if (and (= numer1 " ") (= numer "5")) (setetx2 "t"))
               (if (and (= numer1 "1") (= numer "1")) (setetx3 "a"))
               (if (and (= numer1 "1") (= numer "3")) (setetx3 "d"))
               (if (and (= numer1 "1") (= numer "5")) (setetx3 "g"))
            )  ;progn
         )    ;if 1/16"
      )  ;progn
   )
)

;-- Substitute fraction character for fraction string ("x/x")
(defun setetx1 (ntx)
    (if (>= slloc 3)
       (setq etxt (strcat (substr etxt 1 (- slloc (+ 3 mask)))
               ntx (substr etxt (+ slloc 2))))
       (setq etxt (strcat ntx (substr etxt (+ slloc 2)))))
    (setq movdis tht)
)
(defun setetx2 (ntx)
    (if (>= slloc 3)
       (setq etxt (strcat (substr etxt 1 (- slloc (+ 3 mask)))
               ntx (substr etxt (+ slloc 3))))
       (setq etxt (strcat ntx (substr etxt (+ slloc 3)))))
    (setq movdis tht)
)
(defun setetx3 (ntx)
    (if (>= slloc 4)
       (setq etxt (strcat (substr etxt 1 (- slloc (+ 4 mask)))
               ntx (substr etxt (+ slloc 3))))
       (setq etxt (strcat ntx (substr etxt (+ slloc 3)))))
    (setq movdis tht)
)

(defun fract_text ()      ;Uses entity en1
    (setq etxt (cdr (assoc 1 en1)))
    (setq justi (cdr (assoc 72 en1)))
    (parse_etxt)
    (if (or (< ctest 48) (> ctest 57)) (setq movdis 0))
    (if (> trot 0.5) (setq movvup movdis movdis 0) (setq movvup 0))
    (if (> slloc 0)                  ;Only update if "/" found
       (progn
          (setq en1 (subst
                        (cons 7 "ROMANDFR")
                        (assoc 7 en1)
                        en1
                    )
                en1 (subst
                        (cons 1 etxt)
                        (assoc 1 en1)
                        en1
                    )
                en1 (subst
                         (cons 10 (list (+ (car tloc) movdis)
                                  (+ (cadr tloc) movvup) (cadr (cdr tloc))))
                         (assoc 10 en1)
                         en1
                    )
          )
          (entmod en1)         ;Modify entity
       )   ;prog
    )    ;if slloc > 0
)

