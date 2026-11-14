(defun C:AAR ( / ss ang bp cnt dist d)
   (cond
      (  (setq ss (ssget))
         (initget 1)
         (setq bp (getpoint "\nBase point: "))
         (initget 1)
         (setq ang
            (getangle bp "\nArray direction: "))
         (initget 7)
         (setq dist
            (getdist "\nDistance between objects: "))
         (initget 7)
         (setq cnt (getint "\nNumber of objects: "))
         (setq d 0.0)
         (setvar "cmdecho" 0)
         (command "_.undo" "_g"
                  "_.copy" ss "" "_m" bp)
         (repeat (1- cnt)
            (command
               (polar bp ang (setq d (+ d dist)))))
         (command "" "_.undo" "_e")
      )
   )
   (princ)
)
