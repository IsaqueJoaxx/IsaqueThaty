; TIP910.LSP : DTEXTBOX.LSP   Box for Text   (c)1993, G. RAMESH

(defun C:DTEXTBOX (/ s blist x y p1 p2 p3 p4 i)
   (setvar "cmdecho" 0)(setvar "blipmode" 0)
   (setq s (getstring 1 "\nText: "))
   (while (/= s "")
      (setq blist (textbox (list(cons 1 s)))
         x (- (car(cadr blist)) (caar blist))
         y (- (cadr(cadr blist)) (cadr(car blist)))
         p1 (getvar "lastpoint") i 5
      )
      (command ".undo" "g")
      (while (= i 5)
         (setq d (grread 1 6 1))
         (if (and p2 p3 p4) (grvecs (list 260 p1 p2 p2 p3 p3 p4 p4 p1)))
         (setq p1 (cadr d) p2 (polar p1 0 x)
            p3 (polar p2 (/ pi 2) y) p4 (polar p3 pi x)
         )
         (grvecs (list 260 p1 p2 p2 p3 p3 p4 p4 p1))
         (setq i (car d))
      )
      (grvecs (list 260 p1 p2 p2 p3 p3 p4 p4 p1))
      (command ".text" p1 "" 0 s)
      (setq s (getstring 1 "\nText: ") p2 nil)
   ) 
   (command ".undo" "end")
   (prin1)
); end dtextbox.lsp

