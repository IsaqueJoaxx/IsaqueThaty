; EXTFILE reads the outside database:
;
(defun EXTFILE ()
  (setq FILELIST () )
  (setq FILE (open "TEST.SDF" "r"))
  (setq RECORD (read-line FILE))
  (while RECORD
    (setq ROOMNO (substr RECORD 1 3))
    (setq EMPNAME (substr record 6))
    (setq RECLIST (list ROOMNO EMPNAME))
    (setq FILELIST (cons RECLIST FILELIST))
    (setq RECORD (read-line FILE))
  )
  (close FILE)
)
