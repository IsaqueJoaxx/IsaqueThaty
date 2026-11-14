(defun C:ptc ()
  (setvar "cmdecho" 0)
  
  (setq pt1 (getpoint "\nPique o ponto a coordenar: "))
  (if pt1
    (progn
      (setq pt2 (getpoint pt1 "\nPique ponto de inicio do texto: "))
      (if pt2
        (progn
          (setq txtx (strcat "E=" (rtos (car pt1) 2 3) "\n"))
          (setq txty (strcat "N=" (rtos (cadr pt1) 2 3) "\n"))
          (command "leader" pt1 pt2 "Annotation" txtx txty "")
        );progn
      );if
    );progn
  );if
  (setvar "cmdecho" 1)
  (princ)
);defun

(princ "PTC ")
(princ)
