Labeling N,E Coordinates with Leader - David B. Stewart
(princ "\nDigite: CD")
(defun C:cd(/ PNT1 P1X P1Y STDY DY COORDN COORDE PTXT)
(while    
    (setq PNT1 (getpoint
      "\nPick coordinate point: "))
    (setq P1Y (cadr pnt1)) ;y coord
    (setq P1X (car pnt1)) ;x coord
    (setq STDY (rtos P1Y 2 4))
    (setq STDX (rtos P1X 2 4))
    (setq COORDE (strcat "E=" STDX  ))
    (setq COORDN (strcat "N=" STDY  ))
    (setq PTXT (getpoint
      "\nPick text location: "))
    (command "LEADER" PNT1 PTXT "" COORDN COORDE ""))
    (princ)
)

