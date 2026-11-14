; CHGTEXT.LSP
; Find and replace string for text entities
; Part of the Text Utility Kit Version 1.0
; Copyright 1989 Alacrity

; Alacrity
; 12405 SE 25th St
; Bellevue WA 98005
; Voice: (206)746-0680
; BBS: (206)643-5477
; CompuServe: 73417,1756

; Shareware software, If you use -- Please don't abuse!

(princ "\nCHGTEXT.LSP - (c)1989 Alacrity\n")

;----------------------
; Filter Selection Set
;----------------------
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

(defun C:CHGTEXT (/ ss findstr find repl old new ename elist case
                    len1 len2 len3 num i)
  ;-------------
  ; Find String
  ;-------------
  (defun findstr (old find repl case)
    (setq len1 (strlen old) 
          len2 (strlen find) 
          len3 (strlen repl) 
          i 1 
          num 0
    )
    (while (<= i len1)
      (if 
        (if case
          (equal (substr old i len2) find)
          (equal (strcase (substr old i len2)) (strcase find))
        )
        (setq old (strcat (if (> i 1) (substr old 1 (1- i)) "") 
              repl (substr old (+ i len2))) 
              len1 (strlen old) 
              i (+ i len3)
        )
        (setq i (1+ i))
      )
    )
    old
  )
  ;-----------------------------
  ; Get TEXT entities to change
  ;-----------------------------
  (cond
    ((setq ss (ssget)) (ssfilter ss '(0 . "TEXT") nil))
    (T
      (princ "\nSelecting all TEXT in drawing.")
      (setq ss (ssget "X" '((0 . "TEXT"))))
    )
  )
  (princ (strcat "\n" (itoa (sslength ss)) " TEXT entities selected."))
  ;------------------------------
  ; Get FIND and REPLACE strings
  ;------------------------------
  (if ss
    (progn
      (initget 1)
      (setq find (getstring T "\nSearch text: ")
            repl (getstring T "\nReplace with: ")
            i 0 num 0)
      (initget "Yes No")
      (setq case (if (= (getkword "\nCase sensitive? Yes/<No>: ") "Yes") T nil))
      (while (setq ename (ssname ss i))
        (setq elist (entget ename)
              old (cdr (assoc 1 elist))
              new (findstr old find repl case)
              i (1+ i)
        )
        (if (/= old new)
          (progn
            (entmod (subst (cons 1 new) (cons 1 old) elist))
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
