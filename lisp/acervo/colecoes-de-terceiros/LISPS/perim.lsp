;;;********************;;;
;Here is one my babies. You can hammer it, bump it, twist it or bang the
;monitor with your mouse etc., it should stand the abuse and so should
;the environment in your system. Give it a rough time and let me know
;what you think.
;Two years ago I knew less than screening about computers, let alone CAD
;programs, I am a tool and die designer and (mostly) maker.
;There is about thirty hours of tinkering on these few lines. It's been
;edited with WCEDIT (tm) from Elsa America Inc. And (no) there is no
;copyright warning or donation request assorted with it.
;Explicitely it is free. Please spread it around.
;
;My name is:
;
;Yvan Paquet
;219 rue Baillarg‚
;Boucherville, Qc.
;Canada
;J4B 1E8
;
;Compuserve ID 76460,774
;
;Pour quiconque le souhaite, ce programme est disponible avec tous
;les affichages (prompts) en francais.
;;; --------------------------------------------------------------------------;
;Routine to read, calculate and write at chosen location on screen,
;the perimeter or length of a selected object.
;May be useful to those who, like I, design and cost blanking dies or WEDM
;work in general.
;;; --------------------------------------------------------------------------;
;**************** Deluxe error handler
(defun prmerr (s)
  (if (/= s "Function cancelled")      ; If an error (such as CTRL-C) occurs
(progn (princ (strcat "\nError: " s))  ; while this command is active...
(command "layer" "S" cla "")           ;  Actually this whole paragraph is
(setvar "cmdecho" cmo)                 ;  here only to avoid your text screen
(setvar "menuecho" mno)                ;  or command line to be littered with
(setvar "dimtxt" dmt)                  ;  stoopid parentheses.
       (setq *error* olderr)           ; Restore old *error* handle
       (princ))                        ; Land softly on comfy butt
  )
)
;
;**************** Engine room
(defun perim (/ procs entp ents ead lp1 lp2 aad acp arr entyp kwd p1 p2 wcl
                dmt cla cmo mno entl olderr prmerr)
     (setq olderr *error*
           *error* prmerr)
(setq procs T)
(while procs
(setq entp (entsel "\nPick entity at point where leader should start: "))
(cond ((boundp 'entp)
 (progn (setq p1 (nth 0 (cdr entp)))
        (setq ents (car entp))
        (setq ead (entget ents))
        (setq entyp (cdr (assoc 0 ead))))))
(cond ((equal entyp "LINE")
 (progn (princ "\nThis is a LINE and should be  a polyline! ")
        (princ "\n  Process as is -or- Exit to terminate?  ")
        (initget "Process_line Exit")
        (setq kwd (getkword "\n   Type in your choice: ")))
        (princ))
      ((equal entyp "ARC")
 (progn (princ "\nThis is an ARC and should be  a polyline! ")
        (princ "\n  Process as is -or- Exit to terminate?  ")
        (initget "Process_arc Exit")
        (setq kwd (getkword "\n   Type in your choice: ")))
        (princ)))

(cond ((equal kwd "Exit")
       (setq procs nil))

      ((equal kwd "Process_line")
       (progn (setq lp1 (cdr (assoc 10 ead)))
              (setq lp2 (cdr (assoc 11 ead)))
              (setq wcl (rtos (distance lp1 lp2) 2 3))))

      ((equal kwd "Process_arc")
       (progn (setq acp (cdr (assoc 10 ead)))
              (setq arr (cdr (assoc 40 ead)))
              (setq aad (atof (angtos (- (max (cdr (assoc 50 ead))
                                              (cdr (assoc 51 ead)))
                                         (min (cdr (assoc 50 ead))
                                              (cdr (assoc 51 ead)))) 0 5)))
              (setq wcl (rtos (* aad (/ (* pi (* arr 2)) 360)) 2 3))))

 ((or (equal entyp "POLYLINE")
      (equal entyp "CIRCLE"))
       (progn (command "list" ents "")
              (setq wcl (rtos (getvar "perimeter") 2 3))
              (princ)))
     )
(cond
((boundp 'wcl)
(setq dmt (getvar "dimtxt")
      cla (getvar "clayer")
      cmo (getvar "cmdecho")
      mno (getvar "menuecho")
      entl (cdr (assoc 8 (entget ents))))
(setvar "dimtxt" 0.15)
(setvar "cmdecho" 0)
(setvar "menuecho" 1)
(setq wcl (strcat "WEDM Cut Length: " wcl))
(graphscr)
(setq p2 (getpoint p1 "\nPick where text should start: "))
(command "LAYER" "S" entl "")
(command "dim1" "L" p1 p2 "" wcl)
(command "LAYER" "S" cla "")
(setvar "cmdecho" cmo)
(setvar "menuecho" mno)
(setvar "dimtxt" dmt)
              )
     )
(setq procs nil)
)
       (setq *error* olderr)           ; Restore old *error* handle
       (princ))                        ; Land softly on comfy butt

;**************** Driver seat
(defun c:prm () (perim))
(defun c:perim () (perim))
(princ
  "\nStart command with PRM or PERIM")
(princ)
