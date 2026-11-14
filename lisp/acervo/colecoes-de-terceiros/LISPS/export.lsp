; EXPORT.LSP
; Writes selected text entities to file
; Part of the Text Utility Kit Version 1.0
; Copyright 1989 Alacrity

; Alacrity
; 12405 SE 25th St
; Bellevue WA 98005
; Voice: (206)746-0680
; BBS: (206)643-5477
; CompuServe: 73417,1756

; Shareware software, If you use -- please don't abuse!

(princ "\nEXPORT.LSP - (c)1989 Alacrity\n")

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

(defun C:EXPORT (/ ss ename elist fspec msg key inc i err)
  ;--------------------------
  ; Standard *ERROR* Handler
  ;--------------------------
  (setq err *error*)
  (defun *error* (msg)
    ; Close open file
    (if fspec (setq fspec (close fspec)))
    ; Reset *ERROR* Handler
    (setq *error* err)
    (princ "\nerror: ")
    (princ msg)
    (princ)
  )
  ;-----------------------------
  ; Get TEXT entities to change
  ;-----------------------------
  (cond
    ((setq ss (ssget))
      (setq ss (ssfilter ss '(0 . "TEXT") nil))
    )
    (T
      (princ "\nSelecting all TEXT in drawing...")
      (setq ss (ssget "X" '((0 . "TEXT"))))
    )
  )
  (princ (strcat "\n" (itoa (sslength ss)) " objects selected."))
  ;-----------------
  ; EXPORT Entities
  ;-----------------
  (cond
    (ss
      (initget "Yes No")
      (if (= (getkword "\nReverse selection order? Yes/<No>: ") "Yes")
        (setq inc 1- i (sslength ss))
        (setq inc 1+ i -1)
      )
      ; Get File name 
      (while
        (progn
          (setq key
            (getstring 
              (strcat "\nASCII file" (if fname (strcat " <" fname ">: ") ": "))
            )
          )
          (if (not (equal key ""))
            (setq fname key)
          )
          (cond 
            ((not fname) (princ "\nInvalid input."))
            ((setq fspec (open fname "a")) nil)
            (T (princ "\nFile not found."))
          )
        )
      )
      ; Write Text Entities to File
      (princ "\nWriting to file...")
      (while (setq ename (ssname ss (setq i (inc i))))
        (prin1 (cdr (assoc 1 (entget ename))) fspec)
      )
      (princ " done.")
      ; Close file
      (setq fspec (close fspec))
    )
  )
  (setq *error* err)
  (princ)
)

; End Of File
