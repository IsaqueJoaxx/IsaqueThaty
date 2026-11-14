; TIP959.LSP: CTR.LSP   Centers for Circles   (c)1994, Mark H. Miller

(defun C:CTR (/ i ss)
   (setq ss (ssget "x" '((0 . "circle"))))
   (setq i 0)
   (command ".dim")
   (repeat (sslength ss)
      (command "center" (list (ssname ss i) '(0 0)))  ;note that .center won't work
   (setq i (1+ i)))
   (command "exit")
); end ctr.lsp
