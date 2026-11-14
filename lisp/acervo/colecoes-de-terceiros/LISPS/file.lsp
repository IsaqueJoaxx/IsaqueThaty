; This function will test the file handeling capibilities
; of AutoLISP.
; and is provided as an example of file access.
; Programming AutoCAD Example # 2-95
;
(Defun c:FileTest ()
   (setq file (open "file.dat" "w")) ; opens a file named file.dat in 
                                     ; write mode and assigns file as 
                                     ; its File handle. 
   (setq data (getstring "Enter Text "))  ; get test string to use
   (prin1 data file)                 ; write test string to file
   (print () file)                   ; write newline to file
   (setq file (close file))          ; close file
   (setq file (open "file.dat" "r")) ; reopens a file.dat in read
                                     ; mode 
   (setq new (read-line file))       ; read line from file into new
   (prompt new)                      ; print new to screen
   (setq file (close file))          ; close file
   )
