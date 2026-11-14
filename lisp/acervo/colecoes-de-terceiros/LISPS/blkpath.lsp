;From CADENCE Magazine November , 1991
;Page 120

; This program lists the selected entity type and the block containing
; the entity.  If the blocks are nested, a path of containing blocks is
; displayed from inner- to outermost blocks.
;
(defun C:BLKPATH ( / en nlist entname blklist )
  (setq en (car (setq nlist (nentsel))))
  (if (< (length nlist) 4)
    (princ "\nEntity selected is not within a block or is an attribute")
    (progn         ;Entity is non-attribute in a block
      (setq entname (cdr (assoc 0 (entget en))))
      (princ (strcat "\nThe entity selected is " entname))
      (setq blklist (nth 3 nlist))
      (setq blkname (cdr (assoc 2 (entget (car blklist)))))
      (princ (strcat " in block " blkname))
      (setq blklist (cdr blklist))
      (while blklist  ;Display nested blocks (if any)
        (setq blkname (cdr (assoc 2 (entget (car blklist)))))
        (princ (strcat "\n  ...in block " entname))
        (setq blklist (cdr blklist))
        )
      ))
  (princ)
  )
