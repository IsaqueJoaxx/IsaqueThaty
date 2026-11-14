;TIP834:  LP.LSP (c)1993, J. Daniels squires

;Displays the "lastpoint" entered into the database
;Ver. 1.0
;----------------------------------------------------------

(defun C:LP ()
  (setq VS (getvar "viewsize"))
  (setq PTSZ (/ VS 100))
  (setq LP (getvar "lastpoint"))
  (setq CP1 (list (- (car LP)(/ PTSZ 2))(- (cadr LP)(/ PTSZ 2.0))))
  (setq CP2 (list (+ (car CP1) PTSZ)(+ (cadr CP1) PTSZ)))
  (setq CP3 (list (car CP1)(cadr CP2)))
  (setq CP4 (list (car CP2)(cadr CP1)))
  (setq XLP (list 'X:(car LP) 'Y:(cadr LP) 'Z:(caddr LP)))
  (princ "\nCurrent database point at: ")(princ XLP)
  (repeat 31
    (grdraw CP1 CP2 -1 1)
    (grdraw CP3 CP4 -1 1)
  )
  (prin1)
); end lp.lsp

