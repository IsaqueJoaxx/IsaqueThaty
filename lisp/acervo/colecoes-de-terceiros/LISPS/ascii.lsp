; ASCII.LSP  (copyright) 1988, Barry Bowen
(defun c:ascii_chart (/ in)
 (setq in (getint "\nBegining Number: "))
 (repeat 11
  (princ in) (princ " = ") 
  (princ (chr in))
  (setq in (1+ in))
  (repeat 4 (chr_prn))
  (princ "\n\n")
 )
 (princ)
)

(defun chr_prn ()
 (princ (strcat "\t\t" (itoa in))) 
 (princ " = ") (princ (chr in))
 (setq in (1+ in))
)
