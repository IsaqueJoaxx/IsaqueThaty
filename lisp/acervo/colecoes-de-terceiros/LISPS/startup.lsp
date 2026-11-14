;;;
;;; Set these defaults when loading the routine.
;;;
(defun S::L2CSTARTUP ()
    (if (null dl:ecp) (setq dl:ecp 4))    ; default to auto endcaps
    (if (null dl:snp) (setq dl:snp T))    ; default to snapping ON
    (if (null dl:brk) (setq dl:brk T))    ; default to breaking ON
    (if (null dl:osd) (setq dl:osd 0))    ; default to center alignment
)
