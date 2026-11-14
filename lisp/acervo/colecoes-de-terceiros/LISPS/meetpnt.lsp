;;;********************;;;
;Here is my last baby. You can hammer it, bump it, twist it or bang the
;monitor with your mouse etc., it should stand the abuse and so should
;the environment in your system. Give it a rough time and let me know
;what you think.
;Two years ago I knew less than screening about computers, let alone CAD
;programs, I am a tool and die designer and (mostly) maker.
;There is about thirty hours of tinkering on these few lines. It's been
;edited with WCEDIT (tm) from Elsa America Inc. And (no) there is no
;copyright warning or donation request assorted with it.
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
;Routine to draw on your current layer, a yellow point at would be
;intersection point of two lines wich would meet only if extended.
;it works with LINES and 3DLINES only.
;
;Pour quiconque le souhaite, ce programme est disponible avec tous
;les affichages (prompts) en francais.
;
;Modify slightly the Engine room and it'l feed transparently any
;command that needs a point.
;;; --------------------------------------------------------------------------;
;**************** Deluxe error handler
(defun mpterr (s)
  (if (/= s "Function cancelled")      ; If an error (such as CTRL-C) occurs
(progn (princ (strcat "\nError: " s))  ; while this command is active...
       (setvar "OSMODE" OSM)           ;  Actually this whole paragraph is
       (setvar "PDMODE" PDM)           ;  here only to avoid your text screen
       (setvar "PDSIZE" PDS)           ;  or command line be littered with
       (command "COLOR" CEC)           ;  stoopid parentheses.
       (setvar "CMDECHO" CMD)
       (setvar "ORTHOMODE" ORT)
       (setvar "MENUECHO" MNE)
       (setq *error* olderr)           ; Restore old *error* handler
       (princ))                        ; Land softly on comfy butt
  )
)
;
;**************** Engine room
(defun MEETPNT (/ olderr ntsel1 ntsel2 ent2 ent1 MNE ORT CMD OSM PDM PDS CEC)
     (setq olderr *error*
           *error* mpterr)
     (setq ntsel1 T)
     (While ntsel1
          (setq ent1 (car (entsel "\nPick 1st line ")))
          (if (and (boundp 'ent1)
           (or (eq "LINE" (cdr (assoc 0 (entget ent1))))
               (eq "3DLINE" (cdr (assoc 0 (entget ent1))))))
               (progn (setq ntsel1 nil) (princ "\nFirst entity OK ")
               (princ))
     (progn (princ "\n!! Pick LINES or 3DLINES only !!") (princ)))
     )
     (setq ntsel2 T)
     (While ntsel2
          (setq ent2 (car (entsel "\nPick 2nd line ")))
          (if (and (not (eq ent1 ent2))
                   (boundp 'ent2)
           (or (eq "LINE" (cdr (assoc 0 (entget ent2))))
               (eq "3DLINE" (cdr (assoc 0 (entget ent2))))))
               (progn (setq ntsel2 nil) (princ "\nSecond entity OK ")
               (princ))
     (progn (princ "\n!! Pick different LINES or 3DLINES only !!") (princ)))
     )
     (setq pt1 (cdr (assoc 10 (entget ent1 )))
           pt2 (cdr (assoc 11 (entget ent1 )))
           pt3 (cdr (assoc 10 (entget ent2 )))
           pt4 (cdr (assoc 11 (entget ent2 ))))
     (setq MNE (getvar "MENUECHO")
           ORT (getvar "ORTHOMODE")
           CMD (getvar "CMDECHO")
           OSM (getvar "OSMODE")
           PDM (getvar "PDMODE")
           PDS (getvar "PDSIZE")
           CEC (getvar "CECOLOR"))
     (setvar "CMDECHO" 0)
     (command "COLOR" 2)
     (setvar "OSMODE" 0)
     (setvar "PDMODE" 3)
     (setvar "PDSIZE" -2.0)
     (setvar "ORTHOMODE" 0)
     (setvar "MENUECHO" 0)
     (setq pt5 (inters pt1 pt2 pt3 pt4 nil))
          (if (null pt5)
          (princ "\n!! 1st Line and 2nd Line are parrallel !!")
          )                            ; But the function terminates
   (command "point" pt5)               ; nonotheless.
      (setvar "OSMODE" OSM)
      (setvar "PDMODE" PDM)
      (setvar "PDSIZE" PDS)
      (command "COLOR" CEC)
      (setvar "CMDECHO" CMD)
      (setvar "ORTHOMODE" ORT)
      (setvar "MENUECHO" MNE)
      (setq *error* olderr)            ; Restore old *error* handler
      (princ)                          ; Land softly on comfy butt
)
;**************** Driver seat
(defun c:mpt () (meetpnt))
(defun c:meetpnt () (meetpnt))
(princ
  "\nStart command with MPT or MEETPNT.")
(princ)
