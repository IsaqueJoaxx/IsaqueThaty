; CHGSIZE.LSP
; Change various text properties.
; Part of the Text Utility Kit Version 1.0
; Copyright 1989 Alacrity

; Alacrity
; 12405 SE 25th St
; Bellevue WA 98005
; Voice: (206)746-0680
; BBS: (206)643-5477
; CompuServe: 73417,1756

; Shareware software, If you use -- Please don't abuse!

(princ "\nCHGSIZE.LSP - (c)1989 Alacrity\n")

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

(defun C:CHGSIZE (/ ss ename elist chglst pick ename layer style
                    obliq width rot key group lst key getkey)
  ;------------------------
  ; Pick attribute of text
  ;------------------------
  (defun pick (group)
    (while 
      (and
        (setq ename (car (entsel)))
        (setq elist (entget ename))
        (/= (cdr (assoc 0 elist)) "TEXT")
      )
      (princ "\nInvalid ...")
    )
    (apply
      'princ
      (list "\nPicked " (strcase key T) " is : " (cdr (assoc group elist)))
    )
  )
  ;---------
  ; Get Key
  ;---------
  (defun getkey (msg)
    (initget "Style Layer Height Rot Width Obliq")
    (setq key (getkword (strcat "\n" msg " Style/Height/Rot/Width/Obliq/Layer: ")))
  )
  ;----------------
  ; Get Style Name
  ;----------------
  (defun style (msg)
    (while
      (not
        (cond
          ((= (setq key (strcase (getstring msg))) "P") (setq key (pick 7)))
          ((tblsearch "style" key) T)
          (T nil)
        )
      )
      (princ "\nInvalid style name.")
    )
    (cons 7 key)
  )
  ;----------------
  ; Get Layer Name
  ;----------------
  (defun layer (msg)
    (while
      (not
        (cond
          ((= (setq key (strcase (getstring msg))) "P") (setq key (pick 8)))
          ((tblsearch "layer" key) T)
          (T nil)
        )
      )
      (princ "\nInvalid layer name.")
    )
    (cons 8 key)
  )
  ;------------
  ; Get Height
  ;------------
  (defun height (msg)
    (initget 7 "Pick")
    (setq key (getdist msg))
    (if (= key "Pick") (setq key (pick 40)))
    (cons 40 key)
  )
  ;--------------
  ; Get Rotation
  ;--------------
  (defun rot (msg)
    (initget 1 "Pick")
    (setq key (getangle msg))
    (if (= key "Pick") (setq key (pick 50)))
    (cons 50 key)
  )
  ;-----------
  ; Get Width
  ;-----------
  (defun width (msg)
    (initget 1 "Pick")
    (setq key (getreal msg))
    (if (= key "Pick") (setq key (pick 41)))
    (cons 41 key)
  )
  ;-----------------
  ; Get Obliq angle
  ;-----------------
  (defun obliq (msg)
    (while
      (progn
        (not (initget 1 "Pick"))
        (setq key (getorient msg))
        (cond
          ((and (> key 1.483530) (< key 4.799655))
            (princ "\nAngle too large.")
          )
          ((= key "Pick")
            (not (setq key (pick 51)))
          )
        )
      )
    )
    (cons 51 key)
  )
  ;-----------------------------
  ; Get TEXT entities to change
  ;-----------------------------
  (cond
    ((setq ss (ssget))
      (setq ss (ssfilter ss '(0 . "TEXT") nil))
    )
    (T
      (princ "\nSelecting all TEXT in drawing.")
      (setq ss (ssget "X" '((0 . "TEXT"))))
    )
  )
  ;--------------------------
  ; Specify MATCH parameters
  ;--------------------------
  (while
    (and
      (princ (strcat "\n" (itoa (sslength ss)) " entities selected."))
      (ssname ss 0)
      (setq key (getkey "MATCH"))
    )
    (setq msg (strcat "\nEnter " (strcase key) " to match (or Pick): ")
          key (eval (list (read key) msg))
          ss (ssfilter ss key nil)
    )
  )
  ;---------------------------
  ; Specify CHANGE parameters
  ;---------------------------
  (if ss
    (while
      (setq key (getkey "CHANGE"))
      (setq msg (strcat "\nEnter new " (strcase key) " (or Pick): "))
      (setq key (eval (list (read key) msg)))
      (setq chglst (append (list key) chglst))
    )
  )
  ;----------------------
  ; Change TEXT entities
  ;----------------------
  (if (and ss chglst)
    (progn
      (setq i 0)
      (while (setq ename (ssname ss i))
        (setq elist (entget ename) i (1+ i))
        (foreach
          group
          chglst
          (setq elist (subst group (assoc (car group) elist) elist))
        )
        (entmod elist)
      )
      (princ (strcat "\n" (itoa (sslength ss)) " entities changed."))
    )
  )
  ; All done
  (princ)
)

; End Of File
