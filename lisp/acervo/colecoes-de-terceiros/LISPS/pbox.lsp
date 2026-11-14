; PBOX.LSP

; DRAWS A RECTANGULAR BOX AS A POLYLINE

(defun C:PBOX (/ pt dpt )
(setq pt (getpoint "\nStarting corner: "))
(setq dpt (getcorner pt "\nDiagonal corner: "))
(command "pline" pt ".x" dpt ".y" pt dpt ".x" pt ".y" dpt "C")
)
