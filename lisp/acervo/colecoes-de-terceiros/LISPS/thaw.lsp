; THAW.LSP
; LAYER Thaw w/o the REGEN

; Alacrity
; 12405 SE 25th St
; Bellevue WA 98005
; Voice: (206)746-0680
; BBS: (206)643-LISP
; PPN: 73417,1756

; Brief Description
; ~~~~~ ~~~~~~~~~~~
; THAW.LSP prevent a total drawing REGEN when thawing layers.  The main
; principal is to set REGENAUTO Off so that the normal REGEN does not
; occur when thawing layers.
;
; AutoCAD does not need to REGEN when thawing layers if a REGEN has not
; occured since the layers were frozen.  In such cases it simply REDRAWs the
; entities, thus saving time.
;
; The only hitch that THAW.LSP must overcome is determining if AutoCAD has
; REDRAWn the thawed layers.  This is done by testing the status of a LINE
; found on a thawed layer.  THAW.LSP >NEEDS< this line to be able to
; detemine whether the thawed layer needs to be REGENed.
;
; !! NOTICE !!
; ~~~~~~~~~~~~
; Your drawing must have at least one LINE on every layer to use THAW.LSP
; effectively!  It is okay to place this LINE outside of the limits of the
; drawing so that it's not in the way during editing and plotting.
;
; THAW.LSP Usage
; ~~~~~~~~ ~~~~~
; To load THAW.LSP type (at the command prompt):
;
;      Command: (LOAD "/PATHNAME/THAW") [ENTER]
;
; The 'PATHNAME' is the directory that THAW.LSP is located.  You may want
; to use a menu macro like this one (for automatic loading):
;
;      [THAW:]^C^C^P(IF (NOT C:THAW) (LOAD "/PATHNAME/THAW"));^PTHAW
;
; To use THAW.LSP simply type (at the command prompt or use the above
; menu macro):
;
;      Command: THAW [ENTER]
;      Layer name: LAYERNAME [ENTER]
;
; The 'LAYERNAME' is the layer name that you wish to THAW.  THAW.LSP will
; continue to prompt for layer names until you hit [ENTER] with no input.
; THAW.LSP will not permit you to enter non-existant layer names or
; layer names that are not frozen.
;
; Kudos
; ~~~~~
; This THAW.LSP is based apon the THAW.LSP that Michael Quinlan originally
; created.  He gets all credit for creativity.  No part of Michael
; Quinlan's code has been used in the creation of this THAW.LSP.
;
; Comments are encouaged.
; Regards, Jason Osgood (1/12/89)

(defun C:THAW  (/ regen i ss ename lname llist)
  ;-------------------------
  ; Get Layer Names to THAW
  ;-------------------------
  (while (/= (setq lname (getstring "\nLayer name: ")) "")
    (cond
      ((not (setq i (tblsearch "LAYER" lname)))
        (princ "\nLayer not found! - Try again.")
      )
      ((zerop (boole 1 1 (cdr (assoc 70 i))))
        (princ "\nLayer not frozen! - Try again.")
      )
      (T (setq llist (append (list lname) llist)))
    )
  )
  ;-------------
  ; THAW Layers
  ;-------------
  (setq regen (getvar "RegenMode"))
  (setvar "RegenMode" 0)
  (setvar "CmdEcho" 0)
  (command ".LAYER")
  (foreach
    lname
    llist
    (command "T" lname)
  )
  (command "")
  (setvar "RegenMode" regen)
  (setvar "CmdEcho" 1)
  ;--------------------------------------------------------
  ; Test to see if already redrawn or needs to be regened.
  ; Assumes existance of a LINE on thawed layer...
  ;--------------------------------------------------------
  (setq i -1)
  (while
    (and
      (setq lname (nth (setq i (1+ i)) llist))
      (not (setq ss (ssget "X" (list (cons 0  "LINE") (cons 8 lname)))))
    )
  )
  (setq ename (ssname ss 0)
        pt (cdr (assoc 10 (entget ename)))
  )
  (cond
    ((ssmemb ename (ssget "C" (polar pt 0.785 1.0) (polar pt 3.926 1.0)))
      (princ "\nNo REGEN necessary.")
    )
    (T
      ; Regen entities
      (foreach
        lname
        llist
        (setq ss (ssget "X" (list (cons 8 lname)))
            i -1)
        (while (setq ename (ssname ss (setq i (1+ i))))
          (entupd ename)
        )
      )
    )
  )
  (princ)
)
; End of File
