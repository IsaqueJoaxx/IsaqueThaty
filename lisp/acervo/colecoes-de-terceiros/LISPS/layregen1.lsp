;                         LAYREGEN.LSP      Originally Written 04/29/92
; David H. Marble                                Last Revision 12/18/92
; Routine to regen entities on a layer that was frozen before the
; last regen, or regen layer by picking entity on screen. Uses move
; command to regen entities.

(defun *error* (MSG)
(setvar "cmdecho" OCM)(setvar "osmode" OSM)
(setq OCM nil OSM nil LAY nil NAM nil ENT nil IT nil SS nil)
(princ)
)

(defun TYPE ()
  (if (tblsearch "layer" LAY)
    (progn
    (princ (strcat "Thawing layer - " LAY))
    (command "Layer" "T" LAY "")
    (DOIT)
    )
  (princ (strcat "\nLayer " LAY " does not exist!"))
  )
)

(defun PICK ()
(setq NAM (entsel "Pick object on layer to regen: ") ENT (entget (car NAM)) LAY (cdr(assoc 8 ENT)))
(DOIT)
)

(defun DOIT ()
(princ (strcat "\nSearching for entities on layer - " LAY))
  (if (setq SS (ssget "x" (list (cons 8 LAY))))
    (progn
    (princ (strcat "\nRegenerating layer - " LAY))
    (command "move" SS "" "0,0,0" "0,0,0")
    )
    (progn
    (princ (strcat "\nLayer " LAY " is empty!"))
    )
  )
)

(defun LRX-IT ()
(setvar "cmdecho" OCM)(setvar "osmode" OSM)
(setq OCM nil OSM nil LAY nil NAM nil ENT nil IT nil SS nil)
(princ)
)

(defun C:LAYREGEN (/ OCM OSM LAY NAM ENT IT SS)
(graphscr)
(setq OCM (getvar "cmdecho") OSM (getvar "osmode"))
(setvar "cmdecho" 0)(setvar "osmode" 0)
  (if (= (setq LAY (strcase (getstring "\nType layer name to thaw and regen/Enter to select object: "))) "")
  (PICK)(TYPE)
  )
(LRX-IT)
)

