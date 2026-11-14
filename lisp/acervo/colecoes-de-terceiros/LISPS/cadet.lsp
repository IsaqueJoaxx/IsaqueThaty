;CADET Version 3.0 Installation begins.
;CADET note: Remove the semicolon below to disable CADET's memory management.
;(SETQ CADETM T)
;CADET note: Remove the semicolon below to force sort by LLH text coordinates.
;(SETQ CADETS T)
;CADET note: You may rename CC, CE, CI or CN below, but not the C?30 functions.
(defun C:CC () (C:CC30))
(defun C:CE () (C:CE30))
(defun C:CI () (C:CI30))
(defun c:CN () (C:CN30))
;CADET note: Do not rename the C?30 functions below.
(defun C:CC30 () (prompt "\nLoading...") (load"CC30") (prompt "ed.") (C:CC30))
(defun C:CE30 () (prompt "\nLoading...") (load"CE30") (prompt "ed.") (C:CE30))
(defun C:CI30 () (prompt "\nLoading...") (load"CI30") (prompt "ed.") (C:CI30))
(defun C:CN30 () (prompt "\nLoading...") (load"CN30") (prompt "ed.") (C:CN30))
(SETQ CADETD (getenv "CADET"))
;CADET Version 3.0 Installation ends.
