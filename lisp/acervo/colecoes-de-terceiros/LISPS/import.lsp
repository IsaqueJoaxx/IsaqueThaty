; IMPORT.LSP 
; Import ASCII text file(s) into drawing
; Part of the Text Utility Kit Version 1.0
; Copyright 1989 Alacrity

; Alacrity
; 12405 SE 25th St
; Bellevue WA 98005
; Voice: (206)746-0680
; BBS: (206)643-5477
; CompuServe: 73417,1756

; Shareware software, If you use -- please don't abuse!

(princ "\nIMPORT.LSP - (c)1989 Alacrity\n")

(defun C:IMPORT (/ pt msg imput data group elist old new key temp
                   maketxt group modent modtxt getstyle getheight
                   getrot getline getjust a b >90 strcvt getcase)
  (setvar "CmdEcho" 0)
  ;--------------------------
  ; Standard *ERROR* Handler
  ;--------------------------
  (setq err *error*)
  (defun *error* (msg)
    ; Close open files
    (if input (setq input (close input)))
    ; Reset error handler
    (setq *error* err)
    (princ "\nerror: ")
    (princ msg)
    (princ)
  )
  ;-----------
  ; Make TEXT 
  ;-----------
  (defun maketxt ()
    (command "TEXT")
    (if (/= just "Left") (command (substr just 1 1)))
    (command pt)
    (if height (command height))
    (command (* rot 57.2957) (strcvt data))
  )
  ;-----------------
  ; Get Entity Data
  ;-----------------
  (defun field (group)
    (setq elist (entget (entlast))
          old (cdr (assoc group elist))
    )
  )
  ;-------------------------
  ; Modify Entity Data List
  ;-------------------------
  (defun modent (new old group)
    (entmod (subst (cons group new) (cons group old) elist))
  )
  ;-------------
  ; Modify TEXT
  ;-------------
  (defun modtxt (new group)
    (field group)
    ; Has TEXT changed?
    (if (/= new old) 
      ; Yes, modify entity
      (modent new old group)
    )
    T
  )
  ;---------------------
  ; Convert String Case
  ;---------------------
  (defun strcvt (data)
    (cond
      ((= case "Upper") (strcase data))
      ((= case "Lower") (strcase data T))
      (T data)
    )
  )
  ;---------------------
  ; Get case of strings
  ;---------------------
  (defun getcase ()
    (initget "Upper Lower Norm")
    (if (setq key (getkword (strcat "\nString case? Upper/Lower/Norm/<" case ">: ")))
      (setq case key)
    )
  )
  ;----------------
  ; Get Style name
  ;----------------
  (defun getstyle ()
    (while
      (progn
        (setq key (strcase (getstring (strcat "Style name <" style ">: "))))
        (cond
          ((= key "") nil)
          ((tblsearch "style" key) (setq style key key nil))
          (T (princ "Style not found. "))
        )
      )
    )
    (setq key (cdr (assoc 40 (tblsearch "style" style))))
    (command "TEXT" "Style" style)
    (command)
    (if (zerop key)
      (if (not height) (setq height (GETVAR "TextSize")))
      (setq height nil
            lf (* key 1.6666)
            msg "")
    )
    style
  )
  ;---------------
  ; Get TEXT Hite
  ;---------------
  (defun getheight ()
    (initget 6)
    (setq key (getdist (strcat "Height <" (rtos (if height height (field 40))) ">: ")))
    (cond
      ((and height key)
        (setq height key)
      )
      ((and height (not key))
        height
      )
      (key
        key
      )
      ((not key)
        (field 40)
      )
    )
  )
  ;-------------------
  ; Get TEXT Rotation
  ;-------------------
  (defun getrot ()
    (if (setq key (getorient (strcat "Rotation <" (angtos rot) ">: ")))
      (setq rot key)
    )
    rot
  )
  ;--------------
  ; Get Linefeed
  ;--------------
  (defun getline ()
    (if (setq key (getdist (strcat "Linefeed <" (rtos lf) ">: ")))
      (setq lf key)
    )
    lf
  )
  ;------------------------
  ; Get TEXT Justification
  ;------------------------
  (defun getjust ()
    (initget "Center Middle Right Left")
    (if (setq key (getkword (strcat "Justification Left/Right/Center/Middle/<" just ">: ")))
      (setq just key)
    )
    just
  )
  ;--------------------
  ; Get file to IMPORT
  ;--------------------
  (while
    (progn
      (setq key (strcase (getstring (strcat "\nASCII file"
        (if fname (strcat " <" fname ">: ") ": ")))))
      (if (not (equal key ""))
        (setq fname key)
      )
      (cond 
        ((not fname) (princ "\nInvalid input."))
        ((setq input (open fname "r")) nil)
        (T (princ "\nFile not found."))
      )
    )
  )
  ;----------------
  ; Init variables
  ;----------------
  (setq >90 (/ pi 2))
  (if (not style) (setq style (getvar "TextStyle")))
  (if (not just) (setq just "Left"))
  (if (not rot) (setq rot 0.0))
  (if (not case) (setq case "Norm"))
  (getjust)
  (getstyle)
  (if height (getheight))
  (getrot)
  (if height (setq lf (* height 1.6666)))
  (getline)
  (getcase)
  (graphscr)

  ;--------------
  ; Main routine
  ;--------------
  (while (setq data (read-line input))
    (if (/= data "")
      (progn
        (cond 
          (pt
            (setq pt (polar pt (- rot >90) lf))
            (maketxt)
            (setq pt (getvar "LastPoint"))
          )
          (T
            (setq pt (getvar "ViewCtr"))
            (maketxt)
            (princ "\nStart point: ")
            (command "MOVE" "L" "" pt pause)
            (setq pt (getvar "LastPoint"))
          )
        )
        (while
          (progn
            (not (initget 6 "Move Style Just Lf Height Rot Width Obliq Case Next"))
            (setq key (getint "\nNumber or Move/Style/Just/Lf/Height/Rot/Width/Obliq/Case/<Next>: "))
            (cond
              ((= key "Next") nil)
              ((= key "Style") (modtxt (getstyle) 7))
              ((= key "Height") (modtxt (getheight) 40))
              ((= key "Rot") (modtxt (getrot) 50))
              ((= key "Lf") (setq lf (getline)))
              ((numberp key)
                (while
                  (and
                    (not (zerop key))
                    (setq data (read-line input))
                  )
                  (setq pt (polar pt (- rot >90) lf))
                  (maketxt)
                  (setq pt (getvar "LastPoint"))
                  (setq key (1- key))
                )
                nil
              )
              ((= key "Move")
                (princ "\nStart point: ")
                (command "MOVE" "L" "" pt pause)
                (setq pt (getvar "LastPoint"))
              )
              ((= key "Width")
                (field 41)
                (initget 6)
                (if (setq new (getreal (strcat "Width factor <" (rtos old) ">: ")))
                  (modent new old 41)
                )
                T
              )
              ((= key "Obliq")
                (field 51)
                (if (setq new (getorient (strcat "Obliqueing angle <" (angtos old) ">: ")))
                  (if (and (> new 1.483530) (< new 4.799655))
                    (princ "\nAngle too large.")
                    (modent new old 51)
                  )
                )
                T
              )
              ((= key "Just")
                (setq new (getjust))
                (field 72)
                (setq new (length (member new '(nil "Middle" nil "Right" "Center"))))
                (if (/= new old)
                  (progn
                    (if (member old '(0 3 5))
                      (setq a 11 b 10)
                      (setq a 10 b 11)
                    )
                    (setq elist (subst (cons a (cdr (assoc b elist)))
                      (assoc a elist) elist))
                    (modent new old 72)
                  )
                )
                T
              )
              ((= key "Case")
                (getcase)
                (field 1)
                (if (/= (setq new (strcvt data)) old)
                  (modent new old 1)
                )
                T
              )
            )
          )
        )
      )
    )
  )
  (setq *error* err
        input (close input))
  (setvar "CmdEcho" 1)
  (princ)
)

; End Of File
