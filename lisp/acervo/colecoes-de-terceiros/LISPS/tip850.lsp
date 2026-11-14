; TIP850: BLO.LSP (c)1993, J. Todd Owen
; Changes all entities within selected block to layer 0

(defun C:BL0 (/ CL ENAME ENT)
  (setvar "CMDECHO" 0)    
  (setvar "HIGHLIGHT" 0)
  (setq CL (getvar "CLAYER"))
  (command ".LAYER" "S" "0" "")
  (setq ENAME nil ENT nil)
  (while (= ENAME nil)
    (setq ENAME (car (entsel "\nSelect block to redefine on layer 0: ")))
  )
  (setq ENT (entget ENAME))
  (if (= (ITEM 0 ENT) "INSERT")
    (progn
      (command ".COPY" ENAME "" "@" "@")
      (setq ENAME (entlast))
      (BLOCK0 ENAME)
    )
    (progn (princ "\nError: A block must be selected.") (C:BL0))
  )
  (command ".LAYER" "S" CL "")
  (setvar "HIGHLIGHT" 1)
  (setvar "CMDECHO" 1)
  (princ)
)

(defun BLOCK0 (ENAME / ENAME2 ENT INSPT BLKNAME SSBLK)
  (setq ENT (entget ENAME))
  (setq INSPT (ITEM 10 ENT))
  (setq BLKNAME (ITEM 2 ENT))
  (setq ENT (subst (cons 41 1) (assoc 41 ENT) ENT)) ;x-scale
  (setq ENT (subst (cons 42 1) (assoc 42 ENT) ENT)) ;y-scale
  (setq ENT (subst (cons 43 1) (assoc 43 ENT) ENT)) ;z-scale
  (setq ENT (subst (cons 50 0) (assoc 50 ENT) ENT)) ;rotation angle
  (entmod ENT)
  (command ".EXPLODE" ENAME)
  (setq ENAME (entnext ENAME))
  (setq SSBLK (ssadd))
  (while (/= ENAME nil)
    (setq ENT (entget ENAME))
    (setq ENT (subst (cons 8 "0") (assoc 8 ENT) ENT)) ;layer
    (setq ENT (subst (cons 62 256) (assoc 62 ENT) ENT)) ;color
    (setq ENT (subst (cons 6 "BYLAYER") (assoc 6 ENT) ENT)) ;ltype
    (entmod ENT)
    (setq SSBLK (ssadd ENAME SSBLK))
    (if (= (ITEM 0 ENT) "INSERT") ;if ENAME is a block
      (progn
        (command ".COPY" ENAME "" "@" "@")
        (setq ENAME2 (entlast))
        (BLOCK0 ENAME2)
      )
    )
    (setq ENAME (entnext ENAME))
  )
  (command ".BLOCK" BLKNAME "Y" INSPT SSBLK "")
)

(defun ITEM (N ENT)
  (cdr (assoc N ENT))
)

