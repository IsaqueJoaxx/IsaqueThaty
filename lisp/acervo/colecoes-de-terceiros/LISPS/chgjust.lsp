; CHGJUST.LSP 
; Change the justification of text entities.
; Part of the Text Utilities Kit Version 1.0
; Copyright 1989 Alacrity

; Alacrity
; 12405 SE 25th St
; Bellevue WA 98005
; Voice: (206)746-0680
; BBS: (206)643-5477
; CompuServe: 73417,1756

; Shareware software, If you use -- please don't abuse!

(princ "\nCHGJUST.LSP - (c)1989 Alacrity\n")

;-----------------------
; Filter Selection Sets
;-----------------------
(defun ssfilter (ss lst kill / len i group data match ename elist)
  (princ "\nFiltering selection set...")
  (setq i 0 group (car lst) data (cdr lst))
  (while (ssname ss i)
    (setq ename (ssname ss i) elist (entget ename)
          match (== data (cdr (assoc group elist))))
    (if (= kill match)
      (ssdel ename ss)
      (setq i (1+ i))
    )
  )
  (if (ssname ss 0) ss nil)
)

;-------------
; Fuzzy Equal
;-------------
(defun == (a b)
  (if (member (type a) '(INT REAL)) (> 1.0E-6 (abs (- a b))) (= a b))
)

(defun c:CHGJUST (/ ss elist ename num just key find repl old getjust)
  ;-------------------
  ; Get Justification
  ;-------------------
  (defun getjust (key)
    (length (member key '("Fit" "Middle" "Align" "Right" "Center")))
  )
  ;----------------------
  ; Select TEXT Entities
  ;----------------------
  (cond
    ((setq ss (ssget)) (ssfilter ss '(0 . "TEXT") nil))
    (T
      (princ "\nSelecting all TEXT in drawing...")
      (setq ss (ssget "X" '((0 . "TEXT"))))
    )
  )
  (princ (strcat "\n" (itoa (sslength ss)) " TEXT entities selected."))
  ;----------------
  ; The Main Stuff
  ;----------------
  (if ss
    (progn
      (initget "All Left Right Center Middle Align Fit")
      (setq key (getkword "\nMATCH Left/Right/Center/Middle/Align/Fit/<All>: "))
      (cond
        (key (setq find (getjust key)))
        (T (setq find 0))
      )
      (initget 1 "Left Right Center Middle")
      (setq key (getkword "\nCHANGE Left/Right/Center/Middle: "))
      (setq repl (getjust key)
            num 0
            i -1
      )
      (while (setq ename (ssname ss (setq i (1+ i))))
        (setq elist (entget ename)
              old (cdr (assoc 72 elist))
        )
        (if (or (zerop find) (= find old))
          (progn
            (if (member old '(0 3 5))
              (setq a 11 b 10)
              (setq a 10 b 11)
            )
            (setq elist (subst (cons a (cdr (assoc b elist))) (assoc a elist) elist))
            (entmod (subst (cons 72 repl) (cons 72 old) elist))
            (setq num (1+ num))
          )
        )
      )
    )
  )
  (princ (strcat "\n" (itoa num) " lines changed."))
  (princ)
)

; End Of File
