;;C2C.lsp created by Sergio Queiroz - Brazil
;;smq@zipmail.com.br
;;
(defun c:C2C()
(setvar "cmdecho" 0)
(setq a (command "tilemode" "0"))
(setq pt (getpoint " \nStart point: "));
(setq pt1 (polar pt 1.5708 297)) (setq pt2 (polar pt1 0 210));
(setq pt3 (polar pt2 4.7124 297)) (command "line" pt pt1 pt2 pt3 "close");
(command "zoom" "e")
(command "mview" pt pt2)
(command "mspace")
(command "zoom" "e")
(command "regen")
(command "copylink")
(princ)
)
