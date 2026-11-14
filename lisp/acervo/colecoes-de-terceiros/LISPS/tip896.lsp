;TIP896:  ISO.LSP   Draw an Isometric Box   (C)1993, Tom Johnson

(defun c:ISO () 
   (graphscr)
   (setvar "osmode" 0)
   ( defun dtr (d) (/ (* d pi) 180.0))
   (setq pt1 (getpoint "\nWhere do you want to start your iso box: "))
   (setq a  (dtr 30))  ;These are the set angles of the isometric box.
   (setq b  (dtr 150))
   (setq c  (dtr 210))
   (setq d  (dtr 330))
   (setq e  (dtr -90))
   (setq m (getdist "\nHow deep is the box: ")) ;These are your sizes
   (setq n (getdist "\nHow wide is the box: ")) ;of you box.
   (setq p (getdist "\nHow high is the box: "))
   (setq o m) ;This is so you don't have to repeat you distances.
   (setq r p)
   (setq pt2 (polar pt1 a m)) ;This is for your angle conversion.
   (setq pt3 (polar pt2 b n))
   (setq pt4 (polar pt3 c o))
   (setq pt5 (polar pt1 e r))
   (setq pt6 (polar pt4 e r))
   (setq pt7 (polar pt2 e r))
   (command "line" pt1 pt2 pt3 pt4 "c"  "") ;This draws you lines.
   (command "line" pt1 pt5 "")
   (command "line" pt4 pt6 "")
   (command "line" pt2 pt7 "")
   (command "line" pt6 pt5 "")
   (command "line" pt5 pt7 "")
   (princ)
); end iso_box.lsp

