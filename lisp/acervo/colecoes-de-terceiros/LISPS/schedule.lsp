; SCHEDULE.LSP
; Import schedules into drawing
; Part of the Text Utility Kit Version 1.0
; Copyright 1989 Alacrity

; Alacrity
; 12405 SE 25th St
; Bellevue WA 98005
; Voice: (206)746-0680
; BBS: (206)643-5477
; CompuServe: 73417,1756

; Shareware software, If you use -- Please don't abuse!

(princ "\nSCHEDULE.LSP - (c)1989 Alacrity\n")

(defun C:SCHEDULE (/ minus plus view tabload create import dparse fparse rtod
                     pt1 pt2 fhandle input temp i a b len lst str group getfile
                     parlst tablst col base hite rot just delim offset err)
  ;--------------------
  ; Radians to decimal
  ;--------------------
  (defun rtod (x) (* x (/ 180.0 pi)))
  ;--------------
  ; Get Filename
  ;--------------
  (defun getfile (arg1 arg2 arg3 arg4 / fhandle fname msg)
    (setq msg (strcat arg1 (if arg2 (strcat " <" (strcase arg2) ">") "") ": "))
    (while
      (progn
        (setq fname (strcase (getstring msg)))
        (if (and (= fname "") arg2)
          (setq fname arg2)
        )
        (if (/= fname "")
          (setq fhandle (open (strcat fname (if arg3 arg3 "")) arg4))
        )
        (not fhandle)
      )
      (princ "\nInvalid file name, Try again.")
    )
    (cons fname fhandle)
  )
  ;---------------
  ; Error Handler
  ;---------------
  (setq err *error*)
  (defun *error* (msg)
    (setq *error* err)
    (if fhandle (setq fhandle (close fhandle)))
    (princ "\nError: ")
    (princ msg)
    (princ)
  )
  ;---------------------
  ; Add offset to point
  ;---------------------
  (defun plus (pt1 pt2)
    (list (+ (car pt1) (car pt2)) (+ (cadr pt1) (cadr pt2)))
  )
  ;----------------------------
  ; Subtract offset from point
  ;----------------------------
  (defun minus (pt1 pt2)
    (list (- (car pt1) (car pt2)) (- (cadr pt1) (cadr pt2)))
  )
  ;-------------------
  ; Delimited parsing
  ;-------------------
  (defun dparse (str / b i len lst)
    (setq len (strlen str)
          b 1
          i 1
          delim ","
    )
    (while (<= i len)
      (while
        (and
          (<= i len)
          (/= (substr str i 1) delim)
          (setq i (1+ i))
        )
      ) 
      (setq lst (append lst (list (substr str b (- i b))))
            i (1+ i)
            b i
      )
    )
    lst
  )
  ;---------------
  ; Field parsing
  ;---------------
  (defun fparse (str / lst a b len temp)
    (mapcar
      '(lambda (group)
        (setq a (car group)
              b (- (cdr group) a -1)
              temp (substr str a b)
        )
        (while (= (substr temp (setq len (strlen temp))) " ")
          (setq temp (substr temp 1 (1- len)))
        )
        (setq lst (append lst (list temp)))
      )
      parlst
    )
    lst
  )
  ;----------------------------
  ; View table definition file
  ;----------------------------
  (defun view ()
    (setq fname (getfile "\nDefinition filename" fname ".DEF" "r")
          fhandle (close (cdr fname))
          fname (car fname)
    )
    (command "TYPE" (strcat fname ".DEF"))
  )
  ;----------------------------
  ; Load table definition from 
  ;----------------------------
  (defun tabload ()
    (initget 1)
    (setq tabnym (strcase (getstring "\nDefinition name to load: ")))
    (setq fname (getfile "\nFile to search" fname ".DEF" "r")
          fhandle (cdr fname)
          fname (car fname)
          tablst nil
    )
    (while
      (and
        (setq temp (read-line fhandle))
        (if (= (strcase (substr temp 2)) tabnym)
          (progn
            (setq parlst (read (read-line fhandle))
                  tablst (read (read-line fhandle))
                  col    (read (read-line fhandle))
            )
            (if parlst
              (setq parse fparse)
              (setq parse dparse)
            )
            nil
          )
          T
        )
      )
    )
    (setq fhandle (close fhandle))
    (if tablst 
      (princ (strcat "\nDefinition " tabnym " loaded."))
      (princ (strcat "\nDefinition " tabnym " not found!"))
    )
  )
  ;-------------------------
  ; Create table definition
  ;-------------------------
  (defun create ()
    (if (/= "" (setq tabnym (strcase (getstring "\nName of definition to create: "))))
      (progn
        (setq parlst nil
              tablst nil
        )
        (initget "Comma Field")
        (if (not (setq tparse (getkword "\nImport by Field/<Comma>: ")))
          (setq tparse "Comma")
        )
        (if (= tparse "Comma")
          (setq parse dparse)
          (setq parse fparse)
        )
        (initget 1)
        (setq base (getpoint "\nBase point: ")
              pt1 base
              i 1
        )
        (while (setq pt2 (getpoint pt1 (strcat "\nCell #" (itoa i) " offset: ")))
          ; Field parsing?
          (if (= tparse "Field")
            ; Yes, get field positions
            (progn
              (initget 3)
              (setq a (getint "\nStarting character position: "))
              (initget 3)
              (setq b (getint "\nEnding character position: ")
                    parlst (append parlst (list (cons a b)))
              )
            )
          )
          ; Style of cell
          (if (/= (setq temp (strcase (getstring (strcat "Style name <" style ">: ")))) "")
            (setq style temp)
          )
          ; Justification of cell
          (initget "Left Right Center Middle")
          (if (setq temp (getkword (strcat "\nJustification Left/Right/Center/Middle/<" just ">: ")))
            (setq just temp)
          )
          ; Height of cell
          (initget 2)
          (if (setq temp (getdist pt2 (strcat "\nHeight <" (rtos hite) ">: ")))
            (setq hite temp)
          )
          ; Rotation of cell
          (if (setq temp (getorient pt2 (strcat "\nRotation <" (angtos rot) ">: ")))
            (setq rot temp)
          )
          (setq offset (minus pt2 base)
                tablst (append tablst (list (list offset style just hite rot)))
                i (1+ i)
                pt1 pt2
          )
        )
        (initget 1)
        (setq col (getpoint base "\nRow offset: ")
              col (minus col base)
        )
        ; Save table definition to file?
        (initget "Yes No")
        (if (= (getkword (strcat "\nSave " tabnym " to file? Yes/<No>: ")) "Yes")
          (progn
            (setq fname (getfile "\nFilename" fname ".DEF" "a")
                  fhandle (cdr fname)
                  fname (car fname)
            )
            (princ (strcat "\n*" tabnym) fhandle)
            (print parlst fhandle)
            (print tablst fhandle)
            (print col fhandle)
            (setq fhandle (close fhandle))
            (princ (strcat "\nDefinition " tabnym " successfully saved."))
          )
        )
      )
    )
  )
  ;-------------
  ; Import text
  ;-------------
  (defun import ()
    (initget 1)
    (setq base (getpoint "\nBase point: ")
          pt1 base
          i 1
          data (getfile "\nFile to import" data nil "r")
          input (cdr data)
          data (car data)
    )
    ; Read in table
    (while (setq temp (read-line input))
      (setq strlst (parse temp))   
      (mapcar
        '(lambda (group temp)
          (if (/= temp "")
            (progn
              (setq offset (car group)
                    style (cadr group)
                    just (caddr group)
                    hite (cadddr group)
                    rot (last group))
              (command "TEXT" "S" style)
              (if (/= just "Left") (command (substr just 1 1)))
              (command (plus base offset))
              (if (zerop (cdr (assoc 40 (tblsearch "STYLE" style))))
                (command hite)
              )
              (command (rtod rot) temp)
            )
          )
        )
        tablst
        strlst
      )
      (setq base (plus base col))
    )
    (setq input (close input))
  )
  ;----------------------
  ; Setup initial values
  ;----------------------
  (setvar "CmdEcho" 0)
  (setq strlst nil)
  (if (not style)  (setq style (getvar "Textstyle")))
  (if (not hite)   (setq hite (getvar "TextSize")))
  (if (not rot)    (setq rot (getvar "LastAngle")))
  (if (not just)   (setq just "Left"))
  (if (not tparse) (setq tparse "Delim"))
  ;-------------------------
  ; Main Routine (Oh Yeah!)
  ;-------------------------
  (while
    (progn
      (initget "? Load Create Import")
      (setq temp (getkword "\n?/Load/Create/Import: "))
    )
    (cond
      ((= temp "?") (view))
      ((= temp "Load") (tabload))
      ((= temp "Create") (create))
      ((= temp "Import") 
        (progn
          (if (not tablst) (create))
          (import)
        )
      )
    )
  )
  (setq *error* err)
  (princ)
)

; End Of File
