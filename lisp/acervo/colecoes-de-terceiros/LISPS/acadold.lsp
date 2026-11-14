; The following function will clean the lisp variables from this point down.
; Any user programs that must stay resident should be loaded ahead of this
; program.  RNM

(defun c:clean ()
    (setq atomlist (member 'c:clean atomlist))    
    'done    
)
;THE DTR FUNCTION CONVERTS DEGREES TO RADIANS
 
;THE RTD FUNCTION CONVERTS RADIANS TO DEGREES
(VMON)
(DEFUN DTR (A)
  (* PI (/ A 180.0))
  )
;
(DEFUN RTD (A)
  (/ (* A 180.0) PI)
)
;
  (command "ltscale" "25")
