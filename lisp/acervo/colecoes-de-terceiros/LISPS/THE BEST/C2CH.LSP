;;C2CH.lsp created by Sergio Queiroz - Brazil
;;smq@zipmail.com.br
;;
(defun c:C2CH()
(setvar "cmdecho" 0)
(setq a (command "tilemode" "0"))
(setq pt (getpoint "\n Star point: "));
(setq pt1 (polar pt 1.5708 210)) (setq pt2 (polar pt1 0 297));
(setq pt3 (polar pt2 4.7124 210)) (command "line" pt pt1 pt2 pt3 "close");
(command "zoom" "e")
(command "mview" pt pt2)
(command "mspace")
(command "zoom" "e")
(command "regen")
(command "copylink")
(princ)
)
