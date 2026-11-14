; TIP906.LSP   Toggle Tilemode Variable   TM.LSP (C)1993, Gary K. Sim

(defun C:TM ()
   (command "tilemode" (abs (+ (getvar "tilemode") -1)))
); end tm.lsp
