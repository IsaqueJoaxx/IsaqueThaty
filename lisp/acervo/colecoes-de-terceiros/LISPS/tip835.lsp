;TIP835: PAT.LSP (c)1993, Jody Lund

(defun C:PAT ()
  (setq NO (getstring "Enter number: "))
  (command "attedit" "" "" "number" NO "c" "0,0" "16300,20000")
  (princ)
); end pat.lsp


