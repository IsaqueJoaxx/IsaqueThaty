;***Beginning of WT.LSP***

(Defun C:WT()
 (setq textin nil)
 (setq linecount 0)
 (write-line "Select text to be written to a file: ")
 (setq scratch1 (open "scratch1.txt" "w"))
 (setq textin (ssget))
 (setq counter 0)
 (setq l (sslength textin))  ; # of loops
;look at each of the entities in the set
 (repeat l
   (progn
;if the entity is a "TEXT" then
    (if (= "TEXT" (cdr (assoc 0 (setq e (entget (ssname textin counter))))))
     (progn 
      (setq counter (1+ counter))
;extract out the y value and the string
;HERE'S THE CHANGE: WE ADD A MILLION TO EACH OF THE Y'S BEFORE SORTING
      (setq yval (rtos (+ 1000000 (last (assoc 10 e))) 2 4))
      (setq text1 (cdr (assoc 1 e)))
;and write them to the scratch file #1
      (write-line (strcat yval " " text1) scratch1)
;increment a counter for the number of text strings
      (setq linecount (1+ linecount ))
    )
    )
   )
 )
;shell out to a dos sort command and write scratch #2
 (close scratch1)
 (command "shell" "type scratch1.txt|sort>scratch2.txt /r")
;strip off the y's and write it out to output file
 (setq outfile (getstring "Enter name of output file: "))
 (setq ofile (open outfile "w"))
 (setq infile (open "scratch2.txt" "r"))
 (repeat linecount
  (progn
   (setq textin (read-line infile))
   (setq thechar nil)
   (setq counter 0)
   (while (/= thechar " ")
     (progn
        (setq counter (1+ counter))
        (setq thechar (substr textin counter 1))
     )
   )
   (write-line (substr textin (1+ counter)) ofile)
  )
 )
 (close ofile)
 (close infile)
 (setq commandstring (strcat "type " outfile))
 (command "shell" commandstring)
)

;***End of WT.LSP***
