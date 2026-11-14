; LARRAY.LSP

; DESIGNED TO EQUALLY SPACE BETWEEN REBAR BLOCKS--
; TO LINEAR RECTANGULAR ARRAY EQUALLY BETWEEN TWO EXISTING BLOCKS

(defun C:LARRAY (/ os p1 p2 d1 sa a1 n1 d2 n2)
  (setq os (getvar "osmode")) ;get exist osnap
  (setvar "osmode" 64) ;set osnap to insert
  (initget (+ 1 2 4)) ;disallow null, negative, zero inputs
  (setq p1 (getpoint "\nPick One Block: "))
  (setq p2 (getpoint "\nPick Other Block: "))
  (setq d1 (distance p1 p2))
  (setq sa (getvar "snapang")) ;get exist snapang
  (setq a1 (angle p1 p2))
  (setvar "snapang" a1)
  (princ "The Distance Between the Two Blocks is ")
  (princ d1) (terpri)
  (setq n1 (getint "\nEnter Desired Number of Blocks Inbetween: "))
  (setq d2 (/ d1 (1+ n1)))
  (setq n2 (1+ n1))
  (command "array" p1 "" "r" "" n2 d2)
  (setvar "osmode" os) ;restore original osnap setting
  (setvar "snapang" sa) ;restore original snapang setting
  (princ)
)
